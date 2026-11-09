using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

namespace Peakingu.EditorTools
{
    public static class AnimationSetup
    {
        private const string SPRITESHEET_PATH = "Assets/Sprites/Pingu_Spritesheet.png";
        private const string ANIMATIONS_DIR = "Assets/Animations";
        private const string CONTROLLER_PATH = "Assets/Animations/PinguAnimatorController.controller";
        private const string PINGU_PREFAB_PATH = "Assets/Prefabs/Pingu.prefab";

        [MenuItem("Tools/Peakingu/17. Configurar Animaciones de Pingu (Idle, Run, Jump, Fall, Dash)")]
        public static void SetupPinguAnimations()
        {
            try
            {
                // 1. Cortar el Spritesheet en múltiples Sprites
                SliceSpritesheet();

                // 2. Crear Clips de Animación (Idle, Run, Jump, Fall, Dash)
                var clips = CreateAnimationClips();

                // 3. Crear el Animator Controller con parámetros y transiciones
                var controller = CreateAnimatorController(clips);

                // 4. Configurar el Prefab de Pingu con Animator y PinguAnimationController
                ConfigurePinguPrefab(controller);

                // 5. Configurar Pingu en las escenas activas
                UpdatePinguInScenes(controller);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[AnimationSetup] SUCCESS: Animaciones de Pingu (Idle, Run, Jump, Fall, Dash) y Animator Controller integrados exitosamente.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnimationSetup] Error al configurar animaciones: {ex}");
            }
        }

        #region Paso 1: Cortar Spritesheet
        private static void SliceSpritesheet()
        {
            if (!File.Exists(SPRITESHEET_PATH))
            {
                Debug.LogError($"[AnimationSetup] No se encontró {SPRITESHEET_PATH}");
                return;
            }

            var importer = AssetImporter.GetAtPath(SPRITESHEET_PATH) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            int cellW = 32, cellH = 32;
            int texW = 192, texH = 160;

            // Filas:
            // Fila 0 (arriba, y=128..160): Idle (4 frames)
            // Fila 1 (y=96..128): Run (6 frames)
            // Fila 2 (y=64..96): Jump (3 frames)
            // Fila 3 (y=32..64): Fall (3 frames)
            // Fila 4 (y=0..32): Dash (4 frames)

            var animRows = new (string name, int row, int frameCount)[]
            {
                ("Pingu_Idle", 0, 4),
                ("Pingu_Run",  1, 6),
                ("Pingu_Jump", 2, 3),
                ("Pingu_Fall", 3, 3),
                ("Pingu_Dash", 4, 4),
            };

            List<SpriteMetaData> metaList = new List<SpriteMetaData>();

            foreach (var item in animRows)
            {
                int yMin = texH - (item.row + 1) * cellH;

                for (int col = 0; col < item.frameCount; col++)
                {
                    SpriteMetaData meta = new SpriteMetaData
                    {
                        name = $"{item.name}_{col}",
                        rect = new Rect(col * cellW, yMin, cellW, cellH),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    };
                    metaList.Add(meta);
                }
            }

            importer.spritesheet = metaList.ToArray();
            importer.SaveAndReimport();

            Debug.Log($"[AnimationSetup] Spritesheet cortado en {metaList.Count} sub-sprites.");
        }
        #endregion

        #region Paso 2: Crear AnimationClips
        private static Dictionary<string, AnimationClip> CreateAnimationClips()
        {
            if (!Directory.Exists(ANIMATIONS_DIR))
            {
                Directory.CreateDirectory(ANIMATIONS_DIR);
            }

            // Cargar todos los sprites cortados
            var allSprites = AssetDatabase.LoadAllAssetsAtPath(SPRITESHEET_PATH)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToList();

            var clips = new Dictionary<string, AnimationClip>();

            clips["Idle"] = BuildClip("Pingu_Idle", allSprites.Where(s => s.name.StartsWith("Pingu_Idle_")).ToArray(), fps: 6f, loop: true);
            clips["Run"]  = BuildClip("Pingu_Run",  allSprites.Where(s => s.name.StartsWith("Pingu_Run_")).ToArray(),  fps: 10f, loop: true);
            clips["Jump"] = BuildClip("Pingu_Jump", allSprites.Where(s => s.name.StartsWith("Pingu_Jump_")).ToArray(), fps: 8f, loop: false);
            clips["Fall"] = BuildClip("Pingu_Fall", allSprites.Where(s => s.name.StartsWith("Pingu_Fall_")).ToArray(), fps: 8f, loop: true);
            clips["Dash"] = BuildClip("Pingu_Dash", allSprites.Where(s => s.name.StartsWith("Pingu_Dash_")).ToArray(), fps: 12f, loop: true);

            return clips;
        }

