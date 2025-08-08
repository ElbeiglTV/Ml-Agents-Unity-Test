using UnityEditor;
using UnityEngine;
using Unity.MLAgents.Policies;

namespace MLNavigation.Editor
{
    public static class NavigationSetupEditor
    {
        [MenuItem("ML Navigation/Crear Prefab NavigationAgent")]
        public static void CreateAgentPrefab()
        {
            var go = new GameObject("NavigationAgent");
            go.AddComponent<Rigidbody>();
            go.AddComponent<CapsuleCollider>();
            go.AddComponent<BehaviorParameters>();
            var agent = go.AddComponent<MLNavigation.NavigationAgent>();
            var areaGO = new GameObject("NavigationArea");
            var area = areaGO.AddComponent<MLNavigation.NavigationArea>();

            agent.area = area;
            area.agent = agent;

            string folder = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            string path = folder + "/NavigationAgent.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(areaGO);
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("Prefab creado en: " + path);
        }

        [MenuItem("ML Navigation/Crear Escenas 2D y 3D")] 
        public static void CreateScenes()
        {
            CreateScene("Navigation2D", is3D: false);
            CreateScene("Navigation3D", is3D: true);
        }

        private static void CreateScene(string sceneName, bool is3D)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);

            var areaGO = new GameObject("NavigationArea");
            var area = areaGO.AddComponent<MLNavigation.NavigationArea>();
            if (!is3D)
            {
                // Top-down: mover cámara arriba
                var cam = Camera.main;
                if (cam != null)
                {
                    cam.transform.position = new Vector3(0f, 25f, 0f);
                    cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }

            // Agente
            var agentGO = new GameObject("NavigationAgent");
            agentGO.AddComponent<Rigidbody>();
            agentGO.AddComponent<CapsuleCollider>();
            agentGO.AddComponent<BehaviorParameters>();
            var agent = agentGO.AddComponent<MLNavigation.NavigationAgent>();
            agent.area = area;
            area.agent = agent;

            // Objetivo
            var targetSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetSphere.name = "Target";
            targetSphere.transform.localScale = Vector3.one;
            var renderer = targetSphere.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.sharedMaterial.color = Color.green;
            }
            var targetCtrl = targetSphere.AddComponent<MLNavigation.TargetController>();
            targetCtrl.area = area;
            area.target = targetSphere.transform;
            agent.targetTransform = targetSphere.transform;

            string path = "Assets/Scenes/" + sceneName + ".unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
            Debug.Log("Escena creada: " + path);
        }
    }
}


