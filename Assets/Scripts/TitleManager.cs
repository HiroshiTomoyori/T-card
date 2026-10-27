using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [Header("Startup / 起動画面")]
    [Tooltip("現在はタイトルを非表示にして、メインメニューから開始")]
    public bool skipTitleScreen = true;
    public GameObject titleScreen;
    public GameObject gameScreen;
    public GameObject difficultyScreen;
    public GameObject loadingScreen;

    public float loadingTime = 3f;

    public Image loadingBackground;

    public Sprite easyLoadingImage;
    public Sprite normalLoadingImage;
    public Sprite hardLoadingImage;

    [Header("Difficulty Images")]
    public Image difficultyBackgroundImage;
    public Sprite normalDifficultyImage;
    public Sprite secretUnlockedDifficultyImage;

    [Header("Audio")]
    public AudioSource startSE;
    public AudioSource bgmSource;
    public AudioSource difficultyBGM;
    public AudioSource difficultySE;
    public AudioSource moonSE;

    public AudioClip easyVoice;
    public AudioClip normalVoice;
    public AudioClip hardVoice;
    public AudioClip secretVoice;

    public float voiceFadeTime = 0.6f;

    [Header("Secret")]
    public GameObject secretButton;
    public Sprite secretLoadingImage;

    [Header("UI")]
    public GameObject playerResourceUI;

    public GameObject easyButton;
    public GameObject normalButton;
    public GameObject hardButton;

    [Header("BGM")]
    public AudioSource bgmAudioSource;
    public AudioClip normalBgm;
    public AudioClip secretBgm;

    int moonTapCount = 0;

    // Secretは最初から解放済み
    bool isSecretUnlocked = true;

    bool isTransitioning = false;

    public static bool isAdvancedRule = false;
    public static bool SkipActionConfirmations { get; private set; }
    Toggle skipConfirmationsToggle;
    Font titleOptionsFont;

    void CreateConfirmationOption()
    {
        if (titleScreen == null) return;
        var root = new GameObject("Skip Action Confirmations", typeof(RectTransform), typeof(Image), typeof(Toggle));
        root.transform.SetParent(skipTitleScreen&&difficultyScreen!=null?difficultyScreen.transform:titleScreen.transform, false);
        var rect = root.GetComponent<RectTransform>();
        // SafeArea is scaled up in this scene; its bottom edge is outside the viewport.
        // Place beside the visible title controls using their center-based coordinates.
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -340f);
        rect.sizeDelta = new Vector2(420f, 64f);
        root.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.04f, 0.75f);
        skipConfirmationsToggle = root.GetComponent<Toggle>();

        var box = new GameObject("Checkbox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(root.transform, false);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0f, 0.5f);
        boxRect.anchoredPosition = new Vector2(34f, 0f);
        boxRect.sizeDelta = new Vector2(34f, 34f);
        box.GetComponent<Image>().color = new Color(0.95f, 0.86f, 0.65f);
        var mark = new GameObject("Checked", typeof(RectTransform), typeof(Image));
        mark.transform.SetParent(box.transform, false);
        var markRect = mark.GetComponent<RectTransform>();
        markRect.anchorMin = Vector2.zero; markRect.anchorMax = Vector2.one;
        markRect.offsetMin = new Vector2(7f, 7f); markRect.offsetMax = new Vector2(-7f, -7f);
        mark.GetComponent<Image>().color = new Color(0.3f, 0.18f, 0.05f);
        skipConfirmationsToggle.targetGraphic = box.GetComponent<Image>();
        skipConfirmationsToggle.graphic = mark.GetComponent<Image>();
        skipConfirmationsToggle.isOn = SkipActionConfirmations;
        skipConfirmationsToggle.onValueChanged.AddListener(value=>SkipActionConfirmations=value);

        var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(root.transform, false);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(65f, 0f); labelRect.offsetMax = new Vector2(-12f, 0f);
        titleOptionsFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Noto Sans CJK JP", "Arial" }, 26);
        var text = label.GetComponent<Text>();
        text.font = titleOptionsFont; text.fontSize = 26;
        text.text = "選択確認を省略";
        text.color = new Color(0.95f, 0.86f, 0.65f);
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
    }

    void OnDestroy()
    {
        if (titleOptionsFont != null) Destroy(titleOptionsFont);
    }
    static bool rematchRequested;
    static GameFlowManager.Difficulty rematchDifficulty;
    static bool rematchAdvancedRule;
    static bool rematchTitleRule;

    public static void SetResultDestination(bool rematch, GameFlowManager.Difficulty difficulty)
    {
        rematchRequested = rematch;
        rematchDifficulty = difficulty;
        rematchAdvancedRule = GameSettings.IsAdvancedRule;
        rematchTitleRule = isAdvancedRule;
    }

    void Start()
    {
        Time.timeScale = 1f;
        HandDealer.IsRedrawSelecting = false;
        CreateConfirmationOption();
        if(titleScreen != null)
            titleScreen.SetActive(!skipTitleScreen);

        if(difficultyScreen != null)
            difficultyScreen.SetActive(skipTitleScreen&&!rematchRequested);
        if(skipTitleScreen)StartCoroutine(PositionMenuOption());

        if(loadingScreen != null)
            loadingScreen.SetActive(false);

        if(gameScreen != null)
            gameScreen.SetActive(false);

        if(playerResourceUI != null)
            playerResourceUI.SetActive(false);

        // Secretボタンを最初から表示
        if(secretButton != null)
            secretButton.SetActive(true);

        // 通常難易度ボタンも表示
        if(easyButton != null)
            easyButton.SetActive(true);

        if(normalButton != null)
            normalButton.SetActive(true);

        if(hardButton != null)
            hardButton.SetActive(true);

        // 通常難易度背景を表示
        if(
            difficultyBackgroundImage != null &&
            normalDifficultyImage != null
        )
        {
            difficultyBackgroundImage.sprite =
                normalDifficultyImage;
        }
        if(skipTitleScreen&&!rematchRequested)
        {
            if(bgmSource!=null)bgmSource.Stop();
            if(difficultyBGM!=null&&!difficultyBGM.isPlaying)difficultyBGM.Play();
        }
        if (rematchRequested)
        {
            rematchRequested = false;
            StartCoroutine(StartRematchFromBeginning());
        }
    }

    IEnumerator PositionMenuOption()
    {
        yield return null;
        if(skipConfirmationsToggle==null||difficultyScreen==null)yield break;
        var rect=(RectTransform)skipConfirmationsToggle.transform;var parent=rect.parent as RectTransform;
        var canvas=parent.GetComponentInParent<Canvas>();var camera=canvas!=null&&canvas.rootCanvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.rootCanvas.worldCamera:null;
        if(RectTransformUtility.ScreenPointToWorldPointInRectangle(parent,new Vector2(Screen.width*.5f,Screen.height*.065f),camera,out Vector3 position))rect.position=position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,Vector2.zero,camera,out Vector2 lower);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,new Vector2(Screen.width,Screen.height),camera,out Vector2 upper);
        rect.sizeDelta=new Vector2(Mathf.Abs(upper.x-lower.x)*.72f,Mathf.Abs(upper.y-lower.y)*.04f);
    }

    IEnumerator StartRematchFromBeginning()
    {
        // Let all new-scene Start methods finish before enabling the match root.
        yield return null;
        GameSettings.IsAdvancedRule = rematchAdvancedRule;
        isAdvancedRule = rematchTitleRule;
        foreach (GameFlowManager flow in FindObjectsByType<GameFlowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (flow.gameObject.scene == gameObject.scene) flow.selectedDifficulty = rematchDifficulty;
        if (bgmSource != null) bgmSource.Stop();
        if (difficultyBGM != null) difficultyBGM.Stop();
        if (titleScreen != null) titleScreen.SetActive(false);
        if (difficultyScreen != null) difficultyScreen.SetActive(false);
        if (loadingScreen != null) loadingScreen.SetActive(false);
        if (gameScreen != null) gameScreen.SetActive(true);
        if (playerResourceUI != null) playerResourceUI.SetActive(true);
        if (isAdvancedRule) ChangeToSecretBgm();
        isTransitioning = false;
    }

    public void StartGame()
    {
        if(isTransitioning)
            return;

        SkipActionConfirmations = skipConfirmationsToggle != null && skipConfirmationsToggle.isOn;
        isTransitioning = true;

        StartCoroutine(
            StartGameRoutine()
        );
    }

    IEnumerator StartGameRoutine()
    {
        if(bgmSource != null)
            bgmSource.Stop();

        if(startSE != null)
        {
            startSE.volume = 1f;
            startSE.Play();

            yield return new WaitForSeconds(0.4f);

            float t = 0f;

            while(t < voiceFadeTime)
            {
                t += Time.deltaTime;

                startSE.volume =
                    Mathf.Lerp(
                        1f,
                        0f,
                        t / voiceFadeTime
                    );

                yield return null;
            }

            startSE.Stop();
            startSE.volume = 1f;
        }

        if(titleScreen != null)
            titleScreen.SetActive(false);

        if(difficultyScreen != null)
            difficultyScreen.SetActive(true);

        if(difficultyBGM != null)
            difficultyBGM.Play();

        isTransitioning = false;
    }

    public void SelectDifficulty()
    {
        if(isTransitioning)
            return;

        isTransitioning = true;

        StartCoroutine(
            SelectDifficultyRoutine()
        );
    }

    IEnumerator SelectDifficultyRoutine()
    {
        if(difficultyBGM != null)
            difficultyBGM.Stop();

        if(difficultySE != null)
        {
            difficultySE.volume = 1f;
            difficultySE.Play();

            yield return new WaitForSeconds(0.8f);
        }

        if(difficultyScreen != null)
            difficultyScreen.SetActive(false);

        if(gameScreen != null)
            gameScreen.SetActive(true);

        Debug.Log("GameScreen ON");

        if(playerResourceUI != null)
        {
            playerResourceUI.SetActive(true);
            Debug.Log("PlayerResourceUI ON");
        }
        else
        {
            Debug.LogError(
                "playerResourceUI が未設定"
            );
        }

        isTransitioning = false;
    }

    public void SelectEasy()
    {
        if(isTransitioning)
            return;

        isAdvancedRule = false;

        StartCoroutine(
            SelectRoutine(
                easyVoice,
                easyLoadingImage
            )
        );
    }

    public void SelectNormal()
    {
        if(isTransitioning)
            return;

        isAdvancedRule = false;

        StartCoroutine(
            SelectRoutine(
                normalVoice,
                normalLoadingImage
            )
        );
    }

    public void SelectHard()
    {
        if(isTransitioning)
            return;

        isAdvancedRule = false;

        StartCoroutine(
            SelectRoutine(
                hardVoice,
                hardLoadingImage
            )
        );
    }

    public void SelectSecret()
    {
        if(isTransitioning)
            return;

        isAdvancedRule = true;

        StartCoroutine(
            SelectRoutine(
                secretVoice,
                secretLoadingImage
            )
        );
    }

    IEnumerator SelectRoutine(
        AudioClip clip,
        Sprite loadingImage
    )
    {
        isTransitioning = true;

        if(difficultyBGM != null)
            difficultyBGM.Stop();

        if(
            loadingBackground != null &&
            loadingImage != null
        )
        {
            loadingBackground.sprite =
                loadingImage;
        }

        if(difficultySE != null && clip != null)
        {
            difficultySE.Stop();
            difficultySE.clip = clip;
            difficultySE.volume = 1f;
            difficultySE.Play();

            float waitTime =
                Mathf.Max(
                    0f,
                    clip.length - 0.2f
                );

            yield return new WaitForSeconds(
                waitTime
            );
        }

        if(difficultyScreen != null)
            difficultyScreen.SetActive(false);

        if(loadingScreen != null)
            loadingScreen.SetActive(false);

        if(gameScreen != null)
            gameScreen.SetActive(true);

        if(isAdvancedRule)
        {
            ChangeToSecretBgm();
        }

        Debug.Log("GameScreen ON");

        if(playerResourceUI != null)
        {
            playerResourceUI.SetActive(true);
            Debug.Log("PlayerResourceUI ON");
        }
        else
        {
            Debug.LogError(
                "playerResourceUI が未設定"
            );
        }

        isTransitioning = false;
    }

    void ChangeToSecretBgm()
    {
        if(
            bgmAudioSource == null ||
            secretBgm == null
        )
        {
            return;
        }

        bgmAudioSource.Stop();
        bgmAudioSource.clip = secretBgm;
        bgmAudioSource.loop = true;
        bgmAudioSource.Play();
    }

    public void TapMoon()
    {
        // Secretは常時解放済みなので何もしない
        if(isSecretUnlocked)
            return;

        if(moonSE != null)
            moonSE.Play();

        moonTapCount++;

        Debug.Log(
            "Moon Tap : " +
            moonTapCount
        );

        if(moonTapCount < 5)
            return;

        isSecretUnlocked = true;

        StopAllCoroutines();

        Debug.Log("SECRET解放");

        if(easyButton != null)
            easyButton.SetActive(false);

        if(normalButton != null)
            normalButton.SetActive(false);

        if(hardButton != null)
            hardButton.SetActive(false);

        if(secretButton != null)
            secretButton.SetActive(true);

        if(
            difficultyBackgroundImage != null &&
            secretUnlockedDifficultyImage != null
        )
        {
            difficultyBackgroundImage.sprite =
                secretUnlockedDifficultyImage;
        }

        if(bgmSource != null)
            bgmSource.Stop();

        if(
            difficultyBGM != null &&
            !difficultyBGM.isPlaying
        )
        {
            difficultyBGM.Play();
        }

        if(titleScreen != null)
            titleScreen.SetActive(false);

        if(loadingScreen != null)
            loadingScreen.SetActive(false);

        if(gameScreen != null)
            gameScreen.SetActive(false);

        if(difficultyScreen != null)
            difficultyScreen.SetActive(true);

        isTransitioning = false;
    }
}
