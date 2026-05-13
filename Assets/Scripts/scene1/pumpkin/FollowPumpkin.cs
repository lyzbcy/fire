using UnityEngine;

public class FollowPumpkin : MonoBehaviour
{
    public Transform pumpkinModel;

    void LateUpdate()
    {
        if (pumpkinModel != null)
        {
            // 只同步位置，不同步旋转，防止模型和碰撞体错位
            pumpkinModel.position = transform.position;
        }
    }
}