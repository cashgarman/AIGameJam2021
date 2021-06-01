using System;
using Programming;
using TMPro;
using UnityEngine;

namespace UI
{
	public class LineOfCodeUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text _codeText;
		private Program.LineOfCode _lineOfCode;

		public void Populate(Program.LineOfCode lineOfCode)
		{
			_lineOfCode = lineOfCode;
			UpdateUI();
		}

		private void Update()
		{
			UpdateUI();
		}

		private void UpdateUI()
		{
			_codeText.text = _lineOfCode.Code;
			_codeText.color = _lineOfCode.Status switch
			{
				Program.LineOfCode.LineStatus.Pending => Color.white,
				Program.LineOfCode.LineStatus.Running => Color.yellow,
				Program.LineOfCode.LineStatus.Done => Color.green,
			};
		}
	}
}
