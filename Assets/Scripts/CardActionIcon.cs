using UnityEngine;
using UnityEngine.UI;
using System;

public class CardActionIcon : MonoBehaviour
{
    public GameObject attackIcon;
    public GameObject blockIcon;
    public Vector2 markSize = new Vector2(38f, 44f);
    public float markGap = 8f;
    readonly Vector3[] artworkCorners = new Vector3[4];
    CardController card;

    public static bool CanAttack(CardController controller)
    {
        if(controller == null || controller.data == null || controller.isTapped || controller.hasSummonSickness)
            return false;
        return controller.data.effectTypes == null || !Array.Exists(controller.data.effectTypes,
            effect => effect == EffectType.CannotAttack || effect == EffectType.DestroyOneEnemyBattle ||
                effect == EffectType.TapAllEnemyBattle || effect == EffectType.InstantDestroySelf ||
                effect == EffectType.JokerClearBattleArea);
    }

    public static bool CanBlock(CardController controller)
    {
        return controller != null && controller.data != null && !controller.isTapped &&
            controller.data.effectTypes != null && Array.Exists(controller.data.effectTypes,
                effect => effect == EffectType.BlockOnly);
    }

    public void EnsureIcons(CardActionIcon template)
    {
        // Attack and defense are communicated by glow, not legacy icons.
        HideAll();
    }

    public void SetAvailability(bool attack, bool block)
    {
        if(attack) ShowAttackIcon();
        else if(block) ShowBlockIcon();
        else HideAll();
    }

    void PositionMark(GameObject mark)
    {
        if(mark == null) return;
        RectTransform root = transform as RectTransform;
        RectTransform rect = mark.transform as RectTransform;
        if(root == null || rect == null) return;
        if(card == null) card = GetComponent<CardController>();
        RectTransform artwork = card != null && card.artworkImage != null ? card.artworkImage.rectTransform : root;
        artwork.GetWorldCorners(artworkCorners);
        float left = float.MaxValue, right = float.MinValue, bottom = float.MaxValue;
        foreach(Vector3 corner in artworkCorners)
        {
            Vector3 local = root.InverseTransformPoint(corner);
            left = Mathf.Min(left, local.x); right = Mathf.Max(right, local.x);
            bottom = Mathf.Min(bottom, local.y);
        }
        if(rect.parent != root) rect.SetParent(root, false);
        rect.anchorMin = rect.anchorMax = root.pivot;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.sizeDelta = markSize;
        rect.anchoredPosition = new Vector2((left + right) * 0.5f, bottom - markGap - markSize.y * 0.5f);
        foreach(Graphic graphic in mark.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
            Image image = graphic as Image;
            if(image != null) image.preserveAspect = true;
        }
        // Keep the marker outside the artwork mask and above the table/hand visuals.
        Canvas parentCanvas = root.GetComponentInParent<Canvas>();
        Canvas overlay = mark.GetComponent<Canvas>();
        if(overlay == null) overlay = mark.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingLayerID = parentCanvas != null ? parentCanvas.sortingLayerID : 0;
        overlay.sortingOrder = (parentCanvas != null ? parentCanvas.sortingOrder : 0) + 30;
        rect.SetAsLastSibling();
    }

    public void ShowAttackIcon()
    {
        HideAll();
    }

    public void ShowBlockIcon()
    {
        HideAll();
    }

    public void HideAll()
    {
        if(attackIcon != null)
            attackIcon.SetActive(false);

        if(blockIcon != null)
            blockIcon.SetActive(false);
    }

    void OnDisable() => HideAll();
}
