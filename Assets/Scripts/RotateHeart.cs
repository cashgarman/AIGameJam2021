using UnityEngine;
using System.Collections;

public class RotateHeart : MonoBehaviour {

    

    public Vector3 rotationAngle;
    public float rotationSpeed = 0.2f;

    

	
	
	// Update is called once per frame
	void Update ()
    {

       
     
                transform.Rotate(rotationAngle * rotationSpeed * Time.deltaTime);
          
	}
}
