using System.Collections;
using System.IO;
using SyReality.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace SyReality.Unity
{
    /// <summary>ITimeSource backed by the engine's unscaled real time.</summary>
    public sealed class UnityTimeSource : ITimeSource
    {
        public double NowSeconds => Time.realtimeSinceStartupAsDouble;
    }

    /// <summary>
    /// Thin wrapper over AudioSource. The audio clock is the audible truth during a
    /// performance: PerformanceManager polls <see cref="AudioTimeSeconds"/> each
    /// frame and feeds it to the PlaybackClock for drift correction.
    ///
    /// Test mode: when a song has no audio file (AudioFileName empty) or the clip
    /// fails to load, the engine runs silent and the PlaybackClock free-runs,
    /// lyric sync still works in the silent test mode).
    /// Audio files are never bundled for copyrighted songs; users place their own
    /// licensed files in StreamingAssets/Audio (see that folder's README).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioEngine : MonoBehaviour
    {
        private AudioSource _source;

        public bool HasClip => _source != null && _source.clip != null;
        public bool IsPlaying => _source != null && _source.isPlaying;
        public double AudioTimeSeconds => _source != null ? _source.time : 0;
        public double ClipLengthSeconds => HasClip ? _source.clip.length : 0;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        /// <summary>Load a clip from StreamingAssets/Audio by file name (mp3/ogg/wav).</summary>
        public IEnumerator LoadFromStreamingAssets(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.Log("[SyReality] No audio file for this song. Silent test mode.");
                yield break;
            }
            string path = Path.Combine(Application.streamingAssetsPath, "Audio", fileName);
            var type = fileName.EndsWith(".mp3") ? AudioType.MPEG
                     : fileName.EndsWith(".ogg") ? AudioType.OGGVORBIS
                     : AudioType.WAV;
            using (var request = UnityWebRequestMultimedia.GetAudioClip("file://" + path, type))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    _source.clip = DownloadHandlerAudioClip.GetContent(request);
                    Debug.Log($"[SyReality] Loaded audio '{fileName}'.");
                }
                else
                {
                    Debug.LogWarning($"[SyReality] Could not load audio '{fileName}' " +
                        $"({request.error}); continuing in silent test mode.");
                }
            }
        }

        public void Play(double fromSeconds = 0)
        {
            if (!HasClip) return;
            _source.time = Mathf.Clamp((float)fromSeconds, 0, _source.clip.length);
            _source.Play();
        }

        public void Pause() { if (HasClip) _source.Pause(); }
        public void Resume() { if (HasClip) _source.UnPause(); }

        public void Stop()
        {
            if (HasClip) _source.Stop();
        }

        public void Seek(double seconds)
        {
            if (HasClip)
                _source.time = Mathf.Clamp((float)seconds, 0, _source.clip.length);
        }
    }
}
