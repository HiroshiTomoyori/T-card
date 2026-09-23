using System;
using System.Collections.Generic;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;
using UnityEngine.Events;

namespace CCP
{

public class CardGroup : MonoBehaviour {
    // Runtime state
    [HideInInspector] public List<Card> cards = new List<Card>();
    private int hoveredCardIndex = -1;
    private List<Card> selectedCards = new List<Card>();

    [Header("Layout Settings")]
    [SerializeField] private float cardSpacing = 1f;
    [SerializeField] private float fanRadius = 0f;
    [SerializeField] private float yOffsetPerCard = 0f;
    [SerializeField] private float zOffsetPerCard = 0f;
    [SerializeField] private float cardScale = 1f;
    [Tooltip("When true, card positions start from the first card (index 0) instead of centering around the middle. Useful for deck stacking.")]
    [SerializeField] private bool anchorToFirstCard = false;

    [Header("Card Movement Settings")]
    [Tooltip("Controls how cards animate when moving to their positions")]
    [SerializeField] private Ease tweenEaseType = Ease.OutQuad;
    [SerializeField] private float tweenDuration = 0.3f;

    [Header("Hover Settings")]
    [SerializeField] private float hoverYOffset = 0.5f;
    [SerializeField] private float hoverZOffset = 0.2f;
    [SerializeField] private bool makeRoomOnHover = true;
    [SerializeField] private float hoverSpacing = 0.5f;
    [SerializeReference, SubclassSelector]
    public IHoverAnimation hoverAnimation = new ScaleUpHoverAnimation();

    [Header("On Click Settings")]
    public OnClickAction onClickAction = OnClickAction.Nothing;
    [ShowIf("onClickAction", (int)OnClickAction.Custom)]
    public UnityEvent<Card> onClickCustom;

    [Header("Selection Settings")]
    [SerializeReference, SubclassSelector]
    public ISelectAnimation selectAnimation = new LiftSelectAnimation();
    [SerializeField] public bool showSelectionMark = false;

    [Header("Drag & Pickup Settings")]
    [SerializeField] public bool allowPickup = true;
    [SerializeField] public bool resetRotationOnPickup = false;
    [SerializeField] public bool allowReorderingDuringDrag = true;
    [SerializeField] public float reorderYThreshold = 200f;

    [Header("Targeting Settings")]
    public bool isTargettable = true;
    public bool isPlayerHand = false;

    [Header("Attack Rendering")]
    public Transform AttackingCardParent;

    [Header("Tilt Settings")]
    [SerializeField] private bool enableTilt = true;
    [SerializeField] private bool tiltTowardsCursor = true;
    [SerializeField] private float autoTiltAmount = 5f;
    [SerializeField] private float manualTiltAmount = 15f;
    [SerializeField] private float tiltSpeed = 40f;

    // Properties
    public bool EnableTilt => enableTilt;
    public bool TiltTowardsCursor => tiltTowardsCursor;
    public float AutoTiltAmount => autoTiltAmount;
    public float ManualTiltAmount => manualTiltAmount;
    public float TiltSpeed => tiltSpeed;
    public float GetCardScale() => cardScale;
    public IHoverAnimation HoverAnimationSafe => hoverAnimation ?? new ScaleUpHoverAnimation();
    public ISelectAnimation SelectAnimationSafe => selectAnimation ?? new LiftSelectAnimation();

    // Selection helpers
    public List<Card> GetSelectedCards() => new List<Card>(selectedCards);
    public List<Card> GetNotSelectedCards() => cards.FindAll(c => !selectedCards.Contains(c));

    internal void SelectCard(Card card) {
        if (!selectedCards.Contains(card))
            selectedCards.Add(card);
    }

    internal void DeselectCard(Card card) {
        selectedCards.Remove(card);
    }

    internal bool IsSelected(Card card) => selectedCards.Contains(card);
    
    private void Start() {
        InvokeRepeating(nameof(RecalculateCardOrder), 0.05f, 0.05f);
    }

    private void OnDestroy() {
        CancelInvoke(nameof(RecalculateCardOrder));
    }

    private void OnValidate() {
        if (hoverAnimation == null) hoverAnimation = new ScaleUpHoverAnimation();
        if (selectAnimation == null) selectAnimation = new LiftSelectAnimation();

        // Recalculate positions when values change in Inspector (both Edit and Play mode)
        if (Application.isPlaying && cards != null && cards.Count > 0) {
            RecalculateAndAnimatePositions();
        }
    }

