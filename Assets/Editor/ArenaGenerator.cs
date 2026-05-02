using UnityEngine;
using UnityEditor;

public class ArenaGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Wave Survival Arena")]
    public static void GenerateArena()
    {
        // Destroy existing arena to prevent duplicates and overlapping colliders
        GameObject existingArena = GameObject.Find("WaveSurvivalArena");
        if (existingArena != null)
        {
            DestroyImmediate(existingArena);
        }

        GameObject arenaRoot = new GameObject("WaveSurvivalArena");
        
        // 1. Floor
        // Using a Cube instead of a Plane gives the floor thickness (BoxCollider)
        // This prevents high-velocity tunneling (falling through the floor) when landing from a jump.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(arenaRoot.transform);
        floor.transform.localScale = new Vector3(50f, 1f, 50f); 
        floor.transform.position = new Vector3(0f, -0.5f, 0f); // Top surface at y=0
        SetNavigationStatic(floor);

        // Apply Light Grey Material to Floor
        Material lightGreyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ArenaFloor.mat");
        
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        Shader standardShader = Shader.Find("Standard");
        Shader targetShader = urpShader != null ? urpShader : standardShader;

        if (lightGreyMat == null)
        {
            if (targetShader != null)
            {
                lightGreyMat = new Material(targetShader);
                lightGreyMat.color = Color.black; // Black floor
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                AssetDatabase.CreateAsset(lightGreyMat, "Assets/Materials/ArenaFloor.mat");
            }
        }
        else
        {
            // If the material already exists but has the wrong shader (e.g. from a previous generation), fix it
            if (lightGreyMat.shader != targetShader && targetShader != null)
            {
                lightGreyMat.shader = targetShader;
                EditorUtility.SetDirty(lightGreyMat);
                AssetDatabase.SaveAssets();
            }
        }
        
        if (lightGreyMat != null)
        {
            floor.GetComponent<MeshRenderer>().sharedMaterial = lightGreyMat;
        }

        // 2. Walls (Closed Arena)
        // Arena is 50x50, so from -25 to +25 in X and Z
        float wallHeight = 5f;
        float halfSize = 25f;
        
        CreateWall("Wall_North", new Vector3(0, wallHeight / 2, halfSize), new Vector3(50, wallHeight, 1), arenaRoot.transform);
        CreateWall("Wall_South", new Vector3(0, wallHeight / 2, -halfSize), new Vector3(50, wallHeight, 1), arenaRoot.transform);
        CreateWall("Wall_East", new Vector3(halfSize, wallHeight / 2, 0), new Vector3(1, wallHeight, 50), arenaRoot.transform);
        CreateWall("Wall_West", new Vector3(-halfSize, wallHeight / 2, 0), new Vector3(1, wallHeight, 50), arenaRoot.transform);

        // 3. Jumpable Obstacles
        // Jump force is 9.0. A jump height of 0.8 units is very easy to clear without snagging colliders.
        float obstacleHeight = 0.8f;
        
        // Large center platform
        CreateObstacle("Obstacle_Center", new Vector3(0, obstacleHeight / 2, 0), new Vector3(6, obstacleHeight, 6), arenaRoot.transform);
        
        // Barricades that act as cover or jump spots
        CreateObstacle("Obstacle_Barricade_1", new Vector3(-10, obstacleHeight / 2, 8), new Vector3(10, obstacleHeight, 1.5f), arenaRoot.transform);
        CreateObstacle("Obstacle_Barricade_2", new Vector3(10, obstacleHeight / 2, -8), new Vector3(10, obstacleHeight, 1.5f), arenaRoot.transform);
        
        // Some stepping stones/crates
        CreateObstacle("Obstacle_Crate_1", new Vector3(-8, obstacleHeight / 2, -12), new Vector3(2, obstacleHeight, 2), arenaRoot.transform);
        CreateObstacle("Obstacle_Crate_2", new Vector3(8, obstacleHeight / 2, 12), new Vector3(2, obstacleHeight, 2), arenaRoot.transform);
        
        // Taller obstacles that players/enemies CANNOT jump over (creates maze-like choke points)
        float highBlockerHeight = 3.5f;
        CreateObstacle("HighBlocker_1", new Vector3(15, highBlockerHeight / 2, 15), new Vector3(5, highBlockerHeight, 5), arenaRoot.transform);
        CreateObstacle("HighBlocker_2", new Vector3(-15, highBlockerHeight / 2, -15), new Vector3(5, highBlockerHeight, 5), arenaRoot.transform);

        Debug.Log("Wave Survival Arena generated! Please remember to bake your NavMesh (Window -> AI -> Navigation) so enemies can pathfind around the obstacles.");
    }

    private static void CreateWall(string name, Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.transform.SetParent(parent);
        
        SetNavigationStatic(wall);
        
        // Add dynamic NavMeshObstacle so zombies avoid walls even without baking
        UnityEngine.AI.NavMeshObstacle navObstacle = wall.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        navObstacle.carving = true;
    }

    private static void CreateObstacle(string name, Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;
        obstacle.transform.SetParent(parent);
        
        SetNavigationStatic(obstacle);

        // Add dynamic NavMeshObstacle so zombies avoid obstacles even without baking
        UnityEngine.AI.NavMeshObstacle navObstacle = obstacle.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        navObstacle.carving = true;
    }

    private static void SetNavigationStatic(GameObject obj)
    {
        // Mark the object as Navigation Static so the NavMesh baker recognizes it as an obstacle/walkable surface
        StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(obj);
        flags |= StaticEditorFlags.NavigationStatic;
        GameObjectUtility.SetStaticEditorFlags(obj, flags);
    }
}
