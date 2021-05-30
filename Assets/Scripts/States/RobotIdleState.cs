using System.Linq;
using UnityEngine;

namespace States
{
	public class RobotIdleState : AgentState<Robot>
	{
		public override string Name => "Idle";

		public override void OnTick()
		{
			base.OnTick();
			
			// TODO: All this should be replaced by code
			return;
			
			// Drain the robot's battery
			// TODO: Pull this draining logic into a base class
			Agent.BatteryLevel -= Agent._idleBatteryDrainPerSecond * Time.deltaTime;
			
			// If the robot's battery is running low
			// TODO: Find a more elegant way of handling state conditions here
			if (Agent.IsBatteryLow)
			{
				// Find the nearest battery that has power in it
				var nearestBattery = GameObject.FindObjectsOfType<Battery>()
					.Where(b => !b.IsPickedUp && !b.IsCharging)
					.OrderByDescending(b => Vector3.Distance(Agent.transform.position, b.transform.position))
					.ThenByDescending(b => b.Charge)
					.FirstOrDefault();
            
				// If a charged battery can be found
				if (nearestBattery != null)
				{
					// Head to the battery and plug into it
					Agent.MoveToObjectWithCallback(nearestBattery.transform, () =>
					{
						Agent.GotoState<RobotPlugIntoBattery>(Agent, nearestBattery);
					},
					// Only continue if the battery isn't picked up
					() => nearestBattery.IsPickedUp);
					return;
				}
			}
			
			// Check if there are any drained batteries around
			var nearestLowBattery = GameObject.FindObjectsOfType<Battery>().Where(b => b.IsLow && !b.IsCharging && !b.IsPickedUp)
				.OrderByDescending(b => Vector3.Distance(Agent.transform.position, b.transform.position)).FirstOrDefault();
			if (nearestLowBattery != null)
			{
				Agent.MoveToObjectWithCallback(nearestLowBattery.transform, () =>
					{
						// Pickup the battery
						Agent.PickUp(nearestLowBattery);
				
						// Find the nearest charger
						var nearestCharger = GameObject.FindObjectsOfType<Charger>().OrderByDescending(c => Vector3.Distance(Agent.transform.position, c.transform.position)).FirstOrDefault();
						if (nearestCharger != null)
						{
							// Move to the nearest charger
							Agent.MoveToObjectWithCallback(nearestCharger.transform, () =>
							{
								// Drop the battery
								Agent.Drop(nearestLowBattery);
						
								// Go to idle
								Agent.Idle();
							});
						}
					},
					// Only continue if the battery isn't picked up and isn't charging
					() => nearestLowBattery.IsPickedUp || nearestLowBattery.IsCharging);
				return;
			}
		}
	}
}