    private void RecalculateCardOrder() {
        if (cards == null || cards.Count <= 1) return;

        // Create a sorted list based on Z position (higher Z = render on top)
        List<Card> sortedCards = new List<Card>(cards);
        sortedCards.Sort((a, b) => -a.transform.localPosition.z.CompareTo(b.transform.localPosition.z));

        // Check if order has changed
        bool orderChanged = false;
        for (int i = 0; i < sortedCards.Count; i++) {
            if (sortedCards[i].transform.GetSiblingIndex() != i) {
                orderChanged = true;
                break;
            }
        }

        // Reorder in hierarchy if changed
        if (orderChanged) {
            for (int i = 0; i < sortedCards.Count; i++) {
                sortedCards[i].transform.SetSiblingIndex(i);
            }
        }
    }

    // Add/Remove methods
    public void AddFront(Card card, IPlaceAnimation placeAnimation = null) {
        if (card == null) return;

        cards.Insert(0, card);
        card.transform.SetParent(transform);
        card.transform.localScale = Vector3.one * cardScale;
        card.SetCardGroup(this);
        RecalculateAndAnimatePositions();
        if (placeAnimation != null) {
            card.transform.DOKill();
            placeAnimation.Place(card, card.destinationPosition, null);
        }
    }

    public void AddBack(Card card, IPlaceAnimation placeAnimation = null) {
        if (card == null) return;

        cards.Add(card);
        card.transform.SetParent(transform);
        card.transform.localScale = Vector3.one * cardScale;
        card.SetCardGroup(this);
        RecalculateAndAnimatePositions();
        if (placeAnimation != null) {
            card.transform.DOKill();
            placeAnimation.Place(card, card.destinationPosition, null);
        }
    }

    public void AddAtPosition(Card card, int index, IPlaceAnimation placeAnimation = null) {
        if (card == null) return;
        if (index < 0 || index > cards.Count) {
            Debug.LogWarning($"Index {index} out of bounds. Card count: {cards.Count}");
            return;
        }

        cards.Insert(index, card);
        card.transform.SetParent(transform);
        card.transform.localScale = Vector3.one * cardScale;
        card.SetCardGroup(this);
        RecalculateAndAnimatePositions();
        if (placeAnimation != null) {
            card.transform.DOKill();
            placeAnimation.Place(card, card.destinationPosition, null);
        }
    }

    public Card Front() {
        if (cards.Count == 0) return null;
        return cards[0];
    }

    public Card Back() {
        if (cards.Count == 0) return null;
        return cards[cards.Count - 1];
    }

    public Card RemoveFront(IHideAnimation hideAnimation = null, Action<Card> onComplete = null) {
        if (cards.Count == 0) return null;

        Card card = cards[0];
        cards.RemoveAt(0);
        card.SetCardGroup(null);
        selectedCards.Remove(card);

        // Clear hover if we removed the hovered card
        if (hoveredCardIndex == 0) {
            hoveredCardIndex = -1;
        }
        else if (hoveredCardIndex > 0) {
            hoveredCardIndex--;
        }

        RecalculateAndAnimatePositions();

        if (hideAnimation != null) {
            hideAnimation.Hide(card, () => onComplete?.Invoke(card));
        } else {
            onComplete?.Invoke(card);
        }

        return card;
    }

    public Card RemoveBack(IHideAnimation hideAnimation = null, Action<Card> onComplete = null) {
        if (cards.Count == 0) return null;

        int lastIndex = cards.Count - 1;
        Card card = cards[lastIndex];
        cards.RemoveAt(lastIndex);
        card.SetCardGroup(null);
        selectedCards.Remove(card);

        // Clear hover if we removed the hovered card
        if (hoveredCardIndex == lastIndex) {
            hoveredCardIndex = -1;
        }

        RecalculateAndAnimatePositions();

        if (hideAnimation != null) {
            hideAnimation.Hide(card, () => onComplete?.Invoke(card));
        } else {
            onComplete?.Invoke(card);
        }

        return card;
    }

    public Card RemoveAtPosition(int index, IHideAnimation hideAnimation = null, Action<Card> onComplete = null) {
        if (index < 0 || index >= cards.Count) {
            Debug.LogWarning($"Index {index} out of bounds. Card count: {cards.Count}");
            return null;
        }

        Card card = cards[index];
        cards.RemoveAt(index);
        card.SetCardGroup(null);
        selectedCards.Remove(card);

        // Adjust or clear hover index
        if (hoveredCardIndex == index) {
            hoveredCardIndex = -1;
        }
        else if (hoveredCardIndex > index) {
            hoveredCardIndex--;
        }

        RecalculateAndAnimatePositions();

        if (hideAnimation != null) {
            hideAnimation.Hide(card, () => onComplete?.Invoke(card));
        } else {
            onComplete?.Invoke(card);
        }

        return card;
    }

