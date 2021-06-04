using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
	public class BackgroundUI : MonoBehaviour
	{
		private void OnMouseUp()
		{
			if(!EventSystem.current.IsPointerOverGameObject())
				GameUI.OnAgentSelected(null);
		}
	}
}