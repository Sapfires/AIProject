using System.IO;
using UnityEngine;

namespace AIGames.EditorTools
{
    /// <summary>
    /// Renders the main camera into a fixed-size PNG (independent of the Game view size).
    /// Used to take the screenshots for the documentation.
    /// </summary>
    public static class CaptureUtil
    {
        public static string Capture(string fileName, int width = 1600, int height = 900)
        {
            var cam = Camera.main;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            cam.targetTexture = prevTarget;
            rt.Release();
            Object.DestroyImmediate(rt);

            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Docs", "img");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, fileName + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return path;
        }
    }
}
