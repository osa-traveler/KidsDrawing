using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace KidsDrawing
{
    // 描画テクスチャをRGBA PNGとして自動保存し、起動時に復元する。
    // 描画が止まってしばらくしたら非同期で、バックグラウンド移行時は同期で保存する。
    // 一時ファイルに書いてから置き換えるので、保存中に落ちても前の絵は壊れない。
    public class DrawingPersistence : MonoBehaviour
    {
        const string FileName = "drawing.png";

        [SerializeField] CanvasData canvasData;
        [SerializeField] Shader premultiplyShader;
        [SerializeField] float idleSecondsBeforeSave = 2f;

        readonly object fileLock = new object();
        string path;
        int writtenVersion;   // fileLock で保護
        int observedVersion;
        float lastChangeTime;
        volatile bool saving;

        void Start()
        {
            path = Path.Combine(Application.persistentDataPath, FileName);
            Load();
            writtenVersion = observedVersion = canvasData.Version;
        }

        void Update()
        {
            int version = canvasData.Version;
            if (version != observedVersion)
            {
                observedVersion = version;
                lastChangeTime = Time.unscaledTime;
            }

            if (!saving && version != Volatile.Read(ref writtenVersion) &&
                Time.unscaledTime - lastChangeTime >= idleSecondsBeforeSave)
                SaveAsync();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveNow();
        }

        void OnApplicationQuit()
        {
            SaveNow();
        }

        void SaveAsync()
        {
            saving = true;
            var texture = canvasData.Texture;
            int version = canvasData.Version;
            AsyncGPUReadback.Request(texture, 0, TextureFormat.RGBA32, request =>
            {
                if (request.hasError)
                {
                    saving = false;
                    Debug.LogWarning("DrawingPersistence: GPU readback failed");
                    return;
                }

                byte[] pixels = request.GetData<byte>().ToArray();
                Task.Run(() =>
                {
                    try
                    {
                        Write(pixels, texture.width, texture.height, version);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    finally
                    {
                        saving = false;
                    }
                });
            });
        }

        // アプリが止められる前に、メインスレッドで書き終える
        void SaveNow()
        {
            int version = canvasData.Version;
            if (version == Volatile.Read(ref writtenVersion))
                return;

            var texture = canvasData.Texture;
            var request = AsyncGPUReadback.Request(texture, 0, TextureFormat.RGBA32);
            request.WaitForCompletion();
            if (request.hasError)
            {
                Debug.LogWarning("DrawingPersistence: GPU readback failed");
                return;
            }

            try
            {
                Write(request.GetData<byte>().ToArray(), texture.width, texture.height, version);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void Write(byte[] pixels, int width, int height, int version)
        {
            Unpremultiply(pixels);
            byte[] png = ImageConversion.EncodeArrayToPNG(pixels, GraphicsFormat.R8G8B8A8_UNorm, (uint)width, (uint)height);

            lock (fileLock)
            {
                // 古い内容が新しい内容を上書きしないようにする
                if (version - writtenVersion <= 0)
                    return;

                string temporary = path + ".tmp";
                File.WriteAllBytes(temporary, png);
                if (File.Exists(path))
                    File.Replace(temporary, path, null);
                else
                    File.Move(temporary, path);
                Volatile.Write(ref writtenVersion, version);
            }
        }

        // テクスチャはプリマルチプライドアルファ、PNGはストレートアルファ
        static void Unpremultiply(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                int alpha = pixels[i + 3];
                if (alpha == 0 || alpha == 255)
                    continue;
                pixels[i] = (byte)Mathf.Min(255, pixels[i] * 255 / alpha);
                pixels[i + 1] = (byte)Mathf.Min(255, pixels[i + 1] * 255 / alpha);
                pixels[i + 2] = (byte)Mathf.Min(255, pixels[i + 2] * 255 / alpha);
            }
        }

        void Load()
        {
            if (!File.Exists(path))
                return;

            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var material = new Material(premultiplyShader);
            try
            {
                if (image.LoadImage(File.ReadAllBytes(path)))
                    Graphics.Blit(image, canvasData.Texture, material);
                else
                    Debug.LogWarning($"DrawingPersistence: could not decode {path}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                Destroy(image);
                Destroy(material);
            }
        }
    }
}
