using BetaSharp.Blocks.Materials;
using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Blocks;

internal class BlockLever : Block
{
    public BlockLever(int id, int level) : base(id, level, Material.PistonBreakable)
    {
    }

    public override Box? getCollisionShape(IBlockReader world, int x, int y, int z) => null;

    public override bool isOpaque() => false;

    public override bool isFullCube() => false;

    public override BlockRendererType getRenderType() => BlockRendererType.Lever;

    // Converted nested ternaries to clean boolean logic
    public bool canPlaceAt(IBlockReader world, int x, int y, int z, int side) => 
        (side == 1 && world.ShouldSuffocate(x, y - 1, z)) ||
        (side == 2 && world.ShouldSuffocate(x, y, z + 1)) ||
        (side == 3 && world.ShouldSuffocate(x, y, z - 1)) ||
        (side == 4 && world.ShouldSuffocate(x + 1, y, z)) || 
        (side == 5 && world.ShouldSuffocate(x - 1, y, z));

    public override bool canPlaceAt(CanPlaceAtCtx ctx) => 
        ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z) ||
        ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z) ||
        ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1) ||
        ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1) || 
        ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z);

    public override void onPlaced(OnPlacedEvt ctx)
    {
        int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        int powered = meta & 8;
        meta &= 7;
        meta = -1;

        if (ctx.Direction == 1 && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z))
        {
            // OnPlacedEvt doesn't have a Random instance, so we instantiate one locally 
            // to handle the randomized floor orientation Lever quirk.
            meta = 5 + new JavaRandom().NextInt(2);
        }

        if (ctx.Direction == 2 && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1)) meta = 4;
        if (ctx.Direction == 3 && ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1)) meta = 3;
        if (ctx.Direction == 4 && ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z)) meta = 2;
        if (ctx.Direction == 5 && ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z)) meta = 1;

        if (meta == -1)
        {
            // TODO: Implement this
            // dropStacks(new OnDropEvt(ctx.WorldRead, default!, ctx.IsRemote, ctx.X, ctx.Y, ctx.Z, ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z)));
            ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
        }
        else
        {
            ctx.WorldWrite.SetBlockMeta(ctx.X, ctx.Y, ctx.Z, meta + powered);
        }
    }

    public override void neighborUpdate(OnTickEvt ctx)
    {
        if (breakIfCannotPlaceAt(ctx.WorldRead, ctx.WorldWrite, ctx))
        {
            int direction = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z) & 7;
            bool shouldDrop = false;

            if (!ctx.WorldRead.ShouldSuffocate(ctx.X - 1, ctx.Y, ctx.Z) && direction == 1) shouldDrop = true;
            if (!ctx.WorldRead.ShouldSuffocate(ctx.X + 1, ctx.Y, ctx.Z) && direction == 2) shouldDrop = true;
            if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z - 1) && direction == 3) shouldDrop = true;
            if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y, ctx.Z + 1) && direction == 4) shouldDrop = true;
            if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) && direction == 5) shouldDrop = true;
            if (!ctx.WorldRead.ShouldSuffocate(ctx.X, ctx.Y - 1, ctx.Z) && direction == 6) shouldDrop = true;

            if (shouldDrop)
            {
                // TODO: Implement this
                // dropStacks(new OnDropEvt(ctx.WorldRead, ctx.Rules, ctx.IsRemote, ctx.X, ctx.Y, ctx.Z, ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z)));
                ctx.WorldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            }
        }
    }

    private bool breakIfCannotPlaceAt(IBlockReader worldRead, IBlockWrite worldWrite, OnTickEvt ctx)
    {
        if (!canPlaceAt(new CanPlaceAtCtx(worldRead, worldWrite, 0, ctx.X, ctx.Y, ctx.Z)))
        {
            // TODO: Implement this
            // dropStacks(new OnDropEvt(ctx.WorldRead, ctx.Rules, ctx.IsRemote, ctx.X, ctx.Y, ctx.Z, worldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z)));
            worldWrite.SetBlock(ctx.X, ctx.Y, ctx.Z, 0);
            return false;
        }

        return true;
    }

    public override void updateBoundingBox(IBlockReader iBlockReader, int x, int y, int z)
    {
        int meta = iBlockReader.GetBlockMeta(x, y, z) & 7;
        float width = 3.0F / 16.0F;
        
        if (meta == 1) setBoundingBox(0.0F, 0.2F, 0.5F - width, width * 2.0F, 0.8F, 0.5F + width);
        else if (meta == 2) setBoundingBox(1.0F - width * 2.0F, 0.2F, 0.5F - width, 1.0F, 0.8F, 0.5F + width);
        else if (meta == 3) setBoundingBox(0.5F - width, 0.2F, 0.0F, 0.5F + width, 0.8F, width * 2.0F);
        else if (meta == 4) setBoundingBox(0.5F - width, 0.2F, 1.0F - width * 2.0F, 0.5F + width, 0.8F, 1.0F);
        else
        {
            width = 0.25F;
            setBoundingBox(0.5F - width, 0.0F, 0.5F - width, 0.5F + width, 0.6F, 0.5F + width);
        }
    }

    // Both break start and use trigger the same logic, so we route them here
    public override void onBlockBreakStart(OnBlockBreakStartEvt ctx)
    {
        toggleLever(ctx.WorldRead, ctx.WorldWrite, ctx.Broadcaster, ctx.X, ctx.Y, ctx.Z);
    }

    public override bool onUse(OnUseEvt ctx)
    {
        if (ctx.IsRemote)
        {
            return true;
        }

        toggleLever(ctx.WorldRead, ctx.WorldWrite, ctx.Broadcaster, ctx.X, ctx.Y, ctx.Z);
        return true;
    }

    // Extracted helper method to handle the shared lever flip logic
    private void toggleLever(IBlockReader worldRead, IBlockWrite worldWrite, WorldEventBroadcaster broadcaster, int x, int y, int z)
    {
        int meta = worldRead.GetBlockMeta(x, y, z);
        int direction = meta & 7;
        int powered = 8 - (meta & 8);
        
        worldWrite.SetBlockMeta(x, y, z, direction + powered);
        worldWrite.SetBlocksDirty(x, y, z);
        broadcaster.PlaySoundAtPos(x + 0.5D, y + 0.5D, z + 0.5D, "random.click", 0.3F, powered > 0 ? 0.6F : 0.5F);
        
        broadcaster.NotifyNeighbors(x, y, z, id);
        
        if (direction == 1) broadcaster.NotifyNeighbors(x - 1, y, z, id);
        else if (direction == 2) broadcaster.NotifyNeighbors(x + 1, y, z, id);
        else if (direction == 3) broadcaster.NotifyNeighbors(x, y, z - 1, id);
        else if (direction == 4) broadcaster.NotifyNeighbors(x, y, z + 1, id);
        else broadcaster.NotifyNeighbors(x, y - 1, z, id);
    }

    public override void onBreak(OnBreakEvt ctx)
    {
        int meta = ctx.WorldRead.GetBlockMeta(ctx.X, ctx.Y, ctx.Z);
        if ((meta & 8) > 0)
        {
            ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z, id);
            int direction = meta & 7;
            
            if (direction == 1) ctx.Broadcaster.NotifyNeighbors(ctx.X - 1, ctx.Y, ctx.Z, id);
            else if (direction == 2) ctx.Broadcaster.NotifyNeighbors(ctx.X + 1, ctx.Y, ctx.Z, id);
            else if (direction == 3) ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z - 1, id);
            else if (direction == 4) ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y, ctx.Z + 1, id);
            else ctx.Broadcaster.NotifyNeighbors(ctx.X, ctx.Y - 1, ctx.Z, id);
        }

        base.onBreak(ctx);
    }

    public override bool isPoweringSide(IBlockReader iBlockReader, int x, int y, int z, int side) => 
        (iBlockReader.GetBlockMeta(x, y, z) & 8) > 0;

    public override bool isStrongPoweringSide(IBlockReader world, int x, int y, int z, int side)
    {
        int meta = world.GetBlockMeta(x, y, z);
        if ((meta & 8) == 0)
        {
            return false;
        }

        int direction = meta & 7;
        return (direction == 6 && side == 1) || 
               (direction == 5 && side == 1) || 
               (direction == 4 && side == 2) || 
               (direction == 3 && side == 3) || 
               (direction == 2 && side == 4) || 
               (direction == 1 && side == 5);
    }

    public override bool canEmitRedstonePower() => true;
}