namespace BetaSharp.Client.Rendering.Core;

public class GLAllocation
{
    private static readonly List<int> displayLists = new();
    private static readonly List<int> textureNames = new();
    private static readonly object l = new();
    public static int generateDisplayLists(int count)
    {
        lock (l)
        {
            int displayListId = (int)GLManager.GL.GenLists((uint)count);
            displayLists.Add(displayListId);
            displayLists.Add(count);
            return displayListId;
        }
    }

    public static void generateTextureNames(Span<int> textureIds)
    {
        lock (l)
        {
            uint[] textureUIds = new uint[textureIds.Length];
            GLManager.GL.GenTextures(textureUIds);

            int[] intIds = Array.ConvertAll(textureUIds, id => (int)id);
            textureIds.CopyTo(intIds);

            for (int i = 0; i < textureIds.Length; ++i)
            {
                textureNames.Add(textureIds[i]);
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

    public static void deleteBufferARB(int displayListIdToDelete)
    {
        lock (l)
        {
            int index = displayLists.IndexOf(displayListIdToDelete);
            int list = displayLists[index];
            int range = displayLists[index + 1];
            GLManager.GL.DeleteLists((uint)list, (uint)range);
            displayLists.RemoveAt(index);
            displayLists.RemoveAt(index);
        }
    }

    public static void deleteTexturesAndDisplayLists()
    {
        lock (l)
        {
            for (int i = 0; i < displayLists.Count; i += 2)
            {
                int list = displayLists[i];
                int range = displayLists[i + 1];
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
            Memory<byte> buffer = new byte[capacity];
            return buffer;
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
