using UnityEngine;

[ExecuteInEditMode]
public class FaceCamera : MonoBehaviour
{
    public GameObject currentCam;


    void LateUpdate()
    {
        if(GameObject.Find("City") == null)
        {
            transform.forward = Camera.main.transform.forward;
        }
        else
        {
            currentCam = GameObject.Find("Displays").GetComponent<CameraChangeController>().currentCam;
            transform.forward = currentCam.transform.forward;
        }
        
        
    }
}
