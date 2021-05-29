using UnityEngine;

namespace States
{
	public class RobotChargingState : AgentState<Robot>
	{
		public override string Name => "Charging";
		
		private Battery _battery;

		public override void OnEntered(params object[] args)
		{
			base.OnEntered(args);
			
			// Unpack arguments
			_battery = (Battery) args[0];
		}

		public override void OnTick()
		{
			base.OnTick();

			// Transfer some power from the battery to the robot
			var chargeToTransfer = Mathf.Min(_battery.Charge, Agent._batteryChargeTransferPerSecond * Time.deltaTime);
			_battery.Charge -= chargeToTransfer;
			Agent.BatteryLevel += chargeToTransfer * _battery._energyPerCharge;
			
			// TODO: Move all the battery related stuff into a BatteryDrainingState
			
			// If the robot is fully charged
			if (Agent.FullyCharged)
			{
				Debug.Log($"Robot fully charged");
				
				// Drop the battery
				Agent.Drop(_battery);
				
				// Go back to idling
				Agent.Idle();
			}
			
			// If the battery is empty
			if (_battery.Charge <= 0)
			{
				Debug.Log($"Battery fully drained");
				
				// TODO: Take the battery for charging
				
				// Drop the battery
				Agent.Drop(_battery);
				
				// Go back to idling
				Agent.Idle();
			}
		}
	}
}