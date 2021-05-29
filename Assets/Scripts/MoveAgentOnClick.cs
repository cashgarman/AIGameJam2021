using UnityEngine;

public class MoveAgentOnClick : MonoBehaviour
{
    private Robot _robot;

    void Awake()
    {
        _robot = GetComponent<Robot>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // CREDIT: https://answers.unity.com/questions/1386777/get-origin-of-plane-in-world-coordinates.html
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit))
            {
                // _robot.MoveToPositionAndChangeState(hit.point, () => _robot.Idle());
            }
        }
    }
}
