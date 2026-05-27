using UnityEngine;
using UnityEngine.UI;

public class ButtonGlow : MonoBehaviour
{
    Image img;

    void Awake()
    {
        img = GetComponent<Image>();
    }

    void Update()
    {
        Color c = img.color;

        c.a =
            0.4f +
            Mathf.Sin(
                Time.time * 3f
            ) * 0.2f;

        img.color = c;
    }
}