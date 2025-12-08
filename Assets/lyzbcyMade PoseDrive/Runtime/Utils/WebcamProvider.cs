using System;
using System.Threading.Tasks;
using PoseDrive.Runtime.Core;
using UnityEngine;

namespace PoseDrive.Runtime.Utils
{
    /// <summary>
    /// 摄像头状态。
    /// </summary>
    public enum WebcamStatus
    {
        Idle,
        Initializing,
        Streaming,
        NoDevice,
        Error
    }

    /// <summary>
    /// 简单的摄像头管理器，负责提供最新帧。
    /// </summary>
    public class WebcamProvider : MonoBehaviour, IWebcamProviderFacade
    {
        [SerializeField]
        [Tooltip("首选设备名称，留空则自动选择 (支持在运行时切换)")]
        private string _preferredDeviceName;

        [SerializeField]
        [Tooltip("请求的宽度")]
        private int _requestedWidth = 640;

        [SerializeField]
        [Tooltip("请求的高度")]
        private int _requestedHeight = 480;

        [SerializeField]
        [Tooltip("请求的帧率")]
        private int _requestedFps = 30;

        [SerializeField]
        private bool _autoStart = true;

        [SerializeField]
        [Tooltip("启用后会输出调试日志")]
        private bool _debug;

        private WebCamTexture _webCam;
        private Texture2D _frameTexture;
        private Color32[] _framePixels;
        private string _currentDeviceName;
        private bool _usingFallbackDevice;
        private bool _isInitialized;
        private float _lastFrameTime;
        private WebcamStatus _status = WebcamStatus.Idle;

        /// <summary>摄像头状态。</summary>
        public WebcamStatus Status => _status;

        /// <summary>当前摄像头是否已经输出画面。</summary>
        public bool HasCamera => _status == WebcamStatus.Streaming;

        /// <summary>当前摄像头是否已经初始化。</summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>当前使用的真实设备名称。</summary>
        public string CurrentDeviceName => _currentDeviceName;

        /// <summary>当首选设备不存在时，是否使用了回退设备。</summary>
        public bool IsUsingFallbackDevice => _usingFallbackDevice;

        /// <summary>首选设备名称（更改后会自动重新初始化）。</summary>
        public string PreferredDeviceName
        {
            get => _preferredDeviceName;
            set
            {
                if (_preferredDeviceName == value)
                {
                    return;
                }
                _preferredDeviceName = value;
                if (isActiveAndEnabled)
                {
                    Reinitialize();
                }
            }
        }

        private void Awake()
        {
            if (_autoStart)
            {
                EnsureInitialized();
            }
        }

        private void OnEnable()
        {
            if (_autoStart)
            {
                EnsureInitialized();
            }
        }

        private void Update()
        {
            if (_webCam == null)
            {
                return;
            }

            int width = _webCam.width;
            int height = _webCam.height;
            if (!IsResolutionValid(width, height))
            {
                if (_status != WebcamStatus.NoDevice)
                {
                    _status = WebcamStatus.Initializing;
                }
                return;
            }

            if (_webCam.didUpdateThisFrame)
            {
                if (!EnsureBuffers(width, height))
                {
                    return;
                }

                try
                {
                    _webCam.GetPixels32(_framePixels);
                    _frameTexture.SetPixels32(_framePixels);
                    _frameTexture.Apply(false);
                    _status = WebcamStatus.Streaming;
                    _lastFrameTime = Time.realtimeSinceStartup;
                }
                catch (ArgumentException ex)
                {
                    _framePixels = null;
                    _frameTexture = null;
                    _status = WebcamStatus.Initializing;
                    Log($"捕获到像素写入越界，已重置缓冲：{ex.Message}");
                }
            }
            else if (_status == WebcamStatus.Streaming)
            {
                if (Time.realtimeSinceStartup - _lastFrameTime > 1.5f)
                {
                    _status = WebcamStatus.Initializing;
                }
            }
        }

        private void OnDestroy()
        {
            DisposeCamera();
        }

        /// <summary>确保已经初始化摄像头。</summary>
        public void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            InitializeWebCam();
        }

        /// <summary>初始化摄像头（含设备挑选、虚拟摄像头屏蔽与回退逻辑）。</summary>
        private void InitializeWebCam()
        {
            _status = WebcamStatus.Initializing;
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                _status = WebcamStatus.NoDevice;
                _currentDeviceName = null;
                Log("未检测到摄像头设备。");
                return;
            }

