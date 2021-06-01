using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class RobotAnimationController : MonoBehaviour
{
    public enum FacialExpression
    {
        Angry,
        Annoyed,
        Closed,
        Dead,
        Default,
        Happy,
        Sad,
        Surprised,
    }
    
    [SerializeField] private FacialExpression _expression = FacialExpression.Default;
    
    private Animator _animator;
    private Animation _animation;
    private NavMeshAgent _navAgent;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _animation = GetComponent<Animation>();
        _navAgent = GetComponent<NavMeshAgent>();
        
        _animation.Play(_expression.ToString());
    }

    void Update()
    {
        _animator.SetFloat("Speed", _navAgent.velocity.magnitude / _navAgent.speed);
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var newExpression = (int) _expression + 1;
            if (newExpression >= Enum.GetValues(typeof(FacialExpression)).Length)
                newExpression = 0;
            _expression = (FacialExpression) newExpression;
            _animation.Play(_expression.ToString());
        }
    }

    private void OnGUI()
    {
        foreach(var trigger in _animator.parameters.Where(p => p.type == AnimatorControllerParameterType.Trigger))
            if (GUILayout.Button(trigger.name))
                _animator.SetTrigger(trigger.name);
    }
}