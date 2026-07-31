using UnityEngine;

public class InputLockManager : MonoBehaviour
{
    public static InputLockManager I { get; private set; }

    [Header("通常操作を止める透明ブロッカー")]
    [SerializeField]
    private GameObject inputBlocker;

    public bool IsLocked { get; private set; }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        SetLocked(false);
    }

    public void LockInput()
    {
        SetLocked(true);
    }

    public void UnlockInput()
    {
        SetLocked(false);
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;

        if (inputBlocker != null)
        {
            inputBlocker.SetActive(locked);
        }
    }
}