using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CoinTossManager : MonoBehaviour
{
    [Header("Coin")]
    public Image coinImage;
    public GameObject resultTextObject;
    public TextMeshProUGUI resultText;

    [Header("Sprites")]
    public Sprite kingdomCoin; // 表
    public Sprite fairyCoin;   // 裏

    [Header("Audio Source")]
    public AudioSource seSource;
    public AudioSource gameBgmSource;

    [Header("Coin SE Clips")]
    public AudioClip appearSE;
    public AudioClip spinSE;
    public AudioClip stopSE;

    [Header("Spin")]
    public float minSpinTime = 0.8f;
    public float maxSpinTime = 1.5f;

    bool isSpinning = false;

    void Start()
    {
        if (resultTextObject != null)
        {
            resultTextObject.SetActive(false);
        }

        StartCoinToss();
    }

    public void StartCoinToss()
    {
        if (isSpinning) return;

        if (coinImage == null)
        {
            Debug.LogError("Coin Image が未設定です。");
            return;
        }

        if (seSource == null)
        {
            Debug.LogError("Se Source が未設定です。");
            return;
        }

        StartCoroutine(TossRoutine());
    }

    IEnumerator TossRoutine()
    {
        isSpinning = true;

        if (resultTextObject != null)
        {
            resultTextObject.SetActive(false);
        }

        if (appearSE != null)
        {
            seSource.PlayOneShot(appearSE);
        }

        yield return new WaitForSeconds(0.25f);

        if (spinSE != null)
        {
            seSource.clip = spinSE;
            seSource.loop = true;
            seSource.Play();
        }

        float duration = Random.Range(minSpinTime, maxSpinTime);

        /*if (Random.value < 0.2f)
        {
            duration += 2f;
        }*/

        float timer = 0f;
        float yRotation = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float speed = Mathf.Lerp(
                25f,
                2f,
                timer / duration
            );

            yRotation += speed * 360f * Time.deltaTime;

            coinImage.rectTransform.localEulerAngles =
                new Vector3(0f, yRotation, 0f);

            yield return null;
        }

        if (seSource.isPlaying)
        {
            seSource.Stop();
        }

        seSource.loop = false;
        seSource.clip = null;

        bool front = Random.value > 0.5f;

        coinImage.sprite =
            front ? kingdomCoin : fairyCoin;

        coinImage.rectTransform.localEulerAngles =
            new Vector3(
                0f,
                front ? 0f : 180f,
                0f
            );

        if (stopSE != null)
        {
            seSource.PlayOneShot(stopSE);
        }

        // 表なら先攻、裏なら後攻
        bool firstPlayer = !front;

        if (resultText != null)
        {
            resultText.text =
                firstPlayer ? "First" : "Second";
        }

        if (resultTextObject != null)
        {
            resultTextObject.SetActive(true);
        }

        Debug.Log("Coin front = " + front);
        Debug.Log("PlayerFirst = " + firstPlayer);

        yield return new WaitForSeconds(1.2f);

        if (coinImage != null)
        {
            coinImage.gameObject.SetActive(false);
        }

        if (resultTextObject != null)
        {
            resultTextObject.SetActive(false);
        }

        if (gameBgmSource != null)
        {
            gameBgmSource.Play();
        }

        GameFlowManager flow =
            FindFirstObjectByType<GameFlowManager>();

        if (flow != null)
        {
            flow.OnCoinTossFinished(firstPlayer);
        }
        else
        {
            Debug.LogError("GameFlowManager が見つかりません");
        }

        isSpinning = false;
    }
}