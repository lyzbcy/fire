using System;
using PoseController.Runtime.Utils;
using UnityEngine;

#if UNITY_BARRACUDA
using Unity.Barracuda;
#endif

namespace PoseController.Runtime.Core
{
    /// <summary>
    /// 使用 Barracuda MoveNet 模型检测人体关键点。
    /// </summary>
    [RequireComponent(typeof(WebcamProvider))]
    public class PoseDetector : MonoBehaviour
    {
#if UNITY_BARRACUDA
        [Header("模型配置")]
        [SerializeField]
        private NNModel _compiledModel;

        [SerializeField]
      	private TextAsset _onnxModel;
#else
        [Header("模型配置")]
        [SerializeField]
        [Tooltip("缺少 Barracuda 包时此字段仅用于提示")]
        private TextAsset _placeholder;
#endif

        [SerializeField]
        private int _inputWidth = 192;

        [SerializeField]
        private int _inputHeight = 192;

        [Header("运行参数")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _smoothing = 0.6f;

        [Range(0f, 1f)]
        [SerializeField]
        private float _confidenceThreshold = 0.4f;

        [SerializeField]
        private bool _debugMode;

        private IWebcamProviderFacade _provider;
#if UNITY_BARRACUDA
        private Model _model;
        private IWorker _worker;
#endif
        private readonly PoseData _currentPose = new PoseData();
        private readonly PoseData _smoothedPose = new PoseData();
        private float _lastConfidence;
        private Texture2D _cachedFrame;

        /// <summary>当前平滑后的姿态。</summary>
        public PoseData CurrentPose => _smoothedPose;

        /// <summary>当前姿态是否有效。</summary>
        public bool IsPoseValid => _lastConfidence >= _confidenceThreshold;

        private void Awake()
        {
            _provider = GetComponent<WebcamProvider>();
            if (_provider == null)
            {
                _provider = gameObject.AddComponent<WebcamProvider>();
            }

#if UNITY_BARRACUDA
            InitializeModel();
#else
            Debug.LogWarning("PoseController: 当前项目尚未安装 Barracuda，姿态检测将不会运行。");
            _ = _inputWidth;
            _ = _inputHeight;
            _ = _smoothing;
#endif
        }

        private void OnDestroy()
        {
#if UNITY_BARRACUDA
            _worker?.Dispose();
#endif
        }

        private void Update()
        {
#if UNITY_BARRACUDA
            if (_worker == null)
            {
                return;
            }

            if (!_provider.TryGetFrame(out _cachedFrame) || _cachedFrame == null)
            {
                _lastConfidence = 0f;
                return;
            }

            using Tensor input = BuildInputTensor(_cachedFrame);
            _worker.Execute(input);
            using Tensor output = _worker.PeekOutput();
            ParsePose(output, _currentPose);
            SmoothPose(_currentPose, _smoothedPose);
            _smoothedPose.Timestamp = Time.time;
#else
            _lastConfidence = 0f;
#endif
        }

#if UNITY_BARRACUDA
        private void InitializeModel()
        {
            try
            {
                if (_compiledModel != null)
                {
                    _model = ModelLoader.Load(_compiledModel);
                }
                else if (_onnxModel != null)
                {
                    _model = ModelLoader.Load(_onnxModel.bytes);
                }

                if (_model == null)
                {
                    Debug.LogWarning("PoseController: 未配置 MoveNet 模型。");
                    return;
                }

                _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, _model);
            }
            catch (Exception ex)
            {
                Debug.LogError($"PoseController: 加载 MoveNet 失败 - {ex.Message}");
            }
        }

        private Tensor BuildInputTensor(Texture2D texture)
        {
            Color[] pixels = texture.GetPixels(0, 0, texture.width, texture.height);
            Tensor tensor = new Tensor(1, _inputHeight, _inputWidth, 3);
            float scaleX = texture.width / (float)_inputWidth;
            float scaleY = texture.height / (float)_inputHeight;

            for (int y = 0; y < _inputHeight; y++)
            {
                for (int x = 0; x < _inputWidth; x++)
                {
                    int srcX = Mathf.Clamp(Mathf.RoundToInt(x * scaleX), 0, texture.width - 1);
                    int srcY = Mathf.Clamp(Mathf.RoundToInt(y * scaleY), 0, texture.height - 1);
                    Color color = pixels[srcY * texture.width + srcX];
                    tensor[0, y, x, 0] = color.r;
                    tensor[0, y, x, 1] = color.g;
                    tensor[0, y, x, 2] = color.b;
                }
            }

            return tensor;
        }

        private void ParsePose(Tensor output, PoseData target)
        {
            if (output == null)
            {
                return;
            }

            // MoveNet 结果形状：1 x keypoints x 3
            int keypoints = Math.Min(output.channels, PoseData.KeypointCount);
            float confidenceAccumulator = 0f;

            for (int i = 0; i < keypoints; i++)
            {
                float y = output[0, 0, i, 0];
                float x = output[0, 0, i, 1];
                float confidence = output[0, 0, i, 2];

                PoseKeypoint kp = new PoseKeypoint
                {
                    X = Mathf.Clamp01(x),
                    Y = Mathf.Clamp01(1f - y),
                    Confidence = Mathf.Clamp01(confidence)
                };

                target.SetKeypoint(i, kp);
                confidenceAccumulator += kp.Confidence;
            }

            _lastConfidence = confidenceAccumulator / Mathf.Max(1, keypoints);

            if (_debugMode)
            {
                Debug.Log($"PoseController: 置信度 {_lastConfidence:F2}");
            }
        }

        private void SmoothPose(PoseData source, PoseData destination)
        {
            if (destination == null || source == null)
            {
                return;
            }

            float alpha = Mathf.Clamp01(_smoothing);
            for (int i = 0; i < PoseData.KeypointCount; i++)
            {
                PoseKeypoint src = source.Keypoints[i];
                PoseKeypoint dst = destination.Keypoints[i];
                dst.X = Mathf.Lerp(dst.X, src.X, alpha);
                dst.Y = Mathf.Lerp(dst.Y, src.Y, alpha);
                dst.Confidence = Mathf.Lerp(dst.Confidence, src.Confidence, alpha);
                destination.SetKeypoint(i, dst);
            }
        }
#endif
    }

    /// <summary>
    /// 为了便于测试的摄像头接口。
    /// </summary>
    internal interface IWebcamProviderFacade
    {
        bool TryGetFrame(out Texture2D texture);
    }
}

