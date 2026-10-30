using UnityEngine;
[CreateAssetMenu(menuName="T-card/Summon Effect Settings")]
public sealed class SummonEffectSettings:ScriptableObject
{
    [Header("演出全体 / 着地後の待ち時間")]
    [Tooltip("全パートの速度倍率。1が標準")]
    [Min(.05f)]public float speed=1.3f;
    [Min(0)]public float landingDelay=.5f;
    [Header("オレンジの波紋 / ハイライト")]
    [Min(.05f)]public float pulseDuration=.38f;
    public Color pulseColor=new Color(1,.48f,.12f,1);
    [Min(0)]public float pulseStrength=1;
    [Min(1)]public float pulseExpansion=1.65f;
    [Header("山札からの移動 / 反転 / 表示時間")]
    [Min(.01f)]public float drawTravel=.24f;
    [Min(.01f)]public float drawFlip=.22f;
    [Min(0)]public float drawHold=.18f;
    [Header("手札・リソースへの移動")]
    [Min(.01f)]public float handTravel=.2f;
    [Min(.01f)]public float chargeTravel=.3f;
    [Min(0)]public float chargeHold=.1f;
    [Header("表示サイズ・位置（720幅基準）")]
    public Vector2 playerRevealSize=new Vector2(120,184);
    public Vector2 enemyRevealSize=new Vector2(110,169);
    public Vector2 playerRevealOffset=new Vector2(0,150);
    public Vector2 enemyRevealOffset=new Vector2(0,-155);
    [Header("5の公開位置（Xはリソース中央、Yはデッキ基準）")]
    [Tooltip("デッキ中央から画面上方向への距離。720幅基準")]
    public float chargeRevealAboveDeck=110;
    static SummonEffectSettings cached;
    public static SummonEffectSettings Load(){if(cached==null)cached=Resources.Load<SummonEffectSettings>("TCardField/SummonEffectSettings");if(cached==null){cached=CreateInstance<SummonEffectSettings>();cached.hideFlags=HideFlags.HideAndDontSave;}return cached;}
}
