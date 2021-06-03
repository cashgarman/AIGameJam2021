using UnityEngine;

namespace States
{
	public class BatteryChargingState : AgentState<Battery>
	{
		public override string Name => "Charging";

		private Charger _charger;

		public override void OnEntered(params object[] args)
		{
			base.OnEntered(args);
			
			// Unpack arguments
			_charger = (Charger) args[0];
			
			// Flag the battery as charging
			Agent.IsCharging = true;
		}

		public override void OnTick()
		{
			base.OnTick();
			
			// Take some charge from the charger
			Agent.Charge += _charger.ChargePerSecond * Time.deltaTime;
			
			// If the battery is full
			if (Agent.IsFull)
			{
				// Flag the battery as not charging
				Agent.IsCharging = false;
				
				// Stop charging
				Agent.GotoState<BatteryIdleState>(Agent);
			}
		}
	}
}