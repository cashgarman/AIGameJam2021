namespace States
{
	public class RobotPlugIntoBattery : AgentState<Robot>
	{
		public override string Name => "Plugging In";

		public override void OnEntered(params object[] args)
		{
			base.OnEntered(args);
			
			// Unpack arguments
			var battery = (Battery) args[0];

			// Pick up the battery
			Agent.PickUp(battery);
			
			// Start charging
			Agent.GotoState<RobotChargingState>(Agent, battery);
		}
	}
}