using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;

public class CardDrag :
    MonoBehaviour,
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
    public float dropZoomMultiplier = 1.8f;

    [Tooltip("拡大状態を見せる時間")]
    public float dropZoomDuration = 0.18f;

    [Header("召喚スナップSE")]
    [Tooltip("スナップ拡大開始時に鳴らすSE")]
    public AudioClip dropSnapSE;
    [Min(0f)] public float dropZoomHoldDuration = 0.55f;
    public AudioClip dropLandingSE;

    [Range(0f, 1f)]
    public float dropSnapSEVolume = 1f;

    [Tooltip("任意。未設定ならカードにAudioSourceを自動追加")]
    public AudioSource dropSnapAudioSource;

    Transform originalParent;
    Vector3 originalPosition;
    Quaternion originalRotation;
    Vector3 originalScale;
    Vector2 originalSizeDelta;
    Vector3 draggingScale;

    bool droppedSuccessfully = false;
    bool canDrag = false;

    Canvas canvas;
    CanvasGroup canvasGroup;
    RectTransform rectTransform;

    Coroutine dropSnapCoroutine;
    bool battlePlacementStarted;
    bool battlePlacementFinished;
CCP.Card proCard;
CCP.CardGroup sourceProGroup;
HandController sourceHand;
bool proWasEnabled;

public bool IsDragging => canDrag;

void SuspendProCard()
{
    proCard = GetComponent<CCP.Card>();

    if (proCard == null)
        return;

    sourceProGroup = proCard.parentCardGroup;
    proWasEnabled = proCard.enabled;

    proCard.Unhover();
    proCard.enabled = false;

    transform.DOKill();
    proCard.isBeingMovedManually = true;
}

void ReleaseProHand()
{
    if (proCard == null)
        return;

    transform.DOKill();
    proCard.isBeingMovedManually = false;

    if (sourceProGroup != null &&
        proCard.parentCardGroup == sourceProGroup)
    {
        sourceProGroup.RemoveCard(proCard);
    }

    // フィールドのCCP操作は、次の段階で接続する。
    proCard.enabled = false;

    if (sourceHand != null)
        sourceHand.RefreshProLayout();
}
    public bool IsDropSnapPlaying
    {
        get
        {
            return dropSnapCoroutine != null;
        }
    }

    void Awake()
    {
        canvas =
            GetComponentInParent<Canvas>();

        canvasGroup =
            GetComponent<CanvasGroup>();

        rectTransform =
            GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }
    }
public void OnBeginDrag(PointerEventData eventData)
{
    canDrag = false;

    if (eventData == null ||
        IsInBattleArea() ||
        IsDropSnapPlaying)
    {
        return;
    }

    sourceHand = GetComponentInParent<HandController>();

    // 展開している手札だけ操作する。
    if (sourceHand == null ||
        sourceHand.IsIdle ||
        sourceHand.IsOpeningLook)
    {
        return;
    }

    HandDealer dealer = FindFirstObjectByType<HandDealer>();

    // 自分の手札だけ操作する。
    if (dealer == null ||
        dealer.handArea == null ||
        transform.parent != dealer.handArea ||
        dealer.IsHandLimitSelecting)
    {
        return;
    }

    ResourcePhaseManager rpm =
        FindFirstObjectByType<ResourcePhaseManager>();

TurnManager turnManager =
    FindFirstObjectByType<TurnManager>();

if (turnManager == null || !turnManager.CanDragPlayerHand)
    return;

    if (canvas == null)
        canvas = GetComponentInParent<Canvas>();

    if (rectTransform == null)
        rectTransform = GetComponent<RectTransform>();

    if (canvas == null || rectTransform == null)
        return;

    droppedSuccessfully = false;

    originalParent = transform.parent;
    originalPosition = transform.localPosition;
    originalRotation = transform.localRotation;
    originalScale = transform.localScale;
    originalSizeDelta = rectTransform.sizeDelta;

    sourceHand.ResetDoubleClick();
    SuspendProCard();

    canDrag = true;

    // Keep the expanded hand size when changing to the Canvas parent.
    Vector3 handScale = originalParent.lossyScale;
    Vector3 canvasScale = canvas.transform.lossyScale;
    draggingScale = new Vector3(
        sourceHand.expandScale * handScale.x / canvasScale.x,
        sourceHand.expandScale * handScale.y / canvasScale.y,
        sourceHand.expandScale * handScale.z / canvasScale.z);
    transform.SetParent(canvas.transform, true);
    transform.SetAsLastSibling();
    transform.localRotation = Quaternion.identity;

    ApplyDraggingSize();

    canvasGroup.blocksRaycasts = false;

// リソースへのドロップ表示はResource中だけ。
if (rpm != null && rpm.IsRunning())
{
    rpm.ShowDropHighlight();
    rpm.SetResourceDropRaycast(true);
}
}
    /*public void OnBeginDrag(
        PointerEventData eventData
    )
    {
        if (IsInBattleArea())
            return;

        canDrag = false;

        HandController hand =
            GetComponentInParent<HandController>();

        if (hand != null && hand.IsIdle)
            return;

        if (canvas == null)
        {
            canvas =
                GetComponentInParent<Canvas>();

            if (canvas == null)
                return;
        }

        if (rectTransform == null)
        {
            rectTransform =
                GetComponent<RectTransform>();

            if (rectTransform == null)
                return;
        }

        canDrag = true;
        droppedSuccessfully = false;

        originalParent =
            transform.parent;

        originalPosition =
            transform.localPosition;

        originalRotation =
            transform.localRotation;

        originalScale =
            transform.localScale;

        originalSizeDelta =
            rectTransform.sizeDelta;

        transform.SetParent(
            canvas.transform,
            true
        );

        transform.SetAsLastSibling();

        transform.localRotation =
            Quaternion.identity;

        ApplyDraggingSize();

        canvasGroup.blocksRaycasts = false;

        ResourcePhaseManager rpm =
            FindFirstObjectByType
                <ResourcePhaseManager>();

        if (
            rpm != null &&
            rpm.IsRunning()
        )
        {
            rpm.ShowDropHighlight();
            rpm.SetResourceDropRaycast(true);
        }
    }*/
 /*   public void OnDrag(
        PointerEventData eventData
    )
    {
        if (!canDrag)
            return;

        transform.position =
            eventData.position;

        transform.localRotation =
            Quaternion.identity;

        ApplyDraggingSize();
    }*/
