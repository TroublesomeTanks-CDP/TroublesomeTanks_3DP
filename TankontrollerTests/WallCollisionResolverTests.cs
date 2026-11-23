using Microsoft.Xna.Framework;
using Tankontroller.Managers;
using Tankontroller.World;
using Tankontroller.World.Shapes;

namespace TankontrollerTests
{
    /// <summary>
    /// Tests for the push-out that moves a tank clear of a wall, as it currently behaves (including sliding).
    /// </summary>
    public class WallCollisionResolverTests
    {
        private const float TANK_LENGTH = 60f;
        private const float TANK_WIDTH = 40f;
        private const float DRIVE_SPEED_PER_FRAME = 2f;

        // Vertical wall with faces at x = 90 and x = 110. An unrotated tank touches it when its centre is at x = 60.
        private static readonly Vector2 WALL_CENTRE = new(100f, 100f);
        private static readonly Vector2 VERTICAL_WALL_SIZE = new(20f, 400f);
        private static readonly Vector2 HORIZONTAL_WALL_SIZE = new(400f, 20f);

        #region Helpers

        private static (Transform, RectangleOrientedShape) CreateTank(Vector2 pPosition, float pRotation = 0f)
        {
            Transform transform = new(pPosition, pRotation);
            RectangleOrientedShape shape = new(transform, new Vector2(TANK_LENGTH, TANK_WIDTH));
            return (transform, shape);
        }

        private static RectangleAxisAlignedShape CreateAlignedWall(Vector2 pCentre, Vector2 pSize)
        {
            return new RectangleAxisAlignedShape(new Transform(pCentre), pSize);
        }

        private static RectangleOrientedShape CreateOrientedWall(Vector2 pCentre, Vector2 pSize, float pRotation = 0f)
        {
            return new RectangleOrientedShape(new Transform(pCentre, pRotation), pSize);
        }

        private static float TankHalfWidthAlongX(float pRotation)
        {
            float halfLength = (TANK_LENGTH / 2f) * MathF.Abs(MathF.Cos(pRotation));
            float halfWidth = (TANK_WIDTH / 2f) * MathF.Abs(MathF.Sin(pRotation));
            return halfLength + halfWidth;
        }

        /// <summary>
        /// Drives a tank in a straight line for a number of frames with sliding prevented, resolving walls after every frame.
        /// </summary>
        /// <returns> The number of frames that ended with the tank overlapping a wall </returns>
        private static int DriveFrames(Transform pTransform, CollisionShape pShape, List<CollisionShape> pWalls, Vector2 pDirection, float pRotationPerFrame, int pFrames)
        {
            int overlappingFrames = 0;

            for (int frame = 0; frame < pFrames; frame++)
            {
                TankPlacement startOfFrame = new(pTransform.Position, pTransform.Rotation);
                pTransform.Position += pDirection * DRIVE_SPEED_PER_FRAME;
                pTransform.Rotation += pRotationPerFrame;

                WallCollisionResolver.ResolveFrame(pTransform, pShape, startOfFrame, startOfFrame, pWalls, WallSlidingSettings.SlidingPrevented);

                foreach (CollisionShape wall in pWalls)
                {
                    if (pShape.Intersects(wall).HasCollided)
                    {
                        overlappingFrames++;
                        break;
                    }
                }
            }

            return overlappingFrames;
        }

        /// <summary> Resolves a tank that has moved to its current position from pStart, with sliding prevented. </summary>
        private static void ResolveWithSlidingPrevented(Transform pTransform, CollisionShape pShape, Vector2 pStart, List<CollisionShape> pWalls)
        {
            TankPlacement startOfFrame = new(pStart, pTransform.Rotation);

            WallCollisionResolver.ResolveFrame(pTransform, pShape, startOfFrame, startOfFrame, pWalls, WallSlidingSettings.SlidingPrevented);
        }

        #endregion

        #region No overlap

        [Fact]
        public void PushOutOfWall_TankClearOfWall_ReportsNoOverlapAndDoesNotMove()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(50f, 100f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(transform, shape, wall);

            Assert.Equal(WallPushOutResult.NO_OVERLAP, result);
            Assert.Equal(new Vector2(50f, 100f), transform.Position);
        }

        #endregion

        #region Head-on

