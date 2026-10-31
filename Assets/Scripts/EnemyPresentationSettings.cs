using UnityEngine;
[CreateAssetMenu(menuName="T-card/Enemy Presentation Settings")]
public sealed class EnemyPresentationSettings:ScriptableObject
{
 [Header("Enemy draw / 裏面のドロー")]
 public Vector2 drawSize=new Vector2(130,195);
 [Tooltip("敵ウォール中央を基準とするUI座標。正のYで敵手札側へ")]
 public Vector2 drawOffset=new Vector2(0,90);
 [Min(.01f)]public float drawTravel=.24f;
 [Min(.01f)]public float drawSettle=.22f;
 [Min(0)]public float drawHold=.25f;
 [Min(.01f)]public float drawToHand=.2f;
 [Min(0)]public float drawInterval=.08f;
 [Header("Draw highlight / ハイライトと影")]
 [Range(0,1)]public float highlightStrength=.5f;
 [Min(0)]public float outerGlow=.8f;
 [Min(0)]public float glowSpread=32;
 public Color highlightColor=new Color(1,.97f,.84f,1);
 [Range(0,1)]public float shadowStrength=.24f;
 [Min(1)]public float shadowBlur=22;
 public Vector2 shadowOffset=new Vector2(18,-34);
 public Color shadowColor=new Color(.176f,.114f,.067f,1);
 [Header("Charge reveal / 公開してから落とす")]
 [Tooltip("リソース水面の上方を基準に公開位置を調整。Xは左右、Yは上下のUI単位") ]
 public Vector2 chargeRevealOffset=Vector2.zero;
 [Tooltip("公開カードの大きさ。水面へ落とすカードとは独立して調整")]
 public Vector2 chargeRevealSize=new Vector2(117,175.5f);
 [Min(0),Tooltip("手札中央から下へずらして公開する距離")]
 public float chargeRevealBelowHand=135;
 [Min(0)]public float chargeScreenPadding=20;
 [Min(0)]public float chargeMinimumVisibleDuration=.45f;
 [Min(1)]public int chargeRevealSortingOffset=60;
 [Min(.01f)]public float chargeTravel=.34f;
 [Min(0)]public float chargeHold=.28f;
 [Min(0)]public float afterChargeDelay=.5f;
 static EnemyPresentationSettings cached;
 public static EnemyPresentationSettings Load(){if(cached==null)cached=Resources.Load<EnemyPresentationSettings>("TCardDraw/EnemyPresentationSettings");if(cached==null){cached=CreateInstance<EnemyPresentationSettings>();cached.hideFlags=HideFlags.HideAndDontSave;}return cached;}
}
