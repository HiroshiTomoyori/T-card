using TMPro;
using UnityEngine;

public class EnemyResourceManager : MonoBehaviour
{
    public TextMeshProUGUI enemyResourceText;

    public LiquidResourceSettings liquidSettings;
    LiquidResourceView liquidView;
    public LiquidResourceView EnsureLiquidView()
    {
        if(!Application.isPlaying||enemyResourceText==null)return null;
        if(liquidView!=null)return liquidView;
        if(liquidSettings==null)liquidSettings=Resources.Load<LiquidResourceSettings>("TCardResource/EnemyLiquidResourceSettings");
        if(liquidSettings==null)return null;
        var source=enemyResourceText.rectTransform;
        var host=new GameObject("Enemy Resource UI Host",typeof(RectTransform));host.layer=gameObject.layer;host.transform.SetParent(source.parent,false);
        var hr=(RectTransform)host.transform;hr.anchorMin=source.anchorMin;hr.anchorMax=source.anchorMax;hr.pivot=source.pivot;hr.anchoredPosition=source.anchoredPosition;hr.sizeDelta=source.sizeDelta;
        hr.SetSiblingIndex(source.GetSiblingIndex());
        var go=new GameObject("Enemy Liquid Resource UI",typeof(RectTransform),typeof(LiquidResourceView));go.layer=gameObject.layer;go.transform.SetParent(hr,false);
        liquidView=go.GetComponent<LiquidResourceView>();
        var d=FindFirstObjectByType<HandDealer>();liquidView.ConfigureLayerHand(d!=null?d.enemyHandArea:null,false);
        liquidView.Initialize(liquidSettings,enemyResourceText.font);liquidView.SetAmount(currentResource,maxResource);enemyResourceText.enabled=false;
        return liquidView;
    }

    public int currentResource = 0;
    public int maxResource = 0;

    void Start()
    {
        UpdateText();
    }

    public void AddResource() => AddResource(true);

    public void AddResource(bool refill)
    {
        maxResource++;

        // 現在値も最大値まで回復
        currentResource = refill ? maxResource : currentResource + 1;

        UpdateText();
        EnsureLiquidView()?.PlayCharge();

        Debug.Log(
            "敵リソース：" +
            currentResource +
            "/" +
            maxResource
        );
    }

    public bool UseResource(int amount)
    {
        if(currentResource < amount)
            return false;

        currentResource -= amount;

        UpdateText();

        return true;
    }

    void UpdateText()
    {
        EnsureLiquidView()?.SetAmount(currentResource,maxResource);
        if(enemyResourceText != null)
        {
        enemyResourceText.text =
            currentResource +
            " / " +
            maxResource;
        }
    }

    public void RecoverResource()
    {
        currentResource = maxResource;
        UpdateText();
    }
}
