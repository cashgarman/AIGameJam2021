using UnityEngine;

namespace States
{
	public class RobotIdleState : AgentState<Robot>
	{
		public override string Name => "Idle";

		public override void OnTick()
		{
			base.OnTick();

			// Drain the robot's battery
			Agent.BatteryLevel -= Agent._idleBatteryDrainPerSecond * Time.deltaTime;
		}
	}
}