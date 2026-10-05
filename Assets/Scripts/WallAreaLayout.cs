using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class WallAreaLayout : MonoBehaviour
{
    [SerializeField] float maxWidth = 500f;
    [SerializeField] float overlapLimit = 60f;
    [Header("Wall slots / CCP")]
    [SerializeField, Range(1, 9)] int slotCount = 9;
    [SerializeField] Vector2 cardSize = new Vector2(70f, 105f);
    [SerializeField] Vector2 layoutCenter;
    [SerializeField, Min(0f)] float placementInterval = 0.12f;
    [Header("Slot frames")]
    [SerializeField] bool showFrames = true;
    [SerializeField] Vector2 framePadding = new Vector2(8f, 8f);
    [SerializeField, Min(1f)] float frameThickness = 2f;
    [SerializeField] Color frameColor = new Color(0.8f, 0.72f, 0.5f, 0.28f);
    [SerializeField] Color kingFrameColor = new Color(1f, 0.78f, 0.24f, 1f);
    [Header("CCP placement animations")]
    [SerializeField] WallCardPlaceAnimation normalPlacement = new WallCardPlaceAnimation();
    [SerializeField] WallCardPlaceAnimation kingPlacement = new WallCardPlaceAnimation
    {
        enterFromRight = true, duration = 0.65f, travel = 240f,
        lift = 65f, startAngle = -18f, startScale = 0.65f, ease = Ease.OutBack
    };

    CCP.CardGroup group;
    CCP.Card[] slots;
    RectTransform frameRoot;
    readonly List<Image[]> frames = new List<Image[]>();
    readonly Queue<Placement> pending = new Queue<Placement>();
    Coroutine placementRoutine;
    bool framesVisible = true;
    struct Placement { public CCP.Card card; public int slot; public bool king; }
    public bool IsAnimating => placementRoutine != null;
    public int Capacity => Mathf.Clamp(slotCount, 1, 9);

    void Awake() { Prepare(); }
    void OnEnable() { if (frameRoot != null) frameRoot.gameObject.SetActive(showFrames && framesVisible); }
    void Prepare()
    {
        if (slots != null) return;
        slots = new CCP.Card[Capacity];
        group = GetComponent<CCP.CardGroup>();
        if (group == null) group = gameObject.AddComponent<CCP.CardGroup>();
        // Keep fixed slots: CCP's automatic fan layout/order must not compact them.
        group.enabled = false;
        group.CancelInvoke();
        group.allowPickup = false;
        group.isPlayerHand = false;
        group.cards = new List<CCP.Card>();
        foreach (LayoutGroup legacy in GetComponents<LayoutGroup>()) legacy.enabled = false;
        CreateFrames();
    }
    public bool HasSpace
    {
        get { Prepare(); ReleaseBrokenWalls(); return FindFreeSlot(false) >= 0; }
    }
    int FindFreeSlot(bool fromRight)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int index = fromRight ? slots.Length - 1 - i : i;
            if (slots[index] == null) return index;
        }
        return -1;
    }
    Vector3 SlotPosition(int index)
    {
        float spacing = slots.Length > 1
            ? Mathf.Min(Mathf.Max(0f, overlapLimit), Mathf.Max(0f, maxWidth) / (slots.Length - 1)) : 0f;
        return new Vector3(layoutCenter.x + (index - (slots.Length - 1) * 0.5f) * spacing, layoutCenter.y, 0f);
    }
    public bool AddWall(GameObject wall, bool fromKing, Vector2 size)
    {
        Prepare();
        ReleaseBrokenWalls();
        int index = FindFreeSlot(fromKing);
        if (wall == null || index < 0) return false;
        framesVisible = true;
        cardSize = size;
        wall.transform.SetParent(transform, false);
        CCP.Card card = wall.GetComponent<CCP.Card>();
        if (card == null) card = wall.AddComponent<CCP.Card>();
        card.enabled = false; // Target selection remains with EnemyWallClick.
        card.transform.DOKill();
        if (card.parentCardGroup != null) card.parentCardGroup.cards.Remove(card);
        card.SetCardGroup(group);
        group.cards.Add(card);
        card.isBeingMovedManually = true;
        slots[index] = card; // Reserve immediately, including cards queued for later.
        LayoutElement element = wall.GetComponent<LayoutElement>();
        if (element != null) element.ignoreLayout = true;
        card.transform.localPosition = SlotPosition(index);
        card.transform.localRotation = Quaternion.identity;
        card.transform.localScale = Vector3.zero;
        pending.Enqueue(new Placement { card = card, slot = index, king = fromKing });
        if (placementRoutine == null) placementRoutine = StartCoroutine(PlayPlacements());
        return true;
    }
    IEnumerator PlayPlacements()
    {
        yield return null; // Assign coroutine handle before completion is possible.
        while (pending.Count > 0)
        {
            Placement item = pending.Dequeue();
            if (item.card == null || slots[item.slot] != item.card) continue;
            Vector3 target = transform.TransformPoint(SlotPosition(item.slot));
            item.card.destinationPosition = target;
            item.card.destinationRotation = transform.rotation;
            bool finished = false;
            CCP.IPlaceAnimation animation = item.king ? kingPlacement : normalPlacement;
            if (item.king) PulseFrame(item.slot);
            animation.Place(item.card, target, () => finished = true);
            while (!finished && item.card != null && slots[item.slot] == item.card) yield return null;
            float elapsed = 0f;
            while (elapsed < placementInterval)
            {
                elapsed += CCP.CardControllerSettings.Instance.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }
        placementRoutine = null;
    }
    void LateUpdate() { LayoutWalls(); }
    public void LayoutWalls() { Prepare(); ReleaseBrokenWalls(); UpdateFrames(); }
    void ReleaseBrokenWalls()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            CCP.Card card = slots[i];
            if (card == null) { slots[i] = null; continue; }
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (card.transform.parent == transform && card.gameObject.activeSelf && (cg == null || cg.alpha > 0.01f)) continue;
            card.transform.DOKill();
            group.cards.Remove(card);
            if (card.parentCardGroup == group) card.SetCardGroup(null);
            card.isBeingMovedManually = false;
            slots[i] = null;
        }
        group.cards.RemoveAll(card => card == null);
    }
    void CreateFrames()
    {
        // Sibling, not child: combat code counts direct children as walls.
        var root = new GameObject(name + "_WallSlotFrames", typeof(RectTransform));
        root.layer = gameObject.layer;
        frameRoot = root.GetComponent<RectTransform>();
        frameRoot.SetParent(transform.parent, false);
        for (int i = 0; i < slots.Length; i++)
        {
            var edges = new Image[4];
            for (int edge = 0; edge < 4; edge++)
            {
                var obj = new GameObject("Slot_" + (i + 1) + "_Edge_" + edge, typeof(RectTransform), typeof(Image));
                obj.layer = gameObject.layer;
                obj.transform.SetParent(frameRoot, false);
                edges[edge] = obj.GetComponent<Image>();
                edges[edge].raycastTarget = false;
                edges[edge].color = frameColor;
            }
            frames.Add(edges);
        }
        UpdateFrames();
    }
    void UpdateFrames()
    {
        if (frameRoot == null) return;
        frameRoot.gameObject.SetActive(showFrames && framesVisible);
        RectTransform source = transform as RectTransform;
        if (source != null)
        {
            frameRoot.anchorMin = source.anchorMin;
            frameRoot.anchorMax = source.anchorMax;
            frameRoot.pivot = source.pivot;
            frameRoot.sizeDelta = source.sizeDelta;
            frameRoot.anchoredPosition3D = source.anchoredPosition3D;
        }
        frameRoot.localRotation = transform.localRotation;
        frameRoot.localScale = transform.localScale;
        int zoneIndex = transform.GetSiblingIndex();
        if (frameRoot.GetSiblingIndex() != zoneIndex - 1)
            frameRoot.SetSiblingIndex(frameRoot.GetSiblingIndex() < zoneIndex ? zoneIndex - 1 : zoneIndex);
        Vector2 size = cardSize + framePadding * 2f;
        for (int i = 0; i < frames.Count; i++)
            for (int edge = 0; edge < 4; edge++)
            {
                RectTransform rt = frames[i][edge].rectTransform;
                bool horizontal = edge < 2;
                rt.sizeDelta = horizontal ? new Vector2(size.x, frameThickness) : new Vector2(frameThickness, size.y);
                rt.anchoredPosition = (Vector2)SlotPosition(i) + (horizontal
                    ? new Vector2(0f, (edge == 0 ? 1f : -1f) * size.y * 0.5f)
                    : new Vector2((edge == 2 ? -1f : 1f) * size.x * 0.5f, 0f));
            }
    }
    void PulseFrame(int index)
    {
        foreach (Image edge in frames[index])
        {
            edge.DOKill();
            edge.color = kingFrameColor;
            edge.DOColor(frameColor, Mathf.Max(0.1f, kingPlacement.duration + 0.25f))
                .SetEase(Ease.InQuad).SetUpdate(CCP.CardControllerSettings.Instance.useUnscaledTime);
        }
    }
    public void ClearWalls(bool keepFrames = true)
    {
        Prepare();
        StopAllCoroutines();
        placementRoutine = null;
        pending.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child.GetComponent<CardController>() == null) continue;
            child.transform.DOKill();
            CCP.Card card = child.GetComponent<CCP.Card>();
            if (card != null) { card.SetCardGroup(null); card.isBeingMovedManually = false; }
            child.SetActive(false);
            child.transform.SetParent(null, false);
            Destroy(child);
        }
        Array.Clear(slots, 0, slots.Length);
        group.cards.Clear();
        framesVisible = keepFrames;
        foreach (Image[] edges in frames)
            foreach (Image edge in edges) { edge.DOKill(); edge.color = frameColor; }
        UpdateFrames();
    }
    void OnDisable()
    {
        StopAllCoroutines();
        placementRoutine = null;
        pending.Clear();
        if (slots != null)
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                slots[i].transform.DOKill();
                slots[i].transform.localPosition = SlotPosition(i);
                slots[i].transform.localRotation = Quaternion.identity;
                slots[i].transform.localScale = Vector3.one;
            }
        if (frameRoot != null) frameRoot.gameObject.SetActive(false);
    }
    void OnDestroy()
    {
        foreach (Image[] edges in frames)
            foreach (Image edge in edges) if (edge != null) edge.DOKill();
        if (frameRoot != null) Destroy(frameRoot.gameObject);
    }
}

