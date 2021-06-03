using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraChangeController : MonoBehaviour
{
    public GameObject currentCam;
    GameObject newCam;
    GameObject staticCam;
    string startCam = "WRHS_Cam1";

    public Dropdown myDropdown;
    [SerializeField]
    private GameObject[] dropdownObjects;

    void Start()
    {
        staticCam = GameObject.Find("StaticCam");
        dropdownObjects = GameObject.FindGameObjectsWithTag("Cam"); // For example
        Populate(myDropdown, dropdownObjects);
        currentCam = staticCam;
        newCam = GameObject.Find("WRHS_Cam1");
        ChangeDisplay();
    }


    void Populate(Dropdown dropdown, GameObject[] optionsArray)
    {
        List<string> options = new List<string>();
        foreach (var option in optionsArray)
        {
            options.Add(option.name); // Or whatever you want for a label
            option.GetComponent<Camera>().enabled = false;
        }
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
    }

    public void ChangeDisplay()
    {
        newCam = GameObject.Find(myDropdown.options[myDropdown.value].text);
        //Debug.Log("Name: "+newCam.name+"Pwnd:"+newCam.GetComponent<CameraStatus>().pwnd);

        //If already on the static camera and not pwnd, do nothing
        if (!newCam.GetComponent<CameraStatus>().pwnd)
        {
            Debug.Log("Got Here");
            newCam = staticCam;  
        }
           
        if(newCam != currentCam)
        {
            currentCam.GetComponent<Camera>().enabled = false;
            newCam.GetComponent<Camera>().enabled = true;
            currentCam = newCam;
        }       
    }
}