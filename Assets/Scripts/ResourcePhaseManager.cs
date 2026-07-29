using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ResourcePhaseManager : MonoBehaviour
{
    [Header("Drop Highlight")]
    public GameObject resourceGlow;

    [Header("Drop Area")]
    public Image resourceAreaImage;

    [Header("Resource")]
    public Transform resourceArea;
    public ResourceManager resourceManager;

    [Header("Charge Effect")]
    public GameObject chargeLightEffectPrefab;
    public AudioSource seSource;
    public AudioClip chargeSE;
    public float effectTime = 0.6f;

    [Header("Resource Drop Raycast")]
    public CanvasGroup resourceDropArea;

    bool isRunning = false;
    bool hasCharged = false;

    void Start()
    {
        DisableResourceDrop();

        if(resourceGlow != null)
            resourceGlow.SetActive(false);
    }

    public void ShowDropHighlight()
    {
        if(resourceGlow != null)
        {
            resourceGlow.SetActive(true);
            resourceGlow.transform.SetAsLastSibling();
        }
    }

    public void HideDropHighlight()
    {
        if(resourceGlow != null)
            resourceGlow.SetActive(false);
    }

    public void StartResourcePhase()
    {
        isRunning = true;
        hasCharged = false;

        // 見た目は表示したまま、当たり判定だけOFF
        SetResourceDropRaycast(false);

        Debug.Log("リソースフェイズ開始");
    }

    public bool IsRunning()
    {
        return isRunning;
    }

    public void TryChargeResource(CardDrag card)
    {
        HideDropHighlight();

        if(!isRunning)
        {
            Debug.Log("今はリソースフェイズではありません");
            return;
        }

        if(hasCharged)
        {
            Debug.Log("このターンはすでにチャージ済みです");
            return;
        }

        if(card == null)
        {
            Debug.LogError("CardDrag が未設定です");
            return;
        }

        hasCharged = true;
        card.MarkDroppedSuccessfully();

        StartCoroutine(
            ChargeRoutine(card.gameObject)
        );
    }

    IEnumerator ChargeRoutine(GameObject cardObj)
    {
        DisableResourceDrop();

        if(resourceArea != null)
        {
            cardObj.transform.SetParent(
                resourceArea,
                false
            );

            RectTransform rt =
                cardObj.GetComponent<RectTransform>();

            if(rt != null)
            {
                rt.anchoredPosition =
                    Vector2.zero;

                rt.localScale =
                    Vector3.one;
            }
        }

        // ★ ドロップしたカードをすぐ非表示
        CanvasGroup cg =
            cardObj.GetComponent<CanvasGroup>();

        if(cg != null)
        {
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        if(seSource != null &&
           chargeSE != null)
        {
            seSource.PlayOneShot(chargeSE);
        }

        GameObject effectObj = null;

        if(chargeLightEffectPrefab != null &&
           resourceArea != null)
        {
            effectObj =
                Instantiate(
                    chargeLightEffectPrefab,
                    resourceArea
                );

            RectTransform effectRt =
                effectObj.GetComponent<RectTransform>();

            if(effectRt != null)
            {
                effectRt.anchoredPosition =
                    Vector2.zero;

                effectRt.localScale =
                    Vector3.one;
            }
        }

        yield return new WaitForSeconds(effectTime);

        if(resourceManager != null)
        {
            resourceManager.AddResource();
        }
        else
        {
            Debug.LogError(
                "ResourceManager が未設定です"
            );
        }

        Destroy(cardObj);

        if(effectObj != null)
        {
            Destroy(effectObj);
        }

        Debug.Log("リソースチャージ完了");

        CompleteResourcePhase();
    }

    public void CompleteResourcePhase()
    {
        if(!isRunning)
            return;

        EndResourcePhase();

        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if(turnManager != null)
        {
            turnManager.OnResourcePhaseComplete();
        }
        else
        {
            Debug.LogError(
                "TurnManager が見つかりません"
            );
        }
    }

    public void EndResourcePhase()
    {
        isRunning = false;

        // リソースエリアの当たり判定をOFF
        SetResourceDropRaycast(false);

        HideDropHighlight();

        Debug.Log("リソースフェイズ終了");
    }

    public void SetResourceAreaRaycast(bool enabled)
    {
        if(resourceAreaImage == null)
            return;

        resourceAreaImage.raycastTarget =
            enabled;
    }

    void EnableResourceDrop()
    {
        if(resourceAreaImage == null)
            return;

        resourceAreaImage.raycastTarget =
            true;
    }

    void DisableResourceDrop()
    {
        if(resourceAreaImage == null)
            return;

        resourceAreaImage.raycastTarget =
            false;
    }

    public void SetResourceDropRaycast(bool enabled)
    {
        if(resourceDropArea != null)
        {
            // 表示状態は変更しない
            resourceDropArea.alpha = 1f;

            // リソースフェイズ中かつドラッグ中だけ有効
            resourceDropArea.blocksRaycasts =
                isRunning && enabled;

            resourceDropArea.interactable =
                isRunning && enabled;
        }

        if(resourceAreaImage != null)
        {
            // Image自体の当たり判定も同時に切り替える
            resourceAreaImage.raycastTarget =
                isRunning && enabled;
        }
    }

    public bool IsResourcePhase()
    {
        return isRunning;
    }
}