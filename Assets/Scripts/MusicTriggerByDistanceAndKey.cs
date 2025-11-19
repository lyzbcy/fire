using UnityEngine;
using System.Collections;

public class MusicTriggerByDistanceAndKey : MonoBehaviour
{
    public Transform player;
    public KeyCode triggerKey = KeyCode.F;
    private TextMeshProVisibilityByDistance textVisibilityScript;
    public AudioSource musicAudioSource;
    public Camera camera2;

    private Vector3 lastPlayerPosition;
    private bool isInMusicEvent = false;
    private float musicEventEntryTime = 0f;

    public float exitDelay = 0.2f;

    public float jumpDuration = 0.6f; // 控制跳跃总时长
    public float verticalOffset = 0.3f; // 控制上升幅度
    private Coroutine moveCoroutine;

    void Start()
    {
        textVisibilityScript = GetComponent<TextMeshProVisibilityByDistance>();

        if (camera2 != null)
        {
            camera2.gameObject.SetActive(false);
        }

        if (musicAudioSource != null)
        {
            musicAudioSource.enabled = false;
        }
    }

    void Update()
    {
        if (player != null && textVisibilityScript != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            float distanceThreshold = textVisibilityScript.distanceThreshold;

            if (!isInMusicEvent && distance <= distanceThreshold && Input.GetKeyDown(triggerKey))
            {
                StartMusicEvent();
            }

            if (isInMusicEvent)
            {
                if (Time.time - musicEventEntryTime > exitDelay)
                {
                    float horizontalInput = Input.GetAxis("Horizontal");
                    float verticalInput = Input.GetAxis("Vertical");

                    if (Mathf.Abs(horizontalInput) > 0 || Mathf.Abs(verticalInput) > 0)
                    {
                        ExitMusicEvent();
                    }
                }
            }
        }
    }

    void StartMusicEvent()
    {
        isInMusicEvent = true;
        musicEventEntryTime = Time.time;

        if (player != null)
        {
            Vector3 directionToObject = (transform.position - player.position).normalized;
            Vector3 targetPosition = transform.position + Vector3.up * 2f + directionToObject * 0.5f;

            if (moveCoroutine != null)
                StopCoroutine(moveCoroutine);

            moveCoroutine = StartCoroutine(SmoothJumpToPosition(player.position, targetPosition));
        }

        if (camera2 != null)
        {
            camera2.gameObject.SetActive(true);
        }

        if (musicAudioSource != null)
        {
            musicAudioSource.enabled = true;
            musicAudioSource.Play();
        }

        lastPlayerPosition = player.position;
    }

    void ExitMusicEvent()
    {
        isInMusicEvent = false;

        if (camera2 != null)
        {
            camera2.gameObject.SetActive(false);
        }

        if (musicAudioSource != null)
        {
            musicAudioSource.Stop();
            musicAudioSource.enabled = false;
        }

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }

    // 优雅跳跃：轻柔垂直位移 + 水平插值
    IEnumerator SmoothJumpToPosition(Vector3 startPos, Vector3 targetPos)
    {
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;

            // 插值 XZ 平面
            Vector3 horizontal = Vector3.Lerp(startPos, targetPos, t);

            // 插值 Y 值（带缓动）
            float y = Mathf.Lerp(startPos.y, targetPos.y + verticalOffset, Mathf.SmoothStep(0f, 1f, t));
            // 再平稳落地到目标 Y
            y = Mathf.Lerp(y, targetPos.y, t);

            player.position = new Vector3(horizontal.x, y, horizontal.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        player.position = targetPos;
    }
}
