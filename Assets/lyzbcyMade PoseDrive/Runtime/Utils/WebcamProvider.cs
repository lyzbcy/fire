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
        [Tooltip("首选设备名称，留空则自动选择")]
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
        private Texture2D _bufferTexture;
        private bool _isInitialized;
        private float _lastFrameTime;
        private WebcamStatus _status = WebcamStatus.Idle;

        /// <summary>摄像头状态。</summary>
        public WebcamStatus Status => _status;

        /// <summary>当前摄像头是否已经输出画面。</summary>
        public bool HasCamera => _status == WebcamStatus.Streaming;

        /// <summary>当前摄像头是否已经初始化。</summary>
        public bool IsInitialized => _isInitialized;

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

            if (_webCam.didUpdateThisFrame)
            {
                EnsureBufferTexture();
                _bufferTexture.SetPixels32(_webCam.GetPixels32());
                _bufferTexture.Apply(false);
                _status = WebcamStatus.Streaming;
                _lastFrameTime = Time.realtimeSinceStartup;
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

            Initialize();
        }

        /// <summary>初始化摄像头。</summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _status = WebcamStatus.Initializing;
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                _status = WebcamStatus.NoDevice;
                Log("未检测到摄像头设备。");
                return;
            }

            WebCamDevice selected = devices[0];
            if (!string.IsNullOrEmpty(_preferredDeviceName))
            {
                foreach (WebCamDevice device in devices)
                {
                    if (device.name.IndexOf(_preferredDeviceName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        selected = device;
                        break;
                    }
                }
            }

            try
            {
                _webCam = new WebCamTexture(selected.name, _requestedWidth, _requestedHeight, _requestedFps);
                _webCam.Play();
                _isInitialized = true;
                _lastFrameTime = Time.realtimeSinceStartup;
                Log($"使用摄像头：{selected.name}");
            }
            catch (Exception ex)
            {
                _status = WebcamStatus.Error;
                Log($"初始化摄像头失败: {ex.Message}");
            }
        }

        /// <summary>尝试获取最新的一帧纹理。</summary>
        public bool TryGetFrame(out Texture2D texture)
        {
            texture = null;
            if (_webCam == null)
            {
                return false;
            }

            if (_webCam.didUpdateThisFrame)
            {
                EnsureBufferTexture();
                _bufferTexture.SetPixels32(_webCam.GetPixels32());
                _bufferTexture.Apply(false);
                _status = WebcamStatus.Streaming;
                _lastFrameTime = Time.realtimeSinceStartup;
            }
            else if (_status == WebcamStatus.Streaming && Time.realtimeSinceStartup - _lastFrameTime > 1.5f)
            {
                _status = WebcamStatus.Initializing;
            }

            if (_bufferTexture == null)
            {
                return false;
            }

            texture = _bufferTexture;
            return _status == WebcamStatus.Streaming;
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

        private void EnsureBufferTexture()
        {
            if (_bufferTexture == null || _bufferTexture.width != _requestedWidth || _bufferTexture.height != _requestedHeight)
            {
                _bufferTexture = new Texture2D(_requestedWidth, _requestedHeight, TextureFormat.RGBA32, false);
            }
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

            if (_bufferTexture != null)
            {
                Destroy(_bufferTexture);
                _bufferTexture = null;
            }

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

