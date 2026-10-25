using UnityEngine;
[CreateAssetMenu(menuName="T-card/Card Shadow Settings",fileName="CardShadowSettings")]
public sealed class CardShadowSettings : ScriptableObject
{
    public bool shadowsEnabled=true;
    public Shader shadowShader;
    public Color shadowColor=new Color(24/255f,16/255f,12/255f,1);
    [Range(0,1)] public float strength=.38f;
    [Range(0,30)] public float blur=10;
    [Range(0,1)] public float contactStrength=.24f;
    [Min(.1f)] public float contactBlur=2.5f;
    public Vector2 offset=new Vector2(3,-4);
    [Tooltip("Preview card width. Shadows scale with each card's width.")]
    [Min(1)] public float referenceWidth=156;
    [Range(0,.25f)] public float cornerRadiusRatio=.08f;
}
