using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class NineMeteorEffect : MaskableGraphic
{
    float age, duration;
    bool enemy;
    AudioSource sound;
    public void Initialize(float seconds, bool fromEnemy, AudioClip clip, float volume)
    {
        duration = seconds;
        enemy = fromEnemy;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = rectTransform.offsetMax = Vector2.zero;
        Canvas parent = GetComponentInParent<Canvas>();
        Canvas overlay = gameObject.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = (parent != null ? parent.sortingOrder : 0) + 110;
        gameObject.AddComponent<GraphicRaycaster>();
        raycastTarget = true;
        sound = gameObject.AddComponent<AudioSource>();
        sound.playOnAwake = false;
        sound.spatialBlend = 0f;
        if(clip != null) sound.PlayOneShot(clip, volume);
    }
    void Update() { age += Time.unscaledDeltaTime; SetVerticesDirty(); }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float fade = Mathf.Clamp01(age / 0.2f) * Mathf.Clamp01((duration - age) / 0.4f);
        Quad(vh, new Vector2(r.xMin,r.yMin), new Vector2(r.xMax,r.yMin), new Vector2(r.xMax,r.yMax), new Vector2(r.xMin,r.yMax), new Color(0.015f,0.035f,0.12f,0.32f*fade), new Color(0.015f,0.035f,0.12f,0.32f*fade));
        Color head = enemy ? new Color(1f,0.85f,0.55f,fade) : new Color(0.65f,0.95f,1f,fade);
        Vector2 direction = new Vector2(-0.5f,-1f).normalized;
        Vector2 normal = new Vector2(-direction.y,direction.x);
        for(int i=0;i<36;i++)
        {
            float phase = age * (0.8f + (i%5)*0.08f) - i*0.037f;
            if(phase < 0f) continue;
            phase = Mathf.Repeat(phase,1.5f);
            float x = Mathf.Repeat(i*0.618034f,1f);
            Vector2 p = new Vector2(r.xMin + x*r.width + r.width*0.35f - phase*r.width*0.55f, r.yMax + r.height*0.15f - phase*r.height);
            Vector2 tail = p - direction * (r.height * (0.07f + i%4*0.012f));
            float width = Mathf.Max(2f,r.width*0.003f)*(1+i%3*0.3f);
            Color end = new Color(head.r,head.g,head.b,0f);
            Quad(vh,p+normal*width,p-normal*width,tail-normal*width*0.2f,tail+normal*width*0.2f,head,end);
            Quad(vh,p+Vector2.up*width*2,p+Vector2.right*width,p-Vector2.up*width*2,p-Vector2.right*width,Color.white*fade,Color.white*fade);
        }
    }
    static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color front, Color back)
    {
        int i=vh.currentVertCount;
        vh.AddVert(a,front,Vector2.zero); vh.AddVert(b,front,Vector2.zero);
        vh.AddVert(c,back,Vector2.zero); vh.AddVert(d,back,Vector2.zero);
        vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
    }
}
