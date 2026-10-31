using UnityEngine;
[CreateAssetMenu(menuName="T-card/Attack Selection Settings")]
public sealed class AttackSelectionSettings : ScriptableObject
{
    [Header("対象の色")]
    public Color normalTargetColor=new Color(1,.259f,.341f,1);
    public Color monarchTargetColor=new Color(.714f,.427f,1,1);
    public Color attackerColor=new Color(.12549f,.83922f,.76863f,1);
    public Color selectedWallColor=new Color(.976f,.929f,1,1);
    public Color lowlightColor=new Color(.039f,.059f,.078f,.5f);
    public Color attackerHighlightColor=new Color(1,1,.92f,1);
    [Header("呼吸・発光")]
    [Range(0,3)] public float glowStrength=1.15f;
    [Min(.2f)] public float pulsePeriod=3.2f;
    [Range(0,1)] public float minimumPulse=.3f;
    [Min(1)] public float blur=15;
    [Min(.1f)] public float lineWidth=2.8f;
    [Range(0,.5f)] public float attackerHighlight=.15f;
    [Min(0)] public float fadeDuration=.16f;
    [Header("位置・大きさ / 720px幅基準")]
    public Vector2 glowOffset=Vector2.zero;
    public Vector2 glowSizeAdjustment=Vector2.zero;
    [Min(0)] public float wallZonePadding=12;
    public Vector2 confirmationOffset=new Vector2(0,80);
    public Vector2 confirmationSize=new Vector2(310,60);
    [HideInInspector] public int contrastRevision;
    [Header("ウォールゾーンの丸み・グローぼかし / 720px幅基準")]
    [Min(0)] public float wallZoneCornerRadius=24;
    [Min(1)] public float wallZoneGlowBlur=28;
    static AttackSelectionSettings cached;
    public static AttackSelectionSettings Load(){if(cached!=null)return cached;cached=Resources.Load<AttackSelectionSettings>("TCardField/AttackSelectionSettings");if(cached==null){cached=CreateInstance<AttackSelectionSettings>();cached.hideFlags=HideFlags.HideAndDontSave;}return cached;}
}
