using System.Collections.Generic;
using UnityEngine;

namespace PoseController.Runtime.Input
{
    /// <summary>
    /// 将识别到的手势映射为虚拟按键状态。
    /// </summary>
    public class PoseInputMapper : MonoBehaviour
    {
        private enum KeyFrameState
        {
            Idle,
            Down,
            Held,
            Up
        }

        private class RuntimeKeyState
        {
            public VirtualKey Key;
            public KeyFrameState FrameState;
            public float HoldTimer;
        }

        [SerializeField]
        [Tooltip("可选的动作映射配置资产")]
        private ActionMappingAsset _mappingAsset;

        [SerializeField]
        private List<GestureBinding> _inlineBindings = new List<GestureBinding>();

        private readonly Dictionary<string, GestureBinding> _resolvedBindings = new Dictionary<string, GestureBinding>();
        private readonly Dictionary<VirtualKey, RuntimeKeyState> _runtimeStates = new Dictionary<VirtualKey, RuntimeKeyState>();
        private readonly List<VirtualKey> _pendingRelease = new List<VirtualKey>();
        private readonly List<VirtualKey> _releaseThisFrame = new List<VirtualKey>();

        /// <summary>获取当前映射表。</summary>
        public IReadOnlyDictionary<string, GestureBinding> Bindings => _resolvedBindings;

        private void Awake()
        {
            RebuildBindings();
        }

        private void Update()
        {
            foreach (RuntimeKeyState state in _runtimeStates.Values)
            {
                switch (state.FrameState)
                {
                    case KeyFrameState.Down:
                        state.FrameState = KeyFrameState.Held;
                        break;
                    case KeyFrameState.Up:
                        state.FrameState = KeyFrameState.Idle;
                        state.HoldTimer = 0f;
                        break;
                    case KeyFrameState.Held:
                        state.HoldTimer += Time.deltaTime;
                        break;
                }
            }

            if (_releaseThisFrame.Count > 0)
            {
                foreach (VirtualKey key in _releaseThisFrame)
                {
                    ReleaseKey(key);
                }
                _releaseThisFrame.Clear();
            }

            if (_pendingRelease.Count > 0)
            {
                _releaseThisFrame.AddRange(_pendingRelease);
                _pendingRelease.Clear();
            }
        }

        /// <summary>刷新映射配置。</summary>
        public void RebuildBindings()
        {
            _resolvedBindings.Clear();
            IEnumerable<GestureBinding> sources = GatherBindings();

            foreach (GestureBinding binding in sources)
            {
                if (string.IsNullOrWhiteSpace(binding.GestureName))
                {
                    continue;
                }

                _resolvedBindings[binding.GestureName] = binding;
            }
        }

        private IEnumerable<GestureBinding> GatherBindings()
        {
            if (_mappingAsset != null && _mappingAsset.Bindings != null)
            {
                foreach (GestureBinding binding in _mappingAsset.Bindings)
                {
                    yield return binding;
                }
            }

            if (_inlineBindings != null)
            {
                foreach (GestureBinding binding in _inlineBindings)
                {
                    yield return binding;
                }
            }

            if (_runtimeBindings != null)
            {
                foreach (GestureBinding binding in _runtimeBindings)
                {
                    yield return binding;
                }
            }
        }

        private readonly List<GestureBinding> _runtimeBindings = new List<GestureBinding>();

        /// <summary>根据手势名称触发按键。</summary>
        public void TriggerGesture(string gestureName)
        {
            if (string.IsNullOrWhiteSpace(gestureName) || !_resolvedBindings.TryGetValue(gestureName, out GestureBinding binding))
            {
                return;
            }

            TriggerKey(binding.Key);
            if (!binding.Hold)
            {
                _pendingRelease.Add(binding.Key);
            }
        }

        /// <summary>直接触发虚拟按键。</summary>
        public void TriggerKey(VirtualKey key)
        {
            if (!_runtimeStates.TryGetValue(key, out RuntimeKeyState state))
            {
                state = new RuntimeKeyState
                {
                    Key = key,
                    FrameState = KeyFrameState.Down
                };
                _runtimeStates[key] = state;
            }
            else
            {
                state.FrameState = state.FrameState is KeyFrameState.Held or KeyFrameState.Down
                    ? KeyFrameState.Held
                    : KeyFrameState.Down;
            }
            state.HoldTimer = 0f;
        }

        /// <summary>查询按键是否处于按下状态。</summary>
        public bool GetKey(VirtualKey key)
        {
            return _runtimeStates.TryGetValue(key, out RuntimeKeyState state) &&
                   (state.FrameState == KeyFrameState.Held || state.FrameState == KeyFrameState.Down);
        }

        /// <summary>查询按键是否在本帧被按下。</summary>
        public bool GetKeyDown(VirtualKey key)
        {
            return _runtimeStates.TryGetValue(key, out RuntimeKeyState state) &&
                   state.FrameState == KeyFrameState.Down;
        }

        /// <summary>查询按键是否在本帧松开。</summary>
        public bool GetKeyUp(VirtualKey key)
        {
            return _runtimeStates.TryGetValue(key, out RuntimeKeyState state) &&
                   state.FrameState == KeyFrameState.Up;
        }

        /// <summary>强制释放指定按键。</summary>
        public void ReleaseKey(VirtualKey key)
        {
            if (_runtimeStates.TryGetValue(key, out RuntimeKeyState state))
            {
                state.FrameState = KeyFrameState.Up;
            }
        }

        /// <summary>根据手势释放按键。</summary>
        public void ReleaseGesture(string gestureName)
        {
            if (string.IsNullOrEmpty(gestureName) || !_resolvedBindings.TryGetValue(gestureName, out GestureBinding binding))
            {
                return;
            }

            ReleaseKey(binding.Key);
        }

        /// <summary>设置运行时绑定。</summary>
        public void SetRuntimeBindings(IEnumerable<GestureBinding> bindings, bool clearExisting = true)
        {
            if (clearExisting)
            {
                _runtimeBindings.Clear();
            }

            if (bindings == null)
            {
                return;
            }

            _runtimeBindings.AddRange(bindings);
            RebuildBindings();
        }
    }
}

