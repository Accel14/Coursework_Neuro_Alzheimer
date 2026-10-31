using UnityEngine;
using UnityEngine.UI;

public class EEGGraphUI : MaskableGraphic
{
    [SerializeField] private float lineThickness = 2f;

    private float[] signal;

    public void SetSignal(float[] values)
    {
        signal = values;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (signal == null || signal.Length < 2)
            return;

        Rect rect = GetPixelAdjustedRect();

        float stepX = rect.width / (signal.Length - 1);
        float centerY = rect.y + rect.height / 2f;
        float halfHeight = rect.height * 0.45f;

        for (int i = 0; i < signal.Length - 1; i++)
        {
            float x1 = rect.x + i * stepX;
            float x2 = rect.x + (i + 1) * stepX;

            float y1 = centerY + Mathf.Clamp(signal[i], -1f, 1f) * halfHeight;
            float y2 = centerY + Mathf.Clamp(signal[i + 1], -1f, 1f) * halfHeight;

            Vector2 p1 = new Vector2(x1, y1);
            Vector2 p2 = new Vector2(x2, y2);

            Vector2 direction = (p2 - p1).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x)
                             * lineThickness / 2f;

            int index = vh.currentVertCount;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = p1 - normal;
            vh.AddVert(vertex);

            vertex.position = p1 + normal;
            vh.AddVert(vertex);

            vertex.position = p2 + normal;
            vh.AddVert(vertex);

            vertex.position = p2 - normal;
            vh.AddVert(vertex);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }
}