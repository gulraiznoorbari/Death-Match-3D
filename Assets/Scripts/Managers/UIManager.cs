using UnityEngine;
using TMPro;
using Mirror;

public class UIManager : NetworkBehaviour
{
    public static UIManager instance;

    [Header("HUD")]
    [SerializeField] private Transform HPBar;
    public TextMeshProUGUI AmmoCountText;

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

    [Server]
    public void UpdateHP(int currentHP, int maxHP)
    {
        float curHPPercentage = (float)currentHP / (float)maxHP;
        HPBar.localScale = new Vector3(curHPPercentage, 1, 1);
    }
}
