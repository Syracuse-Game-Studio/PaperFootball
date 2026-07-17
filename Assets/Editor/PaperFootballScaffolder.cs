using System.Collections.Generic;
using System.Linq;
using PaperFootball.Ball;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Match;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using PaperFootball.Tabletop.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaperFootball.Editor
{
    public static class PaperFootballScaffolder
    {
        private const string ScenePath = "Assets/Scenes/PaperFootballGame.unity";
        private const string LauncherScenePath = "Assets/Scenes/PaperFootballLauncher.unity";
        private const string GeneratedFolder = "Assets/Materials/PaperFootballPrototype";
        private const string ConfigPath = "Assets/Materials/PaperFootballPrototype/DefaultPaperFootballConfig.asset";

        private static readonly Vector3 TableScale = new(8f, 0.24f, 12f);
        private const float TableTopY = 0.12f;
        private const float FootballCenterY = TableTopY + 0.09f;
        private const float GoalHalfWidth = 1.1f;
        private const float CrossbarWorldY = TableTopY + 0.72f;

        [MenuItem("Paper Football/Build Prototype Scene")]
        public static void BuildOrRepairScene()
        {
            EnsureFolders();

            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                ? EditorSceneManager.OpenScene(ScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            PaperFootballConfig config = GetOrCreateConfig();
            Material tableMaterial = GetOrCreateMaterial("Table.mat", new Color(0.42f, 0.26f, 0.13f));
            Material floorMaterial = GetOrCreateMaterial("Floor.mat", new Color(0.12f, 0.13f, 0.15f));
            Material footballMaterial = GetOrCreateMaterial("PaperFootball.mat", new Color(0.96f, 0.95f, 0.86f));
            Material edgeOneMaterial = GetOrCreateMaterial("PlayerOneEdge.mat", new Color(0.1f, 0.7f, 0.95f, 0.85f));
            Material edgeTwoMaterial = GetOrCreateMaterial("PlayerTwoEdge.mat", new Color(1f, 0.35f, 0.24f, 0.85f));
            Material indicatorMaterial = GetOrCreateMaterial("AimIndicator.mat", new Color(0.1f, 0.95f, 0.75f));
            PhysicsMaterial footballPhysicsMaterial = GetOrCreatePhysicsMaterial();

            GameObject root = GetOrCreateRoot("PaperFootballPrototype");
            GameObject table = GetOrCreatePrimitive("Table", PrimitiveType.Cube, root.transform);
            table.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            table.transform.localScale = TableScale;
            SetMaterial(table, tableMaterial);
            BoxCollider tableCollider = EnsureComponent<BoxCollider>(table);

            GameObject floor = GetOrCreatePrimitive("Floor", PrimitiveType.Cube, root.transform);
            floor.transform.SetPositionAndRotation(new Vector3(0f, -1.25f, 0f), Quaternion.identity);
            floor.transform.localScale = new Vector3(13f, 0.1f, 17f);
            SetMaterial(floor, floorMaterial);

            GameObject playerOneEdge = GetOrCreatePrimitive("PlayerOneScoringEdge", PrimitiveType.Cube, root.transform);
            ConfigureScoringEdge(playerOneEdge, new Vector3(0f, TableTopY + 0.025f, TableScale.z * 0.5f), edgeOneMaterial);

            GameObject playerTwoEdge = GetOrCreatePrimitive("PlayerTwoScoringEdge", PrimitiveType.Cube, root.transform);
            ConfigureScoringEdge(playerTwoEdge, new Vector3(0f, TableTopY + 0.025f, -TableScale.z * 0.5f), edgeTwoMaterial);

            GameObject playerOneStart = GetOrCreateChild("PlayerOneStart", root.transform);
            playerOneStart.transform.SetPositionAndRotation(new Vector3(0f, FootballCenterY, -config.Rules.kickoffOffsetFromCenter), Quaternion.Euler(90f, 0f, 0f));

            GameObject playerTwoStart = GetOrCreateChild("PlayerTwoStart", root.transform);
            playerTwoStart.transform.SetPositionAndRotation(new Vector3(0f, FootballCenterY, config.Rules.kickoffOffsetFromCenter), Quaternion.Euler(90f, 180f, 0f));

            GameObject playerOneFieldGoalSpot = GetOrCreateChild("PlayerOneFieldGoalSpot", root.transform);
            playerOneFieldGoalSpot.transform.SetPositionAndRotation(new Vector3(0f, FootballCenterY, 2.2f), Quaternion.Euler(90f, 0f, 0f));

            GameObject playerTwoFieldGoalSpot = GetOrCreateChild("PlayerTwoFieldGoalSpot", root.transform);
            playerTwoFieldGoalSpot.transform.SetPositionAndRotation(new Vector3(0f, FootballCenterY, -2.2f), Quaternion.Euler(90f, 180f, 0f));

            GameObject football = GetOrCreateChild("Paper Football", root.transform);
            football.transform.SetPositionAndRotation(playerOneStart.transform.position, playerOneStart.transform.rotation);
            football.transform.localScale = Vector3.one;
            EnsureComponent<MeshFilter>(football);
            MeshRenderer footballRenderer = EnsureComponent<MeshRenderer>(football);
            footballRenderer.sharedMaterial = footballMaterial;
            EnsureComponent<PaperFootballMesh>(football);
            BoxCollider footballCollider = EnsureComponent<BoxCollider>(football);
            footballCollider.size = new Vector3(0.46f, 0.62f, 0.16f);
            footballCollider.center = Vector3.zero;
            footballCollider.material = footballPhysicsMaterial;
            Rigidbody footballBody = EnsureComponent<Rigidbody>(football);
            footballBody.mass = 0.16f;
            footballBody.useGravity = true;
            footballBody.linearDamping = 1.15f;
            footballBody.angularDamping = 1.8f;
            footballBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            FootballPhysicsController physicsController = EnsureComponent<FootballPhysicsController>(football);
            FootballRestDetector restDetector = EnsureComponent<FootballRestDetector>(football);
            restDetector.Configure(config.Rules);

            GameObject goalposts = GetOrCreateChild("Goalposts", root.transform);
            ConfigureGoalpost("PlayerOneGoalpost", goalposts.transform, TableScale.z * 0.5f + 0.35f, edgeOneMaterial);
            ConfigureGoalpost("PlayerTwoGoalpost", goalposts.transform, -TableScale.z * 0.5f - 0.35f, edgeTwoMaterial);
            GoalPostTrigger playerOneGoalTrigger = ConfigureGoalTrigger("PlayerOneGoalTrigger", goalposts.transform, TableScale.z * 0.5f + 0.35f, PaperFootballPlayer.PlayerOne);
            GoalPostTrigger playerTwoGoalTrigger = ConfigureGoalTrigger("PlayerTwoGoalTrigger", goalposts.transform, -TableScale.z * 0.5f - 0.35f, PaperFootballPlayer.PlayerTwo);

            Camera camera = ConfigureCamera(root.transform);
            ConfigureLighting(root.transform);
            ConfigureEventSystem(root.transform);

            GameObject inputObject = GetOrCreateChild("FlickInputReader", root.transform);
            FlickInputReader inputReader = EnsureComponent<FlickInputReader>(inputObject);
            inputReader.Configure(camera, footballCollider, config.Rules, TableTopY + 0.05f);

            GameObject boundaryObject = GetOrCreateChild("TableBoundaryDetector", root.transform);
            TableBoundaryDetector boundaryDetector = EnsureComponent<TableBoundaryDetector>(boundaryObject);
            boundaryDetector.Configure(tableCollider, config.Rules);

            GameObject indicatorObject = GetOrCreateChild("FlickAimIndicator", root.transform);
            LineRenderer lineRenderer = EnsureComponent<LineRenderer>(indicatorObject);
            lineRenderer.sharedMaterial = indicatorMaterial;
            lineRenderer.positionCount = 2;
            FlickAimIndicator indicator = EnsureComponent<FlickAimIndicator>(indicatorObject);

            GameHudController hud = ConfigureHud(root.transform);

            GameObject fieldGoalObject = GetOrCreateChild("FieldGoalController", root.transform);
            FieldGoalController fieldGoalController = EnsureComponent<FieldGoalController>(fieldGoalObject);
            fieldGoalController.Configure(
                playerOneFieldGoalSpot.transform,
                playerTwoFieldGoalSpot.transform,
                playerOneGoalTrigger,
                playerTwoGoalTrigger,
                footballCollider);

            GameObject matchObject = GetOrCreateChild("MatchController", root.transform);
            MatchController matchController = EnsureComponent<MatchController>(matchObject);
            matchController.Configure(
                config,
                physicsController,
                restDetector,
                inputReader,
                boundaryDetector,
                hud,
                indicator,
                fieldGoalController,
                footballCollider,
                playerOneStart.transform,
                playerTwoStart.transform);

            MarkDirty(
                config,
                table,
                floor,
                playerOneEdge,
                playerTwoEdge,
                football,
                goalposts,
                inputObject,
                boundaryObject,
                indicatorObject,
                fieldGoalObject,
                hud.gameObject,
                matchObject);

            if (string.IsNullOrEmpty(scene.path))
            {
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else
            {
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Paper football prototype scene built: {ScenePath}");

            BuildOrRepairLauncherScene();
            ConfigureBuildSettings();
        }

        public static void BuildOrRepairSceneAndExit()
        {
            BuildOrRepairScene();
            EditorApplication.Exit(0);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "PaperFootballPrototype");
            }
        }

        private static PaperFootballConfig GetOrCreateConfig()
        {
            PaperFootballConfig config = AssetDatabase.LoadAssetAtPath<PaperFootballConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PaperFootballConfig>();
                config.Rules.touchdownPoints = 6;
                config.Rules.successfulKickPoints = 3;
                config.Rules.targetScore = 21;
                config.Rules.requiredOverhangPercent = 0f;
                config.Rules.minimumSupportedPercent = 0.25f;
                config.Rules.maximumFlickForce = 18f;
                config.Rules.minimumFlickForce = 1.5f;
                config.Rules.maximumDragDistance = 2.5f;
                config.Rules.footballStoppingThreshold = 0.08f;
                config.Rules.angularStoppingThreshold = 0.25f;
                config.Rules.requiredStillTime = 0.35f;
                config.Rules.fallHeight = -1.2f;
                config.Rules.kickoffOffsetFromCenter = 3.8f;
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.Rules.Sanitize();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static Material GetOrCreateMaterial(string fileName, Color color)
        {
            string path = $"{GeneratedFolder}/{fileName}";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static PhysicsMaterial GetOrCreatePhysicsMaterial()
        {
            string path = $"{GeneratedFolder}/PaperFootballPhysics.physicMaterial";
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material == null)
            {
                material = new PhysicsMaterial("PaperFootballPhysics");
                AssetDatabase.CreateAsset(material, path);
            }

            material.dynamicFriction = 0.55f;
            material.staticFriction = 0.65f;
            material.bounciness = 0.04f;
            material.frictionCombine = PhysicsMaterialCombine.Average;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureScoringEdge(GameObject edge, Vector3 position, Material material)
        {
            edge.transform.SetPositionAndRotation(position, Quaternion.identity);
            edge.transform.localScale = new Vector3(TableScale.x, 0.04f, 0.08f);
            SetMaterial(edge, material);

            if (edge.TryGetComponent(out Collider collider))
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void ConfigureGoalpost(string name, Transform parent, float z, Material material)
        {
            GameObject root = GetOrCreateChild(name, parent);
            ConfigurePostPart($"{name}_LeftUpright", root.transform, new Vector3(-1.1f, TableTopY + 1.1f, z), Quaternion.identity, new Vector3(0.035f, 1.9f, 0.035f), material);
            ConfigurePostPart($"{name}_RightUpright", root.transform, new Vector3(1.1f, TableTopY + 1.1f, z), Quaternion.identity, new Vector3(0.035f, 1.9f, 0.035f), material);
            ConfigurePostPart($"{name}_Crossbar", root.transform, new Vector3(0f, CrossbarWorldY, z), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.035f, 1.1f, 0.035f), material);
        }

        private static GoalPostTrigger ConfigureGoalTrigger(string name, Transform parent, float z, PaperFootballPlayer scoringPlayer)
        {
            GameObject triggerObject = GetOrCreateChild(name, parent);
            triggerObject.transform.SetPositionAndRotation(new Vector3(0f, CrossbarWorldY + 0.85f, z), Quaternion.identity);
            BoxCollider collider = EnsureComponent<BoxCollider>(triggerObject);
            collider.isTrigger = true;
            collider.size = new Vector3(GoalHalfWidth * 2f, 1.7f, 0.35f);

            GoalPostTrigger trigger = EnsureComponent<GoalPostTrigger>(triggerObject);
            trigger.Configure(null, scoringPlayer, null, GoalHalfWidth, CrossbarWorldY);
            return trigger;
        }

        private static void ConfigurePostPart(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            GameObject part = GetOrCreatePrimitive(name, PrimitiveType.Cylinder, parent);
            part.transform.SetPositionAndRotation(position, rotation);
            part.transform.localScale = scale;
            SetMaterial(part, material);

            if (part.TryGetComponent(out Collider collider))
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Camera ConfigureCamera(Transform parent)
        {
            GameObject cameraObject = GetOrCreateChild("Main Camera", parent);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 9.4f, -7.4f);
            cameraObject.transform.LookAt(new Vector3(0f, TableTopY, 0f));

            Camera camera = EnsureComponent<Camera>(cameraObject);
            camera.orthographic = true;
            camera.orthographicSize = 6.8f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.Skybox;
            return camera;
        }

        private static void ConfigureLighting(Transform parent)
        {
            GameObject lightObject = GetOrCreateChild("Directional Light", parent);
            lightObject.transform.SetPositionAndRotation(new Vector3(0f, 6f, -3f), Quaternion.Euler(50f, -25f, 0f));
            Light light = EnsureComponent<Light>(lightObject);
            light.type = LightType.Directional;
            light.intensity = 1.25f;
        }

        private static void ConfigureEventSystem(Transform parent)
        {
            GameObject eventSystemObject = GetOrCreateChild("EventSystem", parent);
            EnsureComponent<EventSystem>(eventSystemObject);

            if (eventSystemObject.TryGetComponent(out StandaloneInputModule legacyModule))
            {
                Object.DestroyImmediate(legacyModule);
            }

            EnsureComponent<InputSystemUIInputModule>(eventSystemObject);
        }

        private static GameHudController ConfigureHud(Transform parent)
        {
            GameObject canvasObject = GetOrCreateUiChild("HudCanvas", parent);
            Canvas canvas = EnsureComponent<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureComponent<GraphicRaycaster>(canvasObject);
            GameHudController hud = EnsureComponent<GameHudController>(canvasObject);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text p1 = ConfigureText("PlayerOneScore", canvasObject.transform, new Vector2(24f, -24f), font, 30, TextAnchor.UpperLeft);
            Text p2 = ConfigureText("PlayerTwoScore", canvasObject.transform, new Vector2(24f, -62f), font, 30, TextAnchor.UpperLeft);
            Text player = ConfigureText("CurrentPlayer", canvasObject.transform, new Vector2(24f, -108f), font, 26, TextAnchor.UpperLeft);
            Text phase = ConfigureText("Phase", canvasObject.transform, new Vector2(24f, -144f), font, 24, TextAnchor.UpperLeft);
            Text flick = ConfigureText("FlickStrength", canvasObject.transform, new Vector2(24f, -180f), font, 24, TextAnchor.UpperLeft);
            Text fieldGoal = ConfigureText("FieldGoalMode", canvasObject.transform, new Vector2(24f, -216f), font, 24, TextAnchor.UpperLeft);
            Text last = ConfigureText("LastResult", canvasObject.transform, new Vector2(24f, -252f), font, 24, TextAnchor.UpperLeft);
            Text possession = ConfigureText("Possession", canvasObject.transform, new Vector2(24f, -288f), font, 24, TextAnchor.UpperLeft);
            Text controls = ConfigureText("Controls", canvasObject.transform, new Vector2(0f, 52f), font, 20, TextAnchor.LowerCenter);
            controls.rectTransform.anchorMin = new Vector2(0f, 0f);
            controls.rectTransform.anchorMax = new Vector2(1f, 0f);
            controls.rectTransform.pivot = new Vector2(0.5f, 0f);
            controls.rectTransform.sizeDelta = new Vector2(-72f, 64f);

            hud.Configure(p1, p2, player, phase, flick, fieldGoal, last, possession, controls);
            return hud;
        }

        private static Text ConfigureText(string name, Transform parent, Vector2 anchoredPosition, Font font, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = GetOrCreateUiChild(name, parent);
            Text text = EnsureComponent<Text>(textObject);
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;

            RectTransform rect = EnsureComponent<RectTransform>(textObject);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(520f, 36f);
            return text;
        }

        private static void BuildOrRepairLauncherScene()
        {
            Scene launcherScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LauncherScenePath) != null
                ? EditorSceneManager.OpenScene(LauncherScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Material backgroundMaterial = GetOrCreateMaterial("LauncherBackground.mat", new Color(0.08f, 0.1f, 0.12f));
            GameObject root = GetOrCreateRoot("PaperFootballLauncher");

            GameObject cameraObject = GetOrCreateChild("Main Camera", root.transform);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            Camera camera = EnsureComponent<Camera>(cameraObject);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            ConfigureEventSystem(root.transform);

            GameObject backdrop = GetOrCreatePrimitive("LauncherBackdrop", PrimitiveType.Cube, root.transform);
            backdrop.transform.SetPositionAndRotation(new Vector3(0f, 0f, 1f), Quaternion.identity);
            backdrop.transform.localScale = new Vector3(16f, 9f, 0.1f);
            SetMaterial(backdrop, backgroundMaterial);
            if (backdrop.TryGetComponent(out Collider backdropCollider))
            {
                Object.DestroyImmediate(backdropCollider);
            }

            GameObject canvasObject = GetOrCreateUiChild("LauncherCanvas", root.transform);
            Canvas canvas = EnsureComponent<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureComponent<GraphicRaycaster>(canvasObject);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text title = ConfigureText("LauncherTitle", canvasObject.transform, new Vector2(0f, -180f), font, 56, TextAnchor.MiddleCenter);
            title.text = "Paper Football Prototype";
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            title.rectTransform.sizeDelta = new Vector2(900f, 80f);

            Button startPrototype = ConfigureButton("StartPrototypeButton", canvasObject.transform, new Vector2(0f, -340f), "Start Tabletop Prototype", font, new Color(0.08f, 0.55f, 0.72f));
            Button legacyMenu = ConfigureButton("LegacyMenuButton", canvasObject.transform, new Vector2(0f, -450f), "Open Existing Main Menu", font, new Color(0.24f, 0.28f, 0.32f));
            Button legacyTable = ConfigureButton("LegacyTableButton", canvasObject.transform, new Vector2(0f, -550f), "Open Existing Table Scene", font, new Color(0.24f, 0.28f, 0.32f));

            PrototypeMenuController controller = EnsureComponent<PrototypeMenuController>(canvasObject);
            controller.Configure(startPrototype, legacyMenu, legacyTable, "PaperFootballGame", "MainMenu", "TableScene");

            MarkDirty(root, canvasObject);

            if (string.IsNullOrEmpty(launcherScene.path))
            {
                EditorSceneManager.SaveScene(launcherScene, LauncherScenePath);
            }
            else
            {
                EditorSceneManager.SaveScene(launcherScene);
            }

            Debug.Log($"Paper football launcher scene built: {LauncherScenePath}");
        }

        private static Button ConfigureButton(string name, Transform parent, Vector2 anchoredPosition, string label, Font font, Color color)
        {
            GameObject buttonObject = GetOrCreateUiChild(name, parent);
            RectTransform rect = EnsureComponent<RectTransform>(buttonObject);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(460f, 72f);

            Image image = EnsureComponent<Image>(buttonObject);
            image.color = color;
            Button button = EnsureComponent<Button>(buttonObject);

            Text text = ConfigureText($"{name}Text", buttonObject.transform, Vector2.zero, font, 28, TextAnchor.MiddleCenter);
            text.text = label;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchoredPosition = Vector2.zero;
            text.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new()
            {
                new EditorBuildSettingsScene(LauncherScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AddSceneIfExists(scenes, "Assets/Scenes/MainMenu.unity");
            AddSceneIfExists(scenes, "Assets/Scenes/TableScene.unity");
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddSceneIfExists(List<EditorBuildSettingsScene> scenes, string path)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (sceneAsset == null || scenes.Any(scene => scene.path == path))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        private static GameObject GetOrCreateRoot(string name)
        {
            GameObject root = GetOrCreateChild(name, null);
            root.transform.SetParent(null);
            return root;
        }

        private static GameObject GetOrCreateChild(string name, Transform parent)
        {
            List<GameObject> matches = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(go => go.scene == SceneManager.GetActiveScene() && go.name == name && (parent == null || go.transform.parent == parent))
                .ToList();

            GameObject result = matches.FirstOrDefault();
            if (result == null)
            {
                result = new GameObject(name);
            }

            result.transform.SetParent(parent);

            for (int i = 1; i < matches.Count; i++)
            {
                Object.DestroyImmediate(matches[i]);
            }

            return result;
        }

        private static GameObject GetOrCreateUiChild(string name, Transform parent)
        {
            List<GameObject> matches = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(go => go.scene == SceneManager.GetActiveScene() && go.name == name && (parent == null || go.transform.parent == parent))
                .ToList();

            GameObject result = matches.FirstOrDefault();
            if (result == null)
            {
                result = new GameObject(name, typeof(RectTransform));
            }

            result.transform.SetParent(parent);

            for (int i = 1; i < matches.Count; i++)
            {
                Object.DestroyImmediate(matches[i]);
            }

            return result;
        }

        private static GameObject GetOrCreatePrimitive(string name, PrimitiveType primitiveType, Transform parent)
        {
            GameObject existing = GetOrCreateChild(name, parent);
            if (existing.TryGetComponent<MeshFilter>(out _) && existing.TryGetComponent<MeshRenderer>(out _))
            {
                return existing;
            }

            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            MeshFilter sourceFilter = primitive.GetComponent<MeshFilter>();
            MeshRenderer sourceRenderer = primitive.GetComponent<MeshRenderer>();
            MeshFilter targetFilter = EnsureComponent<MeshFilter>(existing);
            MeshRenderer targetRenderer = EnsureComponent<MeshRenderer>(existing);
            targetFilter.sharedMesh = sourceFilter.sharedMesh;
            targetRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            Object.DestroyImmediate(primitive);
            return existing;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            if (target.TryGetComponent(out T component))
            {
                return component;
            }

            return target.AddComponent<T>();
        }

        private static void SetMaterial(GameObject target, Material material)
        {
            MeshRenderer renderer = EnsureComponent<MeshRenderer>(target);
            renderer.sharedMaterial = material;
        }

        private static void MarkDirty(params Object[] objects)
        {
            foreach (Object obj in objects)
            {
                if (obj != null)
                {
                    EditorUtility.SetDirty(obj);
                }
            }
        }
    }
}
