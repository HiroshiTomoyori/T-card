using TMPro;
using UnityEngine;

public class EnemyResourceManager : MonoBehaviour
{
    public TextMeshProUGUI enemyResourceText;

    public int currentResource = 0;
    public int maxResource = 0;

    void Start()
    {
        UpdateText();
    }

    public void AddResource() => AddResource(true);

    public void AddResource(bool refill)
    {
        maxResource++;

        // 現在値も最大値まで回復
        currentResource = refill ? maxResource : currentResource + 1;

        UpdateText();

        Debug.Log(
            "敵リソース：" +
            currentResource +
            "/" +
            maxResource
        );
    }

    public bool UseResource(int amount)
    {
        if(currentResource < amount)
            return false;

        currentResource -= amount;

        UpdateText();

        return true;
    }

    void UpdateText()
    {
        if(enemyResourceText != null)
        {
        enemyResourceText.text =
            currentResource +
            " / " +
            maxResource;
        }
    }

    public void RecoverResource()
    {
        currentResource = maxResource;
        UpdateText();
    }
}
