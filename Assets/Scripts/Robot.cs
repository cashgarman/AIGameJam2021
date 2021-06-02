using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agents;
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
    public bool HasReachedDestination => _navAgent.remainingDistance <= _navAgent.stoppingDistance;
    public NavMeshAgent NavAgent => _navAgent;
    public Program Program => _program;

    // Inventory
    [SerializeField] private Transform _hands;
    [SerializeField] private Transform _pluginSlot;
    public IPickup Holding { get; private set; }

    // Brain
    [SerializeField] private bool _runCode = false;
    [SerializeField, TextArea(5, 20)] private string code;
    private Program _program;
    
    // Hacking
    [SerializeField] private float _hackingSpeed = 1f;
    private IHackable _hackingInProgress;
    public float HackingSpeed => _hackingSpeed;

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
            _program.StartMain();
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
        
        // If the robot's program isn't currently interrupted
        if (_program is {Interrupted: false})
        {
            // Foreach if statement in the program
            foreach (var ifStatement in _program.Sections.Where(s => s.Type == CodeSection.BlockType.Conditional))
            {
                // Check if the if statement should run
                Debug.Log($"Checking if if statement '{ifStatement.Lines[0]}' should run");
                if (ifStatement.Lines[0].Run(this))
                {
                    _program.InterruptAndJumpToSection(ifStatement);
                    _program.RunCurrentLine(this);
                    break;
                }
            }
        }
        
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
        // _nameText.text = $"{Name}\n<size=50%>{(_currentState != null ? _currentState.Name : "None")}</size>";
        _nameText.text = Name;
        
        // Update the battery remaining level
        _energyDisplay.value = BatteryLevel / _maxBatteryLevel;
    }

    public void MoveToPositionWithCallback(Vector3 destination, Action onReached)
    {
        InterruptProcesses();
        
        NavAgent.isStopped = false;
        GotoState<RobotMovingToPositionState>(this, destination, onReached);
    }

    public void MoveToObjectWithCallback(Transform target, Action onReached = null, Func<bool> shouldContinueProvider = null)
    {
        InterruptProcesses();
       
        NavAgent.isStopped = false;
        GotoState<RobotMovingToTargetState>(this, target, onReached, shouldContinueProvider);
    }

    private void InterruptProcesses()
    {
        // Interrupt any hacking
        if (_hackingInProgress != null)
        {
            _hackingInProgress.OnHackingInterrupted();
            _hackingInProgress = null;
        }
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
        if (Holding != null)
            throw new Exception($"Tried to pick up {pickup.name} while already holding {Holding.name}");

        // Turn off the pickup's physics
        var rigidbody = pickup.gameObject.GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        
        // Pickup the object
        Debug.Log($"{name} picked up {pickup.name}");
        Holding = pickup;
        pickup.transform.SetParent(_hands);
        pickup.transform.localPosition = Vector3.zero;
        pickup.IsPickedUp = true;
        
        // TODO: Wait for any animations and whatnot
        onComplete?.Invoke();
    }

    public void Drop(IPickup pickup, Action onComplete = null)
    {
        if (Holding != pickup)
            throw new Exception($"Tried to drop {pickup.name} that wasn't holding");

        // Turn on the pickup's physics
        var rigidbody = pickup.gameObject.GetComponent<Rigidbody>();
        rigidbody.useGravity = true;
        rigidbody.isKinematic = false;
        
        // Drop the object
        Debug.Log($"{name} dropped {pickup.name}");
        Holding = null;
        pickup.transform.SetParent(null);
        pickup.IsPickedUp = false;
        
        // TODO: Wait for any animations and whatnot
        onComplete?.Invoke();
    }
    
    private void Plugin(BaseAgent subjectA, BaseAgent subjectB, Action onComplete)
    {
        // The robot should be holding subject A
        if (Holding != subjectA)
            throw new Exception($"Tried to plug in something {name} wasn't holding");
        
        // Turn off subject A's physics
        var rigidbody = subjectA.gameObject.GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        
        // Plug subject A into subject B
        Debug.Log($"{name} is plugging {subjectA} into {subjectB}");
        Holding = null;
        subjectA.transform.SetParent(subjectB.PluginSlot != null ? subjectB.PluginSlot : subjectB.transform);
        subjectA.transform.localPosition = Vector3.zero;
        subjectA.IsPluggedIn = true;
        subjectB.OnObjectPluggedIn(subjectA);
        
        // TODO: Wait for any animations and whatnot
        onComplete?.Invoke();
    }
    
    public void UnplugFromSelf(BaseAgent subject, Action onComplete)
    {
        // The robot should be plugged into the subject already
        if (PluggedIn != subject)
            throw new Exception($"Tried to unplug {name} that wasn't plugged into the robot");
        
        // Turn on the subject's physics
        var rigidbody = subject.gameObject.GetComponent<Rigidbody>();
        rigidbody.useGravity = true;
        rigidbody.isKinematic = false;
        
        // Unplug the subject from the robot
        Debug.Log($"{name} is unplugging {subject} from themselves");
        subject.IsPluggedIn = false;
        subject.transform.SetParent(null);
        subject.OnObjectUnplugged(subject);
        
        // TODO: Wait for any animations and whatnot
        onComplete?.Invoke();
    }

    public override void OnObjectPluggedIn(BaseAgent pluggedIn)
    {
        base.OnObjectPluggedIn(pluggedIn);
        
        // If the object is a battery
        var battery = pluggedIn as Battery;
        if (battery != null)
        {
            // Start charging from the battery
            GotoState<RobotChargingState>(this, battery);
        }
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
                    // If the program isn't currently interrupted
                    // if (!_program.Interrupted)
                    // {
                        // Run the next line of code
                        _program.GotoNextLine();
                        _program.RunCurrentLine(this);
                    // }
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

                void PickUpTarget()
                {
                    PickUp(pickupTarget, () =>
                    {
                        // If the program isn't currently interrupted
                        // if (!_program.Interrupted)
                        // {
                        // Run the next line of code
                        _program.GotoNextLine();
                        _program.RunCurrentLine(this);
                        // }
                    });
                }
                
                // If the target is far away
                if (!InRange(pickupTarget as BaseAgent))
                    MoveToObjectWithCallback(pickupTarget.transform, () =>
                    {
                        AddAgentInteraction(pickupTarget as BaseAgent);
                        PickUpTarget();
                    });
                else
                    PickUpTarget();
                
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
                    // If the program isn't currently interrupted
                    // if (!_program.Interrupted)
                    // {
                        // Run the next line of code
                        _program.GotoNextLine();
                        _program.RunCurrentLine(this);
                    // }
                });
                AddAgentInteraction(dropTarget as BaseAgent);
                break;
            
            case "plug":
                var plugSubjectA = arguments[0] as BaseAgent;
                var plugSubjectB = arguments[1] as BaseAgent;
                
                // If valid agents couldn't be found (there might not be any available right now)
                if (plugSubjectA == null || plugSubjectB == null)
                {
                    // Wait a bit then try to run the same line again
                    // TODO: Find a better place to put this magic number
                    StartCoroutine(Util.Delay(1f, () => _program.RunCurrentLine(this)));
                    return;
                }

                // The robot should be holding subject A
                if (Holding != plugSubjectA)
                    throw new Exception($"Tried to plug in something {name} wasn't holding");
                
                void PluginTarget()
                {
                    Plugin(plugSubjectA, plugSubjectB, () =>
                    {
                        // If the program isn't currently interrupted
                        // if (!_program.Interrupted)
                        // {
                        // Run the next line of code
                        _program.GotoNextLine();
                        _program.RunCurrentLine(this);
                        // }
                    });
                }
                
                // If the target is far away
                if (!InRange(plugSubjectB))
                    MoveToObjectWithCallback(plugSubjectB.transform, () =>
                    {
                        // TODO: Are these necessary, will they cause problems?
                        AddAgentInteraction(plugSubjectA);
                        AddAgentInteraction(plugSubjectB);
                        
                        PluginTarget();
                    });
                else
                    PluginTarget();
                
                break;
            
            case "unplug":
                var subjectToUnplug = arguments[0] as BaseAgent;
                var pluggedIntoSubject = arguments[1] as BaseAgent;
                
                // If valid agents couldn't be found (there might not be any available right now)
                if (subjectToUnplug == null || pluggedIntoSubject == null)
                {
                    // Wait a bit then try to run the same line again
                    // TODO: Find a better place to put this magic number
                    StartCoroutine(Util.Delay(1f, () => _program.RunCurrentLine(this)));
                    return;
                }

                // Subject B should have subject A plugged in
                if (pluggedIntoSubject.PluggedIn != subjectToUnplug)
                    throw new Exception($"Tried to unplug {subjectToUnplug.Name} that wasn't plugged into {pluggedIntoSubject.Name}");
                
                void UnpluginTarget()
                {
                    // HACK
                    // TODO: Add unplugging from something other than self 
                    UnplugFromSelf(subjectToUnplug, () =>
                    {
                        // If the program isn't currently interrupted
                        // if (!_program.Interrupted)
                        // {
                        // Run the next line of code
                        _program.GotoNextLine();
                        _program.RunCurrentLine(this);
                        // }
                    });
                }
                
                // If either subject is far away
                if (!InRange(subjectToUnplug))
                    MoveToObjectWithCallback(subjectToUnplug.transform, () =>
                    {
                        // TODO: Are these necessary, will they cause problems?
                        AddAgentInteraction(subjectToUnplug);
                        AddAgentInteraction(pluggedIntoSubject);
                        
                        UnpluginTarget();
                    });
                else
                    UnpluginTarget();
                
                break;
            
            case "wait":
                var delayNode = arguments[0] as DelayNode;

                StartCoroutine(WaitInternal(delayNode, () =>
                {
                    Debug.Log($"Waiting finished");
                    _program.GotoNextLine();
                    _program.RunCurrentLine(this);
                }));
                break;
            
            case "idle":
                Idle();
                break;
            
            case "hack":
                var hackingTarget = arguments[0] as IHackable;

                if (hackingTarget == null)
                {
                    Debug.Log($"Tried to hack unhackable target {hackingTarget.Name}");
                    return;
                }

                void HackTarget()
                {
                    hackingTarget.OnHackingStarted(this, () =>
                    {
                        _program.GotoNextLine();
                        _program.RunCurrentLine(this);
                    });
                    _hackingInProgress = hackingTarget;
                }
                
                // If either subject is far away
                if (!InRange(hackingTarget as BaseAgent))
                    MoveToObjectWithCallback(hackingTarget.transform, () =>
                    {
                        HackTarget();
                        AddAgentInteraction(hackingTarget as BaseAgent);
                    });
                else
                    HackTarget();
                
                break;
            
            default:
                throw new Exception($"{name} tried to run unknown command '{command}'");
        }
    }

    public void RunProgram(Program program)
    {
        _program = program;
        _program.StartMain();
        _program.RunCurrentLine(Game.ControlledRobot);
    }
    
    private IEnumerator WaitInternal(DelayNode delayNode, Action callback)
    {
        while (delayNode.Waiting)
        {
            Debug.Log($"Waiting...");
            yield return new WaitForEndOfFrame();
        }
        callback();
    }

    public override void OnClicked()
    {
        Debug.Log($"{name} was clicked");
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