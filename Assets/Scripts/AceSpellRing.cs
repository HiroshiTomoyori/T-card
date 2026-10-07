using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public class AceSpellRing : MaskableGraphic
{
    public float phase;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Vector2 radius = rectTransform.rect.size * 0.5f;
        for(int band = 0; band < 2; band++)
        {
            float factor = band == 0 ? 1f : 0.87f;
            for(int i = 0; i < 64; i++)
            {
                if(band == 1 && i % 8 >= 5) continue;
                float a = i * Mathf.PI * 2f / 64f + phase * (band == 0 ? 0.3f : -0.55f);
                float b = (i + 1) * Mathf.PI * 2f / 64f + phase * (band == 0 ? 0.3f : -0.55f);
                Vector2 r = radius * factor;
                Vector2 inner = r - Vector2.one * 6f;
                int index = mesh.currentVertCount;
                mesh.AddVert(new Vector3(Mathf.Cos(a)*r.x, Mathf.Sin(a)*r.y), color, Vector2.zero);
                mesh.AddVert(new Vector3(Mathf.Cos(b)*r.x, Mathf.Sin(b)*r.y), color, Vector2.zero);
                mesh.AddVert(new Vector3(Mathf.Cos(b)*inner.x, Mathf.Sin(b)*inner.y), color, Vector2.zero);
                mesh.AddVert(new Vector3(Mathf.Cos(a)*inner.x, Mathf.Sin(a)*inner.y), color, Vector2.zero);
                mesh.AddTriangle(index,index+1,index+2); mesh.AddTriangle(index,index+2,index+3);
            }
        }
    }
}
