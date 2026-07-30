using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public enum EAudioMixerType
{
    Master,
    BGM,
    SFX
}

// BGM 타입 번호
public enum EBGMType
{
    MainMenu = 0,
    Stage1 = 1,
    Stage2 = 2,
    Stage3 = 3,
    Victory = 4,
    GameOver = 5,
    StageClear = 6
}

// SFX 타입 번호
public enum ESFXType
{
    Fire = 0,
    Reload = 1,
    Hurt = 2,
    Death = 3,
    ExpCrystal = 4,
    RedBerry = 5,
    StarCandy = 6,
    Pie = 7
}

public class SoundManager : Singleton<SoundManager>
{
    private const string DefaultSfxPlaybackProfilePath =
        "SFXPlaybackProfile";

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] bgmClips;
    [SerializeField] private AudioClip[] sfxClips;

    [Header("SFX Playback")]
    [SerializeField]
    private SFXPlaybackProfile sfxPlaybackProfile;

    private readonly bool[] isMute = new bool[3];
    private readonly float[] audioVolumes = new float[3];

    private AudioSource[] sfxVoices =
        Array.Empty<AudioSource>();
    private float[] nextSfxPlayTimes =
        Array.Empty<float>();
    private float[] lastSfxPitches =
        Array.Empty<float>();
    private bool[] hasLastSfxPitch =
        Array.Empty<bool>();
    private int nextSfxVoiceIndex;
    private float defaultSfxPitch = 1f;

    protected override void Awake()
    {
        base.Awake();
        
        // 중복 생성된 SoundManager라면 초기화를 진행하지 않음
        if (!IsSingletonInstance)
        {
            return;
        }

        InitAudioSources();
        InitSfxPlayback();
    }

    private void InitAudioSources()
    {
        if (bgmSource == null)
        {
            Transform bgmTransform = transform.Find("BGM");

            if (bgmTransform != null)
            {
                bgmSource = bgmTransform.GetComponent<AudioSource>();
            }
        }

        if (sfxSource == null)
        {
            Transform sfxTransform = transform.Find("SFX");

            if (sfxTransform != null)
            {
                sfxSource = sfxTransform.GetComponent<AudioSource>();
            }
        }

        if (bgmSource == null)
        {
            Debug.LogWarning(
                "[SoundManager] BGM AudioSource가 연결되지 않았습니다.",
                this
            );
        }

        if (sfxSource == null)
        {
            Debug.LogWarning(
                "[SoundManager] SFX AudioSource가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void InitSfxPlayback()
    {
        if (sfxSource == null)
        {
            return;
        }

        if (sfxPlaybackProfile == null)
        {
            sfxPlaybackProfile =
                Resources.Load<SFXPlaybackProfile>(
                    DefaultSfxPlaybackProfilePath
                );
        }

        defaultSfxPitch = sfxSource.pitch;

        int voiceCount = sfxPlaybackProfile != null
            ? sfxPlaybackProfile.VoicePoolSize
            : 1;

        sfxVoices = new AudioSource[voiceCount];
        sfxVoices[0] = sfxSource;

        for (int i = 1; i < voiceCount; i++)
        {
            AudioSource voice = Instantiate(
                sfxSource,
                sfxSource.transform.parent
            );

            voice.name =
                $"{sfxSource.name} Voice {i + 1:00}";
            voice.playOnAwake = false;
            voice.loop = false;
            voice.clip = null;
            voice.Stop();
            sfxVoices[i] = voice;
        }

        int stateCount = Mathf.Max(
            sfxClips?.Length ?? 0,
            Enum.GetValues(typeof(ESFXType)).Length
        );

        nextSfxPlayTimes = new float[stateCount];
        lastSfxPitches = new float[stateCount];
        hasLastSfxPitch = new bool[stateCount];
    }

    public void SetAudioVolume(
        EAudioMixerType audioMixerType,
        float volume
    )
    {
        if (audioMixer == null)
        {
            return;
        }

        volume = Mathf.Clamp(volume, 0.0001f, 1f);

        float decibel = Mathf.Log10(volume) * 20f;

        audioMixer.SetFloat(
            audioMixerType.ToString(),
            decibel
        );
    }

    public float GetAudioVolume(EAudioMixerType audioMixerType)
    {
        if (audioMixer == null)
        {
            return 1f;
        }

        bool hasParameter = audioMixer.GetFloat(
            audioMixerType.ToString(),
            out float currentDecibel
        );

        if (!hasParameter)
        {
            Debug.LogWarning(
                $"[SoundManager] AudioMixer에 노출된 파라미터가 없습니다: " +
                $"{audioMixerType}",
                this
            );

            return 1f;
        }

        return Mathf.Pow(10f, currentDecibel / 20f);
    }

    public void SetAudioMute(EAudioMixerType audioMixerType)
    {
        if (audioMixer == null)
        {
            return;
        }

        int typeIndex = (int)audioMixerType;
        string parameterName = audioMixerType.ToString();

        if (!isMute[typeIndex])
        {
            bool hasParameter = audioMixer.GetFloat(
                parameterName,
                out float currentDecibel
            );

            if (!hasParameter)
            {
                Debug.LogWarning(
                    $"[SoundManager] AudioMixer에 노출된 파라미터가 없습니다: " +
                    $"{parameterName}",
                    this
                );

                return;
            }

            isMute[typeIndex] = true;
            audioVolumes[typeIndex] = currentDecibel;

            audioMixer.SetFloat(parameterName, -80f);
        }
        else
        {
            isMute[typeIndex] = false;

            audioMixer.SetFloat(
                parameterName,
                audioVolumes[typeIndex]
            );
        }
    }

    public bool IsAudioMute(EAudioMixerType audioMixerType)
    {
        return isMute[(int)audioMixerType];
    }

    public void PlayBGM(EBGMType bgmType)
    {
        PlayBGM((int)bgmType);
    }

    public void PlayBGM(int index)
    {
        if (bgmSource == null)
        {
            return;
        }

        if (!IsValidIndex(bgmClips, index))
        {
            Debug.LogWarning(
                $"[SoundManager] BGM index가 잘못되었습니다. Index: {index}",
                this
            );

            return;
        }

        AudioClip clip = bgmClips[index];

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PauseBGM()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.Pause();
    }

    public void ResumeBGM()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.UnPause();
    }

    public void PlaySFX(ESFXType sfxType)
    {
        PlaySFX((int)sfxType, 0f);
    }

    public void PlaySFX(int index)
    {
        PlaySFX(index, 0f);
    }

    public void PlaySFX(
        ESFXType sfxType,
        float delay
    )
    {
        PlaySFX((int)sfxType, delay);
    }

    public void PlaySFX(
        int index,
        float delay
    )
    {
        if (!IsValidIndex(sfxClips, index))
        {
            Debug.LogWarning(
                $"[SoundManager] SFX index가 잘못되었습니다. Index: {index}",
                this
            );

            return;
        }

        if (delay <= 0f)
        {
            PlaySFXImmediate(index);
            return;
        }

        StartCoroutine(
            PlaySFXWithDelay(index, delay)
        );
    }

    private void PlaySFXImmediate(int index)
    {
        if (sfxVoices.Length == 0)
        {
            return;
        }

        AudioClip clip = sfxClips[index];

        if (clip == null)
        {
            return;
        }

        SFXPlaybackSettings settings =
            GetPlaybackSettings(index);

        if (settings != null &&
            Time.unscaledTime < nextSfxPlayTimes[index])
        {
            return;
        }

        AudioSource voice = GetNextSfxVoice();
        float pitch = defaultSfxPitch;
        float volumeScale = 1f;

        if (settings != null)
        {
            pitch = settings.PickPitch(
                defaultSfxPitch,
                hasLastSfxPitch[index],
                lastSfxPitches[index]
            );
            volumeScale = settings.PickVolumeScale();
            nextSfxPlayTimes[index] =
                Time.unscaledTime +
                settings.MinRetriggerInterval;
        }

        voice.pitch = pitch;
        voice.PlayOneShot(clip, volumeScale);

        lastSfxPitches[index] = pitch;
        hasLastSfxPitch[index] = true;
    }

    private SFXPlaybackSettings GetPlaybackSettings(
        int index
    )
    {
        if (sfxPlaybackProfile == null ||
            !Enum.IsDefined(typeof(ESFXType), index))
        {
            return null;
        }

        sfxPlaybackProfile.TryGetSettings(
            (ESFXType)index,
            out SFXPlaybackSettings settings
        );

        return settings;
    }

    private AudioSource GetNextSfxVoice()
    {
        for (int offset = 0;
            offset < sfxVoices.Length;
            offset++)
        {
            int index =
                (nextSfxVoiceIndex + offset) %
                sfxVoices.Length;
            AudioSource voice = sfxVoices[index];

            if (voice.isPlaying)
            {
                continue;
            }

            nextSfxVoiceIndex =
                (index + 1) % sfxVoices.Length;
            return voice;
        }

        AudioSource reusedVoice =
            sfxVoices[nextSfxVoiceIndex];
        nextSfxVoiceIndex =
            (nextSfxVoiceIndex + 1) %
            sfxVoices.Length;
        reusedVoice.Stop();
        return reusedVoice;
    }

    private IEnumerator PlaySFXWithDelay(
        int index,
        float delay
    )
    {
        yield return new WaitForSecondsRealtime(delay);

        // 지연 시간 중 오브젝트가 파괴되거나 배열이 변경된 상황 방지
        if (!IsValidIndex(sfxClips, index))
        {
            yield break;
        }

        PlaySFXImmediate(index);
    }

    private static bool IsValidIndex(
        AudioClip[] clips,
        int index
    )
    {
        return clips != null
            && index >= 0
            && index < clips.Length
            && clips[index] != null;
    }
}
