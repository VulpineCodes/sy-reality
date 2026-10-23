using System;
using UnityEngine;

namespace SyReality.Unity
{
    /// <summary>
    /// Data for one performance environment. Environments are data, not code:
    /// lighting, fog, accent colors and which stage root to activate. New stages
    /// are added by creating an asset, not by editing the director.
    /// </summary>
    [CreateAssetMenu(menuName = "SyReality/Environment Profile", fileName = "EnvironmentProfile")]
    public sealed class EnvironmentProfile : ScriptableObject
    {
        public string displayName = "Neon Club";
        /// <summary>Name of the stage root GameObject this profile activates.</summary>
        public string stageRootName = "Stage_NeonClub";
        public Color ambientColor = new Color(0.15f, 0.12f, 0.25f);
        public Color fogColor = new Color(0.08f, 0.06f, 0.16f);
        [Range(0f, 0.1f)] public float fogDensity = 0.02f;
        public Color keyLightColor = new Color(0.9f, 0.4f, 1f);
        [Range(0f, 3f)] public float keyLightIntensity = 1.4f;
        public Color accentColor = new Color(1f, 0.3f, 0.8f);
        /// <summary>Which particle preset (child of the stage root named
        /// "Particles_<presetId>") to enable; empty = none.</summary>
        public string particlePresetId = "sparks";
    }

    /// <summary>Applies an <see cref="EnvironmentProfile"/>: activates its stage
    /// root, retunes lighting/fog/ambient, and enables its particle preset.</summary>
    public sealed class EnvironmentDirector : MonoBehaviour
    {
        [SerializeField] private Transform stagesRoot;
        [SerializeField] private Light keyLight;
        [SerializeField] private EnvironmentProfile[] profiles;

        public EnvironmentProfile Current { get; private set; }
        public event Action<EnvironmentProfile> EnvironmentChanged;

        private void Start()
        {
            if (profiles != null && profiles.Length > 0) Apply(profiles[0]);
        }

        public void Apply(EnvironmentProfile profile)
        {
            if (profile == null) return;
            Current = profile;

            if (stagesRoot != null)
            {
                foreach (Transform stage in stagesRoot)
                    stage.gameObject.SetActive(stage.name == profile.stageRootName);
            }

            RenderSettings.ambientLight = profile.ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogColor = profile.fogColor;
            RenderSettings.fogDensity = profile.fogDensity;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            if (keyLight != null)
            {
                keyLight.color = profile.keyLightColor;
                keyLight.intensity = profile.keyLightIntensity;
            }

            if (stagesRoot != null && !string.IsNullOrEmpty(profile.particlePresetId))
            {
                var stage = stagesRoot.Find(profile.stageRootName);
                if (stage != null)
                    foreach (Transform child in stage)
                        if (child.name.StartsWith("Particles_"))
                            child.gameObject.SetActive(
                                child.name == "Particles_" + profile.particlePresetId);
            }

            EnvironmentChanged?.Invoke(profile);
        }

        public void ApplyByIndex(int index)
        {
            if (profiles != null && index >= 0 && index < profiles.Length)
                Apply(profiles[index]);
        }
    }
}
