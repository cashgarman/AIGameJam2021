using System;
using UnityEngine;

namespace States
{
	public class RobotMovingToTargetState : RobotMovingState
	{
		private Transform _target;
		private Vector3 _prevTargetPosition;

		public override string Name => "Moving";

		public override void OnEntered(params object[] args)
		{
			base.OnEntered();

			// Unpack arguments
			_target = (Transform)args[0];
			_onReached = (Action) args[1];
			_abortProvider = (Func<bool>) args[2];

			// Move the target destination
			Debug.Log($"{Agent.Name} moving to object {_target.name}");
			Agent.NavAgent.SetDestination(_target.position);
			_prevTargetPosition = _target.position;
		}

		public override void OnTick()
		{
			// If the robot is holding the target it's trying to reach
			if (_target == Agent.Holding)
				return;
			
			// If the target has moved (and the path has already been calculated)
			if (Vector3.Distance(_target.position, _prevTargetPosition) > Agent.ReachedDestinationThreshold && !Agent.NavAgent.pathPending)
			{
				// TODO: This is happened quite a lot, look into that
				//Debug.Log($"Target moved, recalculating path");
				
				// Move to the target's new position
				Agent.NavAgent.SetDestination(_target.position);
			}
			
			base.OnTick();
		}
	}
}