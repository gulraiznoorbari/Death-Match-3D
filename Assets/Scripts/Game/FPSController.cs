using System.Collections;
using UnityEngine;
using Mirror;

public class FPSController : NetworkBehaviour, IDamageable
{
    #region Variables and References

    [Header("Player stats")]
    [SyncVar] public int Health = 100;
    [SyncVar] public int HealthMax = 100;
    [SyncVar] public int Kills;
    [SyncVar] public int Deaths;
    [SyncVar] public bool isDead;

    private Rigidbody rb;

    int idle, walk, run, crouch, jump;

    [Header("Player Camera")]
    [SerializeField] private Transform playerCam;
    [SerializeField] private Transform orientation;
    [SerializeField] private float sensitivityX;
    [SerializeField] private float sensitivityY;
    private float xRotation;
    private float yRotation;

    [Header("Player Movement")]
    //[SerializeField] private Animator animator;
    //[SerializeField] private NetworkAnimator networkAnimator;
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

    [Header("Weapon")]
    [SerializeField] Transform weaponMuzzle;
    [SyncVar] public int AmmoCountMax = 30;
    [SyncVar] public int AmmoCount = 30;
    [SyncVar] bool Reloading;
    [SerializeField] double reloadTime = 2;
    [SerializeField] int weaponDamage = 10;
    [SerializeField] int range = 20;
    [SerializeField] GameObject bulletHolePrefab;
    [SerializeField] GameObject bulletFXPrefab;
    [SerializeField] GameObject bulletBloodFXPrefab;
    [SerializeField] float WeaponCooldown;
    [SerializeField] private bool allowButtonHold;
    float curCooldown;
    bool shooting ;
    
    //[Header("GFX")]
    //[SerializeField] GameObject[] disableOnClient;
    //[SerializeField] GameObject[] disableOnDeath;

    [Header("Key Bindings")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    #endregion

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        /*
        idle = Animator.StringToHash("idle");
        walk = Animator.StringToHash("walking");
        run = Animator.StringToHash("run");
        crouch = Animator.StringToHash("crouch");
        jump = Animator.StringToHash("jump");
        */

        if (isLocalPlayer)
        {
            //It is local player.

            //Setup FPS camera.
            playerHeight = transform.localScale.y;
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            readyToJump = true;

            CanvasManager.instance.ChangePlayerState(true);
            CanvasManager.instance.UpdateHP(100, 100);
            CanvasManager.instance.localPlayer = this;
            CanvasManager.instance.AmmoCountText.text = AmmoCount.ToString() + "/" + AmmoCountMax.ToString();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        curCooldown -= Time.deltaTime;

        if (!isDead)
        {
            // ground check:
            isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.7f, whatIsGround);

            MouseLookAround();
            MyInput();
            SpeedControl();
            StateHandler();

            if (allowButtonHold) shooting = Input.GetKey(KeyCode.Mouse0);
            else shooting = Input.GetKeyDown(KeyCode.Mouse0);

            if (shooting)
            {
                Shoot();
            }

            if (Input.GetKeyDown(KeyCode.R))
                StartReload();

            // handle drag:
            if (isGrounded)
                rb.drag = groundDrag;
            else
                rb.drag = 0;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    #region State Handler and Movement States

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

    #endregion 
  
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
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
            //networkAnimator.SetTrigger(walk);
        }
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
    
    #region Crouch

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
    
    #region Jump

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

    #region Shooting & FX

    internal void Shoot()
    {
        //First local if can shoot check.
        //if ammoCount > 0 && isAlive
        if (AmmoCount > 0 && !isDead && curCooldown < 0.01f)
        {
            //Do command
            CmdTryShoot(playerCam.transform.forward, playerCam.transform.position);
            curCooldown = WeaponCooldown;
        }
    }

    [Command]
    void CmdTryShoot(Vector3 clientCam, Vector3 clientCamPos)
    {
        //Server side check
        //if ammoCount > 0 && isAlive
        if (AmmoCount > 0 && !isDead)
        {
            AmmoCount--;
            TargetShoot();

            RaycastHit hit;
            if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out hit, range))
            {
                Debug.Log("SERVER: Player shot: " + hit.collider.name);
                if (hit.collider.CompareTag("Player"))
                {
                    RpcPlayerFiredEntity(GetComponent<NetworkIdentity>().netId, hit.collider.GetComponent<NetworkIdentity>().netId, hit.point, hit.normal);
                    hit.collider.GetComponent<FPSController>().Damage(weaponDamage, GetComponent<NetworkIdentity>().netId);
                }
                else
                {
                    RpcPlayerFired(GetComponent<NetworkIdentity>().netId, hit.point, hit.normal);
                }
            }
        }

    }

    [TargetRpc]
    void TargetShoot()
    {
        //We shot successfully.
        //Update UI
        CanvasManager.instance.AmmoCountText.text = AmmoCount.ToString() + "/" + AmmoCountMax.ToString();
    }

    [ClientRpc]
    void RpcPlayerFired(uint shooterID, Vector3 impactPos, Vector3 impactRot)
    {
        Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot));
        Instantiate(bulletFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        NetworkIdentity.spawned[shooterID].GetComponent<FPSController>().MuzzleFlash();
    }

    [ClientRpc]
    void RpcPlayerFiredEntity(uint shooterID, uint targetID, Vector3 impactPos, Vector3 impactRot)
    {
        Instantiate(bulletHolePrefab, impactPos + impactRot * 0.1f, Quaternion.LookRotation(impactRot), NetworkIdentity.spawned[targetID].transform);
        Instantiate(bulletBloodFXPrefab, impactPos, Quaternion.LookRotation(impactRot));
        NetworkIdentity.spawned[shooterID].GetComponent<FPSController>().MuzzleFlash();
    }

    public void MuzzleFlash()
    {
        weaponMuzzle.GetComponent<ParticleSystem>().Play();
    }

    #endregion

    #region Gun Damage

    [Server]
    public void Damage(int amount, uint shooterID)
    {
        Health -= amount;
        TargetGotDamage();
        if (Health < 1)
        {
            Die();
            NetworkIdentity.spawned[shooterID].GetComponent<FPSController>().Kills++;
            NetworkIdentity.spawned[shooterID].GetComponent<FPSController>().TargetGotKill();
        }
    }

    [Server]
    public void Die()
    {
        Deaths++;
        isDead = true;
        Debug.Log("SERVER: Player died.");
        TargetDie();
        //RpcPlayerDie();
    }

    [TargetRpc]
    void TargetDie()
    {
        //Called on the died player.
        CanvasManager.instance.ChangePlayerState(!isDead);
        Debug.Log("You died.");
    }

    [TargetRpc]
    public void TargetGotKill()
    {
        Debug.Log("You got kill.");
    }

    [TargetRpc]
    public void TargetGotDamage()
    {
        CanvasManager.instance.UpdateHP(Health, HealthMax);
        Debug.Log("We got hit!");
    }

