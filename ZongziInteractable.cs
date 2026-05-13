using UnityEngine;

public class ZongziInteractable : MonoBehaviour
{
    [Header("互动设置")]
    public float interactRange = 3f; // 互动距离（玩家离模型多远能按E）
    public KeyCode interactKey = KeyCode.E; // 互动按键，默认E
    public string promptText = "按 E 互动"; // 靠近时的提示文字

    private bool playerInRange = false; // 玩家是否在互动范围内

    void Update()
    {
        // 如果玩家在范围内，并且按下E键
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    // 互动逻辑（你可以在这里写自己的功能）
    public virtual void Interact()
    {
        Debug.Log("和 " + gameObject.name + " 互动了！");
        // 👇 在这里加你的自定义互动逻辑
        // 比如：播放动画、打开UI、开门、触发事件等
    }

    // 玩家进入互动范围
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            // 可以在这里显示互动提示，比如 promptText
            Debug.Log(promptText);
        }
    }

    // 玩家离开互动范围
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            // 可以在这里隐藏互动提示
        }
    }
}