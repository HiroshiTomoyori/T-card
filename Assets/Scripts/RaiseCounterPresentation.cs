using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RaiseCounterPresentation : MonoBehaviour
{
    const string AssetPath = "TCardField/";
    const int FrameCount = 36;
    const int AtlasColumns = 8;
    const int AtlasRows = 5;
    const float TileSize = 128f;

    [SerializeField, Min(4)] float diamondSize = 28f;
    [SerializeField, Min(0)] float rightGap = 10f;
    [SerializeField, Min(0)] float verticalGap = 10f;
    [SerializeField, Min(.1f)] float extinguishDuration = .6f;

    sealed class Side
    {
        internal RectTransform Root;
        internal readonly RawImage[] Marks = new RawImage[HandDealer.RaiseLimit];
        internal readonly float[] SpendStarted = new float[HandDealer.RaiseLimit];
        internal int LastRemaining = -1;
    }

    readonly Side player = new Side();
    readonly Side opponent = new Side();
    HandDealer dealer;
    Texture2D spendAtlas;
    Texture2D emptyFrame;

    internal void Initialize(HandDealer owner)
    {
        dealer = owner;
        spendAtlas = Resources.Load<Texture2D>(AssetPath + "RaiseSpend");
        emptyFrame = Resources.Load<Texture2D>(AssetPath + "RaiseFrame");
    }

    void LateUpdate()
    {
        if (dealer == null || spendAtlas == null || emptyFrame == null) return;
        bool show = !HandDealer.IsRedrawSelecting && (dealer.turnManager == null || !dealer.turnManager.IsBattleEnded);
        UpdateSide(player, dealer.deckPosition, dealer.PlayerRaisesRemaining, show);
        UpdateSide(opponent, dealer.enemyDeckPosition, dealer.EnemyRaisesRemaining, show);
    }

    void UpdateSide(Side side, RectTransform deck, int remaining, bool visible)
    {
        if (deck == null) return;
        if (side.Root == null)
        {
            if (!visible) return;
            var deckCanvas = deck.GetComponentInParent<Canvas>(true);
            if (deckCanvas == null) return;
            var root = new GameObject("Raise charges", typeof(RectTransform));
            side.Root = (RectTransform)root.transform;
            PlaceBehindHands(side.Root, deckCanvas.rootCanvas.transform);
            side.Root.anchorMin = side.Root.anchorMax = new Vector2(.5f, .5f);
            side.Root.sizeDelta = Vector2.zero;
            for (int i = 0; i < side.Marks.Length; i++)
            {
                var icon = new GameObject("Raise diamond " + (i + 1), typeof(RectTransform), typeof(RawImage));
                icon.transform.SetParent(side.Root, false);
                var mark = icon.GetComponent<RawImage>();
                mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(.5f, .5f);
                mark.raycastTarget = false;
                side.Marks[i] = mark;
            }
        }

        side.Root.gameObject.SetActive(visible);
        if (!visible)
        {
            side.LastRemaining = -1;
            return;
        }

        bool enemy = side == opponent;
        var canvas = side.Root.GetComponentInParent<Canvas>().rootCanvas;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var group = (RectTransform)canvas.transform;
        var deckArt = enemy && dealer.enemyDeckImage != null ? dealer.enemyDeckImage.rectTransform : deck;
        var view = OpeningShuffleView.ForDeck(deck);
        Vector3 center = view != null ? view.TopPosition : deckArt.TransformPoint(deckArt.rect.center);
        Vector2 localCenter = group.InverseTransformPoint(center);
        float halfDeck = Mathf.Abs(group.InverseTransformVector(deckArt.TransformVector(Vector3.right * deckArt.rect.width * .5f)).x);
        float offset = (halfDeck + rightGap + diamondSize * .5f) * (enemy ? -1f : 1f);
        Vector2 markCenter = localCenter + Vector2.right * offset;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, group.TransformPoint(markCenter));
        float screenHalfWidth = Mathf.Abs(RectTransformUtility.WorldToScreenPoint(camera,
            group.TransformPoint(markCenter + Vector2.right * diamondSize * .5f)).x - screen.x);
        screen.x = Mathf.Clamp(screen.x, Screen.safeArea.xMin + screenHalfWidth + 2f,
            Screen.safeArea.xMax - screenHalfWidth - 2f);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(group, screen, camera, out Vector2 location))
            side.Root.position = group.TransformPoint(location);

        float now = Time.unscaledTime;
        if (side.LastRemaining < 0 || remaining > side.LastRemaining)
        {
            for (int i = 0; i < side.SpendStarted.Length; i++) side.SpendStarted[i] = float.NegativeInfinity;
        }
        else
        {
            for (int i = remaining; i < side.LastRemaining; i++) side.SpendStarted[i] = now;
        }
        side.LastRemaining = remaining;

        for (int i = 0; i < side.Marks.Length; i++)
        {
            var mark = side.Marks[i];
            mark.rectTransform.sizeDelta = Vector2.one * (diamondSize * 256f / 184f);
            mark.rectTransform.anchoredPosition = Vector2.up * ((.5f - i) * (diamondSize + verticalGap));
            float progress = (now - side.SpendStarted[i]) / Mathf.Max(.1f, extinguishDuration);
            if (i >= remaining && progress >= 1f)
            {
                mark.texture = emptyFrame;
                mark.uvRect = new Rect(0f, 0f, 1f, 1f);
                continue;
            }

            int frame = i < remaining ? 0 : Mathf.Clamp((int)(progress * FrameCount), 0, FrameCount - 1);
            mark.texture = spendAtlas;
            float inset = .5f / TileSize;
            mark.uvRect = new Rect((frame % AtlasColumns + inset) / AtlasColumns,
                (AtlasRows - 1 - frame / AtlasColumns + inset) / AtlasRows,
                (1f - 2f * inset) / AtlasColumns, (1f - 2f * inset) / AtlasRows);
        }
    }

    void PlaceBehindHands(RectTransform counter, Transform canvasRoot)
    {
        Transform playerHand = dealer.handArea;
        Transform enemyHand = dealer.enemyHandArea;
        Transform hand = playerHand != null ? playerHand : enemyHand;
        Transform parent = canvasRoot;
        if (hand != null && hand.IsChildOf(canvasRoot))
        {
            parent = hand.parent;
            // Both hand branches draw above the counters under their common ancestor.
            while (parent != canvasRoot && enemyHand != null && !enemyHand.IsChildOf(parent))
                parent = parent.parent;
        }

        // Preserve canvas units when the game screen scales its children.
        counter.SetParent(canvasRoot, false);
        if (parent != canvasRoot) counter.SetParent(parent, true);
        foreach (Transform handArea in new[] { playerHand, enemyHand })
        {
            if (handArea == null || handArea == parent || !handArea.IsChildOf(parent)) continue;
            Transform branch = handArea;
            while (branch.parent != parent) branch = branch.parent;
            int handIndex = branch.GetSiblingIndex();
            if (counter.GetSiblingIndex() > handIndex) counter.SetSiblingIndex(handIndex);
        }
    }

    void OnDisable()
    {
        if (player.Root != null) player.Root.gameObject.SetActive(false);
        if (opponent.Root != null) opponent.Root.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (player.Root != null) Destroy(player.Root.gameObject);
        if (opponent.Root != null) Destroy(opponent.Root.gameObject);
    }
}
