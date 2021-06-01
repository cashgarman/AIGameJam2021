using Programming;
using TMPro;
using UnityEngine;

namespace UI
{
	public class LineOfCodeUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text _codeText;
		private CodeLine _lineOfCode;

		public void Populate(CodeLine lineOfCode)
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
			_codeText.text = _lineOfCode.ToString();
			_codeText.color = _lineOfCode.Status switch
			{
				CodeLine.LineStatus.Pending => Color.white,
				CodeLine.LineStatus.Running => Color.yellow,
				CodeLine.LineStatus.Done => Color.green,
			};
		}
	}
}
