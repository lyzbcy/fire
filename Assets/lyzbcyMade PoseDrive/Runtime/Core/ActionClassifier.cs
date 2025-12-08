using System;
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
    /// 基于 Unity Sentis 或 Barracuda 的动作分类器。
    /// 通过 ModelCompat 兼容层自动适配不同的推理引擎。
    /// </summary>
    public class ActionClassifier : MonoBehaviour
    {
#if UNITY_BARRACUDA || UNITY_SENTIS
        [Header("模型配置")]
        [Tooltip("Sentis 的 ModelAsset 或 Barracuda 的 NNModel。优先使用它。")]
        [SerializeField]
        private UnityEngine.Object _modelAsset;

        [Tooltip("备用 ONNX TextAsset。仅在未设置 ModelAsset 时使用。")]
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
        private string[] _labels = { "Nod", "Shake", "Wave", "Grab", "RaiseHand" };

        [SerializeField]
        [Tooltip("触发事件所需的最小置信度")]
        private float _triggerThreshold = 0.6f;

        [SerializeField]
        private bool _debugMode;

        private Model _model;
        private IWorker _worker;

        /// <summary>当前动作编号。</summary>
        public int CurrentActionId { get; private set; } = -1;

        /// <summary>当前置信度。</summary>
        public float CurrentConfidence { get; private set; }

        /// <summary>当前标签名称。</summary>
        public string CurrentLabel => CurrentActionId >= 0 && CurrentActionId < _labels.Length ? _labels[CurrentActionId] : string.Empty;

        /// <summary>动作识别事件。</summary>
        public event Action<string> OnActionRecognized;

        private void Awake()
        {
#if UNITY_BARRACUDA || UNITY_SENTIS
            InitializeModel();
#else
            Debug.LogWarning("PoseDrive: 未安装 Unity.Barracuda 或 Unity.Sentis，动作分类器将不会运行。");
#endif
        }

        private void OnDestroy()
        {
#if UNITY_BARRACUDA || UNITY_SENTIS
            _worker?.Dispose();
#endif
        }

        /// <summary>执行一次分类。</summary>
        public void Evaluate(float[] featureVector)
        {
#if UNITY_BARRACUDA || UNITY_SENTIS
            if (_worker == null || featureVector == null || featureVector.Length == 0)
            {
                return;
            }

            using TensorFloat input = BuildInputTensor(featureVector);
            _worker.Execute(input);

            TensorFloat output = _worker.PeekOutput() as TensorFloat;
            if (output == null)
            {
                return;
            }

            ProcessOutput(output);
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
                    Debug.LogWarning("PoseDrive: 未配置动作分类模型（ModelAsset 或 ONNX）。");
                    return;
                }

#if UNITY_SENTIS
                _model = rawModel;
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
                Debug.LogError($"PoseDrive: 动作分类器初始化失败 - {ex.Message}");
                _worker = null;
            }
        }

        private TensorFloat BuildInputTensor(float[] featureVector)
        {
            return ModelCompat.CreateTensorFromFloats(featureVector, batch: 1, height: 1, width: 1, channels: featureVector.Length);
        }

        private void ProcessOutput(TensorFloat output)
        {
            if (output == null)
            {
                return;
            }

            int bestIndex = -1;
            float bestValue = float.MinValue;

#if UNITY_BARRACUDA
            // Barracuda: 使用 channels 属性
            int channelCount = output.channels;
            for (int i = 0; i < channelCount; i++)
            {
                float value = output[0, 0, 0, i];
                if (value > bestValue)
                {
                    bestValue = value;
                    bestIndex = i;
                }
            }
#elif UNITY_SENTIS
            // Sentis: 使用 shape[3] 获取通道数
            int channelCount = output.shape[3];
            for (int i = 0; i < channelCount; i++)
            {
                float value = output[0, 0, 0, i];
                if (value > bestValue)
                {
                    bestValue = value;
                    bestIndex = i;
                }
            }
#endif

            CurrentActionId = bestIndex;
            CurrentConfidence = Mathf.Clamp01(Mathf.Exp(bestValue));

            if (CurrentActionId >= 0 && CurrentConfidence >= _triggerThreshold)
            {
                string label = CurrentLabel;
                OnActionRecognized?.Invoke(label);

                if (_debugMode)
                {
                    Debug.Log($"PoseDrive: 动作 {label} 置信度 {CurrentConfidence:F2}");
                }
            }
        }
    }
}
