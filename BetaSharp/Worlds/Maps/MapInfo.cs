using BetaSharp.Entities;

namespace BetaSharp.Worlds.Maps;

internal class MapInfo
{
    public readonly EntityPlayer Player;
    public int[] StartZ;
    public int[] EndZ;
    private int _nextDirtyPixel;
    private int _colorsUpdateInterval;
    private readonly MapState _mapDataObj;
    private byte[]? _iconsData;

    public MapInfo(MapState state, EntityPlayer player)
    {
        _mapDataObj = state;
        StartZ = new int[128];
        EndZ = new int[128];
        _nextDirtyPixel = 0;
        _colorsUpdateInterval = 0;
        Player = player;

        for (int i = 0; i < StartZ.Length; ++i)
        {
            StartZ[i] = 0;
            EndZ[i] = 127;
        }

    }

    public byte[]? getUpdateData()
    {
        if (--_colorsUpdateInterval < 0)
        {
            _colorsUpdateInterval = 4;
            byte[] data = new byte[_mapDataObj.Icons.Count * 3 + 1];
            data[0] = 1;

            for (int iconIndex = 0; iconIndex < _mapDataObj.Icons.Count; iconIndex++)
            {
                MapIcon icon = _mapDataObj.Icons[iconIndex];
                data[iconIndex * 3 + 1] = (byte)(icon.Type + (icon.Rotation & 15) * 16);
                data[iconIndex * 3 + 2] = icon.X;
                data[iconIndex * 3 + 3] = icon.Z;
            }

            bool isUnchanged = true;
            if (_iconsData != null && _iconsData.Length == data.Length)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] != _iconsData[i])
                    {
                        isUnchanged = false;
                        break;
                    }
                }
            }
            else
            {
                isUnchanged = false;
            }

            if (!isUnchanged)
            {
                _iconsData = data;
                return data;
            }
        }

        for (int i = 0; i < 10; i++)
        {
            int dirtyPixel = _nextDirtyPixel * 11 % 128;
            _nextDirtyPixel++;
            if (StartZ[dirtyPixel] >= 0)
            {
                int stripLength = EndZ[dirtyPixel] - StartZ[dirtyPixel] + 1;
                int startZCoord = StartZ[dirtyPixel];
                byte[] packetData = new byte[stripLength + 3];
                packetData[0] = 0;
                packetData[1] = (byte)dirtyPixel;
                packetData[2] = (byte)startZCoord;

                for (int pixelOffset = 0; pixelOffset < packetData.Length - 3; pixelOffset++)
                {
                    packetData[pixelOffset + 3] = _mapDataObj.Colors[(pixelOffset + startZCoord) * 128 + dirtyPixel];
                }

                EndZ[dirtyPixel] = -1;
                StartZ[dirtyPixel] = -1;
                return packetData;
            }
        }

        return null;
    }
}
