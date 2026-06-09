using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FixedAspectRatioController : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f;
    private const float AspectRatioTolerance = 0.0001f;
    private const float PixelTolerance = 1f;
    private const int InitialRefreshFrames = 30;
    private const float LogicalContentWidth = 800f;
    private const float LogicalContentHeight = 450f;
    private const string SafeAreaRootName = "AspectSafeAreaRoot";
    private const string ContentRootName = "AspectContentRoot";
    private const string LetterboxBarsName = "LetterboxBars";

    private static readonly HashSet<string> supportedScenes = new HashSet<string>
    {
        "Start",
        "Junmo"
    };

    private static FixedAspectRatioController activeController;
    private static Rect contentPixelRect;
    private static bool hasContentPixelRect;
    private static bool sceneHooked;
    private static int pendingRefreshFrameCount;

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private int lastDisplayWidth = -1;
    private int lastDisplayHeight = -1;
    private int lastCameraSignature = -1;
    private FullScreenMode lastFullScreenMode = (FullScreenMode)(-1);
    private int pendingForcedApplies = InitialRefreshFrames;
    private readonly List<Transform> childrenToMove = new List<Transform>();

    public static Rect ContentPixelRect
    {
        get
        {
            if (hasContentPixelRect)
            {
                return contentPixelRect;
            }

            return new Rect(0f, 0f, Screen.width, Screen.height);
        }
    }

    public static bool HasContentPixelRect => hasContentPixelRect;

    public static void RequestRefresh(int frameCount = InitialRefreshFrames)
    {
        if (activeController == null)
        {
            pendingRefreshFrameCount = Mathf.Max(pendingRefreshFrameCount, frameCount);
            return;
        }

        activeController.pendingForcedApplies = Mathf.Max(activeController.pendingForcedApplies, frameCount);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activeController = null;
        contentPixelRect = Rect.zero;
        hasContentPixelRect = false;
        sceneHooked = false;
        pendingRefreshFrameCount = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!sceneHooked)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHooked = true;
        }

        EnsureControllerForScene(SceneManager.GetActiveScene());
    }

    public static Vector3 ClampScreenPointToContent(Vector3 screenPoint)
    {
        Rect contentRect = ContentPixelRect;
        screenPoint.x = Mathf.Clamp(screenPoint.x, contentRect.xMin, contentRect.xMax);
        screenPoint.y = Mathf.Clamp(screenPoint.y, contentRect.yMin, contentRect.yMax);
        return screenPoint;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureControllerForScene(scene);
    }

    private static void EnsureControllerForScene(Scene scene)
    {
        if (!scene.IsValid() || !supportedScenes.Contains(scene.name))
        {
            hasContentPixelRect = false;
            return;
        }

        FixedAspectRatioController[] controllers = FindObjectsByType<FixedAspectRatioController>(FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i].gameObject.scene == scene)
            {
                return;
            }
        }

        GameObject controllerObject = new GameObject(nameof(FixedAspectRatioController));
        SceneManager.MoveGameObjectToScene(controllerObject, scene);
        controllerObject.AddComponent<FixedAspectRatioController>();
    }

    private void OnEnable()
    {
        activeController = this;
        pendingForcedApplies = Mathf.Max(pendingForcedApplies, pendingRefreshFrameCount);
        pendingRefreshFrameCount = 0;
        Apply(force: true);
    }

    private void OnDisable()
    {
        if (activeController == this)
        {
            activeController = null;
            hasContentPixelRect = false;
        }

        ApplyCameraViewport(new Rect(0f, 0f, 1f, 1f));
    }

    private void LateUpdate()
    {
        bool force = pendingForcedApplies > 0;
        if (pendingForcedApplies > 0)
        {
            pendingForcedApplies--;
        }

        Apply(force);
    }

    private void Apply(bool force)
    {
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        FullScreenMode fullScreenMode = Screen.fullScreenMode;
        Vector2Int displaySize = GetCurrentDisplaySize();
        int cameraSignature = GetCameraSignature();

        if (!force &&
            screenWidth == lastScreenWidth &&
            screenHeight == lastScreenHeight &&
            displaySize.x == lastDisplayWidth &&
            displaySize.y == lastDisplayHeight &&
            fullScreenMode == lastFullScreenMode &&
            cameraSignature == lastCameraSignature)
        {
            return;
        }

        lastScreenWidth = screenWidth;
        lastScreenHeight = screenHeight;
        lastDisplayWidth = displaySize.x;
        lastDisplayHeight = displaySize.y;
        lastFullScreenMode = fullScreenMode;
        lastCameraSignature = cameraSignature;

        if (screenWidth <= 0 || screenHeight <= 0)
        {
            contentPixelRect = Rect.zero;
            hasContentPixelRect = false;
            return;
        }

        contentPixelRect = CalculateContentPixelRect(screenWidth, screenHeight);
        Rect normalizedContentRect = PixelRectToNormalized(contentPixelRect, screenWidth, screenHeight);
        hasContentPixelRect = true;

        ApplyCameraViewport(normalizedContentRect);
        ApplyCanvases(normalizedContentRect);
        Canvas.ForceUpdateCanvases();
    }

    private static Vector2Int GetCurrentDisplaySize()
    {
        Resolution currentResolution = Screen.currentResolution;
        if (currentResolution.width > 0 && currentResolution.height > 0)
        {
            return new Vector2Int(currentResolution.width, currentResolution.height);
        }

        if (Display.main != null && Display.main.systemWidth > 0 && Display.main.systemHeight > 0)
        {
            return new Vector2Int(Display.main.systemWidth, Display.main.systemHeight);
        }

        return new Vector2Int(Screen.width, Screen.height);
    }

    private static int GetCameraSignature()
    {
        Camera[] cameras = Camera.allCameras;
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidate = cameras[i];
                if (candidate == null || candidate.targetTexture != null)
                {
                    continue;
                }

                hash = hash * 31 + candidate.GetInstanceID();
            }

            return hash;
        }
    }

    private static void ApplyCameraViewport(Rect normalizedContentRect)
    {
        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera targetCamera = cameras[i];
            if (targetCamera == null || targetCamera.targetTexture != null)
            {
                continue;
            }

            targetCamera.rect = normalizedContentRect;
        }
    }

    private static Rect CalculateContentPixelRect(int screenWidth, int screenHeight)
    {
        float screenAspect = (float)screenWidth / screenHeight;
        if (Mathf.Abs(screenAspect - TargetAspect) <= AspectRatioTolerance)
        {
            return new Rect(0f, 0f, screenWidth, screenHeight);
        }

        if (screenAspect > TargetAspect)
        {
            int contentWidth = Mathf.RoundToInt(screenHeight * TargetAspect);
            if (screenWidth - contentWidth <= PixelTolerance)
            {
                return new Rect(0f, 0f, screenWidth, screenHeight);
            }

            float x = (screenWidth - contentWidth) * 0.5f;
            return new Rect(x, 0f, contentWidth, screenHeight);
        }

        int contentHeight = Mathf.RoundToInt(screenWidth / TargetAspect);
        if (screenHeight - contentHeight <= PixelTolerance)
        {
            return new Rect(0f, 0f, screenWidth, screenHeight);
        }

        float y = (screenHeight - contentHeight) * 0.5f;
        return new Rect(0f, y, screenWidth, contentHeight);
    }

    private static Rect PixelRectToNormalized(Rect pixelRect, float screenWidth, float screenHeight)
    {
        return new Rect(
            pixelRect.x / screenWidth,
            pixelRect.y / screenHeight,
            pixelRect.width / screenWidth,
            pixelRect.height / screenHeight);
    }

    private void ApplyCanvases(Rect normalizedContentRect)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (!canvases[i].isRootCanvas)
            {
                continue;
            }

            RectTransform canvasRect = canvases[i].GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                continue;
            }

            ApplyCanvasScaler(canvases[i]);
            ApplyCanvasRoots(canvasRect, normalizedContentRect);
        }
    }

    private static void ApplyCanvasScaler(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            return;
        }

        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0f;
    }

    private void ApplyCanvasRoots(RectTransform canvasRect, Rect normalizedContentRect)
    {
        RectTransform safeAreaRoot = GetOrCreateRectChild(canvasRect, SafeAreaRootName);
        RectTransform contentRoot = GetOrCreateRectChild(safeAreaRoot, ContentRootName);
        RectTransform barsRoot = GetOrCreateRectChild(canvasRect, LetterboxBarsName);

        MoveExistingUiRoots(canvasRect, safeAreaRoot, contentRoot, barsRoot);

        StretchToAnchors(safeAreaRoot, normalizedContentRect.min, normalizedContentRect.max);

        Vector2 canvasSize = canvasRect.rect.size;
        float safeWidth = canvasSize.x * normalizedContentRect.width;
        float safeHeight = canvasSize.y * normalizedContentRect.height;
        float contentScale = safeWidth > 0f && safeHeight > 0f
            ? Mathf.Min(safeWidth / LogicalContentWidth, safeHeight / LogicalContentHeight)
            : 1f;

        contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(LogicalContentWidth, LogicalContentHeight);
        contentRoot.localScale = new Vector3(contentScale, contentScale, 1f);

        StretchToAnchors(barsRoot, Vector2.zero, Vector2.one);
        barsRoot.SetAsLastSibling();
        ApplyBars(barsRoot, normalizedContentRect);
    }

    private void MoveExistingUiRoots(RectTransform canvasRect, RectTransform safeAreaRoot, RectTransform contentRoot, RectTransform barsRoot)
    {
        childrenToMove.Clear();

        for (int i = 0; i < canvasRect.childCount; i++)
        {
            Transform child = canvasRect.GetChild(i);
            if (child == safeAreaRoot || child == barsRoot)
            {
                continue;
            }

            childrenToMove.Add(child);
        }

        for (int i = 0; i < childrenToMove.Count; i++)
        {
            childrenToMove[i].SetParent(contentRoot, false);
        }
    }

    private void ApplyBars(RectTransform barsRoot, Rect normalizedContentRect)
    {
        ApplyBar(GetOrCreateBar(barsRoot, "LeftBar"), new Vector2(0f, 0f), new Vector2(normalizedContentRect.xMin, 1f));
        ApplyBar(GetOrCreateBar(barsRoot, "RightBar"), new Vector2(normalizedContentRect.xMax, 0f), new Vector2(1f, 1f));
        ApplyBar(GetOrCreateBar(barsRoot, "BottomBar"), new Vector2(normalizedContentRect.xMin, 0f), new Vector2(normalizedContentRect.xMax, normalizedContentRect.yMin));
        ApplyBar(GetOrCreateBar(barsRoot, "TopBar"), new Vector2(normalizedContentRect.xMin, normalizedContentRect.yMax), new Vector2(normalizedContentRect.xMax, 1f));
    }

    private static RectTransform GetOrCreateBar(RectTransform parent, string name)
    {
        RectTransform bar = GetOrCreateRectChild(parent, name);
        Image image = bar.GetComponent<Image>();
        if (image == null)
        {
            image = bar.gameObject.AddComponent<Image>();
        }

        image.color = Color.black;
        image.raycastTarget = false;
        return bar;
    }

    private static void ApplyBar(RectTransform bar, Vector2 anchorMin, Vector2 anchorMax)
    {
        bool hasArea = anchorMax.x - anchorMin.x > 0.0001f && anchorMax.y - anchorMin.y > 0.0001f;
        bar.gameObject.SetActive(hasArea);
        StretchToAnchors(bar, anchorMin, anchorMax);
    }

    private static RectTransform GetOrCreateRectChild(RectTransform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            RectTransform existingRect = existing.GetComponent<RectTransform>();
            if (existingRect != null)
            {
                return existingRect;
            }
        }

        GameObject childObject = new GameObject(name, typeof(RectTransform));
        RectTransform childRect = childObject.GetComponent<RectTransform>();
        childRect.SetParent(parent, false);
        return childRect;
    }

    private static void StretchToAnchors(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }
}
