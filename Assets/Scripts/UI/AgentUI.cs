using DG.Tweening;
using UnityEngine;

namespace UI
{
	public class AgentUI : MonoBehaviour
	{
		[SerializeField] private bool _informAgentOfClick = true;
		[SerializeField] private MeshRenderer _highlightMeshRenderer;
		[SerializeField] private bool _highlightable;
		[SerializeField] private bool _selectable;

		private BaseAgent _agent;
		private bool _highlighted;
		private bool _selected;
		[SerializeField] private CanvasGroup _agentUICanvasGroup;
		[SerializeField] private float _fadeDuration;

		public BaseAgent Agent => _agent;

		private void Awake()
		{
			_agent = GetComponentInParent<BaseAgent>();
			_agentUICanvasGroup.DOFade(0, 0);
			_highlightMeshRenderer.material.DOFade(0, 0);
		}

		private void OnMouseEnter()
		{
			if (!_highlightable)
				return;

			_agentUICanvasGroup.DOFade(1, _fadeDuration);
			_highlightMeshRenderer.material.DOFade(1, _fadeDuration);
		}

		private void OnMouseExit()
		{
			_agentUICanvasGroup.DOFade(0, _fadeDuration);
			_highlightMeshRenderer.material.DOFade(0, _fadeDuration);
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
			// _selected = false;
			// SetHighlightVisible(_highlighted);
		}
	}
}