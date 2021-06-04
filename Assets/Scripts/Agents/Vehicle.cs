using UnityEngine;

namespace Agents
{
	public class Vehicle : Agent<Vehicle>, ICharger
	{
		public override string Name => "Vehicle";
		
		[SerializeField] private Transform _pluginSlot;
		[SerializeField] private float _chargePerSecond;

		public override Transform PluginSlot => _pluginSlot;
		public override BaseAgent PluggedInto { get; set; }
		public override float AlreadyVisitedCooldownMultiplier => 30f;
		public float ChargePerSecond => _chargePerSecond;

		public override void OnObjectPluggedIn(BaseAgent subject)
		{
			base.OnObjectPluggedIn(subject);
			
			// If the object plugged in is a battery
			if(subject is Battery battery)
				battery.StartCharging(this);
		}
		
		public override bool FilterBy(string filter, IComputer computer)
		{
			return filter switch
			{
				"pluggedin" => PluggedInto != null,
				"unplugged" => PluggedInto == null,
				_ => base.FilterBy(filter, computer)
			};
		}
	}
}