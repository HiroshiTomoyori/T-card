using UnityEngine;

[CreateAssetMenu(menuName="T-card/Wall Destruction Settings")]
public sealed class WallDestructionSettings : ScriptableObject
{
    public bool enabledEffect=true;
    [Header("発光 / カード画像とは独立")]
    public Color glowColor=new Color(.965f,.975f,1,1);
    [Range(0,1)] public float opacity=1;
    [Header("破片の光 / 下から上のグラデーション")]
    public Color bottomColor=new Color(.64f,.70f,.78f,1);
    public Color topColor=new Color(.94f,.97f,1,1);
    [Range(0,1)] public float gradientStrength=1;
    [Range(0,.3f)] public float sheenStrength=.08f;
    [Header("破片の縁 / 薄いプラチナ色")]
    public Color rimColor=new Color(.62f,.79f,.90f,1);
    [Range(0,1)] public float rimStrength=.55f;
    [Range(.001f,.025f),Tooltip("カードの高さに対する縁の幅")]
    public float rimWidth=.006f;
    [Header("背面の柔らかなシャドウ")]
    public bool shadowEnabled=true;
    public Color shadowColor=new Color(.055f,.065f,.09f,1);
    [Range(0,1)] public float shadowOpacity=.32f;
    [Range(.01f,.3f),Tooltip("ウォールカードの高さに対するぼかし幅")]
    public float shadowSoftness=.08f;
    [Tooltip("ウォールカードの高さに対する影のずれ")]
    public Vector2 shadowOffset=new Vector2(0,-.025f);
    [Header("演出全体のグラデーション影 / 破片の影とは独立")]
    public bool backdropEnabled=true;
    public Color backdropColor=new Color(.025f,.035f,.055f,1);
    [Range(0,1)] public float backdropOpacity=.52f;
    [Tooltip("演出全体の幅・高さに対する影の広がり")]
    public Vector2 backdropSizeMultiplier=new Vector2(.9f,1.15f);
    [Range(.5f,4),Tooltip("中心から外側へ薄くなるグラデーションの形")]
    public float backdropFalloff=1.8f;
    [Tooltip("ウォールカードの高さに対する影のずれ")]
    public Vector2 backdropOffset=new Vector2(0,.08f);
    [Header("大きさ・位置 / ウォールカードの高さを基準")]
    [Min(.05f)] public float size=1;
    public Vector2 sizeMultiplier=Vector2.one;
    public Vector2 playerOffset=Vector2.zero,enemyOffset=Vector2.zero;
    [Range(-180,180)] public float angle;
    [Header("速さ・レイヤー")]
    [Min(.05f)] public float speed=1;
    [Min(0)] public float endHold=.08f;
    public int sortingOffset=180;
    [HideInInspector] public int dataRevision;
    [HideInInspector] public Vector2 sourceSize=new Vector2(1280,720);
    [HideInInspector] public float sourceCardHeight=396;
    [HideInInspector] public float frameRate=60;
    [HideInInspector] public int impactFrame=7;
    [HideInInspector] public Texture2D[] frames;
    [HideInInspector] public Rect[] rectangles;
    public bool IsValid {get{if(frames==null||impactFrame<0||frames.Length<=impactFrame||rectangles==null||rectangles.Length!=frames.Length)return false;for(int i=impactFrame;i<frames.Length;i++)if(frames[i]==null||rectangles[i].width<=0||rectangles[i].height<=0)return false;return true;}}
    static WallDestructionSettings cached;
    public static WallDestructionSettings Load()=>cached!=null?cached:(cached=Resources.Load<WallDestructionSettings>("TCardWallDestruction/WallDestructionSettings"));
}


