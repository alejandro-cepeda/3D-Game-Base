#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ToughZombieBuilder
{
    private const string ToughZombieFolder = "Assets/assets/Tough_Zombie/Meshy_AI_Twisted_Big_Buffed_un_biped";
    private const string OutputControllerPath = "Assets/assets/Animator/tough_zombie.controller";
    private const string OutputResourcesPrefabPath = "Assets/Resources/Enemies/ToughZombie.prefab";
    private const string OutputProjectPrefabPath = "Assets/assets/Prefab/ToughZombie.prefab";
    private const string OutputMaterialPath = "Assets/assets/Tough_Zombie/ToughZombie.mat";

    [MenuItem("Tools/Zombies/Build Tough Zombie")]
    public static void Build()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Enemies");
        EnsureFolder("Assets/assets");
        EnsureFolder("Assets/assets/Prefab");

        string walkFbxPath = $"{ToughZombieFolder}/Meshy_AI_Twisted_Big_Buffed_un_biped_Animation_Walking_withSkin.fbx";
        string runFbxPath = $"{ToughZombieFolder}/Meshy_AI_Twisted_Big_Buffed_un_biped_Animation_Running_withSkin.fbx";
        EnsureLooping(walkFbxPath);
        EnsureLooping(runFbxPath);

        AnimationClip walk = LoadClip(walkFbxPath);
        AnimationClip run = LoadClip(runFbxPath);
        AnimationClip attack = LoadClip($"{ToughZombieFolder}/Meshy_AI_Twisted_Big_Buffed_un_biped_Animation_Skill_01_withSkin.fbx");
        AnimationClip death = LoadClip($"{ToughZombieFolder}/Meshy_AI_Twisted_Big_Buffed_un_biped_Animation_Dead_withSkin.fbx");

        AnimatorController controller = BuildController(walk, run, attack, death);

        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ToughZombieFolder}/Meshy_AI_Twisted_Big_Buffed_un_biped_Animation_Idle_withSkin.fbx");
        if (modelPrefab == null)
        {
            throw new System.Exception("Could not load tough zombie model prefab.");
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        instance.name = "ToughZombie";

        ApplyMaterials(instance);

        if (instance.GetComponent<Animator>() == null)
        {
            instance.AddComponent<Animator>();
        }

        var animator = instance.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        if (instance.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
        {
            instance.AddComponent<UnityEngine.AI.NavMeshAgent>();
        }

        if (instance.GetComponent<Collider>() == null)
        {
            instance.AddComponent<CapsuleCollider>();
        }

        CapsuleCollider? capsule = instance.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.radius = 0.6f;
            capsule.height = 2.4f;
            capsule.center = new Vector3(0f, 1.2f, 0f);
        }

        var enemy = instance.GetComponent<EnemyController>();
        if (enemy == null)
        {
            enemy = instance.AddComponent<EnemyController>();
        }

        SetSerializedFloat(enemy, "walkSpeed", 1.5f);
        SetSerializedFloat(enemy, "runSpeed", 2.5f);
        SetSerializedFloat(enemy, "sprintSpeed", 3.5f);
        SetSerializedBool(enemy, "debugAttacks", true);

        var health = instance.GetComponent<Health>();
        if (health == null)
        {
            health = instance.AddComponent<Health>();
        }

        SetSerializedInt(health, "maxHealth", 300);
        SetSerializedBool(health, "debugEvents", true);

        var pierce = instance.GetComponent<PierceCost>();
        if (pierce == null)
        {
            pierce = instance.AddComponent<PierceCost>();
        }

        SetSerializedInt(pierce, "cost", 5);

        var agent = instance.GetComponent<UnityEngine.AI.NavMeshAgent>();
        agent.speed = 2.0f;
        agent.acceleration = 6.0f;
        agent.stoppingDistance = 0.25f;
        agent.baseOffset = 0f;

        PrefabUtility.SaveAsPrefabAsset(instance, OutputProjectPrefabPath);
        PrefabUtility.SaveAsPrefabAsset(instance, OutputResourcesPrefabPath);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Built ToughZombie prefabs at {OutputProjectPrefabPath} and {OutputResourcesPrefabPath}");
    }

    private static void ApplyMaterials(GameObject root)
    {
        SkinnedMeshRenderer renderer = root.GetComponentInChildren<SkinnedMeshRenderer>();
        if (renderer == null)
        {
            return;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(OutputMaterialPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, OutputMaterialPath);
        }

        Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ToughZombieFolder}/Meshy_AI_Twisted_Big_Buffed_un_biped_texture_0.png");
        Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ToughZombieFolder}/Meshy_AI_Twisted_Big_Buffed_un_biped_texture_0_normal.png");
        Texture2D metallicMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ToughZombieFolder}/Meshy_AI_Twisted_Big_Buffed_un_biped_texture_0_metallic.png");

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", baseMap);
        }
        else if (mat.HasProperty("_MainTex"))
        {
            mat.SetTexture("_MainTex", baseMap);
        }

        if (normalMap != null)
        {
            if (mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normalMap);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        if (metallicMap != null)
        {
            if (mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", metallicMap);
                mat.EnableKeyword("_METALLICGLOSSMAP");
            }
            else if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", 0.25f);
            }
        }

        renderer.sharedMaterial = mat;
        EditorUtility.SetDirty(mat);
    }

    private static AnimatorController BuildController(AnimationClip walk, AnimationClip run, AnimationClip attack, AnimationClip death)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OutputControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(OutputControllerPath);
        }

        controller.parameters = new[]
        {
            new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float },
            new AnimatorControllerParameter { name = "Attack", type = AnimatorControllerParameterType.Trigger },
            new AnimatorControllerParameter { name = "Hit", type = AnimatorControllerParameterType.Trigger },
            new AnimatorControllerParameter { name = "Dead", type = AnimatorControllerParameterType.Trigger }
        };

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        sm.states = new ChildAnimatorState[0];
        sm.anyStateTransitions = new AnimatorStateTransition[0];

        BlendTree blendTree;
        AnimatorState locomotion = sm.AddState("locomotion", new Vector3(200, 80));
        locomotion.motion = CreateLocomotionBlendTree(walk, run, out blendTree);

        AnimatorState attackState = sm.AddState("attack", new Vector3(520, 20));
        attackState.motion = attack;

        AnimatorState deathState = sm.AddState("death", new Vector3(520, 260));
        deathState.motion = death;

        sm.defaultState = locomotion;

        AnimatorStateTransition toAttack = sm.AddAnyStateTransition(attackState);
        toAttack.hasExitTime = false;
        toAttack.duration = 0.1f;
        toAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        AnimatorStateTransition toDeath = sm.AddAnyStateTransition(deathState);
        toDeath.hasExitTime = false;
        toDeath.duration = 0.1f;
        toDeath.AddCondition(AnimatorConditionMode.If, 0, "Dead");

        AnimatorStateTransition attackReturn = attackState.AddTransition(locomotion);
        attackReturn.hasExitTime = true;
        attackReturn.exitTime = 0.9f;
        attackReturn.duration = 0.1f;

        return controller;
    }

    private static Motion CreateLocomotionBlendTree(AnimationClip walk, AnimationClip run, out BlendTree tree)
    {
        tree = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };

        tree.AddChild(walk, 0f);
        tree.AddChild(run, 1f);
        return tree;
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }

        throw new System.Exception($"No animation clip found in {fbxPath}");
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        string name = System.IO.Path.GetFileName(assetPath);
        if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }

    private static void SetSerializedInt(Object obj, string fieldName, int value)
    {
        SerializedObject so = new SerializedObject(obj);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerializedFloat(Object obj, string fieldName, float value)
    {
        SerializedObject so = new SerializedObject(obj);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SetSerializedBool(Object obj, string fieldName, bool value)
    {
        SerializedObject so = new SerializedObject(obj);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void EnsureLooping(string fbxPath)
    {
        AssetImporter importer = AssetImporter.GetAtPath(fbxPath);
        if (importer is not ModelImporter modelImporter)
        {
            return;
        }

        ModelImporterClipAnimation[] clips = modelImporter.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = modelImporter.defaultClipAnimations;
        }

        bool changed = false;
        for (int i = 0; i < clips.Length; i++)
        {
            if (!clips[i].loopTime)
            {
                clips[i].loopTime = true;
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        modelImporter.clipAnimations = clips;
        modelImporter.SaveAndReimport();
    }
}
#endif
