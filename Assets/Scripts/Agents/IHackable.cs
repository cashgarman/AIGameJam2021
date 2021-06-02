using System;
using UnityEngine;

namespace Agents
{
	public interface IHackable
	{
		string Name { get; }
		Transform transform { get; }
		float HackingDifficulty { get; }
		void OnHacked();
		void OnHackingStarted(Robot hacker, Action callback);
		void OnHackingInterrupted();
	}
}