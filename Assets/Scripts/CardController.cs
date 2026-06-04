using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardController : MonoBehaviour
{
    public CardData data;

    public Image artworkImage;

    public TextMeshProUGUI costText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI hpText;

    public GameObject attackGlow;

    public bool isTapped = false;
    public bool hasSummonSickness = false;

    float lastClickTime = -1f;
    const float DOUBLE_CLICK_TIME = 0.3f;



    bool isAttackable = false;

    void Start()
    {
        if (data != null)
            data.SetPowerFromName();

        Refresh();
    }

    public void SetData(CardData newData)
    {
        data = newData;

        if (data != null)
            data.SetPowerFromName();

        Refresh();
    }

    public void Refresh()
    {
        if (data == null) return;

        data.SetPowerFromName();
        if(data.cardName.Contains("Joker"))
        {
            data.cost = 13;
        }
        else if(data.power == 1)
        {
            data.cost = 4; // Aだけ例外
        }
        else
        {
            data.cost = data.power;
        }

        if (artworkImage != null)
            artworkImage.sprite = data.artwork;

        if (costText != null)
            costText.text = data.cost.ToString();

        // attack / hp 表示は廃止予定なので空にする
        if (attackText != null)
            attackText.text = "";

        if (hpText != null)
            hpText.text = "";

        UpdateTapVisual();
        UpdateAttackGlow();
    }

    public void Tap()
    {
        isTapped = true;
        UpdateTapVisual();
    }

    public void Untap()
    {
        isTapped = false;
        UpdateTapVisual();
    }

    public void SetAttackable(bool value)
    {
        isAttackable = value;
        UpdateAttackGlow();
    }

    public bool IsAttackable()
    {
        return isAttackable;
    }

    void UpdateTapVisual()
    {
        RectTransform rt = GetComponent<RectTransform>();

        if (rt == null) return;

        if (isTapped)
            rt.localRotation = Quaternion.Euler(0, 0, -90f);
        else
            rt.localRotation = Quaternion.identity;
    }

    void UpdateAttackGlow()
    {
        if (attackGlow != null)
            attackGlow.SetActive(isAttackable && !isTapped);
    }

    public void SetSummonSickness(bool value)
    {
        hasSummonSickness = value;

        CanvasGroup cg =
            GetComponent<CanvasGroup>();

        if(cg == null)
            cg =
                gameObject.AddComponent<CanvasGroup>();

        if(value)
        {
            // 召喚酔い中
            cg.alpha = 0.6f;
        }
        else
        {
            // 通常
            cg.alpha = 1f;
        }
    }
}