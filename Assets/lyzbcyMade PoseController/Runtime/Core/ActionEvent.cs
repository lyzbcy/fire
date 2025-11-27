using System;
using UnityEngine;
using UnityEngine.Events;

namespace PoseController.Runtime.Core
{
    /// <summary>
    /// 表示一次动作识别触发的结果。
    /// </summary>
    [Serializable]
    public class ActionEvent : UnityEvent<string>
    {
    }
}

