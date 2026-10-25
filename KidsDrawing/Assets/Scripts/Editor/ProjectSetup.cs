using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace KidsDrawing.Editor
{
    // Player Settings とメインシーンを仕様どおりに整える。
    // メニュー、または -executeMethod KidsDrawing.Editor.ProjectSetup.Run で実行する。
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string BundleId = "com.osa.kidsdrawing";

        [MenuItem("KidsDrawing/Apply Project Setup")]
        public static void Run()
        {
            ApplyPlayerSettings();
            CreateMainScene();
            AssetDatabase.SaveAssets();
            Debug.Log("KidsDrawing: project setup applied");
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "osa";
            PlayerSettings.productName = "おえかき";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);

            // 指定した色の値をそのまま描画・保存するためガンマ空間にする
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.SplashScreen.show = false;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.statusBarHidden = true;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });

            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPadOnly;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.hideHomeButton = true;
            // 画面端からのスワイプで描画が奪われないようにする
            PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All;
            // Team ID は公開リポジトリに含めず、scripts/build-ios.sh が xcodebuild に渡す
            PlayerSettings.iOS.appleDeveloperTeamID = "";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        }

        static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.allowMSAA = false;
            camera.allowHDR = false;

            var drawing = new GameObject("Drawing");
            var canvasData = drawing.AddComponent<CanvasData>();
            var brush = drawing.AddComponent<BrushController>();
            var painter = drawing.AddComponent<CanvasPainter>();
            var input = drawing.AddComponent<InputController>();
            var ui = drawing.AddComponent<DrawingUI>();
            var persistence = drawing.AddComponent<DrawingPersistence>();
            var archiver = drawing.AddComponent<ArtworkArchiver>();
            var suckIn = drawing.AddComponent<SuckInEffect>();

            var painterSettings = new SerializedObject(painter);
            painterSettings.FindProperty("canvasData").objectReferenceValue = canvasData;
            painterSettings.FindProperty("displayCamera").objectReferenceValue = camera;
            painterSettings.FindProperty("brushShader").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/BrushCapsule.shader");
            painterSettings.FindProperty("displayShader").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/CanvasDisplay.shader");
            painterSettings.ApplyModifiedPropertiesWithoutUndo();

            var inputSettings = new SerializedObject(input);
            inputSettings.FindProperty("canvasData").objectReferenceValue = canvasData;
            inputSettings.FindProperty("painter").objectReferenceValue = painter;
            inputSettings.FindProperty("brush").objectReferenceValue = brush;
            inputSettings.ApplyModifiedPropertiesWithoutUndo();

            var uiSettings = new SerializedObject(ui);
            uiSettings.FindProperty("canvasData").objectReferenceValue = canvasData;
            uiSettings.FindProperty("brush").objectReferenceValue = brush;
            uiSettings.FindProperty("archiver").objectReferenceValue = archiver;
            uiSettings.FindProperty("suckInEffect").objectReferenceValue = suckIn;
            uiSettings.FindProperty("shapeShader").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/UIShape.shader");
            uiSettings.ApplyModifiedPropertiesWithoutUndo();

            var persistenceSettings = new SerializedObject(persistence);
            persistenceSettings.FindProperty("canvasData").objectReferenceValue = canvasData;
            persistenceSettings.FindProperty("premultiplyShader").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/Premultiply.shader");
            persistenceSettings.ApplyModifiedPropertiesWithoutUndo();

            var archiverSettings = new SerializedObject(archiver);
            archiverSettings.FindProperty("canvasData").objectReferenceValue = canvasData;
            archiverSettings.ApplyModifiedPropertiesWithoutUndo();

            var suckInSettings = new SerializedObject(suckIn);
            suckInSettings.FindProperty("canvasData").objectReferenceValue = canvasData;
            suckInSettings.FindProperty("shader").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/SuckIn.shader");
            suckInSettings.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
