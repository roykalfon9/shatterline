using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Shatterline;

namespace ShatterlineEditor
{
    public static class MenuPresentation
    {
        static readonly Color Cyan = new Color(.3f,.9f,1f);
        static readonly Color Text = new Color(.87f,.94f,1f);

        [MenuItem("Tools/Shatterline/Polish Existing Menus")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            var ui = Object.FindFirstObjectByType<UIManager>();
            if (ui == null) throw new System.InvalidOperationException("Open Game.unity first.");
            var canvas = ui.GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            Undo.RecordObject(scaler, "Fit typography to portrait arena");
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            foreach (var label in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                Undo.RecordObject(label, "Style menu type");
                label.color = Text;
                label.raycastTarget = false;
            }
            foreach (var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                Undo.RecordObject(button, "Style button");
                var image = button.GetComponent<UnityEngine.UI.Image>();
                Undo.RecordObject(image, "Style button surface");
                image.color = Color.white;
                bool primary = button.name == "PlayButton" || button.name == "RetryButton" || button.name == "ResumeButton";
                button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = primary ? Cyan : new Color(.08f,.2f,.3f);
                colors.highlightedColor = primary ? Color.white : new Color(.14f,.38f,.5f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(.12f,.55f,.7f);
                colors.fadeDuration = .1f;
                button.colors = colors;
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.color = primary ? new Color(.02f,.06f,.12f) : Text;
            }
            var menu = Find(canvas, "MainMenuPanel");
            var title = Find(canvas, "Title").GetComponent<TMP_Text>();
            title.text = "SHATTER\nLINE";
            title.fontSize = 86;
            title.color = Cyan;
            title.characterSpacing = 6;
            Place(title.rectTransform, .08f,.60f,.92f,.84f);
            var font = title.font;
            foreach (string name in new[] { "MainMenuPanel", "PausePanel", "GameOverPanel", "LevelClearPanel" })
            {
                var panel = Find(canvas, name);
                var fade = panel.GetComponent<ScreenFade>();
                if (fade == null) fade = Undo.AddComponent<ScreenFade>(panel.gameObject);
                if (name == "MainMenuPanel")
                {
                    var fadeData = new SerializedObject(fade);
                    fadeData.FindProperty("title").objectReferenceValue = title.rectTransform;
                    fadeData.ApplyModifiedProperties();
                }
            }
            Label(menu, "Edition", "N E O N   A R C A D E", 18, font, .1f,.84f,.9f,.89f, Cyan);
            Label(menu, "Tagline", "BREAK THE PATTERN.", 23, font, .05f,.55f,.95f,.6f, Text);
            Place(Find(canvas,"PlayButton"), .22f,.43f,.78f,.505f);
            Place(Find(canvas,"BestScoreText"), .05f,.365f,.95f,.405f);
            Place(menu.Find("QuitButton").GetComponent<RectTransform>(), .32f,.27f,.68f,.325f);
            Place(Find(canvas,"MuteButton"), .69f,.93f,.96f,.975f);
            Find(canvas,"MuteButton").GetComponentInChildren<TMP_Text>().fontSize = 18;
            Label(menu,"ControlHint", "MOVE  A/D OR MOUSE  /  DRAG ON PHONE\nLAUNCH  SPACE OR CLICK  /  TAP ON PHONE\nCATCH CAPSULES: WIDE / SLOW / MULTI", 16,font,.05f,.14f,.95f,.235f,Text);
            Label(menu,"RunHint", "5 ARENAS   /   3 LIVES   /   ONE MORE TRY", 15,font,.04f,.08f,.96f,.12f,Cyan);
            var hud = Find(canvas,"HUDPanel");
            Label(hud,"LaunchHint","SPACE / CLICK / TAP TO LAUNCH",18,font,.06f,.13f,.94f,.18f,Text);
            var data = new SerializedObject(ui);
            data.FindProperty("launchHint").objectReferenceValue = hud.Find("LaunchHint").gameObject;
            data.FindProperty("muteLabel").objectReferenceValue = Find(canvas,"MuteButton").GetComponentInChildren<TMP_Text>();
            data.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            EditorSceneManager.SaveScene(ui.gameObject.scene);
            AssetDatabase.SaveAssets();
        }

        static RectTransform Find(Canvas canvas, string name)
        {
            foreach(var rect in canvas.GetComponentsInChildren<RectTransform>(true))
                if(rect.name==name) return rect;
            throw new System.InvalidOperationException($"Missing expected UI element: {name}");
        }

        static void Place(RectTransform rect, float x0,float y0,float x1,float y1)
        {
            Undo.RecordObject(rect,"Position menu content");
            rect.anchorMin=new Vector2(x0,y0); rect.anchorMax=new Vector2(x1,y1);
            rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero;
        }

        static void Label(RectTransform parent,string name,string text,float size,TMP_FontAsset font,
            float x0,float y0,float x1,float y1,Color color)
        {
            var existing = parent.Find(name);
            GameObject go;
            if(existing == null)
            {
                go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(go,"Add menu guidance");
                go.transform.SetParent(parent,false);
            }
            else go=existing.gameObject;
            var label=go.GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(label,"Update guidance");
            label.font=font; label.text=text; label.fontSize=size; label.color=color;
            label.alignment=TextAlignmentOptions.Center; label.raycastTarget=false;
            Place(label.rectTransform,x0,y0,x1,y1);
        }
    }
}
