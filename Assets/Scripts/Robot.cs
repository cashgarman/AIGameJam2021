using System;
using System.Collections;
using System.Linq;
using Agents;
using Programming;
using States;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Robot : Agent<Robot>, IComputer, IPickup
{
    public enum RobotTeam
    {
        Player,
        Police,
        Pedestrian
    }

    public override string Name => name;
    public override Transform PluginSlot => _pluginSlot;
    public override BaseAgent PluggedInto { get; set; }
    public override float AlreadyVisitedCooldownMultiplier => 1f;

    [SerializeField] private RobotTeam _team;
    public RobotTeam Team => _team;
    
    // Energy
    [SerializeField] private float _maxBatteryLevel;

    private float _batteryLevel;
    public float BatteryLevel
    {
        get => _batteryLevel;
        set
        {
            _batteryLevel = Mathf.Clamp(value, 0f, _maxBatteryLevel);
            
            // If the battery has run out
            if (_batteryLevel <= 0f)
            {
                // Disable the robot
                OnDisabled();
            }
            
            // If the robot was disabled but now has power
            if (Disabled && _batteryLevel > _minBatteryLevelToReenable)
            {
                OnEnabled();
            }
        }
    }

    public float _idleBatteryDrainPerSecond;
    public float _movingBatteryDrainPerSecond;
    [SerializeField] private float _batteryLowLevel;
    [SerializeField] private float _batteryHighLevel;
    [SerializeField] private float _minChargedBatteryLevel;
    [SerializeField] public float _batteryChargeTransferPerSecond;
    public bool FullyCharged => _batteryLevel >= 0.99f * _maxBatteryLevel;
    public bool IsBatteryLow => _batteryLevel <= _batteryLowLevel;
    public bool IsBatteryEmpty => _batteryLevel == 0f;
    public float MinChargedBatteryLevel => _minChargedBatteryLevel;
    [SerializeField] private float _batteryLevelOverride;
    [SerializeField] private float _minBatteryLevelToReenable;

    // UI
    [SerializeField] private Slider _energyDisplay;
    [SerializeField] private TMP_Text _nameText;

    // Movement
    private NavMeshAgent _navAgent;
    [SerializeField] private float _reachedDestinationThreshold = 0.1f;
    public float ReachedDestinationThreshold => _reachedDestinationThreshold;
    public bool HasReachedDestination => _navAgent.isOnNavMesh ? _navAgent.remainingDistance <= ReachedDestinationThreshold : false;
    public NavMeshAgent NavAgent => _navAgent;
    public Program Program
    {
        get => _program;
        set => _program = value;
    }

    public bool IsPickedUp { get; set; }

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
    private Coroutine _waitingToRetryCommand;
    public float HackingSpeed => _hackingSpeed;
    
    // Combat
    [SerializeField] private float _attackCooldown;
    public float AttackCooldown => _attackCooldown;
    [SerializeField] private float _attackDamage;
    public float AttackDamage => _attackDamage;
    [SerializeField] private float _fightingRange;
    [SerializeField] private string _nameOverride;
    [SerializeField] private float _minIdleTime;
    [SerializeField] private float _maxIdleTime;
    private Coroutine _idleCoroutine;
    public float FightingRange => _fightingRange;
    public bool IsRunningProgram => _program is {Interrupted: false};
    public bool IsEditingCode { get; set; }

    protected override void Awake()
    {
        base.Awake();
        
        _navAgent = GetComponent<NavMeshAgent>();
        BatteryLevel = _batteryLevelOverride != 0 ? _batteryLevelOverride : _maxBatteryLevel;

        name = !string.IsNullOrEmpty(_nameOverride) ? _nameOverride : _team switch
        {
            RobotTeam.Player => Util.RandomElement(new[]
            {
                "CrimeBot",
                "ThugBot",
                "BadBot",
                "CriminalBot",
            }),
            RobotTeam.Police => Util.RandomElement(new[]
            {
                "PoliceBot",
                "CopBot",
                "GuardBot",
                "OfficeBot",
            }),
            RobotTeam.Pedestrian => Util.RandomElement(new[]
            {
                "PedestrianBot",
                "CivilianBot",
                "BystanderBot",
                "RegularBot",
                "BoringBot",
                "BusyBot",
                "BoredBot",
                "LostBot",
            })
        };
    }

    void Start()
    {
        base.Start();
        
        if (_runCode)
        {
            _program = Compiler.Compile(code);
            _program.StartMain(this);
            _program.RunCurrentLine(this);
        }
        else
        {
            GotoState<RobotIdleState>(this);
        }
    }

    protected override void Update()
    {
        // Tick the base agent
        base.Update();

        UpdateUI();

        // If there is a battery plugged into the robot
        var battery = PluggedInto as Battery;
        if (battery != null)
        {
            // Transfer some power from the battery to the robot
            var potentialEnergyTransfer = _batteryChargeTransferPerSecond * Time.deltaTime;     // Maximum potential energy transfer
            var energyToTransfer = Mathf.Min(battery.Charge, potentialEnergyTransfer);          // Make sure not to over-discharge the battery
            energyToTransfer = Mathf.Min(energyToTransfer, _maxBatteryLevel - _batteryLevel);   // Make sure not to over-charge the robot
            
            // Transfer the energy
            battery.Charge -= energyToTransfer;
            BatteryLevel += energyToTransfer;
        }
        
        // If the robot's program isn't currently interrupted
        if (_program is {Interrupted: false})
        {
            // Foreach if statement in the program
            foreach (var ifStatement in _program.Sections.Where(s => s.IsIfStatement))
            {
                // Check if the if statement should run
                if (ifStatement.Lines[0].Run(_program))
                {
                    CancelIdle();
                    _program.InterruptAndJumpToSection(ifStatement);
                    _program.RunCurrentLine(this);
                    break;
                }
            }
        }
        
        HandleDebugControls();
    }

    private void CancelIdle()
    {
        StopCoroutine(_idleCoroutine);
    }

    public override void OnHeardShout(ShoutType shout, BaseAgent shouter)
    {
        base.OnHeardShout(shout, shouter);
        
        // If the robot's program isn't currently interrupted
        if (_program is {Interrupted: false})
        {
            // Foreach heard statement in the program
            foreach (var heardStatement in _program.Sections.Where(s => s.IsHeardStatement))
            {
                // Check if the heard statement should run
                Debug.Log($"Checking if heard statement '{heardStatement.Lines[0]}' should run");
                if ((heardStatement.FirstLine.RootNode as HeardNode).Notification.ToLower() == shout.ToString().ToLower())
                {
                    // Populate the agent that shouted this notification
                    _program.Notifier = shouter;
                    Debug.Log($"Heard statement '{heardStatement.Lines[0]}' should run");
                    _program.InterruptAndJumpToSection(heardStatement);
                    _program.RunCurrentLine(this);
                    break;
                }
            }
        }
    }

    private void HandleDebugControls()
    {
        // if (Input.GetKeyDown(KeyCode.Backspace))
        // {
        //     BatteryLevel -= 10f;
        // }
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

    public void MoveToObjectWithCallback(Transform target, Action onReached, Func<bool> abortProvider = null)
    {
        InterruptProcesses();
       
        NavAgent.isStopped = false;
        GotoState<RobotMovingToTargetState>(this, target, onReached, abortProvider);
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

    public void StopRobot()
    {
        NavAgent.isStopped = true;
        Idle();
    }
    
    public void StartRobot()
    {
        NavAgent.isStopped = false;
    }

    public void Idle()
    {
        GotoState<RobotIdleState>(this);
    }

    public void PickUp(IPickup pickup, Action onComplete = null)
    {
        if (Holding != null)
        {
            // HACK: Check if they REALLY holding something
            if (_hands.childCount == 0)
            {
                Holding = null;
                StartCoroutine(Util.Delay(1f, () => PickUp(pickup, onComplete)));
                return;
            }
            
            throw new Exception($"Tried to pick up {pickup.name} while already holding {Holding.name}");
        }

        if ((pickup as BaseAgent).PluggedInto != null)
        {
            // Unplug the agent
            var agent = pickup as BaseAgent;
            agent.PluggedInto.PluggedInto = null;
            agent.PluggedInto = null;
        }

        // Turn off the pickup's physics
        (pickup as BaseAgent).SetPhysicsEnabled(false);
        
        // Pickup the object
        Debug.Log($"{name} picked up {pickup.name}");
        Holding = pickup;
        pickup.transform.SetParent(_hands);
        pickup.transform.localPosition = Vector3.zero;
        pickup.transform.localRotation = Quaternion.identity;
        pickup.IsPickedUp = true;
        pickup.OnPickedUp();
        
        // TODO: Wait for any animations and whatnot
        onComplete?.Invoke();
    }

    public void Drop(IPickup pickup, Action onComplete = null)
    {
        if (Holding != pickup)
        {
            Debug.LogError($"Tried to drop {pickup.name} that wasn't holding");
            onComplete?.Invoke();
            return;
        }

        // Turn on the pickup's physics
        (pickup as BaseAgent).SetPhysicsEnabled(true);
        
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
        
        // Subject B shouldn't already have something plugged in
        if (subjectB.PluggedInto != null)
        {
            StartCoroutine(Util.Delay(1f, () => Plugin(subjectA, subjectB, onComplete)));
            return;
            // throw new Exception($"Tried to plug in something {subjectA.Name} into something that already had something plugged in");
        }
        
        // Turn off subject A's physics
        subjectA.SetPhysicsEnabled(false);
        
        // Plug subject A into subject B
        Debug.Log($"{name} is plugging {subjectA} into {subjectB}");
        Holding = null;
        subjectA.transform.SetParent(subjectB.PluginSlot != null ? subjectB.PluginSlot : subjectB.transform);
        subjectA.transform.localPosition = Vector3.zero;
        subjectA.transform.localRotation = Quaternion.identity;
        subjectA.PluggedInto = subjectB;
        subjectB.PluggedInto = subjectA;
        subjectB.OnObjectPluggedIn(subjectA);
        
        // TODO: Wait for any animations and whatnot
        onComplete?.Invoke();
    }

    // public override void OnObjectPluggedIn(BaseAgent pluggedIn)
    // {
    //     base.OnObjectPluggedIn(pluggedIn);
    //     
    //     // If the object is a battery
    //     var battery = pluggedIn as Battery;
    //     if (battery != null)
    //     {
    //         // Start charging from the battery
    //         GotoState<RobotChargingState>(this, battery);
    //     }
    // }

    public void ExecuteCommand(string command, params object[] arguments)
    {
        Debug.Log($"Attempting to execute command '{command}' with arguments '{string.Join(", ", arguments)}' on {name}");
        
        switch (command)
        {
            case "goto":
                var moveTarget = arguments[0] as BaseAgent;
                
                // If a valid agent couldn't be found (there might not be one available right now)
                if (moveTarget == null)
                {
                    // Try the command again a bit
                    RetryLineOfCodeAfterDelay();
                    return;
                }
                
                MoveToObjectWithCallback(moveTarget.transform, () =>
                {
                    Debug.Log($"{Name} reached move target {moveTarget.Name}");
                    
                    AddAgentInteraction(moveTarget);
                    
                    // Run the next line of code
                    _program.GotoNextLine();
                    _program.RunCurrentLine(this);
                });
                
                break;

            case "pickup":
                var pickupTarget = arguments[0] as IPickup;

                // If a valid agent couldn't be found (there might not be one available right now)
                if (pickupTarget == null)
                {
                    // Try the command again a bit
                    RetryLineOfCodeAfterDelay();
                    return;
                }

                void PickUpTarget()
                {
                    PickUp(pickupTarget, () =>
                    {
                        // Run the next line of code
                        _program.GotoNextLine();
                        _program.RunCurrentLine(this);
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
                {
                    AddAgentInteraction(pickupTarget as BaseAgent);
                    PickUpTarget();
                }
                
                break;
            
            case "drop":
                var dropTarget = arguments[0] as IPickup;
                
                // If a valid agent couldn't be found (there might not be one available right now)
                if (dropTarget == null)
                {
                    // Try the command again a bit
                    RetryLineOfCodeAfterDelay();
                    return;
                }
                
                Drop(dropTarget, () =>
                {
                    AddAgentInteraction(dropTarget as BaseAgent);
                    
                    // Run the next line of code
                    _program.GotoNextLine();
                    _program.RunCurrentLine(this);
                });
                
                break;
            
            case "plug":
                var plugSubjectA = arguments[0] as BaseAgent;
                var plugSubjectB = arguments[1] as BaseAgent;
                
                // If valid agents couldn't be found (there might not be any available right now)
                if (plugSubjectA == null || plugSubjectB == null)
                {
                    // Try the command again a bit
                    RetryLineOfCodeAfterDelay();
                    return;
                }

                // The robot should be holding subject A
                if (Holding != plugSubjectA)
                    throw new Exception($"Tried to plug in something {name} wasn't holding");
                
                void PluginTarget()
                {
                    Plugin(plugSubjectA, plugSubjectB, () =>
                    {
                        // TODO: Are these necessary, will they cause problems?
                        AddAgentInteraction(plugSubjectA);
                        AddAgentInteraction(plugSubjectB);
                        
                        // Run the next line of code
                        _program.GotoNextLine();
                        _program.RunCurrentLine(this);
                    });
                }
                
                // If the target is far away
                if (!InRange(plugSubjectB))
                    MoveToObjectWithCallback(plugSubjectB.transform, PluginTarget);
                else
                    PluginTarget();
                
                break;
            
            case "unplug":
                var subjectToUnplug = arguments[0] as BaseAgent;
                var pluggedIntoSubject = arguments[1] as BaseAgent;
                
                // If valid agents couldn't be found (there might not be any available right now)
                if (subjectToUnplug == null || pluggedIntoSubject == null)
                {
                    // Try the command again a bit
                    RetryLineOfCodeAfterDelay();
                    return;
                }

                // Subject B should have subject A plugged in
                if (pluggedIntoSubject.PluggedInto != subjectToUnplug)
                    throw new Exception($"Tried to unplug {subjectToUnplug.Name} that wasn't plugged into {pluggedIntoSubject.Name}");
                
                void UnpluginTarget()
                {
                    // HACK
                    // TODO: Add unplugging from something other than self 
                    UnplugFromSelf(subjectToUnplug, () =>
                    {
                        // TODO: Are these necessary, will they cause problems?
                        AddAgentInteraction(subjectToUnplug);
                        AddAgentInteraction(pluggedIntoSubject);
                        
                        // Run the next line of code
                        _program.GotoNextLine();
                        _program.RunCurrentLine(this);
                    });
                }
                
                // If either subject is far away
                if (!InRange(subjectToUnplug))
                    MoveToObjectWithCallback(subjectToUnplug.transform, UnpluginTarget);
                else
                    UnpluginTarget();
                
                break;
            
            case "wait":
                var delayNode = arguments[0] as DelayNode;

                StartCoroutine(WaitInternal(delayNode, () =>
                {
                    _program.GotoNextLine();
                    _program.RunCurrentLine(this);
                }));
                break;
            
            case "idle":
                // Wait around for a random amount of time
                var waitTime = Random.Range(_minIdleTime, _maxIdleTime);

                _idleCoroutine = StartCoroutine(Util.Delay(waitTime, () =>
                {
                    _program.GotoNextLine();
                    _program.RunCurrentLine(this);
                }));
                
                break;
            
            case "hack":
                var hackingTarget = arguments[0] as IHackable;

                // If a valid agent couldn't be found (there might not be one available right now)
                if (arguments[0] == null)
                {
                    // Try the command again a bit
                    RetryLineOfCodeAfterDelay();
                    return;
                }
                
                // Check if the target is even hackable
                if (hackingTarget == null)
                {
                    Debug.Log($"Tried to hack unhackable target {hackingTarget.Name}");
                    return;
                }

                void HackTarget()
                {
                    AddAgentInteraction(hackingTarget as BaseAgent);
                    hackingTarget.OnHackingStarted(this, () =>
                    {
                        StartCoroutine(Util.Delay(2f, () =>
                        {
                            _program.GotoNextLine();
                            _program.RunCurrentLine(this);
                        }));
                    });
                    _hackingInProgress = hackingTarget;
                }
                
                // If either subject is far away
                if (!InRange(hackingTarget as BaseAgent))
                    MoveToObjectWithCallback(hackingTarget.transform, HackTarget);
                else
                    HackTarget();
                
                break;
            
            case "fight":
                var target = arguments[0] as BaseAgent;
                
                // If a valid agent couldn't be found (there might not be one available right now)
                if (target == null)
                {
                    // Try the command again a bit
                    RetryLineOfCodeAfterDelay();
                    return;
                }
                
                void FightTheTarget()
                {
                    FightTarget(target, () =>
                    {
                        // TODO: Are these necessary, will they cause problems?
                        AddAgentInteraction(target);
                        
                        // Run the next line of code
                        _program.GotoNextLine();
                        _program.RunCurrentLine(this);
                    });
                }
                
                // If either subject is far away
                if (!InRange(target, FightingRange))
                    MoveToObjectWithCallback(target.transform, FightTheTarget);
                else
                    FightTheTarget();
                
                break;
            
            default:
                throw new Exception($"{name} tried to run unknown command '{command}'");
        }
    }

    private void FightTarget(BaseAgent target, Action callback)
    {
        GotoState<RobotFightingState>(this, target, callback);
    }

    private void RetryLineOfCodeAfterDelay()
    {
        // Wait a bit then try to run the same line again
        Debug.Log($"WARNING: Couldn't find a matching subject while running '{Program.CurrentLine}', will try again in a moment");
                    
        if(_waitingToRetryCommand != null)
            Debug.LogError($"Shouldn't reach this");
                    
        // TODO: Find a better place to put this magic number
        _waitingToRetryCommand = StartCoroutine(Util.Delay(1f, () =>
        {
            _waitingToRetryCommand = null;
            _program.RunCurrentLine(this);
        }));
    }

    public void RunProgram(Program program)
    {
        _program = program;
        _program.StartMain(this);
        _program.RunCurrentLine(Game.ControlledRobot);
    }
    
    private IEnumerator WaitInternal(DelayNode delayNode, Action callback)
    {
        while (delayNode.Waiting)
        {
            // Debug.Log($"Waiting...");
            yield return new WaitForEndOfFrame();
        }
        callback();
    }

    protected override void OnDoubleClick()
    {
        base.OnDoubleClick();
        
        Game.PossessRobot(this);
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
            "criminal" => IsCriminal,
            "disabled" => Disabled,
            "enabled" => !Disabled,
            "pluggedin" => PluggedInto != null,
            "unplugged" => PluggedInto == null,
            "held" => IsPickedUp,
            "police" => _team == RobotTeam.Police,
            "pedestrian" => _team == RobotTeam.Pedestrian,
            "player" => _team == RobotTeam.Player,
            _ => base.FilterBy(filter, computer)
        };
    }
    
    public void OnPickedUp()
    {
        // Disable the nav mesh agent
        _navAgent.enabled = false;
    }
    
    private void OnDisabled()
    {
        Debug.Log($"{Name} was disabled from lack of power");
        
        Idle();
        Disabled = true;
        _navAgent.isStopped = true;
        Program.Pause();
    }

    private void OnEnabled()
    {
        Debug.Log($"{Name} was re-enabled after being recharged");

        Disabled = false;
        _navAgent.isStopped = false;
        Program.Resume(this);
    }
}