public void OnDrag(PointerEventData eventData)
{
    if (!canDrag ||
        eventData == null ||
        canvas == null ||
        rectTransform == null)
    {
        return;
    }

    RectTransform parentRect =
        rectTransform.parent as RectTransform;

    if (parentRect == null)
        return;

    Camera uiCamera =
        canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

    if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
        parentRect,
        eventData.position,
        uiCamera,
        out Vector3 worldPosition))
    {
        rectTransform.position = worldPosition;
    }

    transform.localRotation = Quaternion.identity;
    ApplyDraggingSize();
}

public void OnEndDrag(PointerEventData eventData)
{
    if (!canDrag)
        return;

    canDrag = false;

    ResourcePhaseManager rpm =
        FindFirstObjectByType<ResourcePhaseManager>();

    if (rpm != null)
    {
        rpm.HideDropHighlight();
        rpm.SetResourceDropRaycast(false);
    }

    if (droppedSuccessfully)
        return;

    canvasGroup.blocksRaycasts = true;

    if (originalParent == null)
    {
        ReleaseProHand();
        return;
    }

    // 現在の見た目の位置を保って手札の親へ戻す。
    transform.SetParent(originalParent, true);
    rectTransform.sizeDelta = originalSizeDelta;

    if (proCard != null &&
        sourceProGroup != null &&
        proCard.parentCardGroup == sourceProGroup)
    {
        proCard.isBeingMovedManually = false;
        proCard.enabled = proWasEnabled;

        // CCPで元の手札配置へ滑らかに戻す。
        if (sourceHand != null)
            sourceHand.RefreshProLayout();
        else
            sourceProGroup.RecalculateAndAnimatePositions();
    }
    else
    {
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        transform.localScale = originalScale;
    }

    if (sourceHand != null)
        sourceHand.ResetDoubleClick();
}
 /*   public void OnEndDrag(
        PointerEventData eventData
    )
    {
        if (!canDrag)
            return;

        canDrag = false;

        ResourcePhaseManager rpm =
            FindFirstObjectByType
                <ResourcePhaseManager>();

        if (rpm != null)
        {
            rpm.HideDropHighlight();
            rpm.SetResourceDropRaycast(false);
        }

        canvasGroup.blocksRaycasts = true;

        if (droppedSuccessfully)
            return;

        transform.SetParent(
            originalParent,
            false
        );

        transform.localPosition =
            originalPosition;

        transform.localRotation =
            originalRotation;

        transform.localScale =
            originalScale;

        if (rectTransform != null)
        {
            rectTransform.sizeDelta =
                originalSizeDelta;
        }
    }*/

    void ApplyDraggingSize()
    {
        if (rectTransform == null) return;
        transform.localScale = draggingScale;
        rectTransform.sizeDelta = originalSizeDelta;
    }

