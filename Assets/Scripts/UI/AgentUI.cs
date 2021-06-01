using UnityEngine;

namespace UI
{
	public class AgentUI : MonoBehaviour
	{
		[SerializeField] private bool _informAgentOfClick = true;
		[SerializeField] private Transform _highlight;
		[SerializeField] private bool _highlightable;
		[SerializeField] private bool _selectable;

		private BaseAgent _agent;
		private Vector3 _initialHighlightScale;
		private bool _highlighted;
		private bool _selected;
		
		public BaseAgent Agent => _agent;

		private void Awake()
		{
			_agent = GetComponent<BaseAgent>();
			_initialHighlightScale = _highlight.localScale;
		}

		private void OnMouseEnter()
		{
			if (!_highlightable)
				return;
		
			// Show the highlight
			ResetHighlight();
			SetHighlightVisible(true);
		}

		private void OnMouseExit()
		{
			// Hide the highlight
			_highlighted = false;
			SetHighlightVisible(_selected);
		}
	
		private void SetHighlightVisible(bool visible)
		{
			_highlight.gameObject.SetActive(visible);
		}

		private void ResetHighlight()
		{
			_highlight.localScale = _initialHighlightScale;
		}

		private void OnMouseUp()
		{
			if (!_selectable)
				return;

			_selected = true;
		
			// Start animating the highlight
			// _highlight.DOT

			GameUI.OnAgentSelected(this);
		
			// Let the agent know it was clicked
			if (_informAgentOfClick)
				_agent.OnClicked();
		}

		public void OnUnselected()
		{
			_selected = false;
			SetHighlightVisible(_highlighted);
		}
	}
}