using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Events;

public class HandController : MonoBehaviour
{
[Header("Card Controller Pro")]
public CCP.CardGroup proHandGroup;

private int lastProCardCount = -1;

public void RefreshProLayout()
{
    if (proHandGroup == null)
        return;

    lastProCardCount = proHandGroup.cards.Count;
    layoutSettingsChanged = false;

    if (isIdle)
    {
        proHandGroup.SetTCardLayout(
            idleMaxWidth,
            idleMaxSpacing,
            idleScale,
            0f,
            0f,
            isOpeningLook
                ? openingLayoutCenter
                : Vector2.zero
        );
    }
    else
    {
        GetExpandedLayout(proHandGroup.cards.Count, out float width, out float spacing,
            out float arcHeight, out float angleStep);
        proHandGroup.SetTCardLayout(width, spacing, expandScale, arcHeight, angleStep, Vector2.zero);
    }
}

    [Header("待機位置")]
    public Vector3 idlePosition = Vector3.zero;

    [Header("展開位置")]
    public Vector3 expandPosition =
        new Vector3(0f, 520f, 0f);

    [Header("収納サイズ")]
    public float idleScale = 0.6f;

    [Header("展開サイズ")]
    public float expandScale = 0.85f;

    [Header("収納レイアウト")]
    public float idleMaxWidth = 500f;
    public float idleMaxSpacing = 80f;
    public float idleMinSpacing = 20f;

    [Header("展開レイアウト")]
    public float expandMaxWidth = 760f;
    public float expandMaxSpacing = 140f;
    public float expandArcHeight = 12f;
    public float expandAngleStep = 8f;

    [Header("展開手札・枚数に応じた圧縮")]
    [Min(2), Tooltip("この枚数を超えると、カードの間隔と扇の開きを徐々に狭めます")]
    public int expandCompressionStartCount = 5;
    [Min(0f), Tooltip("1枚増えるごとの間隔の圧縮率。大きいほど密に並びます")]
    public float expandSpacingCompression = 0.12f;
    [Min(0f), Tooltip("1枚増えるごとの扇形の圧縮率。角度と弧の深さを抑えます")]
    public float expandFanCompression = 0.12f;
    [Min(0f), Tooltip("カード間隔の下限。表示幅を超える場合は幅の制限を優先します")]
    public float expandMinSpacing = 24f;
    [Range(0f, 45f), Tooltip("左右の端のカードが傾く角度の上限")]
    public float expandMaxFanAngle = 15f;
    [Min(0f), Tooltip("表示幅の左右に確保する、カードの端のための余白")]
    public float expandEdgePadding = 120f;

    bool layoutSettingsChanged;
    void OnValidate()
    {
        layoutSettingsChanged = true;
    }

    void GetExpandedLayout(int count, out float width, out float spacing,
        out float arcHeight, out float angleStep)
    {
        int startCount = Mathf.Max(2, expandCompressionStartCount);
        int extra = Mathf.Max(0, count - startCount);
        width = Mathf.Max(0f, expandMaxWidth - Mathf.Max(0f, expandEdgePadding) * 2f);
        spacing = Mathf.Max(Mathf.Max(0f, expandMinSpacing),
            Mathf.Max(0f, expandMaxSpacing) / (1f + extra * Mathf.Max(0f, expandSpacingCompression)));
        spacing = Mathf.Min(spacing, Mathf.Max(0f, expandMaxSpacing));
        if(count > 1) spacing = Mathf.Min(spacing, width / (count - 1));
        else spacing = 0f;
        float fanFactor = 1f / (1f + extra * Mathf.Max(0f, expandFanCompression));
        float countFactor = count > startCount ? (startCount - 1f) / (count - 1f) : 1f;
        arcHeight = expandArcHeight * fanFactor * countFactor;
        angleStep = expandAngleStep * fanFactor * countFactor;
        if(count > 1)
            angleStep = Mathf.Clamp(angleStep,
                -Mathf.Max(0f, expandMaxFanAngle) * 2f / (count - 1),
                Mathf.Max(0f, expandMaxFanAngle) * 2f / (count - 1));
    }
    [Header("初期手札確認レイアウト")]
    public float openingMaxWidth = 760f;
    public float openingMaxSpacing = 110f;

