using States;
using UnityEngine;
using UnityEngine.UI;

public class Battery : Agent<Battery>, IPickup
{
    public override string Name => "Battery";

    public bool PickedUp { get; }

    [SerializeField] private float _maxCharge;
    [SerializeField] private Slider _chargeDisplay;

    private float _charge;
    [SerializeField] public float _energyPerCharge;

    public float Charge
    {
        get => _charge;
        set
        {
            _charge = Mathf.Clamp(value, 0f, _maxCharge);
            _chargeDisplay.value = _charge / _maxCharge;
        }
    }

    void Start()
    {
        Charge = _maxCharge;
        GotoState<BatteryIdleState>(this);
    }
}