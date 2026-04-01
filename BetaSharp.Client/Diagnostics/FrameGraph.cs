using System.Numerics;
using ImGuiNET;

namespace BetaSharp.Client.Diagnostics;

public sealed class FrameGraph(string label, int capacity = 100)
{
    private readonly float[] _values = new float[capacity];
    private int _index;

    public void Push(float value)
    {
        _values[_index] = value;
        _index = (_index + 1) % capacity;
    }

    public void Draw(float height = 50.0f, float? manualMax = null)
    {
        if (!string.IsNullOrEmpty(label))
            ImGui.Text(label);

        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        Vector2 p = ImGui.GetCursorScreenPos();
        float width = ImGui.GetContentRegionAvail().X;

        if (width < 10) width = 100;

        ImGui.Dummy(new Vector2(width, height));

        drawList.AddRectFilled(p, p + new Vector2(width, height), ImGui.GetColorU32(new Vector4(0, 0, 0, 0.4f)));

        float maxValue = manualMax ?? GetMax();
        if (maxValue <= 0) maxValue = 1.0f;

        float scaleY = height / maxValue;
        float barWidth = width / capacity;
        uint color = ImGui.GetColorU32(new Vector4(0.2f, 0.7f, 1.0f, 1.0f));

        for (int i = 0; i < capacity; i++)
        {
            int bufferIndex = (_index + i) % capacity;
            float val = _values[bufferIndex];
            float h = val * scaleY;

            if (h > 0)
            {
                if (h > height) h = height;

                float x = p.X + (i * barWidth);
                float yBase = p.Y + height;

                drawList.AddRectFilled(
                    new Vector2(x, yBase - h),
                    new Vector2(x + barWidth, yBase),
                    color);
            }
        }
    }

    private float GetMax()
    {
        float max = 0;
        for (int i = 0; i < capacity; i++)
        {
            if (_values[i] > max)
                max = _values[i];
        }
        return max;
    }
}
