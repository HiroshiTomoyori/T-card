using UnityEngine;
using System.Reflection;

public class HandController : MonoBehaviour
{
    [Header("待機(Idle)位置")]
    public Vector3 idlePosition = Vector3.zero;

    [Header("展開(Expand)位置")]
    public Vector3 expandPosition = new Vector3(0f, 250f, 0f);

    [Header("展開(Expand)の表示限界")]
    public float expandMaxWidth = 800f;
    public float expandMaxSpacing = 100f;

    [Header("収納(Idle)サイズ")]
    public float idleScale = 0.6f;

    [Header("収納(Idle)の表示限界")]
    public float idleMaxWidth = 500f;
    public float idleMaxSpacing = 80f;
    public float idleMinSpacing = 20f;

    [Header("展開サイズ")]
    public float expandScale = 1.3f;

    private Behaviour fanLayoutGroup;
    private bool isIdle = true;

    void Awake()
    {
        fanLayoutGroup =
            (GetComponent("FanlayoutGroup")
            ?? GetComponent("FanLayoutGroup"))
            as Behaviour;

        ChangeState(true);
    }

    void LateUpdate()
    {
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
        isIdle = toIdle;

        if (isIdle)
        {
            if (fanLayoutGroup != null)
                fanLayoutGroup.enabled = false;

            transform.localPosition = idlePosition;
            transform.localEulerAngles = Vector3.zero;

            ApplyIdleLayout();
        }
        else
        {
            transform.SetAsLastSibling();
            transform.localPosition = expandPosition;
            transform.localEulerAngles = Vector3.zero;

            //ResetChildrenForExpand();

            if (fanLayoutGroup != null)
                fanLayoutGroup.enabled = true;

            ApplyExpandLayout();
        }
    }

    private void ApplyIdleLayout()
    {
        int count = transform.childCount;

        if (count == 0)
            return;

        float t = 0f;

        if (count >= 2)
        {
            t = (float)(count - 2) / 8f;
            t = Mathf.Clamp01(t);
        }

        float spacing =
            Mathf.Lerp(
                idleMaxSpacing,
                idleMinSpacing,
                t
            );

        float totalWidth = spacing * (count - 1);

        if (count > 1 && totalWidth > idleMaxWidth)
        {
            spacing = idleMaxWidth / (count - 1);
            totalWidth = idleMaxWidth;
        }

        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Transform card =
                transform.GetChild(i);

            card.localPosition =
                new Vector3(
                    startX + spacing * i,
                    0f,
                    0f
                );

            card.localRotation =
                Quaternion.identity;

            card.localScale =
                new Vector3(
                    idleScale,
                    idleScale,
                    1f
                );
        }
    }

private void ApplyExpandLayout()
{
    int count = transform.childCount;

    if (count == 0)
        return;

    float spacing = expandMaxSpacing;
    float totalWidth = spacing * (count - 1);

    if (count > 1 && totalWidth > expandMaxWidth)
    {
        spacing = expandMaxWidth / (count - 1);
        totalWidth = expandMaxWidth;
    }

    float startX = -totalWidth * 0.5f;
    float center = (count - 1) * 0.5f;

    for (int i = 0; i < count; i++)
    {
        Transform card = transform.GetChild(i);

        float offset = i - center;

        card.localPosition = new Vector3(
            startX + spacing * i,
            -Mathf.Abs(offset) * 12f,
            0f
        );

        card.localEulerAngles = new Vector3(
            0f,
            0f,
            -offset * 8f
        );

        //card.localScale = Vector3.one;
        card.localScale =
    Vector3.one * expandScale;
    }
}
}