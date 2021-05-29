using UnityEngine;

[ExecuteInEditMode]
public class FaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }
}
