using ImGuiNET;
using System.Linq;

namespace BetaSharp.Profiling;

public static class ProfilerRenderer
{
    private class ProfilerNode
    {
        public string Name;
        public Dictionary<string, ProfilerNode> Children = new();
        public double Last;
        public double Avg;
        public double Max;
        public double[] History;
        public int HistoryIndex;
        public bool HasData;

        public ProfilerNode(string name) { Name = name; }
    }

    private static int _sortColumn = 0; // 0=Section,1=Cur,2=Avg,3=Max
    private static bool _sortDescending = true;

    public static void Draw()
    {
        ImGui.Begin("Profiler");
        
        ImGui.Text("Sort by:");
        ImGui.SameLine();
        if (ImGui.RadioButton("Section", _sortColumn == 0)) _sortColumn = 0;
        ImGui.SameLine();
        if (ImGui.RadioButton("Cur", _sortColumn == 1)) _sortColumn = 1;
        ImGui.SameLine();
        if (ImGui.RadioButton("Avg", _sortColumn == 2)) _sortColumn = 2;
        ImGui.SameLine();
        if (ImGui.RadioButton("Max", _sortColumn == 3)) _sortColumn = 3;
        ImGui.SameLine();
        if (ImGui.SmallButton(_sortDescending ? "Desc" : "Asc")) _sortDescending = !_sortDescending;

        if (ImGui.BeginTable("ProfilerStats", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Section");
            ImGui.TableSetupColumn("Cur (ms)");
            ImGui.TableSetupColumn("Avg (ms)");
            ImGui.TableSetupColumn("Max (ms)");
            ImGui.TableHeadersRow();

            var stats = Profiler.GetStats();
            var root = new ProfilerNode("Root");
            foreach (var (Name, Last, Avg, Max, History, HistoryIndex) in stats)
            {
                var parts = Name.Split('/');
                var current = root;
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (!current.Children.TryGetValue(part, out ProfilerNode value))
                    {
                        value = new ProfilerNode(part);
                        current.Children[part] = value;
                    }
                    current = value;
                }
                current.Last = Last;
                current.Avg = Avg;
                current.Max = Max;
                current.HasData = true;
            }

            CalculateGroupTotals(root);

            int sortColumn = _sortColumn;
            ImGuiSortDirection sortDirection = _sortDescending ? ImGuiSortDirection.Descending : ImGuiSortDirection.Ascending;

            RenderProfilerNode(root, sortColumn, sortDirection);

            ImGui.EndTable();
        }
        ImGui.End();
    }


    private static void CalculateGroupTotals(ProfilerNode node)
    {
        if (node.Children.Count == 0) return;

        foreach (var child in node.Children.Values)
        {
            CalculateGroupTotals(child);
        }

        if (!node.HasData)
        {
            double sumLast = 0;
            double sumAvg = 0;
            double sumMax = 0;
            bool hasChildData = false;

            if (node.History == null)
            {
                node.History = new double[Profiler.HistoryLength];
            }
            else
            {
                System.Array.Clear(node.History, 0, node.History.Length);
            }

            foreach (var child in node.Children.Values)
            {
                if (child.HasData)
                {
                    sumLast += child.Last;
                    sumAvg += child.Avg;
                    sumMax += child.Max;
                    hasChildData = true;

                    if (child.History != null)
                    {
                        for (int i = 0; i < Profiler.HistoryLength; i++)
                        {
                            node.History[i] += child.History[i];
                        }
                        node.HistoryIndex = child.HistoryIndex;
                    }
                }
            }

            if (hasChildData)
            {
                node.Last = sumLast;
                node.Avg = sumAvg;
                node.Max = sumMax;
                node.HasData = true;
            }
        }
    }

    private static int CompareNodes(ProfilerNode a, ProfilerNode b, int sortColumn, ImGuiSortDirection direction)
    {
        int result;
        switch (sortColumn)
        {
            case 1: // Cur (ms)
                result = a.Last.CompareTo(b.Last);
                break;
            case 2: // Avg (ms)
                result = a.Avg.CompareTo(b.Avg);
                break;
            case 3: // Max (ms)
                result = a.Max.CompareTo(b.Max);
                break;
            case 0:
            default: // Section
                result = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                break;
        }

        if (direction == ImGuiSortDirection.Descending)
        {
            result = -result;
        }

        return result;
    }