    public void RemoveCard(Card card, IHideAnimation hideAnimation = null, Action<Card> onComplete = null) {
        if (cards == null) return;
        int index = cards.IndexOf(card);
        if (index == -1) return; // Card not in list
        RemoveAtPosition(index, hideAnimation, onComplete);
    }
    
    public void Shuffle() {
        for (int i = cards.Count - 1; i > 0; i--) {
            int j = UnityEngine.Random.Range(0, i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
        RecalculateAndAnimatePositions();
    }

    // Hover callback methods (called by Card)
    public void OnCardHovered(Card card) {
        int index = cards.IndexOf(card);
        if (index == -1) return; // Card not in list
        if (index == hoveredCardIndex) return; // Already hovered

        hoveredCardIndex = index;
        RecalculateAndAnimatePositions();
    }

    public void OnCardUnhovered(Card card) {
        int index = cards.IndexOf(card);
        if (index == -1) return; // Card not in list
        if (index != hoveredCardIndex) return; // Not the currently hovered card

        hoveredCardIndex = -1;
        RecalculateAndAnimatePositions();
    }

    public void RecalculateAndAnimatePositions(Card skipAnimating = null) {
        int n = cards.Count;
        if (n == 0) return;

        float centerIndex = anchorToFirstCard ? 0f : (n - 1) / 2f;

        for (int i = 0; i < n; i++) {
            float offsetFromCenter = i - centerIndex;

            Vector3 localPos;
            Quaternion localRot;

            if (Mathf.Approximately(fanRadius, 0f)) {
                // Straight line layout
                float x = offsetFromCenter * cardSpacing;
                float y = offsetFromCenter * yOffsetPerCard;
                float z = offsetFromCenter * zOffsetPerCard;

                localPos = new Vector3(x, y, z);
                localRot = Quaternion.identity;
            }
            else {
                // Arc/fan layout (for UI Canvas)
                float angle = (offsetFromCenter * cardSpacing) / fanRadius;

                float x = fanRadius * Mathf.Sin(angle);
                // For UI, arc curvature should be on Y axis (vertical)
                // Positive radius = curves downward, negative radius = curves upward
                float y = fanRadius * (Mathf.Cos(angle) - 1);

                // Add stacking offsets
                y += offsetFromCenter * yOffsetPerCard;
                float z = offsetFromCenter * zOffsetPerCard;

                localPos = new Vector3(x, y, z);
                // For UI cards, rotate around Z axis (perpendicular to canvas)
                localRot = Quaternion.Euler(0, 0, -angle * Mathf.Rad2Deg);
            }

            // Apply hover adjustments (skip if this is the dragged card)
            Vector3 clickTargetHoverOffset = Vector3.zero;
            if (i == hoveredCardIndex && cards[i] != skipAnimating) {
                localPos.y += hoverYOffset;
                localPos.z += hoverZOffset;
                clickTargetHoverOffset = transform.TransformDirection(new Vector3(0f, hoverYOffset, 0f));
            }

            cards[i].SetClickTargetHoverExtension(clickTargetHoverOffset);

            // Apply "make room" effect (skip if this is the dragged card)
            if (makeRoomOnHover && hoveredCardIndex >= 0 && i != hoveredCardIndex && cards[i] != skipAnimating) {
                if (i < hoveredCardIndex) {
                    // Card is to the left of hovered card
                    localPos.x -= hoverSpacing;
                }
                else {
                    // Card is to the right of hovered card
                    localPos.x += hoverSpacing;
                }
            }

            // Convert to world space
            Vector3 worldPos = transform.position + transform.TransformDirection(localPos);
            Quaternion worldRot = transform.rotation * localRot;

            // Skip animating the dragged card - it's being manually positioned
            if (cards[i] == skipAnimating) continue;

            // Animate the card with faster duration if reordering during drag
            float duration = skipAnimating != null ? tweenDuration * 0.7f : tweenDuration;
            cards[i].MoveToPosition(worldPos, worldRot, tweenEaseType, duration, cardScale);
        }
    }

    public void ReorderCardDuringDrag(Card card, int newIndex) {
        if (card == null || newIndex < 0 || newIndex >= cards.Count)
            return;

        int currentIndex = cards.IndexOf(card);
        if (currentIndex == -1) return;
        if (currentIndex == newIndex) return;

        // Remove from current position and insert at new position
        cards.RemoveAt(currentIndex);
        cards.Insert(newIndex, card);

        // Recalculate positions, but skip animating the dragged card
        RecalculateAndAnimatePositions(card);
    }
}

}