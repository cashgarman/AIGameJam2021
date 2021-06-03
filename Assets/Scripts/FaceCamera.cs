using UnityEngine;

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
            transform.forward = CameraChangeController.CurrentCamera.transform.forward;
        }
    }
}
