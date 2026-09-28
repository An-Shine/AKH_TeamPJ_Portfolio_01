using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using TMPro;

namespace Michsky.UI.Shift
{
    public class QualityManager : MonoBehaviour
    {
        [Header("Audio")]
        public AudioMixer mixer;
        public SliderManager masterSlider;
        public SliderManager musicSlider;
        public SliderManager sfxSlider;

        [Header("Resolution")]
        public bool preferSelector = true;
        public HorizontalSelector resolutionSelector;
        public TMP_Dropdown resolutionDropdown;

        [System.Serializable]
        public class DynamicRes : UnityEvent<int> { }

        public DynamicRes clickEvent;

        Resolution[] resolutions;
        List<string> options = new List<string>();

        void Start()
        {
            mixer.SetFloat(
                "Master",
                Mathf.Log10(PlayerPrefs.GetFloat(masterSlider.sliderTag + "SliderValue")) * 20
            );

            mixer.SetFloat(
                "Music",
                Mathf.Log10(PlayerPrefs.GetFloat(musicSlider.sliderTag + "SliderValue")) * 20
            );

            mixer.SetFloat(
                "SFX",
                Mathf.Log10(PlayerPrefs.GetFloat(sfxSlider.sliderTag + "SliderValue")) * 20
            );

            CreateResolutionList();

            if (resolutions.Length == 0)
            {
                // Debug.LogWarning("해상도 확인안됨");
                return;
            }

            if (preferSelector == true)
                SetupResolutionSelector();
            else
                SetupResolutionDropdown();
        }

        void CreateResolutionList()
        {
            Resolution[] supportedResolutions = Screen.resolutions;
            List<Resolution> filteredResolutions = new List<Resolution>();

            for (int i = 0; i < supportedResolutions.Length; i++)
            {
                int refreshRate = GetRefreshRate(supportedResolutions[i]);

                if (refreshRate == 60 || refreshRate == 144)
                    filteredResolutions.Add(supportedResolutions[i]);
            }

            resolutions = filteredResolutions.ToArray();
        }

        void SetupResolutionSelector()
        {
            if (resolutionDropdown != null)
                resolutionDropdown.gameObject.SetActive(false);

            if (resolutionSelector == null)
                return;

            resolutionSelector.gameObject.SetActive(true);
            resolutionSelector.itemList.Clear();
            options.Clear();

            int currentResolutionIndex = FindCurrentResolutionIndex();

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = CreateResolutionLabel(resolutions[i]);

                options.Add(option);
                resolutionSelector.CreateNewItem(option);
            }

            if (currentResolutionIndex < 0)
                currentResolutionIndex = 0;

            resolutionSelector.defaultIndex = currentResolutionIndex;
            resolutionSelector.index = currentResolutionIndex;
            resolutionSelector.UpdateUI();
        }

