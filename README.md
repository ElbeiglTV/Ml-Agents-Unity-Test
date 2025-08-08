# ML Agents Navigation (URP)

Este proyecto implementa un sistema de navegación con aprendizaje por refuerzo en Unity usando ML-Agents, sin NavMesh ni A*. El agente aprende a alcanzar un objetivo evitando obstáculos estáticos y móviles en mapas procedurales y generaliza sin memorizar layouts.

## Requisitos previos
- Unity 2021.3+ o 2022/2023 LTS con URP.
- Paquete ML-Agents instalado en el proyecto (Package Manager: `com.unity.ml-agents`).
- Python 3.8–3.10 y `mlagents` para entrenamiento:
  - `pip install mlagents==0.30.0` (ajusta a tu versión del paquete en Unity).

## Contenido añadido
- `Assets/Scripts/Navigation/NavigationAgent.cs`: agente ML-Agents con observaciones (dirección al objetivo, distancia, raycasts) y acciones continuas (X, Z).
- `Assets/Scripts/Navigation/NavigationArea.cs`: área procedural que genera obstáculos y resetea episodios.
- `Assets/Scripts/Navigation/MovingObstacle.cs`: obstáculo móvil con rebote en límites.
- `Assets/Scripts/Navigation/TargetController.cs`: objetivo que puede deambular aleatoriamente.
- `Assets/ML-Agents/Configs/nav_config.yaml`: configuración PPO lista para entrenar.
- `Assets/Editor/Navigation/NavigationSetupEditor.cs`: utilidades de Editor para crear prefab y escenas.

## Crear prefab y escenas
En el menú de Unity:
- `ML Navigation/Crear Prefab NavigationAgent` → genera `Assets/Prefabs/NavigationAgent.prefab`.
- `ML Navigation/Crear Escenas 2D y 3D` → crea `Assets/Scenes/Navigation2D.unity` y `Assets/Scenes/Navigation3D.unity`.

Cada escena incluye:
- Un `NavigationArea` que genera el suelo, obstáculos y controla reseteos.
- Un `NavigationAgent` con `Rigidbody` y `Agent`.
- Un `Target` con `TargetController` (puede moverse).

Si prefieres hacerlo manualmente:
1. Crea un `Empty` y añade `NavigationArea`.
2. Crea un `Empty` `NavigationAgent` con `Rigidbody`, `CapsuleCollider`, `BehaviorParameters` y `NavigationAgent`.
3. Crea una `Sphere` como `Target` y añade `TargetController`. Asigna referencias en el agente y el área.

## Entrenamiento
1. Abre la escena `Navigation3D` o `Navigation2D`.
2. En el objeto `NavigationAgent`, en `Behavior Parameters` ajusta `Behavior Name` a `Navigation` (ya se establece por script) y marca `Behavior Type: Default`. Opcionalmente, añade `Decision Requester` con `Decision Period = 5`.
3. Desde terminal en la carpeta del proyecto:
   ```bash
   mlagents-learn Assets/ML-Agents/Configs/nav_config.yaml --run-id=nav_run_001 --time-scale=20 --env-args --no-graphics
   ```
4. Pulsa Play en Unity para iniciar el entrenamiento.

Los modelos se guardan en `results/nav_run_001/Navigation/*.nn`. Copia el `.nn` al proyecto, por ejemplo a `Assets/ML-Agents/Models/Navigation.nn`.

## Inferencia (Play)
1. En el `NavigationAgent`, cambia `Behavior Type` a `Inference Only`.
2. Arrastra el `.nn` a `Model`.
3. Pulsa Play. El agente debería moverse hacia el objetivo evitando obstáculos.

## Recompensas y penalizaciones
- Recompensa incremental por acercarse al objetivo en cada paso.
- Penalización pequeña por paso para trayectos cortos.
- Recompensa al llegar al objetivo.
- Penalización y fin de episodio al colisionar con un obstáculo.

## Notas
- No se usa NavMesh ni A*.
- Raycasts radiales proporcionan conciencia espacial.
- Ajusta `numRays`, `rayDistance`, `moveSpeed`, `approachRewardScale` para estabilidad.

