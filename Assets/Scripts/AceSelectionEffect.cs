using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
// Non-interactive Canvas graphics keep target selection available beneath the spell.
public class AceSelectionEffect : MonoBehaviour
{
    TurnManager owner;
    AudioSource sound;
    AudioClip waiting;
    float volume, age, nextChime = 1.5f, ending = -1f;
    readonly List<CardController> targets = new List<CardController>();
    readonly List<AceSpellRing> rings = new List<AceSpellRing>();
    readonly Vector3[] corners = new Vector3[4];

    public void Initialize(TurnManager manager, Transform area, bool enemy, AudioClip cast, AudioClip idle, float level)
    {
        RectTransform overlay = transform as RectTransform;
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        overlay.localScale = Vector3.one;
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Canvas effectCanvas = gameObject.AddComponent<Canvas>();
        effectCanvas.overrideSorting = true;
        effectCanvas.sortingOrder = parentCanvas != null ? parentCanvas.sortingOrder + 100 : 100;
        transform.SetAsLastSibling();
        owner = manager;
        waiting = idle;
        volume = level;
        sound = gameObject.AddComponent<AudioSource>();
        sound.playOnAwake = false;
        sound.spatialBlend = 0f;
        if(cast != null) sound.PlayOneShot(cast, volume);
        Color tint = enemy ? new Color(1f, 0.35f, 0.2f) : new Color(0.25f, 0.85f, 1f);
        if(area == null) return;
        foreach(Transform child in area)
        {
            CardController target = child.GetComponent<CardController>();
            if(target == null || target.data == null || !child.gameObject.activeInHierarchy) continue;
            GameObject halo = new GameObject("Ace Target Aura", typeof(RectTransform), typeof(CanvasRenderer));
            halo.layer = gameObject.layer;
            halo.transform.SetParent(transform, false);
            AceSpellRing ring = halo.AddComponent<AceSpellRing>();
            ring.raycastTarget = false;
            ring.color = tint;
            targets.Add(target);
            rings.Add(ring);
        }
    }

    void LateUpdate()
    {
        age += Time.unscaledDeltaTime;
        bool selecting = owner != null && owner.isActiveAndEnabled && owner.IsSelectingDestroyTarget();
        if(!selecting && ending < 0f) { ending = age; sound.Stop(); }
        float fade = ending < 0f ? 1f : 1f - (age - ending) / 0.35f;
        if(fade <= 0f) { Destroy(gameObject); return; }
        if(selecting && waiting != null && age >= nextChime)
        {
            sound.PlayOneShot(waiting, volume * 0.22f);
            nextChime = age + Mathf.Max(1.8f, waiting.length);
        }
        RectTransform parent = transform as RectTransform;
        if(parent == null) return;
        for(int i = 0; i < rings.Count; i++)
        {
            CardController target = targets[i];
            if(target == null) { rings[i].enabled = false; continue; }
            // Freeze the final aura when the selected card moves to its graveyard.
            if(selecting)
            {
                RectTransform image = target.artworkImage != null ? target.artworkImage.rectTransform : target.transform as RectTransform;
                if(image == null) continue;
                image.GetWorldCorners(corners);
                Vector3 min = parent.InverseTransformPoint(corners[0]), max = min;
                for(int j = 1; j < 4; j++)
                {
                    Vector3 p = parent.InverseTransformPoint(corners[j]);
                    min = Vector3.Min(min, p); max = Vector3.Max(max, p);
                }
                RectTransform rect = rings[i].rectTransform;
                rect.anchorMin = rect.anchorMax = parent.pivot;
                rect.anchoredPosition = (min + max) * 0.5f;
                rect.sizeDelta = new Vector2(max.x - min.x + 28f, max.y - min.y + 28f);
            }
            rings[i].phase = age;
            Color color = rings[i].color;
            color.a = fade * (0.8f + 0.15f * Mathf.Sin(age * 3.5f));
            rings[i].color = color;
            rings[i].SetVerticesDirty();
        }
    }
}
