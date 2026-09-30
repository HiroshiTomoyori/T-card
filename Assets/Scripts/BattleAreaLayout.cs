using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CCP.CardGroup))]
public class BattleAreaLayout : MonoBehaviour
{
    [Serializable]
    public class LayoutSetting
    {
        [Tooltip("表示するカード画像の横幅")]
        public float cardWidth;

        [Tooltip("カード画像同士の間隔。負数なら重なります")]
        public float gap;

        public LayoutSetting(float width, float spacing)
        {
            cardWidth = width;
            gap = spacing;
        }
    }

    [Header("フィールド")]
    [Range(1, 7)]
    public int maxCards = 7;

    [Min(1f)]
    public float fieldWidth = 900f;

    public Vector2 layoutCenter = Vector2.zero;

    [Header("カード自体のサイズ")]
    [Tooltip("1 = 基準サイズ、0.8 = 80%、1.2 = 120%。配置間隔は変わりません")]
    [Min(0.01f)]
    public float cardSize = 1f;

    [Header("枚数ごとのサイズ・間隔")]
    [Tooltip("Element 0 = 1枚、Element 6 = 7枚")]
    public LayoutSetting[] layouts =
    {
        new LayoutSetting(190f, 30f),
        new LayoutSetting(190f, 30f),
        new LayoutSetting(180f, 25f),
        new LayoutSetting(165f, 20f),
        new LayoutSetting(150f, 15f),
        new LayoutSetting(135f, 12f),
        new LayoutSetting(120f, 10f)
    };

    [Header("アニメーション")]
    [Min(0.01f)]
    public float animationDuration = 0.25f;

    public Ease animationEase = Ease.OutCubic;

    private CCP.CardGroup group;

    private readonly List<CardController> fieldCards =
        new List<CardController>();

    private readonly List<CardController> previousCards =
        new List<CardController>();

    private readonly List<bool> previousTapped =
        new List<bool>();

    private readonly List<bool> previousSnapping =
        new List<bool>();

    private bool dirty = true;

    public int CardCount
    {
        get
        {
            CollectCards();
            return fieldCards.Count;
        }
    }

    public bool IsFull
    {
        get { return CardCount >= maxCards; }
    }

    private void Awake()
    {
        PrepareGroup();
    }

    private void OnEnable()
    {
        dirty = true;
    }

    private void OnValidate()
    {
        maxCards = Mathf.Clamp(maxCards, 1, 7);
        fieldWidth = Mathf.Max(1f, fieldWidth);
        animationDuration =
            Mathf.Max(0.01f, animationDuration);

        dirty = true;
    }

    private void LateUpdate()
    {
        CollectCards();

        if (dirty || HasChanged())
        {
            Refresh();
        }
    }

    private void PrepareGroup()
    {
        if (group == null)
        {
            group = GetComponent<CCP.CardGroup>();
        }

        if (group == null)
            return;

        group.isPlayerHand = false;
        group.allowPickup = false;

        // フィールドではCCPの並び替え・クリック処理を使わない
        group.enabled = false;

        if (group.cards == null)
        {
            group.cards = new List<CCP.Card>();
        }
    }

