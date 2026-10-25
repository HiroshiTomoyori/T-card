using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ResourceManager : MonoBehaviour
{
    [Header("Liquid UI / 水面・数字・チャージ演出")]
    public LiquidResourceSettings liquidSettings;
    LiquidResourceView liquidView;
    public int MaxResource => maxResource;

    public LiquidResourceView EnsureLiquidView()
    {
        if(!Application.isPlaying || resourceText == null) return null;
        if(liquidView != null) return liquidView;
        if(liquidSettings == null) liquidSettings = Resources.Load<LiquidResourceSettings>("TCardResource/LiquidResourceSettings");
        if(liquidSettings == null || liquidSettings.liquidShader == null) return null;
        Transform host = resourceText.transform.parent;
        if(liquidSettings.behindHand)
        {
            foreach(var canvas in host.GetComponentsInChildren<Canvas>(true))canvas.overrideSorting=false;
            var hostCanvas=host.GetComponent<Canvas>();if(hostCanvas!=null)hostCanvas.overrideSorting=false;
            var root=host.GetComponentInParent<Canvas>().rootCanvas.transform;
            Transform hand=null;
            foreach(var rt in root.GetComponentsInChildren<RectTransform>(true))if(rt.name=="HandArea"){hand=rt;break;}
            if(hand!=null) LiquidResourceView.PlaceBehindHand(host,hand);
        }

        var go = new GameObject("Liquid Resource UI", typeof(RectTransform), typeof(LiquidResourceView));
        go.layer = resourceText.gameObject.layer;
        go.transform.SetParent(host, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
        rect.anchoredPosition = resourceText.rectTransform.anchoredPosition;
        liquidView = go.GetComponent<LiquidResourceView>();
        liquidView.Initialize(liquidSettings, resourceText.font);
        foreach(Graphic graphic in host.GetComponentsInChildren<Graphic>(true))
            if(!graphic.transform.IsChildOf(go.transform)) graphic.enabled = false;
        return liquidView;
    }

    public TextMeshProUGUI resourceText;
    public Sprite resourceHeartSprite;
    public Material resourceHeartMaterial;
    public float resourceHeartVerticalOffset = 4f;
    [Min(8f)] public float resourceHeartSize = 32f;
    [Min(0f)] public float resourceHeartSpacing = 10f;
    Image resourceHeartImage;
    Material loadedWhiteHeartMaterial;
    string lastHeartText;
    float cachedNumberWidth;
    bool visualStyleReported;
    
    public int currentResource = 0;
    int maxResource = 0;

    void Start()
    {
        if(Application.isPlaying) UpdateUI();
        else UpdateHeartIcon();
    }

    void OnEnable() => UpdateHeartIcon();

    void LateUpdate()
    {
        if(Application.isPlaying)
        {
            var view = EnsureLiquidView();
            if(view != null) { view.SetAmount(currentResource, maxResource); return; }
        }
        UpdateHeartIcon();
    }

    public void AddResource()
    {
        Debug.Log("ResourceManager AddResource 呼ばれた");

        maxResource += 1;
        currentResource += 1;

        UpdateUI();
        if(liquidView != null) liquidView.PlayCharge();
    }

    void UpdateUI()
    {
        Debug.Log("Resource UI 更新: " + currentResource + " / " + maxResource);

        if (resourceText != null)
        {
            resourceText.text =
                currentResource + " / " + maxResource;
            var view = EnsureLiquidView();
            if(view != null) view.SetAmount(currentResource, maxResource);
            else UpdateHeartIcon();
        }
        else
        {
            Debug.LogError("ResourceText が未設定");
        }
    }

    void UpdateHeartIcon()
    {
        if(Application.isPlaying && EnsureLiquidView() != null) return;
        if(resourceText == null) return;
        // Apply the requested layout even when Unity still has an older scene in memory.
        resourceText.rectTransform.anchoredPosition = new Vector2(147f, -10f);
        if(resourceHeartImage == null)
        {
            Transform existing = resourceText.transform.Find("Resource Heart");
            if(existing != null) resourceHeartImage = existing.GetComponent<Image>();
        }
        Sprite heartSprite = resourceHeartSprite != null ? resourceHeartSprite :
            (resourceHeartImage != null ? resourceHeartImage.sprite : null);
        if(heartSprite == null) return;
        if(resourceHeartImage == null)
        {
            GameObject icon = new GameObject("Resource Heart", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icon.layer = resourceText.gameObject.layer;
            icon.transform.SetParent(resourceText.transform, false);
            resourceHeartImage = icon.GetComponent<Image>();
            resourceHeartImage.raycastTarget = false;
            resourceHeartImage.preserveAspect = true;
        }
        resourceHeartImage.sprite = heartSprite;
        resourceHeartImage.enabled = true;
        foreach(Transform child in resourceText.transform)
        {
            if(child == resourceHeartImage.transform || child.name != "Resource Heart") continue;
            Image duplicate = child.GetComponent<Image>();
            if(duplicate != null) duplicate.enabled = false;
        }
        resourceHeartImage.color = Color.white;
        if(loadedWhiteHeartMaterial == null)
            loadedWhiteHeartMaterial = Resources.Load<Material>("UI/ResourceHeartWhite");
        Material whiteMaterial = loadedWhiteHeartMaterial != null ? loadedWhiteHeartMaterial : resourceHeartMaterial;
        if(whiteMaterial != null) resourceHeartImage.material = whiteMaterial;
        RectTransform rect = resourceHeartImage.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.one * resourceHeartSize;
        if(lastHeartText != resourceText.text)
        {
            cachedNumberWidth = resourceText.GetPreferredValues(resourceText.text).x;
            lastHeartText = resourceText.text;
        }
        rect.anchoredPosition = new Vector2(-cachedNumberWidth * 0.5f - resourceHeartSpacing - resourceHeartSize * 0.5f, 4f);
        if(!visualStyleReported && whiteMaterial != null)
        {
            visualStyleReported = true;
            Debug.Log("[Resource UI style applied] number=" + resourceText.rectTransform.anchoredPosition +
                " heart=" + rect.anchoredPosition + " shader=" + whiteMaterial.shader.name);
        }
    }

    public void UseResource(int amount)
    {
        currentResource -= amount;

        if(currentResource < 0)
            currentResource = 0;

        UpdateUI();
    }

        public void RecoverResource()
    {
        currentResource = maxResource;
        UpdateUI();
    }
}
