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
    public override Transform PluginSlot => _pluginSlot;
    public override bool IsPluggedIn { get; set; }
    public override BaseAgent PluggedIn { get; set; }

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
    [SerializeField] private float _batteryHighLevel;
    [SerializeField] private float _minChargedBatteryLevel;
    [SerializeField] public float _batteryChargeTransferPerSecond;
    public bool FullyCharged => _batteryLevel >= 0.99f * _maxBatteryLevel;
    public bool IsBatteryLow => _batteryLevel <= _batteryLowLevel;
    public float MinChargedBatteryLevel => _minChargedBatteryLevel;
    [SerializeField] private float _batteryLevelOverride;

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
    [SerializeField] private Transform _pluginSlot;
    public IPickup _holding;

    // Brain
    [SerializeField] private bool _runCode = false;
    [SerializeField, TextArea(5, 20)] private string code;
    private Program _program;

    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        BatteryLevel = _batteryLevelOverride != 0 ? _batteryLevelOverride : _maxBatteryLevel;
    }

    void Start()
    {
        if (_runCode)
        {
            _program = Compiler.Compile(code);
            _program.RunCurrentLine(this);
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

    public void PickUp(IPickup pickup, Action onComplete = null)
    {
        if (_holding != null)
            throw new Exception($"Tried to pick up {pickup.name} while already holding {_holding.name}");

        // Turn off the pickup's physics
        var rigidbody = pickup.gameObject.GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        
        // Pickup the object
        Debug.Log($"{name} picked up {pickup.name}");
        _holding = pickup;
        pickup.transform.SetParent(_hands);
        pickup.transform.localPosition = Vector3.zero;
        pickup.IsPickedUp = true;
        
        // TODO: Wait for any animations and whatnot
        onComplete?.Invoke();
    }

    public void Drop(IPickup pickup, Action onComplete = null)
    {
        if (_holding != pickup)
            throw new Exception($"Tried to drop {pickup.name} that wasn't holding");

        // Turn on the pickup's physics
        var rigidbody = pickup.gameObject.GetComponent<Rigidbody>();
        rigidbody.useGravity = true;
        rigidbody.isKinematic = false;
        
        // Drop the object
        Debug.Log($"{name} dropped {pickup.name}");
        _holding = null;
        pickup.transform.SetParent(null);
        pickup.IsPickedUp = false;
        
        // TODO: Wait for any animations and whatnot
        onComplete?.Invoke();
    }
    
    private void Plugin(BaseAgent subjectA, BaseAgent subjectB, Action onComplete)
    {
        // The robot should be holding subject A
        if (_holding != subjectA)
            throw new Exception($"Tried to plug in something {name} wasn't holding");
        
        // Turn off subject A's physics
        var rigidbody = subjectA.gameObject.GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        
        // Plug subject A into subject B
        Debug.Log($"{name} is plugging {subjectA} into {subjectB}");
        _holding = null;
        subjectA.transform.SetParent(subjectB.PluginSlot != null ? subjectB.PluginSlot : subjectB.transform);
        subjectA.transform.localPosition = Vector3.zero;
        subjectA.IsPluggedIn = true;
        subjectB.PluggedIn = subjectA;
        
        // TODO: Wait for any animations and whatnot
        onComplete?.Invoke();
    }

    public void ExecuteCommand(string command, params object[] arguments)
    {
        Debug.Log($"Attempting to execute command '{command}' with arguments '{string.Join(", ", arguments)}' on {name}");
        
        switch (command)
        {
            case "goto":
                var agent = arguments[0] as BaseAgent;
                
                // If a valid agent couldn't be found (there might not be one available right now)
                if (agent == null)
                {
                    // Wait a bit then try to run the same line again
                    // TODO: Find a better place to put this magic number
                    StartCoroutine(Util.Delay(1f, () => _program.RunCurrentLine(this)));
                    return;
                }
                
                MoveToObjectWithCallback(agent.transform, () =>
                {
                    _program.GotoNextLine();
                    _program.RunCurrentLine(this);
                });
                AddAgentInteraction(agent);
                break;
            
            case "pickup":
                var pickupTarget = arguments[0] as IPickup;
                
                // If a valid agent couldn't be found (there might not be one available right now)
                if (pickupTarget == null)
                {
                    // Wait a bit then try to run the same line again
                    // TODO: Find a better place to put this magic number
                    StartCoroutine(Util.Delay(1f, () => _program.RunCurrentLine(this)));
                    return;
                }
                
                PickUp(pickupTarget, () =>
                {
                    _program.GotoNextLine();
                    _program.RunCurrentLine(this);
                });
                AddAgentInteraction(pickupTarget as BaseAgent);
                break;
            
            case "drop":
                var dropTarget = arguments[0] as IPickup;
                
                // If a valid agent couldn't be found (there might not be one available right now)
                if (dropTarget == null)
                {
                    // Wait a bit then try to run the same line again
                    // TODO: Find a better place to put this magic number
                    StartCoroutine(Util.Delay(1f, () => _program.RunCurrentLine(this)));
                    return;
                }
                
                Drop(dropTarget, () =>
                {
                    _program.GotoNextLine();
                    _program.RunCurrentLine(this);
                });
                AddAgentInteraction(dropTarget as BaseAgent);
                break;
            
            case "plug":
                var subjectA = arguments[0] as BaseAgent;
                var subjectB = arguments[1] as BaseAgent;
                
                // If valid agents couldn't be found (there might not be any available right now)
                if (subjectA == null || subjectB == null)
                {
                    // Wait a bit then try to run the same line again
                    // TODO: Find a better place to put this magic number
                    StartCoroutine(Util.Delay(1f, () => _program.RunCurrentLine(this)));
                    return;
                }

                // The robot should be holding subject A
                if (_holding != subjectA)
                    throw new Exception($"Tried to plug in something {name} wasn't holding");
                
                // TODO: Subject B should be in range
                
                Plugin(subjectA, subjectB, () =>
                {
                    _program.GotoNextLine();
                    _program.RunCurrentLine(this);
                });
                
                // TODO: Are these necessary, will they cause problems?
                AddAgentInteraction(subjectA);
                AddAgentInteraction(subjectB);
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

    public override bool FilterBy(string filter, IComputer computer)
    {
        return filter switch
        {
            "empty" => _batteryLevel <= _batteryLowLevel, // TODO: Should this be zero, or close enough
            "full" => _batteryLevel >= _batteryHighLevel, // TODO: Should this be zero, or close enough
            "next" => !computer.HasInteractedWithRecently(this),
            _ => throw new Exception($"Invalid filter provided to {GetType().Name}: {filter}")
        };
    }
}