using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ResourcePhaseManager : MonoBehaviour
{
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

    public Button skipResourceButton;

    bool isRunning = false;
    bool hasCharged = false;

    void Start()
    {
        DisableResourceDrop();
        HideSkipResourceButton();
    }

    public void StartResourcePhase()
    {
        isRunning = true;
        hasCharged = false;

        EnableResourceDrop();

        Debug.Log("=== Resource Phase 開始 ===");

        if(skipResourceButton != null)
        {
            skipResourceButton.transform.SetAsLastSibling();
            skipResourceButton.gameObject.SetActive(true);
            skipResourceButton.interactable = true;
        }
    }

    public void TryChargeResource(CardDrag card)
    {
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

        StartCoroutine(ChargeRoutine(card.gameObject));
    }

    IEnumerator ChargeRoutine(GameObject cardObj)
    {
        DisableResourceDrop();

        if(resourceArea != null)
        {
            cardObj.transform.SetParent(resourceArea, false);

            RectTransform rt =
                cardObj.GetComponent<RectTransform>();

            if(rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
            }
        }

        if(seSource != null && chargeSE != null)
        {
            seSource.PlayOneShot(chargeSE);
        }

        GameObject effectObj = null;

        if(chargeLightEffectPrefab != null && resourceArea != null)
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
                effectRt.anchoredPosition = Vector2.zero;
                effectRt.localScale = Vector3.one;
            }
        }

        yield return new WaitForSeconds(effectTime);

        if(resourceManager != null)
        {
            resourceManager.AddResource();
        }
        else
        {
            Debug.LogError("ResourceManager が未設定です");
        }

        Destroy(cardObj);

        if(effectObj != null)
        {
            Destroy(effectObj);
        }

        Debug.Log("リソースチャージ完了");

        EndResourcePhase();

        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if(turnManager != null)
        {
            turnManager.OnResourcePhaseComplete();
        }
        else
        {
            Debug.LogError("TurnManager が見つかりません");
        }
    }

    public void EndResourcePhase()
    {
        isRunning = false;

        HideSkipResourceButton();
        DisableResourceDrop();

        Debug.Log("=== Resource Phase 終了 ===");
    }

    public void SkipResourceCharge()
    {
        Debug.Log("SkipResourceCharge ボタン押下");

        if(!isRunning)
        {
            HideSkipResourceButton();
            DisableResourceDrop();

            Debug.Log("Resource Phase中ではないためボタン非表示");
            return;
        }

        Debug.Log("リソースチャージせずMain Phaseへ");

        EndResourcePhase();

        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if(turnManager != null)
        {
            turnManager.OnResourcePhaseComplete();
        }
        else
        {
            Debug.LogError("TurnManager が見つかりません");
        }
    }

    void EnableResourceDrop()
    {
        if(resourceAreaImage == null)
            return;

        resourceAreaImage.raycastTarget = true;
    }

    void DisableResourceDrop()
    {
        if(resourceAreaImage == null)
            return;

        resourceAreaImage.raycastTarget = false;
    }

    void HideSkipResourceButton()
    {
        if(skipResourceButton == null)
            return;

        skipResourceButton.interactable = false;
        skipResourceButton.gameObject.SetActive(false);

        Debug.Log("SkipResourceButton 非表示");
    }
}