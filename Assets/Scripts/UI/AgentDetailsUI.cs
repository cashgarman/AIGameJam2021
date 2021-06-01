using System;
using Programming;
using TMPro;
using UnityEngine;

namespace UI
{
	public class AgentDetailsUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text _nameLabel;
		[SerializeField] private Transform _codeList;
		[SerializeField] private LineOfCodeUI _lineOfCodePrefab;

		private void Start()
		{
			_codeList.ClearChildren();
		}

		public void SetVisible(bool visible)
		{
			gameObject.SetActive(visible);
		}

		public void Populate(BaseAgent agent)
		{
			_nameLabel.text = agent.Name;

			// If this is a robot
			// TODO: Other types of agents should be able to have code too
			if (agent is Robot robot)
			{
				// Add the code
				PopulateCode(robot.Program);
			}
		}

		private void PopulateCode(Program program)
		{
			_codeList.ClearChildren();
			foreach (var lineOfCode in program.Lines)
			{
				var newLine = Instantiate(_lineOfCodePrefab, _codeList);
				newLine.Populate(lineOfCode);
			}
		}
	}
}