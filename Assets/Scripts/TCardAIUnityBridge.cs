using System.Collections.Generic;
using UnityEngine;

public static class TCardAIUnityBridge
{
    public static List<CardController> GetCards(
        Transform area
    )
    {
        List<CardController> result =
            new List<CardController>();

        if(area == null)
            return result;

        for(int i = 0; i < area.childCount; i++)
        {
            CardController card =
                area.GetChild(i)
                .GetComponent<CardController>();

            if(card != null && card.data != null)
            {
                result.Add(card);
            }
        }

        return result;
    }

    public static int CountAliveWalls(
        Transform wallArea
    )
    {
        if(wallArea == null)
            return 0;

        int count = 0;

        for(int i = 0; i < wallArea.childCount; i++)
        {
            GameObject wall =
                wallArea.GetChild(i).gameObject;

            CanvasGroup cg =
                wall.GetComponent<CanvasGroup>();

            if(cg == null || cg.alpha > 0.01f)
            {
                count++;
            }
        }

        return count;
    }
}