using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public sealed class AttackMotionPresentation : MonoBehaviour
{
    static int active;
    public static bool IsPlaying=>active>0;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics(){active=0;}
    public float Elapsed {get;private set;}
    public bool ImpactEmitted {get;private set;}
    public RectTransform MovingCard {get;private set;}
    public RectTransform DefendingCard {get;private set;}
    public AttackMotionFocusGraphic FocusGraphic {get;private set;}
    public int VictimCount {get;private set;}
    readonly List<AEAttackVisual> effectVisuals=new List<AEAttackVisual>();
    readonly List<CardController> effectVictims=new List<CardController>();
    public IReadOnlyList<AEAttackVisual> EffectVisuals=>effectVisuals;
    public IReadOnlyList<CardController> EffectVictims=>effectVictims;
    public bool UsesWallCross {get;private set;}
    public float ContactFrameGap {get;private set;}
    public static void BattleContactOffsets(Vector2 a,Vector2 b,Vector2 aSize,Vector2 bSize,float direction,float gap,float share,out Vector2 da,out Vector2 db)
    {
        float ha=aSize.y*.5f,hb=bSize.y*.5f;
        float x=Mathf.Lerp(a.x,b.x,share),plane=Mathf.Lerp(a.y+direction*ha,b.y-direction*hb,share);
        da=new Vector2(x,plane-direction*(ha+gap*.5f))-a;
        db=new Vector2(x,plane+direction*(hb+gap*.5f))-b;
    }
    static Rect LocalBox(RectTransform root,RectTransform target)
    {
        var corners=new Vector3[4];target.GetWorldCorners(corners);Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;
        foreach(var p in corners){Vector2 v=root.InverseTransformPoint(p);min=Vector2.Min(min,v);max=Vector2.Max(max,v);}
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    // A legacy zone RectTransform may be narrower than its manually laid-out cards.
    // Size from all visible walls, while keeping the effect centered on the zone itself.
    static float WallRowWidth(RectTransform root,RectTransform zone,Vector2 center,int referenceCount)
    {
        float extent=0;
        foreach(Transform child in zone)
        {
            var card=child.GetComponent<CardController>();var group=child.GetComponent<CanvasGroup>();
            if(card==null||!child.gameObject.activeInHierarchy||(group!=null&&group.alpha<.01f))continue;
            var target=card.artworkImage!=null?card.artworkImage.rectTransform:(RectTransform)child;
            Rect box=LocalBox(root,target);
            extent=Mathf.Max(extent,Mathf.Abs(box.xMin-center.x),Mathf.Abs(box.xMax-center.x));
        }
        var layout=zone.GetComponent<WallAreaLayout>();
        float reference=layout!=null?((Vector2)root.InverseTransformVector(zone.TransformVector(Vector3.right*layout.PresentationSpan(referenceCount)))).magnitude:LocalBox(root,zone).width;
        return Mathf.Max(1,extent*2,reference);
    }
    public static float BattleLateralFactor(float distance,float combinedHalfWidth,float requested)
    {
        float minimum=distance>0?Mathf.Clamp01(1-Mathf.Max(0,combinedHalfWidth-1)/distance):0;
        return Mathf.Max(Mathf.Clamp01(requested),minimum);
    }
    sealed class Actor
    {
        public CardController card;
        public RectTransform visual,shade;
        public TurnDrawFieldShadowGraphic shadow;
        public CanvasGroup original;
        public float alpha;
        public Vector3 center;
        public Vector2 size;
    }
    static float Smooth(float start,float end,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,Mathf.Max(start+.0001f,end),t));
    static float Forward(AttackMotionSettings s,float t)
    {
        float u=Mathf.Clamp01((t-s.liftDuration-s.anticipationDuration)/Mathf.Max(.01f,s.lungeDuration));
        return (1-Mathf.Pow(1-u,3))*(1-Smooth(s.ImpactTime+s.impactPause,s.ImpactTime+s.impactPause+s.returnDuration,t));
    }
    public static Vector3 Pose(AttackMotionSettings s,float t,float direction)
    {
        float raised=Smooth(0,s.liftDuration,t)*(1-Smooth(s.LandStart,s.LandStart+s.landDuration,t));
        float forward=Forward(s,t);
        return new Vector3(direction*(s.liftDistance*raised+s.lungeDistance*forward),Mathf.Lerp(1,s.floatingScale,raised)*Mathf.Lerp(1,s.lungeZoom,forward),raised);
    }
    public static Vector3 DefenderPose(AttackMotionSettings s,float t,float attackerDirection)
    {
        float raised=Smooth(s.defenderLiftDelay,s.defenderLiftDelay+s.defenderLiftDuration,t)*(1-Smooth(s.LandStart,s.LandStart+s.landDuration,t));
        float age=t-s.ImpactTime;
        float recoil=age>=0&&age<s.defenderRecoilDuration?Mathf.Sin(Mathf.PI*age/Mathf.Max(.01f,s.defenderRecoilDuration)):0;
        return new Vector3(-attackerDirection*s.defenderMotion*(s.defenderLiftDistance*raised+s.defenderLungeDistance*Forward(s,t)-s.defenderRecoilDistance*recoil),1+(s.defenderFloatingScale-1)*raised*s.defenderMotion,raised);
    }
    static RectTransform Make(Transform parent,string name)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);return (RectTransform)go.transform;
    }
    static void Stretch(RectTransform rt){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}
    static Actor CreateActor(RectTransform root,CardController card,string name)
    {
        var art=card.artworkImage;var corners=new Vector3[4];art.rectTransform.GetWorldCorners(corners);
        Vector3 min=root.InverseTransformPoint(corners[0]),max=root.InverseTransformPoint(corners[2]);
        var actor=new Actor{card=card,size=new Vector2(Mathf.Abs(max.x-min.x),Mathf.Abs(max.y-min.y)),center=(min+max)*.5f};
        actor.shade=Make(root,name+" field shadow");actor.shade.gameObject.AddComponent<CanvasRenderer>();actor.shadow=actor.shade.gameObject.AddComponent<TurnDrawFieldShadowGraphic>();actor.shadow.raycastTarget=false;actor.shade.sizeDelta=actor.size+Vector2.one*88;
        actor.visual=Make(root,name);actor.visual.sizeDelta=actor.size;actor.visual.localPosition=actor.center;
        var copy=actor.visual.gameObject.AddComponent<Image>();copy.sprite=art.sprite;copy.color=Color.white;copy.raycastTarget=false;copy.preserveAspect=true;
        actor.original=card.GetComponent<CanvasGroup>();if(actor.original==null)actor.original=card.gameObject.AddComponent<CanvasGroup>();actor.alpha=actor.original.alpha;actor.original.alpha=0;
        return actor;
    }
    static void Move(Actor actor,Vector3 pose,float unit,AttackMotionSettings s,float age,bool victim)
    {
        float shake=victim&&age>=0?Mathf.Sin(age*s.hitShakeFrequency)*s.hitShake*Mathf.Exp(-age*s.hitShakeDecay):0;
        actor.visual.localPosition=actor.center+new Vector3(shake,pose.x,0)*unit;actor.visual.localScale=Vector3.one*pose.y;
        actor.shade.localPosition=actor.center+(Vector3)s.shadowOffset*unit;actor.shade.localScale=Vector3.one*(1+.02f*pose.z);
        actor.shadow.Configure(actor.size*.94f,s.shadowBlur*unit,s.fieldShadow*pose.z,new Color(.12f,.08f,.04f,1));
    }
    public IEnumerator Play(TurnManager owner,CardController attacker,IList<CardController> related,IList<CardController> victims,Transform wallZone,bool enemy,bool monarch,Action impact)
    {
        while(IsPlaying)yield return null;
        if(attacker==null||attacker.artworkImage==null)yield break;
        var source=attacker.GetComponentInParent<Canvas>();if(source==null)yield break;source=source.rootCanvas;
        var s=AttackMotionSettings.Load();RectTransform root=null;var actors=new List<Actor>();Actor moving=null;
        active++;Elapsed=0;ImpactEmitted=false;VictimCount=victims.Count;effectVisuals.Clear();effectVictims.Clear();ContactFrameGap=float.NaN;
        var ae=AEAttackSettings.Load();UsesWallCross=monarch&&wallZone!=null;
        var clip=ae!=null?(UsesWallCross?ae.wallCrossClip:ae.straightClip):null;
        bool useAE=ae!=null&&ae.enabledEffect&&clip!=null&&clip.IsValid;
        float effectSpeed=ae!=null?Mathf.Max(.05f,ae.effectSpeed):1;
        float duration=useAE?Mathf.Max(s.Duration,s.ImpactTime+(clip.Duration-clip.contactTime)/effectSpeed):s.Duration;
        try
        {
            root=Make(source.transform,"Attack motion foreground");Stretch(root);
            var overlay=root.gameObject.AddComponent<Canvas>();overlay.overrideSorting=true;overlay.sortingLayerID=source.sortingLayerID;overlay.sortingOrder=source.sortingOrder+s.sortingOffset;root.gameObject.AddComponent<GraphicRaycaster>();
            var blocker=root.gameObject.AddComponent<Image>();blocker.color=Color.clear;blocker.raycastTarget=true;
            var dimRect=Make(root,"Attack execution lowlight");Stretch(dimRect);FocusGraphic=dimRect.gameObject.AddComponent<AttackMotionFocusGraphic>();
            // Defender first, attacker last: the attacker's card stays above the target's glow/artwork.
            if(wallZone==null)foreach(var card in related)
                if(card!=null&&card!=attacker&&card.artworkImage!=null&&!actors.Exists(a=>a.card==card))actors.Add(CreateActor(root,card,"Defending card visual"));
            DefendingCard=actors.Count>0?actors[0].visual:null;
            moving=CreateActor(root,attacker,"Attacking card visual");actors.Add(moving);MovingCard=moving.visual;foreach(var actor in actors)actor.shade.SetSiblingIndex(1);
            var fxRect=Make(root,"Attack impacts");Stretch(fxRect);
            AttackImpactGraphic fx=null;
            if(useAE)
            {
                if(UsesWallCross)effectVisuals.Add(new AEAttackVisual(fxRect,clip,true));
                else foreach(var victim in victims)if(victim!=null&&!effectVictims.Contains(victim))
                {effectVictims.Add(victim);effectVisuals.Add(new AEAttackVisual(fxRect,clip,false));}
            }
            else {fx=fxRect.gameObject.AddComponent<AttackImpactGraphic>();fx.raycastTarget=false;}
            float unit=root.rect.width/720f,direction=enemy?-1:1;
            var boxes=new Vector4[32];var colors=new Vector4[32];var flags=new Vector4[32];var points=new List<Vector2>();var highlighted=new HashSet<CardController>();
            var tint=monarch?s.monarchColor:s.hitColor;var targetTint=monarch?s.monarchColor:s.targetColor;
            Camera camera=source.renderMode==RenderMode.ScreenSpaceOverlay?null:source.worldCamera;
            Vector2 attackerCorrection=Vector2.zero,defenderCorrection=Vector2.zero;
            Actor defending=actors.Find(a=>a!=moving);
            if(useAE&&ae.alignBattleContact&&wallZone==null&&defending!=null)
            {
                var ap=Pose(s,s.ImpactTime,direction);var bp=DefenderPose(s,s.ImpactTime,direction);
                BattleContactOffsets((Vector2)moving.center+Vector2.up*ap.x*unit,(Vector2)defending.center+Vector2.up*bp.x*unit,
                    moving.size*ap.y,defending.size*bp.y,direction,ae.contactGap*unit,ae.attackerContactShare,out attackerCorrection,out defenderCorrection);
                float lateral=BattleLateralFactor(Mathf.Abs(moving.center.x-defending.center.x),(moving.size.x*ap.y+defending.size.x*bp.y)*.5f,ae.battleLateralMotion);
                attackerCorrection.x*=lateral;defenderCorrection.x*=lateral;
            }
            for(Elapsed=0;Elapsed<duration;Elapsed+=Time.unscaledDeltaTime*Mathf.Max(.05f,s.speed))
            {
                if(owner==null||owner.IsBattleEnded||attacker==null||!attacker.gameObject.activeInHierarchy)yield break;
                // Render the exact contact pose once even when a frame steps over its timestamp.
                float motionTime=!ImpactEmitted&&Elapsed>=s.ImpactTime?s.ImpactTime:Elapsed;
                float age=motionTime-s.ImpactTime;int count=0;
                foreach(var actor in actors)
                {
                    if(actor.card==null)continue;
                    var pose=actor==moving?Pose(s,motionTime,direction):DefenderPose(s,motionTime,direction);
                    Move(actor,pose,unit,s,age,victims.Contains(actor.card));
                }
                if(useAE&&ae.alignBattleContact&&wallZone==null&&defending!=null)
                {
                    float forward=Forward(s,motionTime);
                    moving.visual.localPosition+=(Vector3)(attackerCorrection*forward);
                    defending.visual.localPosition+=(Vector3)(defenderCorrection*forward);
                }
                foreach(var actor in actors)if(actor.card!=null)
                    Put(ScreenBox(actor.visual,camera),actor==moving?s.attackerColor:targetTint,actor==moving?s.attackerGlow:s.targetGlow,s.cardLowlightFeather,boxes,colors,flags,ref count);
                if(wallZone!=null)
                {
                    bool first=true;Rect zone=new Rect();
                    foreach(Transform child in wallZone)
                    {
                        var c=child.GetComponent<CardController>();var cg=child.GetComponent<CanvasGroup>();
                        if(c==null||!child.gameObject.activeInHierarchy||(cg!=null&&cg.alpha<.01f))continue;
                        Rect b=ScreenBox(c.artworkImage!=null?c.artworkImage.rectTransform:(RectTransform)child,camera);
                        zone=first?b:Rect.MinMaxRect(Mathf.Min(zone.xMin,b.xMin),Mathf.Min(zone.yMin,b.yMin),Mathf.Max(zone.xMax,b.xMax),Mathf.Max(zone.yMax,b.yMax));first=false;
                    }
                    if(!first){float pad=s.wallPadding*Screen.width/720f;zone.xMin-=pad;zone.yMin-=pad;zone.xMax+=pad;zone.yMax+=pad;Put(zone,targetTint,s.wallGlow,s.wallLowlightFeather,boxes,colors,flags,ref count,true);}
                    // Zone focus keeps the full wall row lit; only the chosen cards receive red outlines.
                    highlighted.Clear();
                    foreach(var card in related)
                    {
                        if(card==null||!card.gameObject.activeInHierarchy||!card.transform.IsChildOf(wallZone)||!highlighted.Add(card))continue;
                        var group=card.GetComponent<CanvasGroup>();if(group!=null&&group.alpha<.01f)continue;
                        var artwork=card.artworkImage!=null?card.artworkImage.rectTransform:(RectTransform)card.transform;
                        Put(ScreenBox(artwork,camera),s.wallTargetColor,s.wallTargetGlow,s.cardLowlightFeather,boxes,colors,flags,ref count,false,true);
                    }
                }
                float opacity=Smooth(0,s.fadeIn,Elapsed)*(1-Smooth(duration-s.fadeOut,duration,Elapsed));
                FocusGraphic.Configure(s,boxes,colors,flags,count,opacity);
                points.Clear();foreach(var victim in victims)if(victim!=null)
                {
                    var visual=actors.Find(a=>a.card==victim);
                    var target=visual!=null?visual.visual:victim.artworkImage!=null?victim.artworkImage.rectTransform:(RectTransform)victim.transform;
                    points.Add(root.InverseTransformPoint(target.TransformPoint(target.rect.center)));
                }
                if(useAE)
                {
                    float sample=clip.contactTime+age*effectSpeed;
                    if(wallZone==null&&ae.battleStartAtContactFrame&&age<0)sample=-1;
                    var palette=ae.Colors(monarch,UsesWallCross);
                    if(UsesWallCross)
                    {
                        var zone=wallZone as RectTransform;
                        if(zone!=null)
                        {
                            Vector2 center=root.InverseTransformPoint(zone.TransformPoint(zone.rect.center));
                            float width=WallRowWidth(root,zone,center,ae.wallReferenceCount)*ae.wallWidthMultiplier*s.hitScale;
                            var size=new Vector2(width*ae.wallSizeMultiplier.x,width*clip.sourceSize.y/clip.sourceSize.x*ae.wallSizeMultiplier.y);
                            effectVisuals[0].Sample(center+ae.wallOffset*unit,size,sample,palette,ae,s.hitStrength);
                        }
                    }
                    else for(int i=0;i<effectVisuals.Count;i++)
                    {
                        var victim=effectVictims[i];if(victim==null)continue;
                        var actor=actors.Find(a=>a.card==victim);
                        var target=actor!=null?actor.visual:victim.artworkImage!=null?victim.artworkImage.rectTransform:victim.transform as RectTransform;
                        if(target==null)continue;Rect box=LocalBox(root,target);
                        float height=box.height*ae.cardHeightMultiplier*s.hitScale;
                        var size=new Vector2(height*clip.sourceSize.x/clip.sourceSize.y*ae.cardSizeMultiplier.x,height*ae.cardSizeMultiplier.y);
                        effectVisuals[i].Sample(box.center+ae.cardOffset*unit,size,sample,palette,ae,s.hitStrength);
                    }
                }
                else fx.Configure(s,points,age,tint,unit);
                if(!ImpactEmitted&&Elapsed>=s.ImpactTime)
                {
                    if(defending!=null){var a=LocalBox(root,moving.visual);var b=LocalBox(root,defending.visual);ContactFrameGap=direction>0?b.yMin-a.yMax:a.yMin-b.yMax;}
                    ImpactEmitted=true;impact?.Invoke();
                }
                yield return null;
            }
        }
        finally
        {
            foreach(var actor in actors)if(actor.original!=null)actor.original.alpha=actor.alpha;
            if(root!=null){root.gameObject.SetActive(false);Destroy(root.gameObject);}
            effectVisuals.Clear();effectVictims.Clear();MovingCard=null;DefendingCard=null;FocusGraphic=null;active=Mathf.Max(0,active-1);
        }
    }
    static Rect ScreenBox(RectTransform rt,Camera camera)
    {
        var c=new Vector3[4];rt.GetWorldCorners(c);Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;
        foreach(var p in c){var point=RectTransformUtility.WorldToScreenPoint(camera,p);min=Vector2.Min(min,point);max=Vector2.Max(max,point);}
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    static void Put(Rect box,Color tint,float power,float feather,Vector4[] boxes,Vector4[] colors,Vector4[] flags,ref int count,bool wallZone=false,bool softWall=false)
    {
        if(count>=32)return;boxes[count]=new Vector4(box.center.x,box.center.y,box.width,box.height);colors[count]=new Vector4(tint.r,tint.g,tint.b,power*tint.a);flags[count]=new Vector4(feather,1,wallZone?1:0,softWall?1:0);count++;
    }
}
