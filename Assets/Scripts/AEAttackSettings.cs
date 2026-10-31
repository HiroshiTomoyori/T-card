using System;
using UnityEngine;

[CreateAssetMenu(menuName="T-card/AE Attack Settings")]
public sealed class AEAttackSettings : ScriptableObject
{
    [Serializable] public sealed class Palette
    {
        public Color blade=Color.white,bloom=Color.white,fragments=Color.white;
        public Color At(int pass)=>pass==0?bloom:pass==1?fragments:blade;
    }
    [Header("AE attack / 有効・無効")]
    public bool enabledEffect=true;
    [Header("Normal / 通常の斬撃")]
    public Palette normal=new Palette{blade=new Color(.96f,1,1),bloom=new Color(.22f,.83f,.72f),fragments=new Color(.72f,1,.91f)};
    [Header("Monarch normal / モナークの通常攻撃（クロスとは独立）")]
    public Palette monarchNormal=new Palette{blade=new Color(1,.965f,1),bloom=new Color(.67f,.31f,.95f),fragments=new Color(.9f,.75f,1)};
    [Header("Monarch wall / モナークのウォール攻撃")]
    public Palette monarchWall=new Palette{blade=new Color(1,.965f,1),bloom=new Color(.67f,.31f,.95f),fragments=new Color(.9f,.75f,1)};
    [Header("Appearance / 発光・透明度")]
    [Range(0,1)] public float opacity=1;
    [Range(0,1)] public float glowOpacity=1;
    [Header("Card effect / 負けたカードへの表示")]
    [Min(.1f),Tooltip("AE画面の高さ / カードの高さ。承認プレビューと同じ比率は約2.54")]
    public float cardHeightMultiplier=2.5411765f;
    public Vector2 cardSizeMultiplier=Vector2.one;
    public Vector2 cardOffset=Vector2.zero;
    [Range(-180,180)] public float cardAngle=0;
    [Header("Wall effect / クロスの大きさ・位置")]
    [Min(.1f)] public float wallWidthMultiplier=1.45f;
    public Vector2 wallSizeMultiplier=Vector2.one;
    public Vector2 wallOffset=Vector2.zero;
    [Range(-180,180)] public float wallAngle=0;
    [Range(1,9),Tooltip("この枚数の列幅を下限に使用。残り1枚でもクロスを縮めない") ] public int wallReferenceCount=5;
    [Header("Timing / 演出の再生速度")]
    [Min(.05f)] public float effectSpeed=1;
    [Tooltip("バトルでは接触した瞬間にAEの着弾フレームを表示。着弾前の予備光は省略")]
    public bool battleStartAtContactFrame=true;
    [Header("Battle contact / 演出カードの接触")]
    [Tooltip("踏み込みの頂点で両カードの縁が触れるよう、演出用コピーの移動を補正")]
    public bool alignBattleContact=true;
    [Range(0,1),Tooltip("接触位置への移動を攻撃カードが受け持つ割合")]
    public float attackerContactShare=.8f;
    [Range(0,1),Tooltip("横方向の寄せ具合。カードが接触できる最小限の移動は維持") ] public float battleLateralMotion=.8f;
    [Min(0)] public float contactGap=0;
    [HideInInspector] public AEAttackClip straightClip,wallCrossClip;
    static AEAttackSettings cached;
    public static AEAttackSettings Load()
    {
        if(cached==null)cached=Resources.Load<AEAttackSettings>("TCardAttackEffects/AEAttackSettings");
        return cached;
    }
    public Palette Colors(bool monarch,bool cross)=>cross?monarchWall:monarch?monarchNormal:normal;
}
