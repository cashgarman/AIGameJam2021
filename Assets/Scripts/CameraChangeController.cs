using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraChangeController : MonoBehaviour
{
    GameObject currentCam;
    GameObject newCam;
    GameObject staticCam;
    public Dropdown myDropdown;
    [SerializeField]
    private GameObject[] dropdownObjects;

    void Start()
    {
        staticCam = GameObject.Find("StaticCam");
        dropdownObjects = GameObject.FindGameObjectsWithTag("Cam"); // For example
        Populate(myDropdown, dropdownObjects);
        currentCam = GameObject.Find(myDropdown.options[0].text);
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
        Debug.Log(myDropdown.options[myDropdown.value].text);
        newCam = GameObject.Find(myDropdown.options[myDropdown.value].text);
        Debug.Log(newCam.GetComponent<CameraStatus>().pwnd);

        if (newCam.GetComponent<CameraStatus>().pwnd)
        {
            currentCam.GetComponent<Camera>().enabled = false;
            newCam.GetComponent<Camera>().enabled = true;
            currentCam = newCam;
        }
        else if (currentCam != staticCam) 
        {
            currentCam.GetComponent<Camera>().enabled = false;
            GameObject.Find("StaticCam").SetActive(true);
            currentCam = GameObject.Find("StaticCam");
        }
    }
}