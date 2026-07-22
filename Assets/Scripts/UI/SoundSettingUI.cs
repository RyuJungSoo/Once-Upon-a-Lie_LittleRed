    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class SoundSettingUI : MonoBehaviour
    {
        [SerializeField]
        private Slider Master;
        [SerializeField]
        private Slider BGM;
        [SerializeField]
        private Slider SFX;

        private void OnEnable()
        {
            Master.value = SoundManager.Instance.GetAudioVolume(EAudioMixerType.Master);
            BGM.value = SoundManager.Instance.GetAudioVolume(EAudioMixerType.BGM);
            SFX.value = SoundManager.Instance.GetAudioVolume(EAudioMixerType.SFX);
        }
    }
