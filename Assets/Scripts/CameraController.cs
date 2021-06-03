using DG.Tweening;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float _zoomSpeed;

    private Vector3 _lookDir;
    public Robot Target { get; set; }

    private void Start()
    {
        _lookDir = Camera.main.transform.forward;
    }

    void Update()
    {
        transform.DOLocalMove(Target.transform.position, 1f);

        // TODO: Clamp the zoom distance
        Camera.main.transform.position += _lookDir * (Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed);
    }
}