    private static void RenderProfilerNode(ProfilerNode node, int sortColumn, ImGuiSortDirection sortDirection)
    {
        var children = node.Children.Values.ToList();
        children.Sort((a, b) => CompareNodes(a, b, sortColumn, sortDirection));

        foreach (var child in children)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            bool isLeaf = child.Children.Count == 0;
            bool open = true;

            if (!isLeaf)
            {
                open = ImGui.TreeNodeEx(child.Name, ImGuiTreeNodeFlags.DefaultOpen);
            }
            else
            {
                ImGui.TreeNodeEx(child.Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Bullet);
            }

            if (child.HasData)
            {
                ImGui.TableNextColumn();
                ImGui.Text($"{child.Last:F3}");
                ImGui.TableNextColumn();
                ImGui.Text($"{child.Avg:F3}");
                ImGui.TableNextColumn();
                ImGui.Text($"{child.Max:F3}");
            }
            else
            {
                ImGui.TableNextColumn(); ImGui.Text("-");
                ImGui.TableNextColumn(); ImGui.Text("-");
                ImGui.TableNextColumn(); ImGui.Text("-");
            }

            if (!isLeaf && open)
            {
                RenderProfilerNode(child, sortColumn, sortDirection);
                ImGui.TreePop();
            }
        }
    }

    private static string _selectedNodePath;

    public static void DrawGraph()
    {
        ImGui.Begin("Frame Time Graph");

        var stats = Profiler.GetStats();

        var root = new ProfilerNode("Root");
        foreach (var (Name, Last, Avg, Max, History, HistoryIndex) in stats)
        {
            var parts = Name.Split('/');
            var current = root;
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (!current.Children.TryGetValue(part, out ProfilerNode value))
                {
                    value = new ProfilerNode(part);
                    current.Children[part] = value;
                }
                current = value;
            }
            current.Last = Last;
            current.Avg = Avg;
            current.Max = Max;
            current.History = History;
            current.HistoryIndex = HistoryIndex;
            current.HasData = true;
        }
        CalculateGroupTotals(root);

        ImGui.Columns(2, "GraphColumns", true);

        RenderSelectionTree(root, "");

        ImGui.NextColumn();

        if (!string.IsNullOrEmpty(_selectedNodePath))
        {
            ProfilerNode selectedNode = null;
            var parts = _selectedNodePath.Split('/');
            var current = root;
            bool found = true;
            foreach (var part in parts)
            {
                if (current != null && current.Children.TryGetValue(part, out var child))
                {
                    current = child;
                }
                else
                {
                    found = false;
                    break;
                }
            }
            if (found && current != null) selectedNode = current;

            if (selectedNode != null && selectedNode.History != null)
            {
                ImGui.Text($"Graph: {_selectedNodePath}");
                var drawList = ImGui.GetWindowDrawList();
                var p = ImGui.GetCursorScreenPos();
                var avail = ImGui.GetContentRegionAvail();
                var width = avail.X;
                var height = 200.0f;

                ImGui.Dummy(new System.Numerics.Vector2(width, height));

                drawList.AddRectFilled(p, p + new System.Numerics.Vector2(width, height), ImGui.GetColorU32(new System.Numerics.Vector4(0, 0, 0, 0.5f)));

                float scaleY = height / 50.0f;
                float y60 = p.Y + height - (16.6f * scaleY);
                float y30 = p.Y + height - (33.3f * scaleY);
                float y144 = p.Y + height - (6.94f * scaleY);

                drawList.AddLine(new System.Numerics.Vector2(p.X, y60), new System.Numerics.Vector2(p.X + width, y60), ImGui.GetColorU32(new System.Numerics.Vector4(0, 1, 0, 0.5f)));
                drawList.AddLine(new System.Numerics.Vector2(p.X, y30), new System.Numerics.Vector2(p.X + width, y30), ImGui.GetColorU32(new System.Numerics.Vector4(1, 1, 0, 0.5f)));
                drawList.AddLine(new System.Numerics.Vector2(p.X, y144), new System.Numerics.Vector2(p.X + width, y144), ImGui.GetColorU32(new System.Numerics.Vector4(0, 1, 1, 0.5f)));

                int count = Profiler.HistoryLength;
                float barWidth = width / count;
                int startIndex = selectedNode.HistoryIndex;
                var color = ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 0.6f, 1.0f, 1.0f));

                for (int i = 0; i < count; i++)
                {
                    int index = (startIndex + i) % count;
                    double val = selectedNode.History[index];
                    float h = (float)val * scaleY;

                    if (h > 0)
                    {
                        float x = p.X + (i * barWidth);
                        float yBase = p.Y + height;
                        drawList.AddRectFilled(
                            new System.Numerics.Vector2(x, yBase - h),
                            new System.Numerics.Vector2(x + barWidth, yBase),
                            color);
                    }
                }

                string label60 = "16.6ms (60 FPS)";
                string label30 = "33.3ms (30 FPS)";
                string label144 = "6.9ms (144 FPS)";
                drawList.AddText(new System.Numerics.Vector2(p.X + 2, y60 - 14), ImGui.GetColorU32(new System.Numerics.Vector4(0, 1, 0, 0.8f)), label60);
                drawList.AddText(new System.Numerics.Vector2(p.X + 2, y30 - 14), ImGui.GetColorU32(new System.Numerics.Vector4(1, 1, 0, 0.8f)), label30);
                drawList.AddText(new System.Numerics.Vector2(p.X + 2, y144 - 14), ImGui.GetColorU32(new System.Numerics.Vector4(0, 1, 1, 0.8f)), label144);
            }
            else
            {
                ImGui.Text("No history data for selection.");
            }
        }
        else
        {
            ImGui.Text("Select a node to view graph.");
        }

        ImGui.Columns(1);
        ImGui.End();
    }

    private static void RenderSelectionTree(ProfilerNode node, string parentPath)
    {
        foreach (var child in node.Children.Values)
        {
            string fullPath = string.IsNullOrEmpty(parentPath) ? child.Name : parentPath + "/" + child.Name;

            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
            if (child.Children.Count == 0) flags |= ImGuiTreeNodeFlags.Leaf;
            if (_selectedNodePath == fullPath) flags |= ImGuiTreeNodeFlags.Selected;

            bool open = ImGui.TreeNodeEx(child.Name, flags);
            if (ImGui.IsItemClicked())
            {
                _selectedNodePath = fullPath;
            }

            if (open)
            {
                RenderSelectionTree(child, fullPath);
                ImGui.TreePop();
            }
        }
    }

}
