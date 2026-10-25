using UnityEngine;

namespace KidsDrawing
{
    // タッチ入力を受け取り、最初にキャンバスに触れた1本の指だけで描画する。
    public class InputController : MonoBehaviour
    {
        const int NoFinger = int.MinValue;
        const int MouseFinger = -1;

        [SerializeField] CanvasData canvasData;
        [SerializeField] CanvasPainter painter;
        [SerializeField] BrushController brush;

        int activeFinger = NoFinger;
        Vector2 lastPoint;

        // 描き始めた時点のブラシ設定。描画中に色を変えても同じストロークは同じ色で続ける
        float strokeRadius;
        Color strokeColor;
        bool strokeErase;

        void Update()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                HandlePointer(touch.fingerId, touch.position, touch.phase);
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.touchCount == 0)
            {
                Vector2 mouse = Input.mousePosition;
                if (Input.GetMouseButtonDown(0))
                    HandlePointer(MouseFinger, mouse, TouchPhase.Began);
                else if (Input.GetMouseButtonUp(0))
                    HandlePointer(MouseFinger, mouse, TouchPhase.Ended);
                else if (Input.GetMouseButton(0))
                    HandlePointer(MouseFinger, mouse, TouchPhase.Moved);
            }
#endif
        }

        void HandlePointer(int fingerId, Vector2 screenPosition, TouchPhase phase)
        {
            if (activeFinger == NoFinger)
            {
                if (phase == TouchPhase.Began && canvasData.ScreenRect.Contains(screenPosition))
                    BeginStroke(fingerId, canvasData.ScreenToCanvas(screenPosition));
                return;
            }

            if (fingerId != activeFinger)
                return;

            switch (phase)
            {
                case TouchPhase.Moved:
                    ContinueStroke(canvasData.ScreenToCanvas(screenPosition));
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    ContinueStroke(canvasData.ScreenToCanvas(screenPosition));
                    activeFinger = NoFinger;
                    break;
            }
        }

        void BeginStroke(int fingerId, Vector2 point)
        {
            activeFinger = fingerId;
            strokeRadius = canvasData.PtToCanvasPixels(brush.DiameterPt) * 0.5f;
            strokeColor = brush.Color;
            strokeErase = brush.Mode == BrushMode.Eraser;

            // 触れた瞬間に色が出るよう、長さ0の線分（円）を描く
            lastPoint = point;
            painter.DrawSegment(point, point, strokeRadius, strokeColor, strokeErase);
        }

        void ContinueStroke(Vector2 point)
        {
            if (point == lastPoint)
                return;
            painter.DrawSegment(lastPoint, point, strokeRadius, strokeColor, strokeErase);
            lastPoint = point;
        }
    }
}