        private static AnimationClip BuildClip(string clipName, Sprite[] frames, float fps, bool loop)
        {
            string path = Path.Combine(ANIMATIONS_DIR, $"{clipName}.anim");
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = fps;

            // Configurar Loop
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Asignar curva de SpriteRenderer
            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps,
                    value = frames[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            EditorUtility.SetDirty(clip);

            Debug.Log($"[AnimationSetup] Clip '{clipName}' creado con {frames.Length} frames a {fps} FPS (Loop={loop}).");
            return clip;
        }
        #endregion

        #region Paso 3: Crear Animator Controller
        private static AnimatorController CreateAnimatorController(Dictionary<string, AnimationClip> clips)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

            // Parámetros
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsDashing", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Crear Estados
            var idleState = rootStateMachine.AddState("Idle", new Vector3(250, 0, 0));
            idleState.motion = clips["Idle"];

            var runState = rootStateMachine.AddState("Run", new Vector3(250, 100, 0));
            runState.motion = clips["Run"];

            var jumpState = rootStateMachine.AddState("Jump", new Vector3(500, -50, 0));
            jumpState.motion = clips["Jump"];

            var fallState = rootStateMachine.AddState("Fall", new Vector3(500, 50, 0));
            fallState.motion = clips["Fall"];

            var dashState = rootStateMachine.AddState("Dash", new Vector3(250, -100, 0));
            dashState.motion = clips["Dash"];

            rootStateMachine.defaultState = idleState;

            // Transiciones:
            // 1. AnyState -> Dash (cuando IsDashing == true)
            var anyToDash = rootStateMachine.AddAnyStateTransition(dashState);
            anyToDash.AddCondition(AnimatorConditionMode.If, 0, "IsDashing");
            anyToDash.hasExitTime = false;
            anyToDash.duration = 0.05f;
            anyToDash.canTransitionToSelf = false;

            // 2. Dash -> Idle (cuando IsDashing == false && IsGrounded == true)
            var dashToIdle = dashState.AddTransition(idleState);
            dashToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDashing");
            dashToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            dashToIdle.hasExitTime = false;
            dashToIdle.duration = 0.05f;

            // 3. Dash -> Fall (cuando IsDashing == false && IsGrounded == false)
            var dashToFall = dashState.AddTransition(fallState);
            dashToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDashing");
            dashToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            dashToFall.hasExitTime = false;
            dashToFall.duration = 0.05f;

            // 4. Idle <-> Run
            var idleToRun = idleState.AddTransition(runState);
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");
            idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.05f;

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");
            runToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.05f;

            // 5. Salto: Idle / Run -> Jump (en el aire ascendiendo)
            var idleToJump = idleState.AddTransition(jumpState);
            idleToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            idleToJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");
            idleToJump.hasExitTime = false;
            idleToJump.duration = 0.05f;

            var runToJump = runState.AddTransition(jumpState);
            runToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            runToJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");
            runToJump.hasExitTime = false;
            runToJump.duration = 0.05f;

            // 6. Jump -> Fall (al empezar a descender)
            var jumpToFall = jumpState.AddTransition(fallState);
            jumpToFall.AddCondition(AnimatorConditionMode.Less, 0.0f, "VerticalVelocity");
            jumpToFall.hasExitTime = false;
            jumpToFall.duration = 0.05f;

            // 7. Caída directa: Idle / Run -> Fall (ej: caer de plataforma sin saltar)
            var idleToFall = idleState.AddTransition(fallState);
            idleToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            idleToFall.AddCondition(AnimatorConditionMode.Less, 0.0f, "VerticalVelocity");
            idleToFall.hasExitTime = false;
            idleToFall.duration = 0.05f;

            var runToFall = runState.AddTransition(fallState);
            runToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            runToFall.AddCondition(AnimatorConditionMode.Less, 0.0f, "VerticalVelocity");
            runToFall.hasExitTime = false;
            runToFall.duration = 0.05f;

            // 8. Fall -> Idle / Run (aterrizaje)
            var fallToIdle = fallState.AddTransition(idleState);
            fallToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            fallToIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");
            fallToIdle.hasExitTime = false;
            fallToIdle.duration = 0.05f;

            var fallToRun = fallState.AddTransition(runState);
            fallToRun.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            fallToRun.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");
            fallToRun.hasExitTime = false;
            fallToRun.duration = 0.05f;

            EditorUtility.SetDirty(controller);
            Debug.Log($"[AnimationSetup] Animator Controller creado en {CONTROLLER_PATH} con 5 estados y transiciones automáticas.");
            return controller;
        }
        #endregion

