using UnityEngine;
[CreateAssetMenu(menuName="T-card/Defense Selection Settings")]
public sealed class DefenseSelectionSettings:ScriptableObject
{
    // Appearance is read live from AttackSelectionSettings. Legacy serialized values remain on the asset.
    [Header("位置・大きさ / 720px幅基準")]
    public Vector2 passButtonOffset=new Vector2(0,65),passButtonSize=new Vector2(260,60);
    public Vector2 confirmationOffset=new Vector2(0,105),confirmationSize=new Vector2(340,155);
    public Color buttonColor=new Color(.2f,.17f,.14f,1),buttonTextColor=new Color(1,.95f,.86f,1);
    static DefenseSelectionSettings cached;
    public static DefenseSelectionSettings Load(){if(cached!=null)return cached;cached=Resources.Load<DefenseSelectionSettings>("TCardField/DefenseSelectionSettings");if(cached==null){cached=CreateInstance<DefenseSelectionSettings>();cached.hideFlags=HideFlags.HideAndDontSave;}return cached;}
}
