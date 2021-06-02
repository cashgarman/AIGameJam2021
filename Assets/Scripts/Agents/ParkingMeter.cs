using System.Collections;
using Programming;
using UnityEngine;

namespace Agents
{
	public class ParkingMeter : Agent<ParkingMeter>, IHackable
	{
		public override string Name => "Parking Meter";

		public override Transform PluginSlot { get; }
		public override bool IsPluggedIn { get; set; }
		public override BaseAgent PluggedIn { get; set; }

		private Coroutine _hackingCoroutine;

		public override float GetOrderBy(string sortType)
		{
			// NOTE: Note used
			return 1;
		}

		public override bool FilterBy(string filter, IComputer computer)
		{
			// NOTE: Note used
			return true;
		}

		public float HackingDifficulty => 5f;
		public void OnHacked()
		{
			Debug.Log($"{Name} has been hacked");	
		}

		public void OnHackingStarted(Robot hacker)
		{
			Debug.Log($"{hacker.Name} is hacking {Name}");
			_hackingCoroutine = StartCoroutine(StartHacking(hacker));
		}

		public void OnHackingInterrupted()
		{
			Debug.Log($"Hacking of {Name} interrupted");
			StopCoroutine(_hackingCoroutine);
		}

		private IEnumerator StartHacking(Robot hacker)
		{
			yield return new WaitForSeconds(HackingDifficulty / hacker.HackingSpeed);
			OnHacked();
		}
	}
}
