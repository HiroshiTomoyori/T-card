using UnityEngine;

public class BattleAreaLayout : MonoBehaviour
{
    public float normalSpacing = 140f;
    public float overlapSpacing = 80f;
    public int overlapStartCount = 4;

    public void Refresh()
    {
        int count = transform.childCount;
        if (count == 0) return;

        float spacing = count >= overlapStartCount
            ? overlapSpacing
            : normalSpacing;

        float totalWidth = spacing * (count - 1);
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            RectTransform card = transform.GetChild(i) as RectTransform;
            if (card == null) continue;

            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);

            //card.localScale = Vector3.one;
            card.anchoredPosition = new Vector2(startX + spacing * i, 0f);
        }
    }
}