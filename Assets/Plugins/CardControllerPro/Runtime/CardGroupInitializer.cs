using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

public class CardGroupInitializer : MonoBehaviour
{
    [Serializable]
    public struct CardEntry
    {
        public GameObject cardPrefab;
        public CardData cardData;
        public int count;
    }

    [Serializable]
    public struct CardGroupEntry
    {
        public CardGroup cardGroup;
        public List<CardEntry> cards;
        public bool shuffle;
        public bool flipped;
        [Tooltip("Delay in seconds between adding each consecutive card. Set to 0 for no delay.")]
        public float delay;
        [Tooltip("Animation used when placing cards into this group. Leave as None to use InstantPlaceAnimation.")]
        [SerializeReference, SubclassSelector]
        public IPlaceAnimation placeAnimation;
    }

    public List<CardGroupEntry> groups;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        foreach (var groupEntry in groups)
        {
            if (groupEntry.cardGroup == null) continue;

            if (groupEntry.delay > 0)
            {
                StartCoroutine(InitializeGroupWithDelay(groupEntry));
            }
            else
            {
                InitializeGroup(groupEntry);
            }
        }
    }

    private void InitializeGroup(CardGroupEntry groupEntry)
    {
        foreach (var cardEntry in groupEntry.cards)
        {
            if (cardEntry.cardPrefab == null) continue;

            int count = Mathf.Max(1, cardEntry.count);
            for (int i = 0; i < count; i++)
            {
                AddCard(groupEntry, cardEntry);
            }
        }

        if (groupEntry.shuffle)
            groupEntry.cardGroup.Shuffle();
    }

    private IEnumerator InitializeGroupWithDelay(CardGroupEntry groupEntry)
    {
        bool first = true;
        foreach (var cardEntry in groupEntry.cards)
        {
            if (cardEntry.cardPrefab == null) continue;

            int count = Mathf.Max(1, cardEntry.count);
            for (int i = 0; i < count; i++)
            {
                if (!first)
                    yield return new WaitForSeconds(groupEntry.delay);
                first = false;

                AddCard(groupEntry, cardEntry);
            }
        }

        if (groupEntry.shuffle)
            groupEntry.cardGroup.Shuffle();
    }

    private void AddCard(CardGroupEntry groupEntry, CardEntry cardEntry)
    {
        GameObject cardObj = Instantiate(cardEntry.cardPrefab);
        Card card = cardObj.GetComponent<Card>();

        if (card == null)
        {
            Debug.LogError($"Prefab '{cardEntry.cardPrefab.name}' is missing a Card component.", cardObj);
            return;
        }

        card.Initialize(cardEntry.cardData);
        if (groupEntry.flipped) {
            card.GetComponent<Card>().FlipCardNoAnimation();
        }
        IPlaceAnimation anim = groupEntry.placeAnimation ?? new InstantPlaceAnimation();
        groupEntry.cardGroup.AddBack(card, anim);
    }
}

}
