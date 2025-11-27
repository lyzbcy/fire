using System;
using System.Threading.Tasks;
using PoseController.Runtime.Core;
using UnityEngine;

namespace PoseController.Runtime.Utils
{
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

        /// <summary>当前摄像头可用状态。</summary>
        public bool HasCamera => _webCam != null && _webCam.didUpdateThisFrame;

        private void Awake()
        {
            if (_autoStart)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            DisposeCamera();
        }

        /// <summary>初始化摄像头。</summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
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

            _webCam = new WebCamTexture(selected.name, _requestedWidth, _requestedHeight, _requestedFps);
            _webCam.Play();
            _bufferTexture = new Texture2D(_requestedWidth, _requestedHeight, TextureFormat.RGBA32, false);
            _isInitialized = true;
            Log($"使用摄像头：{selected.name}");
        }

        /// <summary>尝试获取最新的一帧纹理。</summary>
        public bool TryGetFrame(out Texture2D texture)
        {
            texture = null;
            if (_webCam == null || !_webCam.didUpdateThisFrame)
            {
                return false;
            }

            _bufferTexture.SetPixels32(_webCam.GetPixels32());
            _bufferTexture.Apply(false);
            texture = _bufferTexture;
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

