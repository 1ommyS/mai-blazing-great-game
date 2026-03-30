using System.Collections;
using IndustrialDemo.Foam;
using IndustrialDemo.Core;
using UnityEngine;

namespace IndustrialDemo.Breaching
{
    public class BreachableEntry : MonoBehaviour, IInteractionHighlightTarget, IFoamHighlightTarget
    {
        [Header("State")]
        [SerializeField, Tooltip("Current state of this entry.")]
        private BreachableEntryState currentState = BreachableEntryState.Closed;

        [SerializeField, Tooltip("Type of entry this object represents.")]
        private BreachableEntryType entryType = BreachableEntryType.StandardDoor;

        [Header("Supported Actions")]
        [SerializeField, Tooltip("Allows quiet interaction-based breaching.")]
        private bool supportsManualBreach = true;

        [SerializeField, Tooltip("Allows shot-based breaching through vulnerable zones.")]
        private bool supportsShotBreach = true;

        [SerializeField, Tooltip("Allows loud forced entry.")]
        private bool supportsForcedBreach = true;

        [SerializeField, Tooltip("Allows linked service panels to bypass the lock.")]
        private bool supportsPanelBypass = true;

        [SerializeField, Tooltip("Allows future foam systems to block this entry.")]
        private bool supportsFoamBlock = true;

        [Header("Timings And Noise")]
        [SerializeField, Min(0.01f), Tooltip("Time needed to complete a manual breach.")]
        private float breachTimeManual = 1.8f;

        [SerializeField, Min(0.01f), Tooltip("Time needed to complete a forced breach.")]
        private float breachTimeForced = 0.45f;

        [SerializeField, Min(0f), Tooltip("Noise emitted by a manual breach.")]
        private float noiseManual = 1.5f;

        [SerializeField, Min(0f), Tooltip("Noise emitted by a forced breach.")]
        private float noiseForced = 8f;

        [SerializeField, Min(0f), Tooltip("Noise emitted when a shot breach succeeds.")]
        private float noiseShot = 6f;

        [Header("Motion")]
        [SerializeField, Tooltip("How this entry should open visually.")]
        private BreachMotionType motionType = BreachMotionType.Rotate;

        [SerializeField, Tooltip("Transform to animate. If empty, this object is animated.")]
        private Transform movingTransform;

        [SerializeField, Tooltip("Open angle used for rotating doors.")]
        private float openAngle = 95f;

        [SerializeField, Tooltip("Slide distance used for shutters or hatch-like entries.")]
        private float slideDistance = 2.2f;

        [SerializeField, Min(0.01f), Tooltip("Animation time for the open transition.")]
        private float openDuration = 0.45f;

        [Header("Links")]
        [SerializeField, Tooltip("Optional linked entry opened or unlocked by this one.")]
        private BreachableEntry linkedEntry;

        [SerializeField, Tooltip("Optional extra blocker disabled after successful breach.")]
        private GameObject linkedBlocker;

        [SerializeField, Tooltip("Optional visual lock object disabled when bypassed.")]
        private GameObject linkedLock;

        [Header("Interaction")]
        [SerializeField, Tooltip("Optional label shown to the player for this entry.")]
        private string interactionLabel;

        [SerializeField, Tooltip("Renderers highlighted when the player can interact with this entry.")]
        private Renderer[] highlightRenderers;

        [SerializeField, ColorUsage(false, true), Tooltip("Outline tint applied while the player is targeting this entry.")]
        private Color highlightColor = new(1f, 0.82f, 0.26f, 1f);

        [SerializeField, Tooltip("Offset used for the world marker above this entry.")]
        private Vector3 interactionIndicatorOffset = new(0f, 0.45f, 0f);

        private Coroutine _breachRoutine;
        private Coroutine _animationRoutine;
        private Quaternion _closedLocalRotation;
        private Vector3 _closedLocalPosition;
        private BreachableEntryState _stateBeforeFoamBlock = BreachableEntryState.Closed;
        private GameObject[] _outlineObjects;

        public BreachableEntryState CurrentState => currentState;
        public BreachableEntryType EntryType => entryType;
        public bool SupportsManualBreach => supportsManualBreach;
        public bool SupportsForcedBreach => supportsForcedBreach;
        public bool SupportsShotBreach => supportsShotBreach;
        public bool SupportsPanelBypass => supportsPanelBypass;
        public bool SupportsFoamBlock => supportsFoamBlock;
        public float BreachTimeManual => breachTimeManual;
        public float BreachTimeForced => breachTimeForced;
        public string InteractionLabel => string.IsNullOrWhiteSpace(interactionLabel) ? GetDefaultInteractionLabel() : interactionLabel;
        public Color InteractionIndicatorColor => highlightColor;
        public string FoamActionLabel => "BLOCK ENTRY";
        public string FoamPurposeLabel => "Foam jams this route and buys time.";
        public Color FoamHighlightColor => highlightColor;

