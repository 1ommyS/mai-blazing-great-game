using System.Collections.Generic;
using IndustrialDemo.Combat;
using IndustrialDemo.Core;
using IndustrialDemo.Player;
using UnityEngine;

namespace IndustrialDemo.Actors
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider))]
    public class DemoEnemyActor : MonoBehaviour, IShotDamageReceiver
    {
        public enum CombatArchetype
        {
            Rifleman,
            Assault,
            Enforcer,
            Marksman
        }

        public enum EncounterRole
        {
            Anchor,
            Flanker,
            Pusher
        }

        private enum CombatState
        {
            Idle,
            Investigate,
            Reposition,
            Engage,
            Suppressed
        }

        [Header("Presentation")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Transform[] tacticalPoints;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Renderer[] emissiveRenderers;

        [Header("Combat")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(1f)] private float detectionRange = 80f;
        [SerializeField, Min(0.05f)] private float burstCooldown = 1.15f;
        [SerializeField, Min(0.02f)] private float burstShotSpacing = 0.08f;
        [SerializeField, Min(1)] private int burstShotCount = 3;
        [SerializeField, Min(0f)] private float shotDamage = 8f;
        [SerializeField, Min(0f)] private float shotSpreadDegrees = 2.25f;
        [SerializeField, Min(0.1f)] private float turnSpeed = 8f;
        [SerializeField, Min(0f)] private float aimHeight = 1.45f;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.8f;
        [SerializeField, Min(0.1f)] private float strafeSpeed = 2f;
        [SerializeField, Min(1f)] private float preferredMinRange = 8f;
        [SerializeField, Min(1f)] private float preferredMaxRange = 15f;
        [SerializeField, Min(0.1f)] private float reacquireDuration = 3.2f;
        [SerializeField, Min(0.1f)] private float tacticalPointReachDistance = 0.6f;
        [SerializeField, Min(0.1f)] private float tacticalPointSwitchInterval = 1.35f;
        [SerializeField, Min(1f)] private float wakeRange = 24f;
        [SerializeField, Min(2f)] private float leashRange = 34f;
        [SerializeField, Min(0.5f)] private float alertRetentionDuration = 6f;
        [SerializeField] private EncounterRole encounterRole = EncounterRole.Anchor;
        [SerializeField] private CombatArchetype combatArchetype = CombatArchetype.Rifleman;
        [SerializeField, Min(1f)] private float hearingScale = 4.5f;
        [SerializeField, Min(0f)] private float loudNoiseThreshold = 5f;
        [SerializeField, Min(0.05f)] private float reactionDelay = 0.25f;
        [SerializeField, Min(0.1f)] private float suppressionDuration = 1.35f;
        [SerializeField, Min(0.1f)] private float coverHoldDuration = 1.15f;
        [SerializeField, Min(0.1f)] private float exposeDurationMin = 0.55f;
        [SerializeField, Min(0.1f)] private float exposeDurationMax = 1.25f;
        [SerializeField, Min(0.1f)] private float retreatDistance = 3.2f;
        [SerializeField, Min(0.05f)] private float peekDistance = 0.8f;
        [SerializeField, Min(0.1f)] private float peekSpeed = 1.8f;

        [Header("Feedback")]
        [SerializeField] private Color idleColor = new(0.23f, 0.25f, 0.28f, 1f);
        [SerializeField] private Color alertColor = new(0.82f, 0.28f, 0.18f, 1f);
        [SerializeField] private Color hitColor = new(1f, 0.45f, 0.35f, 1f);
        [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float hitStaggerDuration = 0.18f;

        [Header("Tracer")]
        [SerializeField, ColorUsage(false, true)] private Color tracerColor = new(1f, 0.38f, 0.18f, 0.95f);
        [SerializeField, Min(0.001f)] private float tracerWidth = 0.024f;
        [SerializeField, Min(0.01f)] private float tracerDuration = 0.06f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private CapsuleCollider _capsule;
        private DemoFirstPersonMotor _playerMotor;
        private DemoPlayerHealth _playerHealth;
        private float _currentHealth;
        private float _nextFireTime;
        private float _nextBurstShotTime;
        private float _hitFlashUntil;
        private float _staggerUntil;
        private bool _isDead;
        private int _burstShotsRemaining;
        private int _strafeDirection = 1;
        private Quaternion _visualAliveRotation;
        private Vector3 _visualAlivePosition;
        private Material[] _emissiveMaterials;
        private Vector3 _homePosition;
        private Vector3 _lastSeenPlayerPosition;
        private float _lastSeenUntil;
        private int _activeTacticalPointIndex = -1;
        private float _nextTacticalRetargetTime;
        private Quaternion _staggerRotation = Quaternion.identity;
        private Vector3 _staggerOffset;
        private readonly Dictionary<object, float> _movementSlowSources = new();
        private CombatState _combatState;
        private float _reactionReadyTime;
        private float _suppressedUntil;
        private float _holdPositionUntil;
        private float _peekSign = 1f;
        private float _peekTimeOffset;
        private Vector3 _lastCoverPosition;
        private float _alertUntil;
        private bool _hadLineOfSightLastFrame;

        private void Awake()
        {
            _capsule = GetComponent<CapsuleCollider>();
            _capsule.center = new Vector3(0f, 0.9f, 0f);
            _capsule.height = 1.8f;
            _capsule.radius = 0.32f;

            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (muzzlePoint == null)
            {
                muzzlePoint = transform;
            }

            CacheMaterials();
            ApplyArchetypePreset();
            _homePosition = transform.position;
            ResetState();
        }

        private void OnEnable()
        {
            NoiseSystem.NoiseEmitted += HandleNoise;
        }

        private void OnDisable()
        {
            NoiseSystem.NoiseEmitted -= HandleNoise;
        }

        private void Update()
        {
            if (_isDead || !TryResolvePlayer())
            {
                return;
            }

            Vector3 origin = GetMuzzlePosition();
            Vector3 playerTarget = _playerMotor.transform.position + Vector3.up * aimHeight;
            Vector3 toPlayer = playerTarget - origin;
            float sqrDistance = toPlayer.sqrMagnitude;
            bool canDetect = sqrDistance <= detectionRange * detectionRange;
            bool hasLineOfSight = canDetect && HasLineOfSight(origin, playerTarget);
            bool isAwakeRange = sqrDistance <= wakeRange * wakeRange;
            bool isBeyondLeash = sqrDistance > leashRange * leashRange;
            bool isSuppressed = Time.time < _suppressedUntil;

            if (hasLineOfSight && isAwakeRange)
            {
                AlertTo(_playerMotor.transform.position, alertRetentionDuration + reacquireDuration);
            }

            if (!_isDead && !_hadLineOfSightLastFrame && hasLineOfSight)
            {
                _reactionReadyTime = Mathf.Max(_reactionReadyTime, Time.time + reactionDelay);
            }

            if (Time.time > _alertUntil && !hasLineOfSight && Time.time > _lastSeenUntil && isBeyondLeash)
            {
                _hadLineOfSightLastFrame = false;
                ResetAlertState();
            }

            if (Time.time > _alertUntil && !hasLineOfSight && !isAwakeRange && Time.time > _lastSeenUntil)
            {
                _combatState = CombatState.Idle;
                ApplyIdle();
                Vector3 toHome = _homePosition - transform.position;
                toHome.y = 0f;
                if (toHome.sqrMagnitude > 0.25f)
                {
                    TryMove(toHome.normalized * moveSpeed * 0.75f * Time.deltaTime);
                }
                ApplyLivePose(false);
                _hadLineOfSightLastFrame = false;
                return;
            }

            if (hasLineOfSight)
            {
                _lastSeenPlayerPosition = _playerMotor.transform.position;
                _lastSeenUntil = Time.time + reacquireDuration;
                _alertUntil = Mathf.Max(_alertUntil, Time.time + alertRetentionDuration);
            }

            Vector3 facingTarget = hasLineOfSight ? playerTarget : _lastSeenPlayerPosition + Vector3.up * aimHeight;
            RotateTowards(facingTarget);

            bool isMoving = UpdateMovement(hasLineOfSight, isSuppressed, sqrDistance);

            if (!hasLineOfSight)
            {
                _combatState = CombatState.Investigate;
                ApplyEmissiveColor(idleColor);
                ApplyLivePose(isMoving);
                _hadLineOfSightLastFrame = false;
                return;
            }

            _combatState = isSuppressed ? CombatState.Suppressed : (isMoving ? CombatState.Reposition : CombatState.Engage);
            ApplyEmissiveColor(Time.time < _hitFlashUntil ? hitColor : alertColor);
            UpdateFiring(origin, playerTarget, isSuppressed, isMoving);
            ApplyLivePose(isMoving);
            _hadLineOfSightLastFrame = true;
        }

        public void ReceiveShotDamage(ShotImpactContext context)
        {
            if (_isDead)
            {
                return;
            }

            _currentHealth -= Mathf.Max(0f, context.Damage);
            _hitFlashUntil = Time.time + hitFlashDuration;
            _staggerUntil = Time.time + hitStaggerDuration;
            _staggerOffset = new Vector3(Random.Range(-0.03f, 0.03f), 0f, Random.Range(-0.06f, -0.03f));
            _staggerRotation = Quaternion.Euler(Random.Range(-10f, -5f), Random.Range(-4f, 4f), Random.Range(-7f, 7f));
            ApplyEmissiveColor(hitColor);
            _lastSeenUntil = Time.time + reacquireDuration;
            _lastSeenPlayerPosition = context.Hit.point;
            _suppressedUntil = Time.time + suppressionDuration;
            _reactionReadyTime = Time.time + reactionDelay;
            _nextTacticalRetargetTime = Time.time;
            _holdPositionUntil = 0f;
            AlertTo(context.Hit.point, reacquireDuration + alertRetentionDuration);

            if (_currentHealth > 0f)
            {
                return;
            }

            Die();
        }

        public void ResetState()
        {
            _currentHealth = maxHealth;
            _isDead = false;
            _nextFireTime = 0f;
            _nextBurstShotTime = 0f;
            _hitFlashUntil = 0f;
            _staggerUntil = 0f;
            _burstShotsRemaining = 0;
            _staggerRotation = Quaternion.identity;
            _staggerOffset = Vector3.zero;
            _lastSeenUntil = 0f;
            _activeTacticalPointIndex = -1;
            _nextTacticalRetargetTime = 0f;
            _movementSlowSources.Clear();
            _combatState = CombatState.Idle;
            _reactionReadyTime = 0f;
            _suppressedUntil = 0f;
            _holdPositionUntil = 0f;
            _peekSign = Random.value > 0.5f ? 1f : -1f;
            _peekTimeOffset = Random.Range(0f, 5f);
            _lastCoverPosition = transform.position;
            _alertUntil = 0f;
            _hadLineOfSightLastFrame = false;

            if (_capsule != null)
            {
                _capsule.enabled = true;
            }

            if (visualRoot != null)
            {
                visualRoot.localPosition = _visualAlivePosition;
                visualRoot.localRotation = _visualAliveRotation;
            }

            SetRenderersEnabled(true);
            ApplyEmissiveColor(idleColor);
        }

        public void SetMovementSlow(object source, float multiplier)
        {
            if (source == null)
            {
                return;
            }

            _movementSlowSources[source] = Mathf.Clamp(multiplier, 0.1f, 1f);
        }

        public void ClearMovementSlow(object source)
        {
            if (source == null)
            {
                return;
            }

            _movementSlowSources.Remove(source);
        }

        private void Die()
        {
            _isDead = true;
            if (_capsule != null)
            {
                _capsule.enabled = false;
            }

            if (visualRoot != null)
            {
                Vector3 toPlayer = _playerMotor != null
                    ? (_playerMotor.transform.position - transform.position).normalized
                    : transform.forward;
                visualRoot.localPosition = _visualAlivePosition + new Vector3(0f, 0.08f, 0.32f);
                visualRoot.localRotation = Quaternion.LookRotation(new Vector3(toPlayer.x, 0f, toPlayer.z), Vector3.up) * Quaternion.Euler(84f, 0f, 0f);
            }

            ApplyEmissiveColor(new Color(0.08f, 0.08f, 0.08f, 1f));
        }

        private bool TryResolvePlayer()
        {
            if (_playerMotor == null)
            {
                _playerMotor = FindFirstObjectByType<DemoFirstPersonMotor>();
            }

            if (_playerMotor == null)
            {
                return false;
            }

            if (_playerHealth == null)
            {
                _playerHealth = _playerMotor.GetComponent<DemoPlayerHealth>();
            }

            return _playerHealth != null;
        }

        private void RotateTowards(Vector3 targetPosition)
        {
            Vector3 planar = targetPosition - transform.position;
            planar.y = 0f;
            if (planar.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion desired = Quaternion.LookRotation(planar.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, Time.deltaTime * turnSpeed);
        }

        private bool UpdateMovement(bool hasLineOfSight, bool isSuppressed, float sqrDistanceToPlayer)
        {
            Vector3 desiredDirection = Vector3.zero;
            float desiredSpeed = moveSpeed;
            Transform tacticalPoint = GetCurrentTacticalPoint(hasLineOfSight, isSuppressed);

            if (hasLineOfSight)
            {
                float distance = Mathf.Sqrt(sqrDistanceToPlayer);
                Vector3 toPlayer = (_playerMotor.transform.position - transform.position).normalized;
                Vector3 strafe = Vector3.Cross(Vector3.up, toPlayer) * _strafeDirection;
                float effectiveMinRange = preferredMinRange;
                float effectiveMaxRange = preferredMaxRange;

                switch (encounterRole)
                {
                    case EncounterRole.Flanker:
                        effectiveMinRange -= 1.5f;
                        effectiveMaxRange -= 1.5f;
                        break;
                    case EncounterRole.Pusher:
                        effectiveMinRange -= 3f;
                        effectiveMaxRange -= 3f;
                        break;
                }

                if (isSuppressed)
                {
                    desiredDirection = GetCoverRetreatDirection(toPlayer, tacticalPoint);
                    desiredSpeed = moveSpeed * 1.1f;
                }
                else if (tacticalPoint != null)
                {
                    Vector3 toPoint = tacticalPoint.position - transform.position;
                    toPoint.y = 0f;
                    if (toPoint.sqrMagnitude > tacticalPointReachDistance * tacticalPointReachDistance)
                    {
                        desiredDirection = toPoint.normalized;
                        desiredSpeed = encounterRole == EncounterRole.Flanker ? strafeSpeed * 1.15f : moveSpeed;
                        _lastCoverPosition = tacticalPoint.position;
                    }
                    else if (Time.time < _holdPositionUntil)
                    {
                        desiredDirection = ComputePeekDirection(toPlayer);
                        desiredSpeed = strafeSpeed * 0.85f;
                    }
                }

                if (desiredDirection == Vector3.zero && distance > effectiveMaxRange)
                {
                    desiredDirection = toPlayer;
                    _holdPositionUntil = Time.time + Random.Range(coverHoldDuration * 0.7f, coverHoldDuration * 1.2f);
                }
                else if (desiredDirection == Vector3.zero && distance < effectiveMinRange)
                {
                    desiredDirection = -toPlayer;
                }
                else if (desiredDirection == Vector3.zero)
                {
                    desiredDirection = encounterRole == EncounterRole.Pusher && Random.value > 0.55f
                        ? toPlayer
                        : strafe;
                    desiredSpeed = encounterRole == EncounterRole.Flanker ? strafeSpeed * 1.2f : strafeSpeed;
                }
            }
            else if (Time.time <= _lastSeenUntil)
            {
                Vector3 targetPosition = tacticalPoint != null ? tacticalPoint.position : _lastSeenPlayerPosition;
                Vector3 toLastSeen = targetPosition - transform.position;
                toLastSeen.y = 0f;
                if (toLastSeen.sqrMagnitude > 1f)
                {
                    desiredDirection = toLastSeen.normalized;
                }
            }
            else
            {
                Vector3 toHome = _homePosition - transform.position;
                toHome.y = 0f;
                if (toHome.sqrMagnitude > 0.25f)
                {
                    desiredDirection = toHome.normalized;
                }
            }

            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            desiredDirection.Normalize();
            float stepDistance = desiredSpeed * GetMovementMultiplier() * Time.deltaTime;
            if (TryMove(desiredDirection * stepDistance))
            {
                return true;
            }

            if (hasLineOfSight)
            {
                _strafeDirection *= -1;
            }

            Vector3 fallback = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            return fallback.sqrMagnitude > 0.01f && TryMove(fallback * stepDistance * 0.5f);
        }

        private Vector3 GetCoverRetreatDirection(Vector3 toPlayer, Transform tacticalPoint)
        {
            if (tacticalPoint != null)
            {
                Vector3 toPoint = tacticalPoint.position - transform.position;
                toPoint.y = 0f;
                if (toPoint.sqrMagnitude > tacticalPointReachDistance * tacticalPointReachDistance)
                {
                    return toPoint.normalized;
                }
            }

            Vector3 retreat = -toPlayer;
            Vector3 lateral = Vector3.Cross(Vector3.up, toPlayer) * _strafeDirection * 0.6f;
            Vector3 result = retreat * retreatDistance + lateral;
            result.y = 0f;
            return result.sqrMagnitude > 0.001f ? result.normalized : -toPlayer;
        }

        private Vector3 ComputePeekDirection(Vector3 toPlayer)
        {
            float wave = Mathf.Sin((Time.time + _peekTimeOffset) * peekSpeed);
            if (Mathf.Abs(wave) < 0.05f)
            {
                _peekSign *= -1f;
            }

            Vector3 strafe = Vector3.Cross(Vector3.up, toPlayer) * _peekSign;
            Vector3 offset = strafe * peekDistance;
            offset.y = 0f;
            return offset.sqrMagnitude > 0.001f ? offset.normalized : strafe.normalized;
        }

        private bool TryMove(Vector3 delta)
        {
            if (_capsule == null || delta.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            Vector3 center = transform.position + _capsule.center;
            float halfHeight = Mathf.Max(_capsule.height * 0.5f - _capsule.radius, 0.01f);
            Vector3 bottom = center + Vector3.down * halfHeight;
            Vector3 top = center + Vector3.up * halfHeight;
            float distance = delta.magnitude;

            if (Physics.CapsuleCast(bottom, top, _capsule.radius * 0.95f, delta / distance, out RaycastHit hit, distance + 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<DemoPlayerHealth>() == null)
                {
                    return false;
                }
            }

            transform.position += delta;
            return true;
        }

        private Transform GetCurrentTacticalPoint(bool hasLineOfSight, bool isSuppressed)
        {
            if (tacticalPoints == null || tacticalPoints.Length == 0 || _playerMotor == null)
            {
                return null;
            }

            if (_activeTacticalPointIndex < 0 || _activeTacticalPointIndex >= tacticalPoints.Length || tacticalPoints[_activeTacticalPointIndex] == null || Time.time >= _nextTacticalRetargetTime)
            {
                _activeTacticalPointIndex = SelectBestTacticalPointIndex(hasLineOfSight, isSuppressed);
                _nextTacticalRetargetTime = Time.time + tacticalPointSwitchInterval;
            }

            if (_activeTacticalPointIndex < 0 || _activeTacticalPointIndex >= tacticalPoints.Length)
            {
                return null;
            }

            return tacticalPoints[_activeTacticalPointIndex];
        }

        private int SelectBestTacticalPointIndex(bool hasLineOfSight, bool isSuppressed)
        {
            if (tacticalPoints == null || tacticalPoints.Length == 0 || _playerMotor == null)
            {
                return -1;
            }

            Vector3 playerPosition = _playerMotor.transform.position;
            Vector3 playerTarget = playerPosition + Vector3.up * aimHeight;
            Vector3 playerRight = _playerMotor.transform.right;
            int bestIndex = -1;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < tacticalPoints.Length; i++)
            {
                Transform point = tacticalPoints[i];
                if (point == null)
                {
                    continue;
                }

                Vector3 toPoint = point.position - transform.position;
                toPoint.y = 0f;
                Vector3 fromPlayer = point.position - playerPosition;
                fromPlayer.y = 0f;
                float playerDistance = fromPlayer.magnitude;
                Vector3 fromPlayerDirection = fromPlayer.sqrMagnitude > 0.001f ? fromPlayer.normalized : transform.forward;
                float lateral = Mathf.Abs(Vector3.Dot(fromPlayerDirection, playerRight));
                bool pointHasSight = HasLineOfSight(point.position + Vector3.up * aimHeight, playerTarget);
                float coverScore = EvaluateCoverScore(point.position + Vector3.up * 1.1f, playerTarget);
                float score = 0f;

                switch (encounterRole)
                {
                    case EncounterRole.Anchor:
                        score = -Vector3.Distance(point.position, _homePosition) * 0.6f - Mathf.Abs(playerDistance - preferredMaxRange);
                        score += pointHasSight ? 3.25f : -2f;
                        score += coverScore * 4.25f;
                        break;
                    case EncounterRole.Flanker:
                        score = lateral * 8f - Mathf.Abs(playerDistance - preferredMaxRange) * 0.3f;
                        score += pointHasSight ? 2.25f : -0.9f;
                        score += coverScore * 2.4f;
                        break;
                    case EncounterRole.Pusher:
                        score = -playerDistance * 0.5f + Vector3.Dot(transform.forward, toPoint.normalized) * 0.5f;
                        score += pointHasSight ? 1.75f : 0.25f;
                        score += coverScore * 1.8f;
                        break;
                }

                if (!hasLineOfSight)
                {
                    score += 1.5f;
                }

                if (isSuppressed)
                {
                    score += coverScore * 5f;
                    score += pointHasSight ? -0.75f : 1.25f;
                }

                if (combatArchetype == CombatArchetype.Marksman)
                {
                    score += pointHasSight ? 2f : -1.25f;
                    score -= Mathf.Abs(playerDistance - preferredMaxRange) * 0.55f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private void UpdateFiring(Vector3 origin, Vector3 playerTarget, bool isSuppressed, bool isMoving)
        {
            if (isSuppressed || Time.time < _reactionReadyTime)
            {
                return;
            }

            if (combatArchetype == CombatArchetype.Marksman && isMoving)
            {
                return;
            }

            if (_burstShotsRemaining <= 0 && Time.time >= _nextFireTime)
            {
                _burstShotsRemaining = Mathf.Max(1, burstShotCount);
                _nextBurstShotTime = Time.time;
                _holdPositionUntil = Time.time + Random.Range(exposeDurationMin, exposeDurationMax);
            }

            if (_burstShotsRemaining <= 0 || Time.time < _nextBurstShotTime)
            {
                return;
            }

            FireSingleShot(origin, playerTarget, isMoving);
            _burstShotsRemaining--;

            if (_burstShotsRemaining > 0)
            {
                _nextBurstShotTime = Time.time + burstShotSpacing;
            }
            else
            {
                _nextFireTime = Time.time + burstCooldown;
            }
        }

        private void FireSingleShot(Vector3 origin, Vector3 playerTarget, bool isMoving)
        {
            Vector3 direction = (playerTarget - origin).normalized;
            float spreadMultiplier = isMoving ? 1.55f : 1f;
            if (combatArchetype == CombatArchetype.Assault && isMoving)
            {
                spreadMultiplier = 1.25f;
            }

            direction = Quaternion.Euler(
                Random.Range(-shotSpreadDegrees * spreadMultiplier, shotSpreadDegrees * spreadMultiplier),
                Random.Range(-shotSpreadDegrees * spreadMultiplier, shotSpreadDegrees * spreadMultiplier),
                0f) * direction;

            Vector3 endPoint = origin + direction * detectionRange;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, detectionRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                endPoint = hit.point;
                DemoPlayerHealth hitPlayer = hit.collider.GetComponentInParent<DemoPlayerHealth>();
                if (hitPlayer != null)
                {
                    hitPlayer.ApplyDamage(shotDamage);
                }
            }

            BulletTracer.Spawn(origin, endPoint, tracerColor, tracerWidth, tracerDuration);
            Debug.DrawLine(origin, endPoint, new Color(1f, 0.3f, 0.15f), 0.15f, false);
        }

        private bool HasLineOfSight(Vector3 origin, Vector3 target)
        {
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
            {
                return false;
            }

            if (!Physics.Raycast(origin, direction / distance, out RaycastHit hit, distance + 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return hit.collider.GetComponentInParent<DemoFirstPersonMotor>() != null;
        }

        private Vector3 GetMuzzlePosition()
        {
            return muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * aimHeight;
        }

        private void ApplyIdle()
        {
            ApplyEmissiveColor(idleColor);
        }

        private void HandleNoise(NoiseEvent noiseEvent)
        {
            if (_isDead || noiseEvent.Intensity <= 0f)
            {
                return;
            }

            if (noiseEvent.Source != null && noiseEvent.Source.transform.root == transform.root)
            {
                return;
            }

            Vector3 targetPosition = noiseEvent.Position;
            if (noiseEvent.Source != null)
            {
                DemoFirstPersonMotor sourcePlayer = noiseEvent.Source.GetComponentInParent<DemoFirstPersonMotor>();
                if (sourcePlayer != null)
                {
                    targetPosition = sourcePlayer.transform.position;
                }
            }

            Vector3 toNoise = targetPosition - transform.position;
            toNoise.y = 0f;
            float hearingDistance = Mathf.Max(2.5f, noiseEvent.Intensity * hearingScale);
            if (toNoise.sqrMagnitude > hearingDistance * hearingDistance)
            {
                return;
            }

            float durationMultiplier = noiseEvent.Intensity >= loudNoiseThreshold ? 1.35f : 0.6f;
            if (noiseEvent.Category == "panel_bypass" || noiseEvent.Category == "steam_sealed")
            {
                durationMultiplier *= 0.8f;
            }

            if (noiseEvent.Category == "gunshot" || noiseEvent.Category == "ricochet" || noiseEvent.Category == "cover_break" || noiseEvent.Category == "forced_breach" || noiseEvent.Category == "shot_breach")
            {
                _suppressedUntil = Mathf.Max(_suppressedUntil, Time.time + suppressionDuration * 0.65f);
            }

            _lastSeenPlayerPosition = targetPosition;
            _lastSeenUntil = Mathf.Max(_lastSeenUntil, Time.time + reacquireDuration * durationMultiplier);
            _nextTacticalRetargetTime = Time.time;
            _strafeDirection = Random.value > 0.5f ? 1 : -1;
            _reactionReadyTime = Time.time + reactionDelay;
            AlertTo(targetPosition, reacquireDuration * durationMultiplier + alertRetentionDuration);
        }

        private void AlertTo(Vector3 targetPosition, float duration)
        {
            _lastSeenPlayerPosition = targetPosition;
            _lastSeenUntil = Mathf.Max(_lastSeenUntil, Time.time + duration);
            _alertUntil = Mathf.Max(_alertUntil, Time.time + duration);
        }

        private void ResetAlertState()
        {
            _lastSeenUntil = 0f;
            _alertUntil = 0f;
            _burstShotsRemaining = 0;
            _activeTacticalPointIndex = -1;
            _holdPositionUntil = 0f;
            _reactionReadyTime = 0f;
        }

        private float EvaluateCoverScore(Vector3 point, Vector3 playerTarget)
        {
            Vector3 direction = playerTarget - point;
            float distance = direction.magnitude;
            if (distance <= 0.05f)
            {
                return 0f;
            }

            if (!Physics.Raycast(point, direction / distance, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return 0f;
            }

            return hit.collider.GetComponentInParent<DemoFirstPersonMotor>() == null
                ? Mathf.Clamp01(1f - (hit.distance / distance))
                : 0f;
        }

        private float GetMovementMultiplier()
        {
            float multiplier = 1f;
            foreach (float value in _movementSlowSources.Values)
            {
                multiplier = Mathf.Min(multiplier, value);
            }

            return multiplier;
        }

        private void CacheMaterials()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            }

            if (emissiveRenderers == null)
            {
                emissiveRenderers = System.Array.Empty<Renderer>();
            }

            _emissiveMaterials = new Material[emissiveRenderers.Length];
            for (int i = 0; i < emissiveRenderers.Length; i++)
            {
                _emissiveMaterials[i] = emissiveRenderers[i] != null ? emissiveRenderers[i].material : null;
            }

            _visualAlivePosition = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
            _visualAliveRotation = visualRoot != null ? visualRoot.localRotation : Quaternion.identity;
        }

        private void ApplyArchetypePreset()
        {
            switch (combatArchetype)
            {
                case CombatArchetype.Assault:
                    maxHealth = 110f;
                    moveSpeed = 3.3f;
                    strafeSpeed = 2.5f;
                    shotDamage = 7f;
                    shotSpreadDegrees = 2.8f;
                    burstShotCount = 4;
                    burstCooldown = 0.95f;
                    preferredMinRange = 6f;
                    preferredMaxRange = 12f;
                    wakeRange = 20f;
                    leashRange = 30f;
                    break;
                case CombatArchetype.Enforcer:
                    maxHealth = 160f;
                    moveSpeed = 2.25f;
                    strafeSpeed = 1.6f;
                    shotDamage = 11f;
                    shotSpreadDegrees = 2f;
                    burstShotCount = 5;
                    burstShotSpacing = 0.1f;
                    burstCooldown = 1.45f;
                    preferredMinRange = 7f;
                    preferredMaxRange = 13f;
                    suppressionDuration = 0.9f;
                    wakeRange = 22f;
                    leashRange = 28f;
                    break;
                case CombatArchetype.Marksman:
                    maxHealth = 90f;
                    moveSpeed = 2.4f;
                    strafeSpeed = 1.7f;
                    shotDamage = 14f;
                    shotSpreadDegrees = 0.9f;
                    burstShotCount = 2;
                    burstShotSpacing = 0.18f;
                    burstCooldown = 1.8f;
                    preferredMinRange = 12f;
                    preferredMaxRange = 20f;
                    reactionDelay = 0.16f;
                    wakeRange = 28f;
                    leashRange = 38f;
                    break;
                default:
                    maxHealth = 100f;
                    moveSpeed = 2.8f;
                    strafeSpeed = 2f;
                    shotDamage = 8f;
                    shotSpreadDegrees = 2.25f;
                    burstShotCount = 3;
                    burstCooldown = 1.15f;
                    preferredMinRange = 8f;
                    preferredMaxRange = 15f;
                    wakeRange = 24f;
                    leashRange = 34f;
                    break;
            }
        }

        private void ApplyLivePose(bool isMoving)
        {
            if (visualRoot == null)
            {
                return;
            }

            Quaternion targetRotation = _visualAliveRotation;
            Vector3 targetPosition = _visualAlivePosition;

            if (isMoving)
            {
                float sway = Mathf.Sin(Time.time * 9f) * 3.5f;
                targetRotation *= Quaternion.Euler(0f, 0f, sway);
            }

            if (Time.time < _staggerUntil)
            {
                float t = 1f - Mathf.Clamp01((_staggerUntil - Time.time) / hitStaggerDuration);
                targetRotation *= Quaternion.Slerp(_staggerRotation, Quaternion.identity, t);
                targetPosition += Vector3.Lerp(_staggerOffset, Vector3.zero, t);
            }

            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRotation, Time.deltaTime * 18f);
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPosition, Time.deltaTime * 18f);
        }

        private void SetRenderersEnabled(bool value)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = value;
                }
            }
        }

        private void ApplyEmissiveColor(Color color)
        {
            if (_emissiveMaterials == null)
            {
                return;
            }

            for (int i = 0; i < _emissiveMaterials.Length; i++)
            {
                Material material = _emissiveMaterials[i];
                if (material != null && material.HasProperty(BaseColorId))
                {
                    material.SetColor(BaseColorId, color);
                }
            }
        }
    }
}
