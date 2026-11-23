using Microsoft.Xna.Framework;
using Tankontroller.World.Shapes;

namespace Tankontroller.Managers
{
    /// <summary>
    /// The result of moving a tank along a path until a wall stops it
    /// </summary>
    internal readonly struct BlockedMove
    {
        /// <summary> The furthest position along the path where the tank is not overlapping any wall </summary>
        public Vector2 Position { get; }

        /// <summary> The wall that stopped the tank, or null if the whole path was clear </summary>
        public CollisionShape BlockingWall { get; }

        /// <summary> Direction pointing away from the blocking wall at the contact, or null if there was no wall or no direction </summary>
        public Vector2? ContactNormal { get; }

        public bool IsBlocked => BlockingWall != null;

        public BlockedMove(Vector2 pPosition, CollisionShape pBlockingWall, Vector2? pContactNormal)
        {
            Position = pPosition;
            BlockingWall = pBlockingWall;
            ContactNormal = pContactNormal;
        }
    }
}
