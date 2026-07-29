using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TCardDataExporter
{
    [System.Serializable]
    public class CardExportData
    {
        public string assetName;
        public string cardName;
        public string suit;
        public int power;
        public int cost;
        public List<string> effects = new List<string>();
    }

    [System.Serializable]
    public class CardExportList
    {
        public List<CardExportData> cards =
            new List<CardExportData>();
    }

    [MenuItem("T-card/Export Card Data")]
    public static void ExportCardData()
    {
        string[] guids =
            AssetDatabase.FindAssets("t:CardData");

        CardExportList exportList =
            new CardExportList();

        foreach(string guid in guids)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(guid);

            CardData card =
                AssetDatabase.LoadAssetAtPath<CardData>(
                    assetPath
                );

            if(card == null)
                continue;

            card.SetPowerFromName();
            card.SetCostFromName();

            CardExportData data =
                new CardExportData();

            data.assetName = card.name;
            data.cardName = card.cardName;
            data.suit = card.suit.ToString();
            data.power = card.power;
            data.cost = card.cost;

            if(card.effectTypes != null)
            {
                foreach(EffectType effect in card.effectTypes)
                {
                    data.effects.Add(
                        effect.ToString()
                    );
                }
            }

            exportList.cards.Add(data);
        }

        string json =
            JsonUtility.ToJson(
                exportList,
                true
            );

        string outputPath =
            Path.Combine(
                Application.dataPath,
                "../TCardCards.json"
            );

        File.WriteAllText(
            outputPath,
            json
        );

        Debug.Log(
            "カードデータ出力完了：" +
            outputPath +
            " / " +
            exportList.cards.Count +
            "枚"
        );

        EditorUtility.RevealInFinder(
            outputPath
        );
    }
}