using BetaSharp.Blocks;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;
using BetaSharp.Worlds.Core.Systems;

namespace BetaSharp;

internal class RailLogic
{
    private IWorldContext _level;
    private Vec3i _trackPos;
    private readonly bool _isPoweredRail;
    private readonly List<Vec3i> _connectedTracks = [];
    readonly BlockRail _rail;

    public RailLogic(BlockRail railBlock, IWorldContext level, Vec3i pos)
    {
        _rail = railBlock;
        _level = level;
        _trackPos = pos;

        int blockId = level.Reader.GetBlockId(pos.X, pos.Y, pos.Z);
        int meta = level.Reader.GetBlockMeta(pos.X, pos.Y, pos.Z);

        if (Block.Blocks[blockId] is BlockRail rail && rail.isAlwaysStraight())
        {
            _isPoweredRail = true;
            meta &= -9;
        }
        else
        {
            _isPoweredRail = false;
        }

        SetConnections(meta);
    }

    public void UpdateState(bool powered, bool forceUpdate)
    {
        bool north = AttemptConnectionAt(new Vec3i(_trackPos.X, _trackPos.Y, _trackPos.Z - 1));
        bool south = AttemptConnectionAt(new Vec3i(_trackPos.X, _trackPos.Y, _trackPos.Z + 1));
        bool west = AttemptConnectionAt(new Vec3i(_trackPos.X - 1, _trackPos.Y, _trackPos.Z));
        bool east = AttemptConnectionAt(new Vec3i(_trackPos.X + 1, _trackPos.Y, _trackPos.Z));

        int meta = -1;
        if ((north || south) && !west && !east) meta = 0;
        if ((west || east) && !north && !south) meta = 1;

        if (!_isPoweredRail)
        {
            if (south && east && !north && !west) meta = 6;
            if (south && west && !north && !east) meta = 7;
            if (north && west && !south && !east) meta = 8;
            if (north && east && !south && !west) meta = 9;
        }

        if (meta == -1)
        {
            if (north || south) meta = 0;
            if (west || east) meta = 1;

            if (!_isPoweredRail)
            {
                if (powered)
                {
                    if (south && east) meta = 6;
                    if (west && south) meta = 7;
                    if (east && north) meta = 9;
                    if (north && west) meta = 8;
                }
                else
                {
                    if (north && west) meta = 8;
                    if (east && north) meta = 9;
                    if (west && south) meta = 7;
                    if (south && east) meta = 6;
                }
            }
        }

        if (meta == 0)
        {
            if (BlockRail.isRail(_level, _trackPos.X, _trackPos.Y + 1, _trackPos.Z - 1)) meta = 4;
            if (BlockRail.isRail(_level, _trackPos.X, _trackPos.Y + 1, _trackPos.Z + 1)) meta = 5;
        }

        if (meta == 1)
        {
            if (BlockRail.isRail(_level, _trackPos.X + 1, _trackPos.Y + 1, _trackPos.Z)) meta = 2;
            if (BlockRail.isRail(_level, _trackPos.X - 1, _trackPos.Y + 1, _trackPos.Z)) meta = 3;
        }

        if (meta < 0) meta = 0;

        SetConnections(meta);

        int finalMeta = meta;
        if (_isPoweredRail)
        {
            finalMeta = _level.Reader.GetBlockMeta(_trackPos.X, _trackPos.Y, _trackPos.Z) & 8 | meta;
        }

        if (forceUpdate || _level.Reader.GetBlockMeta(_trackPos.X, _trackPos.Y, _trackPos.Z) != finalMeta)
        {
            _level.Writer.SetBlockMeta(_trackPos.X, _trackPos.Y, _trackPos.Z, finalMeta);

            foreach (Vec3i pos in _connectedTracks)
            {
                RailLogic? logic = GetMinecartTrackLogic(pos);
                if (logic != null)
                {
                    logic.RefreshConnectedTracks();
                    if (logic.CanConnectTo(this))
                    {
                        logic.ConnectTo(this);
                    }
                }
            }
        }
    }

    public static int GetNAdjacentTracks(RailLogic logic) => logic.GetAdjacentTracks();

    private void SetConnections(int meta)
    {
        _connectedTracks.Clear();

        int trackX = _trackPos.X;
        int trackY = _trackPos.Y;
        int trackZ = _trackPos.Z;

        _connectedTracks.AddRange(meta switch
        {
            0 => [new Vec3i(trackX, trackY, trackZ - 1), new Vec3i(trackX, trackY, trackZ + 1)],
            1 => [new Vec3i(trackX - 1, trackY, trackZ), new Vec3i(trackX + 1, trackY, trackZ)],
            2 => [new Vec3i(trackX - 1, trackY, trackZ), new Vec3i(trackX + 1, trackY + 1, trackZ)],
            3 => [new Vec3i(trackX - 1, trackY + 1, trackZ), new Vec3i(trackX + 1, trackY, trackZ)],
            4 => [new Vec3i(trackX, trackY + 1, trackZ - 1), new Vec3i(trackX, trackY, trackZ + 1)],
            5 => [new Vec3i(trackX, trackY, trackZ - 1), new Vec3i(trackX, trackY + 1, trackZ + 1)],
            6 => [new Vec3i(trackX + 1, trackY, trackZ), new Vec3i(trackX, trackY, trackZ + 1)],
            7 => [new Vec3i(trackX - 1, trackY, trackZ), new Vec3i(trackX, trackY, trackZ + 1)],
            8 => [new Vec3i(trackX - 1, trackY, trackZ), new Vec3i(trackX, trackY, trackZ - 1)],
            9 => [new Vec3i(trackX + 1, trackY, trackZ), new Vec3i(trackX, trackY, trackZ - 1)],
            _ => []
        });
    }

