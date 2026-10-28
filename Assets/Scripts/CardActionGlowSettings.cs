using UnityEngine;
[CreateAssetMenu(menuName="T-card/Card Action Glow Settings")]
public sealed class CardActionGlowSettings : ScriptableObject
{
    public bool enabledEffect = true;
    [Header("色 / 初期値は攻撃・防御ともエメラルドブルー")]
    public Color attackColor = new Color(32f/255,214f/255,196f/255,1);
    public Color defenseColor = new Color(32f/255,214f/255,196f/255,1);
    [Header("明るさと呼吸する速さ")]
    [Range(0,2)] public float brightness = 1;
    [Range(0,1)] public float minimumBrightness = .25f;
    [Min(.2f),Tooltip("暗い→明るい→暗いの1周期 / 秒")]
    public float pulsePeriod = 4.4f;
    [Min(0)] public float fadeDuration = .22f;
    [Header("形 / カード幅118px基準")]
    [Min(1)] public float blur = 15;
    [Min(.1f)] public float lineWidth = 2.5f;
    [Min(0)] public float cornerRadius = 8;
    public float edgeOffset = 2;
    public Vector2 positionOffset = Vector2.zero;
    static CardActionGlowSettings cached;
    public static CardActionGlowSettings Load()
    {
        if(cached!=null)return cached;
        cached=Resources.Load<CardActionGlowSettings>("TCardField/CardActionGlowSettings");
        if(cached==null){cached=CreateInstance<CardActionGlowSettings>();cached.hideFlags=HideFlags.HideAndDontSave;}
        return cached;
    }
    public float EvaluatePulse(float time)
    {
        float wave=(1-Mathf.Cos(time/Mathf.Max(.2f,pulsePeriod)*Mathf.PI*2))*.5f;
        return Mathf.Lerp(minimumBrightness,1,wave);
    }
}
