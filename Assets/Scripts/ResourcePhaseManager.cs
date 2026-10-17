using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ResourcePhaseManager : MonoBehaviour
{
    [Header("Drop Highlight")]
    public GameObject resourceGlow;
    [Min(0.2f)] public float dragHighlightPeriod = 0.9f;
    [Range(0f, 1f)] public float dragHighlightMinAlpha = 0.25f;
    public Color dragHighlightColor = new Color(1f, 0.78f, 0.24f, 0.85f);

    Coroutine dragHighlightRoutine;
    CanvasGroup dragHighlightGroup;
    Image dragHighlightImage;
    Color originalHighlightColor;
    bool highlightColorCaptured;

    [Header("Drop Area")]
    public Image resourceAreaImage;

    [Header("Resource")]
    public Transform resourceArea;
    public ResourceManager resourceManager;

    [Header("Charge Effect")]
    public GameObject chargeLightEffectPrefab;
    public AudioSource seSource;
    public AudioClip chargeSE;
    public float effectTime = 0.6f;
    [Header("White Charge Column")]
    [Min(0.1f)] public float chargeColumnDuration = 0.45f;
    [Min(20f)] public float chargeColumnHeight = 320f;
    [Min(8f)] public float chargeColumnWidth = 110f;
    GameObject activeChargeColumn;

    [Header("Resource Drop Raycast")]
    public CanvasGroup resourceDropArea;

    bool isRunning = false;
    bool hasCharged = false;
    public bool IsResolvingCharge => isRunning && hasCharged;

    void Start()
    {
        DisableResourceDrop();

        if(resourceGlow != null)
            resourceGlow.SetActive(false);
    }

    public void ShowDropHighlight()
    {
        if(!isRunning || hasCharged || !isActiveAndEnabled)
        {
            HideDropHighlight();
            return;
        }
        if(resourceGlow == null || dragHighlightRoutine != null) return;

        Canvas parentCanvas = resourceGlow.GetComponentInParent<Canvas>();
        Canvas glowCanvas = resourceGlow.GetComponent<Canvas>();
        if(glowCanvas == null) glowCanvas = resourceGlow.AddComponent<Canvas>();
        if(parentCanvas != null && parentCanvas != glowCanvas)
        {
            glowCanvas.overrideSorting = true;
            glowCanvas.sortingLayerID = parentCanvas.sortingLayerID;
            glowCanvas.sortingOrder = parentCanvas.sortingOrder + 10;
        }

        dragHighlightGroup = resourceGlow.GetComponent<CanvasGroup>();
        if(dragHighlightGroup == null) dragHighlightGroup = resourceGlow.AddComponent<CanvasGroup>();
        dragHighlightGroup.blocksRaycasts = false;
        dragHighlightGroup.interactable = false;
        foreach(Graphic graphic in resourceGlow.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        dragHighlightImage = resourceGlow.GetComponent<Image>();
        if(dragHighlightImage != null)
        {
            if(!highlightColorCaptured)
            {
                originalHighlightColor = dragHighlightImage.color;
                highlightColorCaptured = true;
            }
            dragHighlightImage.color = dragHighlightColor;
        }
        dragHighlightGroup.alpha = 1f;
        resourceGlow.SetActive(true);
        resourceGlow.transform.SetAsLastSibling();
        dragHighlightRoutine = StartCoroutine(PulseDropHighlight());
    }

    IEnumerator PulseDropHighlight()
    {
        float elapsed = 0f;
        while(isRunning && !hasCharged && resourceGlow != null && resourceGlow.activeInHierarchy)
        {
            float phase = elapsed / Mathf.Max(0.2f, dragHighlightPeriod) * Mathf.PI * 2f;
            float brightness = 0.5f + 0.5f * Mathf.Cos(phase);
            if(dragHighlightGroup != null)
                dragHighlightGroup.alpha = Mathf.Lerp(dragHighlightMinAlpha, 1f, brightness);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        dragHighlightRoutine = null;
        HideDropHighlight();
    }

    public void HideDropHighlight()
    {
        if(dragHighlightRoutine != null)
        {
            StopCoroutine(dragHighlightRoutine);
            dragHighlightRoutine = null;
        }
        if(dragHighlightGroup != null) dragHighlightGroup.alpha = 1f;
        if(dragHighlightImage != null && highlightColorCaptured)
            dragHighlightImage.color = originalHighlightColor;
        if(resourceGlow != null)
            resourceGlow.SetActive(false);
    }

    void OnDisable()
    {
        SetResourceDropRaycast(false);
        HideDropHighlight();
        if(activeChargeColumn != null) Destroy(activeChargeColumn);
        activeChargeColumn = null;
    }

    public void StartResourcePhase()
    {
        isRunning = true;
        hasCharged = false;

        // 見た目は表示したまま、当たり判定だけOFF
        SetResourceDropRaycast(false);

        Debug.Log("リソースフェイズ開始");
    }

    public bool IsRunning()
    {
        return isRunning;
    }

    public void TryChargeResource(CardDrag card)
    {
        HideDropHighlight();

        if(!isRunning)
        {
            Debug.Log("今はリソースフェイズではありません");
            return;
        }

        if(hasCharged)
        {
            Debug.Log("このターンはすでにチャージ済みです");
            return;
        }

        if(card == null)
        {
            Debug.LogError("CardDrag が未設定です");
            return;
        }

        TurnManager decisionManager = FindFirstObjectByType<TurnManager>();
        if (decisionManager != null && decisionManager.RequestPlayerDecision("resource:" + card.GetInstanceID(),
            "このカードをリソースに入れます。\nよろしいですか？", () => TryChargeResource(card))) return;

        hasCharged = true;
        card.MarkDroppedSuccessfully();

        StartCoroutine(
            ChargeRoutine(card.gameObject)
        );
    }

    IEnumerator ChargeRoutine(GameObject cardObj)
    {
        DisableResourceDrop();

        if(resourceArea != null)
        {
            cardObj.transform.SetParent(
                resourceArea,
                false
            );

            RectTransform rt =
                cardObj.GetComponent<RectTransform>();

            if(rt != null)
            {
                rt.anchoredPosition =
                    Vector2.zero;

                rt.localScale =
                    Vector3.one;
            }
        }

        // ★ ドロップしたカードをすぐ非表示
        CanvasGroup cg =
            cardObj.GetComponent<CanvasGroup>();

        if(cg != null)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        if(seSource != null &&
           chargeSE != null)
        {
            seSource.PlayOneShot(chargeSE);
        }

        GameObject effectObj = null;
        CreateChargeColumn();

        if(chargeLightEffectPrefab != null &&
           resourceArea != null)
        {
            effectObj =
                Instantiate(
                    chargeLightEffectPrefab,
                    resourceArea
                );

            RectTransform effectRt =
                effectObj.GetComponent<RectTransform>();

            if(effectRt != null)
            {
                effectRt.anchoredPosition =
                    Vector2.zero;

                effectRt.localScale =
                    Vector3.one;
            }
        }

        yield return new WaitForSeconds(effectTime);

        if(resourceManager != null)
        {
            resourceManager.AddResource();
        }
        else
        {
            Debug.LogError(
                "ResourceManager が未設定です"
            );
        }

        Destroy(cardObj);

        if(effectObj != null)
        {
            Destroy(effectObj);
        }
        if(activeChargeColumn != null) Destroy(activeChargeColumn);
        activeChargeColumn = null;

        Debug.Log("リソースチャージ完了");

        CompleteResourcePhase();
    }

    void CreateChargeColumn()
    {
        if(resourceArea == null) return;
        Canvas parentCanvas = resourceArea.GetComponentInParent<Canvas>();
        if(parentCanvas == null) return;
        Canvas rootCanvas = parentCanvas.rootCanvas;
        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if(canvasRect == null) return;
        if(activeChargeColumn != null) Destroy(activeChargeColumn);
        activeChargeColumn = new GameObject("Resource Charge White Column", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Canvas), typeof(ResourceChargeColumnGraphic));
        activeChargeColumn.layer = resourceArea.gameObject.layer;
        activeChargeColumn.transform.SetParent(canvasRect, false);
        RectTransform columnRect = activeChargeColumn.transform as RectTransform;
        columnRect.anchorMin = columnRect.anchorMax = canvasRect.pivot;
        columnRect.pivot = new Vector2(0.5f, 0f);
        RectTransform zoneRect = resourceArea as RectTransform;
        Vector3 origin = zoneRect != null ? zoneRect.TransformPoint(zoneRect.rect.center) : resourceArea.position;
        columnRect.anchoredPosition = canvasRect.InverseTransformPoint(origin);
        columnRect.sizeDelta = new Vector2(chargeColumnWidth * 2f, chargeColumnHeight);
        Canvas overlay = activeChargeColumn.GetComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingLayerID = rootCanvas.sortingLayerID;
        overlay.sortingOrder = rootCanvas.sortingOrder + 40;
        activeChargeColumn.GetComponent<ResourceChargeColumnGraphic>()
            .Initialize(chargeColumnDuration, chargeColumnWidth);
    }

    public void CompleteResourcePhase()
    {
        if(!isRunning)
            return;

        EndResourcePhase();

        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if(turnManager != null)
        {
            turnManager.OnResourcePhaseComplete();
        }
        else
        {
            Debug.LogError(
                "TurnManager が見つかりません"
            );
        }
    }

    public void EndResourcePhase()
    {
        isRunning = false;

        // リソースエリアの当たり判定をOFF
        SetResourceDropRaycast(false);

        HideDropHighlight();

        Debug.Log("リソースフェイズ終了");
    }

    public void SetResourceAreaRaycast(bool enabled)
    {
        if(resourceAreaImage == null)
            return;

        resourceAreaImage.raycastTarget =
            enabled;
    }

    void EnableResourceDrop()
    {
        if(resourceAreaImage == null)
            return;

        resourceAreaImage.raycastTarget =
            true;
    }

    void DisableResourceDrop()
    {
        if(resourceAreaImage == null)
            return;

        resourceAreaImage.raycastTarget =
            false;
    }

    public void SetResourceDropRaycast(bool enabled)
    {
        if(!enabled) HideDropHighlight();
        if(resourceDropArea != null)
        {
            // 表示状態は変更しない
            resourceDropArea.alpha = 1f;

            // リソースフェイズ中かつドラッグ中だけ有効
            resourceDropArea.blocksRaycasts =
                isRunning && !hasCharged && enabled;

            resourceDropArea.interactable =
                isRunning && !hasCharged && enabled;
        }

        if(resourceAreaImage != null)
        {
            // Image自体の当たり判定も同時に切り替える
            resourceAreaImage.raycastTarget =
                isRunning && !hasCharged && enabled;
        }
    }

    public bool IsResourcePhase()
    {
        return isRunning;
    }
}

// Runtime-only UI mesh: a white core, soft halo and rising sparks; no input interception.
public class ResourceChargeColumnGraphic : MaskableGraphic
{
    float age, duration = 0.45f, width = 110f;

    public void Initialize(float seconds, float beamWidth)
    {
        duration = Mathf.Max(0.1f, seconds);
        width = Mathf.Max(8f, beamWidth);
        raycastTarget = false;
        maskable = false;
        color = Color.white;
        SetVerticesDirty();
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        if(age >= duration) { Destroy(gameObject); return; }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        float progress = Mathf.Clamp01(age / duration);
        float rise = Mathf.SmoothStep(0f, 1f, progress / 0.23f);
        float fade = Mathf.Clamp01(progress / 0.055f) * Mathf.Clamp01((1f - progress) / 0.65f);
        float height = rectTransform.rect.height * rise;
        float halfWidth = width * 0.5f * Mathf.Lerp(0.65f, 1f, rise);
        // Layered gradients keep the edges soft without a texture or a particle camera.
        for(int layer = 0; layer < 7; layer++)
        {
            float radius = halfWidth * Mathf.Pow(0.68f, layer);
            float alpha = fade * (layer == 6 ? 0.95f : 0.12f + layer * 0.025f);
            Quad(vertices, new Vector2(-radius, 0f), new Vector2(radius, 0f),
                new Vector2(radius * 0.48f, height), new Vector2(-radius * 0.48f, height),
                new Color(1f, 1f, 1f, alpha), new Color(1f, 1f, 1f, alpha * 0.02f));
        }
        // A brief ground flash marks the resource zone as the light's origin.
        float flash = fade * Mathf.Clamp01((0.55f - progress) / 0.4f);
        int center = vertices.currentVertCount;
        vertices.AddVert(Vector2.zero, new Color(1f, 1f, 1f, flash), Vector2.zero);
        for(int i = 0; i <= 24; i++)
        {
            float angle = i / 24f * Mathf.PI * 2f;
            vertices.AddVert(new Vector2(Mathf.Cos(angle) * width * 0.85f,
                Mathf.Sin(angle) * 15f), new Color(1f, 1f, 1f, 0f), Vector2.zero);
            if(i > 0) vertices.AddTriangle(center, center + i, center + i + 1);
        }
        for(int i = 0; i < 10; i++)
        {
            float travel = Mathf.Clamp01(progress * (1.5f + i * 0.06f) - i * 0.025f);
            float x = Mathf.Sin(i * 2.4f) * halfWidth * 0.8f;
            float y = travel * height;
            float sparkle = fade * Mathf.Sin(travel * Mathf.PI) * 0.75f;
            float radius = 1.4f + (i % 3) * 0.45f;
            Quad(vertices, new Vector2(x - radius, y), new Vector2(x, y + radius * 3f),
                new Vector2(x + radius, y), new Vector2(x, y - radius * 3f),
                new Color(1f, 1f, 1f, sparkle), new Color(1f, 1f, 1f, sparkle));
        }
    }

    static void Quad(VertexHelper vertices, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
        Color bottom, Color top)
    {
        int first = vertices.currentVertCount;
        vertices.AddVert(a, bottom, Vector2.zero); vertices.AddVert(b, bottom, Vector2.zero);
        vertices.AddVert(c, top, Vector2.zero); vertices.AddVert(d, top, Vector2.zero);
        vertices.AddTriangle(first, first + 1, first + 2);
        vertices.AddTriangle(first, first + 2, first + 3);
    }
}
