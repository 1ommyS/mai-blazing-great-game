using UnityEngine;

namespace IndustrialDemo.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class DemoPlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1f), Tooltip("Presentation-only health pool for the demo player.")]
        private float maxHealth = 100f;

        [SerializeField, Min(0.01f), Tooltip("How long the red damage flash remains visible.")]
        private float hitFlashDuration = 0.18f;

        [SerializeField, ColorUsage(false, true), Tooltip("Tint used for the damage flash overlay.")]
        private Color hitFlashColor = new(1f, 0.15f, 0.1f, 0.18f);

        private CharacterController _characterController;
        private float _currentHealth;
        private float _hitFlashUntil;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Texture2D _overlayTexture;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _currentHealth = maxHealth;
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
        }

        private void OnDestroy()
        {
            if (_overlayTexture != null)
            {
                Destroy(_overlayTexture);
            }
        }

        public void ApplyDamage(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            _hitFlashUntil = Time.time + hitFlashDuration;

            if (_currentHealth <= 0f)
            {
                Respawn();
            }
        }

        private void Respawn()
        {
            _currentHealth = maxHealth;

            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);

            if (_characterController != null)
            {
                _characterController.enabled = true;
            }
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (Time.time < _hitFlashUntil)
            {
                EnsureOverlayTexture();
                Color previous = GUI.color;
                GUI.color = hitFlashColor;
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _overlayTexture);
                GUI.color = previous;
            }

            GUI.Label(new Rect(18f, 16f, 180f, 24f), $"HP: {Mathf.CeilToInt(_currentHealth)}");
        }

        private void EnsureOverlayTexture()
        {
            if (_overlayTexture != null)
            {
                return;
            }

            _overlayTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _overlayTexture.SetPixel(0, 0, Color.white);
            _overlayTexture.Apply();
        }
    }
}
