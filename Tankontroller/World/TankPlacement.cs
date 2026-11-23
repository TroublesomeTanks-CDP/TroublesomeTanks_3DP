using Microsoft.Xna.Framework;

namespace Tankontroller.World
{
    /// <summary>
    /// A position and rotation of a tank at one moment in time
    /// </summary>
    public readonly struct TankPlacement
    {
        public Vector2 Position { get; }

        /// <summary> In radians </summary>
        public float Rotation { get; }

        public TankPlacement(Vector2 pPosition, float pRotation)
        {
            Position = pPosition;
            Rotation = pRotation;
        }
    }
}
