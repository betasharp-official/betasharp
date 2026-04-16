using Silk.NET.Maths;

namespace BetaSharp.Client.Rendering.Chunks.Occlusion;

public interface IChunkVisibilityVisitor
{
    void Visit(SubChunkRenderer renderer);
}

public class ChunkOcclusionCuller
{
    private class ChunkQueue
    {
        private readonly SubChunkRenderer[] _data;
        private int _read;
        private int _write;

        public ChunkQueue(int capacity)
        {
            _data = new SubChunkRenderer[capacity];
            _read = 0;
            _write = 0;
        }

        public void Enqueue(SubChunkRenderer item)
        {
            _data[_write++] = item;
        }

        public SubChunkRenderer? Dequeue()
        {
            if (_read == _write) return null;
            return _data[_read++];
        }

        public void Reset()
        {
            _read = 0;
            _write = 0;
        }

        public bool IsEmpty => _read == _write;
    }

    private readonly ChunkQueue[] _queues = [new(32768), new(32768)];
    private int _currentQueue = 0;

    public void FindVisible(
        IChunkVisibilityVisitor visitor,
        SubChunkRenderer? startNode,
        Vector3D<double> viewPos,
        ICuller culler,
        float renderDistance,
        bool useOcclusionCulling,
        int frame)
    {
        ChunkQueue readQueue = _queues[_currentQueue];
        ChunkQueue writeQueue = _queues[1 - _currentQueue];

        readQueue.Reset();
        writeQueue.Reset();

        if (startNode == null)
        {
            return;
        }

        startNode.LastVisibleFrame = frame;
        startNode.IncomingDirections = ChunkDirectionMask.None;
        visitor.Visit(startNode);

        ChunkDirectionMask initialOutgoing = useOcclusionCulling
            ? startNode.VisibilityData.GetVisibleFrom(ChunkDirectionMask.None, viewPos, startNode)
            : ChunkDirectionMask.All;

        EnqueueNeighbors(writeQueue, startNode, initialOutgoing, frame);

        while (!writeQueue.IsEmpty)
        {
            // Swap queues
            _currentQueue = 1 - _currentQueue;
            readQueue = _queues[_currentQueue];
            writeQueue = _queues[1 - _currentQueue];

            writeQueue.Reset();

            SubChunkRenderer? current;
            while ((current = readQueue.Dequeue()) != null)
            {
                if (!current.IsVisible(culler, viewPos, renderDistance))
                    continue;

                visitor.Visit(current);

                ChunkDirectionMask outgoing;
                if (useOcclusionCulling)
                {
                    outgoing = current.VisibilityData.GetVisibleFrom(current.IncomingDirections, viewPos, current);
                }
                else
                {
                    outgoing = ChunkDirectionMask.All;
                }

                outgoing &= GetOutwardDirections(viewPos, current);

                EnqueueNeighbors(writeQueue, current, outgoing, frame);
            }
        }
    }

    private static void EnqueueNeighbors(ChunkQueue queue, SubChunkRenderer current, ChunkDirectionMask outgoing,
        int frame)
    {
        if (outgoing == ChunkDirectionMask.None) return;

        if ((outgoing & ChunkDirectionMask.Down) != 0)
            VisitNode(queue, current.AdjacentDown, ChunkDirectionMask.Up, frame);
        if ((outgoing & ChunkDirectionMask.Up) != 0)
            VisitNode(queue, current.AdjacentUp, ChunkDirectionMask.Down, frame);
        if ((outgoing & ChunkDirectionMask.North) != 0)
            VisitNode(queue, current.AdjacentNorth, ChunkDirectionMask.South, frame);
        if ((outgoing & ChunkDirectionMask.South) != 0)
            VisitNode(queue, current.AdjacentSouth, ChunkDirectionMask.North, frame);
        if ((outgoing & ChunkDirectionMask.West) != 0)
            VisitNode(queue, current.AdjacentWest, ChunkDirectionMask.East, frame);
        if ((outgoing & ChunkDirectionMask.East) != 0)
            VisitNode(queue, current.AdjacentEast, ChunkDirectionMask.West, frame);
    }

    private static void VisitNode(ChunkQueue queue, SubChunkRenderer? neighbor, ChunkDirectionMask incoming, int frame)
    {
        if (neighbor == null) return;

        if (neighbor.LastVisibleFrame != frame)
        {
            neighbor.LastVisibleFrame = frame;
            neighbor.IncomingDirections = ChunkDirectionMask.None;
            queue.Enqueue(neighbor);
        }

        neighbor.IncomingDirections |= incoming;
    }


    private static ChunkDirectionMask GetOutwardDirections(Vector3D<double> viewPos, SubChunkRenderer renderer)
    {
        int chunkX = renderer.Position.X / SubChunkRenderer.Size;
        int chunkY = renderer.Position.Y / SubChunkRenderer.Size;
        int chunkZ = renderer.Position.Z / SubChunkRenderer.Size;

        int viewChunkX = (int)Math.Floor(viewPos.X / SubChunkRenderer.Size);
        int viewChunkY = (int)Math.Floor(viewPos.Y / SubChunkRenderer.Size);
        int viewChunkZ = (int)Math.Floor(viewPos.Z / SubChunkRenderer.Size);

        ChunkDirectionMask mask = ChunkDirectionMask.None;
        if (chunkX <= viewChunkX) mask |= ChunkDirectionMask.West;
        if (chunkX >= viewChunkX) mask |= ChunkDirectionMask.East;
        if (chunkY <= viewChunkY) mask |= ChunkDirectionMask.Down;
        if (chunkY >= viewChunkY) mask |= ChunkDirectionMask.Up;
        if (chunkZ <= viewChunkZ) mask |= ChunkDirectionMask.North;
        if (chunkZ >= viewChunkZ) mask |= ChunkDirectionMask.South;
        return mask;
    }
}
