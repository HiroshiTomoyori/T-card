using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
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
    int moonTapCount = 0;

    bool isSecretUnlocked = false;

    bool isTransitioning = false;

    public GameObject easyButton;
    public GameObject normalButton;
    public GameObject hardButton;

    public static bool isAdvancedRule = false;

    [Header("BGM")]
    public AudioSource bgmAudioSource;
    public AudioClip normalBgm;
    public AudioClip secretBgm;

    void Start()
    {
        titleScreen.SetActive(true);
        difficultyScreen.SetActive(false);
        loadingScreen.SetActive(false);
        gameScreen.SetActive(false);

        if(playerResourceUI != null)
        playerResourceUI.SetActive(false);

        if(secretButton != null)
            secretButton.SetActive(false);

        // 通常難易度画面
        if(difficultyBackgroundImage &&
        normalDifficultyImage)
        {
            difficultyBackgroundImage.sprite =
                normalDifficultyImage;
        }
    }

    public void StartGame()
    {
        if(isTransitioning)
            return;

        isTransitioning = true;

        StartCoroutine(
            StartGameRoutine()
        );
    }
    IEnumerator StartGameRoutine()
    {
        Button startButton =
            GetComponent<Button>();

        if (bgmSource != null)
            bgmSource.Stop();

        if (startSE != null)
        {
            startSE.volume = 1f;
            startSE.Play();

            yield return new WaitForSeconds(0.4f);

            float t = 0f;

            while (t < voiceFadeTime)
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

        titleScreen.SetActive(false);
        difficultyScreen.SetActive(true);

        if (difficultyBGM != null)
            difficultyBGM.Play();
    }

    public void SelectDifficulty()
    {
        StartCoroutine(
            SelectDifficultyRoutine()
        );
    }

    IEnumerator SelectDifficultyRoutine()
    {
        if (difficultyBGM != null)
            difficultyBGM.Stop();

        if (difficultySE != null)
        {
            difficultySE.volume = 1f;
            difficultySE.Play();

            yield return new WaitForSeconds(
                0.8f
            );
        }

        difficultyScreen.SetActive(false);
        gameScreen.SetActive(true);
        Debug.Log("GameScreen ON");
        if (playerResourceUI != null)
        {
            playerResourceUI.SetActive(true);
            Debug.Log("PlayerResourceUI ON");
        }
        else
        {
            Debug.LogError("playerResourceUI が未設定");
        }
    }

    public void SelectEasy()
    {
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
        if (difficultyBGM != null)
            difficultyBGM.Stop();

        if (
            difficultySE != null &&
            clip != null
        )
        {
            difficultySE.clip = clip;
            difficultySE.volume = 1f;
            difficultySE.Play();

            difficultySE.clip = clip;
            difficultySE.volume = 1f;
            difficultySE.Play();

            yield return new WaitForSeconds(
                clip.length - 0.2f
            );
        }

        difficultyScreen.SetActive(false);
        loadingScreen.SetActive(false);
        gameScreen.SetActive(true);

        if(isAdvancedRule)
        {
            ChangeToSecretBgm();
        }
        Debug.Log("GameScreen ON");
        if (playerResourceUI != null)
        {
            playerResourceUI.SetActive(true);
            Debug.Log("PlayerResourceUI ON");
        }
        else
        {
            Debug.LogError("playerResourceUI が未設定");
        }
    }
    void ChangeToSecretBgm()
    {
        if(bgmAudioSource == null || secretBgm == null)
            return;

        bgmAudioSource.Stop();
        bgmAudioSource.clip = secretBgm;
        bgmAudioSource.loop = true;
        bgmAudioSource.Play();
    }
public void TapMoon()
{
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

    // Easy / Normal / Hard を完全無効
    if(easyButton != null)
        easyButton.SetActive(false);

    if(normalButton != null)
        normalButton.SetActive(false);

    if(hardButton != null)
        hardButton.SetActive(false);

    // SECRET表示
    if(secretButton != null)
        secretButton.SetActive(true);

    // 背景差し替え
    if(
        difficultyBackgroundImage != null &&
        secretUnlockedDifficultyImage != null
    )
    {
        difficultyBackgroundImage.sprite =
            secretUnlockedDifficultyImage;
    }

    // 必要ならBGM変更
    if(bgmSource != null)
        bgmSource.Stop();

    if(
        difficultyBGM != null &&
        !difficultyBGM.isPlaying
    )
    {
        difficultyBGM.Play();
    }

    // 難易度画面で停止
    titleScreen.SetActive(false);

    loadingScreen.SetActive(false);

    gameScreen.SetActive(false);

    difficultyScreen.SetActive(true);
}
}