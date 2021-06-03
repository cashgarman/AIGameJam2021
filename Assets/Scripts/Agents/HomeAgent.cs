using UnityEngine;

namespace Agents
{
	public class HomeAgent : Agent<HomeAgent>
	{
		public override string Name => "Home";
		
		public override Transform PluginSlot { get; }
		public override bool IsPluggedIn { get; set; }
		public override BaseAgent PluggedIn { get; set; }
	}
}