using System;
using UnityEngine;

#if UNITY_BARRACUDA
using Unity.Barracuda;
#endif

namespace PoseDrive.Runtime.Core
{
    /// <summary>
    /// 基于 ONNX 的动作分类器。
    /// </summary>
    public class ActionClassifier : MonoBehaviour
    {
#if UNITY_BARRACUDA
        [SerializeField]
        private NNModel _compiledModel;

        [SerializeField]
        private TextAsset _onnxModel;

        [SerializeField]
        private string[] _labels = { "Nod", "Shake", "Wave", "Grab", "RaiseHand" };

        [SerializeField]
        [Tooltip("触发事件所需的最小置信度")]
        private float _triggerThreshold = 0.6f;

        [SerializeField]
        private bool _debugMode;

        private Model _model;
        private IWorker _worker;
#else
        [SerializeField]
        [Tooltip("缺少 Barracuda 包时用于提示的占位标签列表")]
        private string[] _labels = Array.Empty<string>();
#endif

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
#if UNITY_BARRACUDA
            InitializeModel();
#else
            Debug.LogWarning("PoseController: 当前项目尚未安装 Barracuda，动作分类器将不会运行。");
            if (OnActionRecognized != null)
            {
                // 访问事件以避免编译器关于未使用事件的告警
            }
#endif
        }

        private void OnDestroy()
        {
#if UNITY_BARRACUDA
            _worker?.Dispose();
#endif
        }

        /// <summary>执行一次分类。</summary>
        public void Evaluate(float[] featureVector)
        {
#if UNITY_BARRACUDA
            if (_worker == null || featureVector == null || featureVector.Length == 0)
            {
                return;
            }

            using Tensor input = new Tensor(1, 1, 1, featureVector.Length);
            for (int i = 0; i < featureVector.Length; i++)
            {
                input[0, 0, 0, i] = featureVector[i];
            }

            _worker.Execute(input);
            using Tensor output = _worker.PeekOutput();
            ProcessOutput(output);
#else
            // 无 Barracuda 时忽略推理请求
            _ = featureVector;
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
                    Debug.LogWarning("PoseController: 未配置动作分类模型。");
                    return;
                }

                _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, _model);
            }
            catch (Exception ex)
            {
                Debug.LogError($"PoseController: 动作分类器初始化失败 - {ex.Message}");
            }
        }

        private void ProcessOutput(Tensor output)
        {
            if (output == null)
            {
                return;
            }

            int bestIndex = -1;
            float bestValue = float.MinValue;

            for (int i = 0; i < output.channels; i++)
            {
                float value = output[0, 0, 0, i];
                if (value > bestValue)
                {
                    bestValue = value;
                    bestIndex = i;
                }
            }

            CurrentActionId = bestIndex;
            CurrentConfidence = Mathf.Clamp01(Mathf.Exp(bestValue));

            if (CurrentActionId >= 0 && CurrentConfidence >= _triggerThreshold)
            {
                string label = CurrentLabel;
                OnActionRecognized?.Invoke(label);

                if (_debugMode)
                {
                    Debug.Log($"PoseController: 动作 {label} 置信度 {CurrentConfidence:F2}");
                }
            }
        }
#endif
    }
}

