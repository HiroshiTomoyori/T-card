using System;
using System.Collections.Generic;
using UnityEngine;
// A temporary rendering layer; never adds an input raycaster or reparents gameplay cards.
public sealed class OpeningDealLayer : MonoBehaviour, IDisposable
{
    struct State {public Canvas canvas;public bool sorting;public int order,layer;}
    readonly List<State> saved=new List<State>();
    Canvas added;bool restored;
    public static OpeningDealLayer Begin(GameObject card,RectTransform deck,OpeningShuffleSettings settings)
    {
        var scope=card.AddComponent<OpeningDealLayer>();
        var root=card.GetComponentInParent<Canvas>();
        var stack=OpeningShuffleView.ForDeck(deck);
        int order=stack!=null && stack.OverlayCanvas!=null?stack.OverlayCanvas.sortingOrder:root!=null?root.sortingOrder:0;
        int layer=stack!=null && stack.OverlayCanvas!=null?stack.OverlayCanvas.sortingLayerID:root!=null?root.sortingLayerID:0;
        int target=order+Mathf.Max(1,settings!=null?settings.dealSortingOffset:5);
        if(card.GetComponent<Canvas>()==null)scope.added=card.AddComponent<Canvas>();
        foreach(var canvas in card.GetComponentsInChildren<Canvas>(true))
        {
            if(canvas!=scope.added)scope.saved.Add(new State{canvas=canvas,sorting=canvas.overrideSorting,order=canvas.sortingOrder,layer=canvas.sortingLayerID});
            canvas.overrideSorting=true;canvas.sortingLayerID=layer;canvas.sortingOrder=Mathf.Max(target,canvas.sortingOrder);
        }
        return scope;
    }
    void Restore()
    {
        if(restored)return;restored=true;
        foreach(var state in saved)if(state.canvas!=null){state.canvas.overrideSorting=state.sorting;state.canvas.sortingOrder=state.order;state.canvas.sortingLayerID=state.layer;}
        if(added!=null){added.overrideSorting=false;if(Application.isPlaying)Destroy(added);else DestroyImmediate(added);added=null;}
    }
    public void Dispose(){Restore();if(this!=null){if(Application.isPlaying)Destroy(this);else DestroyImmediate(this);}}
    void OnDisable(){Restore();}
    void OnDestroy(){Restore();}
}
