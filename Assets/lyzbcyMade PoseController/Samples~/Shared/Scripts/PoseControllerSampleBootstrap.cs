using System.Collections.Generic;
using PoseController.Runtime.Core;
using PoseController.Runtime.Input;
using PoseController.Runtime.Utils;
using UnityEngine;

namespace PoseController.Samples.Shared
{
    /// <summary>
    /// 在示例场景中自动搭建 PoseController 运行时。
    /// </summary>
    public class PoseControllerSampleBootstrap : MonoBehaviour
    {
        [SerializeField]
        private bool _createIfMissing = true;

        [SerializeField]
        private bool _log;

        private void Awake()
        {
            if (!_createIfMissing)
            {
                return;
            }

            if (FindObjectOfType<PoseControllerManager>() != null)
            {
                return;
            }

            GameObject root = new GameObject("PoseControllerSystem");
            WebcamProvider webcam = root.AddComponent<WebcamProvider>();
            PoseDetector detector = root.AddComponent<PoseDetector>();
            ActionClassifier classifier = root.AddComponent<ActionClassifier>();
            PoseInputMapper mapper = root.AddComponent<PoseInputMapper>();
            PoseControllerManager manager = root.AddComponent<PoseControllerManager>();

            mapper.SetRuntimeBindings(new List<GestureBinding>
            {
                new GestureBinding { GestureName = "Nod", Key = VirtualKey.Keyboard(KeyCode.E) },
                new GestureBinding { GestureName = "Shake", Key = VirtualKey.Keyboard(KeyCode.Q) },
                new GestureBinding { GestureName = "Grab", Key = VirtualKey.Mouse(0) },
                new GestureBinding { GestureName = "RaiseHand", Key = VirtualKey.Keyboard(KeyCode.Space) },
                new GestureBinding { GestureName = "HandsUp", Key = VirtualKey.Keyboard(KeyCode.Space) },
            });

            if (_log)
            {
                Debug.Log("PoseController 示例：已创建运行时。");
            }
        }
    }
}

