using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NAK.PortableVideoPlayer.UI
{
    public class PortableVideoPlayerView : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private CanvasGroup _overlayCanvasGroup;
        [SerializeField] private CanvasGroup _lockableElementsCanvasGroup;

        [SerializeField] private TMP_Text _linkedPlayerText;
        [SerializeField] private TMP_Text _videoTitleText;
        [SerializeField] private TMP_Text _videoUrlText;

        [SerializeField] private GameObject _playButtonImageObject;
        [SerializeField] private GameObject _pauseButtonImageObject;

        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private Image _progressHandle;

        [SerializeField] private GameObject _volumeUnmutedImageObject;
        [SerializeField] private GameObject _volumeMutedImageObject;
        [SerializeField] private Slider _volumeSlider;

        [SerializeField] private RawImage _screenRawImage;

        [SerializeField] private Image _lockUiButtonImage;
        [SerializeField] private Image _disablePickupButtonImage;

        #endregion Serialized Fields

        #region Events

        public event Action LinkClicked;
        public event Action PlayPauseClicked;
        public event Action MuteClicked;
        public event Action<float> VolumeChanged;
        public event Action<float> ProgressChanged;
        public event Action<bool> PickupAllowedChanged;
        public event Action<bool> HoverChanged;

        #endregion Events

        private const float OverlayFadeDuration = 0.25f;

        private bool _isPlaybackUiLocked;
        private bool _isPickupDisabled;
        private bool _isLiveStream;
        private int _shownTime = -1;
        private int _shownDuration = -1;
        private Coroutine _overlayFadeCoroutine;

        #region Ui Handlers

        public void CycleNextVideoPlayer() => LinkClicked?.Invoke();
        public void OnPlayPauseButtonClicked() => PlayPauseClicked?.Invoke();
        public void OnVolumeButtonClicked() => MuteClicked?.Invoke();
        public void OnVolumeSliderChanged(float value) => VolumeChanged?.Invoke(value);
        public void OnPlaybackProgressSliderChanged(float value) => ProgressChanged?.Invoke(value);

        public void OnLockUiButtonClicked()
        {
            _isPlaybackUiLocked = !_isPlaybackUiLocked;
            _lockUiButtonImage.color = _isPlaybackUiLocked ? Color.white : Color.gray;
            _lockableElementsCanvasGroup.alpha = _isPlaybackUiLocked ? 0.8f : 1.0f;
            _lockableElementsCanvasGroup.interactable = !_isPlaybackUiLocked;
            _progressHandle.enabled = !_isPlaybackUiLocked;
        }

        public void OnDisablePickupButtonClicked()
        {
            _isPickupDisabled = !_isPickupDisabled;
            _disablePickupButtonImage.color = _isPickupDisabled ? Color.gray : Color.white;
            PickupAllowedChanged?.Invoke(!_isPickupDisabled);
        }

        public void UiPointerEnter() => HoverChanged?.Invoke(true);
        public void UiPointerExit() => HoverChanged?.Invoke(false);

        #endregion Ui Handlers

        #region State

        public void SetLinkedPlayerName(string playerName)
            => _linkedPlayerText.text = string.IsNullOrEmpty(playerName) ? "No video player linked" : "Linked: " + playerName;

        public void SetVideoInfo(string title, string url)
        {
            _videoTitleText.text = title;
            _videoUrlText.text = url;
        }

        public void SetPlaying(bool isPlaying)
        {
            _playButtonImageObject.SetActive(!isPlaying);
            _pauseButtonImageObject.SetActive(isPlaying);
        }

        public void SetVolume(float volume)
        {
            bool isMuted = volume <= 0.0f;
            _volumeUnmutedImageObject.SetActive(!isMuted);
            _volumeMutedImageObject.SetActive(isMuted);
            _volumeSlider.SetValueWithoutNotify(volume);
        }

        public void SetScreenTexture(Texture texture)
        {
            _screenRawImage.texture = texture;
            _screenRawImage.color = texture ? Color.white : Color.black;
        }

        public void SetProgress(float value, float maxValue)
        {
            _progressSlider.maxValue = maxValue;
            _progressSlider.SetValueWithoutNotify(value);
        }

        public void ClearTime()
        {
            _isLiveStream = false;
            _shownTime = _shownDuration = -1;
            _progressText.text = string.Empty;
            _progressSlider.interactable = true;
            SetProgress(0f, 1f);
        }

        public void SetTime(double videoTime, double duration, bool isLiveStream)
        {
            if (isLiveStream)
            {
                SetProgress(1f, 1f);
                if (_isLiveStream) return;
                _isLiveStream = true;
                _shownTime = _shownDuration = -1;
                _progressText.text = "LIVE";
                _progressSlider.interactable = false;
                return;
            }

            if (_isLiveStream)
            {
                _isLiveStream = false;
                _progressSlider.interactable = true;
            }

            SetProgress((float)videoTime, (float)duration);

            int time = ToWholeSeconds(videoTime);
            int total = ToWholeSeconds(duration);
            if (time == _shownTime && total == _shownDuration) return;
            _shownTime = time;
            _shownDuration = total;

            bool useHours = total >= 3600;
            _progressText.text = FormatTime(time, useHours) + " / " + FormatTime(total, useHours);
        }

        private static int ToWholeSeconds(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) return 0;
            return seconds >= int.MaxValue ? int.MaxValue : (int)seconds;
        }

        private static string FormatTime(int seconds, bool forceHours)
        {
            int hours = seconds / 3600;
            int minutes = seconds / 60 % 60;
            return forceHours || hours > 0
                ? $"{hours}:{minutes:D2}:{seconds % 60:D2}"
                : $"{minutes:D2}:{seconds % 60:D2}";
        }

        #endregion State

        #region Overlay

        public void SetOverlayVisible(bool visible)
        {
            if (_overlayFadeCoroutine != null) StopCoroutine(_overlayFadeCoroutine);
            _overlayFadeCoroutine = StartCoroutine(AnimateOverlayAlpha(visible ? 1.0f : 0.0f));
        }

        private IEnumerator AnimateOverlayAlpha(float target)
        {
            float startAlpha = _overlayCanvasGroup.alpha;
            float elapsed = 0.0f;

            while (elapsed < OverlayFadeDuration)
            {
                elapsed += Time.deltaTime;
                _overlayCanvasGroup.alpha = Mathf.Lerp(startAlpha, target, Mathf.Clamp01(elapsed / OverlayFadeDuration));
                yield return null;
            }

            _overlayCanvasGroup.alpha = target;
            _overlayFadeCoroutine = null;
        }

        #endregion Overlay
    }
}
