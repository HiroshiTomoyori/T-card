using UnityEngine;

namespace CCP
{

/// <summary>
/// Global settings for CardControllerPro.
/// Create an instance via Assets > Create > CardControllerPro > Settings
/// and place it in a Resources folder so it is loaded automatically.
/// If no asset is found, default values are used.
/// </summary>
[CreateAssetMenu(fileName = "CardControllerSettings", menuName = "CardControllerPro/Settings")]
public class CardControllerSettings : ScriptableObject {
    private static CardControllerSettings _instance;

    public static CardControllerSettings Instance {
        get {
            if (_instance == null) {
                _instance = Resources.Load<CardControllerSettings>("CardControllerSettings");
                if (_instance == null)
                    _instance = CreateInstance<CardControllerSettings>();
            }
            return _instance;
        }
    }

    [Header("Animation Settings")]
    [Tooltip("When enabled, all animations run on unscaled time and will continue playing when Time.timeScale is 0 (e.g. while the game is paused).")]
    public bool useUnscaledTime = false;
}

}
