#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
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

    [MenuItem("AR Business Card/3. Import Vuforia Package")]
    public static void ImportVuforiaPackage()
    {
        const string pkgPath = "/Users/fawzo1/Downloads/add-vuforia-package-11-4-4.unitypackage";
        if (!File.Exists(pkgPath))
        {
            EditorUtility.DisplayDialog("AR Business Card", "Could not find:\n" + pkgPath, "OK");
            return;
        }
        Debug.Log("[BusinessCardSceneBuilder] Importing Vuforia package (this can take a minute)...");
        AssetDatabase.ImportPackage(pkgPath, false);
    }

    [MenuItem("AR Business Card/4. Import Target Database")]
    public static void ImportTargetDatabase()
    {
        const string pkgPath = "/Users/fawzo1/Downloads/ARBussinessCard.unitypackage";
        if (!File.Exists(pkgPath))
        {
            EditorUtility.DisplayDialog("AR Business Card", "Could not find:\n" + pkgPath, "OK");
            return;
        }
        Debug.Log("[BusinessCardSceneBuilder] Importing target database...");
        AssetDatabase.ImportPackage(pkgPath, false);
    }

    [MenuItem("AR Business Card/5. Dump ImageTarget Fields (debug)")]
    public static void DumpImageTargetFields()
    {
        var go = GameObject.Find("ImageTarget");
        if (go == null) { Debug.LogError("ImageTarget not found"); return; }
        var comps = go.GetComponents<Component>();
        foreach (var c in comps)
        {
            if (c == null) continue;
            Debug.Log("--- Component: " + c.GetType().FullName + " ---");
            var so = new SerializedObject(c);
            var prop = so.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                string val;
                try { val = prop.propertyType == SerializedPropertyType.String ? prop.stringValue : prop.propertyType.ToString(); }
                catch { val = "?"; }
                Debug.Log(prop.propertyPath + " : " + prop.propertyType + " = " + val);
            }
        }
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
    [MenuItem("AR Business Card/6. Wire AR Anchoring (Task 1)")]
    private static void WireArAnchoring()
    {
        var imageTarget = GameObject.Find("ImageTarget");
        if (imageTarget == null)
        {
            Debug.LogError("WireArAnchoring: could not find 'ImageTarget' GameObject. Make sure the Image Target was created via GameObject > Vuforia Engine > Image Target.");
            return;
        }

        var arCameraGO = GameObject.Find("ARCamera");
        if (arCameraGO == null)
        {
            Debug.LogError("WireArAnchoring: could not find 'ARCamera' GameObject.");
            return;
        }
        var arCamera = arCameraGO.GetComponent<Camera>();

        // Remove the redundant plain Main Camera now that ARCamera handles rendering.
        var oldMainCamera = GameObject.Find("Main Camera");
        if (oldMainCamera != null)
        {
            Object.DestroyImmediate(oldMainCamera);
            Debug.Log("WireArAnchoring: removed redundant 'Main Camera' GameObject.");
        }

        var canvasGO = GameObject.Find("BusinessCardCanvas");
        if (canvasGO == null)
        {
            Debug.LogError("WireArAnchoring: could not find 'BusinessCardCanvas' GameObject.");
            return;
        }
        var canvas = canvasGO.GetComponent<Canvas>();

        // Reparent the whole card layout under the ImageTarget so it inherits the
        // marker's tracked pose (position/rotation/scale) and disappears when the
        // marker is lost.
        canvasGO.transform.SetParent(imageTarget.transform, false);

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = arCamera;

        // The ImageTarget's local plane is the XZ plane (Y points away from the
        // printed marker, towards the camera). Rotate the canvas so its content
        // lies flat on that plane instead of standing upright, and nudge it
        // slightly above the surface to avoid z-fighting with the marker preview.
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rt.localPosition = new Vector3(0f, 0.02f, 0f);
        const float scale = 0.006f; // 1920px-wide canvas -> ~11.5 units wide in scene
        rt.localScale = new Vector3(scale, scale, scale);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("WireArAnchoring: BusinessCardCanvas reparented under ImageTarget, converted to World Space, and scene saved.");
        EditorUtility.DisplayDialog("AR Business Card", "Card layout is now anchored to the ImageTarget (World Space canvas, reparented, Main Camera removed).", "OK");
    }

    [MenuItem("AR Business Card/7. Add Card Animations (Task 2)")]
    private static void AddCardAnimations()
    {
        var canvasGO = GameObject.Find("BusinessCardCanvas");
        if (canvasGO == null)
        {
            Debug.LogError("AddCardAnimations: BusinessCardCanvas not found.");
            return;
        }

        var imageTarget = GameObject.Find("ImageTarget");
        if (imageTarget == null)
        {
            Debug.LogError("AddCardAnimations: ImageTarget not found.");
            return;
        }

        var handler = imageTarget.GetComponent<DefaultObserverEventHandler>();
        if (handler == null)
        {
            Debug.LogError("AddCardAnimations: DefaultObserverEventHandler not found on ImageTarget.");
            return;
        }

        const string genDir = "Assets/Generated";
        if (!AssetDatabase.IsValidFolder(genDir))
            AssetDatabase.CreateFolder("Assets", "Generated");

        // Ensure a CanvasGroup exists so we can fade the whole card in.
        var canvasGroup = canvasGO.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        // ---- Entrance clip: fade the card in, pop each button in with a staggered overshoot ----
        var entranceClip = new AnimationClip { legacy = true, wrapMode = WrapMode.Once, name = "CardEntrance" };

        var fadeCurve = new AnimationCurve();
        fadeCurve.AddKey(0f, 0f);
        fadeCurve.AddKey(0.5f, 1f);
        AnimationUtility.SetEditorCurve(entranceClip, EditorCurveBinding.FloatCurve("", typeof(CanvasGroup), "m_Alpha"), fadeCurve);

        float delay = 0f;
        const float perButtonDelay = 0.12f;
        const float popDuration = 0.35f;
        foreach (var buttonName in LinkNames)
        {
            var buttonTransform = canvasGO.transform.Find("BusinessCardLayout/" + buttonName + "Button");
            if (buttonTransform == null) continue;
            string path = AnimationUtility.CalculateTransformPath(buttonTransform, canvasGO.transform);

            foreach (var axis in new[] { "x", "y", "z" })
            {
                var curve = new AnimationCurve();
                curve.AddKey(0f, 0f);
                curve.AddKey(delay, 0f);
                curve.AddKey(delay + popDuration, 1.15f);
                curve.AddKey(delay + popDuration + 0.12f, 1f);
                for (int i = 0; i < curve.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                }
                AnimationUtility.SetEditorCurve(entranceClip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale." + axis), curve);
            }
            delay += perButtonDelay;
        }

        string entranceClipPath = genDir + "/CardEntrance.anim";
        var existingEntrance = AssetDatabase.LoadAssetAtPath<AnimationClip>(entranceClipPath);
        if (existingEntrance != null) AssetDatabase.DeleteAsset(entranceClipPath);
        AssetDatabase.CreateAsset(entranceClip, entranceClipPath);

        var anim = canvasGO.GetComponent<Animation>();
        if (anim == null) anim = canvasGO.AddComponent<Animation>();
        anim.AddClip(entranceClip, entranceClip.name);
        anim.clip = entranceClip;
        anim.playAutomatically = false;
        anim.wrapMode = WrapMode.Once;

        var trigger = canvasGO.GetComponent<CardAnimationTrigger>();
        if (trigger == null) trigger = canvasGO.AddComponent<CardAnimationTrigger>();

        bool alreadyWired = false;
        int listenerCount = handler.OnTargetFound.GetPersistentEventCount();
        for (int i = 0; i < listenerCount; i++)
        {
            if (handler.OnTargetFound.GetPersistentTarget(i) == trigger &&
                handler.OnTargetFound.GetPersistentMethodName(i) == "PlayEntrance")
            {
                alreadyWired = true;
                break;
            }
        }
        if (!alreadyWired)
        {
            UnityEventTools.AddPersistentListener(handler.OnTargetFound, trigger.PlayEntrance);
        }

        // ---- Idle clip: gentle continuous wobble on each button ----
        var idleClip = new AnimationClip { legacy = true, wrapMode = WrapMode.Loop, name = "IconIdleWobble" };
        float[] angles = { 0f, 6f, 0f, -6f, 0f };
        float[] times = { 0f, 0.6f, 1.2f, 1.8f, 2.4f };
        var curveX = new AnimationCurve();
        var curveY = new AnimationCurve();
        var curveZ = new AnimationCurve();
        var curveW = new AnimationCurve();
        for (int i = 0; i < angles.Length; i++)
        {
            var q = Quaternion.Euler(0f, 0f, angles[i]);
            curveX.AddKey(times[i], q.x);
            curveY.AddKey(times[i], q.y);
            curveZ.AddKey(times[i], q.z);
            curveW.AddKey(times[i], q.w);
        }
        AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.x"), curveX);
        AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.y"), curveY);
        AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.z"), curveZ);
        AnimationUtility.SetEditorCurve(idleClip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.w"), curveW);

        string idleClipPath = genDir + "/IconIdleWobble.anim";
        var existingIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>(idleClipPath);
        if (existingIdle != null) AssetDatabase.DeleteAsset(idleClipPath);
        AssetDatabase.CreateAsset(idleClip, idleClipPath);

        foreach (var buttonName in LinkNames)
        {
            var buttonTransform = canvasGO.transform.Find("BusinessCardLayout/" + buttonName + "Button");
            if (buttonTransform == null) continue;
            var buttonAnim = buttonTransform.GetComponent<Animation>();
            if (buttonAnim == null) buttonAnim = buttonTransform.gameObject.AddComponent<Animation>();
            buttonAnim.AddClip(idleClip, idleClip.name);
            buttonAnim.clip = idleClip;
            buttonAnim.playAutomatically = true;
            buttonAnim.wrapMode = WrapMode.Loop;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("AddCardAnimations: entrance (fade + staggered button pop-in on target found) and idle wobble animations wired up; scene saved.");
        EditorUtility.DisplayDialog("AR Business Card", "Card animations added:\n- Fade + staggered button pop-in when the marker is found\n- Gentle idle wobble on each icon button", "OK");
    }

    [MenuItem("AR Business Card/8. Wire Interactive Links (Task 3)")]
    private static void WireInteractiveLinks()
    {
        var canvasGO = GameObject.Find("BusinessCardCanvas");
        if (canvasGO == null)
        {
            Debug.LogError("WireInteractiveLinks: BusinessCardCanvas not found.");
            return;
        }

        const string genDir = "Assets/Generated";
        if (!AssetDatabase.IsValidFolder(genDir))
            AssetDatabase.CreateFolder("Assets", "Generated");

        const string clipPath = genDir + "/ButtonClick.wav";
        if (!File.Exists(clipPath))
        {
            WriteClickSoundWav(clipPath);
            AssetDatabase.Refresh();
        }
        var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        if (clickClip == null)
        {
            Debug.LogError("WireInteractiveLinks: failed to import generated click sound at " + clipPath);
        }

        int wiredCount = 0;
        foreach (var buttonName in LinkNames)
        {
            var buttonTransform = canvasGO.transform.Find("BusinessCardLayout/" + buttonName + "Button");
            if (buttonTransform == null)
            {
                Debug.LogWarning("WireInteractiveLinks: could not find " + buttonName + "Button");
                continue;
            }
            var buttonGO = buttonTransform.gameObject;

            var audioSource = buttonGO.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = buttonGO.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            var link = buttonGO.GetComponent<SocialLinkButton>();
            if (link == null) link = buttonGO.AddComponent<SocialLinkButton>();
            if (clickClip != null) link.clickSound = clickClip;

            var button = buttonGO.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning("WireInteractiveLinks: no Button component on " + buttonGO.name);
                continue;
            }

            bool alreadyWired = false;
            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (button.onClick.GetPersistentTarget(i) == link &&
                    button.onClick.GetPersistentMethodName(i) == "OpenLink")
                {
                    alreadyWired = true;
                    break;
                }
            }
            if (!alreadyWired)
            {
                UnityEventTools.AddPersistentListener(button.onClick, link.OpenLink);
            }
            wiredCount++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"WireInteractiveLinks: wired click -> OpenURL + audio feedback on {wiredCount} buttons; scene saved.");
        EditorUtility.DisplayDialog("AR Business Card", $"Wired {wiredCount} link buttons:\n- OnClick opens their URL\n- Press color-tint feedback (Button transition)\n- Click sound feedback (procedural)", "OK");
    }

    private static void WriteClickSoundWav(string path)
    {
        const int sampleRate = 44100;
        const float duration = 0.12f;
        int sampleCount = (int)(sampleRate * duration);
        var samples = new float[sampleCount];
        const float freq = 1200f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 30f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.6f;
        }

        using (var stream = new FileStream(path, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            int byteRate = sampleRate * 2;
            int dataSize = samples.Length * 2;
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataSize);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataSize);
            foreach (var s in samples)
            {
                short val = (short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue);
                writer.Write(val);
            }
        }
    }

    [MenuItem("AR Business Card/9. Fix Marker Tracking (Low-Feature Optimization)")]
    private static void FixMarkerTracking()
    {
        var imageTargetGO = GameObject.Find("ImageTarget");
        if (imageTargetGO == null)
        {
            Debug.LogError("FixMarkerTracking: could not find 'ImageTarget' GameObject in the scene.");
            return;
        }

        var behaviour = imageTargetGO.GetComponent<Vuforia.ImageTargetBehaviour>();
        if (behaviour == null)
        {
            Debug.LogError("FixMarkerTracking: 'ImageTarget' has no ImageTargetBehaviour component.");
            return;
        }

        // GetTrackingOptimization()/SetTrackingOptimization() are runtime APIs that need a live
        // native Observer (Play Mode), so calling them in Edit Mode throws a NullReferenceException.
        // Instead, write the serialized field directly, exactly like the Inspector dropdown would.
        var so = new SerializedObject(behaviour);
        var prop = so.FindProperty("mTrackingOptimization");
        if (prop == null)
        {
            Debug.LogError("FixMarkerTracking: could not find serialized property mTrackingOptimization.");
            return;
        }
        var before = (Vuforia.TrackingOptimization)prop.intValue;
        prop.intValue = (int)Vuforia.TrackingOptimization.LOW_FEATURE_OBJECTS;

        var upgradeProp = so.FindProperty("mTrackingOptimizationNeedsUpgrade");
        if (upgradeProp != null) upgradeProp.boolValue = false;

        so.ApplyModifiedProperties();
        var after = (Vuforia.TrackingOptimization)prop.intValue;

        EditorUtility.SetDirty(behaviour);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log($"FixMarkerTracking: TrackingOptimization changed from {before} to {after}. " +
                  "This tells Vuforia's tracker to expect a target with large smooth/flat color " +
                  "regions (like our seahorse logo) instead of a densely-textured one, which " +
                  "should make initial detection and lock-on much more reliable.");
        EditorUtility.DisplayDialog(
            "AR Business Card",
            "Marker tracking optimization set to LOW_FEATURE_OBJECTS.\n\n" +
            "This tunes Vuforia's tracker for the seahorse logo's large flat-color areas, " +
            "which is very likely why detection was unreliable before. Rebuild the app " +
            "(Android and/or iOS) to test the fix on-device.",
            "OK");
    }

}
#endif