        [Fact]
        public void PushOutOfWall_HeadOnIntoAlignedWall_StopsFlushWithWall()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(70f, 100f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(transform, shape, wall);

            Assert.Equal(WallPushOutResult.RESOLVED, result);
            Assert.Equal(new Vector2(60f, 100f), transform.Position);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        [Fact]
        public void PushOutOfWall_HeadOnFromFarSideOfWall_PushesInOppositeDirection()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(130f, 100f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(transform, shape, wall);

            Assert.Equal(WallPushOutResult.RESOLVED, result);
            Assert.Equal(new Vector2(140f, 100f), transform.Position);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        [Fact]
        public void PushOutOfWall_HeadOnIntoOrientedWall_StopsFlushWithWall()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(70f, 100f));
            RectangleOrientedShape wall = CreateOrientedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(transform, shape, wall);

            Assert.Equal(WallPushOutResult.RESOLVED, result);
            Assert.Equal(new Vector2(60f, 100f), transform.Position);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        [Fact]
        public void PushOutOfWall_HeadOnIntoWallRotatedQuarterTurn_StopsAtWallWithinTolerance()
        {
            // Same space as the vertical wall, but built as a horizontal wall turned 90 degrees.
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(70f, 100f));
            RectangleOrientedShape wall = CreateOrientedWall(WALL_CENTRE, HORIZONTAL_WALL_SIZE, MathF.PI / 2f);

            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(transform, shape, wall);

            Assert.Equal(WallPushOutResult.RESOLVED, result);
            Assert.InRange(transform.Position.X, 59f, 60.01f);
            Assert.InRange(transform.Position.Y, 99.99f, 100.01f);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        #endregion

        #region Sliding

        [Fact]
        public void PushOutOfWall_AngledMoveIntoVerticalWall_KeepsMovementAlongWall()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(50f, 100f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            transform.Position += new Vector2(20f, 8f);

            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(transform, shape, wall);

            Assert.Equal(WallPushOutResult.RESOLVED, result);
            Assert.Equal(60f, transform.Position.X);
            Assert.Equal(108f, transform.Position.Y);
        }

        [Fact]
        public void PushOutOfWall_AngledMoveIntoHorizontalWall_KeepsMovementAlongWall()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(100f, 50f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, HORIZONTAL_WALL_SIZE);
            transform.Position += new Vector2(8f, 25f);

            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(transform, shape, wall);

            Assert.Equal(WallPushOutResult.RESOLVED, result);
            Assert.Equal(108f, transform.Position.X);
            Assert.Equal(70f, transform.Position.Y);
        }

        [Fact]
        public void PushOutOfWall_ReversingAtAngleIntoWall_KeepsMovementAlongWall()
        {
            float rotation = MathF.PI + 0.4f;
            Vector2 forward = new(MathF.Cos(rotation), MathF.Sin(rotation));
            Vector2 start = new(45f, 100f);
            (Transform transform, RectangleOrientedShape shape) = CreateTank(start, rotation);
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            Vector2 reversedPosition = start - forward * 12f;
            transform.Position = reversedPosition;

            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(transform, shape, wall);

            // The push-out works in whole pixels, so the tank can end up to a pixel short of the wall.
            float touchingX = 90f - TankHalfWidthAlongX(rotation);
            Assert.Equal(WallPushOutResult.RESOLVED, result);
            Assert.InRange(transform.Position.X, touchingX - 1.01f, touchingX + 0.01f);
            Assert.Equal(reversedPosition.Y, transform.Position.Y);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        #endregion

        #region Fallbacks and failure

        [Fact]
        public void PushOutOfWall_NoCollisionNormal_PushesAlongPositiveX()
        {
            // Shapes with the same centre collide without a normal.
            (Transform transform, RectangleOrientedShape shape) = CreateTank(WALL_CENTRE);
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, new Vector2(20f, 20f));

            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(transform, shape, wall);

            Assert.Equal(WallPushOutResult.RESOLVED, result);
            Assert.Equal(new Vector2(140f, 100f), transform.Position);
        }

        [Fact]
        public void PushOutOfWall_OverlapTooDeepToClear_ReportsFailedAndLeavesTankMoved()
        {
            // The resolver does not undo its moves on failure, the caller does that with PutBack.
            (Transform transform, RectangleOrientedShape shape) = CreateTank(WALL_CENTRE);
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, new Vector2(2000f, 2000f));

