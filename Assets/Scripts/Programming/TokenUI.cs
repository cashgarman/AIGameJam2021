using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Programming
{
    public class TokenUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _background;

        public void Init(Token token)
        {
            _nameText.text = token.Word;
            _background.color = Compiler.GetTokenTypeColor(token.Type);
        }
    }
}
