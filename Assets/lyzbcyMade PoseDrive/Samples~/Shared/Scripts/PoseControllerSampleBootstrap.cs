using System.Collections.Generic;
using PoseDrive.Runtime.Core;
using PoseDrive.Runtime.Input;
using PoseDrive.Runtime.Utils;
using UnityEngine;

namespace PoseDrive.Samples.Shared
{
    /// <summary>
    /// 在示例场景中自动搭建 PoseDrive 运行时。
    /// </summary>
    public class PoseDriveSampleBootstrap : MonoBehaviour
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

            if (FindObjectOfType<PoseDriveManager>() != null)
            {
                return;
            }

            GameObject root = new GameObject("PoseDriveSystem");
            WebcamProvider webcam = root.AddComponent<WebcamProvider>();
            PoseDetector detector = root.AddComponent<PoseDetector>();
            ActionClassifier classifier = root.AddComponent<ActionClassifier>();
            PoseInputMapper mapper = root.AddComponent<PoseInputMapper>();
            PoseDriveManager manager = root.AddComponent<PoseDriveManager>();

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
                Debug.Log("PoseDrive 示例：已创建运行时。");
            }
        }
    }
}

