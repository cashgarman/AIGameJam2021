using System;
using UnityEngine;

namespace States
{
	public abstract class RobotMovingState : AgentState<Robot>
	{
		protected Action _onReached;
		protected Func<bool> _abortProvider;
		
		public override void OnTick()
		{
			base.OnTick();
			
			// Wait until the path to the destination has been calculated
			if (Agent.NavAgent.pathPending)
				return;
			
			// Drain the robot's battery
			Agent.BatteryLevel -= Agent._movingBatteryDrainPerSecond * Time.deltaTime;
			
			// Check if we should abort moving
			if (_abortProvider != null && _abortProvider())
			{
				Debug.Log($"Aborting moving");
				
				// // Stop moving
				Agent.Stop();
				
				// Abort moving and return to idle
				Agent.Idle();
			}
			
			// If the robot has reached the destination
			if (Agent.HasReachedDestination)
			{
				Debug.Log($"{Agent.Name} arrived at destination");
				
				// Stop moving
				Agent.Stop();
				
				// Call the on reached callback
				_onReached?.Invoke();
				
				// Change to the arrival state
				//Agent.GotoState(Agent, _onArriveState, _object);
			}
		}
	}
}