            WallPushOutResult result = WallCollisionResolver.PushOutOfWall(transform, shape, wall);

            Assert.Equal(WallPushOutResult.FAILED, result);
            Assert.Equal(new Vector2(300f, 100f), transform.Position);
            Assert.True(shape.Intersects(wall).HasCollided);
        }

        #endregion

        #region ResolveFrame

        [Fact]
        public void ResolveFrame_NoWalls_DoesNotMoveTank()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(70f, 100f));
            TankPlacement restorePlacement = new(Vector2.Zero, 0f);

            WallCollisionResolver.ResolveFrame(transform, shape, restorePlacement, restorePlacement, new List<CollisionShape>(), WallSlidingSettings.SlidingAllowed);

            Assert.Equal(new Vector2(70f, 100f), transform.Position);
        }

        [Fact]
        public void ResolveFrame_TankClearOfAllWalls_DoesNotMoveTank()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(50f, 100f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            TankPlacement restorePlacement = new(Vector2.Zero, 0f);

            WallCollisionResolver.ResolveFrame(transform, shape, restorePlacement, restorePlacement, new List<CollisionShape> { wall }, WallSlidingSettings.SlidingAllowed);

            Assert.Equal(new Vector2(50f, 100f), transform.Position);
        }

        [Fact]
        public void ResolveFrame_TankOverlappingWall_PushesTankOut()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(70f, 100f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            TankPlacement restorePlacement = new(Vector2.Zero, 0f);

            WallCollisionResolver.ResolveFrame(transform, shape, restorePlacement, restorePlacement, new List<CollisionShape> { wall }, WallSlidingSettings.SlidingAllowed);

            Assert.Equal(new Vector2(60f, 100f), transform.Position);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ResolveFrame_TankInCorner_EndsInSamePlaceWhateverTheWallOrder(bool pVerticalWallFirst)
        {
            // The vertical wall is on the tank's right and the horizontal wall is below it.
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(70f, 125f));
            RectangleAxisAlignedShape verticalWall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            RectangleAxisAlignedShape horizontalWall = CreateAlignedWall(new Vector2(0f, 150f), HORIZONTAL_WALL_SIZE);
            List<CollisionShape> walls = pVerticalWallFirst
                ? new List<CollisionShape> { verticalWall, horizontalWall }
                : new List<CollisionShape> { horizontalWall, verticalWall };
            TankPlacement restorePlacement = new(Vector2.Zero, 0f);

            WallCollisionResolver.ResolveFrame(transform, shape, restorePlacement, restorePlacement, walls, WallSlidingSettings.SlidingAllowed);

            Assert.Equal(new Vector2(60f, 120f), transform.Position);
            Assert.False(shape.Intersects(verticalWall).HasCollided);
            Assert.False(shape.Intersects(horizontalWall).HasCollided);
        }

        [Fact]
        public void ResolveFrame_PushOutFails_PutsTankBackToRestorePlacement()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(WALL_CENTRE);
            RectangleAxisAlignedShape hugeWall = CreateAlignedWall(WALL_CENTRE, new Vector2(2000f, 2000f));
            TankPlacement restorePlacement = new(new Vector2(10f, 20f), 0.5f);

            WallCollisionResolver.ResolveFrame(transform, shape, restorePlacement, restorePlacement, new List<CollisionShape> { hugeWall }, WallSlidingSettings.SlidingAllowed);

            Assert.Equal(restorePlacement.Position, transform.Position);
            Assert.Equal(restorePlacement.Rotation, transform.Rotation);
        }

        [Fact]
        public void ResolveFrame_PushOutFails_StillChecksTheNextWall()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(WALL_CENTRE);
            RectangleAxisAlignedShape hugeWall = CreateAlignedWall(WALL_CENTRE, new Vector2(2000f, 2000f));
            RectangleAxisAlignedShape smallWall = CreateAlignedWall(new Vector2(10f, 20f), new Vector2(20f, 20f));
            TankPlacement restorePlacement = new(new Vector2(10f, 20f), 0f);

            WallCollisionResolver.ResolveFrame(transform, shape, restorePlacement, restorePlacement, new List<CollisionShape> { hugeWall, smallWall }, WallSlidingSettings.SlidingAllowed);

            // The tank is put back on top of the small wall, then pushed along the x axis until it is clear.
            Assert.Equal(new Vector2(50f, 20f), transform.Position);
        }

        #endregion

        #region ResolveFrame with sliding prevented

        [Fact]
        public void ResolveFrame_SlidingAllowed_AngledMove_StillSlides()
        {
            Vector2 start = new(50f, 100f);
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(70f, 108f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            TankPlacement startOfFrame = new(start, 0f);

            WallCollisionResolver.ResolveFrame(transform, shape, startOfFrame, startOfFrame, new List<CollisionShape> { wall }, WallSlidingSettings.SlidingAllowed);

            Assert.Equal(new Vector2(60f, 108f), transform.Position);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_MoveClearOfWalls_MovesFully()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(40f, 100f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            ResolveWithSlidingPrevented(transform, shape, new Vector2(30f, 100f), new List<CollisionShape> { wall });

            Assert.Equal(new Vector2(40f, 100f), transform.Position);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_HeadOn_StopsAtWall()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(70f, 100f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            ResolveWithSlidingPrevented(transform, shape, new Vector2(50f, 100f), new List<CollisionShape> { wall });

            Assert.InRange(transform.Position.X, 59.98f, 60.01f);
            Assert.Equal(100f, transform.Position.Y);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_AngledIntoVerticalWall_StopsWhereItFirstTouches()
        {
            // Sliding would leave the tank at (60, 108). Moving along the path it takes, it first touches the wall at (60, 104).
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(70f, 108f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            ResolveWithSlidingPrevented(transform, shape, new Vector2(50f, 100f), new List<CollisionShape> { wall });

            Assert.InRange(transform.Position.X, 59.98f, 60.01f);
            Assert.InRange(transform.Position.Y, 103.98f, 104.02f);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_AngledIntoHorizontalWall_StopsWhereItFirstTouches()
        {
            // Sliding would leave the tank at (108, 70). It first touches the wall at (106.4, 70).
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(108f, 75f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, HORIZONTAL_WALL_SIZE);

            ResolveWithSlidingPrevented(transform, shape, new Vector2(100f, 50f), new List<CollisionShape> { wall });

            Assert.InRange(transform.Position.X, 106.38f, 106.42f);
            Assert.InRange(transform.Position.Y, 69.98f, 70.01f);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_ReversingAtAngle_StopsWhereItFirstTouches()
        {
            float rotation = MathF.PI + 0.4f;
            Vector2 forward = new(MathF.Cos(rotation), MathF.Sin(rotation));
            Vector2 start = new(45f, 100f);
            Vector2 reversedPosition = start - forward * 12f;
            (Transform transform, RectangleOrientedShape shape) = CreateTank(reversedPosition, rotation);
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            ResolveWithSlidingPrevented(transform, shape, start, new List<CollisionShape> { wall });

            float touchingX = 90f - TankHalfWidthAlongX(rotation);
            float fractionTravelled = (touchingX - start.X) / (reversedPosition.X - start.X);
            float expectedY = start.Y + fractionTravelled * (reversedPosition.Y - start.Y);
            Assert.InRange(transform.Position.X, touchingX - 0.03f, touchingX + 0.01f);
            Assert.InRange(transform.Position.Y, expectedY - 0.03f, expectedY + 0.03f);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_AlreadyTouchingWall_DoesNotMoveAlongWall()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(65f, 108f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            ResolveWithSlidingPrevented(transform, shape, new Vector2(60f, 100f), new List<CollisionShape> { wall });

            Assert.InRange(transform.Position.X, 59.98f, 60.01f);
            Assert.InRange(transform.Position.Y, 99.99f, 100.02f);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_StartedInsideWall_PushesTankOut()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(72f, 100f));
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            ResolveWithSlidingPrevented(transform, shape, new Vector2(70f, 100f), new List<CollisionShape> { wall });

            Assert.Equal(new Vector2(60f, 100f), transform.Position);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ResolveFrame_SlidingPrevented_IntoCorner_StopsAtFirstWallWhateverTheWallOrder(bool pVerticalWallFirst)
        {
            // The tank reaches the vertical wall at (60, 112), before it would reach the horizontal wall.
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(75f, 130f));
            RectangleAxisAlignedShape verticalWall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            RectangleAxisAlignedShape horizontalWall = CreateAlignedWall(new Vector2(0f, 150f), HORIZONTAL_WALL_SIZE);
            List<CollisionShape> walls = pVerticalWallFirst
                ? new List<CollisionShape> { verticalWall, horizontalWall }
                : new List<CollisionShape> { horizontalWall, verticalWall };

            ResolveWithSlidingPrevented(transform, shape, new Vector2(50f, 100f), walls);

            Assert.InRange(transform.Position.X, 59.98f, 60.01f);
            Assert.InRange(transform.Position.Y, 111.98f, 112.02f);
            Assert.False(shape.Intersects(verticalWall).HasCollided);
            Assert.False(shape.Intersects(horizontalWall).HasCollided);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_TurningOnSpotAgainstWall_PushesAwayWithoutMovingAlongWall()
        {
            // The tank touches the wall at (60, 100), then turns without moving, so its corners swing into the wall.
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(60f, 100f), 0.4f);
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            TankPlacement startOfFrame = new(new Vector2(60f, 100f), 0f);

            WallCollisionResolver.ResolveFrame(transform, shape, startOfFrame, startOfFrame, new List<CollisionShape> { wall }, WallSlidingSettings.SlidingPrevented);

            Assert.InRange(transform.Position.X, 53f, 54.7f);
            Assert.Equal(100f, transform.Position.Y);
            Assert.Equal(0.4f, transform.Rotation);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_TurningAndMovingAlongWall_DoesNotSlide()
        {
            // One frame of a single track drive: the tank turns and moves 8 along the wall.
            // Sliding would leave it at y = 108. It should be turned away from the wall first and then stopped by it.
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(61f, 108f), 0.3f);
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            TankPlacement startOfFrame = new(new Vector2(60f, 100f), 0f);

            WallCollisionResolver.ResolveFrame(transform, shape, startOfFrame, startOfFrame, new List<CollisionShape> { wall }, WallSlidingSettings.SlidingPrevented);

            Assert.InRange(transform.Position.Y, 99.99f, 104f);
            Assert.Equal(0.3f, transform.Rotation);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_TurningAndMovingAwayFromWall_MovesAwayFully()
        {
            // The turn is pushed out of the wall first, then the whole 2 unit move away from the wall is kept.
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(58f, 100f), 0.3f);
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            TankPlacement startOfFrame = new(new Vector2(60f, 100f), 0f);

            WallCollisionResolver.ResolveFrame(transform, shape, startOfFrame, startOfFrame, new List<CollisionShape> { wall }, WallSlidingSettings.SlidingPrevented);

            Assert.InRange(transform.Position.X, 52.99f, 53.01f);
            Assert.Equal(100f, transform.Position.Y);
            Assert.False(shape.Intersects(wall).HasCollided);
        }

        #endregion

        #region Driving into corners, seams and gaps

        // Inner corner: the vertical wall's face is at x = 90 and the horizontal wall's top is at y = 140,
        // so the tank is stopped when its centre reaches x = 60 or y = 120.

        [Theory]
        [InlineData(0f)]
        [InlineData(30f)]
        [InlineData(45f)]
        [InlineData(60f)]
        [InlineData(90f)]
        public void ResolveFrame_SlidingPrevented_DrivingIntoInnerCorner_StopsAtTheWalls(float pAngleDegrees)
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(40f, 100f));
            RectangleAxisAlignedShape verticalWall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            RectangleAxisAlignedShape horizontalWall = CreateAlignedWall(new Vector2(0f, 150f), HORIZONTAL_WALL_SIZE);
            float angle = pAngleDegrees * MathF.PI / 180f;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));

            int overlappingFrames = DriveFrames(transform, shape, new List<CollisionShape> { verticalWall, horizontalWall }, direction, 0f, 100);

            Assert.Equal(0, overlappingFrames);
            Assert.InRange(transform.Position.X, 39f, 60.01f);
            Assert.InRange(transform.Position.Y, 99f, 120.01f);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_TurningInInnerCorner_NeverEndsAFrameInsideAWall()
        {
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(60f, 120f));
            RectangleAxisAlignedShape verticalWall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            RectangleAxisAlignedShape horizontalWall = CreateAlignedWall(new Vector2(0f, 150f), HORIZONTAL_WALL_SIZE);
            Vector2 intoCorner = Vector2.Normalize(new Vector2(1f, 1f));

            int overlappingFrames = DriveFrames(transform, shape, new List<CollisionShape> { verticalWall, horizontalWall }, intoCorner, 0.05f, 200);

            Assert.Equal(0, overlappingFrames);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_DrivingAtAngleIntoSeamBetweenTwoWalls_StopsWithoutCrossingTheSeam()
        {
            // The two walls meet at y = 100. The tank first touches at (60, 94.6), well short of the seam.
            // Sliding along the wall would carry it across the seam.
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(40f, 60f));
            RectangleAxisAlignedShape upperWall = CreateAlignedWall(new Vector2(100f, 0f), new Vector2(20f, 200f));
            RectangleAxisAlignedShape lowerWall = CreateAlignedWall(new Vector2(100f, 200f), new Vector2(20f, 200f));
            float angle = 60f * MathF.PI / 180f;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));

            int overlappingFrames = DriveFrames(transform, shape, new List<CollisionShape> { upperWall, lowerWall }, direction, 0f, 100);

            Assert.Equal(0, overlappingFrames);
            Assert.InRange(transform.Position.X, 59.98f, 60.01f);
            Assert.InRange(transform.Position.Y, 94.5f, 94.8f);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_GapNarrowerThanTank_TankDoesNotGetThrough()
        {
            // The gap between the walls is 30 tall and the tank is 40 wide.
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(40f, 100f));
            RectangleAxisAlignedShape upperWall = CreateAlignedWall(new Vector2(100f, -230f), new Vector2(20f, 630f));
            RectangleAxisAlignedShape lowerWall = CreateAlignedWall(new Vector2(100f, 330f), new Vector2(20f, 430f));

            int overlappingFrames = DriveFrames(transform, shape, new List<CollisionShape> { upperWall, lowerWall }, Vector2.UnitX, 0f, 100);

            Assert.Equal(0, overlappingFrames);
            Assert.InRange(transform.Position.X, 39f, 60.01f);
        }

        [Fact]
        public void ResolveFrame_SlidingPrevented_GapWiderThanTank_TankDrivesThrough()
        {
            // The gap between the walls is 60 tall and the tank is 40 wide.
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(40f, 100f));
            RectangleAxisAlignedShape upperWall = CreateAlignedWall(new Vector2(100f, -130f), new Vector2(20f, 400f));
            RectangleAxisAlignedShape lowerWall = CreateAlignedWall(new Vector2(100f, 330f), new Vector2(20f, 400f));

            int overlappingFrames = DriveFrames(transform, shape, new List<CollisionShape> { upperWall, lowerWall }, Vector2.UnitX, 0f, 100);

            Assert.Equal(0, overlappingFrames);
            Assert.True(transform.Position.X > 200f);
            Assert.Equal(100f, transform.Position.Y);
        }

        #endregion

        #region Blocked move and wall faces

        [Fact]
        public void FindFurthestClearPosition_MoveStoppedByVerticalWall_ReportsTheWallAndItsFaceNormal()
        {
            Vector2 start = new(50f, 100f);
            (Transform transform, RectangleOrientedShape shape) = CreateTank(start);
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            BlockedMove move = WallCollisionResolver.FindFurthestClearPosition(transform, shape, start, new Vector2(70f, 108f), new List<CollisionShape> { wall });

            Vector2 normal = move.ContactNormal ?? Vector2.Zero;
            Assert.True(move.IsBlocked);
            Assert.Same(wall, move.BlockingWall);
            Assert.InRange(move.Position.X, 59.98f, 60.01f);
            Assert.InRange(move.Position.Y, 103.98f, 104.02f);
            Assert.InRange(normal.X, -1.001f, -0.999f);
            Assert.InRange(normal.Y, -0.001f, 0.001f);
        }

        [Fact]
        public void FindFurthestClearPosition_ClearPath_ReportsNoWallAndReachesTheTarget()
        {
            Vector2 start = new(30f, 100f);
            Vector2 target = new(40f, 100f);
            (Transform transform, RectangleOrientedShape shape) = CreateTank(start);
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            BlockedMove move = WallCollisionResolver.FindFurthestClearPosition(transform, shape, start, target, new List<CollisionShape> { wall });

            Assert.False(move.IsBlocked);
            Assert.Null(move.BlockingWall);
            Assert.Null(move.ContactNormal);
            Assert.Equal(target, move.Position);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FindFurthestClearPosition_IntoCorner_ReportsTheWallTouchedFirst(bool pVerticalWallFirst)
        {
            Vector2 start = new(50f, 100f);
            (Transform transform, RectangleOrientedShape shape) = CreateTank(start);
            RectangleAxisAlignedShape verticalWall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            RectangleAxisAlignedShape horizontalWall = CreateAlignedWall(new Vector2(0f, 150f), HORIZONTAL_WALL_SIZE);
            List<CollisionShape> walls = pVerticalWallFirst
                ? new List<CollisionShape> { verticalWall, horizontalWall }
                : new List<CollisionShape> { horizontalWall, verticalWall };

            BlockedMove move = WallCollisionResolver.FindFurthestClearPosition(transform, shape, start, new Vector2(75f, 130f), walls);

            Assert.Same(verticalWall, move.BlockingWall);
        }

        [Theory]
        [InlineData(-1f, 0f, true)]
        [InlineData(1f, 0f, true)]
        [InlineData(0f, 1f, true)]
        [InlineData(-0.882f, 0.471f, false)]
        [InlineData(-0.707f, 0.707f, false)]
        public void IsWallFaceNormal_AlignedWall_OnlyAcceptsDirectionsAlongItsFaces(float pNormalX, float pNormalY, bool pExpected)
        {
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);

            bool isFaceNormal = WallCollisionResolver.IsWallFaceNormal(wall, new Vector2(pNormalX, pNormalY));

            Assert.Equal(pExpected, isFaceNormal);
        }

        [Fact]
        public void IsWallFaceNormal_RotatedWall_FollowsTheWallsOwnAxes()
        {
            float rotation = 0.5f;
            RectangleOrientedShape wall = CreateOrientedWall(WALL_CENTRE, VERTICAL_WALL_SIZE, rotation);
            Vector2 alongWallLength = new(MathF.Cos(rotation), MathF.Sin(rotation));
            Vector2 alongWallWidth = new(-MathF.Sin(rotation), MathF.Cos(rotation));

            Assert.True(WallCollisionResolver.IsWallFaceNormal(wall, alongWallLength));
            Assert.True(WallCollisionResolver.IsWallFaceNormal(wall, alongWallWidth));
            Assert.False(WallCollisionResolver.IsWallFaceNormal(wall, Vector2.UnitX));
        }

        [Fact]
        public void IsWallFaceNormal_ShapeWithoutFaces_ReturnsFalse()
        {
            CircleShape circle = new(new Transform(WALL_CENTRE), 20f);

            Assert.False(WallCollisionResolver.IsWallFaceNormal(circle, Vector2.UnitX));
        }

        #endregion

        #region Collision normals for a rotated tank

        [Theory]
        [InlineData(0f)]
        [InlineData(0.3f)]
        [InlineData(0.7f)]
        [InlineData(1.0f)]
        [InlineData(1.4f)]
        [InlineData(1.5708f)]
        public void Intersects_RotatedTankOverlappingWallFace_NormalPointsAwayFromTheFace(float pRotation)
        {
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(90f - TankHalfWidthAlongX(pRotation) + 3f, 100f), pRotation);

            CollisionEvent collision = shape.Intersects(wall);

            Vector2 normal = collision.CollisionNormal ?? Vector2.Zero;
            Assert.True(collision.HasCollided);
            Assert.InRange(normal.X, -1.001f, -0.999f);
            Assert.InRange(normal.Y, -0.001f, 0.001f);
        }

        [Fact]
        public void Intersects_RotatedTankCornerOverlappingWallEnd_NormalIsATankAxis()
        {
            // Near the end of the wall the shortest way out is along the tank's own side axis, which is angled away from the wall's face.
            float rotation = 1.08f;
            RectangleAxisAlignedShape wall = CreateAlignedWall(WALL_CENTRE, VERTICAL_WALL_SIZE);
            (Transform transform, RectangleOrientedShape shape) = CreateTank(new Vector2(90f - TankHalfWidthAlongX(rotation) + 3f, 285f), rotation);

            CollisionEvent collision = shape.Intersects(wall);

            Vector2 normal = collision.CollisionNormal ?? Vector2.Zero;
            Vector2 tankSideAxis = new(-MathF.Sin(rotation), MathF.Cos(rotation));
            Assert.True(collision.HasCollided);
            Assert.InRange(Vector2.Dot(normal, tankSideAxis), 0.999f, 1.001f);
            Assert.True(normal.Y > 0.4f);
        }

        #endregion
    }
}
