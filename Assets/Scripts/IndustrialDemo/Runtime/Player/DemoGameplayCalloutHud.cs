using UnityEngine;

namespace IndustrialDemo.Player
{
    public class DemoGameplayCalloutHud : MonoBehaviour
    {
        private static DemoGameplayCalloutHud _instance;

        [SerializeField, Min(0.5f)]
        private float defaultDuration = 5f;

        private string _title = string.Empty;
        private string _body = string.Empty;
        private float _visibleUntil;
        private Color _accentColor = new(0.92f, 0.78f, 0.32f, 1f);
        private GUIStyle _boxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;

        private void OnEnable()
        {
            _instance = this;
        }

        private void OnDisable()
        {
            if (ReferenceEquals(_instance, this))
            {
                _instance = null;
            }
        }

        public static void Push(string title, string body, float duration, Color accentColor)
        {
            if (_instance == null)
            {
                return;
            }

            _instance.ShowInternal(title, body, duration, accentColor);
        }

        private void ShowInternal(string title, string body, float duration, Color accentColor)
        {
            _title = title ?? string.Empty;
            _body = body ?? string.Empty;
            _accentColor = accentColor;
            _visibleUntil = Time.time + Mathf.Max(0.5f, duration > 0f ? duration : defaultDuration);
        }

        private void OnGUI()
        {
            if (Time.time > _visibleUntil || string.IsNullOrEmpty(_title))
            {
                return;
            }

            EnsureStyles();

            Rect rect = new(Screen.width * 0.5f - 250f, 28f, 500f, 76f);
            Color previous = GUI.color;
            GUI.color = new Color(0.06f, 0.08f, 0.11f, 0.94f);
            GUI.Box(rect, GUIContent.none, _boxStyle);

            GUI.color = _accentColor;
            GUI.DrawTexture(new Rect(rect.x, rect.y, 6f, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;

            GUI.Label(new Rect(rect.x + 18f, rect.y + 10f, rect.width - 30f, 24f), _title, _titleStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 36f, rect.width - 30f, 30f), _body, _bodyStyle);
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null)
            {
                return;
            }

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = Texture2D.whiteTexture;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            _titleStyle.normal.textColor = Color.white;

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            _bodyStyle.normal.textColor = new Color(0.88f, 0.9f, 0.94f, 1f);
        }
    }
}
