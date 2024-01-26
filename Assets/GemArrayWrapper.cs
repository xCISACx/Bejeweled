using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GemArrayWrapper
{
    public Gem[,] Gems;

    public GemArrayWrapper(int width, int height)
    {
        Gems = new Gem[width, height];
    }
}
