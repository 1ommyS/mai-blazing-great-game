using System.Collections;
using IndustrialDemo.Combat;
using UnityEngine;

namespace IndustrialDemo.Actors
{
    public class EnemyPresentationTarget : MonoBehaviour, IShotDamageReceiver
    {
        [SerializeField, Tooltip("Readable archetype label used by tooling and inspector.")]
        private string archetypeId = "Guard";

        [SerializeField, Min(1f), Tooltip("Presentation-only health pool for this demo enemy.")]
        private float maxHealth = 90f;

        [SerializeField, Tooltip("Optional renderers flashed when the enemy takes a hit.")]
        private Renderer[] hitFlashRenderers;

        [SerializeField, Tooltip("Optional object enabled after this enemy is neutralized.")]
        private GameObject deathStateRoot;

        [SerializeField, Tooltip("If enabled, the live renderers are disabled after health reaches zero.")]
        private bool hideLiveRenderersOnDeath = true;

        [SerializeField, Min(0.01f), Tooltip("Duration of the brief hit flash.")]
        private float hitFlashDuration = 0.08f;

        [SerializeField, ColorUsage(false, true), Tooltip("Tint used for the hit flash pulse.")]
        private Color hitFlashColor = new(1.3f, 0.45f, 0.35f, 1f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Renderer[] _cachedRenderers;
        private Color[] _baseColors;
        private float _currentHealth;
        private bool _isDead;
        private Coroutine _flashRoutine;

        public string ArchetypeId => archetypeId;
        public bool IsDead => _isDead;

        private void Awake()
        {
            CacheRenderers();
            ResetState();
        }

        private void OnEnable()
        {
            if (_cachedRenderers == null || _cachedRenderers.Length == 0)
            {
                CacheRenderers();
            }

            if (_currentHealth <= 0f && !_isDead)
            {
                ResetState();
            }
        }

        public void ReceiveShotDamage(ShotImpactContext context)
        {
            if (_isDead)
            {
                return;
            }

            _currentHealth -= Mathf.Max(0f, context.Damage);
            TriggerFlash();

            if (_currentHealth > 0f)
            {
                return;
            }

            _isDead = true;
            SetLiveRenderersEnabled(!hideLiveRenderersOnDeath);

            Collider[] colliders = GetComponentsInChildren<Collider>(includeInactive: true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }

            if (deathStateRoot != null)
            {
                deathStateRoot.SetActive(true);
            }
        }

        public void ResetState()
        {
            _currentHealth = maxHealth;
            _isDead = false;

            if (deathStateRoot != null)
            {
                deathStateRoot.SetActive(false);
            }

            SetLiveRenderersEnabled(true);

            Collider[] colliders = GetComponentsInChildren<Collider>(includeInactive: true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = true;
            }

            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
                animator.Rebind();
            }
        }

        private void CacheRenderers()
        {
            _cachedRenderers = hitFlashRenderers != null && hitFlashRenderers.Length > 0
                ? hitFlashRenderers
                : GetComponentsInChildren<Renderer>(includeInactive: true);

            _baseColors = new Color[_cachedRenderers.Length];
            for (int i = 0; i < _cachedRenderers.Length; i++)
            {
                Material material = _cachedRenderers[i] != null ? _cachedRenderers[i].sharedMaterial : null;
                _baseColors[i] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : Color.white;
            }
        }

        private void TriggerFlash()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            SetFlashColor(hitFlashColor);
            yield return new WaitForSeconds(hitFlashDuration);
            RestoreColors();
            _flashRoutine = null;
        }

        private void SetFlashColor(Color targetColor)
        {
            for (int i = 0; i < _cachedRenderers.Length; i++)
            {
                Renderer cachedRenderer = _cachedRenderers[i];
                if (cachedRenderer == null)
                {
                    continue;
                }

                Material material = cachedRenderer.material;
                if (material != null && material.HasProperty(BaseColorId))
                {
                    material.SetColor(BaseColorId, targetColor);
                }
            }
        }

        private void RestoreColors()
        {
            for (int i = 0; i < _cachedRenderers.Length; i++)
            {
                Renderer cachedRenderer = _cachedRenderers[i];
                if (cachedRenderer == null)
                {
                    continue;
                }

                Material material = cachedRenderer.material;
                if (material != null && material.HasProperty(BaseColorId))
                {
                    material.SetColor(BaseColorId, _baseColors[i]);
                }
            }
        }

        private void SetLiveRenderersEnabled(bool enabled)
        {
            for (int i = 0; i < _cachedRenderers.Length; i++)
            {
                if (_cachedRenderers[i] != null)
                {
                    _cachedRenderers[i].enabled = enabled;
                }
            }
        }
    }
}
