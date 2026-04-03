using Hexa.NET.ImGui;

namespace BetaSharp.Profiling;

public static class ProfilerRenderer
{
    private class ProfilerNode(string name)
    {
        public string Name = name;
        public Dictionary<string, ProfilerNode> Children = new();
        public double Last;
        public double Avg;
        public double Max;
        public double[] History = [];
        public int HistoryHead;
        public bool HasData;
    }

    private static int s_sortColumn; // 0=Section, 1=Cur, 2=Avg, 3=Max
    private static bool s_sortDescending = true;

    public static void Draw()
    {
        ImGui.Begin("Profiler");

        ImGui.Text("Sort by:");
        ImGui.SameLine();
        if (ImGui.RadioButton("Section", s_sortColumn == 0)) s_sortColumn = 0;
        ImGui.SameLine();
        if (ImGui.RadioButton("Cur", s_sortColumn == 1)) s_sortColumn = 1;
        ImGui.SameLine();
        if (ImGui.RadioButton("Avg", s_sortColumn == 2)) s_sortColumn = 2;
        ImGui.SameLine();
        if (ImGui.RadioButton("Max", s_sortColumn == 3)) s_sortColumn = 3;
        ImGui.SameLine();
        if (ImGui.SmallButton(s_sortDescending ? "Desc" : "Asc")) s_sortDescending = !s_sortDescending;

        if (ImGui.BeginTable("ProfilerStats", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Section");
            ImGui.TableSetupColumn("Cur (ms)");
            ImGui.TableSetupColumn("Avg (ms)");
            ImGui.TableSetupColumn("Max (ms)");
            ImGui.TableHeadersRow();

            ProfilerNode root = BuildTree(Profiler.GetStats());
            CalculateGroupTotals(root);
            RenderNode(root, s_sortColumn, s_sortDescending ? ImGuiSortDirection.Descending : ImGuiSortDirection.Ascending);

            ImGui.EndTable();
        }

        ImGui.End();
    }

    private static ProfilerNode BuildTree(IEnumerable<(string Name, double Last, double Avg, double Max, double[] History, int HistoryHead)> stats)
    {
        var root = new ProfilerNode("Root");
        foreach ((string? name, double last, double avg, double max, double[]? history, int historyHead) in stats)
        {
            string[] parts = name.Split('/');
            ProfilerNode current = root;
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (!current.Children.TryGetValue(part, out ProfilerNode? child))
                {
                    child = new ProfilerNode(part);
                    current.Children[part] = child;
                }
                current = child;
            }
            current.Last = last;
            current.Avg = avg;
            current.Max = max;
            current.History = history;
            current.HistoryHead = historyHead;
            current.HasData = true;
        }
        return root;
    }

    private static void CalculateGroupTotals(ProfilerNode node)
    {
        if (node.Children.Count == 0) return;

        foreach (ProfilerNode child in node.Children.Values)
            CalculateGroupTotals(child);

        if (node.HasData) return;

        double sumLast = 0, sumAvg = 0, sumMax = 0;
        bool hasChildData = false;
        node.History = new double[Profiler.HistoryLength];

        foreach (ProfilerNode child in node.Children.Values)
        {
            if (!child.HasData) continue;
            sumLast += child.Last;
            sumAvg += child.Avg;
            sumMax += child.Max;
            hasChildData = true;

            for (int i = 0; i < Profiler.HistoryLength; i++)
                node.History[i] += child.History[i];
            node.HistoryHead = child.HistoryHead;
        }

        if (hasChildData)
        {
            node.Last = sumLast;
            node.Avg = sumAvg;
            node.Max = sumMax;
            node.HasData = true;
        }
    }

    private static int CompareNodes(ProfilerNode a, ProfilerNode b, int sortColumn, ImGuiSortDirection direction)
    {
        int result = sortColumn switch
        {
            1 => a.Last.CompareTo(b.Last),
            2 => a.Avg.CompareTo(b.Avg),
            3 => a.Max.CompareTo(b.Max),
            _ => string.Compare(a.Name, b.Name, StringComparison.Ordinal),
        };
        return direction == ImGuiSortDirection.Descending ? -result : result;
    }

    private static void RenderNode(ProfilerNode node, int sortColumn, ImGuiSortDirection sortDirection)
    {
        List<ProfilerNode> children = [.. node.Children.Values];
        children.Sort((a, b) => CompareNodes(a, b, sortColumn, sortDirection));

        foreach (ProfilerNode child in children)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            bool isLeaf = child.Children.Count == 0;

            if (isLeaf)
            {
                ImGui.TreeNodeEx(child.Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Bullet);
            }
            else
            {
                bool open = ImGui.TreeNodeEx(child.Name, ImGuiTreeNodeFlags.DefaultOpen);
                if (child.HasData)
                {
                    ImGui.TableNextColumn(); ImGui.Text($"{child.Last:F3}");
                    ImGui.TableNextColumn(); ImGui.Text($"{child.Avg:F3}");
                    ImGui.TableNextColumn(); ImGui.Text($"{child.Max:F3}");
                }
                else
                {
                    ImGui.TableNextColumn(); ImGui.Text("-");
                    ImGui.TableNextColumn(); ImGui.Text("-");
                    ImGui.TableNextColumn(); ImGui.Text("-");
                }
                if (open)
                {
                    RenderNode(child, sortColumn, sortDirection);
                    ImGui.TreePop();
                }
                continue;
            }

            if (child.HasData)
            {
                ImGui.TableNextColumn(); ImGui.Text($"{child.Last:F3}");
                ImGui.TableNextColumn(); ImGui.Text($"{child.Avg:F3}");
                ImGui.TableNextColumn(); ImGui.Text($"{child.Max:F3}");
            }
            else
            {
                ImGui.TableNextColumn(); ImGui.Text("-");
                ImGui.TableNextColumn(); ImGui.Text("-");
                ImGui.TableNextColumn(); ImGui.Text("-");
            }
        }
    }
}
