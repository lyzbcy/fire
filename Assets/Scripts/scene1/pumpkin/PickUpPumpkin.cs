using UnityEngine;

public class PickUpPumpkin : MonoBehaviour
{
    [Header("交互设置")]
    public Transform player;
    public Camera mainCam;
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.F;

    [Header("抱起时偏移位置")]
    public float forwardOffset = 1.2f;
    public float heightOffset = -0.3f;

    [Header("赋值")]
    public GameObject 交互Text;       // 拖你叫【交互】的文本对象
    public MonoBehaviour distanceTextScript; // 拖你控制远近显隐的那个脚本

    private bool isPicked = false;
    private Vector3 dropPos;

    void Update()
    {
        float dis = Vector3.Distance(transform.position, player.position);

        if (Input.GetKeyDown(interactKey) && dis <= interactRange)
        {
            if (!isPicked)
                PickUp();
            else
                PutDown();
        }

        if (isPicked)
            FollowCamera();
    }

    void PickUp()
    {
        isPicked = true;

        // 1. 隐藏文本
        if (交互Text != null)
            交互Text.SetActive(false);

        // 2. 禁用距离文本脚本
        if (distanceTextScript != null)
            distanceTextScript.enabled = false;
    }

    void PutDown()
    {
        isPicked = false;
        transform.position = dropPos;

        // 1. 显示文本
        if (交互Text != null)
            交互Text.SetActive(true);

        // 2. 启用距离文本脚本
        if (distanceTextScript != null)
            distanceTextScript.enabled = true;
    }

    void FollowCamera()
    {
        Vector3 pos = mainCam.transform.position
                      + mainCam.transform.forward * forwardOffset
                      + Vector3.up * heightOffset;

        transform.position = pos;
        transform.rotation = mainCam.transform.rotation;
        dropPos = transform.position;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}