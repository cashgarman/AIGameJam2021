using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ProgressUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _progressLabel;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private CanvasGroup _progressContainer;
        [SerializeField] private float _fadeDuration = 0.25f;

        private TweenerCore<float,float,FloatOptions> _progressAnimation;

        public void Begin(string label, float duration)
        {
            // Fade in the progress UI
            _progressContainer.alpha = 0f;
            _progressContainer.DOFade(1f, _fadeDuration);
            
            // Set the process label
            _progressLabel.text = label;
            _progressLabel.text = label;
            
            // Start the progress animation
            _progressSlider.value = 0;
            _progressAnimation = _progressSlider.DOValue(1f, duration).SetEase(Ease.Linear).OnComplete(End);
        }

        public void End()
        {
            // Stop the progress animation
            _progressAnimation.Kill();
            _progressAnimation = null;
            
            // Fade out the progress UI
            _progressContainer.DOFade(0f, _fadeDuration);
        }
    }
}
