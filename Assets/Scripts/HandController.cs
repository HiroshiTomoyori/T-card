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

    [Header("ダブルクリック")]
    public float doubleClickTime = 0.6f;

    bool isIdle = true;

    float lastHandClickTime = -1f;

    public bool IsIdle
    {
        get { return isIdle; }
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
    /// カードが違っても、同じ手札内ならダブルクリックとして扱う。
    /// </summary>
    public void RegisterHandCardClick()
    {
        float now = Time.unscaledTime;

        if(lastHandClickTime >= 0f &&
           now - lastHandClickTime <= doubleClickTime)
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

    void ApplyIdleLayout()
    {
        ApplyLayout(
            idleMaxWidth,
            idleMaxSpacing,
            idleScale,
            0f,
            0f
        );
    }

    void ApplyExpandLayout()
    {
        ApplyLayout(
            expandMaxWidth,
            expandMaxSpacing,
            expandScale,
            expandArcHeight,
            expandAngleStep
        );
    }

    void ApplyLayout(
        float maxWidth,
        float maxSpacing,
        float scale,
        float arcHeight,
        float angleStep
    )
    {
        int count = transform.childCount;

        if(count == 0)
            return;

        float spacing = maxSpacing;

        float totalWidth =
            spacing * (count - 1);

        if(count > 1 &&
           totalWidth > maxWidth)
        {
            spacing =
                maxWidth / (count - 1);

            totalWidth = maxWidth;
        }

        float startX =
            -totalWidth * 0.5f;

        float center =
            (count - 1) * 0.5f;

        for(int i = 0; i < count; i++)
        {
            Transform card =
                transform.GetChild(i);

            float offset =
                i - center;

            card.localPosition =
                new Vector3(
                    startX + spacing * i,
                    -Mathf.Abs(offset) *
                    arcHeight,
                    0f
                );

            card.localEulerAngles =
                new Vector3(
                    0f,
                    0f,
                    -offset * angleStep
                );

            card.localScale =
                Vector3.one * scale;
        }
    }
}