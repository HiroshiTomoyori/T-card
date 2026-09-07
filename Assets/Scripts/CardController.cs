using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardController : MonoBehaviour
{
    [Header("Card Data")]
    public CardData data;

    [Header("UI")]
    public Image artworkImage;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI hpText;

    [Header("Attack Glow")]
    public GameObject attackGlow;

    [SerializeField]
    private CardGlow cardGlow;

    [Header("State")]
    public bool isTapped = false;
    public bool hasSummonSickness = false;

    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    private bool isAttackable = false;


    void Awake()
    {
        // CardGlowを自動取得
        if(cardGlow == null)
            cardGlow = GetComponentInChildren<CardGlow>(true);

        // 旧AttackGlowがある場合は最初にOFF
        if(attackGlow != null)
            attackGlow.SetActive(false);

        // CardGlowも最初はOFF
        if(cardGlow != null)
            cardGlow.gameObject.SetActive(false);
    }


    void Start()
    {
        // カード名からPowerを取得
        if(data != null)
        {
            string cardName = data.cardName;

            if(int.TryParse(cardName, out int power))
            {
                data.power = power;
            }
        }

        Refresh();
    }


    public void SetData(CardData newData)
    {
        data = newData;

        Refresh();
    }


    public void Refresh()
    {
        if(data == null)
            return;


        // =========================
        // Cost
        // =========================

        int cost;

        // Joker
        if(data.cardName == "Joker")
        {
            cost = 13;
        }
        // Ace
        else if(data.power == 1)
        {
            cost = 4;
        }
        else
        {
            cost = data.power;
        }

        if(costText != null)
            costText.text = cost.ToString();


        // =========================
        // Artwork
        // =========================

        if(artworkImage != null)
        {
            artworkImage.sprite = data.artwork;
        }


        // =========================
        // Attack / HP
        // =========================

        if(attackText != null)
            attackText.text = "";

        if(hpText != null)
            hpText.text = "";


        UpdateTapVisual();
        UpdateAttackGlow();
    }


    // ========================================
    // Tap
    // ========================================

    public void Tap()
    {
        isTapped = true;

        UpdateTapVisual();
        UpdateAttackGlow();
    }


    public void Untap()
    {
        isTapped = false;

        UpdateTapVisual();
        UpdateAttackGlow();
    }


    // ========================================
    // Attackable
    // ========================================

    public void SetAttackable(bool value)
    {
        isAttackable = value;

        UpdateAttackGlow();
    }


    public bool IsAttackable()
    {
        return isAttackable;
    }


    // ========================================
    // Attack Glow
    // ========================================

    void UpdateAttackGlow()
    {
        bool shouldGlow =
            isAttackable &&
            !isTapped &&
            !hasSummonSickness;


        // 旧AttackGlow
        if(attackGlow != null)
        {
            attackGlow.SetActive(shouldGlow);
        }


        // 新CardGlow
        if(cardGlow != null)
        {
            if(shouldGlow)
            {
                cardGlow.gameObject.SetActive(true);
                cardGlow.PlayGlow();
            }
            else
            {
                cardGlow.StopGlow();
                cardGlow.gameObject.SetActive(false);
            }
        }
    }


    // ========================================
    // Tap Visual
    // ========================================

    void UpdateTapVisual()
    {
        RectTransform rect =
            GetComponent<RectTransform>();

        if(rect == null)
            return;


        if(isTapped)
        {
            rect.localRotation =
                Quaternion.Euler(0f, 0f, -90f);
        }
        else
        {
            rect.localRotation =
                Quaternion.identity;
        }
    }


    // ========================================
    // Summon Sickness
    // ========================================

    public void SetSummonSickness(bool value)
    {
        hasSummonSickness = value;


        CanvasGroup canvasGroup =
            GetComponent<CanvasGroup>();


        if(canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }


        if(hasSummonSickness)
        {
            canvasGroup.alpha = 0.6f;
        }
        else
        {
            canvasGroup.alpha = 1f;
        }


        UpdateAttackGlow();
    }


    // ========================================
    // Double Click
    // ========================================

    public bool IsDoubleClick()
    {
        float currentTime = Time.time;

        bool isDouble =
            currentTime - lastClickTime <= DOUBLE_CLICK_TIME;

        lastClickTime = currentTime;

        return isDouble;
    }
}