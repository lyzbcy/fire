using System;
using System.Collections.Generic;
using UnityEngine;

namespace PoseDrive.Runtime.Input
{
    /// <summary>
    /// 可序列化的动作映射配置资产。
    /// </summary>
    [CreateAssetMenu(fileName = "ActionMappingAsset", menuName = "PoseController/动作映射", order = 10)]
    public class ActionMappingAsset : ScriptableObject
    {
        /// <summary>
        /// 默认映射列表。
        /// </summary>
        public List<GestureBinding> Bindings = new List<GestureBinding>();
    }

    /// <summary>
    /// 描述单个动作与按键的对应关系。
    /// </summary>
    [Serializable]
    public class GestureBinding
    {
        public string GestureName;
        public VirtualKey Key;
        public bool Hold;
    }
}

