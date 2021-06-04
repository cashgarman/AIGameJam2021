using System;
using Agents;
using Programming;
using States;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Battery : Agent<Battery>, IPickup
{
    public override string Name => "Battery";
    public override Transform PluginSlot => null;
    public override BaseAgent PluggedInto { get; set; }
    public override float AlreadyVisitedCooldownMultiplier => 1f;

    public bool IsPickedUp { get; set; }

    [SerializeField] private float _maxCharge;
    [SerializeField] private Slider _chargeDisplay;

    private float _charge;
    [SerializeField] private float _lowChargePercent;
    [SerializeField] private float _highChargePercent;
    [SerializeField] private float _chargeOverride;

    public float Charge
    {
        get => _charge;
        set
        {
            _charge = Mathf.Clamp(value, 0f, _maxCharge);
            _chargeDisplay.value = _charge / _maxCharge;
        }
    }
    
    public bool IsFull => _charge == _maxCharge;
    public bool IsEmpty => _charge == 0f;
    public bool IsLow => _charge / _maxCharge < _lowChargePercent;
    public bool IsHigh => _charge / _maxCharge > _highChargePercent;
    public bool IsCharging { get; set; }

    void Awake()
    {
        Charge = _chargeOverride > 0 ? _chargeOverride : _maxCharge;
    }

    private void Start()
    {
        GotoState<BatteryIdleState>(this);
    }

    protected override void Update()
    {
        base.Update();

        HandleDebugControls();
    }

    private void HandleDebugControls()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            Charge -= 10f;
        }
    }

    public void StartCharging(ICharger charger)
    {
        // If the battery isn't full
        if (!IsFull)
        {
            GotoState<BatteryChargingState>(this, charger);
        }
    }
    
    public override float GetOrderBy(string sortType)
    {
        return sortType switch
        {
            "fullest" => _charge / _maxCharge,
            "emptiest" => 1f - _charge / _maxCharge
        };
    }
    
    public override bool FilterBy(string filter, IComputer computer)
    {
        return filter switch
        {
            "empty" => IsLow, // TODO: Should this be zero, or close enough
            "full" => IsHigh, // TODO: Should this be zero, or close enough
            "unplugged" => PluggedInto == null,
            "pluggedin" => PluggedInto != null,
            "held" => IsPickedUp,
            _ => base.FilterBy(filter, computer)
        };
    }
    
    public void OnPickedUp()
    {
    }
}