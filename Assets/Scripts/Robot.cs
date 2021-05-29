using System;
using System.Linq;
using States;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class Robot : Agent<Robot>
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

    // UI
    [SerializeField] private Slider _energyDisplay;
    [SerializeField] private TMP_Text _nameText;

    // Movement
    private NavMeshAgent _navAgent;
    [SerializeField] private float _reachedDestinationThreshold = 0.1f;
    public bool HasReachedDestination => _navAgent.remainingDistance < _reachedDestinationThreshold;
    public NavMeshAgent NavAgent => _navAgent;

    // Inventory
    [SerializeField] private Transform _hands;
    private IPickup _holding;

    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        BatteryLevel = _maxBatteryLevel;
        GotoState<RobotIdleState>(this);
    }

    protected override void Update()
    {
        base.Update();

        // If the robot's battery is running low
        // TODO: Find a more elegant way of handling state conditions here
        if (_batteryLevel < _batteryLowLevel
            && !IsInState<RobotMovingState>() && !IsInState<RobotPlugIntoBattery>() && !IsInState<RobotChargingState>())
        {
            // Find the nearest battery that has power in it
            var nearestBattery = FindObjectsOfType<Battery>().Where(b => b.Charge > _minChargedBatteryLevel && !b.PickedUp).OrderBy(b => b.Charge).FirstOrDefault();
            
            // If a charged battery can be found
            if (nearestBattery != null)
            {
                // Head to the battery and plug into it
                MoveToObjectAndChangeState(nearestBattery.transform, () =>
                {
                    GotoState<RobotPlugIntoBattery>(this, nearestBattery);
                });    
            }
        }

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
        // Update the robot's name
        _nameText.text = $"{Name}\n<size=50%>{_currentState.Name}</size>";
        
        // Update the battery remaining level
        _energyDisplay.value = BatteryLevel / _maxBatteryLevel;
    }

    public void MoveToPositionAndChangeState(Vector3 destination, Action onReached)
    {
        GotoState<RobotMovingToPositionState>(this, destination, onReached);
    }

    private void MoveToObjectAndChangeState(Transform target, Action onReached)
    {
        GotoState<RobotMovingToTargetState>(this, target, onReached);
    }

    public void Stop()
    {
        NavAgent.ResetPath();
    }

    public void Idle()
    {
        GotoState<RobotIdleState>(this);
    }

    public void PickUp(IPickup pickup)
    {
        if (_holding != null)
            throw new Exception($"Tried to pick up {pickup.name} while already holding {_holding.name}");

        _holding = pickup;
        pickup.transform.SetParent(_hands);
        pickup.transform.localPosition = Vector3.zero;
    }

    public void Drop(IPickup pickup)
    {
        if (_holding != pickup)
            throw new Exception($"Tried to drop {pickup.name} that wasn't holding");
        
        _holding = null;
        pickup.transform.SetParent(null);
        pickup.transform.position = new Vector3(pickup.transform.position.x, 0, pickup.transform.position.z); // TODO: Use rigid bodies to drop things
    }
}