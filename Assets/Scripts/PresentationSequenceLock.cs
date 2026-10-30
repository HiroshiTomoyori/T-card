using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
// Scaled gameplay time is paused only after landing; all presentation steps run unscaled.
public static class PresentationSequenceLock
{
    static readonly List<Lease> leases=new List<Lease>();
    static readonly Dictionary<EventSystem,bool> events=new Dictionary<EventSystem,bool>();
    static int paused;
    static float previousScale=1;
    static PresentationSequenceLockHost host;
    public static bool IsLocked=>leases.Count>0;
    public sealed class Lease:IDisposable
    {
        internal UnityEngine.Object owner;internal bool dead,pausing;
        public void PauseGame(){if(dead||pausing)return;pausing=true;if(paused++==0){previousScale=Time.timeScale;Time.timeScale=0;}}
        public void Dispose(){if(dead)return;dead=true;leases.Remove(this);if(pausing&&--paused==0){var turn=UnityEngine.Object.FindFirstObjectByType<TurnManager>();if(Application.isPlaying&&Time.timeScale==0&&(turn==null||!turn.IsBattleEnded))Time.timeScale=previousScale;}if(leases.Count==0){foreach(var pair in events)if(pair.Key!=null)pair.Key.enabled=pair.Value;events.Clear();}}
    }
    public static Lease Acquire(UnityEngine.Object owner)
    {
        if(host==null){var go=new GameObject("Presentation sequence input lock");UnityEngine.Object.DontDestroyOnLoad(go);host=go.AddComponent<PresentationSequenceLockHost>();}
        var lease=new Lease{owner=owner};leases.Add(lease);Refresh();return lease;
    }
    public static void Refresh()
    {
        foreach(var lease in leases.ToArray())if(lease.owner==null||(lease.owner is Behaviour b&&!b.isActiveAndEnabled))lease.Dispose();
        if(!IsLocked)return;
        foreach(var system in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None)){if(!events.ContainsKey(system))events.Add(system,system.enabled);system.enabled=false;}
    }
    internal static void ReleaseAll(){foreach(var lease in leases.ToArray())lease.Dispose();}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){leases.Clear();events.Clear();paused=0;previousScale=1;host=null;}
}
public sealed class PresentationSequenceLockHost:MonoBehaviour
{
    void Update(){PresentationSequenceLock.Refresh();}
    void OnDestroy(){PresentationSequenceLock.ReleaseAll();}
}
