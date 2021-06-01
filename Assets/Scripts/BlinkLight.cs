using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkLight : MonoBehaviour
{
    private float cooldown = 2;

    private void Start()
    {
        GetComponent<Renderer>().material.SetColor("_Color", Color.green);
        GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.green);
    }

    // Update is called once per frame
    void Update()
    {
        if(cooldown <= 0) //Random color change
        {
            cooldown = Random.Range(1,4);
            Blink();
        }
        cooldown -= Time.deltaTime;
        
    }

    void Blink()  //Flashes red or green
    {
        if(GetComponent<Renderer>().material.color.ToString() == "RGBA(1.000, 0.000, 0.000, 1.000)")
        {
            GetComponent<Renderer>().material.SetColor("_Color", Color.green);
            GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.green);
        }
        else
        {
            GetComponent<Renderer>().material.SetColor("_Color", Color.red);
            GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.red);
        } 
    }
}
