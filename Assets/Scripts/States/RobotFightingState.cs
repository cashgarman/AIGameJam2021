using System;
using UnityEngine;

namespace States
{
	public class RobotFightingState : AgentState<Robot>
	{
		public override string Name => "Fighting";
		
		private BaseAgent _target;
		private Action _callback;
		private float _attackCooldown;
		private readonly string[] _attackAnimationTriggerNames = {
			"Attack1",
			"Attack2",
		};

		public override void OnEntered(params object[] args)
		{
			base.OnEntered(args);
			
			// Unpack arguments
			_target = (BaseAgent) args[0];
			_callback = (Action) args[1];

			Debug.Log($"{Agent.Name} starting to fight {_target.Name}");
			
			// Start the attack cooldown
			_attackCooldown = Agent.AttackCooldown;
		}

		public override void OnTick()
		{
			base.OnTick();

			// If the target has moved too far away
			if (!Agent.InRange(_target, Agent.FightingRange))
			{
				// Move closer the target
				Agent.MoveToObjectWithCallback(_target.transform, () =>
				{
					Agent.GotoState<RobotFightingState>(Agent, _target, _callback);
				});
				return;
			}

			// Cooldown the charging attack
			_attackCooldown -= Time.deltaTime;
				
			// If we're ready to attack again
			if (_attackCooldown <= 0f)
			{
				// Play the attack animation
				var attackAnimationTrigger = Util.RandomElement(_attackAnimationTriggerNames);
				Agent.PlayAnimationTrigger(attackAnimationTrigger);
					
				// Apply damage to the target
				_target.ApplyDamage(Agent.AttackDamage);
				
				// Reset the attack cooldown
				_attackCooldown = Agent.AttackCooldown;
			}

			// If the target is knocked out
			if (_target.IsKnockedOut)
			{
				// Return to idle
				Agent.Idle();
				
				// Call the callback
				_callback();
			}
				
			// If this robot got knocked out
			if (Agent.IsKnockedOut)
			{
				// Return idle
				Agent.Idle();
			}
		}
	}
}