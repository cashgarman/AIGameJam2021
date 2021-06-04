using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapToggle : MonoBehaviour
{
    private bool mapBool = true;
    public GameObject map;

    public void FlipMap()
    {
        if (mapBool)
        {
            map.SetActive(false);
            mapBool = false;
        }
        else
        {
            map.SetActive(true);
            mapBool = true;
        }
    }
}
