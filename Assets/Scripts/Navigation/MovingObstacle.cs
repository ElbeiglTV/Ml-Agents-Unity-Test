using UnityEngine;

namespace MLNavigation
{
    [AddComponentMenu("ML Navigation/Moving Obstacle")]
    public class MovingObstacle : MonoBehaviour
    {
        [Tooltip("Área a la que pertenece el obstáculo, usada para delimitar su movimiento.")]
        public NavigationArea area;

        [Tooltip("Velocidad de desplazamiento en el plano XZ.")]
        public float speed = 2f;

        [Tooltip("Dirección de movimiento en el plano (normalizada).")]
        public Vector2 direction = Vector2.right;

        private void Update()
        {
            if (area == null) return;

            Vector3 pos = transform.position;
            Vector3 delta = new Vector3(direction.x, 0f, direction.y) * speed * Time.deltaTime;
            pos += delta;

            // Rebotar dentro de los límites del área
            float minX = area.transform.position.x - area.halfExtentX + 0.5f;
            float maxX = area.transform.position.x + area.halfExtentX - 0.5f;
            float minZ = area.transform.position.z - area.halfExtentZ + 0.5f;
            float maxZ = area.transform.position.z + area.halfExtentZ - 0.5f;

            if (pos.x < minX || pos.x > maxX)
            {
                direction.x *= -1f;
                pos.x = Mathf.Clamp(pos.x, minX, maxX);
            }
            if (pos.z < minZ || pos.z > maxZ)
            {
                direction.y *= -1f;
                pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
            }

            transform.position = pos;
        }
    }
}


