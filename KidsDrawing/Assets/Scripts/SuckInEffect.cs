using UnityEngine;
using UnityEngine.Rendering;

namespace KidsDrawing
{
    // 描いた絵を小さな粒に分けて、指定した点へ吸い込ませるエフェクト。
    // 開始時に描画テクスチャをコピーし、粒の動きはすべて頂点シェーダーで計算する（CPU処理なし）。
    public class SuckInEffect : MonoBehaviour
    {
        [SerializeField] CanvasData canvasData;
        [SerializeField] Shader shader;
        [SerializeField] float duration = 1.4f;
        [SerializeField] int cellSize = 4;   // 粒1つの大きさ（キャンバスピクセル）

        RenderTexture snapshot;
        Material material;
        Mesh mesh;
        MeshRenderer meshRenderer;
        float startTime;

        public bool IsPlaying => meshRenderer != null && meshRenderer.enabled;

        void Start()
        {
            var canvas = canvasData.Texture;
            snapshot = new RenderTexture(canvas.descriptor)
            {
                name = "SuckInSnapshot",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            snapshot.Create();

            Rect rect = canvasData.ScreenRect;
            material = new Material(shader) { mainTexture = snapshot };
            material.SetVector("_CanvasRect", new Vector4(rect.x, rect.y, rect.width, rect.height));
            material.SetVector("_CellHalfSize", new Vector4(
                0.5f * cellSize / canvas.width, 0.5f * cellSize / canvas.height,
                0.5f * cellSize * rect.width / canvas.width, 0.5f * cellSize * rect.height / canvas.height));

            mesh = BuildMesh(Mathf.CeilToInt((float)canvas.width / cellSize), Mathf.CeilToInt((float)canvas.height / cellSize));

            var effect = new GameObject("SuckInEffect");
            effect.transform.SetParent(transform, false);
            effect.AddComponent<MeshFilter>().sharedMesh = mesh;
            meshRenderer = effect.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = false;
        }

        void OnDestroy()
        {
            if (snapshot != null)
                snapshot.Release();
            Destroy(snapshot);
            Destroy(material);
            Destroy(mesh);
        }

        // 呼び出し時点の絵を写し取って再生する。呼んだ側はこの直後にキャンバスを消してよい
        public void Play(Vector2 targetScreenPosition)
        {
            Graphics.CopyTexture(canvasData.Texture, snapshot);
            material.SetVector("_Target", targetScreenPosition);
            material.SetFloat("_Progress", 0f);
            startTime = Time.unscaledTime;
            meshRenderer.enabled = true;
        }

        void Update()
        {
            if (!IsPlaying)
                return;

            float progress = (Time.unscaledTime - startTime) / duration;
            if (progress >= 1f)
                meshRenderer.enabled = false;
            else
                material.SetFloat("_Progress", progress);
        }

        // 粒ごとに四角形を1枚。頂点位置には粒の中心（キャンバスUV）、UVには角の向きを入れる
        static Mesh BuildMesh(int columns, int rows)
        {
            int count = columns * rows;
            var vertices = new Vector3[count * 4];
            var corners = new Vector2[count * 4];
            var indices = new int[count * 6];
            Vector2[] cornerOffsets = { new(-1f, -1f), new(-1f, 1f), new(1f, 1f), new(1f, -1f) };

            for (int y = 0, cell = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++, cell++)
                {
                    var center = new Vector3((x + 0.5f) / columns, (y + 0.5f) / rows, 0f);
                    int v = cell * 4;
                    for (int i = 0; i < 4; i++)
                    {
                        vertices[v + i] = center;
                        corners[v + i] = cornerOffsets[i];
                    }

                    int t = cell * 6;
                    indices[t] = v;
                    indices[t + 1] = v + 1;
                    indices[t + 2] = v + 2;
                    indices[t + 3] = v;
                    indices[t + 4] = v + 2;
                    indices[t + 5] = v + 3;
                }
            }

            var mesh = new Mesh { name = "SuckInParticles", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.uv = corners;
            mesh.triangles = indices;
            // 頂点シェーダーで画面上を動かすので、カリングされないよう大きくしておく
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
            return mesh;
        }
    }
}
