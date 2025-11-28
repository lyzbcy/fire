using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VR 手部追踪同步脚本
/// 注意：默认禁用，避免干扰 XR Ray Interactor 的正常工作
/// 如需启用，请在 Inspector 中手动开启 enableSync
/// </summary>
[RequireComponent(typeof(XRRayInteractor))]
public class VRHandTrackingSync : MonoBehaviour
{
    [Tooltip("是否启用同步优化（默认禁用，避免干扰 XR Ray Interactor）")]
    public bool enableSync = false;
    
    private XRRayInteractor rayInteractor;
    private LineRenderer lineRenderer;
    
    void Start()
    {
        rayInteractor = GetComponent<XRRayInteractor>();
        lineRenderer = GetComponent<LineRenderer>();
        
        // 默认禁用，让 XR Ray Interactor 自己处理
        // 如果用户需要，可以在 Inspector 中手动启用
    }
    
    // 暂时不实现任何更新逻辑，避免干扰 XR Ray Interactor
    // 如果将来需要优化，可以在这里添加，但默认应该禁用
}

