using UnityEngine;
using UnityEngine.EventSystems;
public sealed class FieldDropForwarder : MonoBehaviour,IDropHandler
{
    public BattleDropZone owner;
    public void OnDrop(PointerEventData data){if(owner!=null)owner.OnDrop(data);}
}