    private void CollectCards()
    {
        fieldCards.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (!child.gameObject.activeInHierarchy)
                continue;

            CardController controller =
                child.GetComponent<CardController>();

            if (controller == null || controller.data == null)
                continue;

            fieldCards.Add(controller);
        }
    }

    private bool IsSnapping(CardController controller)
    {
        CardDrag drag = controller.GetComponent<CardDrag>();

        return drag != null && drag.IsDropSnapPlaying;
    }

    private bool HasChanged()
    {
        if (fieldCards.Count != previousCards.Count)
            return true;

        for (int i = 0; i < fieldCards.Count; i++)
        {
            CardController controller = fieldCards[i];

            if (controller != previousCards[i])
                return true;

            if (controller.isTapped != previousTapped[i])
                return true;

            if (IsSnapping(controller) != previousSnapping[i])
                return true;
        }

        return false;
    }

    private void RememberState()
    {
        previousCards.Clear();
        previousTapped.Clear();
        previousSnapping.Clear();

        foreach (CardController controller in fieldCards)
        {
            previousCards.Add(controller);
            previousTapped.Add(controller.isTapped);
            previousSnapping.Add(IsSnapping(controller));
        }

        dirty = false;
    }

    public void Refresh()
    {
        PrepareGroup();
        CollectCards();

        if (group == null)
            return;

        group.cards.RemoveAll(card =>
            card == null ||
            card.transform.parent != transform ||
            !card.gameObject.activeInHierarchy
        );

        var orderedCards = new List<CCP.Card>();

        foreach (CardController controller in fieldCards)
        {
            CCP.Card card =
                controller.GetComponent<CCP.Card>();

            if (card == null)
            {
                card = controller.gameObject
                    .AddComponent<CCP.Card>();
            }

            // フィールドの移動のみCCPを使用する
            card.enabled = false;

            if (card.parentCardGroup != group)
            {
                Vector3 savedScale =
                    card.transform.localScale;

                if (card.parentCardGroup != null)
                {
                    card.parentCardGroup.RemoveCard(card);
                }

                group.AddBack(card);

                card.transform.localScale = savedScale;
            }

            card.isBeingMovedManually =
                IsSnapping(controller);

            orderedCards.Add(card);
        }

        group.cards.Clear();
        group.cards.AddRange(orderedCards);

        int count = fieldCards.Count;

        if (count == 0)
        {
            RememberState();
            return;
        }

        LayoutSetting setting = GetSetting(count);

        float width = Mathf.Max(1f, setting.cardWidth);

        // 中心間隔がゼロ以下にならないようにする
        float gap = Mathf.Max(
            -width + 1f,
            setting.gap
        );

        float totalWidth =
            width * count + gap * (count - 1);

        // 指定したフィールド幅に収める
        float fit = Mathf.Min(
            1f,
            fieldWidth / Mathf.Max(1f, totalWidth)
        );

        width *= fit;
        gap *= fit;

        float spacing = width + gap;
        float centerIndex = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            CardController controller = fieldCards[i];
            CCP.Card card = orderedCards[i];

            if (IsSnapping(controller))
                continue;

            RectTransform rect =
                controller.GetComponent<RectTransform>();

            if (rect == null)
                continue;

            // Frontの実際の幅から拡大率を計算
            float visualWidth =
                GetVisualWidth(card, rect);

            float targetScale =
                width / Mathf.Max(1f, visualWidth)
                * Mathf.Max(0.01f, cardSize);

            Vector3 localPosition = new Vector3(
                layoutCenter.x +
                    (i - centerIndex) * spacing,
                layoutCenter.y,
                0f
            );

            Vector3 worldPosition =
                transform.TransformPoint(localPosition);

            // tapVisualがあるカードはそちらで横向きを表現
            bool rotateRoot =
                controller.isTapped &&
                (
                    controller.tapVisual == null ||
                    controller.tapVisual == rect
                );

            Quaternion localRotation =
                rotateRoot
                    ? Quaternion.Euler(0f, 0f, -90f)
                    : Quaternion.identity;

            Quaternion worldRotation =
                transform.rotation * localRotation;

            card.MoveToPosition(
                worldPosition,
                worldRotation,
                animationEase,
                animationDuration,
                targetScale
            );
        }

        RememberState();
    }

    private LayoutSetting GetSetting(int count)
    {
        if (layouts == null || layouts.Length == 0)
        {
            return new LayoutSetting(190f, 20f);
        }

        int index = Mathf.Clamp(
            count - 1,
            0,
            layouts.Length - 1
        );

        return layouts[index] ??
            new LayoutSetting(190f, 20f);
    }

    private float GetVisualWidth(
        CCP.Card card,
        RectTransform root
    )
    {
        RectTransform frontRect = null;

        if (card != null && card.front != null)
        {
            frontRect =
                card.front.GetComponent<RectTransform>();
        }

        if (frontRect == null)
            return root.rect.width;

        // 現在のルートの拡大率を除いて計算する
        float rootUnitWidth =
            root.TransformVector(Vector3.right).magnitude;

        float frontWorldWidth =
            frontRect.TransformVector(
                Vector3.right * frontRect.rect.width
            ).magnitude;

        if (rootUnitWidth < 0.0001f)
            return root.rect.width;

        return frontWorldWidth / rootUnitWidth;
    }
}