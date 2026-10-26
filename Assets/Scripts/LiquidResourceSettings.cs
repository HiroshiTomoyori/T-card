using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName="T-card/Liquid Resource UI", fileName="LiquidResourceSettings")]
public sealed class LiquidResourceSettings : ScriptableObject
{
    [Header("Layout / 大きさ・位置")]
    public Vector2 panelSize = new Vector2(240,154);
    public Vector2 offset = Vector2.zero;
    [Min(0)] public float auraPadding = 65;
    [Min(0)] public float cornerRadius = 22;
    public TMP_FontAsset numberFont;
    public Font sourceFont;
    public Vector2 numberOffset = new Vector2(60.34f,22.63f);
    [Min(.1f)] public float separatorWidth = 3.33f;
    [Min(0)] public float numberGlow = 1;
    public bool behindHand = true;
    [Min(10)] public float fontSize = 90.51f;
    public Color numberColor = Color.white;
    [Header("Liquid / 色・常時の波")]
    public Shader liquidShader;
    public Color dryColor = new Color(.07f,.06f,.13f,1);
    public Color liquidColor = new Color(1f,.945f,.416f,1);
    public Color depthColor = new Color(.80f,.71f,.96f,1);
    public Color wave2Color = Color.white;
    public Color glowColor = new Color(.769f,.608f,1,1);
    [Min(0)] public float liquidGlow = 1;
    [Min(0)] public float lightFlicker = 1;
    [Min(0)] public float lightFlickerSpeed = 1;
    public Color waveColor = new Color(.65f,.37f,1f,1);
    [Range(0,1)] public float auraStrength = 1;
    [Min(0)] public float waveAmplitude = 3;
    [Min(0)] public float waveSpeed = 1.2f;
    [Min(.05f)] public float levelResponse = .2f;
    [Header("Ripple and Numbers / 水位変化・数字")]
    [Min(0)] public float impactWaveAmplitude = 13;
    [Min(.1f)] public float rippleDuration = 1.2f;
    [Min(.1f)] public float numberTransition = .46f;
    [Min(0)] public float numberRefraction = 4.5f;
    [Header("Card Fall / カード落下")]
    [Min(.1f)] public float dropDuration = .37f;
    [Min(0)] public float dropDistance = 182;
    [Min(20)] public float dropCardWidth = 103;
    public float cardStartRoll = -9;
    public bool dropFromRelease=true;
    public float dropArcHeight=0;
    [Header("Charge Column / チャージの光柱")]
    public Color beamColor = new Color(.78f,.59f,1f,1);
    [Min(.1f)] public float beamDuration = .8f;
    [Min(30)] public float beamHeight = 240;
    [Min(10)] public float beamWidth = 173;

    [Header("Charge Shape / 光柱の詳細")]
    public Shader chargeEffectsShader;
    [Min(0)] public float beamIntensity=1;
    [Min(0)] public float beamCoreWidth=10;
    [Range(0,1)] public float beamSoftness=.28f;
    [Range(.02f,1)] public float beamRiseFraction=.3f;
    [Range(0,.95f)] public float beamFadeStart=.34f;
    [Min(.1f)] public float beamFadePower=1;
    [Range(0,1)] public float beamCurveAmount=.04f;
    [Range(0,1)] public float beamFilamentStrength=.08f;
    [Header("Root Glow / 根元の光だまり")]
    public Color rootGlowColor=new Color(.77f,.61f,1,1);
    [Min(0)] public float rootGlowWidth=170;
    [Min(0)] public float rootGlowHeight=60;
    [Min(0)] public float rootGlowStrength=1;
    [Min(0)] public float rootGlowTail=.32f;
    [Header("Charge Layer / 手札より前")]
    [Min(1)] public int chargeSortingOffset=40;
    [Header("Drag Target / 判定内だけの発光と波紋")]
    [Min(.15f)] public float targetRipplePeriod=.72f;
    [Min(0)] public float targetRippleStrength=1.55f;
    [Min(.1f)] public float targetRippleWidth=1.5f;
    [Min(0)] public float targetRippleSpread=30;
    [Range(0,1)] public float targetGlowStrength=.35f;
    [Header("Liquid Suits / 白いスートの光")]
    public Texture2D suitAtlas;
    public Color suitColor=Color.white;
    [Range(0,8)] public int suitCount=6;
    [Range(.1f,3)] public float suitScale=1;
    [Range(0,2)] public float suitWarp=1;
    [Min(0)] public float suitSpeed=1;
    [Range(0,1)] public float suitOpacity=.58f;
    [Min(0)] public float suitGlow=.2f;
}
