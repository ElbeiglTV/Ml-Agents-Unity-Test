using UnityEngine;

namespace MLNavigation
{
    [AddComponentMenu("ML Navigation/Target Controller")]
    public class TargetController : MonoBehaviour
    {
        [Tooltip("Área de navegación a la que pertenece el objetivo.")]
        public NavigationArea area;

        [Header("Movimiento aleatorio opcional")]
        [Tooltip("Si es verdadero, el objetivo deambula aleatoriamente dentro del área.")]
        public bool wander = true;

        [Tooltip("Velocidad de deambular.")]
        public float wanderSpeed = 1.5f;

        [Tooltip("Tiempo medio entre cambios de dirección.")]
        public float directionChangeInterval = 2.5f;

        private float changeTimer;
        private Vector2 currentDir;

        private void OnEnable()
        {
            PickNewDirection();
        }

        private void Update()
        {
            if (area == null || !wander) return;

            changeTimer -= Time.deltaTime;
            if (changeTimer <= 0f)
            {
                PickNewDirection();
            }

            Vector3 pos = transform.position;
            pos += new Vector3(currentDir.x, 0f, currentDir.y) * wanderSpeed * Time.deltaTime;

            float minX = area.transform.position.x - area.halfExtentX + 0.5f;
            float maxX = area.transform.position.x + area.halfExtentX - 0.5f;
            float minZ = area.transform.position.z - area.halfExtentZ + 0.5f;
            float maxZ = area.transform.position.z + area.halfExtentZ - 0.5f;

            // Rebote contra límites
            if (pos.x < minX || pos.x > maxX)
            {
                currentDir.x *= -1f;
                pos.x = Mathf.Clamp(pos.x, minX, maxX);
                changeTimer = directionChangeInterval; // reiniciar temporizador para evitar chattering
            }
            if (pos.z < minZ || pos.z > maxZ)
            {
                currentDir.y *= -1f;
                pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
                changeTimer = directionChangeInterval;
            }

            transform.position = pos;
        }

        public void TeleportRandom()
        {
            if (area == null) return;
            float x = Random.Range(-area.halfExtentX + 0.5f, area.halfExtentX - 0.5f) + area.transform.position.x;
            float z = Random.Range(-area.halfExtentZ + 0.5f, area.halfExtentZ - 0.5f) + area.transform.position.z;
            transform.position = new Vector3(x, area.groundY + 0.5f, z);
        }

        private void PickNewDirection()
        {
            currentDir = Random.insideUnitCircle.normalized;
            changeTimer = directionChangeInterval * Random.Range(0.5f, 1.5f);
        }
    }
}


