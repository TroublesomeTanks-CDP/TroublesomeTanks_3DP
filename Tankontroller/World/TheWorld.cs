using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tankontroller.Managers;
using Tankontroller.Utilities;
using Tankontroller.World.Particles;
using Tankontroller.World.Pickups;
using Tankontroller.World.Shapes;
using Tankontroller.World.WorldObject;

public enum PickupType
{
    HEALTH,
    EMP,
    MINE,
    BOUNCY_BULLET
}

namespace Tankontroller.World
{
    public class TheWorld
    {
        private static readonly Texture2D mPixelTexture = Tankontroller.Instance().CM().Load<Texture2D>("block");
        private static readonly Texture2D m_BulletTexture = Tankontroller.Instance().CM().Load<Texture2D>("circle");
        private static readonly Color GROUND_COLOUR = DGS.Instance.GetColour("COLOUR_GROUND");
        public static bool PICKUP_SPAWN = DGS.Instance.GetBool("PICKUPS_ON");
        public static float PICKUP_SPAWN_TIME = DGS.Instance.GetFloat("PICKUP_SPAWN_RATE");
        public static bool HEALTH_PICKUP = DGS.Instance.GetBool("ADD_PICKUP_HEALTH");
        public static bool EMP_PICKUP = DGS.Instance.GetBool("ADD_PICKUP_EMP");
        public static bool MINE_PICKUP = DGS.Instance.GetBool("ADD_PICKUP_MINE");
        public static bool BOUNCY_BULLET_PICKUP = DGS.Instance.GetBool("ADD_PICKUP_BOUNCYBULLET");

        private Rectangle mPlayArea;
        private Rectangle mPlayAreaOutline;
        private RectangleAxisAlignedShape[] mPlayAreaCollisionShapes = new RectangleAxisAlignedShape[4];
        private List<Tank> mTanks = new List<Tank>();
        private List<RectWall> mWalls;
        private List<CollisionShape> mWallColliders = new List<CollisionShape>();
        private List<PickupSpawnPoint> mPickupSpawnPositions = new List<PickupSpawnPoint>();
        private List<Pickup> mPickups = new List<Pickup>();
        private float mPickupSpawnTimer = PICKUP_SPAWN_TIME;
        private List<PickupType> mActivatedPickups = new List<PickupType>();

        public Rectangle PlayArea { get { return mPlayArea; } }

        public TheWorld(Rectangle pPlayArea, List<RectWall> pWalls, List<Tank> pTanks, List <PickupSpawnPoint> pPickupSpawnPositions)
        {
            mWalls = pWalls;
            mTanks = pTanks;
            mPlayArea = pPlayArea;
            mPickupSpawnPositions = pPickupSpawnPositions;
            mPlayAreaOutline = new Rectangle(mPlayArea.X - 5, mPlayArea.Y - 5, mPlayArea.Width + 10, mPlayArea.Height + 10);
            CreatePlayAreaCollisionShapes();
            CreateWallColliders();
            CheckActivatedPickups();
        }

        private void CreatePlayAreaCollisionShapes()
        {
            // Top
            mPlayAreaCollisionShapes[0] = new RectangleAxisAlignedShape(
                new Transform(new Vector2(mPlayArea.X + mPlayArea.Width / 2f, mPlayArea.Y - 5)), new Vector2(mPlayArea.Width, 10));
            // Bottom
            mPlayAreaCollisionShapes[1] = new RectangleAxisAlignedShape(
                new Transform(new Vector2(mPlayArea.X + mPlayArea.Width / 2f, mPlayArea.Y + mPlayArea.Height + 5)), new Vector2(mPlayArea.Width, 10));
            // Left
            mPlayAreaCollisionShapes[2] = new RectangleAxisAlignedShape(
                new Transform(new Vector2(mPlayArea.X - 5, mPlayArea.Y + mPlayArea.Height / 2f)), new Vector2(10, mPlayArea.Height + 20));
            // Right
            mPlayAreaCollisionShapes[3] = new RectangleAxisAlignedShape(
                new Transform(new Vector2(mPlayArea.X + mPlayArea.Width + 5, mPlayArea.Y + mPlayArea.Height / 2f)), new Vector2(10, mPlayArea.Height + 20));
        }

        /// <summary>
        /// Builds the list of everything a tank or bullet can hit: the walls, then the edges of the play area.
        /// The walls and play area never change, so this is only done once.
        /// </summary>
        private void CreateWallColliders()
        {
            mWalls.ForEach(wall => mWallColliders.Add(wall.CollisionShape));
            mWallColliders.AddRange(mPlayAreaCollisionShapes);
        }

        public List<Tank> GetTanksForPlayers(int pPlayerCount)
        {
            if (mTanks.Count >= pPlayerCount && pPlayerCount > 0)
            {
                mTanks = mTanks.GetRange(0, (int)pPlayerCount);
                return mTanks;
            }
            return null;
        }

