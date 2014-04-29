using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace FunnelCake
{
	abstract class Animal : GameObject
	{

		protected Vector2 velocity;
		public Animal(Rectangle bound, Vector2 vel)
			: base(bound)
		{
			velocity = vel;
		}
		public virtual void doWander(Tile[,] gameScreen) { }
		public virtual void doWander(Tile[,] gameScreen, Random rand) { }
	}

	class Crawler : Animal
	{
		protected Rectangle walkingBox; // For detecting ground to wander on
		public Crawler(Rectangle bound, Vector2 vel)
			: base(bound, new Vector2(vel.X, 0))  // Make sure the Y-velocity remains zero
		{
			// Create a box right below the sprite to find ground
			walkingBox = new Rectangle((int)(base.X + base.Width), (int)(base.Y + base.Height), (int)base.Width, 1);
		}
		public override GOType Type { get { return GOType.CRAWLER; } }

		public override void doWander(Tile[,] gameScreen)
		{
			base.X += velocity.X; walkingBox.X = (int)base.X;
			int intersectedWidth = 0;
			foreach (Tile b in gameScreen)
			{
				if (b != null)
				{
					Rectangle intersect = Intersect(b);
					if (intersect.Width > 0) intersectedWidth += intersect.Width;
				}
			}
			if (intersectedWidth < walkingBox.Width)
			{
				velocity.X *= -1;
			}
		}

		// Only use this function to find ground
		public override Rectangle Intersect(GameObject otherObj)
		{
			Rectangle other = new Rectangle((int)otherObj.X, (int)otherObj.Y, otherObj.Width, otherObj.Height);
			return Rectangle.Intersect(walkingBox, other);
		}
	}

	//// Animal that flocks as well
	//class Jumper : Animal
	//{
	//    bool jumpState;
	//    float curJumpVel;

	//    public Jumper(Rectangle bound, Vector2 vel)
	//        : base(bound, vel) 
	//    {
	//        jumpState = false;
	//        curJumpVel = 0;
	//    }

	//    // Flock algorithm
	//    public override void doWander(Tile[,] gameScreen) 
	//    {
			
	//    }

	//    public bool isJumping
	//    {
	//        get { return jumpState; }
	//        set { jumpState = value; if (!jumpState) curJumpVel = 0; }
	//    }
	//    public float JumpVel
	//    {
	//        get { return curJumpVel; }
	//        set { curJumpVel = value; }
	//    }
	//}

	// Animal that flocks
	class Flyer : Animal
	{
		public Vector2 wanderDir;

		public Flyer(Rectangle bound, Vector2 vel)
			: base(bound, vel) 
		{
			Random rand = new Random();
			wanderDir = new Vector2((float)rand.NextDouble()*2-1.5f, (float)rand.NextDouble()*2-1.5f);
			if(wanderDir != Vector2.Zero) wanderDir.Normalize();
		}

		public override GOType Type { get { return GOType.FLYER; } }

		public override void doWander(Tile[,] gameScreen, Random rand)
		{
			Vector2 tmpWanderDir;
			float tmpX;
			float tmpY;

			tmpWanderDir = wanderDir;
			// Randomly get a direction to head towards
			tmpWanderDir.X += MathHelper.Lerp(-0.4f, 0.4f, (float)rand.NextDouble());
			tmpWanderDir.Y += MathHelper.Lerp(-0.4f, 0.4f, (float)rand.NextDouble());

			// Tend to move away from bounds (towards center)
			Vector2 screenCenter = new Vector2(Game1.WIDTH / 2, Game1.HEIGHT / 2);
			// Find distance from center squared
			float distanceFromCenterSQ = (float)Math.Pow(this.X - screenCenter.X, 2) + (float)Math.Pow(this.Y - screenCenter.Y,2);
			float maxDistanceSQ = screenCenter.X * screenCenter.X ;
			float normalizedDistanceSQ = distanceFromCenterSQ / maxDistanceSQ;
			
			Vector2 centerDir = new Vector2(screenCenter.X - this.X, screenCenter.Y - this.Y);

			if(centerDir != Vector2.Zero) centerDir.Normalize();
			if (tmpWanderDir != Vector2.Zero) tmpWanderDir.Normalize();

			tmpX = boundBox.X + tmpWanderDir.X * 0.5f * velocity.X + centerDir.X * 1.2f * normalizedDistanceSQ * velocity.X;
			tmpY = boundBox.Y + tmpWanderDir.Y * 0.5f * velocity.Y + centerDir.Y * 1.2f * normalizedDistanceSQ * velocity.Y;

			//bool collides = false;
			//// Check if heading in that direction collides with a tile
			//// Calculate possible colliding tiles
			//int startX = (int)Math.Floor(tmpX / boundBox.Width);
			//int endX = (int)Math.Floor((tmpX + boundBox.Width) / boundBox.Width);
			//int startY = (int)Math.Floor(tmpY / boundBox.Height);
			//int endY = (int)Math.Floor((tmpY + boundBox.Height) / boundBox.Height);
			//// Check if each possible tile collides with this object
			//for (int x = startX; x <= endX; x++)
			//{
			//    for (int y = startY; y <= endY; y++)
			//    {
			//        if (x > 0 && x < Game1.COLS
			//            && y > 0 && y < Game1.ROWS)
			//        {
			//            if (gameScreen[y, x] != null && this.Intersects(gameScreen[y, x]))
			//            {
			//                collides = true;
			//                break;
			//            }
			//        }
			//    }
			//}
			wanderDir = tmpWanderDir;
			boundBox.X = (int)MathHelper.Clamp(tmpX, 0, Game1.WIDTH-boundBox.Width);
			boundBox.Y = (int)MathHelper.Clamp(tmpY, 0, Game1.HEIGHT-boundBox.Height-Game1.BLOCK_DIM);

		}
	}

}
