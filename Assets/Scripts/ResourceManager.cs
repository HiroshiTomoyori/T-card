using TMPro;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public TextMeshProUGUI resourceText;
    
    public int currentResource = 0;
    int maxResource = 0;

    void Start()
    {
        UpdateUI();
    }

    public void AddResource()
    {
        Debug.Log("ResourceManager AddResource 呼ばれた");

        maxResource += 1;
        currentResource += 1;

        UpdateUI();
    }

    void UpdateUI()
    {
        Debug.Log("Resource UI 更新: " + currentResource + " / " + maxResource);

        if (resourceText != null)
        {
            resourceText.text =
                currentResource + " / " + maxResource;
        }
        else
        {
            Debug.LogError("ResourceText が未設定");
        }
    }

    public void UseResource(int amount)
    {
        currentResource -= amount;

        if(currentResource < 0)
            currentResource = 0;

        UpdateUI();
    }

        public void RecoverResource()
    {
        currentResource = maxResource;
        UpdateUI();
    }
}