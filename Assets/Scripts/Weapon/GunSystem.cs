using System.Collections;
using UnityEngine;
using Mirror;

public class GunSystem : NetworkBehaviour
{
    [SerializeField] private PlayerControllerFPS fpsController;

    [Header("Gun Stats")]
    [SerializeField] private int damage;
    [SerializeField] private float timeBetweenShooting, spread, range, reloadTime, timeBetweenShots;
    [SerializeField] private int magazineSize, bulletsPerTap;
    [SerializeField] private bool allowButtonHold;
    private int bulletsLeft, bulletsShot;

    //booleans 
    private bool shooting, readyToShoot, reloading;

    [Header("Reference")]
    [SerializeField] private Camera fpsCam;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private RaycastHit rayHit;
    [SerializeField] private LayerMask whatIsEnemy;

    [Header("Graphics")]
    [SerializeField] private GameObject bulletHoleGraphic;
    [SerializeField] private ParticleSystem muzzleFlash;

    [SyncVar] private int currentHealth;

    private void Awake()
    {
        bulletsLeft = magazineSize;
        readyToShoot = true;
        currentHealth = fpsController.Health;
    }
    
    private void Update()
    {
        MyInput();
        // Set Ammo Text
        UIManager.instance.AmmoCountText.SetText(bulletsLeft + " / " + magazineSize);
    }
    
    private void MyInput()
    {
        if (allowButtonHold) shooting = Input.GetKey(KeyCode.Mouse0);
        else shooting = Input.GetKeyDown(KeyCode.Mouse0);

        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading) StartReload();

        //Shoot
        if (readyToShoot && shooting && !reloading && bulletsLeft > 0){
            Debug.Log("hello");
            bulletsShot = bulletsPerTap;
            Shoot();
        }
    }

    private void Shoot()
    {
        muzzleFlash.Play();
        readyToShoot = false;

        //Spread
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);

        //Calculate Direction with Spread
        Vector3 direction = fpsCam.transform.forward + new Vector3(x, y, 0);

        if (Physics.Raycast(fpsCam.transform.position, direction, out rayHit, range))
        {
            Debug.Log(rayHit.collider.name);

            if (rayHit.collider.CompareTag("Player"))
                // rayHit.collider.GetComponent<GunSystem>().TakeDamage(damage);
                TakeDamage(damage);
        }

        //Graphics
        Instantiate(bulletHoleGraphic, rayHit.point, Quaternion.FromToRotation(Vector3.forward, rayHit.normal));

        bulletsLeft--;
        bulletsShot--;

        Invoke("ResetShot", timeBetweenShooting);

        if(bulletsShot > 0 && bulletsLeft > 0)
            Invoke("Shoot", timeBetweenShots);
    }

    private void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UIManager.instance.UpdateHP(currentHealth, fpsController.maxHealth);
        Debug.Log("Shot player");
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
            Debug.Log("Player Dead");
        } 
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }

    private void StartReload()
    {
        StartCoroutine(Reload());
    }

    private IEnumerator Reload()
    {
        reloading = true;
        yield return new WaitForSeconds(reloadTime);
        bulletsLeft = magazineSize;
        reloading = false;
    }
}
