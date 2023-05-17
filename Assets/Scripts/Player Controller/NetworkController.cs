using UnityEngine;
using Mirror;

public class NetworkController : NetworkBehaviour
{
    [SerializeField] private FPSController fpsController;
    //[SerializeField] private GunSystem gun;
    [SerializeField] private Camera playerCam;

    private void Awake()
    {
        fpsController = GetComponent<FPSController>();
    }

    private void Start()
    {
        if (!isLocalPlayer)
        {
            fpsController.enabled = false;
            //gun.enabled = false;
            playerCam.enabled = false;
            playerCam.GetComponent<AudioListener>().enabled = false;
        }
    }
}
