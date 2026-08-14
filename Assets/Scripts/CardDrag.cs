using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

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

    public void OnBeginDrag(
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
    }

    public void OnDrag(
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
    }

    public void OnEndDrag(
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
    }

    void ApplyDraggingSize()
    {
        if (rectTransform == null)
            return;

        transform.localScale =
            Vector3.one;

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
    public void DropToBattleArea(
        Transform battleArea,
        bool playSnap = true
    )
    {
        if (battleArea == null)
            return;

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

        transform.SetParent(
            battleArea,
            false
        );

        transform.localPosition =
            Vector3.zero;

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

    IEnumerator DropSnapRoutine()
    {
        PlayDropSnapSE();

        /*
         * 最初にカードを大きく表示する。
         */
        transform.localScale =
            Vector3.one *
            battleScale *
            dropZoomMultiplier;

        float elapsed = 0f;

        while (
            elapsed < dropZoomDuration
        )
        {
            /*
             * 拡大演出中は召喚酔いが付いても
             * 透明にしない。
             */
            canvasGroup.alpha = 1f;

            elapsed +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        transform.localScale =
            Vector3.one * battleScale;

        /*
         * スナップ完了後は
         * 本来の召喚酔い表示へ戻す。
         */
        CardController card =
            GetComponent<CardController>();

        canvasGroup.alpha =
            card != null &&
            card.hasSummonSickness
                ? 0.6f
                : 1f;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        dropSnapCoroutine = null;
    }

    void PlayDropSnapSE()
    {
        if (dropSnapSE == null)
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
            dropSnapSE,
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