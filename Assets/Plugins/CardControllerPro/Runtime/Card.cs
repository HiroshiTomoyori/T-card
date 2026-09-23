using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CCP
{

public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler {

    // ── Static State ──
    public static Action<bool> OnCardMoved;
    public static Card movingCard = null;
    public static Card isTargetting = null;
    public static Card targetedCard = null;
    public static bool stopOnHover = false;
    private static Card stopOnHoverCard = null;

    private static readonly DefaultPlaceAnimation FallbackPlaceAnimation = new();
    private static readonly ArcAttackAnimation FallbackAttackAnimation = new();
    private static readonly ScaleUpHoverAnimation FallbackHoverAnimation = new();
    private static readonly DefaultPickupAnimation FallbackPickupAnimation = new();
    private static readonly LiftSelectAnimation FallbackSelectAnimation = new();
    private static readonly NoneUseAnimation FallbackUseAnimation = new();

    // ── Inspector: Card Data ──
    [Header("Card Data")]
    public CardData cardData;

    // ── Inspector: Card Hierarchy References ──
    public GameObject cardTilter;
    public GameObject cardDragRotator;
    public GameObject cardFlipper;
    public GameObject cardHoverer;
    public GameObject cardPickuper;
    public GameObject cardVisuals;
    public GameObject front;
    public GameObject back;
    public GameObject border;
    public GameObject selectionMark;
    public GameObject clickTarget;
    public DynamicArrow dynamicArrow;

    // ── Inspector: Targeting ──
    public bool canTargetOwnCardGroup = false;

    // ── Inspector: Flip Settings ──
    [Header("Flip Settings")]
    [SerializeField] private float flipDuration = 0.1f;

    // ── Inspector: Drag Spring Settings ──
    [Header("Drag Spring Settings")]
    [SerializeField] private bool enableDragSpring = true;
    [SerializeField] private DampedSpring dragSpring = new DampedSpring();
    [SerializeField] private float dragSpringInputScale = 20f;
    [SerializeField] private float dragSpringMaxAngle = 25f;

    // ── Events ──
    public Action onPickedUp;
    public Action onDropped;
    public Action onPlaced;
    public Action onTargetingStarted;
    public Action<Card> onTargetedUse;
    public Action<Card> onAttackHit;
    public Action<CardGroup> onUsed;
    public Action onTargeted;
    public Action onUntargeted;

    // ── Runtime: Group & Destination ──
    [HideInInspector] public CardGroup parentCardGroup;
    [HideInInspector] public Vector3 destinationPosition;
    [HideInInspector] public Quaternion destinationRotation;
    [HideInInspector] [NonSerialized] public bool isBeingMovedManually = false;

    // ── Runtime: State Flags ──
    private bool isHovering;
    private bool isDragging;
    internal bool isBeingPlaced;
    internal bool isAttacking;
    internal bool isBeingUsed;
    private bool thisCardTargetting = false;
    private bool dragSpringActive;

    // ── Runtime: Cached Components ──
    private Camera mainCamera;
    private RectTransform rectTransform;
    private RectTransform clickTargetRectTransform;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private Vector2 clickTargetOriginalOffsetMin;
    private bool clickTargetOriginalOffsetMinCached;

    // ── Runtime: Original Scales ──
    Vector3 originalScale;
    internal Vector3 originalVisualsScale;
    internal Vector3 originalPickuperScale;

    // ── Runtime: Drag State ──
    private Vector2 initialCardPosition;
    private Vector2 initialMousePosition;
    private CardGroup dragSourceGroup;
    private int originalIndexInGroup = -1;
    private bool isDraggingWithinGroup = false;
    private bool hasReordered = false;
    private float lastReorderCheckTime = 0f;

    // ── Runtime: Tweens & Animation ──
    private float prevRotation = 0f;
    private Tween pickupRotationTween;
    private Tween flipTween;
    bool flipped = false;
    private float stopOnHoverDuration = 0.3f;
    private Sequence stopOnHoverSeq;

    /// <summary>
    /// Assigns CardData and calls its InitializeCard lifecycle method.
    /// </summary>
    public void Initialize(CardData data) {
        cardData = data;
        cardData?.InitializeCard(this);
    }

    private CardType cardType {
        get {
            if (cardData == null) return CardType.Place;
            if (cardData.placeableFromHand && parentCardGroup != null && parentCardGroup.isPlayerHand
                && cardData.cardType != CardType.Use && cardData.cardType != CardType.TargettedUse)
                return CardType.Place;
            return cardData.cardType;
        }
    }

    public bool IsUseType => cardType == CardType.Use;

    private IPlaceAnimation PlaceAnimationSafe =>
        cardData != null ? cardData.PlaceAnimationSafe : FallbackPlaceAnimation;

    private IAttackAnimation AttackAnimationSafe =>
        cardData != null ? cardData.AttackAnimationSafe : FallbackAttackAnimation;

    private IHoverAnimation HoverAnimationSafe =>
        parentCardGroup != null ? parentCardGroup.HoverAnimationSafe : FallbackHoverAnimation;

    private IPickupAnimation PickupAnimationSafe =>
        cardData != null ? cardData.PickupAnimationSafe : FallbackPickupAnimation;

    private ISelectAnimation SelectAnimationSafe =>
        parentCardGroup != null ? parentCardGroup.SelectAnimationSafe : FallbackSelectAnimation;

    private IUseAnimation UseAnimationSafe =>
        cardData != null ? cardData.UseAnimationSafe : FallbackUseAnimation;

    private void Awake() {
        originalScale = transform.localScale;
        if (cardHoverer != null)
            originalVisualsScale = cardHoverer.transform.localScale;
        if (cardPickuper != null)
            originalPickuperScale = cardPickuper.transform.localScale;
    }

    private void Start() {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;

        ValidateRequiredComponents();
    }

    private void ValidateRequiredComponents() {
        if (rectTransform == null)
            Debug.LogError($"Card '{name}' is missing RectTransform component", this);
        if (canvasGroup == null)
            Debug.LogError($"Card '{name}' is missing CanvasGroup component", this);
        if (parentCanvas == null)
            Debug.LogError($"Card '{name}' must be a child of a Canvas", this);
        if (cardHoverer == null)
            Debug.LogError($"Card '{name}' is missing cardVisuals reference", this);
        if (cardTilter == null && parentCardGroup?.EnableTilt == true)
            Debug.LogWarning($"Card '{name}' has enableTilt=true but cardTilter is not assigned", this);
        if (front == null || back == null)
            Debug.LogWarning($"Card '{name}' is missing front or back reference for flipping", this);
        if (clickTarget != null && clickTarget.GetComponent<RectTransform>() == null)
            Debug.LogWarning($"Card '{name}' clickTarget reference must have a RectTransform component", this);
        if (cardData == null)
            Debug.LogWarning($"Card '{name}' has no CardData assigned. It will default to Place behavior.", this);
    }

    public void SetClickTargetHoverExtension(Vector3 worldHoverOffset) {
        if (!TryGetClickTargetRectTransform())
            return;

        if (!clickTargetOriginalOffsetMinCached) {
            clickTargetOriginalOffsetMin = clickTargetRectTransform.offsetMin;
            clickTargetOriginalOffsetMinCached = true;
        }

        float extension = 0f;
        if (worldHoverOffset.sqrMagnitude > 0f && clickTargetRectTransform.parent != null) {
            Vector3 localHoverOffset = clickTargetRectTransform.parent.InverseTransformVector(worldHoverOffset);
            extension = Mathf.Abs(localHoverOffset.y);
        }

        Vector2 offsetMin = clickTargetOriginalOffsetMin;
        offsetMin.y -= extension;
        clickTargetRectTransform.offsetMin = offsetMin;
    }

    private bool TryGetClickTargetRectTransform() {
        RectTransform assignedRectTransform = clickTarget != null
            ? clickTarget.GetComponent<RectTransform>()
            : null;

        if (assignedRectTransform != clickTargetRectTransform) {
            if (clickTargetRectTransform != null && clickTargetOriginalOffsetMinCached) {
                clickTargetRectTransform.offsetMin = clickTargetOriginalOffsetMin;
            }

            clickTargetRectTransform = assignedRectTransform;
            clickTargetOriginalOffsetMinCached = false;
        }

        return clickTargetRectTransform != null;
    }

    public void MoveToPosition(Vector3 targetPosition, Quaternion targetRotation,
        Ease easeType, float duration, float? targetScale = null) {
        destinationPosition = targetPosition;
        destinationRotation = targetRotation;

        if (isBeingMovedManually) {
            return;
        }

        // Kill any existing tweens on this card
        transform.DOKill();

        // Moving the z position immediately so that the cards are ordered correctly
        transform.position = new Vector3(transform.position.x, transform.position.y, targetPosition.z);
        // Animate position and rotation
        transform.DOMove(targetPosition, duration).SetEase(easeType);
        transform.DORotateQuaternion(targetRotation, duration).SetEase(easeType);

        // Animate scale if target scale is provided
        if (targetScale.HasValue) {
            Vector3 targetScaleVector = Vector3.one * targetScale.Value;
            transform.DOScale(targetScaleVector, duration).SetEase(easeType);
        }
    }

    public void Hover() {
        if (isHovering) return;
        if (isTargetting || movingCard || isBeingPlaced || isAttacking || isBeingUsed || IsHoverSuppressed()) return;

        isHovering = true;
        HoverAnimationSafe.Hover(this);
        parentCardGroup?.OnCardHovered(this);
    }

    public void Unhover() {
        isHovering = false;

        if (isTargetting || movingCard || isBeingPlaced || isBeingUsed) return;

        HoverAnimationSafe.Unhover(this);
        parentCardGroup?.OnCardUnhovered(this);
    }

    public void Targeted() {
        if (!isTargetting || thisCardTargetting) return;
        if (!isTargetting.canTargetOwnCardGroup && parentCardGroup == isTargetting.parentCardGroup) return;
        if (parentCardGroup == null || !parentCardGroup.isTargettable) return;

        targetedCard = this;
        onTargeted?.Invoke();
        SetBorderActive(true);
    }

    public void Untargeted() {
        if (!isTargetting || thisCardTargetting) return;
        if (targetedCard != this) return;

        targetedCard = null;
        onUntargeted?.Invoke();
        SetBorderActive(false);
    }

    private void SetBorderActive(bool active) {
        border?.SetActive(active);
    }

    private void SetSelectionMarkActive(bool active) {
        if (selectionMark != null && parentCardGroup != null && parentCardGroup.showSelectionMark)
            selectionMark.SetActive(active);
    }

    public void SetCardGroup(CardGroup group) {
        if (parentCardGroup != group) {
            SetClickTargetHoverExtension(Vector3.zero);
        }

        if (group != null && canvasGroup != null && !canvasGroup.blocksRaycasts)
            EndDragFromDropZone();
        parentCardGroup = group;
    }

    public int GetPositionInCardGroup() {
        if (parentCardGroup == null) return 0;

        int index = parentCardGroup.cards.IndexOf(this);
        return index >= 0 ? index : 0;
    }

    public void MoveToGroup(CardGroup newGroup, IPlaceAnimation placeAnimation = null) {
        parentCardGroup?.RemoveCard(this);
        newGroup?.AddBack(this, placeAnimation);
    }

    public void MoveToGroupAtPosition(CardGroup newGroup, int index, IPlaceAnimation placeAnimation = null) {
        int targetCardCount = newGroup == parentCardGroup ? newGroup.cards.Count - 1 : newGroup.cards.Count;
        index = Mathf.Clamp(index, 0, targetCardCount);

        parentCardGroup?.RemoveCard(this);
        newGroup.AddAtPosition(this, index, placeAnimation);
    }

    public void StartTrackingMouse() {
        transform.DOKill();
        movingCard = this;
        initialCardPosition = rectTransform.anchoredPosition;
        initialMousePosition = Mouse.current.position.ReadValue();
    }

    public void StopTrackingMouse() {
        movingCard = null;
    }

    public void EndDragFromDropZone() {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;
        OnCardMoved?.Invoke(false);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        Hover();
        Targeted();
    }

    public void OnPointerExit(PointerEventData eventData) {
        Unhover();
        Untargeted();
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (cardType == CardType.Static) return;
        if (parentCardGroup != null && !parentCardGroup.allowPickup && cardType != CardType.Attack && cardType != CardType.TargettedUse) return;
        StartTrackingMouse();
        isBeingMovedManually = true;
        ResetTiltOnPickup();
        if (parentCardGroup != null && parentCardGroup.resetRotationOnPickup) {
            pickupRotationTween?.Kill();
            pickupRotationTween = transform.DOLocalRotate(Vector3.zero, 0.15f).SetEase(Ease.OutQuad);
        }
        PickupAnimationSafe.Pickup(this);
        cardData?.OnPickedUp(this);
        onPickedUp?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData) {
        isBeingMovedManually = false;
        StopTrackingMouse();
        PickupAnimationSafe.PutDown(this);
    }

    public void OnPointerClick(PointerEventData eventData) {
        switch (parentCardGroup.onClickAction) {
            case OnClickAction.Nothing:
                break;
            case OnClickAction.Flip:
                FlipCard();
                break;
            case OnClickAction.Custom:
                parentCardGroup.onClickCustom?.Invoke(this);
                break;
            case OnClickAction.Select:
                if (parentCardGroup.IsSelected(this)) {
                    parentCardGroup.DeselectCard(this);
                    SelectAnimationSafe.Deselect(this);
                    SetSelectionMarkActive(false);
                } else {
                    parentCardGroup.SelectCard(this);
                    SelectAnimationSafe.Select(this);
                    SetSelectionMarkActive(true);
                }
                break;
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (cardType == CardType.Static) return;
        if (parentCardGroup != null && !parentCardGroup.allowPickup && cardType != CardType.Attack && cardType != CardType.TargettedUse) return;
        isDragging = true;

        if (enableDragSpring && (cardType == CardType.Place || cardType == CardType.Use)) {
            dragSpring.Reset();
            dragSpringActive = true;
        }

        if (cardType == CardType.Place || cardType == CardType.Use) {
            BeginPlaceDrag();
        }

        if (cardType == CardType.Attack || cardType == CardType.TargettedUse) {
            BeginAttackTargeting(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData) {
        if (cardType == CardType.Static) return;
        if (parentCardGroup != null && !parentCardGroup.allowPickup && cardType != CardType.Attack && cardType != CardType.TargettedUse) return;

        if (enableDragSpring && dragSpringActive) {
            float scaledDelta = eventData.delta.x / parentCanvas.scaleFactor * dragSpringInputScale;
            dragSpring.AddVelocity(scaledDelta);
        }

        if (cardType == CardType.Place || cardType == CardType.Use) {
            UpdatePlaceDrag(eventData);
        }

        if ((cardType == CardType.Attack || cardType == CardType.TargettedUse) && isTargetting) {
            UpdateAttackTargeting(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (cardType == CardType.Static) return;
        if (parentCardGroup != null && !parentCardGroup.allowPickup && cardType != CardType.Attack && cardType != CardType.TargettedUse) return;

        DisallowOnHover();

        if (cardType == CardType.Place) {
            EndPlaceDrag(eventData);
        }

        if (cardType == CardType.Use) {
            EndUseDrag(eventData);
        }

        if (cardType == CardType.Attack) {
            EndAttackTargeting(eventData);
        }

        if (cardType == CardType.TargettedUse) {
            EndTargettedUseTargeting(eventData);
        }
    }

    private void ResetTiltOnPickup() {
        cardTilter.transform.DOKill();
        cardTilter.transform.localEulerAngles = Vector3.zero;
    }

    private void BeginPlaceDrag() {
        dragSourceGroup = parentCardGroup;

        if (parentCardGroup != null) {
            originalIndexInGroup = parentCardGroup.cards.IndexOf(this);
            isDraggingWithinGroup = true;
            hasReordered = false;
            lastReorderCheckTime = Time.time - 1f;
            parentCardGroup.OnCardUnhovered(this);
        }

        canvasGroup.blocksRaycasts = false;
        OnCardMoved?.Invoke(true);
    }

    private void UpdatePlaceDrag(PointerEventData eventData) {
        RectTransform parentRect = rectTransform.parent as RectTransform;
        Camera canvasCamera = parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, canvasCamera, out Vector2 currentLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position - eventData.delta, canvasCamera, out Vector2 previousLocal);

        rectTransform.anchoredPosition += currentLocal - previousLocal;

        if (parentCardGroup != null && parentCardGroup.allowReorderingDuringDrag) {
            CheckAndUpdateReorder();
        }
    }

    private void EndPlaceDrag(PointerEventData eventData) {
        pickupRotationTween?.Kill();
        canvasGroup.blocksRaycasts = true;
        OnCardMoved?.Invoke(false);
        StopTrackingMouse();

        // Card was dropped without being placed if it's still in the same group
        bool wasDroppedWithoutPlacing = parentCardGroup == dragSourceGroup;

        isBeingMovedManually = false;
        parentCardGroup?.RecalculateAndAnimatePositions();

        if (wasDroppedWithoutPlacing) {
            cardData?.OnDropped(this);
            onDropped?.Invoke();
        }

        ResetDragState();

        if (wasDroppedWithoutPlacing) {
            RefreshHoverUnderPointer(eventData);
        }
    }

    private void EndUseDrag(PointerEventData eventData) {
        EndPlaceDrag(eventData);
    }

    private void ResetDragState() {
        isDragging = false;
        isDraggingWithinGroup = false;
        hasReordered = false;
        originalIndexInGroup = -1;
        dragSourceGroup = null;
    }

    private void BeginAttackTargeting(PointerEventData eventData) {
        isTargetting = this;
        thisCardTargetting = true;
        canvasGroup.blocksRaycasts = false;

        if (dynamicArrow != null) {
            GetTargetingPositions(eventData.position, out Vector2 cardLocalPos, out Vector2 mouseLocalPos);
            dynamicArrow.PointAt(cardLocalPos, mouseLocalPos);
        }

        cardData?.OnTargetingStart(this);
        onTargetingStarted?.Invoke();
    }

    private void UpdateAttackTargeting(PointerEventData eventData) {
        if (dynamicArrow != null) {
            GetTargetingPositions(eventData.position, out Vector2 cardLocalPos, out Vector2 mouseLocalPos);
            dynamicArrow.PointAt(cardLocalPos, mouseLocalPos);
        }
    }

    private void EndAttackTargeting(PointerEventData eventData) {
        if (!isTargetting) return;

        targetedCard?.Untargeted();
        isTargetting = null;
        thisCardTargetting = false;
        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        if (dynamicArrow != null) {
            dynamicArrow.Hide();
        }

        Transform target = FindFirstValidTarget(eventData);
        if (target != null) {
            ExecuteAttack(target, () => Unhover());
        }
        else {
            Unhover();
        }
    }

    private void CheckAndUpdateReorder() {
        if (parentCardGroup == null || !parentCardGroup.allowReorderingDuringDrag)
            return;

        if (!ShouldCheckReorder())
            return;

        Vector3 localPos = parentCardGroup.transform.InverseTransformPoint(transform.position);

        if (Mathf.Abs(localPos.y) > parentCardGroup.reorderYThreshold) {
            isDraggingWithinGroup = false;
            return;
        }

        isDraggingWithinGroup = true;

        int currentIndex = parentCardGroup.cards.IndexOf(this);
        if (currentIndex == -1) return;

        int targetIndex = CalculateTargetIndex(localPos.x, currentIndex);

        if (targetIndex != currentIndex) {
            parentCardGroup.ReorderCardDuringDrag(this, targetIndex);
            hasReordered = true;
        }
    }

    private bool ShouldCheckReorder() {
        if (Time.time - lastReorderCheckTime < 0.05f) {
            return false;
        }

        lastReorderCheckTime = Time.time;
        return true;
    }

    private int CalculateTargetIndex(float draggedX, int currentIndex) {
        int targetIndex = 0;

        for (int i = 0; i < parentCardGroup.cards.Count; i++) {
            if (i == currentIndex) continue;

            Card otherCard = parentCardGroup.cards[i];
            Vector3 otherLocalPos = parentCardGroup.transform.InverseTransformPoint(otherCard.transform.position);

            if (draggedX > otherLocalPos.x) {
                targetIndex++;
            }
        }

        return targetIndex;
    }

    private Transform FindFirstValidTarget(PointerEventData eventData) {
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (var result in raycastResults) {
            if (result.gameObject.transform == this.transform)
                continue;

            // Search up the hierarchy for a Card component
            Card targetCard = result.gameObject.GetComponentInParent<Card>();
            if (targetCard != null && targetCard != this) {
                if (targetCard.parentCardGroup == null || !targetCard.parentCardGroup.isTargettable) continue;
                if (!canTargetOwnCardGroup && targetCard.parentCardGroup == parentCardGroup) continue;
                return targetCard.transform;
            }
        }

        return null;
    }

    private void RefreshHoverUnderPointer(PointerEventData eventData) {
        if (eventData == null || EventSystem.current == null) return;

        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (RaycastResult result in raycastResults) {
            Card hoveredCard = result.gameObject.GetComponentInParent<Card>();
            if (hoveredCard != null && hoveredCard != this) {
                hoveredCard.Hover();
                return;
            }
        }
    }

    public void FlipCard() {
        flipped = !flipped;
        flipTween?.Kill();
        flipTween = DOTween.Sequence()
            .Append(cardFlipper.transform.DOLocalRotate(new Vector3(0f, 90f, 0f), flipDuration))
            .AppendCallback(() => {
                front.SetActive(!flipped);
                back.SetActive(flipped);
            })
            .Append(cardFlipper.transform.DOLocalRotate(new Vector3(0f, 0f, 0f), flipDuration))
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
    
    public void FlipCardNoAnimation() {
        flipTween?.Kill();
        flipped = !flipped;
        front.SetActive(!flipped);
        back.SetActive(flipped);
    }

    // Disallow OnHover for a brief moment, so that cards have the time to settle into their final position
    public void DisallowOnHover() {
        stopOnHover = true;
        stopOnHoverCard = this;
        stopOnHoverSeq?.Kill();
        stopOnHoverSeq = DOTween.Sequence()
            .AppendInterval(stopOnHoverDuration)
            .AppendCallback(() => {
                if (stopOnHoverCard == this) {
                    stopOnHover = false;
                    stopOnHoverCard = null;
                }
            });
    }

    private bool IsHoverSuppressed() {
        return stopOnHover && (stopOnHoverCard == null || stopOnHoverCard == this);
    }
    
    private void Update() {
        CheckRotationBasedFlip();
        UpdateCardTilt();
        UpdateDragSpring();
    }

    private void CheckRotationBasedFlip() {
        float newRotation = cardFlipper.transform.localRotation.eulerAngles.y;

        if (Math.Abs(prevRotation - newRotation) > 0.001f) {
            bool wasBackFacing = IsBackFacing(prevRotation);
            bool isBackFacing = IsBackFacing(newRotation);

            if (wasBackFacing != isBackFacing) {
                FlipCardNoAnimation();
            }
        }

        prevRotation = newRotation;
    }

    private bool IsBackFacing(float rotation) {
        return rotation > 90f && rotation < 270f;
    }

    private void UpdateCardTilt() {
        if (!(parentCardGroup?.EnableTilt ?? false) || cardTilter == null || isDragging || movingCard == this || isBeingPlaced || isAttacking || isBeingUsed) return;

        Vector2 targetTilt = isHovering && Mouse.current != null
            ? CalculateHoverTilt()
            : CalculateIdleTilt();

        ApplyTilt(targetTilt);
    }

    private Vector2 CalculateHoverTilt() {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Camera canvasCamera = parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, mouseScreenPos, canvasCamera, out Vector2 localPoint);

        Vector2 halfSize = rectTransform.rect.size * 0.5f;
        float normalizedX = halfSize.x > 0 ? Mathf.Clamp(localPoint.x / halfSize.x, -1f, 1f) : 0f;
        float normalizedY = halfSize.y > 0 ? Mathf.Clamp(localPoint.y / halfSize.y, -1f, 1f) : 0f;

        float direction = parentCardGroup.TiltTowardsCursor ? 1f : -1f;
        return new Vector2(
            normalizedY * parentCardGroup.ManualTiltAmount * direction,
            -normalizedX * parentCardGroup.ManualTiltAmount * direction
        );
    }

    private Vector2 CalculateIdleTilt() {
        int cardIndex = parentCardGroup != null ? parentCardGroup.cards.IndexOf(this) : 0;
        float phase = Time.time + cardIndex;

        return new Vector2(
            Mathf.Sin(phase) * parentCardGroup.AutoTiltAmount,
            Mathf.Cos(phase) * parentCardGroup.AutoTiltAmount
        );
    }

    private void ApplyTilt(Vector2 targetTilt) {
        Vector3 currentEuler = cardTilter.transform.localEulerAngles;
        float lerpX = Mathf.LerpAngle(currentEuler.x, targetTilt.x, parentCardGroup.TiltSpeed * Time.deltaTime);
        float lerpY = Mathf.LerpAngle(currentEuler.y, targetTilt.y, parentCardGroup.TiltSpeed * Time.deltaTime);

        cardTilter.transform.localEulerAngles = new Vector3(lerpX, lerpY, currentEuler.z);
    }

    private void UpdateDragSpring() {
        if (!enableDragSpring || !dragSpringActive || cardDragRotator == null) return;

        dragSpring.Update(Time.deltaTime);

        if (dragSpring.displacement > dragSpringMaxAngle) {
            dragSpring.displacement = dragSpringMaxAngle;
            if (dragSpring.velocity > 0f) dragSpring.velocity = 0f;
        } else if (dragSpring.displacement < -dragSpringMaxAngle) {
            dragSpring.displacement = -dragSpringMaxAngle;
            if (dragSpring.velocity < 0f) dragSpring.velocity = 0f;
        }

        float clamped = Mathf.Clamp(dragSpring.displacement, -dragSpringMaxAngle, dragSpringMaxAngle);
        cardDragRotator.transform.localEulerAngles = new Vector3(0f, 0f, -clamped);

        if (!isDragging && dragSpring.IsAtRest()) {
            dragSpringActive = false;
            cardDragRotator.transform.localEulerAngles = Vector3.zero;
        }
    }

    private void GetTargetingPositions(Vector2 mouseScreenPos, out Vector2 cardLocalPos, out Vector2 mouseLocalPos) {
        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Camera canvasCamera = parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;

        Vector3 cardLocalPos3D = canvasRect.InverseTransformPoint(rectTransform.position);
        cardLocalPos = new Vector2(cardLocalPos3D.x, cardLocalPos3D.y);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, mouseScreenPos, canvasCamera, out mouseLocalPos);
    }
    
    public void ExecuteAttack(Transform target, Action onComplete = null) {
        // Store original parent info for restoration after attack
        Transform originalParent = transform.parent;
        int originalSiblingIndex = transform.GetSiblingIndex();

        // Change parent to AttackingCardParent if available (for proper rendering order)
        if (parentCardGroup != null && parentCardGroup.AttackingCardParent != null) {
            // Preserve world position/rotation/scale when changing parent
            transform.SetParent(parentCardGroup.AttackingCardParent, true);
        }

        // Wrap the completion callback to restore original parent
        Action wrappedComplete = () => {
            // Restore original parent after attack completes
            if (originalParent != null) {
                transform.SetParent(originalParent, true);
                transform.SetSiblingIndex(originalSiblingIndex);
            }

            // Call the original completion callback
            onComplete?.Invoke();
        };

        Action onAttackHitAction = () => {
            Card targetCard = target.GetComponent<Card>();
            cardData?.OnAttackHit(this, targetCard);
            onAttackHit?.Invoke(targetCard);
        };

        AttackAnimationSafe.Attack(this, target, onAttackHitAction, wrappedComplete);
    }

    private void EndTargettedUseTargeting(PointerEventData eventData) {
        if (!isTargetting) return;

        targetedCard?.Untargeted();
        isTargetting = null;
        thisCardTargetting = false;
        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        if (dynamicArrow != null) {
            dynamicArrow.Hide();
        }

        Transform target = FindFirstValidTarget(eventData);
        if (target != null) {
            Unhover();
            Card targetCard = target.GetComponent<Card>();
            ExecuteTargettedUse(targetCard);
        }
        else {
            Unhover();
        }
    }

    public void ExecuteUse(CardGroup dropZoneGroup) {
        isBeingUsed = true;

        // Remove from group immediately so the hand re-fans
        CardGroup sourceGroup = parentCardGroup;
        sourceGroup?.RemoveCard(this);

        // Reparent for proper rendering order during animation
        if (sourceGroup != null && sourceGroup.AttackingCardParent != null) {
            transform.SetParent(sourceGroup.AttackingCardParent, true);
        }

        UseAnimationSafe.Use(this, null, () => {
            isBeingUsed = false;
            cardData?.OnUsed(this, dropZoneGroup);
            onUsed?.Invoke(dropZoneGroup);
            if (cardData == null || cardData.autoDestroyOnUse)
                Destroy(gameObject);
        });
    }

    public void ExecuteTargettedUse(Card target) {
        isBeingUsed = true;

        // Remove from group immediately so the hand re-fans
        CardGroup sourceGroup = parentCardGroup;
        sourceGroup?.RemoveCard(this);

        // Reparent for proper rendering order during animation
        if (sourceGroup != null && sourceGroup.AttackingCardParent != null) {
            transform.SetParent(sourceGroup.AttackingCardParent, true);
        }

        UseAnimationSafe.Use(this, target?.transform, () => {
            isBeingUsed = false;
            cardData?.OnTargetedUse(this, target);
            onTargetedUse?.Invoke(target);
            if (cardData == null || cardData.autoDestroyOnUse)
                Destroy(gameObject);
        });
    }

    public void PlayUseAnimation(CardGroup dropZoneGroup) {
        ExecuteUse(dropZoneGroup);
    }

    public void PlayPlaceAnimation() {
        PlaceAnimationSafe.Place(this, destinationPosition, () => {
            cardData?.OnPlaced(this);
            onPlaced?.Invoke();
        });
    }

    public void SetAttackingState(bool attacking) {
        isAttacking = attacking;
    }
    
    // TODO: remove
    // /// <summary>
    // /// Places card using the selected IPlaceAnimation.
    // /// DEPRECATED: Configure animation in Card Inspector instead.
    // /// This method still works but delegates to the animation system.
    // /// </summary>
    // [Obsolete("Configure animation in Inspector. PlaceWithSwirlAnimation() delegates to selected animation.")]
    // public void PlaceWithSwirlAnimation() {
    //     // Delegate to strategy pattern - maintains backward compatibility
    //     // Pass null for onComplete since old method had no callback support
    //     PlaceAnimationSafe.Place(this, transform.position, null);
    // }
    //
    // /// <summary>
    // /// Executes attack animation toward the specified target.
    // /// </summary>
    // /// <param name="target">Transform of the attack target</param>
    // /// <param name="onComplete">Optional callback when animation completes</param>
    //
    // public void UseWithSwirlAnimation() {
    //     // Rotation animation
    //     DOTween.Sequence()
    //         .Append(cardVisuals.transform.DOBlendableRotateBy(new Vector3(0, 360, 25f), 0.4f, RotateMode.FastBeyond360)
    //             .SetEase(Ease.OutQuad).SetUpdate(true))
    //         .SetUpdate(true);
    //
    //     // Scale animation
    //     DOTween.Sequence()
    //         .Append(cardVisuals.transform.DOScale(originalVisualsScale * 1.25f, 0.3f).SetEase(Ease.OutExpo)
    //             .SetUpdate(true))
    //         .Append(cardVisuals.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad).SetUpdate(true))
    //         .SetUpdate(true);
    //
    //     // Position animation
    //     DOTween.Sequence()
    //         .Append(cardVisuals.transform.DOMove(transform.position + new Vector3(0f, 2f, 0f), 0.3f)
    //             .SetEase(Ease.OutSine))
    //         .Append(cardVisuals.transform.DOMove(new Vector3(0f, 1.2f, 0f), 0.2f).SetEase(Ease.InSine));
    // }
}

}