        public bool IsInteractable =>
            currentState != BreachableEntryState.Breached &&
            currentState != BreachableEntryState.PeekOpen &&
            currentState != BreachableEntryState.Jammed &&
            currentState != BreachableEntryState.Sealed &&
            currentState != BreachableEntryState.FoamBlocked;

        public string CurrentPrompt
        {
            get
            {
                if (currentState == BreachableEntryState.FoamBlocked)
                {
                    return "Foam blocked";
                }

                if (currentState == BreachableEntryState.Jammed)
                {
                    return "Jammed";
                }

                if (currentState == BreachableEntryState.Sealed)
                {
                    return "Sealed";
                }

                return entryType.ToString();
            }
        }

        private void Awake()
        {
            if (movingTransform == null)
            {
                movingTransform = transform;
            }

            _closedLocalRotation = movingTransform.localRotation;
            _closedLocalPosition = movingTransform.localPosition;
            CacheHighlightMaterials();
        }

        private void OnDisable()
        {
            CancelBreachRoutine();
            CancelAnimationRoutine();
            SetInteractionHighlight(false);
        }

        public bool CanManualBreach() => supportsManualBreach && IsInteractable;
        public bool CanForcedBreach() => supportsForcedBreach && IsInteractable;
        public bool CanPanelBypass() => supportsPanelBypass && IsInteractable;
        public bool CanShotBreach() => supportsShotBreach && IsInteractable;
        public bool CanFoamBlock() =>
            supportsFoamBlock &&
            currentState != BreachableEntryState.Sealed &&
            currentState != BreachableEntryState.FoamBlocked;

        public bool TryBeginManualBreach(MonoBehaviour runner)
        {
            if (!CanManualBreach() || runner == null || !isActiveAndEnabled)
            {
                return false;
            }

            CancelBreachRoutine();
            _breachRoutine = StartCoroutine(DoManualBreach());
            return true;
        }

        public bool TryBeginForcedBreach(MonoBehaviour runner)
        {
            if (!CanForcedBreach() || runner == null || !isActiveAndEnabled)
            {
                return false;
            }

            CancelBreachRoutine();
            _breachRoutine = StartCoroutine(DoForcedBreach());
            return true;
        }

        public bool TryShotBreach(ShotBreachZoneType zoneType)
        {
            if (!CanShotBreach())
            {
                return false;
            }

            bool validZone = zoneType == ShotBreachZoneType.Lock || zoneType == ShotBreachZoneType.Hinge;
            if (!validZone)
            {
                return false;
            }

            currentState = zoneType == ShotBreachZoneType.Hinge
                ? BreachableEntryState.Jammed
                : BreachableEntryState.Breached;

            NoiseSystem.Emit(transform.position, noiseShot, gameObject, "shot_breach");
            ApplyLinkedUnlocks();

            if (currentState == BreachableEntryState.Breached)
            {
                PlayOpenImmediate();
            }
            else
            {
                PlayOpenPartial();
            }

            return true;
        }

        public bool TryPanelBypass()
        {
            if (!CanPanelBypass())
            {
                return false;
            }

            currentState = BreachableEntryState.PeekOpen;
            NoiseSystem.Emit(transform.position, noiseManual * 0.5f, gameObject, "panel_bypass");
            ApplyLinkedUnlocks();
            PlayOpenPartial();
            return true;
        }

        public void SetFoamBlocked(bool blocked)
        {
            if (!supportsFoamBlock)
            {
                return;
            }

            if (blocked)
            {
                if (!CanFoamBlock())
                {
                    return;
                }

                _stateBeforeFoamBlock = currentState;
                currentState = BreachableEntryState.FoamBlocked;
                return;
            }

            if (currentState == BreachableEntryState.FoamBlocked)
            {
                currentState = _stateBeforeFoamBlock;
            }
        }

        public void ForceSetState(BreachableEntryState state)
        {
            currentState = state;
        }

        private IEnumerator DoManualBreach()
        {
            yield return new WaitForSeconds(breachTimeManual);
            if (!CanManualBreach())
            {
                _breachRoutine = null;
                yield break;
            }

            currentState = BreachableEntryState.PeekOpen;
            NoiseSystem.Emit(transform.position, noiseManual, gameObject, "manual_breach");
            ApplyLinkedUnlocks();
            PlayOpenPartial();
            _breachRoutine = null;
        }

