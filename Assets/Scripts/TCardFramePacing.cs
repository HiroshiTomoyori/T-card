using UnityEngine;

static class TCardFramePacing
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void UseDisplayRefreshRate()
    {
        if (Application.isEditor || !Application.isMobilePlatform) return;
        Apply();
        Application.focusChanged += focused => { if (focused) Apply(); };
    }

    static void Apply()
    {
        double refreshRate = Screen.currentResolution.refreshRateRatio.value;
        Application.targetFrameRate = refreshRate > 0 ? Mathf.RoundToInt((float)refreshRate) : 60;
    }
}
