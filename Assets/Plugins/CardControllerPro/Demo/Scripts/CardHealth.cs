using System;
using UnityEngine;
using UnityEngine.UI;

namespace CCP
{

/// <summary>
/// Tracks runtime health for a card. Add this component to the Card prefab.
/// Call Initialize(maxHealth) after spawning, or it will self-initialize on Start with maxHealth.
/// </summary>
[RequireComponent(typeof(Card))]
public class CardHealth : MonoBehaviour {
    public static Action<CardHealth> OnCardDied;

    public event Action<int, int> OnHealthChanged; // (currentHealth, maxHealth)
    public event Action OnDied;

    [SerializeField] private int maxHealth = 10;
    [SerializeField] private Text healthDisplay;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    private bool initialized;

    private void Start() {
        if (!initialized)
            Initialize(maxHealth);
    }

    public void Initialize(int max) {
        maxHealth = max;
        CurrentHealth = max;
        initialized = true;
        UpdateHealthDisplay();
    }

    public void TakeDamage(int amount) {
        if (IsDead || amount <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        UpdateHealthDisplay();

        if (IsDead)
            Die();
    }

    public void Heal(int amount) {
        if (IsDead || amount <= 0) return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        UpdateHealthDisplay();
    }

    public void SetHealth(int value) {
        if (IsDead) return;

        CurrentHealth = Mathf.Clamp(value, 0, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        UpdateHealthDisplay();

        if (IsDead)
            Die();
    }

    private void Die() {
        OnDied?.Invoke();
        OnCardDied?.Invoke(this);

        Card card = GetComponent<Card>();
        card.parentCardGroup?.RemoveCard(card);
        Destroy(gameObject);
    }

    public void UpdateHealthDisplay() {
        if (healthDisplay != null) {
            healthDisplay.text = CurrentHealth + "/" + maxHealth;
        }
    }
    
    public void StartDisplayingHealth() {
        if (healthDisplay != null) {
            healthDisplay.gameObject.SetActive(true);
            UpdateHealthDisplay();
        }
    }
}

}
