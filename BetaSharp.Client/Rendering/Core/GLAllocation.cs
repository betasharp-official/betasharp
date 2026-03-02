
namespace BetaSharp.Client.Rendering.Core;

public class GLAllocation
{
    private static readonly List<int> displayLists = new();
    private static readonly List<int> textureNames = new();
    private static readonly object l = new();
    public static int generateDisplayLists(int var0)
    {
        lock (l)
        {
            int var1 = (int)GLManager.GL.GenLists((uint)var0);
            displayLists.Add(var1);
            displayLists.Add(var0);
            return var1;
        }
    }

    public static void func_28194_b(int var0)
    {
        lock (l)
        {
            int var1 = displayLists.IndexOf(var0);
            int list = displayLists[var1];
            int range = displayLists[var1 + 1];
            GLManager.GL.DeleteLists((uint)list, (uint)range);
            displayLists.RemoveAt(var1);
            displayLists.RemoveAt(var1);
        }
    }

    public static void deleteTexturesAndDisplayLists()
    {
        lock (l)
        {
            for (int var0 = 0; var0 < displayLists.Count; var0 += 2)
            {
                int list = displayLists[var0];
                int range = displayLists[var0 + 1];
                GLManager.GL.DeleteLists((uint)list, (uint)range);
            }

            if (textureNames.Count > 0)
            {
                uint[] textureIds = new uint[textureNames.Count];
                for (int i = 0; i < textureNames.Count; i++)
                {
                    textureIds[i] = (uint)textureNames[i];
                }
                GLManager.GL.DeleteTextures(textureIds);
            }

            displayLists.Clear();
            textureNames.Clear();
        }
    }

}
