using States;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Battery : Agent<Battery>, IPickup
{
    public override string Name => "Battery";

    public bool IsPickedUp { get; set; }

    [SerializeField] private float _maxCharge;
    [SerializeField] private Slider _chargeDisplay;

    private float _charge;
    [SerializeField] public float _energyPerCharge;
    [SerializeField] private float _lowChargeLevel;
    [SerializeField] private float _chargeOverride;
    [SerializeField] private TMP_Text _nameText;

    public bool IfFull => _charge == _maxCharge;

    public float Charge
    {
        get => _charge;
        set
        {
            _charge = Mathf.Clamp(value, 0f, _maxCharge);
            _chargeDisplay.value = _charge / _maxCharge;
        }
    }

    public bool IsLow => _charge < _lowChargeLevel;
    public bool IsCharging { get; set; }

    void Start()
    {
        Charge = _chargeOverride > 0 ? _chargeOverride : _maxCharge;
        GotoState<BatteryIdleState>(this);
    }
    
    protected override void Update()
    {
        base.Update();

        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // Update the battery's name and state
        _nameText.text = $"{Name}\n<size=50%>{_currentState.Name}</size>";
    }

    public void OnNearbyCharger(Charger charger)
    {
        // If the battery isn't full
        if (!IfFull)
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
}