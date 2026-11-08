using UnityEngine;

[CreateAssetMenu(menuName = "T-card/Confirmation Theme")]
public sealed class ConfirmationTheme : ScriptableObject
{
    [Header("Window / 720 px reference width")]
    public Vector2 windowSize = new Vector2(366f, 208f);
    [Min(0)] public float cornerRadius = 11f;
    [Range(0, 32)] public float backdropBlur = 12f;
    [Range(0, .3f)] public float confirmationLowlightBoost = .10f;
    [Range(0, 1)] public float glassOpacity = .67f;
    public Color glassColor = new Color32(17, 29, 50, 255);
    public Color lowerLight = new Color32(88, 121, 159, 255);
    public Color frameColor = new Color32(173, 193, 218, 105);
    [Range(0, 1)] public float reflectionStrength = .76f;
    public Color textColor = new Color32(248, 249, 251, 255);

    [Header("Choice buttons")]
    public Vector2 buttonSize = new Vector2(134f, 44f);
    [Min(0)] public float buttonCornerRadius = 6f;
    public Color primaryButtonColor = new Color32(50, 70, 94, 232);
    public Color secondaryButtonColor = new Color32(29, 41, 58, 232);
    public Color buttonTextColor = new Color32(251, 252, 255, 255);

    [Header("Attack positions / +X right, +Y up")]
    public Vector2 fieldAttackOffset;
    public Vector2 wallAttackOffset;
    public Vector2 monarchWallAttackOffset;
    public Vector2 directAttackOffset;
    [Tooltip("Find the closest position that keeps attack targets and swords clear. Turn off to place confirmations at the configured positions, within the screen.")]
    public bool avoidCardsAndSwords = true;

    [System.Serializable]
    public sealed class IncomingAttackOffsets
    {
        public Vector2 fieldTarget;
        public Vector2 wallTarget;
        public Vector2 monarchWallTarget;
        public Vector2 directAttack;

        internal Vector2 For(bool field, bool wall, bool monarch) =>
            field ? fieldTarget : wall ? (monarch ? monarchWallTarget : wallTarget) : directAttack;
    }

    [Header("Defense positions")]
    public IncomingAttackOffsets blockConfirmation = new IncomingAttackOffsets();
    public IncomingAttackOffsets passConfirmation = new IncomingAttackOffsets();
    public Vector2 passButtonOffset;

    [Header("Action confirmation positions")]
    public Vector2 playCardOffset;
    public Vector2 endTurnOffset;
    public Vector2 useShieldEffectOffset;
    public Vector2 takeShieldIntoHandOffset;
    public Vector2 shieldOrderOffset;
    public Vector2 destroyCardOffset;
    public Vector2 jokerClearFieldOffset;
    public Vector2 jokerExtraTurnOffset;
    public Vector2 otherActionOffset;

    [Header("Opening, card choices, and results")]
    public Vector2 openingHandOffset;
    public Vector2 handLimitOffset;
    public Vector2 enemyHandDiscardOffset;
    public Vector2 victoryOffset;
    public Vector2 defeatOffset;

    internal Vector2 AttackOffset(AttackConfirmationKind kind)
    {
        switch (kind)
        {
            case AttackConfirmationKind.Wall: return wallAttackOffset;
            case AttackConfirmationKind.MonarchWall: return monarchWallAttackOffset;
            case AttackConfirmationKind.Direct: return directAttackOffset;
            default: return fieldAttackOffset;
        }
    }

    internal Vector2 ActionOffset(string key)
    {
        switch (key)
        {
            case "end-turn": return endTurnOffset;
            case "shield-use": return useShieldEffectOffset;
            case "shield-hand": return takeShieldIntoHandOffset;
            case "joker-clear": return jokerClearFieldOffset;
            case "joker-extra": return jokerExtraTurnOffset;
        }
        if (key == null) return otherActionOffset;
        if (key.StartsWith("summon:", System.StringComparison.Ordinal)) return playCardOffset;
        if (key.StartsWith("shield-order:", System.StringComparison.Ordinal)) return shieldOrderOffset;
        if (key.StartsWith("destroy-target:", System.StringComparison.Ordinal)) return destroyCardOffset;
        return otherActionOffset;
    }

    static ConfirmationTheme cached;
    public static ConfirmationTheme Load()
    {
        if (cached != null) return cached;
        cached = Resources.Load<ConfirmationTheme>("UI/ConfirmationTheme");
        if (cached == null)
        {
            cached = CreateInstance<ConfirmationTheme>();
            cached.hideFlags = HideFlags.HideAndDontSave;
        }
        return cached;
    }

    public Vector2 WindowSize => new Vector2(Mathf.Max(210f, windowSize.x), Mathf.Max(168f, windowSize.y));

    internal Color StrengthenLowlight(Color color, float amount = 1f)
    {
        color.a = Mathf.Clamp01(color.a + confirmationLowlightBoost * Mathf.Clamp01(amount));
        return color;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/T-card/Confirmation Theme")]
    static void SelectTheme()
    {
        const string path = "Assets/Resources/UI/ConfirmationTheme.asset";
        var theme = UnityEditor.AssetDatabase.LoadAssetAtPath<ConfirmationTheme>(path);
        if (theme == null)
        {
            theme = CreateInstance<ConfirmationTheme>();
            UnityEditor.AssetDatabase.CreateAsset(theme, path);
            cached = theme;
        }
        UnityEditor.Selection.activeObject = theme;
        UnityEditor.EditorGUIUtility.PingObject(theme);
    }
#endif
}

internal enum AttackConfirmationKind { Field, Wall, MonarchWall, Direct }
