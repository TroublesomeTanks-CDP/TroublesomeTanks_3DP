using Microsoft.Xna.Framework;
using System;
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

        /// <summary> Distance in pixels the tank is moved per check when looking for where its movement is blocked. Must stay below the thickness of the thinnest wall. </summary>
        private const float MARCH_STEP_PIXELS = 1.0f;

        /// <summary> Number of times the gap between the last clear position and the first blocked position is halved. </summary>
        private const int BISECTION_STEPS = 8;

        /// <summary>
        /// Moves a tank out of any walls it overlaps after moving.
        /// <br></br>
        /// If sliding is allowed, the tank is pushed out of each wall in turn and keeps any movement along the wall.
        /// <br></br>
        /// If sliding is prevented, the tank stops where its movement first touches a wall. If the tank was already
        /// overlapping a wall at the start of the frame, or turned into one, it is pushed straight out of the wall
        /// first and then moved as far as it can go.
        /// </summary>
        /// <param name="pTransform"> The transform of the tank, already moved for this frame. It is moved back if it overlaps a wall. </param>
        /// <param name="pTankShape"> The collision shape of the tank. It must be owned by pTransform. </param>
        /// <param name="pStartOfFrame"> Where the tank was before it moved this frame. </param>
        /// <param name="pRestorePlacement"> Where the tank is put back to when a push-out fails. </param>
        /// <param name="pWalls"> The collision shapes of the walls. </param>
        /// <param name="pPreventSliding"> True to stop the tank at the wall, false to let it slide along the wall. </param>
        public static void ResolveFrame(Transform pTransform, CollisionShape pTankShape, TankPlacement pStartOfFrame,
            TankPlacement pRestorePlacement, IReadOnlyList<CollisionShape> pWalls, bool pPreventSliding)
        {
            if (!IsOverlappingAny(pTankShape, pWalls))
            {
                return;
            }

            if (pPreventSliding)
            {
                Vector2 movedPosition = pTransform.Position;
                float movedRotation = pTransform.Rotation;
                Vector2 movement = movedPosition - pStartOfFrame.Position;

                // Put the tank back to its start position, turned the way it is now. If turning swings it into
                // a wall, push it straight out of the wall before it is moved.
                pTransform.Position = pStartOfFrame.Position;
                if (IsOverlappingAny(pTankShape, pWalls))
                {
                    PushOutOfWalls(pTransform, pTankShape, pRestorePlacement, pWalls);
                }

                if (!IsOverlappingAny(pTankShape, pWalls))
                {
                    Vector2 target = pTransform.Position + movement;
                    pTransform.Position = FindFurthestClearPosition(pTransform, pTankShape, pTransform.Position, target, pWalls);
                    if (!IsOverlappingAny(pTankShape, pWalls))
                    {
                        return;
                    }
                }

                pTransform.Position = movedPosition;
                pTransform.Rotation = movedRotation;
            }

            PushOutOfWalls(pTransform, pTankShape, pRestorePlacement, pWalls);
        }

        /// <summary>
        /// Moves a tank out of every wall it overlaps, one wall at a time in list order.
        /// If the tank cannot be moved clear of a wall, it is put back to pRestorePlacement and the next wall is checked.
        /// </summary>
        private static void PushOutOfWalls(Transform pTransform, CollisionShape pTankShape, TankPlacement pRestorePlacement, IReadOnlyList<CollisionShape> pWalls)
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
        /// Finds how far a tank can travel from a clear start position towards a position that overlaps a wall.
        /// Steps along the path to find the first overlap, then narrows down the exact point by halving the gap.
        /// The tank's transform is moved while searching, so the caller must set its position afterwards.
        /// </summary>
        /// <returns> The furthest position along the path where the tank is not overlapping any wall </returns>
        private static Vector2 FindFurthestClearPosition(Transform pTransform, CollisionShape pTankShape, Vector2 pStart, Vector2 pTarget, IReadOnlyList<CollisionShape> pWalls)
        {
            float distance = Vector2.Distance(pStart, pTarget);
            int marchSteps = Math.Max(1, (int)MathF.Ceiling(distance / MARCH_STEP_PIXELS));

            float clearFraction = 0f;
            float blockedFraction = 1f;
            for (int i = 1; i <= marchSteps; i++)
            {
                float fraction = (float)i / marchSteps;
                pTransform.Position = Vector2.Lerp(pStart, pTarget, fraction);

                if (IsOverlappingAny(pTankShape, pWalls))
                {
                    blockedFraction = fraction;
                    break;
                }

                clearFraction = fraction;
            }

            for (int i = 0; i < BISECTION_STEPS; i++)
            {
                float middleFraction = (clearFraction + blockedFraction) / 2f;
                pTransform.Position = Vector2.Lerp(pStart, pTarget, middleFraction);

                if (IsOverlappingAny(pTankShape, pWalls))
                {
                    blockedFraction = middleFraction;
                }
                else
                {
                    clearFraction = middleFraction;
                }
            }

            return Vector2.Lerp(pStart, pTarget, clearFraction);
        }

        /// <returns> True if the tank shape overlaps any of the walls </returns>
        private static bool IsOverlappingAny(CollisionShape pTankShape, IReadOnlyList<CollisionShape> pWalls)
        {
            foreach (CollisionShape wall in pWalls)
            {
                if (pTankShape.Intersects(wall).HasCollided)
                {
                    return true;
                }
            }

            return false;
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
