#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayerMovementAutoInstaller
{
    static PlayerMovementAutoInstaller()
    {
        EditorApplication.delayCall += ApplyToLoadedScenes;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        ApplyToLoadedScenes();
    }

    private static void ApplyToLoadedScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        bool changed = false;

        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject == null || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            {
                continue;
            }

            if (EditorUtility.IsPersistent(gameObject))
            {
                continue;
            }

            PlayerMovement movement = gameObject.GetComponent<PlayerMovement>();
            bool looksLikePlayerRoot = LooksLikePlayerRoot(gameObject);
            bool hasPlayerAncestor = HasPlayerAncestor(gameObject.transform);

            if (hasPlayerAncestor)
            {
                if (movement != null)
                {
                    Undo.DestroyObjectImmediate(movement);
                    changed = true;
                }

                CharacterController duplicateController = gameObject.GetComponent<CharacterController>();
                if (duplicateController != null)
                {
                    Undo.DestroyObjectImmediate(duplicateController);
                    changed = true;
                }

                continue;
            }

            if (movement != null && !looksLikePlayerRoot)
            {
                Undo.DestroyObjectImmediate(movement);
                changed = true;
            }

            if (!looksLikePlayerRoot)
            {
                continue;
            }

            if (movement == null)
            {
                Undo.AddComponent<PlayerMovement>(gameObject);
                changed = true;
                movement = gameObject.GetComponent<PlayerMovement>();
            }

            Camera playerCamera = FindPlayerCamera(gameObject);
            if (movement != null && movement.playerCamera != playerCamera)
            {
                Undo.RecordObject(movement, "Assign Player Camera");
                movement.playerCamera = playerCamera;
                EditorUtility.SetDirty(movement);
                changed = true;
            }

            Transform movementBasis = FindMovementBasis(gameObject.transform);
            if (movement != null && movement.movementBasis != movementBasis)
            {
                Undo.RecordObject(movement, "Assign Movement Basis");
                movement.movementBasis = movementBasis;
                EditorUtility.SetDirty(movement);
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkAllScenesDirty();
        }
    }

    private static bool LooksLikePlayerRoot(GameObject gameObject)
    {
        if (gameObject.GetComponent<CharacterController>() == null)
        {
            return false;
        }

        string lowerName = gameObject.name.ToLowerInvariant();
        return lowerName.Contains("player") || lowerName.Contains("xr origin") || lowerName.Contains("xrorigin") || lowerName.Contains("rig") || lowerName.Contains("camera offset") || lowerName.Contains("头显") || lowerName.Contains("玩家");
    }

    private static Camera FindPlayerCamera(GameObject rootObject)
    {
        Camera[] childCameras = rootObject.GetComponentsInChildren<Camera>(true);
        foreach (Camera childCamera in childCameras)
        {
            if (childCamera != null && childCamera.isActiveAndEnabled)
            {
                return childCamera;
            }
        }

        foreach (Camera childCamera in childCameras)
        {
            if (childCamera != null)
            {
                return childCamera;
            }
        }

        return Camera.main;
    }

    private static Transform FindMovementBasis(Transform root)
    {
        foreach (Transform child in root)
        {
            if (IsPreferredMovementBasis(child))
            {
                return child;
            }

            Transform nestedMatch = FindMovementBasis(child);
            if (nestedMatch != null)
            {
                return nestedMatch;
            }
        }

        return root;
    }

    private static bool IsPreferredMovementBasis(Transform candidate)
    {
        if (candidate.GetComponent<Camera>() != null)
        {
            return false;
        }

        if (candidate.GetComponent<CharacterController>() != null)
        {
            return false;
        }

        if (candidate.GetComponent<Canvas>() != null)
        {
            return false;
        }

        if (candidate.GetComponentInChildren<Renderer>(true) == null)
        {
            return false;
        }

        string lowerName = candidate.name.ToLowerInvariant();
        if (lowerName.Contains("controller") || lowerName.Contains("hand") || lowerName.Contains("camera") || lowerName.Contains("rig") || lowerName.Contains("offset"))
        {
            return false;
        }

        return lowerName.Contains("player") || lowerName.Contains("model") || lowerName.Contains("body") || lowerName.Contains("avatar") || lowerName.Contains("character");
    }

    private static bool HasPlayerAncestor(Transform transform)
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (LooksLikePlayerRoot(current.gameObject))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
#endif
