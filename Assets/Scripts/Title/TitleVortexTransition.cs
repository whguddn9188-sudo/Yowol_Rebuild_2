
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class TitleVortexTransition : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private Button startButton;
    [SerializeField] private CanvasGroup titleMenu;
    [SerializeField] private Camera titleCamera;
    [SerializeField] private Transform zoomTarget;

    [Header("Transition UI")]
    [SerializeField] private Image vortexImage;
    [SerializeField] private Image blackFade;

    [Header("Effect Settings")]
    [SerializeField] private float duration = 1.8f;
    [SerializeField] private float zoomFactor = 0.5f;
    [SerializeField] private float vortexFinalScale = 7f;
    [SerializeField] private float vortexTurns = 2.5f;

    [Header("Scene Loading")]
    [SerializeField] private string nextScene = "MainStage";
    [SerializeField] private bool useExistingSceneLoader = false;
    [SerializeField] private UnityEvent onBlackoutComplete;

    private bool transitioning;

    private void Awake()
    {
        if (titleCamera == null)
            titleCamera = Camera.main;

        if (vortexImage != null)
        {
            SetAlpha(vortexImage, 0f);
            vortexImage.rectTransform.localScale =
                Vector3.one * 0.2f;
        }

        if (blackFade != null)
            SetAlpha(blackFade, 0f);
    }

    public void StartTransition()
    {
        if (transitioning) return;

        if (vortexImage == null || blackFade == null)
        {
            Debug.LogError("Transition UI가 연결되지 않았습니다.");
            return;
        }

        transitioning = true;

        if (startButton != null)
            startButton.interactable = false;

        if (titleMenu != null)
        {
            titleMenu.interactable = false;
            titleMenu.blocksRaycasts = false;
        }

        StartCoroutine(PlayTransition());
    }

    private IEnumerator PlayTransition()
    {
        float elapsed = 0f;
        float length = Mathf.Max(0.1f, duration);

        float originalSize = titleCamera != null
            ? titleCamera.orthographicSize : 5f;

        Vector3 originalPosition = titleCamera != null
            ? titleCamera.transform.position : Vector3.zero;

        Vector3 targetPosition = originalPosition;

        if (zoomTarget != null)
        {
            targetPosition = new Vector3(
                zoomTarget.position.x,
                zoomTarget.position.y,
                originalPosition.z
            );
        }

        float originalMenuAlpha =
            titleMenu != null ? titleMenu.alpha : 1f;

        while (elapsed < length)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / length);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            // Camera close-up
            if (titleCamera != null && titleCamera.orthographic)
            {
                titleCamera.orthographicSize = Mathf.Lerp(
                    originalSize,
                    originalSize * zoomFactor,
                    smooth
                );

                titleCamera.transform.position = Vector3.Lerp(
                    originalPosition,
                    targetPosition,
                    smooth
                );
            }

            // Hide title menu
            if (titleMenu != null)
                titleMenu.alpha = originalMenuAlpha *
                    (1f - Mathf.Clamp01(t / 0.32f));

            // Vortex
            float appear = Mathf.Clamp01((t - 0.12f) / 0.58f);
            float vortexSmooth =
                Mathf.SmoothStep(0f, 1f, appear);

            vortexImage.rectTransform.localScale =
                Vector3.one * Mathf.Lerp(
                    0.2f, vortexFinalScale, vortexSmooth
                );

            vortexImage.rectTransform.localRotation =
                Quaternion.Euler(
                    0f, 0f, -360f * vortexTurns * t
                );

            SetAlpha(vortexImage,
                Mathf.Clamp01((t - 0.12f) / 0.3f));

            // Blackout
            SetAlpha(blackFade,
                Mathf.Clamp01((t - 0.73f) / 0.27f));

            yield return null;
        }

        SetAlpha(blackFade, 1f);

        if (useExistingSceneLoader)
        {
            onBlackoutComplete?.Invoke();
        }
        else
        {
            yield return SceneManager.LoadSceneAsync(nextScene);
        }
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
