using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetColor : MonoBehaviour
{
    

    public void SetMarkerGreen()
    {
        GetComponent<Renderer>().material.SetColor("_Color", Color.green);
        GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.green);
    }

    public void SetMarkerRed()
    {
        GetComponent<Renderer>().material.SetColor("_Color", Color.red);
        GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.red);
    }
       
}
