using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotcoinController : MonoBehaviour
{

    public Vector3 rotationAngle;
    public float rotationSpeed = 0.2f;
    public float m_Thrust;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Rigidbody>().AddForce(new Vector3(Random.Range(.01f,.1f), 1.5f, Random.Range(.01f, .1f)) * m_Thrust, ForceMode.Impulse);
    }

    void Update()
    {



        transform.Rotate(rotationAngle * rotationSpeed * Time.deltaTime);
        Destroy(gameObject, .8f);

    }

    
}
