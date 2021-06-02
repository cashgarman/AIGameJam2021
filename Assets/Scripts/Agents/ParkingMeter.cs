using System;
using System.Collections;
using Programming;
using UI;
using UnityEngine;

namespace Agents
{
	public class ParkingMeter : Agent<ParkingMeter>, IHackable
	{
		public override string Name => "Parking Meter";
		
		[SerializeField] private ProgressUI _progress;

		public override Transform PluginSlot { get; }
		public override bool IsPluggedIn { get; set; }
		public override BaseAgent PluggedIn { get; set; }

		private Coroutine _hackingCoroutine;

		public override bool FilterBy(string filter, IComputer computer)
		{
			// NOTE: Note used
			return true;
		}

		public float HackingDifficulty => 5f;
		public bool IsCrimeToHack => true;
		public void OnHacked()
		{
			Debug.Log($"{Name} has been hacked");	
		}

		public void OnHackingStarted(Robot hacker, Action callback)
		{
			if (IsCrimeToHack)
			{
				// Flag the hacker as a criminal
				hacker.FlagACriminal();
				
				// Make a shout out that a crime is in progress
				Shout(ShoutType.CrimeInProgress);
			}
			
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
	}
}
