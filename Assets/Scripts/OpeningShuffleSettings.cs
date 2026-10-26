using UnityEngine;
[CreateAssetMenu(menuName="T-card/Opening Shuffle Settings",fileName="OpeningShuffleSettings")]
public sealed class OpeningShuffleSettings : ScriptableObject
{
    [Header("Overall / 全体")]
    [Min(.05f)] public float globalSpeed=1;
    public bool useUnscaledTime=false;
    [Header("Shuffle / 左画面外からの落下")]
    [Range(1,60)] public int visualCardCount=20;
    [Min(0)] public float startDelay=.16f;
    [Min(.02f)] public float cardFallDuration=.56f;
    [Min(0)] public float cardDropInterval=.055f;
    [Min(.1f),Tooltip("落下距離をカードの高さの倍数で指定")]
    public float fallHeightInCards=2.2f;
    [Min(0)] public float offscreenPadding=30;
    [Range(0,1)] public float sourceSpread=.28f;
    [Range(0,20)] public float startAngle=3;
    [Range(0,.5f)] public float approachScale=.1f;
    [Range(0,3)] public float stackThickness=.55f;
    [Min(1)] public int sortingOffset=25;
    [Min(1),Tooltip("配布中のカードを山札より前へ表示するソート差分")]
    public int dealSortingOffset=5;
    [Header("Settle / 整列と配布までの待機")]
    [Min(0)] public float alignDuration=.22f;
    [Min(0)] public float beforeDealDelay=.08f;
    [Header("Walls / 両陣営のウォール配布")]
    [Min(.02f)] public float wallTravelDuration=.45f;
    [Min(0)] public float wallDealInterval=.13f;
    [Min(0)] public float wallArcHeight=16;
    [Header("Initial Hand / 両陣営の初期手札・Redraw")]
    [Min(.02f)] public float handTravelDuration=.45f;
    [Min(0)] public float handDealInterval=.13f;
    public bool revealDuringDeal=true;
    [Min(0)] public float handFlipDelay=.09f;
    [Min(.02f)] public float handFlipDuration=.22f;
    [Min(0)] public float afterDealHold=.15f;
    public float Seconds(float value)=>Mathf.Max(0,value)/Mathf.Max(.05f,globalSpeed);
    public float DropEnd=>Mathf.Max(0,startDelay)+(Mathf.Clamp(visualCardCount,1,60)-1)*Mathf.Max(0,cardDropInterval)+Mathf.Max(.02f,cardFallDuration);
    public float ShuffleDuration=>DropEnd+Mathf.Max(0,alignDuration)+Mathf.Max(0,beforeDealDelay);
}
