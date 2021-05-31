using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRayTool : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //making sure the object has a renderer
        if(GetComponent<Renderer>())
        {
            //This next line queues the object’s render order at 3002.
            GetComponent<Renderer>().material.renderQueue = 3002; //set renderQueue to render after our Invisible mask(3001)
        }

    }
}


