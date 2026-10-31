using System;
using UnityEngine;

[CreateAssetMenu(menuName="T-card/AE Attack Clip")]
public sealed class AEAttackClip : ScriptableObject
{
    [Serializable] public sealed class Pass
    {
        public Texture2D[] frames;
        public Rect[] rectangles;
    }
    public Vector2 sourceSize;
    public float frameRate=60;
    public float contactTime=.235f;
    public int frameCount;
    public Pass[] passes; // Native compositing order: bloom, fragments, blade.
    public float Duration=>frameCount/Mathf.Max(1,frameRate);
    public bool IsValid=>passes!=null&&passes.Length==3&&frameCount>0;
}
