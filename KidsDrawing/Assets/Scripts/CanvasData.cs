using UnityEngine;

namespace KidsDrawing
{
    // 描画テクスチャ（透明背景のRGBA）と、画面上のキャンバス領域を管理する。
    // 将来のエフェクトはここから Texture を参照・加工する。
    [DefaultExecutionOrder(-100)]
    public class CanvasData : MonoBehaviour
    {
        // レイアウトは「画面の高さ = 768pt」とみなした単位で指定する（iPad横向き）
        public const float ReferenceHeightPt = 768f;

        [SerializeField] int maxTextureWidth = 1024;
        [SerializeField] int maxTextureHeight = 768;
        [SerializeField] float toolbarHeightPt = 120f;

        // 中身はプリマルチプライドアルファ。透明 = 何も描かれていない
        public RenderTexture Texture { get; private set; }

        // キャンバスの表示領域（画面ピクセル、左下原点）
        public Rect ScreenRect { get; private set; }

        // ツールバーの領域（画面ピクセル、左下原点）
        public Rect ToolbarScreenRect { get; private set; }

        public float ScreenPixelsPerPt => screenPixelsPerPt;

        // 描画内容が変わるたびに増える。自動保存の判定に使う
        public int Version { get; private set; }

        float screenPixelsPerPt;
        float canvasPixelsPerScreenPixel;

        void Awake()
        {
            screenPixelsPerPt = Screen.height / ReferenceHeightPt;
            float toolbarHeight = toolbarHeightPt * screenPixelsPerPt;
            ScreenRect = new Rect(0f, toolbarHeight, Screen.width, Screen.height - toolbarHeight);
            ToolbarScreenRect = new Rect(0f, 0f, Screen.width, toolbarHeight);

            float scale = Mathf.Min(1f, maxTextureWidth / ScreenRect.width, maxTextureHeight / ScreenRect.height);
            int width = Mathf.RoundToInt(ScreenRect.width * scale);
            int height = Mathf.RoundToInt(ScreenRect.height * scale);
            canvasPixelsPerScreenPixel = width / ScreenRect.width;

            Texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                name = "PaintTexture",
                useMipMap = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            Texture.Create();
            Clear();
        }

        void OnDestroy()
        {
            if (Texture != null)
            {
                Texture.Release();
                Destroy(Texture);
            }
        }

        public void Clear()
        {
            var previous = RenderTexture.active;
            RenderTexture.active = Texture;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = previous;
        }

        public void MarkChanged()
        {
            Version++;
        }

        public Vector2 ScreenToCanvas(Vector2 screenPosition)
        {
            return (screenPosition - ScreenRect.position) * canvasPixelsPerScreenPixel;
        }

        public float PtToCanvasPixels(float pt)
        {
            return pt * screenPixelsPerPt * canvasPixelsPerScreenPixel;
        }
    }
}
