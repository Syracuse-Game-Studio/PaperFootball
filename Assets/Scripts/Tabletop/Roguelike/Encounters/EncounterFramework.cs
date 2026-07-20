using System;
using System.Collections.Generic;
using System.Linq;
using PaperFootball.Tabletop.Roguelike.Opponents;
using PaperFootball.Tabletop.Roguelike.Random;
using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Encounters
{
    public enum EncounterType
    {
        StandardMatch,
        PrecisionDrill,
        EliteMatch,
        BossMatch
    }

    public enum TableSurfaceKind
    {
        NormalDesk,
        SlipperyDesk,
        RoughDesk,
        ScienceLabTable
    }

    public enum ObstacleLayoutKind
    {
        None,
        Pencil,
        Eraser,
        Book,
        Mixed
    }

    public enum ObstacleKind
    {
        Pencil,
        Eraser,
        Book
    }

    [CreateAssetMenu(menuName = "Paper Football/Roguelike/Table Surface", fileName = "TableSurface")]
    public partial class TableSurfaceDefinition
    {
        [SerializeField] private string stableId = "normal_desk";
        [SerializeField] private string displayName = "Normal Desk";
        [SerializeField] private TableSurfaceKind kind;
        [SerializeField] private float dynamicFriction = 0.55f;
        [SerializeField] private float staticFriction = 0.65f;
        [SerializeField] private float bounciness = 0.04f;
        [SerializeField] private float linearDampingMultiplier = 1f;
        [SerializeField] private Color debugColor = new(0.42f, 0.26f, 0.13f);

        public string StableId => string.IsNullOrWhiteSpace(stableId) ? name : stableId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? StableId : displayName;
        public TableSurfaceKind Kind => kind;
        public float DynamicFriction => Mathf.Max(0f, dynamicFriction);
        public float StaticFriction => Mathf.Max(0f, staticFriction);
        public float Bounciness => Mathf.Clamp01(bounciness);
        public float LinearDampingMultiplier => Mathf.Max(0.05f, linearDampingMultiplier);
        public Color DebugColor => debugColor;

        public void Configure(string id, string surfaceName, TableSurfaceKind surfaceKind, float dynamic, float stat, float bounce, float dampingMultiplier, Color color)
        {
            stableId = id;
            displayName = surfaceName;
            kind = surfaceKind;
            dynamicFriction = Mathf.Max(0f, dynamic);
            staticFriction = Mathf.Max(0f, stat);
            bounciness = Mathf.Clamp01(bounce);
            linearDampingMultiplier = Mathf.Max(0.05f, dampingMultiplier);
            debugColor = color;
        }

        public PhysicsMaterial CreateRuntimePhysicsMaterial()
        {
            PhysicsMaterial material = new($"{DisplayName} Runtime Physics")
            {
                dynamicFriction = DynamicFriction,
                staticFriction = StaticFriction,
                bounciness = Bounciness,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            return material;
        }
    }

    [CreateAssetMenu(menuName = "Paper Football/Roguelike/Table Surface Catalog", fileName = "TableSurfaceCatalog")]
    public partial class TableSurfaceCatalog
    {
        [SerializeField] private TableSurfaceDefinition[] surfaces = Array.Empty<TableSurfaceDefinition>();

        public IReadOnlyList<TableSurfaceDefinition> Surfaces => surfaces ?? Array.Empty<TableSurfaceDefinition>();

        public void Configure(TableSurfaceDefinition[] definitions)
        {
            surfaces = definitions ?? Array.Empty<TableSurfaceDefinition>();
        }

        public TableSurfaceDefinition GetById(string stableId)
        {
            return Surfaces.FirstOrDefault(surface => surface != null && surface.StableId == stableId);
        }
    }

    [Serializable]
    public sealed class ObstacleSpawn
    {
        public ObstacleKind kind;
        public Vector3 position;
        public Vector3 scale = Vector3.one;
        public Vector3 eulerAngles;

        public Bounds ToBounds()
        {
            return new Bounds(position, scale);
        }
    }

    [CreateAssetMenu(menuName = "Paper Football/Roguelike/Obstacle Layout", fileName = "ObstacleLayout")]
    public partial class ObstacleLayoutDefinition
    {
        [SerializeField] private string stableId = "no_obstacles";
        [SerializeField] private string displayName = "No Obstacles";
        [SerializeField] private ObstacleLayoutKind kind;
        [SerializeField] private ObstacleSpawn[] obstacles = Array.Empty<ObstacleSpawn>();

        public string StableId => string.IsNullOrWhiteSpace(stableId) ? name : stableId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? StableId : displayName;
        public ObstacleLayoutKind Kind => kind;
        public IReadOnlyList<ObstacleSpawn> Obstacles => obstacles ?? Array.Empty<ObstacleSpawn>();

        public void Configure(string id, string layoutName, ObstacleLayoutKind layoutKind, ObstacleSpawn[] spawns)
        {
            stableId = id;
            displayName = layoutName;
            kind = layoutKind;
            obstacles = spawns ?? Array.Empty<ObstacleSpawn>();
        }

        public bool OverlapsForbiddenAreas(IEnumerable<Bounds> forbiddenAreas)
        {
            if (forbiddenAreas == null)
            {
                return false;
            }

            Bounds[] forbidden = forbiddenAreas.ToArray();
            return Obstacles.Any(obstacle => forbidden.Any(area => area.Intersects(obstacle.ToBounds())));
        }
    }

    [CreateAssetMenu(menuName = "Paper Football/Roguelike/Obstacle Layout Catalog", fileName = "ObstacleLayoutCatalog")]
    public partial class ObstacleLayoutCatalog
    {
        [SerializeField] private ObstacleLayoutDefinition[] layouts = Array.Empty<ObstacleLayoutDefinition>();

        public IReadOnlyList<ObstacleLayoutDefinition> Layouts => layouts ?? Array.Empty<ObstacleLayoutDefinition>();

        public void Configure(ObstacleLayoutDefinition[] definitions)
        {
            layouts = definitions ?? Array.Empty<ObstacleLayoutDefinition>();
        }

        public ObstacleLayoutDefinition GetById(string stableId)
        {
            return Layouts.FirstOrDefault(layout => layout != null && layout.StableId == stableId);
        }
    }

    [Serializable]
    public sealed class GeneratedEncounter
    {
        public string encounterId;
        public EncounterType encounterType;
        public int stageIndex;
        public string opponentId;
        public string surfaceId;
        public string obstacleLayoutId;
        public string specialRule;
        public bool rewardEligible;
        public int difficultyRating;
        public int seed;
        public string displayTitle;
        public string description;
        public int targetScore = 6;
        public int maximumPossessions = 6;
        public int precisionAttemptLimit = 3;
        public bool guaranteedUncommonReward;
        public bool isBoss;
        public Vector3 precisionTargetCenter = new(0f, 0.2f, 3.25f);
        public Vector3 precisionTargetSize = new(1.1f, 0.35f, 1.1f);
    }

    public static class EncounterGenerator
    {
        public static List<GeneratedEncounter> GenerateSixEncounterRun(
            int runSeed,
            OpponentCatalog opponentCatalog,
            TableSurfaceCatalog surfaceCatalog,
            ObstacleLayoutCatalog obstacleCatalog)
        {
            List<GeneratedEncounter> encounters = new();
            encounters.Add(Build(runSeed, 0, EncounterType.StandardMatch, "power_flicker", "normal_desk", "no_obstacles", "Opening drive", true, 1));
            encounters.Add(Build(runSeed, 1, EncounterType.PrecisionDrill, "calculator", "rough_desk", "pencil_lane", "Land inside the target zone in three flicks", true, 2));
            encounters.Add(Build(runSeed, 2, EncounterType.StandardMatch, "calculator", "normal_desk", "book_bank", "Conservative opponent", true, 2));
            encounters.Add(Build(runSeed, 3, EncounterType.StandardMatch, "power_flicker", "slippery_desk", "eraser_midfield", "Long travel surface", true, 3));
            encounters.Add(Build(runSeed, 4, EncounterType.EliteMatch, "spinner", "slippery_desk", "mixed_office", "Increased contact variance", true, 4, guaranteedUncommon: true));
            encounters.Add(Build(runSeed, 5, EncounterType.BossMatch, "spinner", "science_lab_table", "mixed_office", "Desk shake every third completed flick. Full yaw touchdowns score double.", false, 5, isBoss: true));

            ValidateCatalogReferences(encounters, opponentCatalog, surfaceCatalog, obstacleCatalog);
            return encounters;
        }

        private static GeneratedEncounter Build(
            int runSeed,
            int stageIndex,
            EncounterType type,
            string opponentId,
            string surfaceId,
            string obstacleLayoutId,
            string specialRule,
            bool rewardEligible,
            int difficulty,
            bool guaranteedUncommon = false,
            bool isBoss = false)
        {
            int seed = StableSeedUtility.DeriveSeed(runSeed, RunRandomStream.EncounterGeneration, stageIndex, stableIdentifier: type.ToString());
            string typeName = type switch
            {
                EncounterType.PrecisionDrill => "Precision Drill",
                EncounterType.EliteMatch => "Elite Match",
                EncounterType.BossMatch => "Science Lab Table",
                _ => "Standard Match"
            };

            return new GeneratedEncounter
            {
                encounterId = $"{stageIndex + 1:00}_{type.ToString().ToLowerInvariant()}_{seed}",
                encounterType = type,
                stageIndex = stageIndex,
                opponentId = opponentId,
                surfaceId = surfaceId,
                obstacleLayoutId = obstacleLayoutId,
                specialRule = specialRule,
                rewardEligible = rewardEligible,
                difficultyRating = difficulty,
                seed = seed,
                displayTitle = $"{stageIndex + 1}. {typeName}",
                description = BuildDescription(type, opponentId, surfaceId),
                targetScore = type == EncounterType.BossMatch ? 12 : 6,
                maximumPossessions = type == EncounterType.PrecisionDrill ? 0 : 6,
                precisionAttemptLimit = 3,
                guaranteedUncommonReward = guaranteedUncommon,
                isBoss = isBoss
            };
        }

        private static string BuildDescription(EncounterType type, string opponentId, string surfaceId)
        {
            return type switch
            {
                EncounterType.PrecisionDrill => "Stop the football inside the target zone after normal physics rest detection.",
                EncounterType.EliteMatch => $"An elite {opponentId} match on {surfaceId}.",
                EncounterType.BossMatch => "Final boss encounter with deterministic desk shakes.",
                _ => $"A short tabletop match against {opponentId} on {surfaceId}."
            };
        }

        private static void ValidateCatalogReferences(
            IEnumerable<GeneratedEncounter> encounters,
            OpponentCatalog opponentCatalog,
            TableSurfaceCatalog surfaceCatalog,
            ObstacleLayoutCatalog obstacleCatalog)
        {
            foreach (GeneratedEncounter encounter in encounters)
            {
                if (opponentCatalog != null && opponentCatalog.GetById(encounter.opponentId) == null)
                {
                    Debug.LogWarning($"Generated encounter references missing opponent '{encounter.opponentId}'.");
                }

                if (surfaceCatalog != null && surfaceCatalog.GetById(encounter.surfaceId) == null)
                {
                    Debug.LogWarning($"Generated encounter references missing surface '{encounter.surfaceId}'.");
                }

                if (obstacleCatalog != null && obstacleCatalog.GetById(encounter.obstacleLayoutId) == null)
                {
                    Debug.LogWarning($"Generated encounter references missing obstacle layout '{encounter.obstacleLayoutId}'.");
                }
            }
        }
    }

}
