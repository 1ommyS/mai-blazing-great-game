using UnityEngine;
using UnityEngine.Rendering;

namespace IndustrialDemo.Breaching
{
    public static class InteractionHighlightUtility
    {
        public static GameObject[] CreateOutlineObjects(Renderer[] sourceRenderers, Color color, string prefix)
        {
            if (sourceRenderers == null || sourceRenderers.Length == 0)
            {
                return System.Array.Empty<GameObject>();
            }

            GameObject[] outlineObjects = new GameObject[sourceRenderers.Length];
            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                outlineObjects[i] = CreateOutlineObject(sourceRenderers[i], color, $"{prefix}_{i}");
            }

            return outlineObjects;
        }

        private static GameObject CreateOutlineObject(Renderer sourceRenderer, Color color, string name)
        {
            if (sourceRenderer == null)
            {
                return null;
            }

            MeshRenderer meshRenderer = sourceRenderer as MeshRenderer;
            MeshFilter meshFilter = meshRenderer != null ? sourceRenderer.GetComponent<MeshFilter>() : null;
            SkinnedMeshRenderer skinnedRenderer = sourceRenderer as SkinnedMeshRenderer;

            if ((meshFilter == null || meshFilter.sharedMesh == null) &&
                (skinnedRenderer == null || skinnedRenderer.sharedMesh == null))
            {
                return null;
            }

            GameObject outline = new(name);
            outline.hideFlags = HideFlags.HideAndDontSave;
            outline.transform.SetParent(sourceRenderer.transform, false);
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localRotation = Quaternion.identity;
            outline.transform.localScale = Vector3.one * 1.06f;

            Material outlineMaterial = CreateOutlineMaterial(color);

            if (meshFilter != null && meshRenderer != null)
            {
                MeshFilter outlineFilter = outline.AddComponent<MeshFilter>();
                outlineFilter.sharedMesh = meshFilter.sharedMesh;

                MeshRenderer outlineRenderer = outline.AddComponent<MeshRenderer>();
                outlineRenderer.sharedMaterial = outlineMaterial;
                outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;
                outlineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }
            else if (skinnedRenderer != null)
            {
                SkinnedMeshRenderer outlineRenderer = outline.AddComponent<SkinnedMeshRenderer>();
                outlineRenderer.sharedMesh = skinnedRenderer.sharedMesh;
                outlineRenderer.rootBone = skinnedRenderer.rootBone;
                outlineRenderer.bones = skinnedRenderer.bones;
                outlineRenderer.sharedMaterial = outlineMaterial;
                outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;
                outlineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                outlineRenderer.updateWhenOffscreen = true;
                outlineRenderer.localBounds = skinnedRenderer.localBounds;
            }

            outline.SetActive(false);
            return outline;
        }

        private static Material CreateOutlineMaterial(Color color)
        {
            Shader shader = Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            Material material = new(shader)
            {
                enableInstancing = true
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }
    }
}
