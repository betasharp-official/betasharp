using System.Numerics;
using Hexa.NET.ImGui;

namespace BetaSharp.Client.Diagnostics;

public sealed class FrameGraph(string label, int capacity = 100)
{
    private readonly float[] _values = new float[capacity];
    private int _index;

    private float _customMax;
    private bool _autoScale = true;
    private bool _initializedMax;

    public void Push(float value)
    {
        _values[_index] = value;
        _index = (_index + 1) % capacity;
    }

    public void Draw(float height = 50.0f, float? manualMax = null)
    {
        if (!_initializedMax)
        {
            if (manualMax.HasValue)
            {
                _customMax = manualMax.Value;
                _autoScale = false;
            }
            else
            {
                _customMax = 100.0f;
                _autoScale = true;
            }
            _initializedMax = true;
        }

        if (!string.IsNullOrEmpty(label))
        {
            ImGui.Text(label);
            ImGui.SameLine();
            ImGui.Checkbox($"Auto##auto_{label}", ref _autoScale);
            if (!_autoScale)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150);
                ImGui.DragFloat($"##max_{label}", ref _customMax, _customMax * 0.05f + 1.0f, 0.1f, float.MaxValue, "Max: %.1f");
            }
        }

        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        Vector2 p = ImGui.GetCursorScreenPos();
        float width = ImGui.GetContentRegionAvail().X;

        if (width < 10) width = 100;

        ImGui.Dummy(new Vector2(width, height));

        drawList.AddRectFilled(p, p + new Vector2(width, height), ImGui.GetColorU32(new Vector4(0, 0, 0, 0.4f)));

        float maxValue = _autoScale ? GetMax() : _customMax;
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
