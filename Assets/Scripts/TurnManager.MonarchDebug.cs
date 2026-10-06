using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TurnManager
{
    public enum MonarchWallTest { NoShields, OneShield, TwoShields, LastWall }

    [Header("Monarch Debug (Editor / Development Build)")]
    public MonarchWallTest monarchWallTest = MonarchWallTest.TwoShields;
    [Tooltip("次の敵バトルフェーズだけ、指定盤面でモナーク攻撃を強制します")]
    public bool debugNextEnemyMonarchAttack;
    [Tooltip("未設定なら cardList の DoubleWallBreak カードを使用")]
    public CardData debugMonarchCard;
    [Tooltip("未設定なら cardList のシールドカードを使用。2枚は別々に設定可能")]
    public CardData debugFirstShield;
    public CardData debugSecondShield;

    readonly List<CardData> monarchDebugData = new List<CardData>();

    [ContextMenu("Debug/Arm Enemy Monarch Attack")]
    public void ArmEnemyMonarchAttack()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!Application.isPlaying) { Debug.LogWarning("Playモードで実行してください"); return; }
        debugNextEnemyMonarchAttack = true;
        Debug.Log("次の敵バトルフェーズにモナークテストを予約：" + monarchWallTest);
#endif
    }

    CardController PrepareMonarchDebugAttack()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!debugNextEnemyMonarchAttack) return null;
        debugNextEnemyMonarchAttack = false;
        if (handDealer == null || cardPrefab == null || enemyBattleArea == null) return null;
        CardData monarch = debugMonarchCard ?? handDealer.cardList.Find(IsMonarchCard);
        CardData shield = debugFirstShield ?? handDealer.cardList.Find(data => HasMonarchTestEffect(data, EffectType.ShieldTrigger));
        CardData normal = handDealer.cardList.Find(data => data != null && !HasMonarchTestEffect(data, EffectType.ShieldTrigger));
        int count = monarchWallTest == MonarchWallTest.LastWall ? 1 : 2;
        int shieldCount = monarchWallTest == MonarchWallTest.NoShields ? 0 : monarchWallTest == MonarchWallTest.TwoShields ? 2 : 1;
        if (monarch == null || !IsMonarchCard(monarch) || normal == null || (shieldCount > 0 && shield == null))
        { Debug.LogError("モナークテスト用カードデータが不足しています"); return null; }
        var walls = new List<CardData>();
        for (int i = 0; i < count; i++)
        {
            bool isShield = i < shieldCount;
            CardData source = isShield ? (i == 1 ? debugSecondShield ?? handDealer.cardList.Find(data => data != shield && HasMonarchTestEffect(data, EffectType.ShieldTrigger)) ?? shield : shield) : normal;
            var clone = Instantiate(source);
            var effects = new List<EffectType>(clone.effectTypes ?? new EffectType[0]);
            effects.RemoveAll(effect => effect == EffectType.ShieldTrigger);
            if (isShield) effects.Add(EffectType.ShieldTrigger);
            clone.effectTypes = effects.ToArray();
            monarchDebugData.Add(clone);
            walls.Add(clone);
        }
        if (!handDealer.PrepareMonarchDebugWalls(walls)) return null;
        var obj = Instantiate(cardPrefab, enemyBattleArea);
        var attacker = obj.GetComponent<CardController>();
        if (attacker == null) { Destroy(obj); return null; }
        var rect = obj.GetComponent<RectTransform>();
        if (rect != null) { rect.localScale = Vector3.one * 0.7f; rect.sizeDelta = new Vector2(160f, 230f); rect.anchoredPosition = Vector2.zero; }
        attacker.SetData(monarch);
        attacker.SetSummonSickness(false);
        attacker.SetAttackable(false);
        if (obj.GetComponent<EnemyBattleCardTargetClick>() == null) obj.AddComponent<EnemyBattleCardTargetClick>();
        if (enemyBattleLayout != null) enemyBattleLayout.Refresh();
        Debug.Log("モナークテスト開始：" + monarchWallTest + " / ウォール " + count + "枚 / シールド " + shieldCount + "枚");
        return attacker;
#else
        return null;
#endif
    }

    static bool HasMonarchTestEffect(CardData data, EffectType effect)
    {
        return data != null && data.effectTypes != null && System.Array.Exists(data.effectTypes, value => value == effect);
    }

    void OnDestroy()
    {
        foreach (var data in monarchDebugData) if (data != null) Destroy(data);
    }
}
