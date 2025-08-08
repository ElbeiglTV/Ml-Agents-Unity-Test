using System.Collections.Generic;
using UnityEngine;

namespace MLNavigation
{
    [AddComponentMenu("ML Navigation/Navigation Area")]
    public class NavigationArea : MonoBehaviour
    {
        [Header("Dimensiones del área (en XZ)")]
        [Tooltip("Semiextensión en X (desde el centro a cada borde).")]
        public float halfExtentX = 12f;

        [Tooltip("Semiextensión en Z (desde el centro a cada borde).")]
        public float halfExtentZ = 12f;

        [Header("Obstáculos")]
        [Tooltip("Número de obstáculos a generar por episodio.")]
        public int obstacleCount = 20;

        [Tooltip("Porcentaje [0-1] de obstáculos que serán móviles.")]
        [Range(0f, 1f)] public float movingObstacleRatio = 0.25f;

        [Tooltip("Prefab del obstáculo (recomendado: Cubo con Collider). Si está vacío, se generará un Cubo primitivo.")]
        public GameObject obstaclePrefab;

        [Tooltip("Tamaño uniforme de los obstáculos (lado del cubo).")]
        public Vector2 obstacleSizeRange = new Vector2(0.6f, 2.0f);

        [Tooltip("Velocidad de los obstáculos móviles (rango aleatorio).")]
        public Vector2 movingSpeedRange = new Vector2(1.0f, 3.5f);

        [Header("Suelo")]
        [Tooltip("Altura del suelo. El agente y obstáculos se colocan sobre esta Y.")]
        public float groundY = 0f;

        [Tooltip("Grosor del plano suelo (para colisión).")]
        public float groundThickness = 1f;

        [Header("Referencias")]
        public NavigationAgent agent;
        public Transform target;

        private readonly List<GameObject> spawnedObstacles = new List<GameObject>();
        private Transform obstaclesParent;

        public float MaxRadiusXZ => Mathf.Max(halfExtentX, halfExtentZ);

        private void Awake()
        {
            EnsureGround();

            if (obstaclesParent == null)
            {
                var parent = new GameObject("Obstacles");
                parent.transform.SetParent(transform);
                obstaclesParent = parent.transform;
            }
        }

        public void ResetArea(NavigationAgent caller)
        {
            if (agent == null)
            {
                agent = caller;
            }

            // Limpiar obstáculos anteriores
            for (int i = 0; i < spawnedObstacles.Count; i++)
            {
                if (spawnedObstacles[i] != null)
                {
                    Destroy(spawnedObstacles[i]);
                }
            }
            spawnedObstacles.Clear();

            // Reposicionar agente y objetivo
            Vector3 agentPos = SampleFreePosition(minDistanceFromEdges: 1.0f);
            PlaceAgent(agentPos);

            Vector3 targetPos = SampleFreePosition(minDistanceFromEdges: 1.0f, avoid: agentPos, minDistanceToAvoid: 6.0f);
            PlaceTarget(targetPos);

            // Generar obstáculos
            int numMoving = Mathf.RoundToInt(obstacleCount * movingObstacleRatio);
            for (int i = 0; i < obstacleCount; i++)
            {
                bool makeMoving = i < numMoving;
                SpawnObstacle(makeMoving);
            }
        }

        private void EnsureGround()
        {
            // Crear un suelo sencillo si no existe uno como hijo
            var ground = transform.Find("Ground");
            if (ground == null)
            {
                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plane.name = "Ground";
                plane.transform.SetParent(transform);
                plane.transform.localPosition = new Vector3(0f, groundY - groundThickness * 0.5f, 0f);
                plane.transform.localScale = new Vector3(halfExtentX * 2f, groundThickness, halfExtentZ * 2f);
                var renderer = plane.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    renderer.sharedMaterial.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                }
                plane.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
        }

        private void PlaceAgent(Vector3 position)
        {
            if (agent != null)
            {
                agent.transform.position = position;
                var rb = agent.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        private void PlaceTarget(Vector3 position)
        {
            if (target == null)
            {
                // Crear esfera objetivo si no existe
                GameObject t = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                t.name = "Target";
                t.transform.localScale = Vector3.one * 1.0f;
                var renderer = t.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    renderer.sharedMaterial.color = Color.green;
                }
                target = t.transform;
            }

            target.position = new Vector3(position.x, groundY + 0.5f, position.z);

            var mover = target.GetComponent<TargetController>();
            if (mover == null)
            {
                mover = target.gameObject.AddComponent<TargetController>();
            }
            mover.area = this;
        }

        private void SpawnObstacle(bool moving)
        {
            Vector3 pos = SampleFreePosition(minDistanceFromEdges: 0.5f, avoid: agent != null ? agent.transform.position : (Vector3?)null, minDistanceToAvoid: 2.5f);
            GameObject obj = obstaclePrefab != null ? Instantiate(obstaclePrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = moving ? "Obstacle_Moving" : "Obstacle";
            obj.transform.SetParent(obstaclesParent);
            obj.transform.position = new Vector3(pos.x, groundY + 0.5f, pos.z);

            float size = Random.Range(obstacleSizeRange.x, obstacleSizeRange.y);
            obj.transform.localScale = new Vector3(size, size, size);

            // Asegurar collider y rigidbody según sea necesario (los estáticos sin Rigidbody)
            var collider = obj.GetComponent<Collider>();
            if (collider == null)
            {
                collider = obj.AddComponent<BoxCollider>();
            }

            int obstacleLayer = LayerMask.NameToLayer("Obstacle");
            if (obstacleLayer < 0) { obstacleLayer = 0; }
            obj.layer = obstacleLayer;

            if (moving)
            {
                var mov = obj.AddComponent<MovingObstacle>();
                mov.area = this;
                mov.speed = Random.Range(movingSpeedRange.x, movingSpeedRange.y);
                mov.direction = Random.insideUnitCircle.normalized;
            }

            spawnedObstacles.Add(obj);
        }

        private Vector3 SampleFreePosition(float minDistanceFromEdges, Vector3? avoid = null, float minDistanceToAvoid = 0f)
        {
            // Intentos para encontrar una posición libre simple (sin chequeos de colisión complejos)
            const int maxAttempts = 50;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float x = Random.Range(-halfExtentX + minDistanceFromEdges, halfExtentX - minDistanceFromEdges);
                float z = Random.Range(-halfExtentZ + minDistanceFromEdges, halfExtentZ - minDistanceFromEdges);
                Vector3 candidate = new Vector3(x, groundY, z);

                if (avoid.HasValue && Vector3.Distance(new Vector3(avoid.Value.x, groundY, avoid.Value.z), candidate) < minDistanceToAvoid)
                {
                    continue;
                }

                return candidate;
            }

            // Fallback (centro)
            return new Vector3(0f, groundY, 0f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(transform.position.x, groundY, transform.position.z), new Vector3(halfExtentX * 2f, 0.01f, halfExtentZ * 2f));
        }
    }
}


