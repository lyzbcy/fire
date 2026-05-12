using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HouseEnterPrompt : MonoBehaviour
{
    [SerializeField]
    private Transform player;

    [SerializeField]
    private TextMeshPro promptText;

    [SerializeField]
    private float distanceThreshold = 2.5f;

    [SerializeField]
    private string promptMessage = "按下F进入";

    [SerializeField]
    private Vector3 promptWorldOffset = new Vector3(0f, 2.5f, 0f);

    [SerializeField]
    private string targetSceneName = "1-2TropicalEnvironmentLite_Demo";

    private Collider targetCollider;
    private bool isLoadingScene;

    private void Awake()
    {
        if (player == null)
        {
            player = ResolvePlayerTransform();
        }

        if (promptText == null)
        {
            promptText = GetComponentInChildren<TextMeshPro>(true);
        }

        targetCollider = GetComponent<Collider>();

        if (promptText != null)
        {
            promptText.text = promptMessage;
            promptText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (player == null || promptText == null)
        {
            return;
        }

        float distance = GetDistanceToPlayer();
        bool shouldShow = distance <= distanceThreshold;
        if (promptText.gameObject.activeSelf != shouldShow)
        {
            promptText.gameObject.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            return;
        }

        promptText.text = promptMessage;
        promptText.transform.position = transform.position + promptWorldOffset;
        FacePlayer();

        if (Input.GetKeyDown(KeyCode.F))
        {
            LoadTargetScene();
        }
    }

    private float GetDistanceToPlayer()
    {
        Vector3 playerPosition = player.position;

        if (targetCollider != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(playerPosition);
            return Vector3.Distance(closestPoint, playerPosition);
        }

        return Vector3.Distance(transform.position, playerPosition);
    }

    private Transform ResolvePlayerTransform()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            return playerObject.transform;
        }

        PlayerMovement playerMovement = FindFirstPlayerMovement();
        if (playerMovement != null)
        {
            return playerMovement.transform;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera.transform;
        }

        return null;
    }

    private PlayerMovement FindFirstPlayerMovement()
    {
        PlayerMovement[] movements = Resources.FindObjectsOfTypeAll<PlayerMovement>();
        foreach (PlayerMovement movement in movements)
        {
            if (movement != null && movement.gameObject.scene.IsValid() && movement.gameObject.scene.isLoaded && movement.gameObject.activeInHierarchy)
            {
                return movement;
            }
        }

        return null;
    }

    private void FacePlayer()
    {
        Vector3 targetPosition = player.position;
        targetPosition.y = promptText.transform.position.y;

        promptText.transform.LookAt(targetPosition);
        promptText.transform.Rotate(0f, 180f, 0f);
    }

    private void LoadTargetScene()
    {
        if (isLoadingScene)
        {
            return;
        }

        int buildIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{targetSceneName}.unity");
        if (buildIndex < 0)
        {
            Debug.LogError($"场景未加入 Build Settings: {targetSceneName}");
            return;
        }

        isLoadingScene = true;
        SceneManager.LoadScene(buildIndex);
    }
}