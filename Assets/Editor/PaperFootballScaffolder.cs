using System.Collections.Generic;
using System.Linq;
using PaperFootball.Ball;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Match;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using PaperFootball.Tabletop.Roguelike.Consumables;
using PaperFootball.Tabletop.Roguelike.Encounters;
using PaperFootball.Tabletop.Roguelike.Modifiers;
using PaperFootball.Tabletop.Roguelike.Opponents;
using PaperFootball.Tabletop.Roguelike.Presentation;
using PaperFootball.Tabletop.Roguelike.Run;
using PaperFootball.Tabletop.Roguelike.Variance;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Shots;
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
        private const string RoguelikeFolder = "Assets/Materials/PaperFootballPrototype/Roguelike";
        private const string ShotVariancePath = "Assets/Materials/PaperFootballPrototype/Roguelike/DefaultShotVarianceSettings.asset";
        private const string AirFlickSettingsPath = "Assets/Materials/PaperFootballPrototype/AirFlickShotSettings.asset";
        private const string UpgradeCatalogPath = "Assets/Materials/PaperFootballPrototype/Roguelike/DefaultUpgradeCatalog.asset";
        private const string OpponentCatalogPath = "Assets/Materials/PaperFootballPrototype/Roguelike/DefaultOpponentCatalog.asset";
        private const string SurfaceCatalogPath = "Assets/Materials/PaperFootballPrototype/Roguelike/DefaultTableSurfaceCatalog.asset";
        private const string ObstacleCatalogPath = "Assets/Materials/PaperFootballPrototype/Roguelike/DefaultObstacleLayoutCatalog.asset";

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
            ShotVarianceSettings shotVarianceSettings = GetOrCreateShotVarianceSettings();
            AirFlickShotSettings airFlickShotSettings = GetOrCreateAirFlickShotSettings();
            UpgradeCatalog upgradeCatalog = GetOrCreateUpgradeCatalog();
            OpponentCatalog opponentCatalog = GetOrCreateOpponentCatalog();
            TableSurfaceCatalog surfaceCatalog = GetOrCreateSurfaceCatalog();
            ObstacleLayoutCatalog obstacleCatalog = GetOrCreateObstacleLayoutCatalog();
            Material tableMaterial = GetOrCreateMaterial("Table.mat", new Color(0.42f, 0.26f, 0.13f));
            Material floorMaterial = GetOrCreateMaterial("Floor.mat", new Color(0.12f, 0.13f, 0.15f));
            Material footballMaterial = GetOrCreateMaterial("PaperFootball.mat", new Color(0.96f, 0.95f, 0.86f));
            Material footballFoldLineMaterial = GetOrCreateMaterial("FootballFoldLine.mat", new Color(0.12f, 0.09f, 0.065f));
            Material footballCornerMarkMaterial = GetOrCreateMaterial("FootballCornerMark.mat", new Color(0.72f, 0.14f, 0.09f));
            Material edgeOneMaterial = GetOrCreateMaterial("PlayerOneEdge.mat", new Color(0.1f, 0.7f, 0.95f, 0.85f));
            Material edgeTwoMaterial = GetOrCreateMaterial("PlayerTwoEdge.mat", new Color(1f, 0.35f, 0.24f, 0.85f));
            Material indicatorMaterial = GetOrCreateMaterial("AimIndicator.mat", new Color(0.1f, 0.95f, 0.75f));
            Material contactMarkerMaterial = GetOrCreateMaterial("ContactMarker.mat", new Color(1f, 0.82f, 0.12f));
            Material uncertaintyMaterial = GetOrCreateMaterial("UncertaintyPreview.mat", new Color(0.95f, 0.85f, 0.18f));
            Material targetMaterial = GetOrCreateMaterial("PrecisionTarget.mat", new Color(0.2f, 0.85f, 0.32f, 0.55f));
            Material obstacleMaterial = GetOrCreateMaterial("EncounterObstacle.mat", new Color(0.18f, 0.16f, 0.14f));
            Material tapeMaterial = GetOrCreateMaterial("TapeFrictionPatch.mat", new Color(0.9f, 0.88f, 0.62f, 0.7f));
            Material eraserMaterial = GetOrCreateMaterial("EraserBlocker.mat", new Color(0.95f, 0.35f, 0.45f));
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
            playerOneStart.transform.SetPositionAndRotation(new Vector3(0f, FootballCenterY, -config.Rules.kickoffOffsetFromCenter), Quaternion.identity);

            GameObject playerTwoStart = GetOrCreateChild("PlayerTwoStart", root.transform);
            playerTwoStart.transform.SetPositionAndRotation(new Vector3(0f, FootballCenterY, config.Rules.kickoffOffsetFromCenter), Quaternion.Euler(0f, 180f, 0f));

            GameObject playerOneFieldGoalSpot = GetOrCreateChild("PlayerOneFieldGoalSpot", root.transform);
            playerOneFieldGoalSpot.transform.SetPositionAndRotation(new Vector3(0f, FootballCenterY, 2.2f), Quaternion.identity);

            GameObject playerTwoFieldGoalSpot = GetOrCreateChild("PlayerTwoFieldGoalSpot", root.transform);
            playerTwoFieldGoalSpot.transform.SetPositionAndRotation(new Vector3(0f, FootballCenterY, -2.2f), Quaternion.Euler(0f, 180f, 0f));

            GameObject football = GetOrCreateChild("Paper Football", root.transform);
            football.transform.SetPositionAndRotation(playerOneStart.transform.position, playerOneStart.transform.rotation);
            football.transform.localScale = Vector3.one;
            RemoveComponent<PaperFootballMesh>(football);
            RemoveComponent<MeshFilter>(football);
            RemoveComponent<MeshRenderer>(football);
            GameObject footballVisual = GetOrCreateChild("PaperFootballVisual", football.transform);
            footballVisual.transform.localPosition = Vector3.zero;
            footballVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            footballVisual.transform.localScale = Vector3.one;
            EnsureComponent<MeshFilter>(footballVisual);
            MeshRenderer footballRenderer = EnsureComponent<MeshRenderer>(footballVisual);
            footballRenderer.sharedMaterial = footballMaterial;
            EnsureComponent<PaperFootballMesh>(footballVisual);
            BoxCollider footballCollider = EnsureComponent<BoxCollider>(football);
            footballCollider.size = new Vector3(0.46f, 0.16f, 0.62f);
            footballCollider.center = Vector3.zero;
            footballCollider.material = footballPhysicsMaterial;
            Rigidbody footballBody = EnsureComponent<Rigidbody>(football);
            footballBody.mass = 0.16f;
            footballBody.useGravity = true;
            footballBody.linearDamping = 1.15f;
            footballBody.angularDamping = config.Rules.footballAngularDamping;
            footballBody.maxAngularVelocity = config.Rules.maximumFootballAngularVelocity;
            footballBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            FootballPhysicsController physicsController = EnsureComponent<FootballPhysicsController>(football);
            AirFlickLandingController airFlickLandingController = EnsureComponent<AirFlickLandingController>(football);
            airFlickLandingController.Configure(physicsController, tableCollider, airFlickShotSettings);
            FootballRestDetector restDetector = EnsureComponent<FootballRestDetector>(football);
            restDetector.Configure(config.Rules);
            GameObject footballFoldLine = ConfigureFootballSpinReferencePart(
                football.transform,
                "FootballFoldLine",
                new Vector3(0f, 0.083f, 0.02f),
                new Vector3(0.032f, 0.01f, 0.48f),
                footballFoldLineMaterial);
            GameObject footballCornerMark = ConfigureFootballSpinReferencePart(
                football.transform,
                "FootballCornerMark",
                new Vector3(-0.12f, 0.086f, -0.22f),
                new Vector3(0.12f, 0.012f, 0.05f),
                footballCornerMarkMaterial);

            GameObject goalposts = GetOrCreateChild("Goalposts", root.transform);
            ConfigureGoalpost("PlayerOneGoalpost", goalposts.transform, TableScale.z * 0.5f + 0.35f, edgeOneMaterial);
            ConfigureGoalpost("PlayerTwoGoalpost", goalposts.transform, -TableScale.z * 0.5f - 0.35f, edgeTwoMaterial);
            GoalPostTrigger playerOneGoalTrigger = ConfigureGoalTrigger("PlayerOneGoalTrigger", goalposts.transform, TableScale.z * 0.5f + 0.35f, PaperFootballPlayer.PlayerOne);
            GoalPostTrigger playerTwoGoalTrigger = ConfigureGoalTrigger("PlayerTwoGoalTrigger", goalposts.transform, -TableScale.z * 0.5f - 0.35f, PaperFootballPlayer.PlayerTwo);

            Camera camera = ConfigureCamera(root.transform);
            FootballCameraController cameraController = EnsureComponent<FootballCameraController>(camera.gameObject);
            cameraController.Configure(
                camera,
                football.transform,
                new Vector3(0f, 9.4f, -7.4f),
                new Vector3(0f, TableTopY, 0f),
                6.8f,
                new Vector3(0f, 2.15f, -1.85f),
                0.95f,
                0.35f);
            ConfigureLighting(root.transform);
            ConfigureEventSystem(root.transform);

            GameObject inputObject = GetOrCreateChild("FlickInputReader", root.transform);
            FlickInputReader inputReader = EnsureComponent<FlickInputReader>(inputObject);
            inputReader.Configure(camera, footballCollider, config.Rules, TableTopY + 0.05f);

            GameObject contactSelectorObject = GetOrCreateChild("ContactPointSelector", root.transform);
            ContactPointSelector contactSelector = EnsureComponent<ContactPointSelector>(contactSelectorObject);
            contactSelector.Configure(camera, footballCollider);

            GameObject boundaryObject = GetOrCreateChild("TableBoundaryDetector", root.transform);
            TableBoundaryDetector boundaryDetector = EnsureComponent<TableBoundaryDetector>(boundaryObject);
            boundaryDetector.Configure(tableCollider, config.Rules);

            GameObject indicatorObject = GetOrCreateChild("FlickAimIndicator", root.transform);
            LineRenderer lineRenderer = EnsureComponent<LineRenderer>(indicatorObject);
            lineRenderer.sharedMaterial = indicatorMaterial;
            lineRenderer.positionCount = 2;
            FlickAimIndicator indicator = EnsureComponent<FlickAimIndicator>(indicatorObject);

            GameObject trajectoryObject = GetOrCreateChild("FieldGoalTrajectoryPreview", root.transform);
            LineRenderer trajectoryLine = EnsureComponent<LineRenderer>(trajectoryObject);
            trajectoryLine.sharedMaterial = indicatorMaterial;
            trajectoryLine.positionCount = 0;
            TrajectoryPreviewRenderer trajectoryPreview = EnsureComponent<TrajectoryPreviewRenderer>(trajectoryObject);
            trajectoryPreview.Configure(footballBody, config.Rules);

            GameHudController hud = ConfigureHud(root.transform);
            ShotSelectionController shotSelection = ConfigureShotSelection(hud.transform);
            OverhangDebugOverlay overhangDebugOverlay = ConfigureOverhangDebugOverlay(hud.transform);
            FootballSpinDebugOverlay spinDebugOverlay = ConfigureSpinDebugOverlay(hud.transform, physicsController);
            ContactPointIndicator contactIndicator = ConfigureContactPointIndicator(root.transform, hud.transform, contactMarkerMaterial, indicatorMaterial);

            GameObject shotVarianceObject = GetOrCreateChild("ShotVarianceController", root.transform);
            ShotVarianceController shotVarianceController = EnsureComponent<ShotVarianceController>(shotVarianceObject);
            shotVarianceController.Configure(shotVarianceSettings, footballCollider, 12345);
            shotVarianceController.SetVarianceEnabled(false);

            ShotUncertaintyPreview uncertaintyPreview = ConfigureShotUncertaintyPreview(
                root.transform,
                hud.transform,
                uncertaintyMaterial,
                contactSelector,
                shotVarianceController);

            TableSurfaceApplier surfaceApplier = EnsureComponent<TableSurfaceApplier>(table);
            surfaceApplier.Configure(tableCollider, table.GetComponent<Renderer>());

            GameObject obstacleRoot = GetOrCreateChild("EncounterObstacles", root.transform);
            ObstacleLayoutController obstacleLayoutController = EnsureComponent<ObstacleLayoutController>(obstacleRoot);
            obstacleLayoutController.Configure(obstacleRoot.transform, obstacleMaterial);

            GameObject temporaryRoot = GetOrCreateChild("TemporaryEncounterPlacements", root.transform);
            TemporaryPlacementController temporaryPlacementController = EnsureComponent<TemporaryPlacementController>(temporaryRoot);
            temporaryPlacementController.Configure(temporaryRoot.transform, tableCollider, footballCollider, tapeMaterial, eraserMaterial);

            PrecisionTargetZone precisionTargetZone = ConfigurePrecisionTargetZone(root.transform, targetMaterial);

            GameObject fieldGoalObject = GetOrCreateChild("FieldGoalController", root.transform);
            FieldGoalController fieldGoalController = EnsureComponent<FieldGoalController>(fieldGoalObject);
            fieldGoalController.Configure(
                playerOneFieldGoalSpot.transform,
                playerTwoFieldGoalSpot.transform,
                playerOneGoalTrigger,
                playerTwoGoalTrigger,
                footballCollider);

            GameObject interactionObject = GetOrCreateChild("FlickInteractionController", root.transform);
            FlickInteractionController flickInteraction = EnsureComponent<FlickInteractionController>(interactionObject);
            flickInteraction.Configure(
                contactSelector,
                inputReader,
                cameraController,
                contactIndicator,
                physicsController,
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
                overhangDebugOverlay,
                trajectoryPreview,
                fieldGoalController,
                footballCollider,
                playerOneStart.transform,
                playerTwoStart.transform,
                flickInteraction,
                shotVarianceController,
                uncertaintyPreview,
                shotSelection,
                airFlickLandingController,
                airFlickShotSettings);

            GameObject opponentObject = GetOrCreateChild("OpponentTurnController", root.transform);
            OpponentTurnController opponentTurnController = EnsureComponent<OpponentTurnController>(opponentObject);
            opponentTurnController.Configure(matchController, physicsController, footballCollider, contactIndicator, indicator, obstacleLayoutController);
            opponentTurnController.SetAiEnabled(false);

            RunProgressionUiController runUi = ConfigureRunUi(root.transform);

            GameObject runObject = GetOrCreateChild("RunController", root.transform);
            RunController runController = EnsureComponent<RunController>(runObject);
            runController.Configure(
                upgradeCatalog,
                opponentCatalog,
                surfaceCatalog,
                obstacleCatalog,
                matchController,
                physicsController,
                shotVarianceController,
                opponentTurnController,
                surfaceApplier,
                obstacleLayoutController,
                temporaryPlacementController,
                precisionTargetZone,
                runUi);

            RoguelikeDebugOverlay roguelikeDebugOverlay = ConfigureRoguelikeDebugOverlay(
                hud.transform,
                runController,
                shotVarianceController,
                opponentTurnController,
                surfaceApplier,
                obstacleLayoutController);

            GameObject devCommandsObject = GetOrCreateChild("RunDevelopmentCommands", root.transform);
            RunDevelopmentCommands devCommands = EnsureComponent<RunDevelopmentCommands>(devCommandsObject);
            devCommands.Configure(runController, shotVarianceController, true);

            MarkDirty(
                config,
                shotVarianceSettings,
                airFlickShotSettings,
                upgradeCatalog,
                opponentCatalog,
                surfaceCatalog,
                obstacleCatalog,
                table,
                floor,
                playerOneEdge,
                playerTwoEdge,
                football,
                footballVisual,
                footballFoldLine,
                footballCornerMark,
                goalposts,
                inputObject,
                contactSelectorObject,
                boundaryObject,
                indicatorObject,
                trajectoryObject,
                shotVarianceObject,
                uncertaintyPreview.gameObject,
                shotSelection.gameObject,
                obstacleRoot,
                temporaryRoot,
                precisionTargetZone.gameObject,
                overhangDebugOverlay.gameObject,
                spinDebugOverlay.gameObject,
                roguelikeDebugOverlay.gameObject,
                contactIndicator.gameObject,
                fieldGoalObject,
                interactionObject,
                hud.gameObject,
                matchObject,
                opponentObject,
                runUi.gameObject,
                runObject,
                devCommandsObject);

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

            if (!AssetDatabase.IsValidFolder(RoguelikeFolder))
            {
                AssetDatabase.CreateFolder(GeneratedFolder, "Roguelike");
            }
        }

        private static ShotVarianceSettings GetOrCreateShotVarianceSettings()
        {
            ShotVarianceSettings settings = GetOrCreateScriptableObject<ShotVarianceSettings>(ShotVariancePath);
            settings.Configure(true, 0.03f, 1.5f, 0.0075f, false, "Stable");
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static AirFlickShotSettings GetOrCreateAirFlickShotSettings()
        {
            AirFlickShotSettings settings = GetOrCreateScriptableObject<AirFlickShotSettings>(AirFlickSettingsPath);
            settings.Sanitize();
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static UpgradeCatalog GetOrCreateUpgradeCatalog()
        {
            FootballUpgradeDefinition tightFold = GetOrCreateUpgrade(
                "tight_fold",
                "Tight Fold",
                "Cleaner creases reduce contact and direction uncertainty, with slightly calmer spin.",
                UpgradeRarity.Common,
                3,
                1.2f,
                new[]
                {
                    new FootballModifier("tight_fold.contact", FootballModifierType.ContactPointVariance, FootballModifierOperation.Multiply, 0.65f),
                    new FootballModifier("tight_fold.direction", FootballModifierType.DirectionVariance, FootballModifierOperation.Multiply, 0.75f),
                    new FootballModifier("tight_fold.spin", FootballModifierType.SpinTorque, FootballModifierOperation.Multiply, 0.9f)
                },
                new[] { "fold", "control" });

            FootballUpgradeDefinition weightedCenter = GetOrCreateUpgrade(
                "weighted_center",
                "Weighted Center",
                "Adds stability and straighter movement while reducing spin sensitivity.",
                UpgradeRarity.Uncommon,
                2,
                0.8f,
                new[]
                {
                    new FootballModifier("weighted.spin", FootballModifierType.SpinTorque, FootballModifierOperation.Multiply, 0.75f),
                    new FootballModifier("weighted.direction", FootballModifierType.DirectionVariance, FootballModifierOperation.Multiply, 0.85f),
                    new FootballModifier("weighted.angular", FootballModifierType.AngularDamping, FootballModifierOperation.Multiply, 1.1f),
                    new FootballModifier("weighted.com_y", FootballModifierType.CenterOfMassY, FootballModifierOperation.Add, -0.01f)
                },
                new[] { "weight", "control" },
                new[] { "loose_weight" });

            FootballUpgradeDefinition looseFold = GetOrCreateUpgrade(
                "loose_fold",
                "Loose Fold",
                "More dramatic spin potential at the cost of contact consistency.",
                UpgradeRarity.Common,
                3,
                1f,
                new[]
                {
                    new FootballModifier("loose.spin", FootballModifierType.SpinTorque, FootballModifierOperation.Multiply, 1.3f),
                    new FootballModifier("loose.contact", FootballModifierType.ContactPointVariance, FootballModifierOperation.Multiply, 1.35f),
                    new FootballModifier("loose.max_av", FootballModifierType.MaximumAngularVelocity, FootballModifierOperation.Multiply, 1.15f)
                },
                new[] { "fold", "loose_weight", "spin" },
                new[] { "weight" });

            FootballUpgradeDefinition waxedPaper = GetOrCreateUpgrade(
                "waxed_paper",
                "Waxed Paper",
                "Slides farther on the desk, but adds a little force uncertainty.",
                UpgradeRarity.Uncommon,
                2,
                0.8f,
                new[]
                {
                    new FootballModifier("wax.friction", FootballModifierType.Friction, FootballModifierOperation.Multiply, 0.75f),
                    new FootballModifier("wax.force", FootballModifierType.FlickForce, FootballModifierOperation.Multiply, 1.08f),
                    new FootballModifier("wax.force_variance", FootballModifierType.ForceVariance, FootballModifierOperation.Multiply, 1.15f),
                    new FootballModifier("wax.linear", FootballModifierType.LinearDamping, FootballModifierOperation.Multiply, 0.85f)
                },
                new[] { "surface", "travel" });

            FootballUpgradeDefinition reinforcedTip = GetOrCreateUpgrade(
                "reinforced_tip",
                "Reinforced Tip",
                "Field-goal flicks carry a little better and preview more clearly.",
                UpgradeRarity.Rare,
                2,
                0.45f,
                new[]
                {
                    new FootballModifier("tip.fg_force", FootballModifierType.FieldGoalForce, FootballModifierOperation.Multiply, 1.08f),
                    new FootballModifier("tip.fg_direction", FootballModifierType.FieldGoalDirectionVariance, FootballModifierOperation.Multiply, 0.75f),
                    new FootballModifier("tip.preview", FootballModifierType.PreviewAccuracy, FootballModifierOperation.Add, 0.15f)
                },
                new[] { "field_goal", "control" });

            UpgradeCatalog catalog = GetOrCreateScriptableObject<UpgradeCatalog>(UpgradeCatalogPath);
            catalog.Configure(new[] { tightFold, weightedCenter, looseFold, waxedPaper, reinforcedTip });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static FootballUpgradeDefinition GetOrCreateUpgrade(
            string id,
            string displayName,
            string description,
            UpgradeRarity rarity,
            int maxStacks,
            float rewardWeight,
            FootballModifier[] modifiers,
            string[] tags,
            string[] mutualExclusionTags = null)
        {
            string path = $"{RoguelikeFolder}/{id}.asset";
            FootballUpgradeDefinition upgrade = GetOrCreateScriptableObject<FootballUpgradeDefinition>(path);
            upgrade.Configure(id, displayName, description, rarity, maxStacks, rewardWeight, modifiers, tags, mutualExclusionTags);
            EditorUtility.SetDirty(upgrade);
            return upgrade;
        }

        private static OpponentCatalog GetOrCreateOpponentCatalog()
        {
            OpponentProfile power = GetOrCreateOpponent(
                "power_flicker",
                "Power Flicker",
                0.86f,
                0.16f,
                0.25f,
                OpponentContactPreference.SlightlyOffCenter,
                0.55f,
                0.82f,
                0.78f,
                0.45f,
                0.2f,
                0.5f);
            OpponentProfile spinner = GetOrCreateOpponent(
                "spinner",
                "Spinner",
                0.55f,
                0.18f,
                0.95f,
                OpponentContactPreference.OffCenter,
                0.58f,
                0.65f,
                0.55f,
                0.55f,
                0.55f,
                0.55f);
            OpponentProfile calculator = GetOrCreateOpponent(
                "calculator",
                "Calculator",
                0.48f,
                0.06f,
                0.05f,
                OpponentContactPreference.Center,
                0.9f,
                0.25f,
                0.35f,
                0.7f,
                0.25f,
                0.45f);

            OpponentCatalog catalog = GetOrCreateScriptableObject<OpponentCatalog>(OpponentCatalogPath);
            catalog.Configure(new[] { power, spinner, calculator });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static OpponentProfile GetOrCreateOpponent(
            string id,
            string displayName,
            float preferredPower,
            float powerVariance,
            float preferredSpin,
            OpponentContactPreference contactPreference,
            float accuracy,
            float risk,
            float edgePreference,
            float fieldGoalSkill,
            float obstaclePreference,
            float delay)
        {
            string path = $"{RoguelikeFolder}/{id}.asset";
            OpponentProfile profile = GetOrCreateScriptableObject<OpponentProfile>(path);
            profile.Configure(id, displayName, preferredPower, powerVariance, preferredSpin, contactPreference, accuracy, risk, edgePreference, fieldGoalSkill, obstaclePreference, delay);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static TableSurfaceCatalog GetOrCreateSurfaceCatalog()
        {
            TableSurfaceDefinition normal = GetOrCreateSurface("normal_desk", "Normal Desk", TableSurfaceKind.NormalDesk, 0.55f, 0.65f, 0.04f, 1f, new Color(0.42f, 0.26f, 0.13f));
            TableSurfaceDefinition slippery = GetOrCreateSurface("slippery_desk", "Slippery Desk", TableSurfaceKind.SlipperyDesk, 0.25f, 0.32f, 0.035f, 0.85f, new Color(0.25f, 0.38f, 0.42f));
            TableSurfaceDefinition rough = GetOrCreateSurface("rough_desk", "Rough Desk", TableSurfaceKind.RoughDesk, 0.95f, 1.05f, 0.02f, 1.2f, new Color(0.35f, 0.31f, 0.22f));
            TableSurfaceDefinition science = GetOrCreateSurface("science_lab_table", "Science Lab Table", TableSurfaceKind.ScienceLabTable, 0.34f, 0.44f, 0.03f, 0.9f, new Color(0.32f, 0.43f, 0.46f));

            TableSurfaceCatalog catalog = GetOrCreateScriptableObject<TableSurfaceCatalog>(SurfaceCatalogPath);
            catalog.Configure(new[] { normal, slippery, rough, science });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static TableSurfaceDefinition GetOrCreateSurface(string id, string displayName, TableSurfaceKind kind, float dynamicFriction, float staticFriction, float bounce, float damping, Color color)
        {
            string path = $"{RoguelikeFolder}/{id}.asset";
            TableSurfaceDefinition surface = GetOrCreateScriptableObject<TableSurfaceDefinition>(path);
            surface.Configure(id, displayName, kind, dynamicFriction, staticFriction, bounce, damping, color);
            EditorUtility.SetDirty(surface);
            return surface;
        }

        private static ObstacleLayoutCatalog GetOrCreateObstacleLayoutCatalog()
        {
            ObstacleLayoutDefinition none = GetOrCreateLayout("no_obstacles", "No Obstacles", ObstacleLayoutKind.None, new ObstacleSpawn[0]);
            ObstacleLayoutDefinition pencil = GetOrCreateLayout("pencil_lane", "Pencil Lane", ObstacleLayoutKind.Pencil, new[]
            {
                new ObstacleSpawn { kind = ObstacleKind.Pencil, position = new Vector3(0f, TableTopY + 0.18f, 0.4f), scale = new Vector3(0.055f, 1.6f, 0.055f), eulerAngles = new Vector3(0f, 0f, 90f) }
            });
            ObstacleLayoutDefinition eraser = GetOrCreateLayout("eraser_midfield", "Eraser Midfield", ObstacleLayoutKind.Eraser, new[]
            {
                new ObstacleSpawn { kind = ObstacleKind.Eraser, position = new Vector3(0.95f, TableTopY + 0.18f, 0.1f), scale = new Vector3(0.55f, 0.18f, 0.36f), eulerAngles = Vector3.zero }
            });
            ObstacleLayoutDefinition book = GetOrCreateLayout("book_bank", "Book Bank", ObstacleLayoutKind.Book, new[]
            {
                new ObstacleSpawn { kind = ObstacleKind.Book, position = new Vector3(-1.1f, TableTopY + 0.13f, 1.25f), scale = new Vector3(1.15f, 0.12f, 0.8f), eulerAngles = new Vector3(0f, 18f, 0f) }
            });
            ObstacleLayoutDefinition mixed = GetOrCreateLayout("mixed_office", "Mixed Office", ObstacleLayoutKind.Mixed, new[]
            {
                new ObstacleSpawn { kind = ObstacleKind.Pencil, position = new Vector3(-0.75f, TableTopY + 0.18f, -0.55f), scale = new Vector3(0.055f, 1.35f, 0.055f), eulerAngles = new Vector3(0f, 0f, 70f) },
                new ObstacleSpawn { kind = ObstacleKind.Eraser, position = new Vector3(1.05f, TableTopY + 0.18f, 0.85f), scale = new Vector3(0.48f, 0.18f, 0.34f), eulerAngles = new Vector3(0f, -18f, 0f) },
                new ObstacleSpawn { kind = ObstacleKind.Book, position = new Vector3(-1.45f, TableTopY + 0.13f, 2.1f), scale = new Vector3(0.95f, 0.12f, 0.72f), eulerAngles = new Vector3(0f, 24f, 0f) }
            });

            ObstacleLayoutCatalog catalog = GetOrCreateScriptableObject<ObstacleLayoutCatalog>(ObstacleCatalogPath);
            catalog.Configure(new[] { none, pencil, eraser, book, mixed });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static ObstacleLayoutDefinition GetOrCreateLayout(string id, string displayName, ObstacleLayoutKind kind, ObstacleSpawn[] spawns)
        {
            string path = $"{RoguelikeFolder}/{id}.asset";
            ObstacleLayoutDefinition layout = GetOrCreateScriptableObject<ObstacleLayoutDefinition>(path);
            layout.Configure(id, displayName, kind, spawns);
            EditorUtility.SetDirty(layout);
            return layout;
        }

        private static T GetOrCreateScriptableObject<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
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
                config.Rules.maximumFlickForce = 4f;
                config.Rules.minimumFlickForce = 0.35f;
                config.Rules.flickForceResponseExponent = 1.6f;
                config.Rules.maximumDragDistance = 2.5f;
                config.Rules.footballStoppingThreshold = 0.08f;
                config.Rules.angularStoppingThreshold = 0.25f;
                config.Rules.footballAngularDamping = 0.8f;
                config.Rules.contactYawTorqueMultiplier = 2.5f;
                config.Rules.maximumFootballAngularVelocity = 24f;
                config.Rules.requiredStillTime = 0.35f;
                config.Rules.fallHeight = -1.2f;
                config.Rules.fieldGoalTimeLimit = 6f;
                config.Rules.kickoffOffsetFromCenter = 3.8f;
                config.Rules.minimumFieldGoalForce = 2.5f;
                config.Rules.maximumFieldGoalForce = 9f;
                config.Rules.minimumFieldGoalLaunchAngle = 28f;
                config.Rules.maximumFieldGoalLaunchAngle = 58f;
                config.Rules.minimumFieldGoalUpwardForce = 2f;
                config.Rules.maximumFieldGoalUpwardForce = 7f;
                config.Rules.trajectoryPointCount = 28;
                config.Rules.trajectoryTimeStep = 0.075f;
                config.Rules.maximumTrajectoryPreviewTime = 2.1f;
                config.Rules.trajectoryCollisionMask = 0;
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            if (config.Rules.footballAngularDamping <= 0.05f)
            {
                config.Rules.footballAngularDamping = 0.8f;
            }

            if (config.Rules.contactYawTorqueMultiplier <= 0f)
            {
                config.Rules.contactYawTorqueMultiplier = 2.5f;
            }

            if (config.Rules.maximumFootballAngularVelocity <= 0.1f)
            {
                config.Rules.maximumFootballAngularVelocity = 24f;
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

        private static ShotSelectionController ConfigureShotSelection(Transform hudParent)
        {
            GameObject selectionObject = GetOrCreateUiChild("ShotSelectionController", hudParent);
            RectTransform selectionRect = EnsureComponent<RectTransform>(selectionObject);
            selectionRect.anchorMin = new Vector2(0f, 1f);
            selectionRect.anchorMax = new Vector2(0f, 1f);
            selectionRect.pivot = new Vector2(0f, 1f);
            selectionRect.anchoredPosition = new Vector2(410f, -24f);
            selectionRect.sizeDelta = new Vector2(560f, 210f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text label = ConfigureText("SelectedShotLabel", selectionObject.transform, new Vector2(0f, 0f), font, 24, TextAnchor.UpperLeft);
            label.rectTransform.anchorMin = new Vector2(0f, 1f);
            label.rectTransform.anchorMax = new Vector2(0f, 1f);
            label.rectTransform.pivot = new Vector2(0f, 1f);
            label.rectTransform.sizeDelta = new Vector2(320f, 34f);

            Text description = ConfigureText("ShotDescription", selectionObject.transform, new Vector2(0f, -36f), font, 17, TextAnchor.UpperLeft);
            description.rectTransform.anchorMin = new Vector2(0f, 1f);
            description.rectTransform.anchorMax = new Vector2(0f, 1f);
            description.rectTransform.pivot = new Vector2(0f, 1f);
            description.rectTransform.sizeDelta = new Vector2(520f, 84f);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;

            Button flat = ConfigureCompactButton("FlatShotButton", selectionObject.transform, new Vector2(0f, -142f), "1 Flat", font, new Color(0.08f, 0.44f, 0.58f));
            Button air = ConfigureCompactButton("AirFlickShotButton", selectionObject.transform, new Vector2(180f, -142f), "2 Flick", font, new Color(0.56f, 0.36f, 0.08f));

            ShotSelectionController selector = EnsureComponent<ShotSelectionController>(selectionObject);
            selector.Configure(flat, air, label, description);
            return selector;
        }

        private static OverhangDebugOverlay ConfigureOverhangDebugOverlay(Transform parent)
        {
            GameObject overlayObject = GetOrCreateUiChild("OverhangDebugOverlay", parent);
            RectTransform overlayRect = EnsureComponent<RectTransform>(overlayObject);
            overlayRect.anchorMin = new Vector2(1f, 1f);
            overlayRect.anchorMax = new Vector2(1f, 1f);
            overlayRect.pivot = new Vector2(1f, 1f);
            overlayRect.anchoredPosition = new Vector2(-24f, -24f);
            overlayRect.sizeDelta = new Vector2(560f, 440f);

            GameObject textObject = GetOrCreateUiChild("OverhangDebugText", overlayObject.transform);
            Text text = EnsureComponent<Text>(textObject);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = new Color(0.95f, 0.98f, 1f);
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = false;

            RectTransform textRect = EnsureComponent<RectTransform>(textObject);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(1f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;

            OverhangDebugOverlay overlay = EnsureComponent<OverhangDebugOverlay>(overlayObject);
            overlay.Configure(null, text, false);
            return overlay;
        }

        private static FootballSpinDebugOverlay ConfigureSpinDebugOverlay(Transform parent, FootballPhysicsController physicsController)
        {
            GameObject overlayObject = GetOrCreateUiChild("SpinDebugOverlay", parent);
            RectTransform overlayRect = EnsureComponent<RectTransform>(overlayObject);
            overlayRect.anchorMin = new Vector2(1f, 0f);
            overlayRect.anchorMax = new Vector2(1f, 0f);
            overlayRect.pivot = new Vector2(1f, 0f);
            overlayRect.anchoredPosition = new Vector2(-24f, 120f);
            overlayRect.sizeDelta = new Vector2(620f, 190f);

            GameObject textObject = GetOrCreateUiChild("SpinDebugText", overlayObject.transform);
            Text text = EnsureComponent<Text>(textObject);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = new Color(1f, 0.92f, 0.62f);
            text.alignment = TextAnchor.LowerRight;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform textRect = EnsureComponent<RectTransform>(textObject);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(1f, 0f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;

            FootballSpinDebugOverlay overlay = EnsureComponent<FootballSpinDebugOverlay>(overlayObject);
            overlay.Configure(physicsController, text, true);
            return overlay;
        }

        private static ContactPointIndicator ConfigureContactPointIndicator(Transform parent, Transform hudParent, Material markerMaterial, Material lineMaterial)
        {
            GameObject indicatorObject = GetOrCreateChild("ContactPointIndicator", parent);

            GameObject markerObject = GetOrCreatePrimitive("ContactPointMarker", PrimitiveType.Sphere, indicatorObject.transform);
            markerObject.transform.localScale = Vector3.one * 0.075f;
            SetMaterial(markerObject, markerMaterial);
            if (markerObject.TryGetComponent(out Collider markerCollider))
            {
                Object.DestroyImmediate(markerCollider);
            }

            GameObject yawObject = GetOrCreateChild("ContactYawPreview", indicatorObject.transform);
            LineRenderer yawLine = EnsureComponent<LineRenderer>(yawObject);
            yawLine.sharedMaterial = lineMaterial;
            yawLine.useWorldSpace = true;
            yawLine.positionCount = 0;
            yawLine.startWidth = 0.025f;
            yawLine.endWidth = 0.01f;
            yawLine.enabled = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text feedback = ConfigureText("ContactFeedback", hudParent, new Vector2(24f, -326f), font, 18, TextAnchor.UpperLeft);
            feedback.rectTransform.sizeDelta = new Vector2(560f, 120f);
            feedback.enabled = false;

            ContactPointIndicator indicator = EnsureComponent<ContactPointIndicator>(indicatorObject);
            indicator.Configure(markerObject.transform, feedback, yawLine);
            return indicator;
        }

        private static ShotUncertaintyPreview ConfigureShotUncertaintyPreview(
            Transform parent,
            Transform hudParent,
            Material lineMaterial,
            ContactPointSelector contactSelector,
            ShotVarianceController shotVarianceController)
        {
            GameObject previewObject = GetOrCreateChild("ShotUncertaintyPreview", parent);

            LineRenderer left = ConfigurePreviewLine("UncertaintyConeLeft", previewObject.transform, lineMaterial);
            LineRenderer right = ConfigurePreviewLine("UncertaintyConeRight", previewObject.transform, lineMaterial);
            LineRenderer jitter = ConfigurePreviewLine("ContactJitterRadius", previewObject.transform, lineMaterial);
            jitter.loop = true;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text label = ConfigureText("UncertaintyPreviewText", hudParent, new Vector2(24f, -454f), font, 18, TextAnchor.UpperLeft);
            label.rectTransform.sizeDelta = new Vector2(560f, 132f);
            label.enabled = false;

            ShotUncertaintyPreview preview = EnsureComponent<ShotUncertaintyPreview>(previewObject);
            preview.Configure(left, right, jitter, label, contactSelector, shotVarianceController);
            return preview;
        }

        private static LineRenderer ConfigurePreviewLine(string name, Transform parent, Material material)
        {
            GameObject lineObject = GetOrCreateChild(name, parent);
            LineRenderer line = EnsureComponent<LineRenderer>(lineObject);
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.startWidth = 0.018f;
            line.endWidth = 0.008f;
            line.enabled = false;
            return line;
        }

        private static PrecisionTargetZone ConfigurePrecisionTargetZone(Transform parent, Material material)
        {
            GameObject zoneObject = GetOrCreateChild("PrecisionTargetZone", parent);
            GameObject visual = GetOrCreatePrimitive("PrecisionTargetVisual", PrimitiveType.Cube, zoneObject.transform);
            SetMaterial(visual, material);
            if (visual.TryGetComponent(out Collider collider))
            {
                Object.DestroyImmediate(collider);
            }

            PrecisionTargetZone zone = EnsureComponent<PrecisionTargetZone>(zoneObject);
            zone.Configure(visual.transform);
            return zone;
        }

        private static RoguelikeDebugOverlay ConfigureRoguelikeDebugOverlay(
            Transform hudParent,
            RunController runController,
            ShotVarianceController shotVarianceController,
            OpponentTurnController opponentTurnController,
            TableSurfaceApplier surfaceApplier,
            ObstacleLayoutController obstacleLayoutController)
        {
            GameObject overlayObject = GetOrCreateUiChild("RoguelikeDebugOverlay", hudParent);
            RectTransform overlayRect = EnsureComponent<RectTransform>(overlayObject);
            overlayRect.anchorMin = new Vector2(1f, 0f);
            overlayRect.anchorMax = new Vector2(1f, 0f);
            overlayRect.pivot = new Vector2(1f, 0f);
            overlayRect.anchoredPosition = new Vector2(-24f, 322f);
            overlayRect.sizeDelta = new Vector2(620f, 280f);

            GameObject textObject = GetOrCreateUiChild("RoguelikeDebugText", overlayObject.transform);
            Text text = EnsureComponent<Text>(textObject);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 15;
            text.color = new Color(0.75f, 1f, 0.82f);
            text.alignment = TextAnchor.LowerRight;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform textRect = EnsureComponent<RectTransform>(textObject);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(1f, 0f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;

            RoguelikeDebugOverlay overlay = EnsureComponent<RoguelikeDebugOverlay>(overlayObject);
            overlay.Configure(runController, shotVarianceController, opponentTurnController, surfaceApplier, obstacleLayoutController, text, true);
            return overlay;
        }

        private static RunProgressionUiController ConfigureRunUi(Transform parent)
        {
            GameObject canvasObject = GetOrCreateUiChild("RunCanvas", parent);
            Canvas canvas = EnsureComponent<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureComponent<GraphicRaycaster>(canvasObject);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject startPanel = ConfigurePanel("RunStartPanel", canvasObject.transform, new Vector2(0f, 0f), new Vector2(760f, 420f), new Color(0.05f, 0.07f, 0.08f, 0.92f));
            Text startTitle = ConfigurePanelText("RunStartTitle", startPanel.transform, new Vector2(0f, -42f), "Roguelike Run", font, 38, TextAnchor.MiddleCenter, new Vector2(680f, 56f));
            InputField seedInput = ConfigureInputField("SeedInput", startPanel.transform, new Vector2(0f, -120f), "12345", font);
            Button randomSeed = ConfigureButton("RandomSeedButton", startPanel.transform, new Vector2(-190f, -205f), "Random Seed", font, new Color(0.22f, 0.34f, 0.42f));
            Button startRun = ConfigureButton("StartRunButton", startPanel.transform, new Vector2(190f, -205f), "Start Run", font, new Color(0.1f, 0.55f, 0.38f));
            Button returnLocal = ConfigureButton("ReturnLocalButton", startPanel.transform, new Vector2(0f, -305f), "Return To Local Match", font, new Color(0.24f, 0.28f, 0.32f));
            startTitle.raycastTarget = false;

            GameObject introPanel = ConfigurePanel("EncounterIntroPanel", canvasObject.transform, new Vector2(0f, 0f), new Vector2(840f, 500f), new Color(0.05f, 0.07f, 0.08f, 0.92f));
            Text introText = ConfigurePanelText("EncounterIntroText", introPanel.transform, new Vector2(0f, -42f), string.Empty, font, 26, TextAnchor.UpperLeft, new Vector2(720f, 320f));
            Button continueButton = ConfigureButton("ContinueEncounterButton", introPanel.transform, new Vector2(0f, -390f), "Continue", font, new Color(0.1f, 0.55f, 0.38f));

            GameObject activePanel = ConfigurePanel("ActiveRunPanel", canvasObject.transform, new Vector2(0f, -12f), new Vector2(600f, 280f), new Color(0.04f, 0.055f, 0.06f, 0.72f));
            RectTransform activeRect = EnsureComponent<RectTransform>(activePanel);
            activeRect.anchorMin = new Vector2(1f, 1f);
            activeRect.anchorMax = new Vector2(1f, 1f);
            activeRect.pivot = new Vector2(1f, 1f);
            activeRect.anchoredPosition = new Vector2(-24f, -24f);
            Text activeText = ConfigurePanelText("ActiveRunText", activePanel.transform, new Vector2(0f, -18f), string.Empty, font, 18, TextAnchor.UpperLeft, new Vector2(536f, 236f));

            GameObject rewardPanel = ConfigurePanel("RewardSelectionPanel", canvasObject.transform, new Vector2(0f, 0f), new Vector2(1100f, 520f), new Color(0.05f, 0.07f, 0.08f, 0.94f));
            Text rewardHeader = ConfigurePanelText("RewardHeader", rewardPanel.transform, new Vector2(0f, -34f), "Choose one upgrade", font, 26, TextAnchor.MiddleCenter, new Vector2(980f, 72f));
            Button[] rewardButtons = new Button[3];
            Text[] rewardTexts = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float x = -350f + i * 350f;
                rewardButtons[i] = ConfigureButton($"RewardChoice{i + 1}", rewardPanel.transform, new Vector2(x, -260f), string.Empty, font, new Color(0.12f, 0.18f, 0.22f));
                RectTransform buttonRect = EnsureComponent<RectTransform>(rewardButtons[i].gameObject);
                buttonRect.sizeDelta = new Vector2(310f, 330f);
                rewardTexts[i] = rewardButtons[i].GetComponentInChildren<Text>();
                rewardTexts[i].alignment = TextAnchor.UpperLeft;
                rewardTexts[i].fontSize = 19;
            }

            GameObject summaryPanel = ConfigurePanel("RunSummaryPanel", canvasObject.transform, new Vector2(0f, 0f), new Vector2(860f, 620f), new Color(0.05f, 0.07f, 0.08f, 0.94f));
            Text summaryText = ConfigurePanelText("RunSummaryText", summaryPanel.transform, new Vector2(0f, -36f), string.Empty, font, 24, TextAnchor.UpperLeft, new Vector2(740f, 400f));
            Button restartSame = ConfigureButton("RestartSameSeedButton", summaryPanel.transform, new Vector2(-250f, -500f), "Restart Seed", font, new Color(0.1f, 0.45f, 0.55f));
            Button newSeed = ConfigureButton("NewSeedButton", summaryPanel.transform, new Vector2(0f, -500f), "New Seed", font, new Color(0.1f, 0.55f, 0.38f));
            Button summaryLocal = ConfigureButton("SummaryLocalButton", summaryPanel.transform, new Vector2(250f, -500f), "Local Match", font, new Color(0.24f, 0.28f, 0.32f));

            RunProgressionUiController ui = EnsureComponent<RunProgressionUiController>(canvasObject);
            ui.Configure(
                startPanel,
                seedInput,
                randomSeed,
                startRun,
                returnLocal,
                introPanel,
                introText,
                continueButton,
                activePanel,
                activeText,
                rewardPanel,
                rewardHeader,
                rewardButtons,
                rewardTexts,
                summaryPanel,
                summaryText,
                restartSame,
                newSeed,
                summaryLocal);
            ui.HideRunPanels();
            return ui;
        }

        private static GameObject ConfigurePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject panel = GetOrCreateUiChild(name, parent);
            RectTransform rect = EnsureComponent<RectTransform>(panel);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image image = EnsureComponent<Image>(panel);
            image.color = color;
            return panel;
        }

        private static Text ConfigurePanelText(string name, Transform parent, Vector2 anchoredPosition, string value, Font font, int fontSize, TextAnchor alignment, Vector2 size)
        {
            Text text = ConfigureText(name, parent, anchoredPosition, font, fontSize, alignment);
            text.text = value;
            text.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            text.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            text.rectTransform.sizeDelta = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static InputField ConfigureInputField(string name, Transform parent, Vector2 anchoredPosition, string value, Font font)
        {
            GameObject inputObject = GetOrCreateUiChild(name, parent);
            RectTransform rect = EnsureComponent<RectTransform>(inputObject);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(460f, 62f);
            Image image = EnsureComponent<Image>(inputObject);
            image.color = new Color(0.9f, 0.94f, 0.95f, 0.96f);

            InputField input = EnsureComponent<InputField>(inputObject);
            Text text = ConfigureText($"{name}Text", inputObject.transform, Vector2.zero, font, 26, TextAnchor.MiddleLeft);
            text.color = Color.black;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(18f, 0f);
            text.rectTransform.offsetMax = new Vector2(-18f, 0f);
            Text placeholder = ConfigureText($"{name}Placeholder", inputObject.transform, Vector2.zero, font, 24, TextAnchor.MiddleLeft);
            placeholder.color = new Color(0f, 0f, 0f, 0.45f);
            placeholder.text = "numeric seed";
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = new Vector2(18f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-18f, 0f);
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = value;
            input.contentType = InputField.ContentType.IntegerNumber;
            return input;
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

            Button startPrototype = ConfigureButton("StartPrototypeButton", canvasObject.transform, new Vector2(0f, -310f), "Local Match", font, new Color(0.08f, 0.55f, 0.72f));
            Button startRun = ConfigureButton("StartRunButton", canvasObject.transform, new Vector2(0f, -405f), "Roguelike Run", font, new Color(0.1f, 0.55f, 0.38f));
            Button quit = ConfigureButton("QuitButton", canvasObject.transform, new Vector2(0f, -500f), "Quit", font, new Color(0.36f, 0.22f, 0.24f));
            Button legacyMenu = ConfigureButton("LegacyMenuButton", canvasObject.transform, new Vector2(-240f, -610f), "Existing Menu", font, new Color(0.24f, 0.28f, 0.32f));
            Button legacyTable = ConfigureButton("LegacyTableButton", canvasObject.transform, new Vector2(240f, -610f), "Existing Table", font, new Color(0.24f, 0.28f, 0.32f));

            PrototypeMenuController controller = EnsureComponent<PrototypeMenuController>(canvasObject);
            controller.Configure(startPrototype, startRun, quit, legacyMenu, legacyTable, "PaperFootballGame", "MainMenu", "TableScene");

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

        private static Button ConfigureCompactButton(string name, Transform parent, Vector2 anchoredPosition, string label, Font font, Color color)
        {
            GameObject buttonObject = GetOrCreateUiChild(name, parent);
            RectTransform rect = EnsureComponent<RectTransform>(buttonObject);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(160f, 48f);

            Image image = EnsureComponent<Image>(buttonObject);
            image.color = color;
            Button button = EnsureComponent<Button>(buttonObject);

            Text text = ConfigureText($"{name}Text", buttonObject.transform, Vector2.zero, font, 20, TextAnchor.MiddleCenter);
            text.text = label;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchoredPosition = Vector2.zero;
            text.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }

        private static GameObject ConfigureFootballSpinReferencePart(
            Transform football,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject part = GetOrCreatePrimitive(name, PrimitiveType.Cube, football);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;
            SetMaterial(part, material);

            if (part.TryGetComponent(out Collider collider))
            {
                Object.DestroyImmediate(collider);
            }

            return part;
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
            RemoveMissingMonoBehaviours(target);

            if (target.TryGetComponent(out T component))
            {
                return component;
            }

            return target.AddComponent<T>();
        }

        private static void RemoveMissingMonoBehaviours(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
        }

        private static void RemoveComponent<T>(GameObject target) where T : Component
        {
            if (target.TryGetComponent(out T component))
            {
                Object.DestroyImmediate(component);
            }
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
