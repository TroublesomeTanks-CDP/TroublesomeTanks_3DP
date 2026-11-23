namespace Tankontroller.Managers
{
    /// <summary>
    /// Settings that decide how a tank may move along a wall it touches
    /// </summary>
    internal readonly struct WallSlidingSettings
    {
        /// <summary> True to stop the tank where it touches a wall, false to let it slide along the wall </summary>
        public bool PreventSliding { get; }

        public WallSlidingSettings(bool pPreventSliding)
        {
            PreventSliding = pPreventSliding;
        }

        public static WallSlidingSettings SlidingAllowed => new WallSlidingSettings(false);

        public static WallSlidingSettings SlidingPrevented => new WallSlidingSettings(true);
    }
}
