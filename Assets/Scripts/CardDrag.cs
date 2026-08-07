using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class CardDrag : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("ドラッグ中サイズ（ピクセル）")]
    [Tooltip("ドラッグ中のカード幅")]
    public float dragWidth = 190f;

    [Tooltip("ドラッグ中のカード高さ")]
    public float dragHeight = 285f;

    [Header("バトルエリアサイズ")]
    public float battleScale = 1.2f;

    [Header("召喚スナップ演出")]
    [Tooltip("場に出る直前の拡大倍率")]
    public float dropZoomMultiplier = 1.35f;

    [Tooltip("拡大状態を見せる時間")]
    public float dropZoomDuration = 0.16f;

    [Header("召喚スナップSE")]
    [Tooltip("スナップ拡大開始時に鳴らすSE")]
    public AudioClip dropSnapSE;

    [Range(0f, 1f)]
    public float dropSnapSEVolume = 1f;

    [Tooltip("任意。未設定ならカードにAudioSourceを自動追加")]
    public AudioSource dropSnapAudioSource;

    Transform originalParent;
    Vector3 originalPosition;
    Quaternion originalRotation;
    Vector3 originalScale;
    Vector2 originalSizeDelta;

    bool droppedSuccessfully = false;
    bool canDrag = false;

    Canvas canvas;
    CanvasGroup canvasGroup;
    RectTransform rectTransform;
    Coroutine dropSnapCoroutine;

    public bool IsDropSnapPlaying
    {
        get { return dropSnapCoroutine != null; }
    }

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        if(canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if(IsInBattleArea())
            return;

        canDrag = false;

        HandController hand = GetComponentInParent<HandController>();
        if(hand != null && hand.IsIdle)
            return;

        if(canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if(canvas == null)
                return;
        }

        if(rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if(rectTransform == null)
                return;
        }

        canDrag = true;
        droppedSuccessfully = false;

        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;
        originalSizeDelta = rectTransform.sizeDelta;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        transform.localRotation = Quaternion.identity;

        ApplyDraggingSize();
        canvasGroup.blocksRaycasts = false;

        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null && rpm.IsRunning())
        {
            rpm.ShowDropHighlight();
            rpm.SetResourceDropRaycast(true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!canDrag)
            return;

        transform.position = eventData.position;
        transform.localRotation = Quaternion.identity;
        ApplyDraggingSize();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(!canDrag)
            return;

        canDrag = false;

        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null)
        {
            rpm.HideDropHighlight();
            rpm.SetResourceDropRaycast(false);
        }

        canvasGroup.blocksRaycasts = true;

        if(droppedSuccessfully)
            return;

        transform.SetParent(originalParent, false);
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        transform.localScale = originalScale;

        if(rectTransform != null)
            rectTransform.sizeDelta = originalSizeDelta;
    }

    void ApplyDraggingSize()
    {
        if(rectTransform == null)
            return;

        transform.localScale = Vector3.one;

        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            dragWidth
        );

        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            dragHeight
        );
    }

    public void MarkDroppedSuccessfully()
    {
        droppedSuccessfully = true;
    }

    public void DropToBattleArea(Transform battleArea)
    {
        if(battleArea == null)
            return;

        droppedSuccessfully = true;

        transform.SetParent(battleArea, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if(rectTransform != null)
            rectTransform.sizeDelta = originalSizeDelta;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if(dropSnapCoroutine != null)
            StopCoroutine(dropSnapCoroutine);

        dropSnapCoroutine = StartCoroutine(DropSnapRoutine());

        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null)
        {
            rpm.HideDropHighlight();
            rpm.SetResourceDropRaycast(false);
        }
    }

    IEnumerator DropSnapRoutine()
    {
        PlayDropSnapSE();

        // まず大きく見せ、短い間を置いてから場のサイズへビタッと戻す。
        transform.localScale =
            Vector3.one * battleScale * dropZoomMultiplier;

        // 拡大演出中は召喚酔いが付与されても透過させない。
        float elapsed = 0f;

        while(elapsed < dropZoomDuration)
        {
            canvasGroup.alpha = 1f;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localScale = Vector3.one * battleScale;

        // スナップ後は本来の召喚酔い表示へ戻す。
        CardController card = GetComponent<CardController>();
        canvasGroup.alpha =
            card != null && card.hasSummonSickness
            ? 0.6f
            : 1f;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        dropSnapCoroutine = null;
    }

    void PlayDropSnapSE()
    {
        if(dropSnapSE == null)
            return;

        if(dropSnapAudioSource == null)
        {
            dropSnapAudioSource = GetComponent<AudioSource>();

            if(dropSnapAudioSource == null)
                dropSnapAudioSource = gameObject.AddComponent<AudioSource>();

            dropSnapAudioSource.playOnAwake = false;
            dropSnapAudioSource.spatialBlend = 0f;
        }

        dropSnapAudioSource.PlayOneShot(
            dropSnapSE,
            dropSnapSEVolume
        );
    }

    bool IsInBattleArea()
    {
        Transform current = transform.parent;

        while(current != null)
        {
            if(current.name == "PlayerBattleArea" ||
               current.name == "EnemyBattleArea")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    void OnDisable()
    {
        if(dropSnapCoroutine != null)
        {
            StopCoroutine(dropSnapCoroutine);
            dropSnapCoroutine = null;
        }

        if(canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null)
        {
            rpm.HideDropHighlight();
            rpm.SetResourceDropRaycast(false);
        }
    }
}
