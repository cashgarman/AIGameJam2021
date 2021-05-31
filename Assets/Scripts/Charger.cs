using System;
using Programming;
using States;
using UnityEngine;

public class Charger : Agent<Charger>
{
    public override string Name => "Charger";
    public override Transform PluginSlot => _pluginSlot;    // TODO: Add multiple plugin slots
    public override bool IsPluggedIn { get; set; }
    public override BaseAgent PluggedIn { get; set; }

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
            battery.OnNearbyCharger(this);
        }
    }
    
    public override float GetOrderBy(string sortType)
    {
        // TODO: Nothing to sort chargers by right now
        return 1;
    }

    public override bool FilterBy(string filter, IComputer computer)
    {
        return filter switch
        {
            "next" => !computer.HasInteractedWithRecently(this),
            _ => throw new Exception($"Invalid filter provided to {GetType().Name}: {filter}")
        };
    }
}