using UnityEngine;

public class WallAreaLayout : MonoBehaviour
{
    [SerializeField]
    float maxWidth = 500f;

    [SerializeField]
    float overlapLimit = 60f;

    void LateUpdate()
    {
        LayoutWalls();
    }

    public void LayoutWalls()
    {
        int count = transform.childCount;

        if (count == 0)
            return;

        float spacing;

        if (count == 1)
        {
            spacing = 0f;
        }
        else
        {
            spacing = maxWidth / (count - 1);

            if (spacing > overlapLimit)
                spacing = overlapLimit;
        }

        float totalWidth = spacing * (count - 1);
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Transform wall =
                transform.GetChild(i);

            wall.localPosition =
                new Vector3(
                    startX + spacing * i,
                    0f,
                    0f
                );

            wall.localRotation =
                Quaternion.identity;

            wall.localScale =
                Vector3.one;
        }
    }
}