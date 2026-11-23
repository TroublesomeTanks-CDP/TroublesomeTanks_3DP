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
        /// Moves a tank out of a wall it is overlapping. If the tank cannot be moved clear, it is put back to its previous position.
        /// </summary>
        /// <returns> True if the tank was overlapping the wall and has been moved clear, otherwise false </returns>
        public static bool ResolveTankWallCollision(Tank pTank, CollisionShape pWall)
        {
            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(pTank.Transform, pTank.CollisionShape, pWall);

            if (result == WallPushOutResult.FAILED)
            {
                pTank.PutBack();
            }

            return result == WallPushOutResult.RESOLVED;
        }
    }
}
