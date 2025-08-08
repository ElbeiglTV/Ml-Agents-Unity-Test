using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

namespace MLNavigation
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [AddComponentMenu("ML Navigation/Navigation Agent")]
    public class NavigationAgent : Agent
    {
        [Header("Referencias")]
        [Tooltip("Transform del objetivo a alcanzar.")]
        public Transform targetTransform;

        [Tooltip("Referencia al área que gestiona la generación procedural y reseteos.")]
        public NavigationArea area;

        [Header("Movimiento")]
        [Tooltip("Velocidad base de movimiento (magnitud del vector de acciones).")]
        public float moveSpeed = 6f;

        [Tooltip("Velocidad máxima del Rigidbody en el plano XZ.")]
        public float maxSpeed = 6f;

        [Tooltip("Fricción lineal cuando no hay input (reduce derrape).")]
        public float damping = 10f;

        [Header("Percepción por Raycast")]
        [Tooltip("Número de rayos equiespaciados en el plano XZ (360°).")]
        [Range(4, 64)] public int numRays = 16;

        [Tooltip("Longitud máxima de los raycasts.")]
        public float rayDistance = 8f;

        [Tooltip("Altura desde la que se disparan los rayos (evita tocar el suelo)." )]
        public float rayStartHeight = 0.5f;

        [Tooltip("Capas que se consideran obstáculos para los raycasts y colisiones.")]
        public LayerMask obstacleMask;

        [Header("Recompensas")]
        [Tooltip("Recompensa por acercamiento incremental al objetivo por paso.")]
        public float approachRewardScale = 0.05f;

        [Tooltip("Penalización pequeña por paso para incentivar trayectorias cortas.")]
        public float stepPenalty = -0.001f;

        [Tooltip("Recompensa al alcanzar el objetivo.")]
        public float goalReward = 1.0f;

        [Tooltip("Penalización al colisionar con un obstáculo.")]
        public float collisionPenalty = -0.5f;

        [Tooltip("Distancia considerada como haber llegado al objetivo.")]
        public float goalThreshold = 1.0f;

        private Rigidbody agentRigidbody;
        private float previousDistanceToTarget;
        private const int decisionPeriod = 5;

        public override void Initialize()
        {
            agentRigidbody = GetComponent<Rigidbody>();
            agentRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            agentRigidbody.useGravity = false;

            // Asegurar que el BehaviorParameters tenga configuración continua de 2 acciones
            var behaviorParams = GetComponent<BehaviorParameters>();
            if (behaviorParams == null)
            {
                behaviorParams = gameObject.AddComponent<BehaviorParameters>();
            }
            behaviorParams.BehaviorName = "Navigation";
            behaviorParams.BehaviorType = BehaviorType.Default;
            behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(2);

            // Asegurar un DecisionRequester para solicitar decisiones periódicas
            var requester = GetComponent<DecisionRequester>();
            if (requester == null)
            {
                requester = gameObject.AddComponent<DecisionRequester>();
            }
            requester.DecisionPeriod = Mathf.Max(1, decisionPeriod);
        }

        public override void OnEpisodeBegin()
        {
            // Reset físico del agente
            agentRigidbody.velocity = Vector3.zero;
            agentRigidbody.angularVelocity = Vector3.zero;

            // Delegar en el área la recolocación procedural de agente, objetivo y obstáculos
            if (area != null)
            {
                area.ResetArea(this);
            }

            previousDistanceToTarget = GetDistanceToTarget();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // Observación 1-3: Dirección normalizada hacia el objetivo (x, z) y distancia normalizada
            Vector3 toTarget = GetVectorToTargetXZ();
            float distance = toTarget.magnitude;
            Vector3 dirToTarget = distance > 1e-5f ? toTarget / Mathf.Max(distance, 1e-5f) : Vector3.zero;

            float normalizationRadius = (area != null) ? area.MaxRadiusXZ : 15f;
            float normalizedDistance = Mathf.Clamp01(distance / Mathf.Max(normalizationRadius, 1e-3f));

            sensor.AddObservation(dirToTarget.x);
            sensor.AddObservation(dirToTarget.z);
            sensor.AddObservation(normalizedDistance);

            // Observación 4-5: Velocidad propia en XZ (limitada)
            Vector3 vel = agentRigidbody.velocity;
            sensor.AddObservation(Mathf.Clamp(vel.x / maxSpeed, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(vel.z / maxSpeed, -1f, 1f));

            // Observaciones por raycasts: para cada rayo, distancia normalizada (1: libre, 0: impacto inmediato)
            Vector3 origin = transform.position + Vector3.up * rayStartHeight;
            for (int i = 0; i < numRays; i++)
            {
                float angleDeg = (360f / numRays) * i;
                Vector3 dir = Quaternion.Euler(0f, angleDeg, 0f) * Vector3.forward;
                if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, obstacleMask == 0 ? Physics.DefaultRaycastLayers : obstacleMask))
                {
                    sensor.AddObservation(Mathf.Clamp01(hit.distance / rayDistance));
                }
                else
                {
                    sensor.AddObservation(1f);
                }
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            // Acción continua: (moveX, moveZ)
            Vector2 move = actionBuffers.ContinuousActions.Length >= 2
                ? new Vector2(actionBuffers.ContinuousActions[0], actionBuffers.ContinuousActions[1])
                : Vector2.zero;

            Vector3 desiredVelocity = new Vector3(move.x, 0f, move.y) * moveSpeed;

            // Aplicar movimiento tipo "locomoción simple" manteniendo Y
            Vector3 currentVelocity = agentRigidbody.velocity;
            Vector3 targetVelocity = new Vector3(desiredVelocity.x, currentVelocity.y, desiredVelocity.z);

            // Interpolación suave + clamp de velocidad máxima en XZ
            Vector3 newVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * damping);
            Vector3 planar = new Vector3(newVelocity.x, 0f, newVelocity.z);
            if (planar.magnitude > maxSpeed)
            {
                planar = planar.normalized * maxSpeed;
            }
            agentRigidbody.velocity = new Vector3(planar.x, currentVelocity.y, planar.z);

            // Recompensas de shaping por acercamiento al objetivo y penalización por paso
            float distance = GetDistanceToTarget();
            float delta = previousDistanceToTarget - distance; // positivo si nos acercamos
            AddReward(delta * approachRewardScale);
            AddReward(stepPenalty);
            previousDistanceToTarget = distance;

            // Comprobación de objetivo alcanzado
            if (distance <= goalThreshold)
            {
                AddReward(goalReward);
                EndEpisode();
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var cont = actionsOut.ContinuousActions;
            cont[0] = Input.GetAxis("Horizontal");
            cont[1] = Input.GetAxis("Vertical");
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Penalizar colisiones con obstáculos y terminar el episodio
            if (IsInLayerMask(collision.collider.gameObject.layer, obstacleMask))
            {
                AddReward(collisionPenalty);
                EndEpisode();
            }
        }

        private Vector3 GetVectorToTargetXZ()
        {
            if (targetTransform == null)
            {
                return Vector3.zero;
            }
            Vector3 delta = targetTransform.position - transform.position;
            return new Vector3(delta.x, 0f, delta.z);
        }

        private float GetDistanceToTarget()
        {
            return GetVectorToTargetXZ().magnitude;
        }

        private static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }
    }
}


