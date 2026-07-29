using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TCardAIRankWeight
{
    public string rank;
    public float value;
    public float chargePenalty;
}

[Serializable]
public class TCardAIConfig
{
    public string configVersion = "1.0";
    public int lookaheadDepth = 2;
    public int maxSummonsPerTurn = 2;

    public string attackOrderMode = "Adaptive";

    public float jokerClearBias;
    public float reverseBias;
    public float multiSummonBonus;
    public float kStackBonus;
    public float aggressionBonus;
    public float lowWallAttackBonus;

    public List<TCardAIRankWeight> rankWeights =
        new List<TCardAIRankWeight>();

    public TCardAIRankWeight GetRankWeight(string rank)
    {
        if(rankWeights == null)
            return null;

        return rankWeights.Find(x => x != null && x.rank == rank);
    }
}

public class TCardAIConfigLoader : MonoBehaviour
{
    public static TCardAIConfigLoader I { get; private set; }

    [Header("Resources/AI filename without extension")]
    [SerializeField]
    string resourcePath = "AI/TCardAIConfig";

    public TCardAIConfig Config { get; private set; }

    void Awake()
    {
        if(I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        Load();
    }

    public void Load()
    {
        TextAsset json = Resources.Load<TextAsset>(resourcePath);

        if(json == null)
        {
            Debug.LogError(
                "TCardAIConfigLoader: 設定が見つかりません Resources/" +
                resourcePath + ".json"
            );

            Config = CreateFallback();
            return;
        }

        Config = JsonUtility.FromJson<TCardAIConfig>(json.text);

        if(Config == null)
        {
            Debug.LogError("TCardAIConfigLoader: JSON読込失敗");
            Config = CreateFallback();
            return;
        }

        Debug.Log(
            "T-card AI設定読込：" +
            Config.configVersion +
            " / RankWeights=" +
            (Config.rankWeights != null ? Config.rankWeights.Count : 0)
        );
    }

    TCardAIConfig CreateFallback()
    {
        return new TCardAIConfig();
    }
}
