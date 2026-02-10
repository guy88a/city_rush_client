using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityRush.Core.Services
{
    public sealed class AudioService : IAudioService
    {
        private const string RootName = "__AudioRoot";

        private readonly Dictionary<SoundCategory, CategoryPool> _pools = new();
        private readonly Dictionary<int, PoolEntry> _entriesById = new();
        private readonly Dictionary<AmbientType, PoolEntry> _ambientByType = new();

        private IReadOnlyList<AudioClip> _musicPlaylist;
        private bool _musicLoopPlaylist;
        private int _musicIndex;

        private AudioSource _musicSource;

        private bool _globalPaused;
        private bool _ambientPaused;

        private int _nextEntryId = 0;

        public AudioService()
        {
            EnsureRootAndHost(out var root, out var host);
            BuildPools(root.transform);
            host.Bind(this);
        }

        public void PlayOneShot(SoundCategory category, AudioClip clip, float volume01 = 1f)
            => PlayOneShot(category, clip, volume01, 1f);

        public void PlayOneShot(SoundCategory category, AudioClip clip, float volume01, float pitch)
        {
            if (clip == null)
                return;

            if (!_pools.TryGetValue(category, out var pool))
                return;

            // Music/Ambient are not played via one-shot
            if (category == SoundCategory.Music || category == SoundCategory.Ambient)
                return;

            var entry = AcquireForOneShot(pool);
            if (entry == null)
                return;

            StartEntry(entry, clip, loop: false, volume01: volume01, pitch: pitch);
        }

        public void PlayOneShot(SoundCategory category, AudioClip clip, float volume01, float pitchMin, float pitchMax)
        {
            if (clip == null)
                return;

            float pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
            PlayOneShot(category, clip, volume01, pitch);
        }

        public SoundHandle PlayLoop(SoundCategory category, AudioClip clip, float volume01 = 1f, float pitch = 1f)
        {
            if (clip == null)
                return SoundHandle.Invalid;

            if (!_pools.TryGetValue(category, out var pool))
                return SoundHandle.Invalid;

            // Music/Ambient handled by dedicated APIs
            if (category == SoundCategory.Music || category == SoundCategory.Ambient)
                return SoundHandle.Invalid;

            var entry = AcquireForLoop(pool);
            if (entry == null)
                return SoundHandle.Invalid;

            entry.ReservedLoop = true;
            StartEntry(entry, clip, loop: true, volume01: volume01, pitch: pitch);

            return new SoundHandle(entry.Id, entry.Generation);
        }

        public void Stop(SoundHandle handle)
        {
            if (!TryGetValidHandleEntry(handle, out var entry))
                return;

            entry.Source.Stop();
            entry.Source.clip = null;

            entry.ReservedLoop = false;
            entry.LocalVolume = 1f;
            entry.LocalPitch = 1f;
            entry.StartDspTime = 0;
        }

        public void SetVolume(SoundHandle handle, float volume01)
        {
            if (!TryGetValidHandleEntry(handle, out var entry))
                return;

            entry.LocalVolume = Mathf.Clamp01(volume01);
            ApplyEntryMix(entry);
        }

        public void SetPitch(SoundHandle handle, float pitch)
        {
            if (!TryGetValidHandleEntry(handle, out var entry))
                return;

            entry.LocalPitch = Mathf.Clamp(pitch, 0.1f, 3f);
            entry.Source.pitch = entry.LocalPitch;
        }

        public void StartAmbient(AmbientType type, AudioClip clip, float volume01 = 1f, float pitch = 1f)
        {
            if (!_ambientByType.TryGetValue(type, out var entry))
                return;

            if (clip == null)
            {
                StopAmbient(type);
                return;
            }

            entry.ReservedLoop = true;
            StartEntry(entry, clip, loop: true, volume01: Mathf.Clamp01(volume01), pitch: Mathf.Clamp(pitch, 0.1f, 3f));

            if (_ambientPaused || _globalPaused)
                entry.Source.Pause();
        }

        public void StopAmbient(AmbientType type)
        {
            if (!_ambientByType.TryGetValue(type, out var entry))
                return;

            entry.Source.Stop();
            entry.Source.clip = null;

            entry.ReservedLoop = false;
            entry.LocalVolume = 1f;
            entry.LocalPitch = 1f;
            entry.StartDspTime = 0;
        }

        public void StopAllAmbient()
        {
            foreach (var kv in _ambientByType)
                StopAmbient(kv.Key);
        }

        public void PauseAllAmbient(bool paused)
        {
            _ambientPaused = paused;

            foreach (var kv in _ambientByType)
            {
                var src = kv.Value.Source;
                if (src == null)
                    continue;

                if (paused)
                {
                    if (src.isPlaying)
                        src.Pause();
                }
                else
                {
                    if (!_globalPaused && src.clip != null)
                        src.UnPause();
                }
            }
        }

        public void ApplyAreaAmbient(AudioAreaProfile profile)
        {
            // 1) Clear current ambient
            StopAllAmbient();

            if (profile == null || profile.AmbientLayers == null)
                return;

            // 2) Apply new layers
            for (int i = 0; i < profile.AmbientLayers.Length; i++)
            {
                var layer = profile.AmbientLayers[i];
                if (layer.Clip == null)
                    continue;

                StartAmbient(layer.Type, layer.Clip, layer.Volume01);
            }

            // 3) Respect current pause rules
            if (_ambientPaused || _globalPaused)
                PauseAllAmbient(true);
        }

        public void SetMusicPlaylist(IReadOnlyList<AudioClip> tracks, bool loopPlaylist)
        {
            _musicPlaylist = tracks;
            _musicLoopPlaylist = loopPlaylist;
            _musicIndex = 0;
        }

        public void PlayMusic()
        {
            if (_musicSource == null)
                return;

            var next = GetCurrentPlaylistTrack();
            if (next == null)
                return;

            _musicSource.clip = next;
            _musicSource.loop = false;
            ApplyMusicMix();
            _musicSource.Play();

            if (_globalPaused)
                _musicSource.Pause();
        }

        public void StopMusic()
        {
            if (_musicSource == null)
                return;

            _musicSource.Stop();
            _musicSource.clip = null;
        }

        public void NextTrack()
        {
            AdvancePlaylistIndex();
            PlayMusic();
        }

        public void SetCategoryVolume(SoundCategory category, float volume01)
        {
            if (!_pools.TryGetValue(category, out var pool))
                return;

            pool.Volume01 = Mathf.Clamp01(volume01);
            pool.ApplyMixToAll();
        }

        public void MuteCategory(SoundCategory category, bool isMuted)
        {
            if (!_pools.TryGetValue(category, out var pool))
                return;

            pool.Muted = isMuted;
            pool.ApplyMixToAll();
        }

        public void PauseAll(bool paused)
        {
            _globalPaused = paused;

            foreach (var pool in _pools.Values)
            {
                foreach (var entry in pool.Entries)
                {
                    if (entry.Source == null)
                        continue;

                    if (paused)
                    {
                        if (entry.Source.isPlaying)
                            entry.Source.Pause();
                    }
                    else
                    {
                        if (entry.Source.clip == null)
                            continue;

                        bool isAmbient = entry.Category == SoundCategory.Ambient;
                        if (isAmbient && _ambientPaused)
                            continue;

                        entry.Source.UnPause();
                    }
                }
            }
        }

        internal void Tick()
        {
            if (_globalPaused)
                return;

            AutoAdvanceMusicIfNeeded();
        }

        private void AutoAdvanceMusicIfNeeded()
        {
            if (_musicSource == null)
                return;

            if (_musicSource.clip == null)
                return;

            if (_musicSource.isPlaying)
                return;

            if (_musicPlaylist == null || _musicPlaylist.Count == 0)
                return;

            if (_musicIndex >= _musicPlaylist.Count - 1)
            {
                if (_musicLoopPlaylist)
                {
                    _musicIndex = 0;
                    PlayMusic();
                }
                else
                {
                    StopMusic();
                }

                return;
            }

            _musicIndex++;
            PlayMusic();
        }

        private AudioClip GetCurrentPlaylistTrack()
        {
            if (_musicPlaylist == null || _musicPlaylist.Count == 0)
                return null;

            if (_musicIndex < 0)
                _musicIndex = 0;

            if (_musicIndex >= _musicPlaylist.Count)
                _musicIndex = 0;

            return _musicPlaylist[_musicIndex];
        }

        private void AdvancePlaylistIndex()
        {
            if (_musicPlaylist == null || _musicPlaylist.Count == 0)
                return;

            _musicIndex++;

            if (_musicIndex >= _musicPlaylist.Count)
                _musicIndex = _musicLoopPlaylist ? 0 : _musicPlaylist.Count - 1;
        }

        private void EnsureRootAndHost(out GameObject root, out AudioServiceHost host)
        {
            root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                UnityEngine.Object.DontDestroyOnLoad(root);
            }

            host = root.GetComponent<AudioServiceHost>();
            if (host == null)
                host = root.AddComponent<AudioServiceHost>();
        }

        private void BuildPools(Transform root)
        {
            CreatePool(root, SoundCategory.Music, 2);
            CreatePool(root, SoundCategory.UI, 6);
            CreatePool(root, SoundCategory.SFX, 16);
            CreatePool(root, SoundCategory.Voice, 8);

            int ambientCount = Enum.GetValues(typeof(AmbientType)).Length;
            CreatePool(root, SoundCategory.Ambient, ambientCount);

            _musicSource = _pools[SoundCategory.Music].Entries[0].Source;

            BuildAmbientTypeMap();
        }

        private void BuildAmbientTypeMap()
        {
            _ambientByType.Clear();

            if (!_pools.TryGetValue(SoundCategory.Ambient, out var pool))
                return;

            var types = (AmbientType[])Enum.GetValues(typeof(AmbientType));
            for (int i = 0; i < types.Length; i++)
            {
                if (i >= pool.Entries.Count)
                    break;

                _ambientByType[types[i]] = pool.Entries[i];
            }
        }

        private void CreatePool(Transform root, SoundCategory category, int size)
        {
            if (_pools.ContainsKey(category))
                return;

            var poolRoot = new GameObject(category.ToString());
            poolRoot.transform.SetParent(root, worldPositionStays: false);

            var pool = new CategoryPool(category, poolRoot.transform, this);
            _pools[category] = pool;

            for (int i = 0; i < size; i++)
            {
                var go = new GameObject($"{category}_Source_{i}");
                go.transform.SetParent(poolRoot.transform, worldPositionStays: false);

                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;

                var entry = new PoolEntry(
                    id: _nextEntryId++,
                    category: category,
                    source: src
                );

                pool.Entries.Add(entry);
                _entriesById[entry.Id] = entry;

                ApplyEntryMix(entry);
            }
        }

        private PoolEntry AcquireForOneShot(CategoryPool pool)
        {
            for (int i = 0; i < pool.Entries.Count; i++)
            {
                var e = pool.Entries[i];
                if (e.ReservedLoop)
                    continue;

                if (!e.Source.isPlaying)
                    return e;
            }

            if (pool.Category == SoundCategory.UI || pool.Category == SoundCategory.SFX || pool.Category == SoundCategory.Voice)
                return StealOldestOneShot(pool);

            return null;
        }

        private PoolEntry StealOldestOneShot(CategoryPool pool)
        {
            PoolEntry oldest = null;
            double oldestTime = double.MaxValue;

            for (int i = 0; i < pool.Entries.Count; i++)
            {
                var e = pool.Entries[i];
                if (e.ReservedLoop)
                    continue;

                if (!e.Source.isPlaying)
                    continue;

                if (e.Source.loop)
                    continue;

                if (e.StartDspTime > 0 && e.StartDspTime < oldestTime)
                {
                    oldestTime = e.StartDspTime;
                    oldest = e;
                }
            }

            if (oldest == null)
                return null;

            oldest.Source.Stop();
            oldest.Source.clip = null;
            oldest.ReservedLoop = false;

            return oldest;
        }

        private PoolEntry AcquireForLoop(CategoryPool pool)
        {
            for (int i = 0; i < pool.Entries.Count; i++)
            {
                var e = pool.Entries[i];
                if (e.ReservedLoop)
                    continue;

                if (!e.Source.isPlaying)
                    return e;
            }

            return null;
        }

        private void StartEntry(PoolEntry entry, AudioClip clip, bool loop, float volume01, float pitch)
        {
            entry.Generation++;
            entry.StartDspTime = AudioSettings.dspTime;

            entry.LocalVolume = Mathf.Clamp01(volume01);
            entry.LocalPitch = Mathf.Clamp(pitch, 0.1f, 3f);

            entry.Source.Stop();
            entry.Source.clip = clip;
            entry.Source.loop = loop;
            entry.Source.pitch = entry.LocalPitch;

            ApplyEntryMix(entry);

            entry.Source.Play();
        }

        private void ApplyEntryMix(PoolEntry entry)
        {
            if (!_pools.TryGetValue(entry.Category, out var pool))
                return;

            float v = entry.LocalVolume * pool.Volume01;
            entry.Source.volume = Mathf.Clamp01(v);
            entry.Source.mute = pool.Muted;
        }

        private void ApplyMusicMix()
        {
            if (_musicSource == null)
                return;

            if (!_pools.TryGetValue(SoundCategory.Music, out var pool))
                return;

            _musicSource.volume = Mathf.Clamp01(pool.Volume01);
            _musicSource.mute = pool.Muted;
        }

        private bool TryGetValidHandleEntry(SoundHandle handle, out PoolEntry entry)
        {
            entry = null;

            if (!handle.IsValid)
                return false;

            if (!_entriesById.TryGetValue(handle.Id, out entry))
                return false;

            if (entry.Generation != handle.Generation)
                return false;

            if (!entry.ReservedLoop)
                return false;

            return true;
        }

        private sealed class CategoryPool
        {
            public readonly SoundCategory Category;
            public readonly Transform Root;
            public readonly List<PoolEntry> Entries = new();

            public float Volume01 = 1f;
            public bool Muted;

            private readonly AudioService _owner;

            public CategoryPool(SoundCategory category, Transform root, AudioService owner)
            {
                Category = category;
                Root = root;
                _owner = owner;
            }

            public void ApplyMixToAll()
            {
                for (int i = 0; i < Entries.Count; i++)
                    _owner.ApplyEntryMix(Entries[i]);
            }
        }

        private sealed class PoolEntry
        {
            public readonly int Id;
            public readonly SoundCategory Category;
            public readonly AudioSource Source;

            public int Generation = 0;

            public bool ReservedLoop;
            public float LocalVolume = 1f;
            public float LocalPitch = 1f;

            public double StartDspTime;

            public PoolEntry(int id, SoundCategory category, AudioSource source)
            {
                Id = id;
                Category = category;
                Source = source;
            }
        }
    }
}
