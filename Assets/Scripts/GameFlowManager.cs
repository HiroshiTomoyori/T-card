using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameFlowManager : MonoBehaviour
{
    [Header("Coin Toss")]
    public GameObject coinTossPanel;

    [Header("Turn Result")]
    public GameObject turnResultPanel;
    public Text turnResultText;

    [Header("Deck")]
    public DeckShuffle deckShuffle;

    bool coinTossFinished = false;
    public bool PlayerFirst = true;

    void Start()
    {
        StartCoroutine(GameStartFlow());
    }
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public Difficulty selectedDifficulty = Difficulty.Normal;
    IEnumerator GameStartFlow()
    {
        // 1. コイントス開始
        if (coinTossPanel != null)
            coinTossPanel.SetActive(true);

        // 2. コイントス終了通知を待つ
        yield return new WaitUntil(() => coinTossFinished);

        if (coinTossPanel != null)
            coinTossPanel.SetActive(false);

        // 3. 先攻後攻表示
        if (turnResultPanel != null)
            turnResultPanel.SetActive(true);

        if (turnResultText != null)
            turnResultText.text = PlayerFirst ? "先攻" : "後攻";

        yield return new WaitForSeconds(1.0f);

        if (turnResultPanel != null)
            turnResultPanel.SetActive(false);

        // 4. 山札シャッフル
        if (deckShuffle != null)
        {
            deckShuffle.StartShuffle();
        }
        else
        {
            Debug.LogError("DeckShuffle が設定されていません");
        }
    }

    public void OnCoinTossFinished(bool playerFirst)
    {
        PlayerFirst = playerFirst;
        coinTossFinished = true;
    }

    public void SetEasy()
    {
        selectedDifficulty = Difficulty.Easy;
    }

    public void SetNormal()
    {
        selectedDifficulty = Difficulty.Normal;
    }

    public void SetHard()
    {
        selectedDifficulty = Difficulty.Hard;
    }
}