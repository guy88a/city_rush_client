using System.Collections.Generic;
using UnityEngine;

namespace CityRush.Core.Services
{
    public enum SoundCategory
    {
        Music,
        UI,
        SFX,
        Ambient,
        Voice
    }

    public enum AmbientType
    {
        Birds,
        Crowd,
        Cars,
        // Add more as needed
    }

    public readonly struct SoundHandle
    {
        public readonly int Id;
        public readonly int Generation;

        public SoundHandle(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }

        public bool IsValid => Id >= 0 && Generation > 0;

        public static SoundHandle Invalid => new SoundHandle(-1, 0);
    }

    public interface IAudioService : IGameService
    {
        // One-shots
        void PlayOneShot(SoundCategory category, AudioClip clip, float volume01 = 1f);
        void PlayOneShot(SoundCategory category, AudioClip clip, float volume01, float pitch);
        void PlayOneShot(SoundCategory category, AudioClip clip, float volume01, float pitchMin, float pitchMax);

        // Loops (non-ambient)
        SoundHandle PlayLoop(SoundCategory category, AudioClip clip, float volume01 = 1f, float pitch = 1f);
        void Stop(SoundHandle handle);
        void SetVolume(SoundHandle handle, float volume01);
        void SetPitch(SoundHandle handle, float pitch);

        // Ambient (typed layers)
        void StartAmbient(AmbientType type, AudioClip clip, float volume01 = 1f, float pitch = 1f);
        void StopAmbient(AmbientType type);
        void StopAllAmbient();
        void PauseAllAmbient(bool paused);

        // Music (playlist)
        void SetMusicPlaylist(IReadOnlyList<AudioClip> tracks, bool loopPlaylist);
        void PlayMusic();
        void StopMusic();
        void NextTrack();

        // Global controls
        void SetCategoryVolume(SoundCategory category, float volume01);
        void MuteCategory(SoundCategory category, bool isMuted);
        void PauseAll(bool paused);
    }
}