    private void RefreshConnectedTracks()
    {
        for (int i = _connectedTracks.Count - 1; i >= 0; i--)
        {
            Vec3i pos = _connectedTracks[i];
            RailLogic? logic = GetMinecartTrackLogic(pos);

            if (logic != null && logic.IsConnectedTo(this))
            {
                _connectedTracks[i] = new Vec3i(logic._trackPos.X, logic._trackPos.Y, logic._trackPos.Z);
            }
            else
            {
                _connectedTracks.RemoveAt(i);
            }
        }
    }

    private bool IsMinecartTrack(Vec3i pos)
    {
        return BlockRail.isRail(_level, pos.X, pos.Y, pos.Z) ||
               BlockRail.isRail(_level, pos.X, pos.Y + 1, pos.Z) ||
               BlockRail.isRail(_level, pos.X, pos.Y - 1, pos.Z);
    }

    private RailLogic? GetMinecartTrackLogic(Vec3i pos)
    {
        if (BlockRail.isRail(_level, pos.X, pos.Y, pos.Z))
            return new RailLogic(_rail, _level, pos);

        if (BlockRail.isRail(_level, pos.X, pos.Y + 1, pos.Z))
            return new RailLogic(_rail, _level, new Vec3i(pos.X, pos.Y + 1, pos.Z));

        if (BlockRail.isRail(_level, pos.X, pos.Y - 1, pos.Z))
            return new RailLogic(_rail, _level, new Vec3i(pos.X, pos.Y - 1, pos.Z));

        return null;
    }

    private bool IsConnectedTo(RailLogic targetLogic)
    {
        foreach (Vec3i pos in _connectedTracks)
        {
            if (pos.X == targetLogic._trackPos.X && pos.Z == targetLogic._trackPos.Z) return true;
        }
        return false;
    }

    private bool IsInTrack(Vec3i pos)
    {
        foreach (Vec3i connectedPos in _connectedTracks)
        {
            if (connectedPos.X == pos.X && connectedPos.Z == pos.Z) return true;
        }
        return false;
    }

    private int GetAdjacentTracks()
    {
        int count = 0;
        if (IsMinecartTrack(new Vec3i(_trackPos.X, _trackPos.Y, _trackPos.Z - 1))) ++count;
        if (IsMinecartTrack(new Vec3i(_trackPos.X, _trackPos.Y, _trackPos.Z + 1))) ++count;
        if (IsMinecartTrack(new Vec3i(_trackPos.X - 1, _trackPos.Y, _trackPos.Z))) ++count;
        if (IsMinecartTrack(new Vec3i(_trackPos.X + 1, _trackPos.Y, _trackPos.Z))) ++count;
        return count;
    }

    private bool CanConnectTo(RailLogic targetLogic)
    {
        if (IsConnectedTo(targetLogic)) return true;
        if (_connectedTracks.Count == 2) return false;
        if (_connectedTracks.Count == 0) return true;

        // This logic originally returned true regardless of the condition in decompiled source.
        // It's a known Beta 1.7.3 quirk. Kept original behavior but cleaned up.
        // Maybe this should be removed!!!
        Vec3i pos = _connectedTracks[0];
        return true;
    }

    private void ConnectTo(RailLogic targetLogic)
    {
        _connectedTracks.Add(new Vec3i(targetLogic._trackPos.X, targetLogic._trackPos.Y, targetLogic._trackPos.Z));

        bool north = IsInTrack(new Vec3i(_trackPos.X, _trackPos.Y, _trackPos.Z - 1));
        bool south = IsInTrack(new Vec3i(_trackPos.X, _trackPos.Y, _trackPos.Z + 1));
        bool west = IsInTrack(new Vec3i(_trackPos.X - 1, _trackPos.Y, _trackPos.Z));
        bool east = IsInTrack(new Vec3i(_trackPos.X + 1, _trackPos.Y, _trackPos.Z));

        int meta = -1;
        if (north || south) meta = 0;
        if (west || east) meta = 1;

        if (!_isPoweredRail)
        {
            if (south && east && !north && !west) meta = 6;
            if (south && west && !north && !east) meta = 7;
            if (north && west && !south && !east) meta = 8;
            if (north && east && !south && !west) meta = 9;
        }

        if (meta == 0)
        {
            if (BlockRail.isRail(_level, _trackPos.X, _trackPos.Y + 1, _trackPos.Z - 1)) meta = 4;
            if (BlockRail.isRail(_level, _trackPos.X, _trackPos.Y + 1, _trackPos.Z + 1)) meta = 5;
        }

        if (meta == 1)
        {
            if (BlockRail.isRail(_level, _trackPos.X + 1, _trackPos.Y + 1, _trackPos.Z)) meta = 2;
            if (BlockRail.isRail(_level, _trackPos.X - 1, _trackPos.Y + 1, _trackPos.Z)) meta = 3;
        }

        if (meta < 0) meta = 0;

        int finalMeta = meta;
        if (_isPoweredRail)
        {
            finalMeta = _level.Reader.GetBlockMeta(_trackPos.X, _trackPos.Y, _trackPos.Z) & 8 | meta;
        }

        _level.Writer.SetBlockMeta(_trackPos.X, _trackPos.Y, _trackPos.Z, finalMeta);
    }

    private bool AttemptConnectionAt(Vec3i pos)
    {
        RailLogic? logic = GetMinecartTrackLogic(pos);
        if (logic == null) return false;

        logic.RefreshConnectedTracks();
        return logic.CanConnectTo(this);
    }
}