[Serializable]
public class WallCardPlaceAnimation : CCP.IPlaceAnimation
{
    public bool enterFromRight;
    [Min(0.01f)] public float duration = 0.35f;
    [Min(0f)] public float travel = 180f;
    public float lift = 12f;
    public float startAngle = 8f;
    [Min(0.01f)] public float startScale = 0.85f;
    public Ease ease = Ease.OutCubic;
    public void Place(CCP.Card card, Vector3 target, Action onComplete)
    {
        if (card == null) { onComplete?.Invoke(); return; }
        Transform parent = card.transform.parent;
        card.transform.DOKill();
        Vector3 offset = new Vector3(enterFromRight ? travel : -travel, lift, 0f);
        card.transform.position = target + (parent != null ? parent.TransformVector(offset) : offset);
        card.transform.rotation = card.destinationRotation * Quaternion.Euler(0f, 0f, startAngle);
        card.transform.localScale = Vector3.one * startScale;
        bool called = false;
        Action complete = () => { if (called) return; called = true; onComplete?.Invoke(); };
        Sequence sequence = DOTween.Sequence().SetTarget(card.transform)
            .SetUpdate(CCP.CardControllerSettings.Instance.useUnscaledTime);
        float time = Mathf.Max(0.01f, duration);
        sequence.Join(card.transform.DOMove(target, time).SetEase(ease));
        sequence.Join(card.transform.DORotateQuaternion(card.destinationRotation, time).SetEase(Ease.OutCubic));
        sequence.Join(card.transform.DOScale(Vector3.one, time).SetEase(ease));
        sequence.OnComplete(() => { card.onPlaced?.Invoke(); complete(); });
        sequence.OnKill(() => complete());
    }
}
