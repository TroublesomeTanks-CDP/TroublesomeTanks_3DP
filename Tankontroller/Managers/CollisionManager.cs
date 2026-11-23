using System.Collections.Generic;
using Tankontroller.World;
using Tankontroller.World.Shapes;

namespace Tankontroller.Managers
{
    /// <summary>
    /// Static class that manages logic for all collision detection and response
    /// </summary>
    internal static class CollisionManager
    {
        /// <summary> Whether collision shapes are drawn on screen. Read from the DGS file. </summary>
        public static readonly bool DRAW_COLLISION_SHAPES = DGS.Instance.GetBool("DRAW_COLLISION_SHAPES");

        /// <summary>
        /// Whether tanks are stopped from sliding along walls. Starts with the value from the DGS file.
        /// </summary>
        public static bool IsWallSlidingPrevented { get; set; } = DGS.Instance.GetBool("PREVENT_TANK_WALL_SLIDING");

        /// <summary>
        /// How much of its movement along a wall a stopped tank keeps, from 0 to 1. Starts with the value from the DGS file.
        /// </summary>
        public static float WallSlideAllowance { get; set; } = DGS.Instance.GetFloat("TANK_WALL_SLIDE_ALLOWANCE");

        /// <summary>
        /// How fast, in radians per second, a tank stopped by a wall is turned away from it. Starts with the value from the DGS file.
        /// </summary>
        public static float WallDeflectionSpeed { get; set; } = DGS.Instance.GetFloat("TANK_WALL_DEFLECTION_SPEED");

        /// <summary>
        /// Moves a tank out of every wall it is overlapping, using IsWallSlidingPrevented to decide whether it may slide along them.
        /// If the tank cannot be moved clear of a wall, it is put back to where it was at the end of the last frame when
        /// sliding is prevented, or to the placement PutBack would use when sliding is allowed.
        /// </summary>
        /// <param name="pSeconds"> The time the frame took, which sets how far a tank may be turned away from a wall. </param>
        public static void ResolveTankWallCollisions(Tank pTank, IReadOnlyList<CollisionShape> pWalls, float pSeconds)
        {
            TankPlacement restorePlacement = IsWallSlidingPrevented ? pTank.CommittedPlacement : pTank.PutBackPlacement;

            WallSlidingSettings settings = new WallSlidingSettings(IsWallSlidingPrevented, WallSlideAllowance, WallDeflectionSpeed * pSeconds);

            WallCollisionResolver.ResolveFrame(pTank.Transform, pTank.CollisionShape, pTank.CommittedPlacement, restorePlacement, pWalls, settings);
        }
    }
}
