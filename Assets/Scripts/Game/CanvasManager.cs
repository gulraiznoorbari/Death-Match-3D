using UnityEngine;
using TMPro;

public class CanvasManager : MonoBehaviour
{
    public static CanvasManager instance;

    public FPSController localPlayer;

    [Header("Panels")]
    [SerializeField] GameObject UI_Alive;
    [SerializeField] GameObject UI_Death;

    [Header("HUD")]
    public TextMeshProUGUI AmmoCountText;
    public Transform HPBar;

    [Header("Death screen")]
    public TextMeshProUGUI killsCount;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

    }

    public void ChangePlayerState(bool isAlive)
    {
        UI_Alive.SetActive(isAlive);
        UI_Death.SetActive(!isAlive);

        //Update stats ui
        if (!isAlive)
        {
            killsCount.text = "Kills: " + localPlayer.Kills;
        }
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        float curHPPerc = (float)currentHP / (float)maxHP;
        HPBar.localScale = new Vector3(curHPPerc, 1, 1);
    }

    //respawn button
    public void RespawnBtn()
    {
        if (localPlayer != null)
            localPlayer.CmdRespawn();
    }

}
