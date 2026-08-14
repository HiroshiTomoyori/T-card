using UnityEngine;
using TMPro;
using System.Collections;

public class CoinTossManager : MonoBehaviour
{
    [Header("3D Coin")]
    [Tooltip("coin_high.fbx をシーンに配置し、そのTransformを指定します")]
    public Transform coinModel;
    public GameObject resultTextObject;
    public TextMeshProUGUI resultText;

    [Header("Coin Orientation")]
    [Tooltip("モデルの表が正面を向くローカル角度")]
    public Vector3 frontLocalEulerAngles = Vector3.zero;
    [Tooltip("モデルの裏が正面を向くローカル角度")]
    public Vector3 backLocalEulerAngles = new Vector3(180f, 0f, 0f);

    [Header("Audio Source")]
    public AudioSource seSource;
    public AudioSource gameBgmSource;

    [Header("Coin SE Clips")]
    public AudioClip appearSE;
    public AudioClip spinSE;
    public AudioClip stopSE;

    [Header("Spin")]
    [Min(0.01f)] public float minSpinTime = 0.8f;
    [Min(0.01f)] public float maxSpinTime = 1.5f;
    [Min(1)] public int flipRotations = 4;
    [Tooltip("FBXの表裏を返す回転軸。通常はX軸です")]
    public Vector3 flipAxis = Vector3.right;
    [Tooltip("回転中に加える横方向のひねり")]
    public float spinRotations = 1f;

    [Header("Flip Animation")]
    public float jumpHeight = 4f;
    [Min(1f)] public float zoomScale = 1.5f;
    [Range(0f, 1f)] public float slowMotionStrength = 0.35f;
    public float landingBounceHeight = 0.05f;
    public float landingBounceDuration = 0.08f;

    private bool isSpinning;
    private Vector3 startLocalPosition;
    private Vector3 startLocalScale;

    private void Start()
    {
        if (resultTextObject != null) resultTextObject.SetActive(false);
        StartCoinToss();
    }

    public void StartCoinToss()
    {
        if (isSpinning) return;

        if (coinModel == null)
        {
            Debug.LogError("Coin Model が未設定です。coin_high.fbx のTransformを指定してください。");
            return;
        }

        if (seSource == null)
        {
            Debug.LogError("SE Source が未設定です。");
            return;
        }

        StartCoroutine(TossRoutine());
    }

    private IEnumerator TossRoutine()
    {
        isSpinning = true;
        startLocalPosition = coinModel.localPosition;
        startLocalScale = coinModel.localScale;

        coinModel.gameObject.SetActive(true);
        coinModel.localPosition = startLocalPosition;
        coinModel.localScale = startLocalScale;
        coinModel.localRotation = Quaternion.Euler(frontLocalEulerAngles);

        if (resultTextObject != null) resultTextObject.SetActive(false);
        if (appearSE != null) seSource.PlayOneShot(appearSE);

        yield return new WaitForSeconds(0.25f);

        if (spinSE != null)
        {
            seSource.clip = spinSE;
            seSource.loop = true;
            seSource.Play();
        }

        // 結果を先に決め、最後は必ず対応する面の角度に合わせる。
        bool front = Random.value > 0.5f;
        float duration = Random.Range(minSpinTime, maxSpinTime);
        float timer = 0f;
        Vector3 normalizedFlipAxis = flipAxis.sqrMagnitude > 0f
            ? flipAxis.normalized
            : Vector3.right;
        Quaternion frontRotation = Quaternion.Euler(frontLocalEulerAngles);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            // 頂点付近で少しだけ回転を緩める。
            float warpedProgress = progress
                + Mathf.Sin(progress * Mathf.PI * 2f)
                / (Mathf.PI * 2f) * slowMotionStrength;

            float flipAngle = 360f * flipRotations * warpedProgress;
            float twistAngle = 360f * spinRotations
                * Mathf.Sin(warpedProgress * Mathf.PI);

            Quaternion flipRotation = Quaternion.AngleAxis(flipAngle, normalizedFlipAxis);
            Quaternion twistRotation = Quaternion.AngleAxis(twistAngle, Vector3.up);
            coinModel.localRotation = frontRotation * twistRotation * flipRotation;

            float arc = Mathf.Sin(progress * Mathf.PI);
            coinModel.localPosition = startLocalPosition + Vector3.up * (jumpHeight * arc);
            coinModel.localScale = Vector3.LerpUnclamped(
                startLocalScale,
                startLocalScale * zoomScale,
                arc);

            yield return null;
        }

        StopSpinSound();
        coinModel.localPosition = startLocalPosition;
        coinModel.localScale = startLocalScale;
        coinModel.localRotation = Quaternion.Euler(
            front ? frontLocalEulerAngles : backLocalEulerAngles);

        yield return LandingBounce();

        if (stopSE != null) seSource.PlayOneShot(stopSE);

        // 元の仕様を維持：裏なら先攻、表なら後攻。
        bool firstPlayer = !front;

        if (resultText != null) resultText.text = firstPlayer ? "First" : "Second";
        if (resultTextObject != null) resultTextObject.SetActive(true);

        Debug.Log("Coin front = " + front);
        Debug.Log("PlayerFirst = " + firstPlayer);

        yield return new WaitForSeconds(1.2f);

        coinModel.gameObject.SetActive(false);
        if (resultTextObject != null) resultTextObject.SetActive(false);
        if (gameBgmSource != null) gameBgmSource.Play();

        GameFlowManager flow = FindFirstObjectByType<GameFlowManager>();
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

    private IEnumerator LandingBounce()
    {
        if (landingBounceDuration <= 0f || landingBounceHeight <= 0f) yield break;

        float timer = 0f;
        while (timer < landingBounceDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / landingBounceDuration);
            coinModel.localPosition = startLocalPosition
                + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * landingBounceHeight);
            yield return null;
        }

        coinModel.localPosition = startLocalPosition;
    }

    private void StopSpinSound()
    {
        if (seSource == null) return;
        if (seSource.isPlaying) seSource.Stop();
        seSource.loop = false;
        seSource.clip = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        StopSpinSound();
        isSpinning = false;
    }
}
