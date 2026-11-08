using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Shared by both wall damage paths, including suppressed legacy slashes and two-wall breaks.
public static class WallDestructionPresentation
{
    public static IEnumerator Play(HandDealer owner,IEnumerable<GameObject> requested)
    {
        var s=WallDestructionSettings.Load();if(s==null||!s.enabledEffect||!s.IsValid)yield break;
        var walls=new List<GameObject>();foreach(var wall in requested)if(wall!=null&&!walls.Contains(wall))walls.Add(wall);
        if(walls.Count==0)yield break;
        var source=walls[0].GetComponentInParent<Canvas>();if(source==null)yield break;source=source.rootCanvas;
        var groups=new List<CanvasGroup>();var alphas=new List<float>();var visuals=new List<WallDestructionGraphic>();
        var layoutHolds=new List<System.IDisposable>();var layouts=new HashSet<WallAreaLayout>();
        RectTransform root=null;System.IDisposable focusHold=null;var lease=PresentationSequenceLock.Acquire(owner);
        try
        {
            lease.PauseGame();
            var attack=owner.turnManager!=null?owner.turnManager.GetComponent<AttackMotionPresentation>():null;
            focusHold=attack!=null?attack.HoldWallDestructionFocus():null;
            // Keep alpha-hidden broken walls registered until the caller commits removal.
            foreach(var wall in walls)
            {
                var layout=wall!=null?wall.GetComponentInParent<WallAreaLayout>():null;
                if(layout!=null&&layouts.Add(layout))layoutHolds.Add(layout.HoldPresentationLayout());
            }
            var go=new GameObject("AE wall destruction foreground",typeof(RectTransform),typeof(Canvas));go.layer=source.gameObject.layer;go.transform.SetParent(source.transform,false);root=(RectTransform)go.transform;
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            var canvas=go.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingLayerID=source.sortingLayerID;canvas.sortingOrder=source.sortingOrder+s.sortingOffset;
            foreach(var wall in walls)
            {
                if(wall==null)continue;
                var card=wall.GetComponentInChildren<CardController>();var art=card!=null&&card.artworkImage!=null?card.artworkImage.rectTransform:wall.GetComponent<RectTransform>();if(art==null)continue;
                var corners=new Vector3[4];art.GetWorldCorners(corners);var bottom=root.InverseTransformPoint(corners[0]);var top=root.InverseTransformPoint(corners[1]);var center=root.InverseTransformPoint((corners[0]+corners[2])*.5f);
                bool enemy=owner.enemyWallArea!=null&&wall.transform.IsChildOf(owner.enemyWallArea);
                if(s.backdropEnabled&&s.backdropOpacity>0)
                {
                    var backdrop=new GameObject("Wall destruction gradient backdrop",typeof(RectTransform),typeof(CanvasRenderer),typeof(WallDestructionBackdropGraphic));backdrop.layer=go.layer;backdrop.transform.SetParent(root,false);backdrop.transform.SetAsFirstSibling();
                    var br=(RectTransform)backdrop.transform;float cardHeight=Vector3.Distance(bottom,top)*s.size;
                    br.localPosition=center+(Vector3)(enemy?s.enemyOffset:s.playerOffset)+(Vector3)(s.backdropOffset*cardHeight);
                    br.localRotation=Quaternion.Euler(0,0,s.angle);
                    br.sizeDelta=Vector2.Scale(Vector2.Scale(s.sourceSize*(cardHeight/Mathf.Max(1,s.sourceCardHeight)),s.sizeMultiplier),s.backdropSizeMultiplier);
                    backdrop.GetComponent<WallDestructionBackdropGraphic>().Configure(s);
                }
                if(s.shadowEnabled&&s.shadowOpacity>0)
                {
                    var shade=new GameObject("Wall destruction soft shadow",typeof(RectTransform),typeof(CanvasRenderer),typeof(WallDestructionGraphic));shade.layer=go.layer;shade.transform.SetParent(root,false);shade.transform.SetSiblingIndex(root.GetComponentsInChildren<WallDestructionBackdropGraphic>().Length);
                    var sr=(RectTransform)shade.transform;sr.localPosition=center+(Vector3)(enemy?s.enemyOffset:s.playerOffset)+(Vector3)(s.shadowOffset*Vector3.Distance(bottom,top)*s.size);
                    sr.localRotation=Quaternion.Euler(0,0,s.angle);sr.sizeDelta=Vector2.Scale(s.sourceSize*(Vector3.Distance(bottom,top)/Mathf.Max(1,s.sourceCardHeight)*s.size),s.sizeMultiplier);
                    var shadow=shade.GetComponent<WallDestructionGraphic>();shadow.ConfigureShadow();shadow.Sample(s,0);visuals.Add(shadow);
                }
                var effect=new GameObject("Platinum wall fragments",typeof(RectTransform),typeof(CanvasRenderer),typeof(WallDestructionGraphic));effect.layer=go.layer;effect.transform.SetParent(root,false);
                var rt=(RectTransform)effect.transform;rt.localPosition=center+(Vector3)(enemy?s.enemyOffset:s.playerOffset);rt.localRotation=Quaternion.Euler(0,0,s.angle);
                float scale=Vector3.Distance(bottom,top)/Mathf.Max(1,s.sourceCardHeight)*s.size;
                rt.sizeDelta=Vector2.Scale(s.sourceSize*scale,s.sizeMultiplier);
                var graphic=effect.GetComponent<WallDestructionGraphic>();graphic.Sample(s,0);visuals.Add(graphic);
                var group=wall.GetComponent<CanvasGroup>();if(group==null)group=wall.AddComponent<CanvasGroup>();groups.Add(group);alphas.Add(group.alpha);group.alpha=0;
            }
            float duration=(s.frames.Length-s.impactFrame)/Mathf.Max(1,s.frameRate);
            for(float age=0;age<duration+Mathf.Max(0,s.endHold);age+=Time.unscaledDeltaTime*Mathf.Max(.05f,s.speed))
            {
                if(owner==null||!owner.isActiveAndEnabled)yield break;
                foreach(var visual in visuals)if(visual!=null)visual.Sample(s,Mathf.Min(age,duration));
                yield return null;
            }
        }
        finally
        {
            // The caller commits wall removal immediately after return; cancellation restores visuals.
            for(int i=0;i<groups.Count;i++)if(groups[i]!=null)groups[i].alpha=alphas[i];
            if(root!=null)Object.Destroy(root.gameObject);
            foreach(var hold in layoutHolds)hold.Dispose();
            focusHold?.Dispose();
            lease.Dispose();
        }
    }
}
