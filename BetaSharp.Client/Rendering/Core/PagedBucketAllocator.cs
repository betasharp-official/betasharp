using System.Diagnostics;
using System.Numerics;
using Silk.NET.OpenGL;

namespace BetaSharp.Client.Rendering.Core;

internal class PagedBucketAllocator
{
    internal struct AllocationHandle
    {
        public int Bucket;
        public int Page;
        public uint Slot;
    }

    private class Page
    {
        public uint _bufferId;
        public Stack<uint> FreeSlots { get; private set; } = [];
        private readonly Bucket _bucket;
        private readonly GL _gl;

        public unsafe Page(Bucket bucket, GL gl)
        {
            _bucket = bucket;
            _gl = gl;

            _bufferId = gl.CreateBuffer();
            gl.NamedBufferStorage(_bufferId, (nuint)bucket.Size, null, BufferStorageMask.None);

            for (uint i = 0; i < bucket.Slots; i++)
            {
                FreeSlots.Push(i);
            }
        }

        public bool TryAllocate(uint stagingBufferId, int offset, int length, out uint slot)
        {
            System.Diagnostics.Debug.Assert(length <= _bucket.BytesPerSlot);

            if (!FreeSlots.TryPop(out slot))
            {
                return false;
            }

            _gl.CopyNamedBufferSubData(stagingBufferId, _bufferId, offset, (nint)(slot * _bucket.BytesPerSlot), (nuint)length);
            return true;
        }

        public void Deallocate(uint slot)
        {
#if DEBUG
            if (FreeSlots.Contains(slot))
            {
                throw new Exception("Tried to deallocated a slot that already exists!");
            }
#endif
            if (slot >= _bucket.Slots)
            {
                throw new Exception($"Tried to deallocate an invalid slot: {slot}");
            }

            FreeSlots.Push(slot);
        }
    }

    private class Bucket
    {
        public int Size { get; }
        public int Slots { get; }
        public int BytesPerSlot { get; }
        public int Index { get; }

        private readonly List<Page> _pages = [];
        private readonly GL _gl;

        public Bucket(GL gl, int index, int size, int slots)
        {
            _gl = gl;
            Index = index;
            Size = size;
            Slots = slots;
            BytesPerSlot = size / slots;
        }

        public AllocationHandle Allocate(uint stagingBufferId, int offset, int length)
        {
            AllocateNewPageIfNecessary();

            int leastEmptyPageIndex = GetLeastEmptyPageIndex();
            Page page = _pages[leastEmptyPageIndex];

            if (!page.TryAllocate(stagingBufferId, offset, length, out uint slot))
            {
                throw new UnreachableException();
            }

            return new()
            {
                Bucket = Index,
                Page = leastEmptyPageIndex,
                Slot = slot
            };
        }

        public void Deallocate(AllocationHandle handle)
        {
            _pages[handle.Page].Deallocate(handle.Slot);
        }

        private void AllocateNewPageIfNecessary()
        {
            foreach (Page page in _pages)
            {
                if (page.FreeSlots.Count > 0) return;
            }

            _pages.Add(new Page(this, _gl));
        }

        private int GetLeastEmptyPageIndex()
        {
            int leastEmptyIndex = 0;
            for (int i = 1; i < _pages.Count; i++)
            {
                if (_pages[i].FreeSlots.Count < _pages[leastEmptyIndex].FreeSlots.Count)
                    leastEmptyIndex = i;
            }
            return leastEmptyIndex;
        }
    }

    private readonly Bucket[] _buckets = new Bucket[14];

    public AllocationHandle Allocate(uint stagingBufferId, int offset, int length)
    {
        return _buckets[GetBucketIndex(length)].Allocate(stagingBufferId, offset, length);
    }

    public void Deallocate(AllocationHandle handle)
    {
        if (handle.Bucket >= _buckets.Length || handle.Bucket < 0)
        {
            throw new Exception($"Tried to deallocate handle with invalid bucket! {handle.Bucket}");
        }

        _buckets[handle.Bucket].Deallocate(handle);
    }

    private static int GetBucketIndex(int size)
    {
        System.Diagnostics.Debug.Assert(size > 0 && size <= 1 << 20, $"Size out of bucket range: {size}");
        return BitOperations.Log2((uint)size) - 7;
    }
}
