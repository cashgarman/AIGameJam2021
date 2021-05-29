using States;
using UnityEngine;

public class Charger : Agent<Charger>
{
    public override string Name => "Charger";

    [SerializeField] private float _chargePerSecond;
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
}