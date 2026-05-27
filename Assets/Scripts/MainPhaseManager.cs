using UnityEngine;
using UnityEngine.UI;

public class MainPhaseManager : MonoBehaviour
{
    public Image battleAreaImage;

    bool isMainPhase = false;

    public bool IsMainPhase
    {
        get { return isMainPhase; }
    }

    public void StartMainPhase()
    {
        isMainPhase = true;

        if (battleAreaImage != null)
        {
            battleAreaImage.raycastTarget = true;
            Debug.Log("BattleArea Raycast ON");
        }
        else
        {
            Debug.Log("battleAreaImage が None");
        }

        Debug.Log("=== Main Phase 開始 ===");
    }

    public void EndMainPhase()
    {
        isMainPhase = false;

        if (battleAreaImage != null)
        {
            battleAreaImage.raycastTarget = false;
            Debug.Log("BattleArea Raycast OFF");
        }

        Debug.Log("=== Main Phase 終了 ===");
    }
}