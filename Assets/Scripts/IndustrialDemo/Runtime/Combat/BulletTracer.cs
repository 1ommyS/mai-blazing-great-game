using UnityEngine;

namespace IndustrialDemo.Combat
{
    public class BulletTracer : MonoBehaviour
    {
        private static Material s_sharedMaterial;

        private LineRenderer _lineRenderer;
        private Color _baseColor;
        private float _duration;
        private float _spawnTime;

        public static void Spawn(Vector3 start, Vector3 end, Color color, float width = 0.025f, float duration = 0.06f)
        {
            GameObject go = new("BulletTracer");
            BulletTracer tracer = go.AddComponent<BulletTracer>();
            tracer.Initialize(start, end, color, width, duration);
        }

        private void Initialize(Vector3 start, Vector3 end, Color color, float width, float duration)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.sharedMaterial = GetSharedMaterial();
            _lineRenderer.textureMode = LineTextureMode.Stretch;
            _lineRenderer.alignment = LineAlignment.View;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
            _lineRenderer.numCapVertices = 2;
            _lineRenderer.widthMultiplier = width;
            _lineRenderer.SetPosition(0, start);
            _lineRenderer.SetPosition(1, end);

            _baseColor = color;
            _duration = Mathf.Max(0.01f, duration);
            _spawnTime = Time.time;
            SetColor(color);
        }

        private void Update()
        {
            if (_lineRenderer == null)
            {
                Destroy(gameObject);
                return;
            }

            float t = Mathf.Clamp01((Time.time - _spawnTime) / _duration);
            Color color = _baseColor;
            color.a *= 1f - t;
            SetColor(color);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void SetColor(Color color)
        {
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = color;
        }

        private static Material GetSharedMaterial()
        {
            if (s_sharedMaterial != null)
            {
                return s_sharedMaterial;
            }

            Shader shader = Shader.Find("HDRP/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            s_sharedMaterial = new Material(shader)
            {
                color = Color.white
            };

            if (s_sharedMaterial.HasProperty("_BaseColor"))
            {
                s_sharedMaterial.SetColor("_BaseColor", Color.white);
            }

            if (s_sharedMaterial.HasProperty("_UnlitColor"))
            {
                s_sharedMaterial.SetColor("_UnlitColor", Color.white);
            }

            return s_sharedMaterial;
        }
    }
}