        #region Paso 4: Configurar Prefab de Pingu
        private static void ConfigurePinguPrefab(AnimatorController controller)
        {
            var pinguPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PINGU_PREFAB_PATH);
            if (pinguPrefab == null) return;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(PINGU_PREFAB_PATH))
            {
                var root = scope.prefabContentsRoot;

                // 1. Animator
                var anim = root.GetComponent<Animator>();
                if (anim == null) anim = root.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;

                // 2. PinguAnimationController
                var pAnimCtrl = root.GetComponent<PinguAnimationController>();
                if (pAnimCtrl == null) pAnimCtrl = root.AddComponent<PinguAnimationController>();

                // 3. Sprite inicial Idle
                var allSprites = AssetDatabase.LoadAllAssetsAtPath(SPRITESHEET_PATH).OfType<Sprite>().ToList();
                var idleSprite = allSprites.FirstOrDefault(s => s.name == "Pingu_Idle_0");

                var sr = root.GetComponent<SpriteRenderer>();
                if (sr != null && idleSprite != null)
                {
                    sr.sprite = idleSprite;
                }
            }

            AssetDatabase.SaveAssetIfDirty(pinguPrefab);
            Debug.Log($"[AnimationSetup] Prefab {PINGU_PREFAB_PATH} actualizado con Animator y PinguAnimationController.");
        }
        #endregion

        #region Paso 5: Actualizar Pingu en Escenas
        private static void UpdatePinguInScenes(AnimatorController controller)
        {
            string[] scenes = new string[]
            {
                "Assets/Scenes/Cueva.unity",
                "Assets/Scenes/Nivel1.unity"
            };

            var allSprites = AssetDatabase.LoadAllAssetsAtPath(SPRITESHEET_PATH).OfType<Sprite>().ToList();
            var idleSprite = allSprites.FirstOrDefault(s => s.name == "Pingu_Idle_0");

            foreach (var scenePath in scenes)
            {
                if (!File.Exists(scenePath)) continue;

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var pingu = GameObject.Find("Pingu");
                if (pingu != null)
                {
                    var anim = pingu.GetComponent<Animator>();
                    if (anim == null) anim = pingu.AddComponent<Animator>();
                    anim.runtimeAnimatorController = controller;

                    var pAnimCtrl = pingu.GetComponent<PinguAnimationController>();
                    if (pAnimCtrl == null) pAnimCtrl = pingu.AddComponent<PinguAnimationController>();

                    var sr = pingu.GetComponent<SpriteRenderer>();
                    if (sr != null && idleSprite != null)
                    {
                        sr.sprite = idleSprite;
                    }

                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[AnimationSetup] Pingu actualizado en escena: {scenePath}");
                }
            }
        }
        #endregion
    }
}
