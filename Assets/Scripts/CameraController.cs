using DG.Tweening;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _zoomSpeed;

    private Vector3 _lookDir;

    private void Awake()
    {
        _lookDir = Camera.main.transform.forward;
    }

    void Update()
    {
        transform.DOLocalMove(_target.position, 1f);

        // TODO: Clamp the zoom distance
        Camera.main.transform.position += _lookDir * (Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed);
    }
}
