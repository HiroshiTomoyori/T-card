using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class KeepPlayModeEdits
{
    const string Menu="Tools/T-card/Keep Play Mode Edits";
    static readonly string Prefix="TCard.PlayEdits."+Hash128.Compute(Application.dataPath)+".";
    [Serializable] sealed class Identity
    {
        public string key,gid,scenePath,hierarchy,componentType;
        public int sceneIndex,componentIndex;
    }
    [Serializable] sealed class Mapping {public int instance;public string key;}
    [Serializable] sealed class Change
    {
        public string key,path;
        public int instance;
        public PlayModeEditValue.Value value;
    }
    [Serializable] sealed class Journal
    {
        public List<Identity> targets=new List<Identity>();
        public List<Mapping> playObjects=new List<Mapping>();
        public List<Change> changes=new List<Change>();
        public bool awaitingApply;
    }
    static Journal journal;
    static bool applying,dirty;
    static double lastWrite;
    public static int PendingCount=>State.changes.Count;
    public static int LastAppliedCount=>SessionState.GetInt(Prefix+"Applied",0);
    public static bool Enabled {get=>EditorPrefs.GetBool(Prefix+"Enabled",true);set{EditorPrefs.SetBool(Prefix+"Enabled",value);if(!value){State.changes.Clear();Save();}}}
    static Journal State=>journal??(journal=JsonUtility.FromJson<Journal>(SessionState.GetString(Prefix+"Journal","{}"))??new Journal());
    static KeepPlayModeEdits()
    {
        Undo.postprocessModifications+=OnModifications;
        Undo.undoRedoPerformed+=OnUndoRedo;
        EditorApplication.playModeStateChanged+=OnPlayMode;
        AssemblyReloadEvents.beforeAssemblyReload+=Save;
        EditorApplication.update+=Flush;
        EditorApplication.delayCall+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode&&State.awaitingApply)ApplyPending();};
    }
    [MenuItem(Menu)] static void Toggle(){Enabled=!Enabled;Debug.Log("[T-card] Keep Play Mode Edits: "+(Enabled?"ON":"OFF"));}
    [MenuItem(Menu,true)] static bool ValidateMenu(){UnityEditor.Menu.SetChecked(Menu,Enabled);return true;}
    static void Flush(){if(dirty&&EditorApplication.timeSinceStartup-lastWrite>.25)Save();}
    static void Save(){if(journal==null)return;SessionState.SetString(Prefix+"Journal",JsonUtility.ToJson(journal));dirty=false;lastWrite=EditorApplication.timeSinceStartup;}
    static string GlobalKey(Object target)
    {
        var id=GlobalObjectId.GetGlobalObjectIdSlow(target);
        return id.identifierType!=0&&id.targetObjectId!=0&&id.assetGUID.ToString()!=new string('0',32)?id.ToString():null;
    }
    static GameObject GameObjectOf(Object target)=>target is GameObject go?go:(target as Component)?.gameObject;
    static bool SceneObject(Object target)
    {
        var go=GameObjectOf(target);return go!=null&&go.scene.IsValid()&&go.scene.isLoaded&&!EditorUtility.IsPersistent(go)&&!EditorSceneManager.IsPreviewScene(go.scene);
    }
    static IEnumerable<Object> SceneObjects()
    {
        foreach(var go in Resources.FindObjectsOfTypeAll<GameObject>())if(SceneObject(go))
        {yield return go;foreach(var c in go.GetComponents<Component>())if(c!=null)yield return c;}
    }
    static Identity SceneIdentity(Object target)
    {
        var go=GameObjectOf(target);if(go==null)return null;
        int sceneIndex=0;for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i)==go.scene){sceneIndex=i;break;}
        var indexes=new List<int>();for(var t=go.transform;t!=null;t=t.parent)indexes.Add(t.GetSiblingIndex());indexes.Reverse();
        string type="";int ordinal=0;
        if(target is Component component){type=component.GetType().AssemblyQualifiedName;ordinal=Array.IndexOf(go.GetComponents(component.GetType()),component);}
        string gid=GlobalKey(target),hierarchy=string.Join("/",indexes);
        return new Identity{gid=gid,scenePath=go.scene.path,sceneIndex=sceneIndex,hierarchy=hierarchy,componentType=type,componentIndex=ordinal,key=gid??("scene:"+go.scene.path+":"+sceneIndex+":"+hierarchy+":"+type+":"+ordinal)};
    }
    static void OnPlayMode(PlayModeStateChange change)
    {
        if(change==PlayModeStateChange.ExitingEditMode)
        {
            journal=new Journal();SessionState.SetInt(Prefix+"Applied",0);
            if(Enabled)foreach(var target in SceneObjects())State.targets.Add(SceneIdentity(target));
            Save();
        }
        else if(change==PlayModeStateChange.EnteredPlayMode)
        {
            State.playObjects.Clear();
            var keys=new HashSet<string>(State.targets.Select(x=>x.key));
            foreach(var target in SceneObjects()){var id=SceneIdentity(target);if(keys.Contains(id.key))State.playObjects.Add(new Mapping{instance=target.GetInstanceID(),key=id.key});}
            Save();
        }
        else if(change==PlayModeStateChange.ExitingPlayMode)
        {
            Undo.FlushUndoRecordObjects();State.awaitingApply=Enabled&&State.changes.Count>0;Save();
        }
        else if(change==PlayModeStateChange.EnteredEditMode&&State.awaitingApply)EditorApplication.delayCall+=ApplyPending;
    }
    static string Identify(Object target)
    {
        if(target==null)return "";
        if(EditorUtility.IsPersistent(target))
        {
            string path=AssetDatabase.GetAssetPath(target);if(!path.StartsWith("Assets/",StringComparison.Ordinal))return null;
            string gid=GlobalKey(target);return gid==null?null:"asset:"+gid;
        }
        return State.playObjects.FirstOrDefault(x=>x.instance==target.GetInstanceID())?.key;
    }
    static bool SupportedPath(string path)
    {
        return !string.IsNullOrEmpty(path)&&path!="m_Script"&&path!="m_Father"&&path!="m_RootOrder"&&!path.StartsWith("m_Children")&&!path.StartsWith("m_Component")&&!path.StartsWith("m_Prefab")&&path!="m_ObjectHideFlags";
    }
    static UndoPropertyModification[] OnModifications(UndoPropertyModification[] changes)
    {
        if(!Enabled||applying||!EditorApplication.isPlaying)return changes;
        foreach(var modification in changes)
        {
            var current=modification.currentValue;if(current?.target==null||!SupportedPath(current.propertyPath))continue;
            string key=Identify(current.target);if(key==null)continue;
            try{Capture(current.target,key,current.propertyPath);}catch(Exception e){Debug.LogWarning("[T-card] Could not keep "+current.propertyPath+": "+e.Message);}
        }
        dirty=true;return changes;
    }
    static void Capture(Object target,string key,string path)
    {
        using(var so=new SerializedObject(target))
        {
            var property=so.FindProperty(path);
            while(property==null&&path.Contains(".")){path=path.Substring(0,path.LastIndexOf('.'));property=so.FindProperty(path);}
            if(property==null)return;
            if(property.propertyType==SerializedPropertyType.ArraySize)
            {int end=path.LastIndexOf(".Array.size",StringComparison.Ordinal);if(end>=0)property=so.FindProperty(path.Substring(0,end));}
            AddValue(target,key,property);
            if(property.propertyType==SerializedPropertyType.Generic||property.propertyType==SerializedPropertyType.ManagedReference)
            {
                var iterator=property.Copy();var end=property.GetEndProperty();bool enter=true;
                while(iterator.Next(enter)&&!SerializedProperty.EqualContents(iterator,end))
                {AddValue(target,key,iterator);enter=iterator.propertyType==SerializedPropertyType.Generic||iterator.propertyType==SerializedPropertyType.ManagedReference;}
            }
        }
    }
    static void AddValue(Object target,string key,SerializedProperty property)
    {
        var value=PlayModeEditValue.Read(property,Identify);if(value==null)return;
        var change=State.changes.FirstOrDefault(x=>x.key==key&&x.path==property.propertyPath);
        if(change==null){change=new Change{key=key,path=property.propertyPath};State.changes.Add(change);}
        change.instance=target.GetInstanceID();change.value=value;
    }
    static void OnUndoRedo()
    {
        if(!Enabled||applying||!EditorApplication.isPlaying)return;
        foreach(var edit in State.changes.ToArray())
        {
            var target=EditorUtility.InstanceIDToObject(edit.instance);if(target==null)continue;
            using(var so=new SerializedObject(target)){var p=so.FindProperty(edit.path);if(p==null){State.changes.Remove(edit);continue;}var value=PlayModeEditValue.Read(p,Identify);if(value!=null)edit.value=value;}
        }
        dirty=true;
    }
    static Object Resolve(string key)
    {
        if(string.IsNullOrEmpty(key))return null;
        string gid=key.StartsWith("asset:",StringComparison.Ordinal)?key.Substring(6):key;
        if(GlobalObjectId.TryParse(gid,out var id)){var found=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);if(found!=null)return found;}
        var record=State.targets.FirstOrDefault(x=>x.key==key);if(record==null)return null;
        Scene scene=default;
        for(int i=0;i<SceneManager.sceneCount;i++)
        {var candidate=SceneManager.GetSceneAt(i);if(!string.IsNullOrEmpty(record.scenePath)?candidate.path==record.scenePath:i==record.sceneIndex){scene=candidate;break;}}
        if(!scene.IsValid()||!scene.isLoaded)return null;
        var indexes=record.hierarchy.Split('/').Select(int.Parse).ToArray();var roots=scene.GetRootGameObjects();
        if(indexes.Length==0||indexes[0]>=roots.Length)return null;
        Transform t=roots[indexes[0]].transform;
        for(int i=1;i<indexes.Length;i++){if(indexes[i]>=t.childCount)return null;t=t.GetChild(indexes[i]);}
        if(string.IsNullOrEmpty(record.componentType))return t.gameObject;
        var type=Type.GetType(record.componentType);if(type==null)return null;var components=t.GetComponents(type);
        return record.componentIndex<components.Length?components[record.componentIndex]:null;
    }
    static void ApplyPending()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=ApplyPending;return;}
        if(!State.awaitingApply||!Enabled)return;
        applying=true;int applied=0,skipped=0;Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Keep Play Mode Inspector Edits");
        try
        {
            foreach(var edits in State.changes.GroupBy(x=>x.key))
            {
                var target=Resolve(edits.Key);if(target==null){skipped+=edits.Count();continue;}
                Undo.RecordObject(target,"Keep Play Mode Inspector Edits");
                using(var so=new SerializedObject(target))
                {
                    // Resize arrays/create managed reference instances before assigning their fields.
                    var ordered=edits.OrderBy(x=>x.path.Count(c=>c=='.')).ThenBy(x=>x.value.type==SerializedPropertyType.ArraySize?0:1).ToArray();
                    foreach(var edit in ordered)
                    {
                        try{var property=so.FindProperty(edit.path);if(property==null||!PlayModeEditValue.Write(property,edit.value,Resolve)){skipped++;continue;}so.ApplyModifiedPropertiesWithoutUndo();applied++;}
                        catch(Exception e){skipped++;Debug.LogWarning("[T-card] Could not restore "+edit.path+": "+e.Message);}
                    }
                }
                EditorUtility.SetDirty(target);
                if(SceneObject(target)){var go=GameObjectOf(target);EditorSceneManager.MarkSceneDirty(go.scene);if(PrefabUtility.IsPartOfPrefabInstance(target))PrefabUtility.RecordPrefabInstancePropertyModifications(target);}
                else if(EditorUtility.IsPersistent(target))AssetDatabase.SaveAssetIfDirty(target);
            }
            Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);
        }
        finally
        {
            applying=false;State.awaitingApply=false;State.changes.Clear();SessionState.SetInt(Prefix+"Applied",applied);Save();
        }
        if(applied>0)Debug.Log("[T-card] Kept "+applied+" manually edited properties after Play Mode. Scene changes are unsaved; Undo is available.");
        if(skipped>0)Debug.LogWarning("[T-card] "+skipped+" properties had no editable counterpart or a changed type and were skipped.");
    }
}
