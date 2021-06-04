using System;
using System.Collections;
using UI;
using UnityEngine;

namespace Agents
{
	public class Waypoint : Agent<Waypoint>
	{
		public enum WaypointType
		{
			Police,
			Pedestrian,
			Player
		}
		
		public override string Name => "Parking Meter";
		
		[SerializeField] private ProgressUI _progress;

		public override Transform PluginSlot { get; }
		public override BaseAgent PluggedInto { get; set; }
		public override float AlreadyVisitedCooldownMultiplier => 10f;

		private Coroutine _hackingCoroutine;
		[SerializeField] private WaypointType _waypointType;

		public float HackingDifficulty => 5f;

		public void OnHacked()
		{
			Debug.Log($"{Name} has been hacked");	
		}

		public void OnHackingStarted(Robot hacker, Action callback)
		{
			Debug.Log($"{hacker.Name} is hacking {Name}");
			_hackingCoroutine = StartCoroutine(StartHacking(hacker, callback));
		}

		public void OnHackingInterrupted()
		{
			Debug.Log($"Hacking of {Name} interrupted");
			StopCoroutine(_hackingCoroutine);
			_progress.End();
		}

		private IEnumerator StartHacking(Robot hacker, Action callback)
		{
			var duration = HackingDifficulty / hacker.HackingSpeed;
			_progress.Begin("Hacking", duration);
			yield return new WaitForSeconds(duration);
			OnHacked();
			callback();
		}

		public override bool FilterBy(string filter, IComputer computer)
		{
			return filter switch
			{
				"police" => _waypointType == WaypointType.Police,
				"pedestrian" => _waypointType == WaypointType.Pedestrian,
				"player" => _waypointType == WaypointType.Player,
				_ => base.FilterBy(filter, computer)
			};
		}
	}
}
