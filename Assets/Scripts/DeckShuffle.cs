using System.Collections;
using UnityEngine;

public class DeckShuffle : MonoBehaviour
{
    RectTransform rt;
    Vector2 startPos;

    public float moveAmount = 20f;
    public float speed = 0.04f;
    public int shuffleCount = 10;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
    }

    public void StartShuffle()
    {
        StartCoroutine(ShuffleAnimation());
    }

    IEnumerator ShuffleAnimation()
    {
        for (int i = 0; i < shuffleCount; i++)
        {
            rt.anchoredPosition =
                startPos + Vector2.left * moveAmount;

            yield return new WaitForSeconds(speed);

            rt.anchoredPosition =
                startPos + Vector2.right * moveAmount;

            yield return new WaitForSeconds(speed);
        }

        rt.anchoredPosition = startPos;

        HandDealer dealer =
            FindFirstObjectByType<HandDealer>();

        if (dealer != null)
        {
            dealer.DealStart();
        }
    }
}