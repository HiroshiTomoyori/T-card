using UnityEngine;
using UnityEngine.UI;

public class TitleAdvancedRuleToggle : MonoBehaviour
{
    [SerializeField] private Toggle advancedRuleToggle;

    private void Start()
    {
        advancedRuleToggle.isOn = GameSettings.IsAdvancedRule;
        advancedRuleToggle.onValueChanged.AddListener(OnAdvancedRuleChanged);
    }

    private void OnAdvancedRuleChanged(bool isOn)
    {
        GameSettings.IsAdvancedRule = isOn;

        Debug.Log("アドバンスドルール: " + isOn);
    }
}