public void MarkDroppedSuccessfully()
{
    droppedSuccessfully = true;
    ReleaseProHand();
}

    /*
     * 通常カードはスナップ演出を再生する。
     *
     * A：
     * DestroyOneEnemyBattle
     *
     * 9：
     * TapAllEnemyBattle
     *
     * 上記の効果を持つカードは、
     * 呼び出し側の指定に関係なく
     * スナップ演出とSEを無効化する。
     */
    public void PlaceFromShield(Transform battleArea)
    {
        // A shield summon has no OnBeginDrag to capture its starting geometry.
        if(rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if(rectTransform != null) originalSizeDelta = rectTransform.sizeDelta;
        DropToBattleArea(battleArea);
    }

    public void DropToBattleArea(
        Transform battleArea,
        bool playSnap = true
    )
    {
        if (battleArea == null)
            return;

            ReleaseProHand();

        CardController card =
            GetComponent<CardController>();

        bool isAce =
            HasEffect(
                card,
                EffectType.DestroyOneEnemyBattle
            );

        bool isNine =
            HasEffect(
                card,
                EffectType.TapAllEnemyBattle
            );

        /*
         * BattleDropZoneが古い呼び出し方でも、
         * A・9なら強制的にスナップを無効化する。
         */
        if (isAce || isNine)
        {
            playSnap = false;

            Debug.Log(
                "アクトカードのスナップを強制無効化：" +
                GetCardDebugName(card)
            );
        }

        droppedSuccessfully = true;
        canDrag = false;

        if (dropSnapCoroutine != null)
        {
            StopCoroutine(
                dropSnapCoroutine
            );

            dropSnapCoroutine = null;
        }

        BattleCardClick battleClick = GetComponent<BattleCardClick>();
        if (battleClick == null) battleClick = gameObject.AddComponent<BattleCardClick>();
        battleClick.enabled = true;

        transform.SetParent(battleArea, true);
        if (!playSnap) transform.localPosition = Vector3.zero;

        transform.localRotation =
            Quaternion.identity;

        if (rectTransform != null)
        {
            rectTransform.sizeDelta =
                originalSizeDelta;
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (playSnap)
        {
            battlePlacementStarted = false;
            battlePlacementFinished = false;
            if (proCard != null) proCard.isBeingMovedManually = true;
            dropSnapCoroutine =
                StartCoroutine(
                    DropSnapRoutine()
                );
        }
        else
        {
            ApplyBattleAreaSizeWithoutSnap();

            Debug.Log(
                "スナップなしでカードを配置：" +
                GetCardDebugName(card)
            );
        }

        ResourcePhaseManager rpm =
            FindFirstObjectByType
                <ResourcePhaseManager>();

        if (rpm != null)
        {
            rpm.HideDropHighlight();
            rpm.SetResourceDropRaycast(false);
        }
    }

    void ApplyBattleAreaSizeWithoutSnap()
    {
        /*
         * 拡大アニメーションを行わず、
         * 最終的なバトルエリア用サイズへ
         * 直接変更する。
         */
        transform.localScale =
            Vector3.one * battleScale;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        dropSnapCoroutine = null;
    }

    public void PlayBattlePlacement(Vector3 target, Quaternion rotation, float targetScale, float duration)
    {
        if (!IsDropSnapPlaying || battlePlacementStarted) return;
        battlePlacementStarted = true;
        proCard = GetComponent<CCP.Card>();
        if (proCard == null) proCard = gameObject.AddComponent<CCP.Card>();
        proCard.enabled = false;
        proCard.destinationPosition = target;
        proCard.destinationRotation = rotation;
        CCP.IPlaceAnimation animation = new BattleCardPlaceAnimation
        {
            targetScale = targetScale,
            zoomMultiplier = dropZoomMultiplier,
            zoomDuration = dropZoomDuration,
            holdDuration = dropZoomHoldDuration,
            onLanding = () => PlaySummonSE(dropLandingSE),
            settleDuration = Mathf.Max(0.3f, duration)
        };
        animation.Place(proCard, target, () => battlePlacementFinished = true);
    }

    IEnumerator DropSnapRoutine()
    {
        PlayDropSnapSE();
        yield return null; // Allow the coroutine handle and battle layout to be ready.
        BattleAreaLayout layout = GetComponentInParent<BattleAreaLayout>();
        if (layout != null) layout.Refresh();
        else PlayBattlePlacement(transform.position, transform.rotation, battleScale, 0.25f);
        while (!battlePlacementFinished)
        {
            canvasGroup.alpha = 1f;
            yield return null;
        }
        if (proCard != null) proCard.isBeingMovedManually = false;
        CardController card = GetComponent<CardController>();
        canvasGroup.alpha = card != null && card.hasSummonSickness ? 0.6f : 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        dropSnapCoroutine = null;
    }
    void PlayDropSnapSE() { PlaySummonSE(dropSnapSE); }

    void PlaySummonSE(AudioClip clip)
    {
        if (clip == null)
            return;

        if (dropSnapAudioSource == null)
        {
            dropSnapAudioSource =
                GetComponent<AudioSource>();

            if (dropSnapAudioSource == null)
            {
                dropSnapAudioSource =
                    gameObject
                        .AddComponent<AudioSource>();
            }

            dropSnapAudioSource.playOnAwake =
                false;

            dropSnapAudioSource.spatialBlend =
                0f;
        }

        dropSnapAudioSource.PlayOneShot(
            clip,
            dropSnapSEVolume
        );
    }

    bool HasEffect(
        CardController card,
        EffectType effectType
    )
    {
        if (
            card == null ||
            card.data == null ||
            card.data.effectTypes == null
        )
        {
            return false;
        }

        return System.Array.Exists(
            card.data.effectTypes,
            effect => effect == effectType
        );
    }

    string GetCardDebugName(
        CardController card
    )
    {
        if (
            card != null &&
            card.data != null
        )
        {
            if (
                !string.IsNullOrEmpty(
                    card.data.cardName
                )
            )
            {
                return card.data.cardName;
            }

            if (
                !string.IsNullOrEmpty(
                    card.data.name
                )
            )
            {
                return card.data.name;
            }
        }

        return gameObject.name;
    }

    bool IsInBattleArea()
    {
        Transform current =
            transform.parent;

        while (current != null)
        {
            if (
                current.name ==
                    "PlayerBattleArea" ||
                current.name ==
                    "EnemyBattleArea"
            )
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    void OnDisable()
    {
        if (dropSnapCoroutine != null)
        {
            transform.DOKill();
            if (proCard != null) proCard.isBeingMovedManually = false;
            StopCoroutine(
                dropSnapCoroutine
            );

            dropSnapCoroutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        ResourcePhaseManager rpm =
            FindFirstObjectByType
                <ResourcePhaseManager>();

        if (rpm != null)
        {
            rpm.HideDropHighlight();
            rpm.SetResourceDropRaycast(false);
        }
    }
}
[System.Serializable]
public class BattleCardPlaceAnimation : CCP.IPlaceAnimation
{
    public float targetScale = 1f;
    public float zoomMultiplier = 1.35f;
    public float zoomDuration = 0.16f;
    public float settleDuration = 0.3f;
    public float holdDuration = 0.55f;
    public System.Action onLanding;

    public void Place(CCP.Card card, Vector3 target, System.Action onComplete)
    {
        if (card == null) { onComplete?.Invoke(); return; }
        card.transform.DOKill();

        Vector3 enlargedScale = card.transform.localScale * Mathf.Max(1f, zoomMultiplier);
        bool completed = false;
        System.Action finish = () =>
        {
            if (completed) return;
            completed = true;

            onComplete?.Invoke();
        };
        Sequence sequence = DOTween.Sequence().SetTarget(card.transform)
            .SetUpdate(CCP.CardControllerSettings.Instance.useUnscaledTime);
        sequence.Append(card.transform.DOScale(enlargedScale, Mathf.Max(0.01f, zoomDuration)).SetEase(Ease.OutBack));
        sequence.AppendInterval(Mathf.Max(0f, holdDuration));
        sequence.Append(card.transform.DOMove(target, Mathf.Max(0.01f, settleDuration)).SetEase(Ease.OutCubic));
        sequence.Join(card.transform.DORotateQuaternion(card.destinationRotation, Mathf.Max(0.01f, settleDuration)).SetEase(Ease.OutCubic));
        sequence.Join(card.transform.DOScale(Vector3.one * targetScale, Mathf.Max(0.01f, settleDuration)).SetEase(Ease.OutCubic));
        sequence.AppendCallback(() => onLanding?.Invoke());
        sequence.Append(card.transform.DOPunchScale(Vector3.one * targetScale * 0.12f, 0.18f, 2, 0.4f));
        sequence.OnComplete(() => { card.onPlaced?.Invoke(); finish(); });
        sequence.OnKill(() => finish());
    }
}
