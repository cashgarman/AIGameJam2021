using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public class GameUI : SerializedMonoBehaviour
	{
		[SerializeField] private AgentDetailsUI _agentDetailsUI;
		
		private static GameUI _instance;
		private AgentUI _selectedAgentUI;

		public static BaseAgent SelectedAgent => _instance._selectedAgentUI?.Agent;

		private void Awake()
		{
			_instance = this;
		}

		public static void OnAgentSelected(AgentUI agentUI)
		{
			Game.OnAgentSelected(agentUI.Agent);
			return;
			
			if(_instance._selectedAgentUI != agentUI)
				_instance._selectedAgentUI?.OnUnselected();
			
			_instance._selectedAgentUI = agentUI;
			
			UpdateAgentUI();
		}

		private static void UpdateAgentUI()
		{
			// TODO: Update the selected agent UI
			_instance._agentDetailsUI.SetVisible(SelectedAgent != null);
			
			if(SelectedAgent != null)
				_instance._agentDetailsUI.Populate(SelectedAgent);
		}
	}
}