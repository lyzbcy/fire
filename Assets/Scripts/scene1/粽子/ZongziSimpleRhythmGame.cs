using UnityEngine;

public class ZongziSimpleRhythmGame : MonoBehaviour
{
    [Header("互动设置")]
    public KeyCode interactKey = KeyCode.F;

    [Header("UI物体直接拖进来")]
    public GameObject interactPrompt;  // 靠近提示：按F互动
    public GameObject readyTip;         // 游戏开始提示
    public GameObject nowPressTip;      // 现在按F提示
    public GameObject hitTip;           // 命中提示
    public GameObject missTip;          // 错过提示
    public GameObject winTip;           // 成功得代币
    public GameObject loseTip;          // 失败提示

    [Header("游戏参数（宽松好跟上）")]
    public int totalNotes = 5;
    public float waitBeforePrompt = 1.5f;  // 每轮先等待多久
    public float promptStayTime = 0.8f;    // 提示显示多久
    public int needHits = 3;

    private bool playerInRange = false;
    private bool isPlaying = false;
    private int currentNote;
    private int hitCount;
    private float timer;
    private bool canPress;
    private bool showingPrompt;

    void Start()
    {
        readyTip.SetActive(false);
        nowPressTip.SetActive(false);
        hitTip.SetActive(false);
        missTip.SetActive(false);
        winTip.SetActive(false);
        loseTip.SetActive(false);
    }

    void Update()
    {
        // 靠近按F开始游戏
        if (playerInRange && Input.GetKeyDown(interactKey) && !isPlaying)
        {
            Debug.Log("F 键按下成功！开始游戏");
            StartGame();
        }

        if (!isPlaying) return;

        timer += Time.deltaTime;

        // 1. 先等待一段时间，再弹出按F提示
        if (!showingPrompt && timer >= waitBeforePrompt && currentNote < totalNotes)
        {
            ShowNowPressTip();
        }

        // 2. 玩家及时按F
        if (canPress && Input.GetKeyDown(interactKey))
        {
            Hit();
        }

        // 3. 提示时间过完还没按 = 错过
        if (showingPrompt && timer >= waitBeforePrompt + promptStayTime)
        {
            Miss();
            NextNote();
        }

        // 全部结束结算
        if (currentNote >= totalNotes && !showingPrompt)
        {
            EndGame();
        }
    }

    void StartGame()
    {
        isPlaying = true;
        interactPrompt.SetActive(false);
        readyTip.SetActive(true);

        currentNote = 0;
        hitCount = 0;
        timer = 0;
        canPress = false;
        showingPrompt = false;

        Invoke(nameof(HideReadyTip), 1f);
    }

    void HideReadyTip()
    {
        readyTip.SetActive(false);
    }

    void ShowNowPressTip()
    {
        showingPrompt = true;
        canPress = true;
        nowPressTip.SetActive(true);
    }

    void NextNote()
    {
        currentNote++;
        timer = 0;
        canPress = false;
        showingPrompt = false;
        nowPressTip.SetActive(false);
    }

    void Hit()
    {
        hitCount++;
        canPress = false;
        showingPrompt = false;
        nowPressTip.SetActive(false);

        hitTip.SetActive(true);
        Invoke(nameof(HideAllGameTip), 0.6f);

        NextNote();
    }

    void Miss()
    {
        canPress = false;
        showingPrompt = false;
        nowPressTip.SetActive(false);

        missTip.SetActive(true);
        Invoke(nameof(HideAllGameTip), 0.6f);

        NextNote();
    }

    void HideAllGameTip()
    {
        hitTip.SetActive(false);
        missTip.SetActive(false);
        nowPressTip.SetActive(false);
    }

    void EndGame()
    {
        isPlaying = false;
        HideAllGameTip();

        if (hitCount >= needHits)
        {
            winTip.SetActive(true);
        }
        else
        {
            loseTip.SetActive(true);
        }

        Invoke(nameof(ResetAll), 3f);
    }

    void ResetAll()
    {
        winTip.SetActive(false);
        loseTip.SetActive(false);
        playerInRange = true;
        interactPrompt.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            interactPrompt.SetActive(false);
        }
    }
}