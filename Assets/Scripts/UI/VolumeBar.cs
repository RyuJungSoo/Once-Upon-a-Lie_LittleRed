using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeBar : MonoBehaviour
{
    [SerializeField]
    private EAudioMixerType mixerType;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void Mute()
    {
        if (!SoundManager.HasInstance)
        {
            Debug.LogWarning(
                "[VolumeBar] SoundManager 인스턴스가 존재하지 않습니다.",
                this
            );

            return;
        }

        SoundManager.Instance.SetAudioMute(mixerType);
    }

    public void ChangeVolume()
    {
        if (!SoundManager.HasInstance)
        {
            Debug.LogWarning(
                "[VolumeBar] SoundManager 인스턴스가 존재하지 않습니다.",
                this
            );

            return;
        }

        SoundManager.Instance.SetAudioVolume(
            mixerType,
            slider.value
        );
    }
}