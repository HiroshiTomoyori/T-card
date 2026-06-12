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
    }

public IEnumerator ShowChoiceRoutine(
    CardData data,
    System.Action<bool> onSelected
)
{
    if(isChoosing)
        yield break;

    isChoosing = true;
    result = false;
    callback = onSelected;

    if(choicePanel != null)
        choicePanel.SetActive(true);

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