    [Tooltip("初期手札の並びの中心軸。Xで左右、Yで上下を調整します")]
    public Vector2 openingLayoutCenter = Vector2.zero;

    [Header("ダブルクリック")]
    public float doubleClickTime = 0.6f;

    bool isIdle = true;
    bool isOpeningLook = false;

    float lastHandClickTime = -1f;

    float normalIdleScale;
    Vector3 normalIdlePosition;
    float normalIdleMaxWidth;
    float normalIdleMaxSpacing;

    bool hasSavedNormalIdle = false;
[Header("手札選択中の操作制限")]
public HandDealer handDealer;

private CCP.CardGroup clickBoundGroup;

void BindProHandClicks()
{
    if (proHandGroup == null)
        return;

    clickBoundGroup = proHandGroup;
    clickBoundGroup.onClickAction = CCP.OnClickAction.Custom;

    if (clickBoundGroup.onClickCustom == null)
    {
        clickBoundGroup.onClickCustom =
            new UnityEvent<CCP.Card>();
    }

    clickBoundGroup.onClickCustom.AddListener(
        OnProHandCardClicked
    );
}

void OnProHandCardClicked(CCP.Card card)
{
    if (card == null ||
        card.parentCardGroup != proHandGroup)
    {
        return;
    }

    RegisterHandCardClick();
}

void OnDestroy()
{
    if (clickBoundGroup != null &&
        clickBoundGroup.onClickCustom != null)
    {
        clickBoundGroup.onClickCustom.RemoveListener(
            OnProHandCardClicked
        );
    }
}
    public bool IsIdle
    {
        get { return isIdle; }
    }

    public bool IsOpeningLook
    {
        get { return isOpeningLook; }
    }

    void Awake()
    {
        SaveNormalIdleSettings();
    }

void Start()
{
    if (handDealer == null)
        handDealer = FindFirstObjectByType<HandDealer>();

    BindProHandClicks();
    ChangeState(true);
}

    void LateUpdate()
    {
        if (proHandGroup != null)
        {
            if (layoutSettingsChanged || lastProCardCount != proHandGroup.cards.Count)
            {
                RefreshProLayout();
            }

            return;
        }

        if (isIdle)
        {
            ApplyIdleLayout();
        }
        else
        {
            ApplyExpandLayout();
        }
    }

    public void Toggle()
    {
        ChangeState(!isIdle);
    }

public void ChangeState(bool toIdle)
{
    List<CCP.Card> animatedCards = new List<CCP.Card>();
    List<Vector3> currentPositions = new List<Vector3>();
    List<Quaternion> currentRotations =
        new List<Quaternion>();

    if (proHandGroup != null)
    {
        foreach (CCP.Card card in proHandGroup.cards)
        {
            if (card == null ||
                card.transform.parent != proHandGroup.transform)
            {
                continue;
            }

            card.transform.DOKill();

            animatedCards.Add(card);
            currentPositions.Add(card.transform.position);
            currentRotations.Add(card.transform.rotation);
        }
    }

    isIdle = toIdle;

    transform.localEulerAngles = Vector3.zero;

    if (!isIdle)
        transform.SetAsLastSibling();

    transform.localPosition = isIdle
        ? idlePosition
        : expandPosition;

    // 親の位置変更によるカードの瞬間移動を打ち消す。
    for (int i = 0; i < animatedCards.Count; i++)
    {
        if (animatedCards[i] == null)
            continue;

        animatedCards[i].transform.SetPositionAndRotation(
            currentPositions[i],
            currentRotations[i]
        );
    }

    ResetDoubleClick();

    // 新しい配置へ位置・角度・サイズをアニメーション。
    RefreshProLayout();
}

    /// <summary>
    /// 手札内のカードがクリックされたときに呼ぶ。
    /// カードが違っても、同じ手札内なら
    /// ダブルクリックとして扱う。
    /// </summary>
public void RegisterHandCardClick()
{
    // 初期手札確認・手札上限選択中は切り替えない。
    if (isOpeningLook ||
        (handDealer != null &&
         handDealer.IsHandLimitSelecting))
    {
        ResetDoubleClick();
        return;
    }

    // ドラッグ中のクリックは数えない。
    if (CCP.Card.movingCard != null)
    {
        ResetDoubleClick();
        return;
    }

    float now = Time.unscaledTime;

    if (lastHandClickTime >= 0f &&
        now - lastHandClickTime <= doubleClickTime)
    {
        Toggle();
        return;
    }

    lastHandClickTime = now;
}

