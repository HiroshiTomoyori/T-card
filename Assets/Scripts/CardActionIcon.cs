using UnityEngine;

public class CardActionIcon : MonoBehaviour
{
    public GameObject attackIcon;
    public GameObject blockIcon;

    public void ShowAttackIcon()
    {
        if(attackIcon != null)
            attackIcon.SetActive(true);

        if(blockIcon != null)
            blockIcon.SetActive(false);
    }

    public void ShowBlockIcon()
    {
        Debug.Log(name + " 盾表示");

        if(attackIcon != null)
            attackIcon.SetActive(false);

        if(blockIcon != null)
        {
            blockIcon.SetActive(true);

            // 最前面へ
            blockIcon.transform.SetAsLastSibling();
        }
    }

    public void HideAll()
    {
        if(attackIcon != null)
            attackIcon.SetActive(false);

        if(blockIcon != null)
            blockIcon.SetActive(false);
    }
}