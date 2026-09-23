#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public static class BusinessCardSceneBuilder
{
    private const string NameOnCard = "Fawziyyah Oke";
    private const string TitleOnCard = "Product Manager";
    private const string MarkerPath = "Assets/Images/MarkerReference.jpg";

    private static readonly string[] LinkNames = { "GitHub", "LinkedIn", "TikTok", "YouTube" };
    private static readonly string[] IconPaths =
    {
        "Assets/Images/github-logo-removebg-preview.png",
        "Assets/Images/InBug-Black-removebg-preview.png",
        "Assets/Images/tiktok-logo-tiktok-logo-transparent-tiktok-icon-transparent-free-free-png-removebg-preview.png",
        "Assets/Images/yt_icon_almostblack_digital-removebg-preview.png",
    };
    private static readonly string[] LinkUrls =
    {
        "https://github.com/ZeeyahOke",
        "https://www.linkedin.com/in/fawziyyah-oke-8720b0318",
        "https://www.tiktok.com/@zeeyah.xoxo",
        "https://www.youtube.com/@FawziyyahOke",
    };
    // (x, y) offsets from card center for each icon, scattered around the left of the marker
    private static readonly Vector2[] IconOffsets =
    {
        new Vector2(-480, -220), // GitHub - bottom
        new Vector2(-640, -70),  // LinkedIn - lower left
        new Vector2(-640, 90),   // TikTok - upper left
        new Vector2(-480, 240),  // YouTube - top
    };
    private static readonly Color32 AccentRed = new Color32(0xE7, 0x4C, 0x3C, 255);
    private static readonly Color32 AccentTeal = new Color32(0x1A, 0xBC, 0x9C, 255);

    [MenuItem("AR Business Card/2. Export Layout Screenshot")]
    public static void ExportLayoutScreenshot()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            EditorUtility.DisplayDialog("AR Business Card", "No Main Camera found. Build the static layout first.", "OK");
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        RenderMode prevMode = RenderMode.ScreenSpaceOverlay;
        Camera prevWorldCamera = null;
        bool switchedMode = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay;
        if (switchedMode)
        {
            prevMode = canvas.renderMode;
            prevWorldCamera = canvas.worldCamera;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
        }

        int w = 1920, h = 1080;
        var rt = new RenderTexture(w, h, 24);
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;
        var prevAspect = cam.aspect;
        cam.targetTexture = rt;
        cam.aspect = (float)w / h;
        RenderTexture.active = rt;
        cam.Render();

        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        cam.aspect = prevAspect;
        cam.ResetAspect();
        RenderTexture.active = prevActive;
        rt.Release();
        Object.DestroyImmediate(rt);

        if (switchedMode)
        {
            canvas.renderMode = prevMode;
            canvas.worldCamera = prevWorldCamera;
        }

        var dir = Path.Combine(Application.dataPath, "..", "Docs");
        Directory.CreateDirectory(dir);
        var outPath = Path.Combine(dir, "0-layout-screenshot.png");
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        Debug.Log("[BusinessCardSceneBuilder] Layout screenshot saved to " + outPath);
        EditorUtility.DisplayDialog("AR Business Card", "Screenshot saved to Docs/0-layout-screenshot.png", "OK");
    }

    [MenuItem("AR Business Card/1. Build Static Layout")]
    public static void BuildStaticLayout()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        TryCreateEventSystem();

        var camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        var cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x39, 0x39, 0x39, 255);
        cam.orthographic = true;

        var canvasGO = new GameObject("BusinessCardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Root object that will later be re-parented under the Vuforia ImageTarget (task 2)
        var layoutGO = CreateUIObject("BusinessCardLayout", canvasGO.transform);
        var layoutRT = layoutGO.GetComponent<RectTransform>();
        layoutRT.anchorMin = new Vector2(0.5f, 0.5f);
        layoutRT.anchorMax = new Vector2(0.5f, 0.5f);
        layoutRT.sizeDelta = new Vector2(1920, 1080);
        layoutRT.anchoredPosition = Vector2.zero;

        // Marker placeholder (center) - shows where the printed AR marker sits
        var markerGO = CreateUIObject("MarkerPlaceholder", layoutGO.transform);
        var markerImg = markerGO.AddComponent<Image>();
        markerImg.sprite = LoadAsSprite(MarkerPath);
        markerImg.preserveAspect = true;
        var markerRT = markerGO.GetComponent<RectTransform>();
        markerRT.anchorMin = markerRT.anchorMax = new Vector2(0.5f, 0.5f);
        markerRT.sizeDelta = new Vector2(420, 420);
        markerRT.anchoredPosition = Vector2.zero;

        var markerBorderGO = CreateUIObject("MarkerBorder", markerGO.transform);
        var borderImg = markerBorderGO.AddComponent<Image>();
        borderImg.sprite = null;
        borderImg.color = new Color(1, 1, 1, 0); // invisible spacer, kept for future ring/pulse animation
        var borderRT = markerBorderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-16, -16);
        borderRT.offsetMax = new Vector2(16, 16);

        // Name (right of marker)
        var nameGO = CreateUIObject("NameText", layoutGO.transform);
        var nameText = nameGO.AddComponent<Text>();
        nameText.text = NameOnCard;
        nameText.font = GetDefaultFont();
        nameText.fontSize = 64;
        nameText.fontStyle = FontStyle.Bold;
        nameText.color = AccentRed;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = nameRT.anchorMax = new Vector2(0.5f, 0.5f);
        nameRT.pivot = new Vector2(0, 0.5f);
        nameRT.sizeDelta = new Vector2(700, 90);
        nameRT.anchoredPosition = new Vector2(300, 90);

        // Title (below name)
        var titleGO = CreateUIObject("TitleText", layoutGO.transform);
        var titleText = titleGO.AddComponent<Text>();
        titleText.text = TitleOnCard;
        titleText.font = GetDefaultFont();
        titleText.fontSize = 40;
        titleText.fontStyle = FontStyle.Italic;
        titleText.color = AccentTeal;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = titleRT.anchorMax = new Vector2(0.5f, 0.5f);
        titleRT.pivot = new Vector2(0, 0.5f);
        titleRT.sizeDelta = new Vector2(700, 60);
        titleRT.anchoredPosition = new Vector2(300, 0);

        // Four circular icon links scattered to the left of the marker
        for (int i = 0; i < LinkNames.Length; i++)
        {
            var btnGO = CreateUIObject(LinkNames[i] + "Button", layoutGO.transform);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = btnRT.anchorMax = new Vector2(0.5f, 0.5f);
            btnRT.sizeDelta = new Vector2(130, 130);
            btnRT.anchoredPosition = IconOffsets[i];

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = CircleSprite;
            btnImg.color = AccentRed;
            btnImg.type = Image.Type.Simple;

            var btn = btnGO.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            colors.pressedColor = AccentTeal;
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;
            btn.targetGraphic = btnImg;

            var link = btnGO.AddComponent<SocialLinkButton>();
            link.url = LinkUrls[i];

            var iconGO = CreateUIObject("Icon", btnGO.transform);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = LoadAsSprite(IconPaths[i]);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(28, 28);
            iconRT.offsetMax = new Vector2(-28, -28);
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ARBusinessCard.unity");
        AssetDatabase.Refresh();
        Debug.Log("[BusinessCardSceneBuilder] Static layout built and saved to Assets/Scenes/ARBusinessCard.unity");
        EditorUtility.DisplayDialog("AR Business Card", "Static layout built and saved to Assets/Scenes/ARBusinessCard.unity", "OK");
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void TryCreateEventSystem()
    {
        try
        {
            var esType = FindType("UnityEngine.EventSystems.EventSystem");
            var inputType = FindType("UnityEngine.EventSystems.StandaloneInputModule")
                ?? FindType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (esType == null)
            {
                Debug.LogWarning("[BusinessCardSceneBuilder] EventSystem type not found; skipping.");
                return;
            }
            var go = new GameObject("EventSystem", esType);
            if (inputType != null) go.AddComponent(inputType);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[BusinessCardSceneBuilder] Could not create EventSystem: " + e.Message);
        }
    }

    private static System.Type FindType(string fullName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }

    private static Font GetDefaultFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    private static Sprite LoadAsSprite(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static Sprite _circleSprite;
    private static Sprite CircleSprite
    {
        get
        {
            if (_circleSprite != null) return _circleSprite;
            const string dir = "Assets/Generated";
            const string path = dir + "/CircleSprite.png";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", "Generated");

            if (File.Exists(path))
            {
                _circleSprite = LoadAsSprite(path);
                if (_circleSprite != null) return _circleSprite;
            }

            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((radius - dist) + 1f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.Refresh();
            _circleSprite = LoadAsSprite(path);
            return _circleSprite;
        }
    }
}
#endif
