using UnityEngine;
using UnityEngine.UI;

namespace CCP
{

public class BalatroCardValue : MonoBehaviour
{
    public int value;
    public GameObject valueText;

    public void ShowValue() {
        // perform shake animation
        new ShakeRotation2DPickupAnimation().Pickup(GetComponent<Card>());
        valueText.GetComponent<Text>().text = "+" + value;
        valueText.SetActive(true);
    }
}

}
