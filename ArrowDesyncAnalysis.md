Desync Analysis
There are 3 compounding sources of desync for projectile hitboxes:
1. Arrow runs independent physics on both sides with no correction
EntityArrow.cs:93-268 — The tick() method has no world.isRemote guard. Both server and client simulate the arrow independently. After initial spawn (position + velocity), there's no mechanism to correct the client arrow's trajectory if it diverges.
2. Arrow position updates are extremely infrequent
EntityTracker.cs:41 — Arrows are tracked with frequency = 20 (every 20 ticks = 1 second). At arrow speed ~1.5 blocks/tick, the arrow travels ~30 blocks between syncs. Since alwaysUpdateVelocity = false, no velocity corrections are sent either.
3. Entity position interpolation lag
EntityLiving.cs:714-733 — Target entities (players, mobs) are displayed on the client at interpolated positions (3 ticks / 150ms behind the server). The server's arrow raycast at EntityArrow.cs:164-165 checks against true server positions, not the positions the client sees. A fast arrow can travel several blocks in 150ms.
How these compound:
1. Server skeleton shoots arrow → client gets spawn packet with position + velocity
2. Both sides simulate independently → trajectories begin to diverge (float precision, tick timing)
3. Server arrow raycasts against entity at true position → HIT → damage applied
4. Client arrow is 5+ blocks off, entity is rendered 150ms behind → client shows MISS
5. Player sees arrow fly past them but still takes damage