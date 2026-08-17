using UnityEngine;

public class HandController : MonoBehaviour
{
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
        ChangeState(true);
    }

    void LateUpdate()
    {
        if(isIdle)
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
        isIdle = toIdle;

        transform.localEulerAngles =
            Vector3.zero;

        if(isIdle)
        {
            transform.localPosition =
                idlePosition;
        }
        else
        {
            transform.SetAsLastSibling();

            transform.localPosition =
                expandPosition;
        }

        ResetDoubleClick();
    }

    /// <summary>
    /// 手札内のカードがクリックされたときに呼ぶ。
    /// カードが違っても、同じ手札内なら
    /// ダブルクリックとして扱う。
    /// </summary>
    public void RegisterHandCardClick()
    {
        float now =
            Time.unscaledTime;

        if(lastHandClickTime >= 0f &&
           now - lastHandClickTime <=
           doubleClickTime)
        {
            lastHandClickTime = -1f;

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
        ApplyLayout(
            expandMaxWidth,
            expandMaxSpacing,
            expandScale,
            expandArcHeight,
            expandAngleStep,
            Vector2.zero
        );
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