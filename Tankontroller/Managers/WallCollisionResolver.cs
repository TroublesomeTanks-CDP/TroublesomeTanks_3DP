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

        /// <summary> How closely a collision normal must line up with a wall's face direction to count as a face contact (a dot product, 1 is exact). </summary>
        private const float FACE_NORMAL_TOLERANCE = 0.999f;

        /// <summary>
        /// Moves a tank out of any walls it overlaps after moving.
        /// <br></br>
        /// If sliding is allowed, the tank is pushed out of each wall in turn and keeps any movement along the wall.
        /// <br></br>
        /// If sliding is prevented, the tank stops where its movement first touches a wall. If the tank was already
        /// overlapping a wall at the start of the frame, or turned into one, it is pushed straight out of the wall
        /// first and then moved as far as it can go. If the settings give a slide allowance, a stopped tank then
        /// keeps that share of its movement along the face of the wall.
        /// </summary>
        /// <param name="pTransform"> The transform of the tank, already moved for this frame. It is moved back if it overlaps a wall. </param>
        /// <param name="pTankShape"> The collision shape of the tank. It must be owned by pTransform. </param>
        /// <param name="pStartOfFrame"> Where the tank was before it moved this frame. </param>
        /// <param name="pRestorePlacement"> Where the tank is put back to when a push-out fails. </param>
        /// <param name="pWalls"> The collision shapes of the walls. </param>
        /// <param name="pSettings"> Whether the tank may slide along the walls it touches. </param>
        public static void ResolveFrame(Transform pTransform, CollisionShape pTankShape, TankPlacement pStartOfFrame,
            TankPlacement pRestorePlacement, IReadOnlyList<CollisionShape> pWalls, WallSlidingSettings pSettings)
        {
            if (!IsOverlappingAny(pTankShape, pWalls))
            {
                return;
            }

            if (pSettings.PreventSliding)
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
                    BlockedMove blockedMove = FindFurthestClearPosition(pTransform, pTankShape, pTransform.Position, target, pWalls);
                    pTransform.Position = blockedMove.Position;
                    if (blockedMove.IsBlocked && pSettings.SlideAllowance > 0f)
                    {
                        SlideAlongWall(pTransform, pTankShape, blockedMove, target, pSettings.SlideAllowance, pWalls);
                    }

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
        /// <returns> The furthest clear position along the path, and the wall that stopped the tank at that point if there was one </returns>
        internal static BlockedMove FindFurthestClearPosition(Transform pTransform, CollisionShape pTankShape, Vector2 pStart, Vector2 pTarget, IReadOnlyList<CollisionShape> pWalls)
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

            Vector2 clearPosition = Vector2.Lerp(pStart, pTarget, clearFraction);

            // Look at the first position past the furthest clear one to see which wall the tank ran into.
            pTransform.Position = Vector2.Lerp(pStart, pTarget, blockedFraction);
            CollisionShape blockingWall = FindFirstOverlappingWall(pTankShape, pWalls);
            if (blockingWall == null)
            {
                return new BlockedMove(clearPosition, null, null);
            }

            return new BlockedMove(clearPosition, blockingWall, pTankShape.Intersects(blockingWall).CollisionNormal);
        }

        /// <summary>
        /// Moves a tank that a wall has stopped along the face of that wall, by a share of the movement it had left.
        /// Does nothing if the contact is not along a face, such as at a corner or the end of a wall.
        /// </summary>
        /// <param name="pTransform"> The transform of the tank, at the position where it was stopped. </param>
        /// <param name="pBlockedMove"> Where the tank was stopped and by which wall. </param>
        /// <param name="pTarget"> Where the tank would have ended up if nothing had stopped it. </param>
        /// <param name="pAllowance"> The share of the movement along the wall to keep, from 0 to 1. </param>
        private static void SlideAlongWall(Transform pTransform, CollisionShape pTankShape, BlockedMove pBlockedMove, Vector2 pTarget, float pAllowance, IReadOnlyList<CollisionShape> pWalls)
        {
            if (pBlockedMove.ContactNormal == null || !IsWallFaceNormal(pBlockedMove.BlockingWall, pBlockedMove.ContactNormal.Value))
            {
                return;
            }

            Vector2 normal = pBlockedMove.ContactNormal.Value;
            Vector2 remaining = pTarget - pBlockedMove.Position;
            Vector2 alongWall = remaining - Vector2.Dot(remaining, normal) * normal;
            Vector2 slideTarget = pBlockedMove.Position + alongWall * pAllowance;

            pTransform.Position = FindFurthestClearPosition(pTransform, pTankShape, pBlockedMove.Position, slideTarget, pWalls).Position;
        }

        /// <summary>
        /// Checks whether a collision normal points along one of the wall's own faces. At a corner or the end of a wall,
        /// the normal can be one of the tank's own axes instead, which does not describe the wall's surface.
        /// </summary>
        /// <param name="pNormal"> A unit vector pointing towards or away from the wall. </param>
        /// <returns> True if the contact is along a face of the wall, false if it is not or the wall's shape is not supported </returns>
        internal static bool IsWallFaceNormal(CollisionShape pWall, Vector2 pNormal)
        {
            float wallRotation;
            if (pWall is RectangleAxisAlignedShape)
            {
                wallRotation = 0f;
            }
            else if (pWall is RectangleOrientedShape orientedWall)
            {
                wallRotation = orientedWall.WorldRotation;
            }
            else
            {
                return false;
            }

            Vector2 faceAxisX = new Vector2(MathF.Cos(wallRotation), MathF.Sin(wallRotation));
            Vector2 faceAxisY = new Vector2(-MathF.Sin(wallRotation), MathF.Cos(wallRotation));

            return MathF.Abs(Vector2.Dot(pNormal, faceAxisX)) >= FACE_NORMAL_TOLERANCE
                || MathF.Abs(Vector2.Dot(pNormal, faceAxisY)) >= FACE_NORMAL_TOLERANCE;
        }

        /// <returns> True if the tank shape overlaps any of the walls </returns>
        private static bool IsOverlappingAny(CollisionShape pTankShape, IReadOnlyList<CollisionShape> pWalls)
        {
            return FindFirstOverlappingWall(pTankShape, pWalls) != null;
        }

        /// <returns> The first wall in the list that the tank shape overlaps, or null if there is none </returns>
        private static CollisionShape FindFirstOverlappingWall(CollisionShape pTankShape, IReadOnlyList<CollisionShape> pWalls)
        {
            foreach (CollisionShape wall in pWalls)
            {
                if (pTankShape.Intersects(wall).HasCollided)
                {
                    return wall;
                }
            }

            return null;
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
