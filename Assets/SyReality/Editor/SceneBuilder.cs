using System.IO;
using SyReality.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SyReality.Editor
{
    /// <summary>
    /// Builds Assets/Scenes/MainStage.unity entirely from code (menu:
    /// SyReality → Build MainStage Scene) so the scene is reproducible from the
    /// repository. No hand-authored binary scene YAML. Also generates the three
    /// environment profile assets. See Assets/Scenes/README.md for the composition
    /// this produces and the manual wiring checklist.
    ///
    /// The XR rig here is a placeholder (Camera Offset + camera + two controller
    /// cubes). On first real device work, replace "XR Rig (Placeholder)" with the
    /// XR Interaction Toolkit "XR Origin (XR Rig)" prefab. PerformanceManager and
    /// the UI are rig-agnostic.
    /// </summary>
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainStage.unity";
        private const string ProfileFolder = "Assets/SyReality/Data";

        [MenuItem("SyReality/Build MainStage Scene")]
        public static void BuildMainStage()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory(ProfileFolder);
            var profiles = CreateProfiles();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Lighting ---
            var lightGo = new GameObject("Key Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // --- XR rig placeholder ---
            var rig = new GameObject("XR Rig (Placeholder)");
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(rig.transform);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetParent(offset.transform);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            MakeController(offset.transform, "Left Controller", -0.25f);
            MakeController(offset.transform, "Right Controller", 0.25f);

            // --- Stages (one root per environment profile) ---
            var stages = new GameObject("Stages");
            MakeStage(stages.transform, "Stage_NeonClub", new Color(0.35f, 0.1f, 0.55f), "sparks");
            MakeStage(stages.transform, "Stage_SunsetRooftop", new Color(0.95f, 0.5f, 0.25f), "embers");
            MakeStage(stages.transform, "Stage_CosmicStage", new Color(0.1f, 0.2f, 0.6f), "stardust");

            // --- Managers ---
            var managers = new GameObject("Managers");
            var audioGo = new GameObject("AudioEngine");
            audioGo.transform.SetParent(managers.transform);
            var audio = audioGo.AddComponent<AudioEngine>();
            audioGo.AddComponent<AudioSource>();

            // --- World-space lyric canvas in front of the stage ---
            var canvasGo = new GameObject("Lyric Canvas");
            canvasGo.transform.position = new Vector3(0f, 1.7f, 3.2f);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(900f, 420f);
            canvasRect.localScale = Vector3.one * 0.0025f;

            var previous = MakeText(canvasGo.transform, "Previous Line", 34, new Vector2(0, 120));
            var current = MakeText(canvasGo.transform, "Current Line", 64, new Vector2(0, 20));
            var next = MakeText(canvasGo.transform, "Next Line", 34, new Vector2(0, -80));

            var barGo = new GameObject("Progress Bar");
            barGo.transform.SetParent(canvasGo.transform, false);
            var barRect = barGo.AddComponent<RectTransform>();
            barRect.anchoredPosition = new Vector2(0, -150);
            barRect.sizeDelta = new Vector2(700, 14);
            var barBg = barGo.AddComponent<Image>();
            barBg.color = new Color(1, 1, 1, 0.2f);
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barGo.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fill = fillGo.AddComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;

            var display = canvasGo.AddComponent<LyricDisplay>();
            var displaySo = new SerializedObject(display);
            displaySo.FindProperty("previousLineText").objectReferenceValue = previous;
            displaySo.FindProperty("currentLineText").objectReferenceValue = current;
            displaySo.FindProperty("nextLineText").objectReferenceValue = next;
            displaySo.FindProperty("progressBar").objectReferenceValue = fill;
            displaySo.ApplyModifiedPropertiesWithoutUndo();

            // --- Song library panel ---
            var panel = new GameObject("Song Library Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var listRoot = new GameObject("Song List");
            listRoot.transform.SetParent(panel.transform, false);
            listRoot.AddComponent<VerticalLayoutGroup>();
            var library = panel.AddComponent<SongLibraryUI>();
            var librarySo = new SerializedObject(library);
            librarySo.FindProperty("listRoot").objectReferenceValue = listRoot.transform;
            librarySo.FindProperty("panelRoot").objectReferenceValue = panel;
            librarySo.ApplyModifiedPropertiesWithoutUndo();

            // --- Performance manager + environment director wiring ---
            var pm = managers.AddComponent<PerformanceManager>();
            var pmSo = new SerializedObject(pm);
            pmSo.FindProperty("audioEngine").objectReferenceValue = audio;
            pmSo.FindProperty("lyricDisplay").objectReferenceValue = display;
            pmSo.FindProperty("songLibraryUI").objectReferenceValue = library;
            pmSo.ApplyModifiedPropertiesWithoutUndo();
            var librarySo2 = new SerializedObject(library);
            librarySo2.FindProperty("performanceManager").objectReferenceValue = pm;
            librarySo2.ApplyModifiedPropertiesWithoutUndo();

            var directorGo = new GameObject("EnvironmentDirector");
            directorGo.transform.SetParent(managers.transform);
            var director = directorGo.AddComponent<EnvironmentDirector>();
            var dirSo = new SerializedObject(director);
            dirSo.FindProperty("stagesRoot").objectReferenceValue = stages.transform;
            dirSo.FindProperty("keyLight").objectReferenceValue = light;
            var profilesProp = dirSo.FindProperty("profiles");
            profilesProp.arraySize = profiles.Length;
            for (int i = 0; i < profiles.Length; i++)
                profilesProp.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];
            dirSo.ApplyModifiedPropertiesWithoutUndo();

            // Song button prefab (simple TMP button).
            var prefab = MakeSongButtonPrefab();
            var libSo3 = new SerializedObject(library);
            libSo3.FindProperty("songButtonPrefab").objectReferenceValue =
                prefab.GetComponent<Button>();
            libSo3.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[SyReality] MainStage scene built at {ScenePath}. " +
                      "Assign a TMP font asset if the default font is missing, " +
                      "then verify wiring per Assets/Scenes/README.md.");
        }

        private static void MakeController(Transform parent, string name, float x)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(x, 1.3f, 0.25f);
            go.transform.localScale = Vector3.one * 0.06f;
        }

        private static void MakeStage(Transform parent, string name, Color color, string particles)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Stage Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(0f, 0.75f, 6f);
            floor.transform.localScale = new Vector3(8f, 0.15f, 5f);
            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard")) { color = color };
                renderer.sharedMaterial = mat;
            }
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "Backdrop";
            backdrop.transform.SetParent(root.transform);
            backdrop.transform.position = new Vector3(0f, 2.5f, 8.4f);
            backdrop.transform.localScale = new Vector3(9f, 5f, 0.2f);
            var particlesGo = new GameObject("Particles_" + particles);
            particlesGo.transform.SetParent(root.transform);
            var ps = particlesGo.AddComponent<ParticleSystem>();
            ps.Stop();
            root.SetActive(false);
        }

        private static TextMeshProUGUI MakeText(Transform parent, string name, int size, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(860, 100);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = string.Empty;
            return tmp;
        }

        private static GameObject MakeSongButtonPrefab()
        {
            var go = new GameObject("SongButton");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(700, 70);
            var image = go.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0.12f);
            go.AddComponent<Button>();
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.fontSize = 30;
            label.alignment = TextAlignmentOptions.Center;
            string path = "Assets/SyReality/Data/SongButton.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static EnvironmentProfile[] CreateProfiles()
        {
            var defs = new[]
            {
                new { Name = "Neon Club", Root = "Stage_NeonClub", Particles = "sparks",
                      Ambient = new Color(0.15f, 0.12f, 0.25f), Fog = new Color(0.08f, 0.06f, 0.16f),
                      Key = new Color(0.9f, 0.4f, 1f), Accent = new Color(1f, 0.3f, 0.8f) },
                new { Name = "Sunset Rooftop", Root = "Stage_SunsetRooftop", Particles = "embers",
                      Ambient = new Color(0.35f, 0.22f, 0.15f), Fog = new Color(0.3f, 0.16f, 0.1f),
                      Key = new Color(1f, 0.6f, 0.3f), Accent = new Color(1f, 0.55f, 0.2f) },
                new { Name = "Cosmic Stage", Root = "Stage_CosmicStage", Particles = "stardust",
                      Ambient = new Color(0.05f, 0.08f, 0.2f), Fog = new Color(0.02f, 0.03f, 0.1f),
                      Key = new Color(0.4f, 0.7f, 1f), Accent = new Color(0.4f, 0.9f, 1f) },
            };
            var result = new EnvironmentProfile[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                string path = $"{ProfileFolder}/Environment_{defs[i].Root}.asset";
                var profile = AssetDatabase.LoadAssetAtPath<EnvironmentProfile>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<EnvironmentProfile>();
                    AssetDatabase.CreateAsset(profile, path);
                }
                profile.displayName = defs[i].Name;
                profile.stageRootName = defs[i].Root;
                profile.particlePresetId = defs[i].Particles;
                profile.ambientColor = defs[i].Ambient;
                profile.fogColor = defs[i].Fog;
                profile.fogDensity = 0.02f;
                profile.keyLightColor = defs[i].Key;
                profile.keyLightIntensity = 1.4f;
                profile.accentColor = defs[i].Accent;
                EditorUtility.SetDirty(profile);
                result[i] = profile;
            }
            AssetDatabase.SaveAssets();
            return result;
        }
    }
}
