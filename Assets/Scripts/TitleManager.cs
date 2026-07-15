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

    void Start()
    {
        if(titleScreen != null)
            titleScreen.SetActive(true);

        if(difficultyScreen != null)
            difficultyScreen.SetActive(false);

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