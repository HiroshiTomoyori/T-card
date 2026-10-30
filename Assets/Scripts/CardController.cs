using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardController : MonoBehaviour
{
    [Header("Card Data")]
    public CardData data;

    [Header("UI")]
    public Image artworkImage;

    public RectTransform tapVisual;
    [Header("Rest lowlight")]
    [SerializeField, Range(0f, 1f)] float restDarkness = 0.1f;
    [SerializeField, HideInInspector] Image restArtwork;
    [SerializeField, HideInInspector] Color artworkColorBeforeRest = Color.white;
    [SerializeField, HideInInspector] bool artworkIsDimmed;
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
    [SerializeField,HideInInspector] bool playedFromReverse;
    public bool PlayedFromReverse=>playedFromReverse;
    public static bool CanReceiveSummonSickness(CardData card)
    {
        if(card==null)return false;
        return card.effectTypes==null||!System.Array.Exists(card.effectTypes,e=>e==EffectType.CannotAttack||e==EffectType.NoSummonSickness||e==EffectType.DestroyOneEnemyBattle||e==EffectType.TapAllEnemyBattle||e==EffectType.InstantDestroySelf||e==EffectType.JokerClearBattleArea);
    }
    public void BeginNormalPlay(){playedFromReverse=false;GetComponent<CardReadinessVisual>()?.ResetPreparationTransition();}
    public void MarkPlayedFromReverse(){playedFromReverse=true;SetSummonSickness(false);}
    public void RefreshStateVisuals(){UpdateTapVisual();}

    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    private bool isAttackable = false;


    void Awake()
    {
        if(GetComponent<CardBoundaryShadow>()==null)gameObject.AddComponent<CardBoundaryShadow>();
        RemoveLegacyRestOverlay();
        if(GetComponent<CardReadinessVisual>()==null)gameObject.AddComponent<CardReadinessVisual>();
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
        RectTransform target = tapVisual != null
            ? tapVisual : GetComponent<RectTransform>();
        if (target != null) target.localRotation = Quaternion.identity;
        // Tint the sprite itself so its alpha, rounded edges and position stay exact.
        if (artworkIsDimmed && (restArtwork != artworkImage || !isTapped))
        {
            if (restArtwork != null) restArtwork.color = artworkColorBeforeRest;
            artworkIsDimmed = false;
        }
        if (isTapped && artworkImage != null)
        {
            if (!artworkIsDimmed)
            {
                restArtwork = artworkImage;
                artworkColorBeforeRest = artworkImage.color;
                artworkIsDimmed = true;
            }
            float brightness = 1f - Mathf.Clamp01(CardReadinessSettings.Load().restDarkness);
            Color original = artworkColorBeforeRest;
            artworkImage.color = new Color(original.r * brightness, original.g * brightness,
                original.b * brightness, original.a);
        }
    }

    void RemoveLegacyRestOverlay()
    {
        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject.name != "RestLowlight") continue;
            image.gameObject.SetActive(false);
            Destroy(image.gameObject);
        }
    }
    // ========================================
    // Summon Sickness
    // ========================================

    public void SetSummonSickness(bool value)
    {
        bool wasPreparing=hasSummonSickness;
        hasSummonSickness=value&&!playedFromReverse&&CanReceiveSummonSickness(data);
        if(hasSummonSickness&&!wasPreparing)GetComponent<CardReadinessVisual>()?.ResetPreparationTransition();
        if(!hasSummonSickness)GetComponent<CardReadinessVisual>()?.ClearPreparationTransition();
        var layout=transform.parent!=null?transform.parent.GetComponent<BattleAreaLayout>():null;
        if(layout!=null)layout.MarkDirty();
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
