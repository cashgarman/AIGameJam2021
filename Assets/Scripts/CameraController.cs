using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float _zoomSpeed;

    [SerializeField] private float _mouseRotateSensitivity;
    public Robot Target { get; set; }

    void Update()
    {
        transform.DOLocalMove(Target.transform.position, 1f);
        
        if(Input.GetMouseButton(1))
            transform.Rotate(0, Input.GetAxis("Mouse X") * _mouseRotateSensitivity, 0);

        // TODO: Clamp the zoom distance

        Camera.main.transform.position += Camera.main.transform.forward * (Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed);
    }
}
