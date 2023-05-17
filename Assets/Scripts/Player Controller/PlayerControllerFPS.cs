using UnityEngine;
using Mirror;

public class PlayerControllerFPS : NetworkBehaviour
{
    #region Variables and References


    [Header("Player Health")]
    [SyncVar] public int Health = 100;
    [SyncVar] public int maxHealth = 100;

    private Rigidbody rb;

    [Header("Player Camera")]
    [SerializeField] private Transform playerCam;
    [SerializeField] private Transform orientation;
    [SerializeField] private float sensitivityX;
    [SerializeField] private float sensitivityY;
    private float xRotation;
    private float yRotation;

    [Header("Player Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float wallRunSpeed;
    [HideInInspector] public bool wallRunning;
    private float moveSpeed;
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private MovementStates state;

    [Header("Ground Check")]
    [SerializeField] private float groundDrag;
    [SerializeField] private LayerMask whatIsGround;
    private float playerHeight;
    private bool isGrounded;

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCoolDown;
    [SerializeField] private float airMultiplier;
    private bool readyToJump;

    [Header("Crouching")]
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float crouchYScale;

    [Header("Key Bindings")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    

    #endregion

    private void Start()
    {
        playerHeight = transform.localScale.y;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
        UIManager.instance.UpdateHP(Health, maxHealth);
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    private void Update()
    {
        // ground check:
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.7f, whatIsGround);

        MouseLookAround();
        MyInput();
        SpeedControl();
        StateHandler();

        // handle drag:
        if (isGrounded)
           rb.drag = groundDrag;
        else
            rb.drag = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    #region Player Movement

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(jumpKey) && readyToJump && isGrounded)
        {
            Debug.Log("Jump");
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCoolDown);
        }
        if (Input.GetKey(crouchKey))
            StartCrouch();
        if (Input.GetKeyUp(crouchKey))
            StopCrouch();
    }


    private void MovePlayer()
    {
        // move in the direction the player is currently looking at:
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on ground
        if (isGrounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        // in air:
        else if (!isGrounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
    }

    private void SpeedControl()
    {
        // get current velocity:
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        // limit velocity if needed:
        if (flatVelocity.magnitude > moveSpeed)
        {
            Vector3 limitVelocity = flatVelocity.normalized * moveSpeed;
            rb.velocity = new Vector3(limitVelocity.x, rb.velocity.y, limitVelocity.z);
        }
    }

    #endregion

    #region Crouching

    private void StartCrouch()
    {
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        if (rb.velocity.magnitude > 0.5f)
        {
            if (isGrounded)
            {
                rb.AddForce(orientation.transform.forward * 350);
            }
        }
    }

    private void StopCrouch()
    {
        transform.localScale = new Vector3(transform.localScale.x, playerHeight, transform.localScale.z);
    }

    #endregion

    #region Player Jump

    private void Jump()
    {
        // reset y-axis velocity so that everytime player jumps at equal height:
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        // add impulsive force in upward direction:
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        ResetJump();
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    #endregion

    private void StateHandler()
    {
        // Wall running Mode
        if (wallRunning)
        {
            state = MovementStates.wallrunning;
            moveSpeed = wallRunSpeed;
        }
        // Sprint Mode
        if (Input.GetKey(sprintKey) && isGrounded)
        {
            state = MovementStates.sprint;
            moveSpeed = sprintSpeed;
        }
        // Crouching Mode
        else if (Input.GetKey(crouchKey))
        {
            state = MovementStates.crouching;
            moveSpeed = crouchSpeed;
        }
        // Walking Mode
        else if (isGrounded)
        {
            state = MovementStates.walking;
            moveSpeed = walkSpeed;
        }
        // Jumping Mode
        else
        {
            state = MovementStates.jump;
        }
    }

    public enum MovementStates
    {
        walking,
        crouching,
        sprint,
        jump,
        wallrunning,
    }

    #region Camera Look-Around
    private void MouseLookAround()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivityY * Time.deltaTime;

        // This is confusing but this is how unity calculates rotations and inputs:
        yRotation += mouseX;
        //yRotation = Mathf.Clamp(yRotation, -90, 90);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90, 90);

        // rotate camera on both axis:
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);
        // rotate player on y-axis:
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
    #endregion

}
