using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KidsDrawing
{
    // 描画テクスチャへのブラシ描画と、キャンバスの画面表示を行う。
    // （UnityEngine.CanvasRenderer と名前が衝突するため CanvasPainter とした）
    //
    // 1本の線分を「カプセル」として四角形1枚で描く。シェーダーが線分までの距離で
    // 丸く塗るので、端と角が丸くなり、座標間の隙間もできない。
    // 合成はGPUのブレンドだけで行うので、一時テクスチャもCPUへの読み戻しも不要。
    public class CanvasPainter : MonoBehaviour
    {
        const int MaxSegmentsPerBatch = 256;
        static readonly Bounds BatchBounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

        [SerializeField] CanvasData canvasData;
        [SerializeField] Camera displayCamera;
        [SerializeField] Shader brushShader;
        [SerializeField] Shader displayShader;
        [SerializeField] Color backgroundColor = Color.white;
        [SerializeField] Color surroundColor = new Color32(0xEE, 0xEB, 0xE6, 0xFF);

        Material penMaterial;
        Material eraserMaterial;
        Material displayMaterial;
        Mesh displayMesh;

        // 1フレームに複数回送るとき、GPUが描く前にメッシュを書き換えないよう送るたびに別のメッシュを使う
        readonly List<Mesh> batchMeshes = new List<Mesh>();
        int batchMeshesUsed;
        CommandBuffer commandBuffer;

        readonly Vector3[] vertices = new Vector3[MaxSegmentsPerBatch * 4];
        readonly Vector4[] segments = new Vector4[MaxSegmentsPerBatch * 4];
        readonly Vector2[] radii = new Vector2[MaxSegmentsPerBatch * 4];
        readonly Color[] colors = new Color[MaxSegmentsPerBatch * 4];
        readonly int[] indices = new int[MaxSegmentsPerBatch * 6];
        int segmentCount;
        bool batchIsEraser;

        void Start()
        {
            penMaterial = CreateBrushMaterial(BlendMode.One, BlendMode.OneMinusSrcAlpha);
            eraserMaterial = CreateBrushMaterial(BlendMode.Zero, BlendMode.OneMinusSrcAlpha);

            commandBuffer = new CommandBuffer { name = "Paint" };

            SetUpDisplay();
        }

        void OnDestroy()
        {
            Destroy(penMaterial);
            Destroy(eraserMaterial);
            Destroy(displayMaterial);
            foreach (var mesh in batchMeshes)
                Destroy(mesh);
            Destroy(displayMesh);
            commandBuffer?.Release();
        }

        // from/to はキャンバスピクセル座標。キャンバス外の部分はGPUで自然に切り取られる
        public void DrawSegment(Vector2 from, Vector2 to, float radius, Color color, bool erase)
        {
            if (segmentCount > 0 && (erase != batchIsEraser || segmentCount == MaxSegmentsPerBatch))
                Flush();
            batchIsEraser = erase;

            // アンチエイリアス用に1px広げたバウンディングボックス
            float extent = radius + 1f;
            Vector2 min = Vector2.Min(from, to) - new Vector2(extent, extent);
            Vector2 max = Vector2.Max(from, to) + new Vector2(extent, extent);
            var segment = new Vector4(from.x, from.y, to.x, to.y);

            int v = segmentCount * 4;
            vertices[v] = new Vector3(min.x, min.y, 0f);
            vertices[v + 1] = new Vector3(min.x, max.y, 0f);
            vertices[v + 2] = new Vector3(max.x, max.y, 0f);
            vertices[v + 3] = new Vector3(max.x, min.y, 0f);
            for (int i = v; i < v + 4; i++)
            {
                segments[i] = segment;
                radii[i] = new Vector2(radius, 0f);
                colors[i] = color;
            }

            int t = segmentCount * 6;
            indices[t] = v;
            indices[t + 1] = v + 1;
            indices[t + 2] = v + 2;
            indices[t + 3] = v;
            indices[t + 4] = v + 2;
            indices[t + 5] = v + 3;
            segmentCount++;
        }

        void LateUpdate()
        {
            Flush();
            batchMeshesUsed = 0;
        }

        void Flush()
        {
            if (segmentCount == 0)
                return;

            if (batchMeshesUsed == batchMeshes.Count)
            {
                var mesh = new Mesh { name = "BrushBatch" };
                mesh.MarkDynamic();
                batchMeshes.Add(mesh);
            }
            var batchMesh = batchMeshes[batchMeshesUsed++];

            int vertexCount = segmentCount * 4;
            batchMesh.Clear(true);
            batchMesh.SetVertices(vertices, 0, vertexCount, MeshUpdateFlags.DontRecalculateBounds);
            batchMesh.SetUVs(0, segments, 0, vertexCount);
            batchMesh.SetUVs(1, radii, 0, vertexCount);
            batchMesh.SetColors(colors, 0, vertexCount);
            batchMesh.SetIndices(indices, 0, segmentCount * 6, MeshTopology.Triangles, 0, false);
            batchMesh.bounds = BatchBounds;

            var texture = canvasData.Texture;
            commandBuffer.Clear();
            commandBuffer.SetRenderTarget(texture);
            commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity,
                Matrix4x4.Ortho(0f, texture.width, 0f, texture.height, -1f, 1f));
            commandBuffer.DrawMesh(batchMesh, Matrix4x4.identity, batchIsEraser ? eraserMaterial : penMaterial);
            Graphics.ExecuteCommandBuffer(commandBuffer);
            canvasData.MarkChanged();

            segmentCount = 0;
        }

        Material CreateBrushMaterial(BlendMode source, BlendMode destination)
        {
            var material = new Material(brushShader);
            material.SetFloat("_SrcBlend", (float)source);
            material.SetFloat("_DstBlend", (float)destination);
            return material;
        }

        // 画面ピクセル = ワールド座標 になる正射影カメラで、キャンバス領域に四角形を1枚置く
        void SetUpDisplay()
        {
            displayCamera.orthographic = true;
            displayCamera.orthographicSize = Screen.height * 0.5f;
            displayCamera.transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, -10f);
            displayCamera.clearFlags = CameraClearFlags.SolidColor;
            displayCamera.backgroundColor = surroundColor;

            Rect rect = canvasData.ScreenRect;
            displayMesh = new Mesh { name = "CanvasDisplay" };
            displayMesh.vertices = new[]
            {
                new Vector3(rect.xMin, rect.yMin, 0f),
                new Vector3(rect.xMin, rect.yMax, 0f),
                new Vector3(rect.xMax, rect.yMax, 0f),
                new Vector3(rect.xMax, rect.yMin, 0f),
            };
            displayMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            displayMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };

            displayMaterial = new Material(displayShader) { mainTexture = canvasData.Texture };
            displayMaterial.SetColor("_BackgroundColor", backgroundColor);

            var display = new GameObject("CanvasDisplay");
            display.transform.SetParent(transform, false);
            display.AddComponent<MeshFilter>().sharedMesh = displayMesh;
            var meshRenderer = display.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = displayMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }
    }
}
