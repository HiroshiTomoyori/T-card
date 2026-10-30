using System;
using UnityEngine;
[Serializable] public sealed class PlayModeEditProbeNode {public float value;public string text;}
public sealed class PlayModeEditProbeAsset:ScriptableObject
{
    public float speed=1;
    public int counter=7;
    public Color tint=Color.white;
    public Vector3 offset=Vector3.zero;
    public float[] values={1,2};
    public AnimationCurve curve=AnimationCurve.Linear(0,0,1,1);
    public Gradient gradient=new Gradient();
    [SerializeReference]public PlayModeEditProbeNode node=new PlayModeEditProbeNode{value=1,text="original"};
}
