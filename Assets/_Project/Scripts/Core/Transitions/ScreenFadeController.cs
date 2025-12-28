using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CityRush.Core.Transitions
{
    public class ScreenFadeController : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Image _fadeImage;
        [SerializeField] private float _fadeDuration = 0.15f;

        private Coroutine _fadeRoutine;

        public bool IsFading { get; private set; }

        private void Awake()
        {
            if (_canvas != null)
                _canvas.enabled = true;

            SetAlpha(0f);
        }

        public void FadeOut(Action onComplete = null)
        {
            StartFade(1f, onComplete);
        }

        public void FadeIn(Action onComplete = null)
        {
            StartFade(0f, onComplete);
        }

        private void StartFade(float targetAlpha, Action onComplete)
        {
            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);

            _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, onComplete));
        }

        private IEnumerator FadeRoutine(float targetAlpha, Action onComplete)
        {
            IsFading = true;

            float startAlpha = _fadeImage.color.a;
            float time = 0f;

            while (time < _fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / _fadeDuration);
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }

            SetAlpha(targetAlpha);
            IsFading = false;

            onComplete?.Invoke();
        }

        private void SetAlpha(float alpha)
        {
            Color c = _fadeImage.color;
            c.a = alpha;
            _fadeImage.color = c;
        }
    }
}
