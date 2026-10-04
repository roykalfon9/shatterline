using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor.Events;
using Shatterline;

public class CreateGameWinScreen : EditorWindow
{
    [MenuItem("Tools/Shatterline/Create Game Win Screen")]
    public static void Create()
    {
        GameObject uiManagerObj = GameObject.FindObjectOfType<UIManager>()?.gameObject;
        if (uiManagerObj == null)
        {
            Debug.LogError("SHATTERLINE: Could not find UIManager in scene. Please open Game.unity first.");
            return;
        }

        UIManager uiManager = uiManagerObj.GetComponent<UIManager>();
        
        // Find the Canvas. We assume there's one Canvas in the scene.
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("SHATTERLINE: Could not find Canvas in scene.");
            return;
        }

        // Create GameWinPanel
        GameObject winPanel = new GameObject("GameWinPanel");
        winPanel.transform.SetParent(canvas.transform, false);
        RectTransform rect = winPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        winPanel.SetActive(false);

        // Background Image
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(winPanel.transform, false);
        Image img = bg.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Win Text
        GameObject textObj = new GameObject("WinText");
        textObj.transform.SetParent(winPanel.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "GAME COMPLETE!";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 64;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.6f);
        textRect.anchorMax = new Vector2(1, 0.8f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Button Container
        GameObject btnContainer = new GameObject("ButtonContainer");
        btnContainer.transform.SetParent(winPanel.transform, false);
        RectTransform containerRect = btnContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.3f, 0.3f);
        containerRect.anchorMax = new Vector2(0.7f, 0.5f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = btnContainer.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 20;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        btnContainer.AddComponent<ContentSizeFitter>().verticalFit = (ContentSizeFitter.FitMode)1;

        // Helper to create buttons
        void CreateButton(string label)
        {
            GameObject btnObj = new GameObject(label + "Button");
            btnObj.transform.SetParent(btnContainer.transform, false);
            Button btn = btnObj.AddComponent<Button>();
            
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnText = txtObj.AddComponent<TextMeshProUGUI>();
            btnText.text = label;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontSize = 24;
            btnText.color = Color.white;
            RectTransform btnTextRect = txtObj.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
        }

        CreateButton("Main Menu");
        CreateButton("Start Again");

        // Assign fields to UIManager via reflection (since they are private)
        var type = typeof(UIManager);
        type.GetField("gameWinPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(uiManager, winPanel);
        type.GetField("winText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(uiManager, text);

        EditorUtility.SetDirty(uiManagerObj);
        Debug.Log("SHATTERLINE: Game Win Screen created. IMPORTANT: Please manually assign the onClick events for the buttons to the UIManager in the Inspector.");
    }
}
