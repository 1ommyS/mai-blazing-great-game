using UnityEngine;

namespace IndustrialDemo.Player
{
    [RequireComponent(typeof(BoxCollider))]
    public class DemoGameplayCalloutZone : MonoBehaviour
    {
        [SerializeField]
        private string title = "GAMEPLAY NOTE";

        [SerializeField, TextArea(2, 4)]
        private string body = "Use the highlighted mechanic in this section.";

        [SerializeField, Min(0.5f)]
        private float duration = 5f;

        [SerializeField]
        private bool triggerOnce = true;

        [SerializeField]
        private Color accentColor = new(0.92f, 0.78f, 0.32f, 1f);

        private bool _triggered;

        private void Reset()
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(4f, 2f, 4f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered && triggerOnce)
            {
                return;
            }

            DemoFirstPersonMotor playerMotor = other.GetComponent<DemoFirstPersonMotor>() ?? other.GetComponentInParent<DemoFirstPersonMotor>();
            if (playerMotor == null)
            {
                return;
            }

            _triggered = true;
            DemoGameplayCalloutHud.Push(title, body, duration, accentColor);
        }
    }
}
