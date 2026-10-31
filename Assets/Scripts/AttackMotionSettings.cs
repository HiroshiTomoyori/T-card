using UnityEngine;
[CreateAssetMenu(menuName="T-card/Attack Motion Settings")]
public sealed class AttackMotionSettings : ScriptableObject
{
    [Header("Timing / 秒・全体速度")]
    [Min(.05f)] public float speed=1;
    [Min(.01f)] public float liftDuration=.22f;
    [Min(0)] public float anticipationDuration=.33f;
    [Min(.01f)] public float lungeDuration=.09f;
    [Min(0)] public float impactPause=.01f;
    [Min(.01f)] public float returnDuration=.20f;
    [Min(0)] public float beforeLand=.03f;
    [Min(.01f)] public float landDuration=.20f;
    [Min(0)] public float finishDuration=.27f;
    [Header("Attacker / 720px幅基準のUI単位")]
    [Min(0)] public float liftDistance=22.4f;
    [Min(0)] public float lungeDistance=80;
    [Range(1,1.5f)] public float floatingScale=1.16f;
    [Range(1,1.3f), Tooltip("踏み込み中だけ追加する拡大倍率。1.07 = さらに7%拡大")]
    public float lungeZoom=1.07f;
    [Header("Battle target / バトル相手の浮上・応戦・反動")]
    [Range(0,2)] public float defenderMotion=1;
    [Min(0)] public float defenderLiftDelay=.04f;
    [Min(.01f)] public float defenderLiftDuration=.21f;
    [Min(0)] public float defenderLiftDistance=11.2f;
    [Min(0)] public float defenderLungeDistance=20.8f;
    [Range(1,1.5f)] public float defenderFloatingScale=1.08f;
    [Min(0)] public float defenderRecoilDistance=13.6f;
    [Min(.01f)] public float defenderRecoilDuration=.30f;
    [Min(0)] public float hitShake=3;
    [Min(0)] public float hitShakeFrequency=84;
    [Min(0)] public float hitShakeDecay=14;
    [Header("Glow / 実行中の発光")]
    public Color attackerColor=new Color(.322f,.878f,.82f,1);
    public Color targetColor=new Color(.8392f,.2196f,.3137f,1);
    [Min(0)] public float attackerGlow=.65f;
    [Min(0)] public float targetGlow=.6f;
    [Min(0)] public float wallGlow=.25f;
    [Header("Selected wall / 攻撃実行中のウォール単体")]
    [Tooltip("ウォールゾーンの色とは独立した攻撃対象の色。モナークでも選択カードはこの色で発光")]
    public Color wallTargetColor=new Color(.8392f,.2196f,.3137f,1);
    [Min(0)] public float wallTargetGlow=.9f;
    [Min(.1f)] public float glowBlur=15;
    [Min(.1f)] public float glowLineWidth=2;
    [Range(0,1)] public float fieldShadow=.22f;
    public Vector2 shadowOffset=new Vector2(8,-18);
    [Min(0)] public float shadowBlur=18;
    [Header("Impact / 着弾")]
    public Color hitColor=new Color(1,.902f,.682f,1);
    public Color monarchColor=new Color(.741f,.518f,1,1);
    [Min(0)] public float hitStrength=1;
    [Min(.05f)] public float hitDuration=.49f;
    [Min(.1f)] public float hitScale=1;
    public float slashAngle=-35;
    [Header("Lowlight / 実行中は関連部分以外を暗くする")]
    public Color lowlightColor=new Color(.031f,.047f,.078f,.42f);
    [Min(.01f)] public float fadeIn=.13f;
    [Min(.01f)] public float fadeOut=.25f;
    [Min(0), Tooltip("カードの周囲で暗さが滑らかに変わる幅")]
    public float cardLowlightFeather=6.4f;
    [Min(0), Tooltip("ウォールゾーン周りのローライト境界のぼかし幅。大きいほど柔らかい")]
    public float wallLowlightFeather=120;
    [Header("ウォールゾーンの丸み・グローぼかし / 720px幅基準")]
    [Min(0)] public float wallZoneCornerRadius=72;
    [Min(1)] public float wallZoneGlowBlur=64;
    [Min(0)] public float wallPadding=12;
    [Min(1)] public int sortingOffset=140;
    public float ImpactTime=>liftDuration+anticipationDuration+lungeDuration;
    public float LandStart=>ImpactTime+impactPause+returnDuration+beforeLand;
    public float Duration=>Mathf.Max(ImpactTime+hitDuration,LandStart+landDuration+finishDuration);
    static AttackMotionSettings cached;
    public static AttackMotionSettings Load(){if(cached==null)cached=Resources.Load<AttackMotionSettings>("TCardField/AttackMotionSettings");if(cached==null){cached=CreateInstance<AttackMotionSettings>();cached.hideFlags=HideFlags.HideAndDontSave;}return cached;}
}
