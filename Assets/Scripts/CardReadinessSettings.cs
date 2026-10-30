using UnityEngine;
[CreateAssetMenu(menuName="T-card/Card Readiness Settings")]
public sealed class CardReadinessSettings:ScriptableObject
{
    [Header("レスト：傾けず軽く暗くする")]
    [Range(0,1)] public float restDarkness=.1f;
    [Header("出撃準備：着地後の変化")]
    [Min(0),Tooltip("カードがフィールドに着いてから準備姿勢へ変わる時間（秒）") ] public float preparationTransitionDuration=.5f;
    [Header("出撃準備：後方へ配置 / UI単位")]
    [Min(0)] public float rearOffset=10;
    [Range(.8f,1)] public float preparingScale=.97f;
    [Header("準備アイコン / カード幅110基準")]
    [Min(1)] public float iconSize=30;
    public Vector2 iconOffset=new Vector2(-4,4);
    public Color iconColor=new Color(.91f,.8f,.55f,1);
    public Color iconBackground=new Color(.2f,.18f,.145f,1);
    [Min(.2f)] public float pulsePeriod=3.2f;
    static CardReadinessSettings cached;
    public static CardReadinessSettings Load(){if(cached!=null)return cached;cached=Resources.Load<CardReadinessSettings>("TCardField/CardReadinessSettings");if(cached==null){cached=CreateInstance<CardReadinessSettings>();cached.hideFlags=HideFlags.HideAndDontSave;}return cached;}
}
