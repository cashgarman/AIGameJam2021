using System;
using Programming;
using States;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class Robot : Agent<Robot>, IComputer
{
    public override string Name => name;

    // Energy
    [SerializeField] private float _maxBatteryLevel;

    private float _batteryLevel;
    public float BatteryLevel
    {
        get => _batteryLevel;
        set => _batteryLevel = Mathf.Clamp(value, 0f, _maxBatteryLevel);
    }

    public float _idleBatteryDrainPerSecond;
    public float _movingBatteryDrainPerSecond;
    [SerializeField] private float _batteryLowLevel;
    [SerializeField] private float _minChargedBatteryLevel;
    [SerializeField] public float _batteryChargeTransferPerSecond;
    public bool FullyCharged => _batteryLevel >= 0.99f * _maxBatteryLevel;
    public bool IsBatteryLow => _batteryLevel <= _batteryLowLevel;
    public float MinChargedBatteryLevel => _minChargedBatteryLevel;
    
    // UI
    [SerializeField] private Slider _energyDisplay;
    [SerializeField] private TMP_Text _nameText;

    // Movement
    private NavMeshAgent _navAgent;
    [SerializeField] private float _reachedDestinationThreshold = 0.1f;
    public float ReachedDestinationThreshold => _reachedDestinationThreshold;
    public bool HasReachedDestination => _navAgent.remainingDistance < _reachedDestinationThreshold;
    public NavMeshAgent NavAgent => _navAgent;

    // Inventory
    [SerializeField] private Transform _hands;
    private IPickup _holding;
    
    // Brain
    [SerializeField] private bool _runCode = false;
    [SerializeField, TextArea(5, 20)] private string code;
    private Program _program;

    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        BatteryLevel = _maxBatteryLevel;

        if (_runCode)
        {
            _program = Compiler.Compile(code);
            _program.Run(this);
        }
        else
        {
            GotoState<RobotIdleState>(this);
        }
    }

    protected override void Update()
    {
        base.Update();

        UpdateUI();
        
        HandleDebugControls();
    }

    private void HandleDebugControls()
    {
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            BatteryLevel -= 10f;
        }
    }

    private void UpdateUI()
    {
        // Update the robot's name and state
        _nameText.text = $"{Name}\n<size=50%>{(_currentState != null ? _currentState.Name : "None")}</size>";
        
        // Update the battery remaining level
        _energyDisplay.value = BatteryLevel / _maxBatteryLevel;
    }

    // public void MoveToPositionAndChangeState(Vector3 destination, Action onReached)
    // {
    //     GotoState<RobotMovingToPositionState>(this, destination, onReached);
    // }

    public void MoveToObjectWithCallback(Transform target, Action onReached = null, Func<bool> shouldContinueProvider = null)
    {
        NavAgent.isStopped = false;
        GotoState<RobotMovingToTargetState>(this, target, onReached, shouldContinueProvider);
    }

    public void Stop()
    {
        NavAgent.isStopped = true;
    }

    public void Idle()
    {
        GotoState<RobotIdleState>(this);
    }

    public void PickUp(IPickup pickup)
    {
        if (_holding != null)
            throw new Exception($"Tried to pick up {pickup.name} while already holding {_holding.name}");

        // Turn off the pickup's physics
        var rigidbody = pickup.gameObject.GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        
        // Pickup the object
        _holding = pickup;
        pickup.transform.SetParent(_hands);
        pickup.transform.localPosition = Vector3.zero;
        pickup.IsPickedUp = true;
    }

    public void Drop(IPickup pickup)
    {
        if (_holding != pickup)
            throw new Exception($"Tried to drop {pickup.name} that wasn't holding");

        // Turn on the pickup's physics
        var rigidbody = pickup.gameObject.GetComponent<Rigidbody>();
        rigidbody.useGravity = true;
        rigidbody.isKinematic = false;
        
        // Drop the object
        _holding = null;
        pickup.transform.SetParent(null);
        pickup.IsPickedUp = false;
    }

    public void ExecuteCommand(string command, object argument)
    {
        switch (command)
        {
            case "goto":
                var agent = argument as BaseAgent;
                if (agent == null)
                    throw new Exception($"Failed to execute command {command}: Argument provided is not an agent");
                
                MoveToObjectWithCallback(agent.transform);
                break;
        }
    }
    
    public override float GetOrderBy(string sortType)
    {
        return sortType switch
        {
            "fullest" => _batteryLevel / _maxBatteryLevel,
            "emptiest" => 1f - _batteryLevel / _maxBatteryLevel
        };
    }
}