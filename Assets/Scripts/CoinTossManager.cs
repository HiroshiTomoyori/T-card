using System.Collections;
using TMPro;
using UnityEngine;

public class CoinTossManager : MonoBehaviour
{
    [Header("2D Coin / ここから設定アセットを編集")]
    public CoinTossSettings settings;
    [Tooltip("従来のCoinImage。既存シーンの参照を維持し、再生時は新しい2D描画へ置き換えます。")]
    public Transform coinModel;
    public GameObject resultTextObject;
    public TextMeshProUGUI resultText;
    [Header("Audio / 既存の効果音")]
    public AudioSource seSource;
    public AudioSource gameBgmSource;
    public AudioClip appearSE, spinSE, stopSE;

    private CoinTossGraphic graphic;
    private Coroutine routine;
    private AudioSource spinSource;
    private bool finished;
    private bool started;
    public bool IsSpinning => routine != null;

    private void Start()
    {
        started = true;
        if (resultTextObject != null) resultTextObject.SetActive(false);
        StartCoinToss();
    }

    private void OnEnable()
    {
        if (started && !finished && routine == null) StartCoinToss();
    }

    public void StartCoinToss()
    {
        if (routine != null || finished) return;
        if (settings == null) settings = Resources.Load<CoinTossSettings>("TCardCoin/CoinTossSettings");
        if (settings == null || settings.coinShader == null || settings.butterflyTexture == null || settings.moonTexture == null)
        {
            Debug.LogError("Coin Toss Settings / shader / textures が未設定です。", this);
            return;
        }
        EnsureGraphic();
        routine = StartCoroutine(TossRoutine());
    }

    private void EnsureGraphic()
    {
        if (graphic != null) return;
        Transform parent = coinModel != null ? coinModel.parent : transform.parent;
        var go = new GameObject("Coin Toss 2D", typeof(RectTransform), typeof(CanvasRenderer), typeof(CoinTossGraphic));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        // Keep result labels and the remaining interface above the animation.
        if (coinModel != null)
        {
            rt.SetSiblingIndex(coinModel.GetSiblingIndex());
            coinModel.gameObject.SetActive(false);
        }
        graphic = go.GetComponent<CoinTossGraphic>();
        // Separate loop source: stopping the loop never cuts another shared SE.
        spinSource = go.AddComponent<AudioSource>();
        spinSource.playOnAwake = false; spinSource.loop = true;
        if (seSource != null)
        {
            spinSource.outputAudioMixerGroup = seSource.outputAudioMixerGroup;
            spinSource.volume = seSource.volume;
            spinSource.pitch = seSource.pitch;
        }
    }

    private IEnumerator TossRoutine()
    {
        bool front = Random.value > 0.5f;
        bool firstPlayer = !front; // Existing rule: butterfly = second, moon = first.
        finished = false;
        if (resultTextObject != null) resultTextObject.SetActive(false);
        graphic.gameObject.SetActive(true);
        if (appearSE != null && seSource != null) seSource.PlayOneShot(appearSE);
        if (spinSE != null) { spinSource.clip = spinSE; spinSource.Play(); }
        float progress = 0f;
        bool landed = false;
        while (progress < 1f)
        {
            graphic.ShowFrame(settings, CoinTossMotion.Evaluate(settings, progress, front));
            if (!landed && progress >= settings.flightPortion)
            {
                landed = true;
                if (stopSE != null && seSource != null) seSource.PlayOneShot(stopSE);
            }
            if (progress >= settings.spinPortion) StopSpinSound();
            yield return null;
            float dt = settings.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            progress = Mathf.Min(1f, progress + dt * Mathf.Max(0.05f, settings.playbackSpeed) / Mathf.Max(0.1f, settings.duration));
        }
        graphic.ShowFrame(settings, CoinTossMotion.Evaluate(settings, 1f, front));
        StopSpinSound();
        if (!landed && stopSE != null && seSource != null) seSource.PlayOneShot(stopSE);
        if (resultText != null) resultText.text = firstPlayer ? "First" : "Second";
        if (resultTextObject != null) resultTextObject.SetActive(true);
        float held = 0f;
        while (held < Mathf.Max(0f, settings.resultHoldSeconds))
        {
            // Continue applying live Inspector edits while holding the result.
            graphic.ShowFrame(settings, CoinTossMotion.Evaluate(settings, 1f, front));
            yield return null;
            held += settings.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }
        graphic.gameObject.SetActive(false);
        if (resultTextObject != null) resultTextObject.SetActive(false);
        routine = null;
        finished = true;
        if (gameBgmSource != null) gameBgmSource.Play();
        var flow = FindFirstObjectByType<GameFlowManager>();
        if (flow != null) flow.OnCoinTossFinished(firstPlayer);
        else Debug.LogError("GameFlowManager が見つかりません。", this);
    }

    private void StopSpinSound()
    {
        if (spinSource != null && spinSource.isPlaying) spinSource.Stop();
    }

    private void OnDisable()
    {
        StopAllCoroutines(); routine = null; StopSpinSound();
        if (graphic != null) graphic.gameObject.SetActive(false);
        if (resultTextObject != null) resultTextObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (graphic != null) Destroy(graphic.gameObject);
    }
}
