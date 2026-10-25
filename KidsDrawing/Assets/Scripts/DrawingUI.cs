using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KidsDrawing
{
    // 下部ツールバー（7色パレット + 消しゴム）の表示とタップ処理。
    // 色を選ぶとペンになるので、ペン専用のボタンは置かない。
    // 図形は角丸四角形の距離関数シェーダーで描き、テクスチャを使わない。
    public class DrawingUI : MonoBehaviour
    {
        [SerializeField] CanvasData canvasData;
        [SerializeField] BrushController brush;
        [SerializeField] Shader shapeShader;
        [SerializeField] Color[] palette =
        {
            new Color32(0x21, 0x21, 0x21, 0xFF),
            new Color32(0xF4, 0x43, 0x36, 0xFF),
            new Color32(0xFF, 0x98, 0x00, 0xFF),
            new Color32(0xFF, 0xEB, 0x3B, 0xFF),
            new Color32(0x4C, 0xAF, 0x50, 0xFF),
            new Color32(0x21, 0x96, 0xF3, 0xFF),
            new Color32(0x9C, 0x27, 0xB0, 0xFF),
        };

        [Header("Layout (pt)")]
        [SerializeField] float buttonDiameterPt = 72f;
        [SerializeField] float buttonSpacingPt = 100f;
        [SerializeField] float eraserGapPt = 40f;
        [SerializeField] float selectedScale = 1.15f;
        [SerializeField] float ringGapPt = 4f;
        [SerializeField] float ringWidthPt = 6f;

        [Header("Colors")]
        [SerializeField] Color ringColor = new Color32(0x5D, 0x5A, 0x55, 0xFF);
        [SerializeField] Color toolbarColor = new Color32(0xEE, 0xEB, 0xE6, 0xFF);
        [SerializeField] Color outlineColor = new Color(0f, 0f, 0f, 0.12f);
        [SerializeField] Color eraserButtonColor = Color.white;
        [SerializeField] Color eraserBodyColor = new Color32(0xF8, 0xBB, 0xD0, 0xFF);
        [SerializeField] Color eraserSleeveColor = new Color32(0x64, 0xB5, 0xF6, 0xFF);

        int EraserIndex => palette.Length;

        Vector2[] buttonCenters;
        int selected;
        float pt;

        Material material;
        Mesh mesh;
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector4> shapes = new List<Vector4>();
        readonly List<Vector2> radii = new List<Vector2>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> indices = new List<int>();

        void Start()
        {
            pt = canvasData.ScreenPixelsPerPt;
            LayOutButtons();

            selected = System.Array.IndexOf(palette, brush.Color);
            if (selected < 0)
                selected = 1;
            Select(selected);

            material = new Material(shapeShader);
            mesh = new Mesh { name = "Toolbar" };
            var toolbar = new GameObject("Toolbar");
            toolbar.transform.SetParent(transform, false);
            toolbar.transform.localPosition = new Vector3(0f, 0f, -1f);
            toolbar.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = toolbar.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            RebuildMesh();
        }

        void OnDestroy()
        {
            Destroy(material);
            Destroy(mesh);
        }

        void Update()
        {
            // 押した瞬間に反応させる。描画中の指とは独立に扱う
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                    HandleTap(touch.position);
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
                HandleTap(Input.mousePosition);
#endif
        }

        void HandleTap(Vector2 screenPosition)
        {
            if (!canvasData.ToolbarScreenRect.Contains(screenPosition))
                return;

            // 一番近いボタンを選ぶ。ボタンの間を押しても何かが選ばれるようにする
            int nearest = -1;
            float nearestDistance = buttonSpacingPt * pt * 0.75f;
            for (int i = 0; i < buttonCenters.Length; i++)
            {
                float distance = Mathf.Abs(screenPosition.x - buttonCenters[i].x);
                if (distance < nearestDistance)
                {
                    nearest = i;
                    nearestDistance = distance;
                }
            }

            if (nearest < 0 || nearest == selected)
                return;
            Select(nearest);
            RebuildMesh();
        }

        void Select(int index)
        {
            selected = index;
            if (index == EraserIndex)
                brush.SelectEraser();
            else
                brush.SelectColor(palette[index]);
        }

        void LayOutButtons()
        {
            buttonCenters = new Vector2[palette.Length + 1];
            float totalWidth = (palette.Length - 1) * buttonSpacingPt + eraserGapPt + buttonSpacingPt;
            float x = (Screen.width / pt - totalWidth) * 0.5f;
            float y = canvasData.ToolbarScreenRect.center.y;
            for (int i = 0; i < palette.Length; i++)
            {
                buttonCenters[i] = new Vector2(x * pt, y);
                x += buttonSpacingPt;
            }
            buttonCenters[EraserIndex] = new Vector2((x + eraserGapPt) * pt, y);
        }

        void RebuildMesh()
        {
            vertices.Clear();
            shapes.Clear();
            radii.Clear();
            colors.Clear();
            indices.Clear();

            for (int i = 0; i < buttonCenters.Length; i++)
            {
                Vector2 center = buttonCenters[i];
                float radius = buttonDiameterPt * 0.5f * pt * (i == selected ? selectedScale : 1f);

                if (i == selected)
                {
                    float ringInner = radius + ringGapPt * pt;
                    AddCircle(center, ringInner + ringWidthPt * pt, ringColor);
                    AddCircle(center, ringInner, toolbarColor);
                }

                AddCircle(center, radius + 1.5f * pt, outlineColor);
                if (i == EraserIndex)
                {
                    AddCircle(center, radius, eraserButtonColor);
                    AddEraserIcon(center, radius);
                }
                else
                {
                    AddCircle(center, radius, palette[i]);
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, shapes);
            mesh.SetUVs(1, radii);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
        }

        // 斜めに置いたピンクの消しゴムに、青い紙スリーブを巻いた形
        void AddEraserIcon(Vector2 center, float buttonRadius)
        {
            const float angle = 30f;
            float halfLength = buttonRadius * 0.62f;
            float halfHeight = buttonRadius * 0.34f;
            AddShape(center, new Vector2(halfLength, halfHeight), halfHeight * 0.45f, angle, eraserBodyColor);

            Vector2 axis = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            AddShape(center + axis * (halfLength * 0.3f), new Vector2(halfLength * 0.3f, halfHeight + 1f), 2f * pt, angle,
                eraserSleeveColor);
        }

        void AddCircle(Vector2 center, float radius, Color color)
        {
            AddShape(center, new Vector2(radius, radius), radius, 0f, color);
        }

        void AddShape(Vector2 center, Vector2 halfSize, float cornerRadius, float angle, Color color)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            Vector2 extent = halfSize + Vector2.one;
            int start = vertices.Count;
            for (int i = 0; i < 4; i++)
            {
                var local = new Vector2(i < 2 ? -extent.x : extent.x, i == 0 || i == 3 ? -extent.y : extent.y);
                vertices.Add(center + (Vector2)(rotation * local));
                shapes.Add(new Vector4(local.x, local.y, halfSize.x, halfSize.y));
                radii.Add(new Vector2(cornerRadius, 0f));
                colors.Add(color);
            }

            indices.Add(start);
            indices.Add(start + 1);
            indices.Add(start + 2);
            indices.Add(start);
            indices.Add(start + 2);
            indices.Add(start + 3);
        }
    }
}