#endregion

    #region Gun Reload

    [Client]
    internal void StartReload()
    {
        if (Reloading || AmmoCount != AmmoCountMax)
            CmdTryReload();
    }

    [Command]
    void CmdTryReload()
    {
        if (Reloading || AmmoCount == AmmoCountMax)
            return;

        StartCoroutine(reloadingWeapon());
    }

    IEnumerator reloadingWeapon()
    {
        Reloading = true;
        yield return new WaitForSeconds((float)reloadTime);
        AmmoCount = AmmoCountMax;
        TargetReload();
        Reloading = false;

        yield return null;
    }

    [TargetRpc]
    void TargetReload()
    {
        //We reloaded successfully.
        //Update UI
        CanvasManager.instance.AmmoCountText.text = AmmoCount.ToString() + "/" + AmmoCountMax.ToString();
    }

#endregion

    #region Respawning
    [Command]
    public void CmdRespawn()
    {
        //Check if dead
        if (isDead)
        {
            Health = HealthMax;
            AmmoCount = AmmoCountMax;
            isDead = false;
            TargetRespawn();
            //RpcPlayerRespawn();
        }
    }

    [TargetRpc]
    void TargetRespawn()
    {
        CanvasManager.instance.ChangePlayerState(true);
        CanvasManager.instance.UpdateHP(Health, HealthMax);
        //set position
        transform.position = NetworkManager.singleton.GetStartPosition().position;

    }
    #endregion


    //[ClientRpc]
    //void RpcPlayerDie()
    //{
    //    foreach (GameObject item in disableOnDeath)
    //    {
    //        item.SetActive(false);
    //    }
    //}

    //[ClientRpc]
    //void RpcPlayerRespawn()
    //{
    //    foreach (GameObject item in disableOnDeath)
    //    {
    //        item.SetActive(true);
    //    }
    //}
    
}
