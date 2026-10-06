using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReverseChoiceManager : MonoBehaviour
{
    public static ReverseChoiceManager I;

    [Header("UI")]
    public GameObject choicePanel;
    public Button useButton;
    public Button handButton;

    [Header("Monarch Shield Order")]
    public GameObject orderPanel;
    public Button firstOrderButton;
    public Button secondOrderButton;
    public Image firstOrderPreview;
    public Image secondOrderPreview;
    public TMPro.TMP_Text choiceTitle;
    int orderSelection = -1;

    bool isChoosing = false;
    bool result = false;

    [Header("Preview")]
    public Image cardPreviewImage;

    Action<bool> callback;

    void Awake()
    {
        I = this;

        if(choicePanel != null)
            choicePanel.SetActive(false);
        if (orderPanel != null) orderPanel.SetActive(false);
    }

public IEnumerator ShowChoiceRoutine(
    CardData data,
    System.Action<bool> onSelected
)
{
    if(isChoosing)
        yield break;

    if (choicePanel == null || useButton == null || handButton == null)
        throw new InvalidOperationException("Shield choice UI is not configured" );
    isChoosing = true;
    result = false;
    callback = onSelected;

    if(choicePanel != null)
    {
        choicePanel.transform.SetAsLastSibling();
        choicePanel.SetActive(true);
    }
    if (choiceTitle != null) choiceTitle.text = "Use shield: " + data.cardName + "?";

    // =====================
    // カード表面表示
    // =====================

    if(cardPreviewImage != null)
    {
        if(data != null &&
           data.artwork != null)
        {
            cardPreviewImage.sprite =
                data.artwork;

            cardPreviewImage.preserveAspect = true;
            cardPreviewImage.gameObject.SetActive(true);
        }
        else
        {
            cardPreviewImage.gameObject.SetActive(false);
        }
    }

    if(useButton != null)
    {
        useButton.onClick.RemoveAllListeners();
        useButton.onClick.AddListener(
            OnUseSelected
        );
    }

    if(handButton != null)
    {
        handButton.onClick.RemoveAllListeners();
        handButton.onClick.AddListener(
            OnHandSelected
        );
    }

    Debug.Log(
        "リバース選択開始：" +
        (data != null
            ? data.cardName
            : "null")
    );

    while(isChoosing)
    {
        yield return null;
    }

    callback?.Invoke(result);
    callback = null;

    // =====================
    // プレビュー非表示
    // =====================

    if(cardPreviewImage != null)
    {
        cardPreviewImage.sprite = null;
        cardPreviewImage.gameObject.SetActive(false);
    }

    if(choicePanel != null)
        choicePanel.SetActive(false);
}

    public IEnumerator ShowOrderRoutine(CardData first, CardData second, Action<int> selected)
    {
        while (isChoosing) yield return null;
        if (orderPanel == null || firstOrderButton == null || secondOrderButton == null ||
            firstOrderPreview == null || secondOrderPreview == null)
            throw new InvalidOperationException("Shield order UI is not configured");
        firstOrderPreview.sprite = first.artwork;
        secondOrderPreview.sprite = second.artwork;
        firstOrderPreview.preserveAspect = secondOrderPreview.preserveAspect = true;
        orderSelection = -1;
        isChoosing = true;
        firstOrderButton.onClick.RemoveAllListeners();
        secondOrderButton.onClick.RemoveAllListeners();
        firstOrderButton.onClick.AddListener(() => orderSelection = 0);
        secondOrderButton.onClick.AddListener(() => orderSelection = 1);
        orderPanel.transform.SetAsLastSibling();
        orderPanel.SetActive(true);
        try
        {
            while (orderSelection < 0) yield return null;
            selected(orderSelection);
        }
        finally
        {
            isChoosing = false;
            orderPanel.SetActive(false);
            firstOrderButton.onClick.RemoveAllListeners();
            secondOrderButton.onClick.RemoveAllListeners();
        }
    }
    void OnUseSelected()
    {
        result = true;
        isChoosing = false;

        Debug.Log("リバース選択：使用する");
    }

    void OnHandSelected()
    {
        result = false;
        isChoosing = false;

        Debug.Log("リバース選択：手札に加える");
    }
}