using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MyOvercooked.Editor
{
    public class AnimationSystemGenerator : EditorWindow
    {
        private string sourceFolder = "Assets/Sprites/sprites";
        private string outputBaseFolder = "Assets/Animations2";
        private bool overwriteExisting = true;
        private bool generateAnimator = true;

        [MenuItem("Tools/MyOvercooked/Generate Customer Animations")]
        public static void ShowWindow()
        {
            GetWindow<AnimationSystemGenerator>("Animation Generator");
        }

        [MenuItem("Tools/MyOvercooked/Generate Customer Animations (Quick Run)")]
        public static void QuickRun()
        {
            var window = CreateInstance<AnimationSystemGenerator>();
            window.sourceFolder = "Assets/Sprites/sprites";
            window.outputBaseFolder = "Assets/Animations2";
            window.overwriteExisting = true;
            window.generateAnimator = true;
            window.GenerateAnimations();
        }

        private void OnGUI()
        {
            GUILayout.Label("Animation Generator Settings", EditorStyles.boldLabel);

            sourceFolder = EditorGUILayout.TextField("Source Folder", sourceFolder);
            outputBaseFolder = EditorGUILayout.TextField("Output Folder", outputBaseFolder);

            GUILayout.Space(5);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Clips", overwriteExisting);
            generateAnimator = EditorGUILayout.Toggle("Generate Animator", generateAnimator);

            GUILayout.Space(15);

            if (GUILayout.Button("Generate Animations & Animator", GUILayout.Height(40)))
                GenerateAnimations();
        }

        private void GenerateAnimations()
        {
            if (!AssetDatabase.IsValidFolder(sourceFolder))
            {
                Debug.LogError($"[AnimationSystemGenerator] Source folder '{sourceFolder}' not found.");
                return;
            }

            EnsureFolder(outputBaseFolder, "Assets", Path.GetFileName(outputBaseFolder));

            string[] characterFolders = AssetDatabase.GetSubFolders(sourceFolder);
            if (characterFolders.Length == 0) characterFolders = new[] { sourceFolder };

            int created = 0, updated = 0;

            foreach (string charFolder in characterFolders)
            {
                string characterName = Path.GetFileName(charFolder);

                // Only process MaleCustomer and FemaleCustomer
                if (characterName != "MaleCustomer" && characterName != "FemaleCustomer")
                    continue;

                // --- Group PNGs by clip name using FILE PATH (not stale sprite.name) ---
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { charFolder });
                var clipPathGroups = new Dictionary<string, List<(string path, int frameIndex)>>();

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileNameWithoutExtension(path);

                    // Match pattern: ActionDirection_N  (e.g. RunDown_3, IdleUp_0)
                    Match m = Regex.Match(fileName, @"^(.+?)_(\d+)$");
                    if (!m.Success) continue;

                    string clipName = m.Groups[1].Value;          // "RunDown"
                    int frameIdx = int.Parse(m.Groups[2].Value);  // 3

                    if (!clipPathGroups.ContainsKey(clipName))
                        clipPathGroups[clipName] = new List<(string, int)>();
                    clipPathGroups[clipName].Add((path, frameIdx));
                }

                if (clipPathGroups.Count == 0)
                {
                    Debug.LogWarning($"[AnimationSystemGenerator] No sprite files found in {charFolder}");
                    continue;
                }

                string charOutputFolder = $"{outputBaseFolder}/{characterName}";
                EnsureFolder(charOutputFolder, outputBaseFolder, characterName);

                foreach (var kvp in clipPathGroups)
                {
                    string clipName = kvp.Key;
                    string clipPath = $"{charOutputFolder}/{clipName}.anim";

                    AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (existingClip != null && !overwriteExisting) continue;

                    // Sort frames by index, load sprites
                    List<Sprite> sprites = kvp.Value
                        .OrderBy(t => t.frameIndex)
                        .Select(t => LoadFirstSprite(t.path))
                        .Where(s => s != null)
                        .ToList();

                    if (sprites.Count == 0)
                    {
                        Debug.LogWarning($"[AnimationSystemGenerator] Could not load sprites for clip '{clipName}'");
                        continue;
                    }

                    bool isNew = existingClip == null;
                    CreateOrUpdateClip(clipPath, existingClip, sprites, clipName);
                    if (isNew) created++; else updated++;
                }

                if (generateAnimator)
                    GenerateAnimatorController(charOutputFolder, characterName);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[AnimationSystemGenerator]</color> Done! Created={created} Updated={updated}");
        }

        private static Sprite LoadFirstSprite(string assetPath)
        {
            // Try loading all sub-assets (sprite sheet or single sprite)
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Sprite s = all.OfType<Sprite>().FirstOrDefault();
            if (s != null) return s;

            // Fallback: load as Texture2D and get sprite directly
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null) return null;

            // Force import as sprite if needed
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                s = all.OfType<Sprite>().FirstOrDefault();
            }

            return s;
        }

        private static void CreateOrUpdateClip(string path, AnimationClip existing, List<Sprite> sprites, string clipName)
        {
            bool isNew = existing == null;
            AnimationClip clip = isNew ? new AnimationClip() : existing;

            clip.frameRate = 12f;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = IsLooping(clipName);
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            float timePerFrame = 1f / clip.frameRate;
            ObjectReferenceKeyframe[] keyframes = sprites
                .Select((sp, i) => new ObjectReferenceKeyframe { time = i * timePerFrame, value = sp })
                .ToArray();

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            if (isNew) AssetDatabase.CreateAsset(clip, path);
            else EditorUtility.SetDirty(clip);
        }

        private static bool IsLooping(string clipName)
        {
            string lower = clipName.ToLower();
            return lower.Contains("idle") || lower.Contains("run") || lower.Contains("walk");
        }

        private void GenerateAnimatorController(string folderPath, string characterName)
        {
            string controllerPath = $"{folderPath}/{characterName}_Animator.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            AddParam(controller, "MoveX", AnimatorControllerParameterType.Float);
            AddParam(controller, "MoveY", AnimatorControllerParameterType.Float);
            AddParam(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            AddParam(controller, "IsAngry", AnimatorControllerParameterType.Bool);
            AddParam(controller, "IsJoyful", AnimatorControllerParameterType.Bool);

            // Load all clips and group by action + direction
            string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });

            // actionName → { Vector2 dir → AnimationClip }
            var actionTrees = new Dictionary<string, Dictionary<Vector2, AnimationClip>>();

            foreach (string guid in clipGuids)
            {
                string clipPath = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null) continue;

                string name = clip.name; // e.g., "RunDown", "IdleUp", "AngryDown"

                // Parse direction suffix
                Vector2 dir;
                string action;
                if (name.EndsWith("Up"))        { dir = new Vector2(0, 1);  action = name[..^2]; }
                else if (name.EndsWith("Down"))  { dir = new Vector2(0, -1); action = name[..^4]; }
                else if (name.EndsWith("Left"))  { dir = new Vector2(-1, 0); action = name[..^4]; }
                else if (name.EndsWith("Right")) { dir = new Vector2(1, 0);  action = name[..^5]; }
                else continue;

                if (!actionTrees.ContainsKey(action))
                    actionTrees[action] = new Dictionary<Vector2, AnimationClip>();
                actionTrees[action][dir] = clip;
            }

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState idleState = null;
            AnimatorState runState = null;
            AnimatorState angryState = null;
            AnimatorState joyfulState = null;

            // Clear orphan blend trees to avoid duplicates
            foreach (var kv in actionTrees)
            {
                string stateName = GetStateName(kv.Key);
                var existing = sm.states.FirstOrDefault(s => s.state.name == stateName);
                if (existing.state != null)
                {
                    var oldTree = existing.state.motion as BlendTree;
                    if (oldTree != null) oldTree.children = new ChildMotion[0];
                }
            }

            foreach (var kv in actionTrees)
            {
                string action = kv.Key;
                var dirClips = kv.Value;
                string stateName = GetStateName(action);

                AnimatorState state = sm.states.FirstOrDefault(s => s.state.name == stateName).state;
                if (state == null)
                    state = sm.AddState(stateName);

                BlendTree tree = state.motion as BlendTree;
                if (tree == null)
                {
                    tree = new BlendTree
                    {
                        name = stateName,
                        blendType = BlendTreeType.SimpleDirectional2D,
                        blendParameter = "MoveX",
                        blendParameterY = "MoveY"
                    };
                    AssetDatabase.AddObjectToAsset(tree, controller);
                    state.motion = tree;
                }
                else
                {
                    tree.children = new ChildMotion[0];
                }

                foreach (var dc in dirClips)
                    tree.AddChild(dc.Value, dc.Key);

                switch (action)
                {
                    case "Idle":    idleState    = state; break;
                    case "Run":     runState     = state; break;
                    case "Angry":   angryState   = state; break;
                    case "Joyful":  joyfulState  = state; break;
                }
            }

            if (idleState != null)
                sm.defaultState = idleState;

            // Idle ↔ Run
            SetupTransition(idleState, runState,    "IsMoving", AnimatorConditionMode.If,    0);
            SetupTransition(runState,  idleState,   "IsMoving", AnimatorConditionMode.IfNot, 0);

            // Idle → Angry (any direction, not moving)
            SetupTransition(idleState,   angryState, "IsAngry", AnimatorConditionMode.If, 0);
            SetupTransition(angryState,  idleState,  "IsAngry", AnimatorConditionMode.IfNot, 0);

            // Idle → Joyful
            SetupTransition(idleState,   joyfulState, "IsJoyful", AnimatorConditionMode.If, 0);
            SetupTransition(joyfulState, idleState,   "IsJoyful", AnimatorConditionMode.IfNot, 0);

            EditorUtility.SetDirty(controller);
            Debug.Log($"<color=green>[AnimationSystemGenerator]</color> Animator updated: {characterName}");
        }

        private static string GetStateName(string action) => action switch
        {
            "Run"    => "Running_Tree",
            "Idle"   => "Idle_Tree",
            "Angry"  => "Angry_Tree",
            "Joyful" => "Joyful_Tree",
            _        => $"{action}_Tree"
        };

        private static void SetupTransition(AnimatorState from, AnimatorState to, string param, AnimatorConditionMode mode, float threshold)
        {
            if (from == null || to == null) return;
            if (from.transitions.Any(t => t.destinationState == to)) return;

            AnimatorStateTransition tr = from.AddTransition(to);
            tr.hasExitTime = false;
            tr.duration = 0.1f;
            tr.AddCondition(mode, threshold, param);
        }

        private static void AddParam(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
        {
            if (!ctrl.parameters.Any(p => p.name == name))
                ctrl.AddParameter(name, type);
        }

        private static void EnsureFolder(string fullPath, string parentPath, string folderName)
        {
            if (!AssetDatabase.IsValidFolder(fullPath))
                AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }
}
