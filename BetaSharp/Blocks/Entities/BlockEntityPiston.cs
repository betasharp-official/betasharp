using BetaSharp.Entities;
using BetaSharp.NBT;
using BetaSharp.Util.Maths;

namespace BetaSharp.Blocks.Entities;

public class BlockEntityPiston : BlockEntity
{
    private static readonly ThreadLocal<List<Entity>> s_pushedEntities = new(() => []);
    private readonly bool _source;
    private bool _extending;
    private int _facing;
    private float _lastProgess;
    private float _progress;
    private int _pushedBlockData;
    private int _pushedBlockId;

    public BlockEntityPiston()
    {
    }

    public BlockEntityPiston(int pushedBlockId, int pushedBlockData, int facing, bool extending, bool source)
    {
        _pushedBlockId = pushedBlockId;
        _pushedBlockData = pushedBlockData;
        _facing = facing;
        _extending = extending;
        _source = source;
    }

    public int getPushedBlockId() => _pushedBlockId;

    public override int getPushedBlockData() => _pushedBlockData;

    public bool isExtending() => _extending;

    public int getFacing() => _facing;

    public bool isSource() => _source;

    public float getProgress(float tickDelta)
    {
        if (tickDelta > 1.0F)
        {
            tickDelta = 1.0F;
        }

        return _progress + (_lastProgess - _progress) * tickDelta;
    }

    public float getRenderOffsetX(float tickDelta) => _extending ? (getProgress(tickDelta) - 1.0F) * PistonConstants.HEAD_OFFSET_X[_facing] : (1.0F - getProgress(tickDelta)) * PistonConstants.HEAD_OFFSET_X[_facing];

    public float getRenderOffsetY(float tickDelta) => _extending ? (getProgress(tickDelta) - 1.0F) * PistonConstants.HEAD_OFFSET_Y[_facing] : (1.0F - getProgress(tickDelta)) * PistonConstants.HEAD_OFFSET_Y[_facing];

    public float getRenderOffsetZ(float tickDelta) => _extending ? (getProgress(tickDelta) - 1.0F) * PistonConstants.HEAD_OFFSET_Z[_facing] : (1.0F - getProgress(tickDelta)) * PistonConstants.HEAD_OFFSET_Z[_facing];

    private void pushEntities(float collisionShapeSizeMultiplier, float entityMoveMultiplier)
    {
        if (!_extending)
        {
            --collisionShapeSizeMultiplier;
        }
        else
        {
            collisionShapeSizeMultiplier = 1.0F - collisionShapeSizeMultiplier;
        }

        Box? pushCollisionBox = Block.MovingPiston.getPushedBlockCollisionShape(World, X, Y, Z, _pushedBlockId, collisionShapeSizeMultiplier, _facing);
        if (pushCollisionBox != null)
        {
            List<Entity> entitiesToPush = World.getEntities(null!, pushCollisionBox.Value);
            if (entitiesToPush.Count > 0)
            {
                List<Entity> pushedEntities = s_pushedEntities.Value!;
                pushedEntities.AddRange(entitiesToPush);
                foreach (Entity entity in pushedEntities)
                {
                    entity.move(
                        entityMoveMultiplier * PistonConstants.HEAD_OFFSET_X[_facing],
                        entityMoveMultiplier * PistonConstants.HEAD_OFFSET_Y[_facing],
                        entityMoveMultiplier * PistonConstants.HEAD_OFFSET_Z[_facing]
                    );
                }

                pushedEntities.Clear();
            }
        }
    }

    public void finish()
    {
        if (_progress < 1.0F)
        {
            _progress = _lastProgess = 1.0F;
            World.Entities.RemoveBlockEntity(X, Y, Z);
            markRemoved();
            if (World.getBlockId(X, Y, Z) == Block.MovingPiston.id)
            {
                World.setBlock(X, Y, Z, _pushedBlockId, _pushedBlockData);
            }
        }
    }

    public override void tick()
    {
        _progress = _lastProgess;
        if (_progress >= 1.0F)
        {
            pushEntities(1.0F, 0.25F);
            World.Entities.RemoveBlockEntity(X, Y, Z);
            markRemoved();
            if (World.getBlockId(X, Y, Z) == Block.MovingPiston.id)
            {
                World.setBlock(X, Y, Z, _pushedBlockId, _pushedBlockData);
            }
        }
        else
        {
            _lastProgess += 0.5F;
            if (_lastProgess >= 1.0F)
            {
                _lastProgess = 1.0F;
            }

            if (_extending)
            {
                pushEntities(_lastProgess, _lastProgess - _progress + 1.0F / 16.0F);
            }
        }
    }

    public override void readNbt(NBTTagCompound nbt)
    {
        base.readNbt(nbt);
        _pushedBlockId = nbt.GetInteger("blockId");
        _pushedBlockData = nbt.GetInteger("blockData");
        _facing = nbt.GetInteger("facing");
        _progress = _lastProgess = nbt.GetFloat("progress");
        _extending = nbt.GetBoolean("extending");
    }

    public override void writeNbt(NBTTagCompound nbt)
    {
        base.writeNbt(nbt);
        nbt.SetInteger("blockId", _pushedBlockId);
        nbt.SetInteger("blockData", _pushedBlockData);
        nbt.SetInteger("facing", _facing);
        nbt.SetFloat("progress", _progress);
        nbt.SetBoolean("extending", _extending);
    }
}