        public void AddPickup()
        {
            mPickupSpawnTimer = PICKUP_SPAWN_TIME;
            if (PICKUP_SPAWN && mActivatedPickups.Count() > 0 && mPickupSpawnPositions.Count() > 0)
            {
                int randPos = new Random().Next(0, mPickupSpawnPositions.Count());
                //Checks for any pickups at this position to prevent spawn overlap
                foreach (Pickup p in mPickups)
                {
                    if (p.Transform.Position == mPickupSpawnPositions[randPos].GetPosition())
                    {
                        return;
                    }
                }

                int randPickup = new Random().Next(0, mActivatedPickups.Count());
                if (mActivatedPickups[randPickup] == PickupType.HEALTH && mPickupSpawnPositions[randPos].IsPickupTypeActivated(PickupType.HEALTH))
                {
                    HealthPickup mHealthPickup = new HealthPickup(mPickupSpawnPositions[randPos].GetPosition());
                    mPickups.Add(mHealthPickup);
                }
                else if (mActivatedPickups[randPickup] == PickupType.EMP && mPickupSpawnPositions[randPos].IsPickupTypeActivated(PickupType.EMP))
                {
                    EMPPickup mEMPPickup = new EMPPickup(mPickupSpawnPositions[randPos].GetPosition());
                    mPickups.Add(mEMPPickup);
                }
                else if (mActivatedPickups[randPickup] == PickupType.MINE && mPickupSpawnPositions[randPos].IsPickupTypeActivated(PickupType.MINE))
                {
                    MinePickup mMinePickup = new MinePickup(mPickupSpawnPositions[randPos].GetPosition());
                    mPickups.Add(mMinePickup);
                }
                else if (mActivatedPickups[randPickup] == PickupType.BOUNCY_BULLET && mPickupSpawnPositions[randPos].IsPickupTypeActivated(PickupType.BOUNCY_BULLET))
                {
                    BouncyBulletPickup mBouncyBulletPickup = new BouncyBulletPickup(mPickupSpawnPositions[randPos].GetPosition());
                    mPickups.Add(mBouncyBulletPickup);
                }
            }
        }

        public void CheckActivatedPickups()
        {
            foreach (PickupType p in Enum.GetValues(typeof(PickupType)))
            {
                if(p == PickupType.HEALTH && HEALTH_PICKUP)
                {
                    mActivatedPickups.Add(p);
                }
                else if (p == PickupType.EMP && EMP_PICKUP)
                {
                    mActivatedPickups.Add(p);
                }
                else if (p == PickupType.MINE && MINE_PICKUP)
                {
                    mActivatedPickups.Add(p);
                }
                else if (p == PickupType.BOUNCY_BULLET && BOUNCY_BULLET_PICKUP)
                {
                    mActivatedPickups.Add(p);
                }
            }
        }

        public void Update(float pSeconds)
        {
            mPickupSpawnTimer -= pSeconds;
            if(mPickupSpawnTimer <= 0) { AddPickup(); }
            Particles.ParticleManager.Instance().Update(pSeconds);

            // Check collisions for each tank
            foreach (Tank tank in mTanks)
            {
                if (tank.GetState() == TankStates.DESTROYED)
                {
                    continue;
                }

                tank.Update(pSeconds);

                // Bullet collisions
                tank.HandleBulletCollisions(mTanks, mWallColliders);

                // Pickup collisions
                foreach (Pickup pickup in mPickups)
                {
                    // This is to avoid any dead tanks from picking up a pickup
                    if (tank.GetState() != TankStates.ALIVE)
                    {
                        continue;
                    }
                    else if (pickup.CollisionShape.Intersects(tank.CollisionShape).HasCollided)
                    {
                        pickup.TriggerEffect(tank);
                        mPickups.Remove(pickup);
                        break;
                    }
                }

                CollisionManager.ResolveTankWallCollisions(tank, mWallColliders);
                ResolveTankCollisions(tank);

                tank.CommitPlacement();
            }
        }

        /// <summary>
        /// Puts a tank back to its previous position if it is overlapping another tank that has not been destroyed
        /// </summary>
        private void ResolveTankCollisions(Tank pTank)
        {
            foreach (Tank otherTank in mTanks)
            {
                if (otherTank == pTank || otherTank.GetState() == TankStates.DESTROYED)
                {
                    continue;
                }

                if (pTank.CollisionShape.Intersects(otherTank.CollisionShape).HasCollided)
                {
                    pTank.PutBack();
                }
            }
        }

        public void Draw(SpriteBatch pSpriteBatch)
        {
            pSpriteBatch.Draw(mPixelTexture, mPlayAreaOutline, Color.Black);
            pSpriteBatch.Draw(mPixelTexture, mPlayArea, GROUND_COLOUR);

            TrackSystem.GetInstance().Draw(pSpriteBatch);

            foreach (Pickup p in mPickups)
            {
                p.Draw(pSpriteBatch);
            }

            ParticleManager.Instance().Draw(pSpriteBatch);

            //Draws the tanks (on top of tracks but below particles)
            foreach (Tank t in mTanks)
            {
                t.DrawBullets(pSpriteBatch, m_BulletTexture);
            }

            //Draws the tanks
            foreach (Tank t in mTanks)
            {
                t.Draw(pSpriteBatch);
            }

            //Draws the walls
            foreach (RectWall w in mWalls)
            {
                w.DrawOutlines(pSpriteBatch);
            }
            foreach (RectWall w in mWalls)
            {
                w.Draw(pSpriteBatch);
            }

            if (CollisionManager.DRAW_COLLISION_SHAPES)
            {
                foreach (RectangleAxisAlignedShape shape in mPlayAreaCollisionShapes)
                {
                    DrawUtilities.DrawRectangle(pSpriteBatch, shape.ToRectangle(), Color.Red, 0, shape.WorldPosition, 1);
                }
            }
        }
    }
}
