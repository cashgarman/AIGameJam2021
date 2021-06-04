using States;
using UnityEngine;

public class Charger : Agent<Charger>, ICharger
{
    public override string Name => "Charger";
    public override Transform PluginSlot => _pluginSlot;    // TODO: Add multiple plugin slots
    public override BaseAgent PluggedInto { get; set; }
    public override float AlreadyVisitedCooldownMultiplier => 1f;

    [SerializeField] private float _chargePerSecond;
    [SerializeField] private Transform _pluginSlot;
    public float ChargePerSecond => _chargePerSecond;

    void Start()
    {
        GotoState<ChargerWorkingState>(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        // If a battery is near
        var battery = other.GetComponent<Battery>();
        if (battery != null)
        {
            // Let the battery know it's near a charger
            battery.StartCharging(this);
        }
    }
}