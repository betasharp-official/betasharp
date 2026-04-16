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

    public static void generateTextureNames(Span<int> var0)
    {
        lock (l)
        {
            uint[] textureIds = new uint[var0.Length];
            GLManager.GL.GenTextures(textureIds);

            int[] intIds = Array.ConvertAll(textureIds, id => (int)id);
            var0.CopyTo(intIds);

            for (int var1 = 0; var1 < var0.Length; ++var1)
            {
                textureNames.Add(var0[var1]);
            }
        }
    }

    public static void generateBuffersARB(Span<int> vertexBuffers)
    {
        lock (l)
        {
            uint[] bufferIds = new uint[vertexBuffers.Length];
            GLManager.GL.GenBuffers(bufferIds);
            int[] intIds = Array.ConvertAll(bufferIds, id => (int)id);
            vertexBuffers.CopyTo(intIds);
        }
    }

    public static void deleteBufferARB(int var0)
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

    public static Memory<byte> createDirectByteBuffer(int capacity)
    {
        lock (l)
        {
            Memory<byte> var1 = new byte[capacity];
            return var1;
        }
    }

    public static Memory<int> createDirectIntBuffer(int capacity)
    {
        return new int[capacity];
    }

    public static Memory<float> createDirectFloatBuffer(int capacity)
    {
        return new float[capacity];
    }
}
