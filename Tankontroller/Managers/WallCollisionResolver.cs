using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Tankontroller.World;
using Tankontroller.World.Shapes;

namespace Tankontroller.Managers
{
    /// <summary>
    /// The outcome of trying to push a tank out of a wall.
    /// </summary>
    internal enum WallPushOutResult
    {
        /// <summary> The tank was not overlapping the wall, so nothing was done. </summary>
        NO_OVERLAP,
        /// <summary> The tank was overlapping the wall and has been moved clear of it. </summary>
        RESOLVED,
        /// <summary> The tank was overlapping the wall and could not be moved clear of it. </summary>
        FAILED
    }

    /// <summary>
    /// Static class responsible for moving a tank out of a wall it overlaps.
    /// Works on a Transform and a collision shape rather than a Tank so that the logic can be unit tested.
    /// </summary>
    internal static class WallCollisionResolver
    {
        /// <summary> Distance in pixels the tank is moved per iteration while being pushed out of a wall. </summary>
        private const float PUSH_OUT_STEP_PIXELS = 1.0f;

        /// <summary> Maximum number of iterations before giving up, to avoid an infinite loop. </summary>
        private const int MAX_PUSH_OUT_STEPS = 200;

        /// <summary>
        /// Moves a tank out of every wall it overlaps, one wall at a time in list order.
        /// If the tank cannot be moved clear of a wall, it is put back to pRestorePlacement and the next wall is checked.
        /// </summary>
        /// <param name="pTransform"> The transform of the tank. This is moved if the tank overlaps a wall. </param>
        /// <param name="pTankShape"> The collision shape of the tank. It must be owned by pTransform. </param>
        /// <param name="pRestorePlacement"> Where the tank is put back to when a push-out fails. </param>
        /// <param name="pWalls"> The collision shapes of the walls. </param>
        public static void ResolveFrame(Transform pTransform, CollisionShape pTankShape, TankPlacement pRestorePlacement, IReadOnlyList<CollisionShape> pWalls)
        {
            foreach (CollisionShape wall in pWalls)
            {
                WallPushOutResult result = PushOutOfWall(pTransform, pTankShape, wall);

                if (result == WallPushOutResult.FAILED)
                {
                    pTransform.Position = pRestorePlacement.Position;
                    pTransform.Rotation = pRestorePlacement.Rotation;
                }
            }
        }

        /// <summary>
        /// Moves a tank away from a wall along the collision normal, in small steps, until it no longer overlaps the wall.
        /// </summary>
        /// <param name="pTransform"> The transform of the tank. This is moved if the tank overlaps the wall. </param>
        /// <param name="pTankShape"> The collision shape of the tank. It must be owned by pTransform. </param>
        /// <param name="pWall"> The collision shape of the wall. </param>
        /// <returns> NO_OVERLAP if there was nothing to do, RESOLVED if the tank was moved clear of the wall,
        /// or FAILED if it was still overlapping after the maximum number of steps. </returns>
        public static WallPushOutResult PushOutOfWall(Transform pTransform, CollisionShape pTankShape, CollisionShape pWall)
        {
            CollisionEvent collisionEvent = pTankShape.Intersects(pWall);

            // Nothing to do if there's no collision
            if (!collisionEvent.HasCollided)
                return WallPushOutResult.NO_OVERLAP;

            // Get the collision normal from the collision event (pointing away from the wall).
            Vector2 normal = collisionEvent.CollisionNormal ?? Vector2.UnitX;

            // Normalize to ensure unit vector (defensive programming, should already be normalized)
            if (normal.LengthSquared() > 0f)
            {
                normal = Vector2.Normalize(normal);
            }

            // Small iterative nudge until the shape no longer collides.
            for (int i = 0; i < MAX_PUSH_OUT_STEPS; ++i)
            {
                pTransform.Position += normal * PUSH_OUT_STEP_PIXELS;

                if (!pTankShape.Intersects(pWall).HasCollided)
                    return WallPushOutResult.RESOLVED;
            }

            return WallPushOutResult.FAILED;
        }
    }
}