        private IEnumerator DoForcedBreach()
        {
            yield return new WaitForSeconds(breachTimeForced);
            if (!CanForcedBreach())
            {
                _breachRoutine = null;
                yield break;
            }

            currentState = BreachableEntryState.Breached;
            NoiseSystem.Emit(transform.position, noiseForced, gameObject, "forced_breach");
            ApplyLinkedUnlocks();
            PlayOpenImmediate();
            _breachRoutine = null;
        }

        private void PlayOpenPartial()
        {
            StartMotion(motionType == BreachMotionType.Rotate ? openAngle * 0.35f : slideDistance * 0.4f);
        }

        private void PlayOpenImmediate()
        {
            StartMotion(motionType == BreachMotionType.Rotate ? openAngle : slideDistance);
        }

        private void StartMotion(float amount)
        {
            CancelAnimationRoutine();
            _animationRoutine = StartCoroutine(AnimateOpen(amount));
        }

        private IEnumerator AnimateOpen(float amount)
        {
            if (movingTransform == null)
            {
                _animationRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            Quaternion startRotation = movingTransform.localRotation;
            Vector3 startPosition = movingTransform.localPosition;
            Quaternion targetRotation = _closedLocalRotation;
            Vector3 targetPosition = _closedLocalPosition;

            if (motionType == BreachMotionType.Rotate)
            {
                targetRotation = _closedLocalRotation * Quaternion.Euler(0f, amount, 0f);
            }
            else
            {
                targetPosition = _closedLocalPosition + Vector3.up * amount;
            }

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                if (motionType == BreachMotionType.Rotate)
                {
                    movingTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                }
                else
                {
                    movingTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                }

                yield return null;
            }

            if (motionType == BreachMotionType.Rotate)
            {
                movingTransform.localRotation = targetRotation;
            }
            else
            {
                movingTransform.localPosition = targetPosition;
            }

            _animationRoutine = null;
        }

        public void SetInteractionHighlight(bool isHighlighted)
        {
            if (_outlineObjects == null)
            {
                return;
            }

            for (int i = 0; i < _outlineObjects.Length; i++)
            {
                GameObject outline = _outlineObjects[i];
                if (outline == null)
                {
                    continue;
                }

                outline.SetActive(isHighlighted);
            }
        }

        public void SetFoamHighlight(bool isHighlighted)
        {
            SetInteractionHighlight(isHighlighted);
        }

        private void CacheHighlightMaterials()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                highlightRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            }

            _outlineObjects = InteractionHighlightUtility.CreateOutlineObjects(highlightRenderers, highlightColor, "DoorOutline");
        }

        private string GetDefaultInteractionLabel()
        {
            return entryType switch
            {
                BreachableEntryType.MetalShutter => "metal shutter",
                BreachableEntryType.ServicePanel => "service panel hatch",
                BreachableEntryType.VentCover => "vent cover",
                BreachableEntryType.SideHatch => "side hatch",
                _ => "service door"
            };
        }

        public Vector3 GetInteractionWorldPosition()
        {
            if (highlightRenderers != null)
            {
                for (int i = 0; i < highlightRenderers.Length; i++)
                {
                    Renderer renderer = highlightRenderers[i];
                    if (renderer != null)
                    {
                        return renderer.bounds.center + interactionIndicatorOffset;
                    }
                }
            }

            Collider collider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
            if (collider != null)
            {
                return collider.bounds.center + interactionIndicatorOffset;
            }

            return transform.position + Vector3.up * (2.1f + interactionIndicatorOffset.y);
        }

        private void ApplyLinkedUnlocks()
        {
            if (linkedEntry != null &&
                linkedEntry.CurrentState != BreachableEntryState.Breached &&
                linkedEntry.CurrentState != BreachableEntryState.PeekOpen)
            {
                linkedEntry.ForceSetState(BreachableEntryState.Unlocked);
            }

            if (linkedBlocker != null)
            {
                linkedBlocker.SetActive(false);
            }

            if (linkedLock != null)
            {
                linkedLock.SetActive(false);
            }
        }

        private void CancelBreachRoutine()
        {
            if (_breachRoutine == null)
            {
                return;
            }

            StopCoroutine(_breachRoutine);
            _breachRoutine = null;
        }

        private void CancelAnimationRoutine()
        {
            if (_animationRoutine == null)
            {
                return;
            }

            StopCoroutine(_animationRoutine);
            _animationRoutine = null;
        }
    }
}
