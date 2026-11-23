using System;

namespace Tankontroller.Managers
{
    /// <summary>
    /// Settings that decide how a tank may move along a wall it touches
    /// </summary>
    internal readonly struct WallSlidingSettings
    {
        /// <summary> True to stop the tank where it touches a wall, false to let it slide along the wall </summary>
        public bool PreventSliding { get; }

        /// <summary> How much of its movement along a wall a stopped tank keeps, from 0 (none) to 1 (all of it). Only used when sliding is prevented. </summary>
        public float SlideAllowance { get; }

        /// <summary> The most, in radians, that a tank stopped by a wall may be turned this frame to swing it away from the wall. Only used when sliding is prevented. </summary>
        public float MaxDeflectionRadians { get; }

        public WallSlidingSettings(bool pPreventSliding, float pSlideAllowance = 0f, float pMaxDeflectionRadians = 0f)
        {
            PreventSliding = pPreventSliding;
            SlideAllowance = Math.Clamp(pSlideAllowance, 0f, 1f);
            MaxDeflectionRadians = Math.Max(pMaxDeflectionRadians, 0f);
        }

        public static WallSlidingSettings SlidingAllowed => new WallSlidingSettings(false);

        public static WallSlidingSettings SlidingPrevented => new WallSlidingSettings(true);
    }
}
