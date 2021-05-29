using System;
using UnityEngine;

namespace States
{
	public class RobotMovingToPositionState : RobotMovingState
	{
		public override string Name => "Moving";

		public override void OnEntered(params object[] args)
		{
			base.OnEntered();

			// Unpack arguments
			var destination = (Vector3)args[0];
			_onReached = (Action) args[1];

			// Move the target destination
			Debug.Log($"{Agent.Name} moving to {destination}");
			Agent.NavAgent.SetDestination(destination);
		}
	}
}