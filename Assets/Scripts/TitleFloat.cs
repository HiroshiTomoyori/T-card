using UnityEngine;

public class TitleFloat : MonoBehaviour
{
    public float amplitude = 15f;
    public float speed = 2f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        transform.localPosition =
            startPos +
            new Vector3(
                0f,
                Mathf.Sin(Time.time * speed) * amplitude,
                0f
            );
    }
}