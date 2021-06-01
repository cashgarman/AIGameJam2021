using TMPro;
using UnityEngine;

namespace UI
{
	public class CommandLineErrorUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text _errorText;
		[SerializeField] private GameObject _errorContainer;

		public void Show(string message)
		{
			_errorText.text = message;
			_errorContainer.SetActive(true);
		}

		public void Hide()
		{
			_errorContainer.SetActive(false);
		}
	}
}