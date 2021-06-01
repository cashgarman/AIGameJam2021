using UnityEngine;

namespace UI
{
	public class BackgroundUI : MonoBehaviour
	{
		private void OnMouseUp()
		{
			GameUI.OnAgentSelected(null);
		}
	}
}