using System;
using PoseDrive.Runtime.Utils;
using UnityEngine;

#if UNITY_BARRACUDA
using Unity.Barracuda;
using TensorFloat = Unity.Barracuda.Tensor;
using Model = Unity.Barracuda.Model;
using IWorker = Unity.Barracuda.IWorker;
#elif UNITY_SENTIS
using Unity.Sentis;
using TensorFloat = Unity.Sentis.TensorFloat;
using Model = Unity.Sentis.Model;
using IWorker = Unity.Sentis.IWorker;
#else
// 占位类型（未安装任何推理引擎时）
using TensorFloat = System.Object;
using Model = System.Object;
using IWorker = System.IDisposable;
#endif

namespace PoseDrive.Runtime.Core
{
    /// <summary>
    /// 使用 Unity Sentis 或 Barracuda 运行 MoveNet 模型，检测人体关键点。
    /// 通过 ModelCompat 兼容层自动适配不同的推理引擎。
    /// </summary>
    [RequireComponent(typeof(WebcamProvider))]
    public class PoseDetector : MonoBehaviour
    {
#if UNITY_BARRACUDA
        [Header("模型配置")]
        [Tooltip("Barracuda 的 NNModel。优先使用它。")]
        [SerializeField]
        private UnityEngine.Object _modelAsset;

        [Tooltip("备用的 MoveNet ONNX 模型文件（TextAsset）。当未设置 ModelAsset 时使用。")]
        [SerializeField]
        private TextAsset _onnxModelFallback;
#elif UNITY_SENTIS
        [Header("模型配置")]
        [Tooltip("Sentis 的 ModelAsset。优先使用它。")]
        [SerializeField]
        private UnityEngine.Object _modelAsset;

        [Tooltip("备用的 MoveNet ONNX 模型文件（TextAsset）。当未设置 ModelAsset 时使用。")]
        [SerializeField]
        private TextAsset _onnxModelFallback;
#else
        [Header("模型配置")]
        [Tooltip("需要安装 Unity.Barracuda 或 Unity.Sentis 包")]
        [SerializeField]
        private UnityEngine.Object _modelAsset;

        [SerializeField]
        private TextAsset _onnxModelFallback;
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
        private Model _model;
        private IWorker _worker;

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

#if UNITY_BARRACUDA || UNITY_SENTIS
            InitializeModel();
#else
            Debug.LogWarning("PoseDrive: 未安装 Unity.Barracuda 或 Unity.Sentis，姿态检测将不会运行。");
#endif
        }

        private void OnDestroy()
        {
#if UNITY_BARRACUDA || UNITY_SENTIS
            _worker?.Dispose();
#endif
        }

        private void Update()
        {
#if UNITY_BARRACUDA || UNITY_SENTIS
            if (_worker == null)
            {
                _lastConfidence = 0f;
                return;
            }

            if (!_provider.TryGetFrame(out _cachedFrame) || _cachedFrame == null)
            {
                _lastConfidence = 0f;
                return;
            }

            using TensorFloat input = BuildInputTensor(_cachedFrame);
            _worker.Execute(input);

            TensorFloat output = _worker.PeekOutput() as TensorFloat;
            if (output == null)
            {
                _lastConfidence = 0f;
                return;
            }

            ParsePose(output, _currentPose);
            SmoothPose(_currentPose, _smoothedPose);
            _smoothedPose.Timestamp = Time.time;
#else
            _lastConfidence = 0f;
#endif
        }

        private void InitializeModel()
        {
            Model rawModel = null;

            try
            {
                if (_modelAsset != null)
                {
                    rawModel = ModelCompat.LoadModel(_modelAsset);
                }
                else if (_onnxModelFallback != null && _onnxModelFallback.bytes != null && _onnxModelFallback.bytes.Length > 0)
                {
                    rawModel = ModelCompat.LoadModel(_onnxModelFallback.bytes);
                }

                if (rawModel == null)
                {
                    Debug.LogWarning("PoseDrive: 未配置 MoveNet 模型（ModelAsset 或 ONNX）。");
                    return;
                }

#if UNITY_SENTIS
                _model = rawModel; // Sentis 的 ModelOptimizer 在 CreateWorker 中处理
                _worker = ModelCompat.CreateWorker(rawModel);
#elif UNITY_BARRACUDA
                _model = rawModel;
                _worker = ModelCompat.CreateWorker(rawModel);
#else
                _worker = null;
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"PoseDrive: 加载 MoveNet 模型失败 - {ex.Message}");
                _worker = null;
            }
        }

        private TensorFloat BuildInputTensor(Texture2D texture)
        {
            return ModelCompat.CreateTensorFromTexture(texture, _inputWidth, _inputHeight);
        }

        private void ParsePose(TensorFloat output, PoseData target)
        {
            if (output == null)
            {
                return;
            }

#if UNITY_BARRACUDA
            // Barracuda Tensor: shape 是 int[]，访问方式相同
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
#elif UNITY_SENTIS
            // Sentis TensorFloat: shape 是 TensorShape，访问方式相同
            int keypoints = Math.Min(output.shape[2], PoseData.KeypointCount);
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
#else
            _lastConfidence = 0f;
#endif

            if (_debugMode)
            {
                Debug.Log($"PoseDrive: 置信度 {_lastConfidence:F2}");
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
    }

    /// <summary>
    /// 为了便于测试的摄像头接口。
    /// </summary>
    internal interface IWebcamProviderFacade
    {
        bool TryGetFrame(out Texture2D texture);
    }
}
