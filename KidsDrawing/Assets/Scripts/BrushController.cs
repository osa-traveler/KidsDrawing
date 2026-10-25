using UnityEngine;

namespace KidsDrawing
{
    public enum BrushMode
    {
        Pen,
        Eraser,
    }

    // ブラシの色・太さ・モードを管理する。
    public class BrushController : MonoBehaviour
    {
        [SerializeField] Color color = new Color32(0xF4, 0x43, 0x36, 0xFF);
        [SerializeField] float penDiameterPt = 32f;
        [SerializeField] float eraserDiameterPt = 64f;

        public BrushMode Mode { get; private set; } = BrushMode.Pen;
        public Color Color => color;
        public float DiameterPt => Mode == BrushMode.Pen ? penDiameterPt : eraserDiameterPt;

        // 色を選ぶとペンに戻る
        public void SelectColor(Color newColor)
        {
            color = newColor;
            Mode = BrushMode.Pen;
        }

        public void SelectEraser()
        {
            Mode = BrushMode.Eraser;
        }
    }
}
