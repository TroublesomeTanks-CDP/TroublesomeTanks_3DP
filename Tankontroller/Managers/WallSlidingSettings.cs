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

        public WallSlidingSettings(bool pPreventSliding, float pSlideAllowance = 0f)
        {
            PreventSliding = pPreventSliding;
            SlideAllowance = Math.Clamp(pSlideAllowance, 0f, 1f);
        }

        public static WallSlidingSettings SlidingAllowed => new WallSlidingSettings(false);

        public static WallSlidingSettings SlidingPrevented => new WallSlidingSettings(true);
    }
}
