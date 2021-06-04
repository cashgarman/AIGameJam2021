using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shake : MonoBehaviour
{
    
    private float shake_intensity = 0.125f;
    public Vector3 originPosition;

    public bool isShaking = true;

    void Start()
    {
        originPosition = transform.position;
    }

    void Update()
    {
        if (isShaking)
        {
            float step = shake_intensity * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, originPosition + Random.insideUnitSphere, step);
        }
    }
}
