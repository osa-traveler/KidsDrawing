using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#else
using System.IO;
#endif

namespace KidsDrawing
{
    // 今の絵を白背景の画像にして、写真アプリへ保存する。何も描かれていなければ保存しない。
    // エディタでは persistentDataPath/Artworks に書き出す。
    public class ArtworkArchiver : MonoBehaviour
    {
        [SerializeField] CanvasData canvasData;

        // ネイティブの保存処理はメインスレッドで呼ぶ
        readonly ConcurrentQueue<byte[]> encoded = new ConcurrentQueue<byte[]>();

        // 呼び出した時点の絵を保存する。直後にキャンバスを消してよい
        public void SaveCurrent()
        {
            var texture = canvasData.Texture;
            int width = texture.width;
            int height = texture.height;
            AsyncGPUReadback.Request(texture, 0, TextureFormat.RGBA32, request =>
            {
                if (request.hasError)
                {
                    Debug.LogWarning("ArtworkArchiver: GPU readback failed");
                    return;
                }

                byte[] pixels = request.GetData<byte>().ToArray();
                Task.Run(() =>
                {
                    try
                    {
                        if (FlattenOntoWhite(pixels))
                            encoded.Enqueue(ImageConversion.EncodeArrayToPNG(
                                pixels, GraphicsFormat.R8G8B8A8_UNorm, (uint)width, (uint)height));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                });
            });
        }

        void Update()
        {
            while (encoded.TryDequeue(out byte[] png))
                SaveToPhotos(png);
        }

        // プリマルチプライドアルファを白の上に重ねる。描かれたピクセルがあれば true
        static bool FlattenOntoWhite(byte[] pixels)
        {
            bool painted = false;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                int background = 255 - pixels[i + 3];
                if (background != 255)
                    painted = true;
                pixels[i] = (byte)(pixels[i] + background);
                pixels[i + 1] = (byte)(pixels[i + 1] + background);
                pixels[i + 2] = (byte)(pixels[i + 2] + background);
                pixels[i + 3] = 255;
            }
            return painted;
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void KidsDrawing_SaveImageToPhotos(byte[] data, int length);

        static void SaveToPhotos(byte[] png)
        {
            KidsDrawing_SaveImageToPhotos(png, png.Length);
        }
#else
        static void SaveToPhotos(byte[] png)
        {
            string directory = Path.Combine(Application.persistentDataPath, "Artworks");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, $"{DateTime.Now:yyyyMMdd-HHmmss}.png"), png);
        }
#endif
    }
}
