using System;
using UnityEngine;

namespace CCP
{

[Serializable]
public struct PlayerSlot
{
    public CardGroup cardGroup;
    public bool flipCards;
}

public class PokerDemoCardDealer : MonoBehaviour
{
    public CardGroup sourceCardGroup;
    public PlayerSlot[] players = new PlayerSlot[4];
    public CardGroup tableCardGroup;

    private int _dealIndex;

    public void Deal()
    {
        if (sourceCardGroup.cards.Count == 0) return;

        var card = sourceCardGroup.Back();
        int totalPlayerCards = players.Length * 2;

        if (_dealIndex < totalPlayerCards)
        {
            int playerIndex = _dealIndex % players.Length;
            card.MoveToGroup(players[playerIndex].cardGroup);
            if (players[playerIndex].flipCards)
                card.FlipCard();
        }
        else
        {
            card.MoveToGroup(tableCardGroup);
            card.FlipCard();
        }

        _dealIndex++;
    }
}

}
