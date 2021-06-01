using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopulateDropdown : MonoBehaviour
{
    GameObject currentCam;
    GameObject newCam;
    public Dropdown myDropdown;
    [SerializeField]
    private RenderTexture displayTexture;

    void Start()
    {
        GameObject[] dropdownObjects = GameObject.FindGameObjectsWithTag("Cam"); // For example
        Populate(myDropdown, dropdownObjects);
        currentCam = GameObject.Find(myDropdown.options[myDropdown.value].text);
        ChangeDisplay();
    }


    void Populate(Dropdown dropdown, GameObject[] optionsArray)
    {
        List<string> options = new List<string>();
        foreach (var option in optionsArray)
        {
            options.Add(option.name); // Or whatever you want for a label
            option.GetComponent<Camera>().targetTexture = null;
        }
        dropdown.ClearOptions();
        dropdown.AddOptions(options);

    }

    public void ChangeDisplay()
    {
        currentCam.GetComponent<Camera>().targetTexture = null;
        GameObject.Find(myDropdown.options[myDropdown.value].text).GetComponent<Camera>().targetTexture = displayTexture;
        currentCam = GameObject.Find(myDropdown.options[myDropdown.value].text);
    }
}