using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public sealed class OpeningDiscardPresentation : MonoBehaviour
{
    struct Entry{public CanvasGroup group;public float alpha;public bool interactable,raycasts;}
    readonly List<Entry> entries=new List<Entry>();
    OpeningShuffleSettings settings;Coroutine fade;
    public bool IsRevealed {get;private set;}
    public void Initialize(Transform player,Transform enemy,OpeningShuffleSettings value)
    {
        if(fade!=null)StopCoroutine(fade);fade=null;settings=value;IsRevealed=false;
        if(entries.Count==0)foreach(var target in new[]{player,enemy})
        {
            if(target==null)continue;var group=target.GetComponent<CanvasGroup>();if(group==null)group=target.gameObject.AddComponent<CanvasGroup>();
            entries.Add(new Entry{group=group,alpha=group.alpha,interactable=group.interactable,raycasts=group.blocksRaycasts});
        }
        foreach(var e in entries)if(e.group!=null){e.group.alpha=0;e.group.interactable=false;e.group.blocksRaycasts=false;}
    }
    public void Reveal(){if(IsRevealed)return;if(fade!=null)StopCoroutine(fade);fade=StartCoroutine(Fade());}
    IEnumerator Fade()
    {
        float elapsed=0,duration=settings!=null?settings.Seconds(settings.discardFadeDuration):.35f;
        foreach(var e in entries)if(e.group!=null){e.group.interactable=false;e.group.blocksRaycasts=false;}
        while(elapsed<duration)
        {
            float p=Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.001f,duration));
            foreach(var e in entries)if(e.group!=null)e.group.alpha=e.alpha*p;
            yield return null;elapsed+=settings!=null && settings.useUnscaledTime?Time.unscaledDeltaTime:Time.deltaTime;
        }
        Restore();IsRevealed=true;fade=null;
    }
    void Restore(){foreach(var e in entries)if(e.group!=null){e.group.alpha=e.alpha;e.group.interactable=e.interactable;e.group.blocksRaycasts=e.raycasts;}}
    void OnDisable(){if(fade!=null)StopCoroutine(fade);fade=null;Restore();}
}
