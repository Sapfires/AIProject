using UnityEngine;
using UnityEngine.UI;

namespace AIGames.Common
{
    /// <summary>
    /// Small on-screen info panel. Uses a Screen Space - Camera canvas so it also shows up
    /// in camera renders (screenshots for the documentation).
    /// </summary>
    public class DemoHUD : MonoBehaviour
    {
        static DemoHUD instance;
        Text title;
        Text body;

        public static void Show(string heading, string text)
        {
            if (instance == null)
                instance = Create();
            instance.title.text = heading;
            instance.body.text = text;
        }

        static DemoHUD Create()
        {
            var go = new GameObject("Demo HUD");
            var hud = go.AddComponent<DemoHUD>();
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 1;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(go.transform, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(24, -24);
            rt.sizeDelta = new Vector2(470, 150);
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hud.title = MakeText(panel.transform, font, 26, FontStyle.Bold, new Vector2(16, -12), 36);
            hud.body = MakeText(panel.transform, font, 20, FontStyle.Normal, new Vector2(16, -50), 96);
            return hud;
        }

        static Text MakeText(Transform parent, Font font, int size, FontStyle style, Vector2 pos, float height)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(440, height);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
