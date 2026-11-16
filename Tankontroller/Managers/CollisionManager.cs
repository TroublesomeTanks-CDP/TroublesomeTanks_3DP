using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Tankontroller.World;
using Tankontroller.World.Bullets;
using Tankontroller.World.Particles;
using Tankontroller.World.Shapes;

namespace Tankontroller.Managers
{
    /// <summary>
    /// Static class that manages logic for all collision detection and response
    /// </summary>
    internal static class CollisionManager
    {
        public static readonly bool DRAW_COLLISION_SHAPES = DGS.Instance.GetBool("DRAW_COLLISION_SHAPES");

        /// <summary>
        /// This is the collision response for moving a tank away from a wall when a tank collides with a wall
        /// </summary>
        /// <returns> If the function fails return false, otherwise returns true</returns>
        static public bool ResolveTankWallCollision(Tank tank, CollisionShape wall)
        {
            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(tank.Transform, tank.CollisionShape, wall);

            // If resolution failed, revert to previous position (safe fallback)
            if (result == WallPushOutResult.FAILED)
                tank.PutBack();

            return result == WallPushOutResult.RESOLVED;
        }
    }
}
