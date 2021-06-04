using UnityEngine;

namespace Agents
{
	public class HomeAgent : Agent<HomeAgent>
	{
		public override string Name => "Home";
		
		public override Transform PluginSlot { get; }
		public override BaseAgent PluggedInto { get; set; }
		public override float AlreadyVisitedCooldownMultiplier => 1f;
	}
}