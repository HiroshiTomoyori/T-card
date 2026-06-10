using UnityEngine;

public class HandExpandManager : MonoBehaviour
{
    public static HandExpandManager I;

    public bool IsExpanded { get; private set; }

    [Header("Hand")]
    public Transform handArea;

    [Header("Expanded")]
    public Vector2 expandedCenter = new Vector2(0f, 760f);
public float expandedSpacing = 95f;
public float expandedScale = 0.85f;
public float expandedAngleStep = 6f;
public float expandedArcHeight = 12f;

    Vector2 originalHandAreaPosition;
    bool hasOriginalPosition = false;

    void Awake()
    {
        I = this;
    }

    public void ToggleHand()
    {
        if(IsExpanded)
            CollapseHand();
        else
            ExpandHand();
    }

    public void ExpandHand()
    {
        if(handArea == null)
            return;

        RectTransform handRt =
            handArea.GetComponent<RectTransform>();

        if(handRt != null && !hasOriginalPosition)
        {
            originalHandAreaPosition = handRt.anchoredPosition;
            hasOriginalPosition = true;
        }

        IsExpanded = true;
        RefreshLayout();

        Debug.Log("手札展開");
    }

public void CollapseHand()
{
    IsExpanded = false;

    if(handArea != null)
    {
        RectTransform handRt =
            handArea.GetComponent<RectTransform>();

        if(handRt != null && hasOriginalPosition)
        {
            handRt.anchoredPosition = originalHandAreaPosition;
        }

        for(int i = 0; i < handArea.childCount; i++)
        {
            RectTransform rt =
                handArea.GetChild(i)
                .GetComponent<RectTransform>();

            if(rt == null)
                continue;

            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }

    HandDealer dealer =
        FindFirstObjectByType<HandDealer>();

    if(dealer != null)
    {
        dealer.SortPlayerHand();
    }

    Debug.Log("手札収納");
}

    public void RefreshLayout()
    {
        if(!IsExpanded)
            return;

        LayoutExpanded();
    }

    void LayoutExpanded()
    {
        if(handArea == null)
            return;

        RectTransform handRt =
            handArea.GetComponent<RectTransform>();

        if(handRt != null)
        {
            handRt.anchoredPosition = expandedCenter;
        }

        int count = handArea.childCount;

        if(count == 0)
            return;

        float startX =
            -expandedSpacing * (count - 1) / 2f;

        float maxAngle =
            expandedAngleStep * (count - 1) / 2f;

        for(int i = 0; i < count; i++)
        {
            RectTransform rt =
                handArea.GetChild(i)
                .GetComponent<RectTransform>();

            if(rt == null)
                continue;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float t = 0f;

            if(count > 1)
            {
                t =
                    (i - (count - 1) / 2f)
                    /
                    ((count - 1) / 2f);
            }

            float x =
                startX + expandedSpacing * i;

            float y =
                -Mathf.Abs(t) * expandedArcHeight;

            rt.anchoredPosition =
                new Vector2(x, y);

            rt.localScale =
                Vector3.one * expandedScale;

            rt.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    -t * maxAngle
                );
        }

        handArea.SetAsLastSibling();
    }
}