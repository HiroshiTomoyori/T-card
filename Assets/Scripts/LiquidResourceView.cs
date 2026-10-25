using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LiquidResourceView : MonoBehaviour
{
    public LiquidResourceSettings Settings {get;private set;}
    LiquidResourceGraphic surface,beam;
    TextMeshProUGUI digits,oldDigits,maxDigits,oldMaxDigits;
    LiquidResourceSeparator separator;
    TMP_FontAsset runtimeFont;
    Font activeSource;
    RectTransform panel,fallingCard;
    Material digitMaterial,oldMaterial;
    int current=-1,maximum=-1;
    float level,targetLevel,velocity,clock,numberAge=99,rippleAge=99,beamAge=99;
    bool initialized;
    Vector2 initialPosition;

    public void Initialize(LiquidResourceSettings settings,TMP_FontAsset fallbackFont)
    {
        Settings=settings;
        if(initialized)return;
        initialized=true;
        panel=(RectTransform)transform;
        initialPosition=panel.anchoredPosition;
        var body=new GameObject("Ever-moving water",typeof(RectTransform),typeof(CanvasRenderer),typeof(LiquidResourceGraphic));
        body.layer=gameObject.layer;body.transform.SetParent(transform,false);surface=body.GetComponent<LiquidResourceGraphic>();
        var column=new GameObject("Charge light column",typeof(RectTransform),typeof(CanvasRenderer),typeof(LiquidResourceGraphic));
        column.layer=gameObject.layer;column.transform.SetParent(transform,false);beam=column.GetComponent<LiquidResourceGraphic>();
        oldDigits=MakeDigits("Previous value",fallbackFont,out oldMaterial);
        digits=MakeDigits("Resource value",fallbackFont,out digitMaterial);
        maxDigits=MakeDigits("Capacity",fallbackFont,out _);
        oldMaxDigits=MakeDigits("Previous capacity",fallbackFont,out _);
        var slash=new GameObject("Fixed elegant separator",typeof(RectTransform),typeof(CanvasRenderer),typeof(LiquidResourceSeparator));
        slash.layer=gameObject.layer;slash.transform.SetParent(transform,false);separator=slash.GetComponent<LiquidResourceSeparator>();
        digits.text="0";maxDigits.text="0";oldDigits.text="";oldMaxDigits.text="";
        UpdateVisuals(0);
    }

    TextMeshProUGUI MakeDigits(string name,TMP_FontAsset fallback,out Material owned)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));
        go.layer=gameObject.layer;go.transform.SetParent(transform,false);
        var text=go.GetComponent<TextMeshProUGUI>();text.font=Settings.numberFont!=null?Settings.numberFont:fallback;
        text.extraPadding=true;text.raycastTarget=false;text.alignment=TextAlignmentOptions.Center;text.textWrappingMode=TextWrappingModes.NoWrap;
        text.overflowMode=TextOverflowModes.Overflow;text.color=Settings.numberColor;text.fontSize=Settings.fontSize;
        owned=text.font!=null?new Material(text.font.material):null;
        if(owned!=null)
        {
            owned.hideFlags=HideFlags.HideAndDontSave;
            if(owned.HasProperty("_OutlineColor"))owned.SetColor("_OutlineColor",Settings.waveColor);
            if(owned.HasProperty("_OutlineWidth"))owned.SetFloat("_OutlineWidth",.11f);
            if(owned.HasProperty("_UnderlayColor"))
            {
                owned.EnableKeyword("UNDERLAY_ON");owned.SetColor("_UnderlayColor",new Color(.52f,.27f,.85f,.8f));
                owned.SetFloat("_UnderlayOffsetY",-.3f);owned.SetFloat("_UnderlayDilate",.25f);owned.SetFloat("_UnderlaySoftness",.7f);
            }
            text.fontSharedMaterial=owned;
        }
        return text;
    }

    public static float ResourceFraction(int available,int capacity)
        => capacity<=0?0f:Mathf.Clamp01((float)Mathf.Max(0,available)/capacity);

    public void SetAmount(int available,int capacity,bool charge=false)
    {
        available=Mathf.Max(0,available);capacity=Mathf.Max(0,capacity);
        if(current!=available || maximum!=capacity)
        {
            bool first=current<0;
            oldDigits.text=digits.text;oldMaxDigits.text=maxDigits.text;digits.text=available.ToString();maxDigits.text=capacity.ToString();
            current=available;maximum=capacity;
            targetLevel=ResourceFraction(available,capacity);
            if(first){level=targetLevel;numberAge=99;}else{numberAge=0;rippleAge=0;}
        }
        if(charge)PlayCharge();
    }
    public void PlayCharge(){beamAge=0;rippleAge=0;}

    void Update(){if(initialized)Advance(Time.unscaledDeltaTime);}
    public void Advance(float dt)
    {
        dt=Mathf.Max(0,dt);clock+=dt;numberAge+=dt;rippleAge+=dt;beamAge+=dt;
        level=Mathf.SmoothDamp(level,targetLevel,ref velocity,Mathf.Max(.05f,Settings.levelResponse),Mathf.Infinity,dt);
        UpdateVisuals(dt);
    }
    void UpdateVisuals(float dt)
    {
        if(!initialized || Settings==null)return;
        panel.sizeDelta=Settings.panelSize;
        panel.anchoredPosition=initialPosition+Settings.offset;
        var sr=surface.rectTransform;sr.sizeDelta=Settings.panelSize+Vector2.one*Settings.auraPadding*2;
        var br=beam.rectTransform;br.pivot=new Vector2(.5f,0);br.anchoredPosition=new Vector2(0,-Settings.panelSize.y*.15f);
        br.sizeDelta=new Vector2(Settings.beamWidth,Settings.beamHeight);
        float impulse=Mathf.Exp(-4*rippleAge/Mathf.Max(.1f,Settings.rippleDuration));
        surface.Draw(Settings,clock,level,impulse,beamAge,false);
        beam.gameObject.SetActive(beamAge<Settings.beamDuration);
        if(beam.gameObject.activeSelf)beam.Draw(Settings,clock,level,impulse,beamAge,true);
        float p=Mathf.Clamp01(numberAge/Mathf.Max(.1f,Settings.numberTransition));
        float newAlpha=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.85f,p));
        float oldAlpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.6f,p));
        EnsureFont();
        separator.Configure(Settings);
        UpdateDigits(digits,newAlpha,impulse,Mathf.Lerp(-5,0,p));
        UpdateDigits(maxDigits,newAlpha,impulse,Mathf.Lerp(-5,0,p));
        UpdateDigits(oldMaxDigits,oldAlpha,impulse,Mathf.Lerp(0,7,p));
        UpdateDigits(oldDigits,oldAlpha,impulse,Mathf.Lerp(0,7,p));
    }
    void UpdateDigits(TextMeshProUGUI text,float alpha,float impulse,float y)
    {
        text.rectTransform.sizeDelta=Settings.panelSize;
        bool right=text==maxDigits||text==oldMaxDigits;
        text.rectTransform.anchoredPosition=new Vector2((right?1:-1)*Settings.numberOffset.x,(right?-1:1)*Settings.numberOffset.y+y);
        text.enableAutoSizing=true;text.fontSizeMin=Settings.fontSize*.42f;text.fontSizeMax=Settings.fontSize;
        text.rectTransform.sizeDelta=new Vector2(Settings.panelSize.x*.47f,Settings.panelSize.y);
        text.fontSize=Settings.fontSize;text.color=new Color(Settings.numberColor.r,Settings.numberColor.g,Settings.numberColor.b,alpha*Settings.numberColor.a);
        if(text.fontSharedMaterial!=null && text.fontSharedMaterial.HasProperty("_OutlineColor"))text.fontSharedMaterial.SetColor("_OutlineColor",Settings.glowColor);
        var mat=text.fontSharedMaterial;
        if(mat!=null && mat.HasProperty("_UnderlayColor")){mat.SetColor("_UnderlayColor",Settings.glowColor*new Color(1,1,1,Settings.numberGlow));mat.SetFloat("_UnderlayDilate",.04f);mat.SetFloat("_UnderlaySoftness",.45f);}
        if(mat!=null && mat.HasProperty("_GlowColor")){mat.EnableKeyword("GLOW_ON");mat.SetColor("_GlowColor",Settings.glowColor);mat.SetFloat("_GlowPower",Settings.numberGlow*.25f);mat.SetFloat("_GlowOuter",.1f);}
        text.UpdateMeshPadding();
        text.ForceMeshUpdate();
        if(impulse<.002f || alpha<.001f)return;
        var info=text.textInfo;
        for(int c=0;c<info.characterCount;c++)
        {
            var ch=info.characterInfo[c];if(!ch.isVisible)continue;
            var vertices=info.meshInfo[ch.materialReferenceIndex].vertices;
            for(int j=0;j<4;j++)
            {
                int index=ch.vertexIndex+j;Vector3 v=vertices[index];
                v.x+=Mathf.Sin(v.y*.055f+clock*10)*Settings.numberRefraction*.4f*impulse;
                v.y+=Mathf.Sin(v.x*.055f-clock*9)*Settings.numberRefraction*impulse;vertices[index]=v;
            }
        }
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    public IEnumerator DropCard(GameObject source)
    {
        if(source==null)yield break;
        var card=source.GetComponent<CardController>();
        Sprite sprite=card!=null&&card.artworkImage!=null?card.artworkImage.sprite:null;
        if(sprite==null && card!=null && card.data!=null)sprite=card.data.artwork;
        var cg=source.GetComponent<CanvasGroup>();if(cg==null)cg=source.AddComponent<CanvasGroup>();
        cg.alpha=0;cg.blocksRaycasts=false;cg.interactable=false;
        var root=panel;
        var go=new GameObject("Resource card falling",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(CanvasGroup));
        go.layer=gameObject.layer;go.transform.SetParent(root,false);
        fallingCard=(RectTransform)go.transform;
        var image=go.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;
        var fade=go.GetComponent<CanvasGroup>();fade.blocksRaycasts=false;
        fallingCard.sizeDelta=new Vector2(Settings.dropCardWidth,Settings.dropCardWidth*1.46f);
        Vector3 end=root.InverseTransformPoint(panel.TransformPoint(new Vector3(0,Settings.panelSize.y*(.06f+.85f*level-.5f),0)));
        float elapsed=0;
        try
        {
            while(elapsed<Settings.dropDuration && fallingCard!=null)
            {
                float p=Mathf.Clamp01(elapsed/Mathf.Max(.1f,Settings.dropDuration));
                Vector4 pose=CardPose(Settings,p);
                fallingCard.anchoredPosition=(Vector2)end+Vector2.up*pose.x;
                fallingCard.localRotation=Quaternion.Euler(0,0,pose.y);
                fallingCard.localScale=Vector3.one*pose.z;
                fade.alpha=pose.w;
                yield return null;elapsed+=Time.unscaledDeltaTime;
            }
        }
        finally{if(go!=null)Destroy(go);fallingCard=null;}
    }
    public static Vector4 CardPose(LiquidResourceSettings s,float progress)
    {
        float p=Mathf.Clamp01(progress);
        return new Vector4(s.dropDistance*(p>=1?0:1-(.16f*p+.84f*Mathf.Pow(p,2.3f))),Mathf.Lerp(s.cardStartRoll,0,p),Mathf.Lerp(1,.54f,p*p),
            1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1,p)));
    }
    public static void PlaceBehindHand(Transform host,Transform hand)
    {
        // Share the hand's parent: this puts the resource above the board background
        // while keeping every resource effect below ordinary hand cards.
        if(host==null || hand==null || host==hand || hand.IsChildOf(host))return;
        var handCanvas=hand.GetComponentInParent<Canvas>();
        foreach(var canvas in host.GetComponentsInChildren<Canvas>(true))
        {
            canvas.overrideSorting=false;
            if(handCanvas!=null){canvas.sortingLayerID=handCanvas.sortingLayerID;canvas.sortingOrder=handCanvas.sortingOrder;}
        }
        if(host.parent!=hand.parent)host.SetParent(hand.parent,true);
        int index=hand.GetSiblingIndex();if(host.GetSiblingIndex()<index)index--;
        if(host.GetSiblingIndex()!=index)host.SetSiblingIndex(index);
    }
    Transform layerHand;
    void LateUpdate()
    {
        if(!initialized || Settings==null || !Settings.behindHand || transform.parent==null)return;
        if(layerHand==null)
        {
            var dealer=FindFirstObjectByType<HandDealer>();
            if(dealer!=null)layerHand=dealer.handArea;
        }
        if(layerHand!=null)PlaceBehindHand(transform.parent,layerHand);
    }
    public void PlaceEffectBehindHand(Transform effect)
    {
        if(effect==null)return;
        effect.SetParent(transform.parent,true);
        effect.SetSiblingIndex(transform.GetSiblingIndex());
        var parentCanvas=transform.parent.GetComponentInParent<Canvas>();
        foreach(var c in effect.GetComponentsInChildren<Canvas>(true))
        {
            c.overrideSorting=false;
            if(parentCanvas!=null){c.sortingLayerID=parentCanvas.sortingLayerID;c.sortingOrder=parentCanvas.sortingOrder;}
        }
    }
    void EnsureFont()
    {
        if(Settings.sourceFont!=null && activeSource!=Settings.sourceFont)
        {
            activeSource=Settings.sourceFont;
            if(runtimeFont!=null)Destroy(runtimeFont);
            runtimeFont=TMP_FontAsset.CreateFontAsset(activeSource,90,12,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024);
            runtimeFont.hideFlags=HideFlags.HideAndDontSave;
        }
        var font=Settings.numberFont!=null?Settings.numberFont:runtimeFont;
        if(font==null)return;
        foreach(var text in new[]{digits,oldDigits,maxDigits,oldMaxDigits})
        {
            if(text.font==font)continue;
            var previous=text.fontSharedMaterial;
            text.font=font;text.fontSharedMaterial=new Material(font.material){hideFlags=HideFlags.HideAndDontSave};
            if(previous!=digitMaterial && previous!=oldMaterial && previous!=null && previous.hideFlags==HideFlags.HideAndDontSave)Destroy(previous);
            var m=text.fontSharedMaterial;m.EnableKeyword("UNDERLAY_ON");m.SetFloat("_OutlineWidth",.025f);
        }
    }
    void OnDisable(){if(fallingCard!=null)Destroy(fallingCard.gameObject);fallingCard=null;}
    void OnDestroy()
    {
        foreach(var text in new[]{digits,oldDigits,maxDigits,oldMaxDigits})
            if(text!=null && text.fontSharedMaterial!=null && text.fontSharedMaterial!=digitMaterial && text.fontSharedMaterial!=oldMaterial && text.fontSharedMaterial.hideFlags==HideFlags.HideAndDontSave) {if(Application.isPlaying)Destroy(text.fontSharedMaterial);else DestroyImmediate(text.fontSharedMaterial);}
        if(runtimeFont!=null){if(Application.isPlaying)Destroy(runtimeFont);else DestroyImmediate(runtimeFont);}
        if(digitMaterial!=null){if(Application.isPlaying)Destroy(digitMaterial);else DestroyImmediate(digitMaterial);}
        if(oldMaterial!=null){if(Application.isPlaying)Destroy(oldMaterial);else DestroyImmediate(oldMaterial);}
    }
}
