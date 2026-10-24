using UnityEngine;

[CreateAssetMenu(menuName = "T-card/Coin Toss Settings", fileName = "CoinTossSettings")]
public sealed class CoinTossSettings : ScriptableObject
{
    [Header("Design / デザイン")]
    public Texture2D butterflyTexture;
    public Texture2D moonTexture;
    public Shader coinShader;
    [Tooltip("Texture UV bounds; update these if replacing the source images. x,y = lower left, z,w = size.")]
    public Vector4 butterflyUV = new Vector4(0, 0, 1, 1);
    public Vector4 moonUV = new Vector4(0, 0, 1, 1);
    public Color coinTint = Color.white;
    [Range(0.1f, 2f)] public float brightness = 1f;
    [Range(0f, 2f)] public float saturation = 1f;
    [Range(0f, 0.3f)] public float sheen = 0.09f;
    [Range(0f, 0.8f)] public float backgroundDim = 0.42f;

    [Header("Size and Position / 大きさ・位置 (432 x 924 基準)")]
    [Min(0.1f)] public float overallScale = 1f;
    [Min(1)] public float startDiameter = 180f;
    [Min(1)] public float endDiameter = 224f;
    [Range(0f, 0.65f)] public float perspectiveStrength = 0.43f;
    public Vector2 positionOffset = Vector2.zero;
    [Range(0f, 1f)] public float restingHeightFromTop = 0.59f;
    [Min(0)] public float jumpHeight = 194f;
    public float lateralDrift = 10f;

    [Header("Timing / 時間")]
    [Min(0.1f)] public float duration = 2f;
    [Min(0.05f)] public float playbackSpeed = 1f;
    [Min(0)] public float resultHoldSeconds = 0.9f;
    public bool useUnscaledTime = true;
    [Range(0.1f, 0.95f)] public float flightPortion = 0.82f;
    [Range(0.1f, 1f)] public float spinPortion = 0.91f;

    [Header("Rotation and Landing / 回転・着地")]
    [Min(1)] public int fullRotations = 2;
    public float initialFlipDegrees = 12f;
    [Range(0.1f, 0.99f)] public float brakingStart = 0.83f;
    public float initialRollDegrees = -12f;
    public float rollSwayDegrees = 5f;
    [Min(0)] public float bounceHeight = 15f;
    [Min(0)] public float settlingWobbleDegrees = 7f;

    [Header("Edge / 厚み・側面")]
    [Range(0f, 0.1f)] public float thicknessRatio = 0.041f;
    public Color edgeDark = new Color(101f / 255f, 69f / 255f, 28f / 255f);
    public Color edgeLight = new Color(197f / 255f, 143f / 255f, 65f / 255f);
    public Color edgeHighlight = new Color(239f / 255f, 196f / 255f, 111f / 255f);
    [Min(1)] public float reedingSpacing = 5f;
    [Range(0f, 1f)] public float reedingStrength = 0.55f;
    [Range(0f, 1f)] public float shadowOpacity = 0.45f;
}
