using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DifficultySelectUI : MonoBehaviour
{
    [Header("Buttons")]
    public RectTransform level1Button;
    public RectTransform level2Button;
    public RectTransform level3Button;

    [Header("Icons")]
    public CanvasGroup level1Icon;
    public CanvasGroup level2Icon;
    public CanvasGroup level3Icon;

    [Header("Animation")]
    public float floatSpeed = 1.2f;
    public float floatHeight = 10f;
    public float hoverScale = 1.05f;
    public float scaleSpeed = 10f;

    Vector2 level1Start;
    Vector2 level2Start;
    Vector2 level3Start;

    bool level1Hover;
    bool level2Hover;
    bool level3Hover;

    void Start()
    {
        if(level1Button != null)
            level1Start = level1Button.anchoredPosition;

        if(level2Button != null)
            level2Start = level2Button.anchoredPosition;

        if(level3Button != null)
            level3Start = level3Button.anchoredPosition;

        SetupButton(level1Button, 1);
        SetupButton(level2Button, 2);
        SetupButton(level3Button, 3);
    }

    void Update()
    {
        AnimateButton(level1Button, level1Start, 0f, level1Hover, level1Icon);
        AnimateButton(level2Button, level2Start, 0.4f, level2Hover, level2Icon);
        AnimateButton(level3Button, level3Start, 0.8f, level3Hover, level3Icon);
    }

    void AnimateButton(
        RectTransform button,
        Vector2 startPos,
        float delay,
        bool isHover,
        CanvasGroup iconGroup
    )
    {
        if(button == null)
            return;

        float y =
            Mathf.Sin((Time.time + delay) * floatSpeed)
            * floatHeight;

        button.anchoredPosition =
            startPos + new Vector2(0f, y);

        float targetScale = isHover ? hoverScale : 1f;

        button.localScale = Vector3.Lerp(
            button.localScale,
            Vector3.one * targetScale,
            Time.deltaTime * scaleSpeed
        );

        if(iconGroup != null)
        {
            float targetAlpha = isHover ? 0.35f : 0.15f;

            iconGroup.alpha = Mathf.Lerp(
                iconGroup.alpha,
                targetAlpha,
                Time.deltaTime * scaleSpeed
            );
        }
    }

    void SetupButton(RectTransform button, int level)
    {
        if(button == null)
            return;

        EventTrigger trigger =
            button.GetComponent<EventTrigger>();

        if(trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        AddEvent(
            trigger,
            EventTriggerType.PointerEnter,
            () => SetHover(level, true)
        );

        AddEvent(
            trigger,
            EventTriggerType.PointerExit,
            () => SetHover(level, false)
        );

        AddEvent(
            trigger,
            EventTriggerType.PointerClick,
            () => SelectLevel(level)
        );
    }

    void AddEvent(
        EventTrigger trigger,
        EventTriggerType type,
        UnityEngine.Events.UnityAction action
    )
    {
        EventTrigger.Entry entry =
            new EventTrigger.Entry();

        entry.eventID = type;

        entry.callback.AddListener(
            (BaseEventData data) =>
            {
                action.Invoke();
            }
        );

        trigger.triggers.Add(entry);
    }

    void SetHover(int level, bool hover)
    {
        if(level == 1)
            level1Hover = hover;
        else if(level == 2)
            level2Hover = hover;
        else if(level == 3)
            level3Hover = hover;
    }

    public void SelectLevel(int level)
    {
        Debug.Log("Select Level " + level);

        if(level == 1)
        {
            GameSettings.SelectedLevel = 1;
        }
        else if(level == 2)
        {
            GameSettings.SelectedLevel = 2;
        }
        else if(level == 3)
        {
            GameSettings.SelectedLevel = 3;
        }

        // ここで次のシーンへ進む
        // UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }
}