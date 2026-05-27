using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class GraveyardHover :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public Transform graveyard;
    public TextMeshProUGUI countText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(graveyard == null || countText == null)
            return;

        int count = 0;

        for(int i=0;i<graveyard.childCount;i++)
        {
            Transform child =
                graveyard.GetChild(i);

            // 背景除外
            if(child.name == "GraveBackground")
                continue;

            count++;
        }

        countText.text =
            "墓地：" + count + "枚";

        countText.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(countText != null)
        {
            countText.gameObject.SetActive(false);
        }
    }
}