using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DebugManager : MonoBehaviour
{
    public static DebugManager I { get; private set; }

    [Header("UI Raycast Debug")]
    [SerializeField]
    private bool enableUIRaycastDebug = true;

    [SerializeField]
    private bool showOnlyTopResult = false;

    [SerializeField]
    private KeyCode toggleKey = KeyCode.F1;

    void Awake()
    {
        if(I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    void Update()
    {
        if(Input.GetKeyDown(toggleKey))
        {
            enableUIRaycastDebug =
                !enableUIRaycastDebug;

            Debug.Log(
                enableUIRaycastDebug
                    ? "UI Raycast Debug：ON"
                    : "UI Raycast Debug：OFF"
            );
        }

        if(!enableUIRaycastDebug)
            return;

        if(Input.GetMouseButtonDown(0))
        {
            LogUIRaycast(Input.mousePosition);
        }
    }

    public void LogUIRaycast(Vector2 screenPosition)
    {
        if(EventSystem.current == null)
        {
            Debug.LogWarning(
                "EventSystemが見つかりません"
            );

            return;
        }

        PointerEventData pointerData =
            new PointerEventData(
                EventSystem.current
            );

        pointerData.position = screenPosition;

        List<RaycastResult> results =
            new List<RaycastResult>();

        EventSystem.current.RaycastAll(
            pointerData,
            results
        );

        Debug.Log(
            "=== UI Raycast Results ==="
        );

        Debug.Log(
            "クリック位置：" +
            screenPosition
        );

        if(results.Count == 0)
        {
            Debug.Log(
                "Raycast対象なし"
            );

            return;
        }

        int resultCount =
            showOnlyTopResult
                ? 1
                : results.Count;

        for(int i = 0; i < resultCount; i++)
        {
            RaycastResult result =
                results[i];

            GameObject hitObject =
                result.gameObject;

            string hierarchyPath =
                GetHierarchyPath(
                    hitObject.transform
                );

            Debug.Log(
                "[" + i + "] " +
                hierarchyPath +
                "\nComponent: " +
                GetRaycastComponentName(
                    hitObject
                )
            );
        }
    }

    string GetHierarchyPath(
        Transform target
    )
    {
        if(target == null)
            return "null";

        string path = target.name;

        Transform parent =
            target.parent;

        while(parent != null)
        {
            path =
                parent.name +
                "/" +
                path;

            parent = parent.parent;
        }

        return path;
    }

    string GetRaycastComponentName(
        GameObject target
    )
    {
        if(target == null)
            return "null";

        UnityEngine.UI.Graphic graphic =
            target.GetComponent<
                UnityEngine.UI.Graphic
            >();

        if(graphic != null)
        {
            return
                graphic.GetType().Name +
                " / RaycastTarget: " +
                graphic.raycastTarget;
        }

        return "Graphicなし";
    }
}