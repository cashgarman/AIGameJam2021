using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraChangeController : MonoBehaviour
{
    private static CameraChangeController _instance;

    public Camera currentCam;
    Camera newCam;
    Camera staticCam;
    string startCam = "WRHS_Cam1";

    public Dropdown myDropdown;
    [SerializeField]
    private GameObject[] dropdownObjects;

    private Camera _mobileCamera;

    public static Camera CurrentCamera
    {
        get => _instance.currentCam;
        set => _instance.currentCam = value;
    }

    private void Awake()
    {
        _instance = this;
        _mobileCamera = Camera.main;
    }

    void Start()
    {
        staticCam = GameObject.Find("StaticCam").GetComponent<Camera>();
        dropdownObjects = GameObject.FindGameObjectsWithTag("Cam"); // For example
        Populate(myDropdown, dropdownObjects);
        currentCam = staticCam;
        newCam = GameObject.Find("WRHS_Cam1").GetComponent<Camera>();
        ChangeDisplay();
    }

    void Populate(Dropdown dropdown, GameObject[] optionsArray)
    {
        List<string> options = new List<string>
        {
            "Player Camera"
        };
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
        var cameraOption = myDropdown.options[myDropdown.value].text;
        if (cameraOption == "Player Camera")
        {
            currentCam.GetComponent<Camera>().enabled = false;
            _mobileCamera.GetComponent<Camera>().enabled = true;
            currentCam = _mobileCamera;
        }
        else
        {
            newCam = GameObject.Find(cameraOption).GetComponent<Camera>();
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
}