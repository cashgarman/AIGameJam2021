using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPeds : MonoBehaviour
{
    public GameObject ped;
    private int pedCount = 40;


    // Update is called once per frame
    void Update()
    {
        if(pedCount > 0)
        {
            Instantiate(ped, transform);
            pedCount -= 1;
        }
    }
}
