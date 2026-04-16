namespace BetaSharp.Client.Rendering.Core;

public class VertexArray : IDisposable
{
    private uint id;
    private bool disposed;

    public VertexArray()
    {
        id = GLManager.GL.GenVertexArray();
    }

    public void Bind()
    {
        if (disposed || id == 0)
        {
            throw new Exception("Attempted to bind invalid VertexArray");
        }

        GLManager.GL.BindVertexArray(id);
    }

    public static void Unbind()
    {
        GLManager.GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        GC.SuppressFinalize(this);

        if (id != 0)
        {
            GLManager.GL.DeleteVertexArray(id);
            id = 0;
        }

        disposed = true;
    }
}
