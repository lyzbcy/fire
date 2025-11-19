using UnityEngine;
using TMPro;

public class TextMeshProVisibilityByDistance : MonoBehaviour
{
    // 引用玩家对象
    public Transform player;
    // TextMeshPro 文本对象
    public TextMeshPro textBox;
    // 触发文本框显示的距离阈值
    public float distanceThreshold = 2f;

    void Start()
    {
        // 初始时隐藏文本框
        if (textBox != null)
        {
            textBox.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (player != null && textBox != null)
        {
            // 计算玩家与物体之间的距离
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= distanceThreshold)
            {
                // 距离小于阈值，显示文本框
                textBox.gameObject.SetActive(true);
                // 使文本框始终面向玩家
                FacePlayer();
            }
            else
            {
                // 距离大于阈值，隐藏文本框
                textBox.gameObject.SetActive(false);
            }
        }
    }

    void FacePlayer()
    {
        Vector3 targetPosition = new Vector3(player.position.x, textBox.transform.position.y, player.position.z);
        textBox.transform.LookAt(targetPosition);
        // 由于默认文本正面是 forward，可以加一个旋转修正，如果正面朝后，可用如下代码反转它：
        textBox.transform.Rotate(0, 180f, 0);
    }

}