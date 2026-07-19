using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Encounters
{
    public class TableSurfaceApplier : MonoBehaviour
    {
        [SerializeField] private Collider tableCollider;
        [SerializeField] private Renderer tableRenderer;
        [SerializeField] private TableSurfaceDefinition currentSurface;

        private PhysicsMaterial baselineMaterial;
        private Material baselineRenderMaterial;

        public TableSurfaceDefinition CurrentSurface => currentSurface;

        public void Configure(Collider table, Renderer rendererReference)
        {
            tableCollider = table;
            tableRenderer = rendererReference;
            baselineMaterial = tableCollider != null ? tableCollider.sharedMaterial : null;
            baselineRenderMaterial = tableRenderer != null ? tableRenderer.sharedMaterial : null;
        }

        public void Apply(TableSurfaceDefinition surface)
        {
            currentSurface = surface;
            if (tableCollider != null)
            {
                tableCollider.material = surface != null ? surface.CreateRuntimePhysicsMaterial() : baselineMaterial;
            }

            if (tableRenderer != null && surface != null)
            {
                Material runtimeMaterial = tableRenderer.material;
                if (runtimeMaterial.HasProperty("_BaseColor"))
                {
                    runtimeMaterial.SetColor("_BaseColor", surface.DebugColor);
                }
                else if (runtimeMaterial.HasProperty("_Color"))
                {
                    runtimeMaterial.SetColor("_Color", surface.DebugColor);
                }
            }
            else if (tableRenderer != null && baselineRenderMaterial != null)
            {
                tableRenderer.sharedMaterial = baselineRenderMaterial;
            }
        }
    }
}
