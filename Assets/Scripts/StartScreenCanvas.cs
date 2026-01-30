using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class StartScreenCanvas : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private float _fadeDuration = 1.0f;
    private CanvasGroup _canvasGroup;
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        StartCoroutine(FadeCoroutine());
    }

    private IEnumerator FadeCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / _fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        
        Destroy(gameObject);
    }
}
