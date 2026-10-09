// ScreenDiagnostic.cs — Diagnostic script to capture screen and verify rendering

using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalMatch.Rendering
{
    public class ScreenDiagnostic : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log($"ScreenDiagnostic: Screen size = {Screen.width}x{Screen.height}");
            Debug.Log($"ScreenDiagnostic: Background color = {Camera.main.backgroundColor}");
            Debug.Log($"ScreenDiagnostic: Clear flags = {Camera.main.clearFlags}");
            Debug.Log($"ScreenDiagnostic: URP active = {GraphicsSettings.defaultRenderPipeline != null}");
            if (GraphicsSettings.defaultRenderPipeline != null)
                Debug.Log($"ScreenDiagnostic: Render pipeline = {GraphicsSettings.defaultRenderPipeline.GetType().Name}");
        }

        private void Update()
        {
            // Take a screenshot every few seconds
            if (Time.frameCount == 300 || Time.frameCount == 600 || Time.frameCount == 900)
            {
                CaptureScreenshot();
            }
        }

        private void CaptureScreenshot()
        {
            var width = Screen.width;
            var height = Screen.height;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Read the render texture
            var oldActive = RenderTexture.active;
            RenderTexture.active = new RenderTexture(width, height, 24);

            // This should trigger a render
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            RenderTexture.active = oldActive;

            // Sample colors
            var center = tex.GetPixel(width / 2, height / 2);
            var corner = tex.GetPixel(0, 0);

            Debug.Log($"ScreenDiagnostic: Center pixel = {center}, Corner pixel = {corner}");

            // Save to file
            var bytes = tex.EncodeToPNG();
            var path = Path.Combine(Application.persistentDataPath, $"screenshot_{Time.frameCount}.png");
            File.WriteAllBytes(path, bytes);
            Debug.Log($"ScreenDiagnostic: Screenshot saved to {path}");

            Destroy(tex);
        }
    }
}
