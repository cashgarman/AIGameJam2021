using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraStatus : MonoBehaviour
{

    public bool pwnd = false;
    private bool stateCheck;
    public GameObject marker;


    // Start is called before the first frame update
    void Start()
    {
        marker = Resources.Load<GameObject>("CamMarker");
        if(gameObject.name == "WRHS_Cam1" || gameObject.name == "WRHS_Cam2") { pwnd = true; }
        //Instantiate(marker, transform);
        stateCheck = pwnd;
        ChangeColor();
    }

    private void Update()
    {
        if(stateCheck != pwnd)
        {
            ChangeColor();
            stateCheck = pwnd;
        }
    }

    void ChangeColor()
    {
        if (pwnd)
        {
            //GetComponentInChildren<SetColor>().SetMarkerGreen();
        }
        else
        {
            //GetComponentInChildren<SetColor>().SetMarkerRed();
        };
    }
}