            WebCamDevice selected = SelectDevice(devices, _preferredDeviceName, out _usingFallbackDevice);
            _currentDeviceName = selected.name;

            try
            {
                _webCam = new WebCamTexture(_currentDeviceName, _requestedWidth, _requestedHeight, _requestedFps);
                _webCam.Play();
                _isInitialized = true;
                _lastFrameTime = Time.realtimeSinceStartup;
                Log($"使用摄像头：{_currentDeviceName}");
            }
            catch (Exception ex)
            {
                _status = WebcamStatus.Error;
                Log($"初始化摄像头失败: {ex.Message}");
            }
        }

        /// <summary>外部调用重新初始化（用于设备切换）。</summary>
        public void Reinitialize()
        {
            DisposeCamera();
            EnsureInitialized();
        }

        /// <summary>返回当前可用设备列表。</summary>
        public string[] GetAvailableDeviceNames(bool includeVirtual = true)
        {
            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                return Array.Empty<string>();
            }

            if (includeVirtual)
            {
                string[] names = new string[devices.Length];
                for (int i = 0; i < devices.Length; i++)
                {
                    names[i] = devices[i].name;
                }
                return names;
            }

            var list = new System.Collections.Generic.List<string>();
            foreach (var device in devices)
            {
                if (!IsLikelyVirtualDevice(device.name))
                {
                    list.Add(device.name);
                }
            }
            return list.ToArray();
        }

        /// <summary>尝试获取最新的一帧纹理。</summary>
        public bool TryGetFrame(out Texture2D texture)
        {
            if (_status != WebcamStatus.Streaming || _frameTexture == null)
            {
                texture = null;
                return false;
            }

            texture = _frameTexture;
            return true;
        }

        /// <summary>异步等待下一帧。</summary>
        public async Task<Texture2D> WaitForNextFrameAsync()
        {
            if (_webCam == null)
            {
                return null;
            }

            while (!_webCam.didUpdateThisFrame)
            {
                await Task.Yield();
            }

            return TryGetFrame(out Texture2D tex) ? tex : null;
        }

        private bool EnsureBuffers(int width, int height)
        {
            if (!IsResolutionValid(width, height))
            {
                return false;
            }

            int pixelCount = width * height;
            if (_framePixels == null || _framePixels.Length != pixelCount)
            {
                _framePixels = new Color32[pixelCount];
            }

            if (_frameTexture == null || _frameTexture.width != width || _frameTexture.height != height)
            {
                _frameTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            return true;
        }

        private static bool IsResolutionValid(int width, int height)
        {
            return width > 32 && height > 32;
        }

        private static WebCamDevice SelectDevice(WebCamDevice[] devices, string preferred, out bool usingFallback)
        {
            usingFallback = false;

            if (!string.IsNullOrEmpty(preferred))
            {
                foreach (var device in devices)
                {
                    if (string.Equals(device.name, preferred, StringComparison.OrdinalIgnoreCase))
                    {
                        return device;
                    }
                }

                usingFallback = true;
            }

            foreach (var device in devices)
            {
                if (!IsLikelyVirtualDevice(device.name))
                {
                    return device;
                }
            }

            usingFallback = usingFallback || !string.IsNullOrEmpty(preferred);
            return devices[0];
        }

        private static bool IsLikelyVirtualDevice(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                return false;
            }

            string lower = deviceName.ToLowerInvariant();
            return lower.Contains("virtual")
                   || lower.Contains("obs")
                   || lower.Contains("snap")
                   || lower.Contains("xsplit")
                   || lower.Contains("manycam")
                   || lower.Contains("camtwist")
                   || lower.Contains("logicapture")
                   || lower.Contains("droidcam");
        }

        private void DisposeCamera()
        {
            if (_webCam != null)
            {
                if (_webCam.isPlaying)
                {
                    _webCam.Stop();
                }

                Destroy(_webCam);
                _webCam = null;
            }

            if (_frameTexture != null)
            {
                Destroy(_frameTexture);
                _frameTexture = null;
            }

            _framePixels = null;
            _currentDeviceName = null;
            _usingFallbackDevice = false;
            _isInitialized = false;
            _status = WebcamStatus.Idle;
        }

        private void Log(string message)
        {
            if (_debug)
            {
                Debug.Log($"[WebcamProvider] {message}");
            }
        }
    }
}

