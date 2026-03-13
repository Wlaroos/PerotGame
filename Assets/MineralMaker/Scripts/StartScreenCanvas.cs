using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class StartScreenCanvas : MonoBehaviour//, IPointerClickHandler
{
    [SerializeField] private float _fadeDuration = 1.0f;
    [SerializeField] private GameObject _chooseMineralCanvas;
    private CanvasGroup _canvasGroup;
    private CanvasGroup _childCanvasGroup;
    private GameObject _panel01;
    private GameObject _panel02;
    private GameObject _panel03;
    private int _currentPanelIndex = 0;
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 1f;

        _panel01 = transform.GetChild(0).gameObject;
        _panel02 = transform.GetChild(1).gameObject;
        _panel03 = transform.GetChild(2).gameObject;

        _panel01.SetActive(true);
        _panel02.SetActive(false);
        _panel03.SetActive(false);
    }

    void Start()
    {
        _chooseMineralCanvas.SetActive(false);
    }
    // public void OnPointerClick(PointerEventData eventData)
    // {
    //     // StartCoroutine(FadeCoroutine());
    //     if (_currentPanelIndex == 0)
    //     {
    //         _panel01.SetActive(false);
    //         _panel02.SetActive(true);
    //         _currentPanelIndex++;
    //     }
    //     else if (_currentPanelIndex == 1)
    //     {
    //         _panel02.SetActive(false);
    //         _panel03.SetActive(true);
    //         _currentPanelIndex++;
    //     }
    //     else
    //     {
    //         StartCoroutine(FadeCoroutine());
    //     }
    // }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_currentPanelIndex == 0)
            {
                _panel01.SetActive(false);
                _panel02.SetActive(true);
                _currentPanelIndex++;
            }
            else if (_currentPanelIndex == 1)
            {
                _panel02.SetActive(false);
                _panel03.SetActive(true);
                _currentPanelIndex++;
            }
            else
            {
                StartCoroutine(FadeCoroutine());
            }
        }
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
        _chooseMineralCanvas.SetActive(true);

        Destroy(gameObject);
    }
}