        void SetupResolutionDropdown()
        {
            if (resolutionSelector != null)
                resolutionSelector.gameObject.SetActive(false);

            if (resolutionDropdown == null)
                return;

            resolutionDropdown.gameObject.SetActive(true);
            resolutionDropdown.ClearOptions();

            List<string> dropdownOptions = new List<string>();

            for (int i = 0; i < resolutions.Length; i++)
                dropdownOptions.Add(CreateResolutionLabel(resolutions[i]));

            int currentResolutionIndex = FindCurrentResolutionIndex();

            if (currentResolutionIndex < 0)
                currentResolutionIndex = 0;

            resolutionDropdown.AddOptions(dropdownOptions);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();

            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        string CreateResolutionLabel(Resolution resolution)
        {
            int refreshRate = GetRefreshRate(resolution);

            return resolution.width
                   + "x"
                   + resolution.height
                   + " "
                   + refreshRate
                   + "Hz";
        }

        int FindCurrentResolutionIndex()
        {
            int currentRefreshRate = GetRefreshRate(Screen.currentResolution);

            for (int i = 0; i < resolutions.Length; i++)
            {
                bool sameWidth =
                    resolutions[i].width == Screen.currentResolution.width;

                bool sameHeight =
                    resolutions[i].height == Screen.currentResolution.height;

                bool sameRefreshRate =
                    GetRefreshRate(resolutions[i]) == currentRefreshRate;

                if (sameWidth && sameHeight && sameRefreshRate)
                    return i;
            }

            return -1;
        }

        int GetRefreshRate(Resolution resolution)
        {
#if UNITY_2022_2_OR_NEWER
            return Mathf.RoundToInt(
                (float)resolution.refreshRateRatio.value
            );
#else
            return resolution.refreshRate;
#endif
        }

        public void UpdateResolution()
        {
            if (resolutionSelector == null)
                return;

            SetResolution(resolutionSelector.index);
        }

        public void SetResolution(int resolutionIndex)
        {
            if (resolutionIndex < 0 || resolutionIndex >= resolutions.Length)
                return;

            Resolution selectedResolution = resolutions[resolutionIndex];

#if UNITY_2022_2_OR_NEWER
            Screen.SetResolution(
                selectedResolution.width,
                selectedResolution.height,
                Screen.fullScreenMode,
                selectedResolution.refreshRateRatio
            );
#else
            Screen.SetResolution(
                selectedResolution.width,
                selectedResolution.height,
                Screen.fullScreen,
                selectedResolution.refreshRate
            );
#endif
        }

        public void AnisotrpicFilteringEnable()
        {
            QualitySettings.anisotropicFiltering =
                AnisotropicFiltering.ForceEnable;
        }

        public void AnisotrpicFilteringDisable()
        {
            QualitySettings.anisotropicFiltering =
                AnisotropicFiltering.Disable;
        }

        public void AntiAlisasingSet(int index)
        {
            // 0, 2, 4, 8 - Zero means off
            QualitySettings.antiAliasing = index;
        }

        public void VsyncSet(int index)
        {
            // 0, 1 - Zero means off
            QualitySettings.vSyncCount = index;
        }

        public void ShadowResolutionSet(int index)
        {
            if (index == 3)
                QualitySettings.shadowResolution =
                    ShadowResolution.VeryHigh;

            else if (index == 2)
                QualitySettings.shadowResolution =
                    ShadowResolution.High;

            else if (index == 1)
                QualitySettings.shadowResolution =
                    ShadowResolution.Medium;

            else if (index == 0)
                QualitySettings.shadowResolution =
                    ShadowResolution.Low;
        }

        public void ShadowsSet(int index)
        {
            if (index == 0)
                QualitySettings.shadows = ShadowQuality.Disable;

            else if (index == 1)
                QualitySettings.shadows = ShadowQuality.All;
        }

        public void ShadowsCascasedSet(int index)
        {
            // 0 = No, 2 = Two, 4 = Four
            QualitySettings.shadowCascades = index;
        }

        public void TextureSet(int index)
        {
            // 0 = Full, 4 = Eight Resolution
#if UNITY_2022_2_OR_NEWER
            QualitySettings.globalTextureMipmapLimit = index;
#else
            QualitySettings.masterTextureLimit = index;
#endif
        }

        public void SoftParticleSet(int index)
        {
            if (index == 0)
                QualitySettings.softParticles = false;

            else if (index == 1)
                QualitySettings.softParticles = true;
        }

        public void ReflectionSet(int index)
        {
            if (index == 0)
                QualitySettings.realtimeReflectionProbes = false;

            else if (index == 1)
                QualitySettings.realtimeReflectionProbes = true;
        }

        public void VolumeSetMaster(float volume)
        {
            mixer.SetFloat(
                "Master",
                Mathf.Log10(volume) * 20
            );
        }

        public void VolumeSetMusic(float volume)
        {
            mixer.SetFloat(
                "Music",
                Mathf.Log10(volume) * 20
            );
        }

        public void VolumeSetSFX(float volume)
        {
            mixer.SetFloat(
                "SFX",
                Mathf.Log10(volume) * 20
            );
        }

        public void SetOverallQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
        }

        public void WindowFullscreen()
        {
            Screen.fullScreen = true;
            Screen.fullScreenMode =
                FullScreenMode.ExclusiveFullScreen;
        }

        public void WindowBorderless()
        {
            Screen.fullScreenMode =
                FullScreenMode.FullScreenWindow;
        }

        public void WindowWindowed()
        {
            Screen.fullScreen = false;
            Screen.fullScreenMode =
                FullScreenMode.Windowed;
        }
    }
}