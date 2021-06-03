using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinSpawn : MonoBehaviour
{

    public GameObject botcoin;
    public int coinCount = 25;
    public GameObject atm;
    private string atmState;

    // Update is called once per frame
    void Update()
    {
        atmState = atm.GetComponent<ATM>().state.ToString();
        //Debug.Log("Coin Spawn atmState is:" + atmState);

        if (atmState == "seduced")
        {

             if(coinCount > 0)
             {
                Instantiate(botcoin, transform);
                coinCount -= 1;
             }
            
        }
        else
        {
                coinCount = 25;
        }
       
    }
}