    public void ResetDoubleClick()
    {
        lastHandClickTime = -1f;
    }

    void SaveNormalIdleSettings()
    {
        if(hasSavedNormalIdle)
            return;

        normalIdleScale =
            idleScale;

        normalIdlePosition =
            idlePosition;

        normalIdleMaxWidth =
            idleMaxWidth;

        normalIdleMaxSpacing =
            idleMaxSpacing;

        hasSavedNormalIdle = true;
    }

    /// <summary>
    /// 初期手札確認中だけ、
    /// 収納状態のままサイズ・位置・間隔を変更する。
    /// </summary>
    public void ApplyOpeningIdleLook(
        float scale,
        float yOffset
    )
    {
        SaveNormalIdleSettings();

        isOpeningLook = true;
        isIdle = true;

        idleScale =
            scale;

        idlePosition =
            normalIdlePosition +
            new Vector3(
                0f,
                yOffset,
                0f
            );

        idleMaxWidth =
            openingMaxWidth;

        idleMaxSpacing =
            openingMaxSpacing;

        transform.localEulerAngles =
            Vector3.zero;

        transform.localPosition =
            idlePosition;

        // 初期手札を前面へ
        transform.SetAsLastSibling();

        ResetDoubleClick();
        RefreshProLayout();

        Debug.Log(
            "初期手札表示ON / Scale：" +
            idleScale +
            " / Position：" +
            idlePosition +
            " / LayoutCenter：" +
            openingLayoutCenter +
            " / MaxWidth：" +
            idleMaxWidth +
            " / MaxSpacing：" +
            idleMaxSpacing
        );
    }

    /// <summary>
    /// 初期手札確認終了後、
    /// 通常の収納状態へ戻す。
    /// </summary>
    public void RestoreNormalIdleLook()
    {
        if(!hasSavedNormalIdle)
            return;

        isOpeningLook = false;
        isIdle = true;

        idleScale =
            normalIdleScale;

        idlePosition =
            normalIdlePosition;

        idleMaxWidth =
            normalIdleMaxWidth;

        idleMaxSpacing =
            normalIdleMaxSpacing;

        transform.localEulerAngles =
            Vector3.zero;

        transform.localPosition =
            idlePosition;

        ResetDoubleClick();
        RefreshProLayout();

        Debug.Log("初期手札表示OFF");
    }

    void ApplyIdleLayout()
    {
        Vector2 layoutCenter =
            isOpeningLook
                ? openingLayoutCenter
                : Vector2.zero;

        ApplyLayout(
            idleMaxWidth,
            idleMaxSpacing,
            idleScale,
            0f,
            0f,
            layoutCenter
        );
    }

    void ApplyExpandLayout()
    {
        GetExpandedLayout(transform.childCount, out float width, out float spacing,
            out float arcHeight, out float angleStep);
        ApplyLayout(width, spacing, expandScale, arcHeight, angleStep, Vector2.zero);
    }

    void ApplyLayout(
        float maxWidth,
        float maxSpacing,
        float scale,
        float arcHeight,
        float angleStep,
        Vector2 layoutCenter
    )
    {
        int count =
            transform.childCount;

        if(count == 0)
            return;

        float spacing =
            maxSpacing;

        float totalWidth =
            spacing * (count - 1);

        if(count > 1 &&
           totalWidth > maxWidth)
        {
            spacing =
                maxWidth / (count - 1);

            totalWidth =
                maxWidth;
        }

        float startX =
            layoutCenter.x -
            totalWidth * 0.5f;

        float center =
            (count - 1) * 0.5f;

        for(int i = 0;
            i < count;
            i++)
        {
            Transform card =
                transform.GetChild(i);

            float offset =
                i - center;

            card.localPosition =
                new Vector3(
                    startX +
                    spacing * i,
                    layoutCenter.y -
                    Mathf.Abs(offset) *
                    arcHeight,
                    0f
                );

            card.localEulerAngles =
                new Vector3(
                    0f,
                    0f,
                    -offset *
                    angleStep
                );

            card.localScale =
                Vector3.one *
                scale;
        }
    }
}