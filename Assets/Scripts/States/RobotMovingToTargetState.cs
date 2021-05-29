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

			// Move the target destination
			Debug.Log($"{Agent.Name} moving to object {_target.name}");
			Agent.NavAgent.SetDestination(_target.position);
			_prevTargetPosition = _target.position;
		}

		public override void OnTick()
		{
			// If the target has moved
			if (_target.position != _prevTargetPosition)
			{
				// Move to the target's new position
				Agent.NavAgent.SetDestination(_target.position);
			}
			
			base.OnTick();
		}
	}
}