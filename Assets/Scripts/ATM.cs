using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ATM : MonoBehaviour
{
    public GameObject _invisBarrier;
    public enum ATMState {idle, swooning, seduced};
    private float speed = 0.02f;
    [SerializeField]
    private float cooldown = 30.0f;

    public ATMState state;

    private void Update()
    {
        switch (state.ToString())
        {
            case "idle":
                //Debug.Log("State = Idle");
                MoveBarrierDown();
                break;
            case "swooning":
                //Debug.Log("State = Swooning");
                MoveBarrierUp();
                break;
            case "seduced":
                //Debug.Log("State = Seduced");
                if(cooldown > 0)
                {
                    cooldown -= Time.deltaTime;
                }
                else
                {
                    cooldown = 30;
                    state = ATMState.idle;
                }
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        state = ATMState.swooning;
    }

    private void OnTriggerExit(Collider other)
    {
        if(state.ToString() != "seduced")
        {
            state = ATMState.idle;
        }
    }

    private void MoveBarrierUp()
    {
        if(_invisBarrier.transform.localPosition.z <= 0.7f)
        {
            _invisBarrier.transform.Translate(Vector3.up * Time.deltaTime * speed);
        }
        else
        {
            state = ATMState.seduced;
        }
    }

    private void MoveBarrierDown()
    {
        if (_invisBarrier.transform.localPosition.z > 0.53f)
        {
            _invisBarrier.transform.Translate(Vector3.up * Time.deltaTime * speed * -1);
        }
    }
}
