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
    [Header("Wall gaps by count")]
    [Tooltip("Element 0 = 1 wall, Element 8 = 9 walls. Gap is the space between card edges; negative values overlap.")]
    [SerializeField] float[] gapsByCount = { -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f };
    [SerializeField, Min(0f)] float placementInterval = 0.06f;
    [Header("CCP placement animations")]
    [SerializeField] WallCardPlaceAnimation normalPlacement = new WallCardPlaceAnimation();
    [SerializeField] WallCardPlaceAnimation kingPlacement = new WallCardPlaceAnimation
    {
        enterFromRight = true, duration = 0.5f, travel = 240f,
        lift = 65f, startAngle = -18f, startScale = 0.65f, ease = Ease.OutBack
    };

    CCP.CardGroup group;
    CCP.Card[] slots;


    readonly Queue<Placement> pending = new Queue<Placement>();
    Coroutine placementRoutine;

    struct Placement { public CCP.Card card; public int slot; public bool king; }
    public bool IsAnimating => placementRoutine != null;
    public int Capacity => Mathf.Clamp(slotCount, 1, 9);

    void Awake() { Prepare(); }
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
        // A script reload discards this runtime slot array while scene cards remain.
        // Recover their registrations before reserving a slot for the next King.
        foreach (Transform child in transform)
        {
            if (child.GetComponent<CardController>() == null || !child.gameObject.activeSelf) continue;
            CanvasGroup canvasGroup = child.GetComponent<CanvasGroup>();
            if (canvasGroup != null && canvasGroup.alpha <= 0.01f) continue;
            int index = FindFreeSlot(false);
            if (index < 0) break;
            CCP.Card card = child.GetComponent<CCP.Card>();
            if (card == null) card = child.gameObject.AddComponent<CCP.Card>();
            card.enabled = false;
            card.transform.DOKill();
            card.SetCardGroup(group);
            card.isBeingMovedManually = false;
            slots[index] = card;
            group.cards.Add(card);
            LayoutElement element = child.GetComponent<LayoutElement>();
            if (element != null) element.ignoreLayout = true;
        }
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
        int count = 0;
        int order = 0;
        for(int i = 0; i < slots.Length; i++)
        {
            if(slots[i] == null) continue;
            if(i < index) order++;
            count++;
        }
        float gap = gapsByCount != null && count > 0 && count <= gapsByCount.Length
            ? gapsByCount[count - 1] : overlapLimit - cardSize.x;
        float spacing = Mathf.Max(1f, cardSize.x + gap);
        if(count > 1)
            spacing = Mathf.Min(spacing, Mathf.Max(1f, maxWidth - cardSize.x) / (count - 1));
        return new Vector3(layoutCenter.x + (order - (count - 1) * 0.5f) * spacing, layoutCenter.y, 0f);
    }
    public bool AddWall(GameObject wall, bool fromKing, Vector2 size)
    {
        Prepare();
        ReleaseBrokenWalls();
        int index = FindFreeSlot(false);
        if (wall == null || index < 0) return false;

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

            animation.Place(item.card, target, () => finished = true);
            while (!finished && item.card != null && slots[item.slot] == item.card) yield return null;
            if(item.card != null && slots[item.slot] == item.card)
            {
                item.card.isBeingMovedManually = false;
                item.card.transform.localPosition = SlotPosition(item.slot);
            }
            LayoutWalls();
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
    public void LayoutWalls()
    {
        Prepare();
        ReleaseBrokenWalls();
        for(int i = 0; i < slots.Length; i++)
        {
            CCP.Card card = slots[i];
            if(card == null || card.isBeingMovedManually) continue;
            card.transform.localPosition = SlotPosition(i);
            card.transform.localRotation = Quaternion.identity;
            card.transform.localScale = Vector3.one;
        }
    }

    void OnValidate()
    {
        maxWidth = Mathf.Max(1f, maxWidth);
        slotCount = Mathf.Clamp(slotCount, 1, 9);
        if(gapsByCount == null) gapsByCount = new float[9];
        if(gapsByCount.Length != 9) Array.Resize(ref gapsByCount, 9);
    }
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
                slots[i].isBeingMovedManually = false;
                slots[i].transform.localPosition = SlotPosition(i);
                slots[i].transform.localRotation = Quaternion.identity;
                slots[i].transform.localScale = Vector3.one;
            }

    }

}

[Serializable]
public class WallCardPlaceAnimation : CCP.IPlaceAnimation
{
    public bool enterFromRight;
    [Min(0.01f)] public float duration = 0.25f;
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
