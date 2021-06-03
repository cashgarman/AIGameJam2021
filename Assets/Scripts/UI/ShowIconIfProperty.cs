using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ShowIconIfProperty : MonoBehaviour
    {
        [SerializeField] private BaseAgent _target;
        [SerializeField] private string _propertyName;
        
        private Text _text;

        private void Awake()
        {
            _text = GetComponent<Text>();
        }

        void Update()
        {
            var propertyInfo = _target.GetType().GetProperty(_propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (propertyInfo.PropertyType == typeof(bool))
            {
                _text.enabled = (bool)propertyInfo.GetValue(_target);
            }
        }
    }
}
