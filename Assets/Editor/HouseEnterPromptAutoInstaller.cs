#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class HouseEnterPromptAutoInstaller
{
    private static readonly string[] NameKeywords =
    {
        "house",
        "home",
        "hut",
        "cabin",
        "房",
        "屋"
    };

    static HouseEnterPromptAutoInstaller()
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

            if (!LooksLikeHouse(gameObject.name))
            {
                continue;
            }

            HouseEnterPrompt prompt = gameObject.GetComponent<HouseEnterPrompt>();
            if (prompt == null)
            {
                Undo.AddComponent<HouseEnterPrompt>(gameObject);
                changed = true;
                prompt = gameObject.GetComponent<HouseEnterPrompt>();
            }

            if (EnsurePromptTextChild(gameObject))
            {
                changed = true;
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkAllScenesDirty();
        }
    }

    private static bool LooksLikeHouse(string objectName)
    {
        string lowerName = objectName.ToLowerInvariant();

        for (int index = 0; index < NameKeywords.Length; index++)
        {
            if (lowerName.Contains(NameKeywords[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EnsurePromptTextChild(GameObject rootObject)
    {
        TextMeshPro textComponent = rootObject.GetComponentInChildren<TextMeshPro>(true);
        if (textComponent != null)
        {
            return false;
        }

        GameObject textObject = new GameObject("HouseEnterPromptText");
        Undo.RegisterCreatedObjectUndo(textObject, "Create House Enter Prompt Text");
        textObject.transform.SetParent(rootObject.transform, false);
        textObject.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = Vector3.one;

        TextMeshPro promptText = textObject.AddComponent<TextMeshPro>();
        promptText.text = "按下F进入";
        promptText.fontSize = 4f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;

        if (TMP_Settings.defaultFontAsset != null)
        {
            promptText.font = TMP_Settings.defaultFontAsset;
        }

        return true;
    }
}
#endif