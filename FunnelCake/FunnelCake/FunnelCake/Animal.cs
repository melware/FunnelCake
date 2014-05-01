using System;
using System.Collections.Generic;
using System.Globalization;
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
	abstract class Animal : Player
	{
		public Vector2 velocity;
		protected static float VIEW_RADIUS;
		protected static float SEP_RADIUS;
		protected static float COHESION_WT;
		protected static float SEPARATION_WT;
		protected static float ALIGNMENT_WT;

		public Animal(Rectangle bound, float vel)
			: base(bound, vel)
		{
			pt1 = portalType1.NORMAL;
			pt2 = portalType2.NORMAL;
		}

		public virtual void doWander(Tile[,] gameScreen, List<Animal> flock = null) { }


		///// <summary>
		///// Returns whether the parameter animal is within the separation radius of this animal
		///// </summary>
		//public bool isTooClose(Animal animal)
		//{
		//    return (Vector2.DistanceSquared(this.Origin, animal.Origin) <= SEP_RADIUS*SEP_RADIUS);
		//}


		/// <summary>
		/// Returns the neighbors within the viewing range of an Animal
		/// </summary>
		public static List<Animal> getNeighbors(Animal me, List<Animal> flock)
		{
			List<Animal> neighs = new List<Animal>();
			foreach (Animal a in flock)
			{
				float distanceSq = Vector2.DistanceSquared(a.Origin, me.Origin);
				if (distanceSq < VIEW_RADIUS * VIEW_RADIUS &&
					!me.Equals(a))
				{
					neighs.Add(a);
				}
			}
			return neighs;
		}

		/// <summary>
		/// Returns the solid or plank blocks within the viewing range of an Animal
		/// </summary>
		public static List<Tile> getNearbyBlocks(Animal me, Tile[,] map)
		{
			List<Tile> blocks = new List<Tile>();
			foreach(Tile b in map)
			{
				if (b != null && (b.Type == GOType.BPLANK || b.Type == GOType.BSOLID) &&
					Vector2.DistanceSquared(b.Origin, me.Origin) <= VIEW_RADIUS*VIEW_RADIUS)
				{
					blocks.Add(b);
				}
			}
			return blocks;
		}
	}

	class Crawler : Animal
	{
		protected Rectangle walkingBox; // For detecting ground to wander on

		public Crawler(Rectangle bound, float vel)
			: base(bound, vel)  // Make sure the Y-velocity remains zero
		{
			// Create a box right below the sprite to find ground
			walkingBox = new Rectangle((int)(base.X + base.Width), (int)(base.Y + base.Height), (int)base.Width, 1);
			velocity = new Vector2(vel,0);
		}
		public override GOType Type { get { return GOType.CRAWLER; } }

		public override void doWander(Tile[,] gameScreen, List<Animal> flock = null)
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
			if (intersectedWidth != 0 && intersectedWidth < walkingBox.Width)
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

	// Animal that flocks
	class Flyer : Animal
	{
		//public Vector2 wanderDir; // UNNECESSARY ?

		public Flyer(Rectangle bound, float vel)
			: base(bound, vel) 
		{
			//wanderDir = new Vector2((float)rand.NextDouble()*2-1.5f, (float)rand.NextDouble()*2-1.5f);
			//if(wanderDir != Vector2.Zero) wanderDir.Normalize();
			
			Random rand = new Random();
			velocity = Vector2.Zero;
			VIEW_RADIUS = 2 * Game1.BLOCK_DIM;
			SEP_RADIUS = 1.2f*Game1.BLOCK_DIM;
			ALIGNMENT_WT = 0.4f;
			COHESION_WT = 0.6f;
			SEPARATION_WT = 0.6f;
		}

		public override GOType Type { get { return GOType.FLYER; } }



		public override void doWander(Tile[,] gameScreen, List<Animal> flock)
		{
			//Vector2 tmpWanderDir;
			//float tmpX;
			//float tmpY;

			List<Animal> neighbors = getNeighbors(this, flock);
			List<Tile> blocks = getNearbyBlocks(this, gameScreen);

			// Determine Cohesion velocity
			Vector2 cohesionAvg = Vector2.Zero;
			Vector2 separationAvg = Vector2.Zero;
			Vector2 alignmentAvg = Vector2.Zero;

			int cohesionCount = 0;
			int separationCount = 0;

			if (neighbors.Count > 0)
			{
				// Find the average component locations
				foreach (Animal a in neighbors)
				{
					float distSq = Vector2.DistanceSquared(this.Origin, a.Origin);

					// Handle separation/cohesion
					if (distSq <= SEP_RADIUS* SEP_RADIUS) // neighbor is too close
					{
						// Add to the separation vector
						float diff = distSq - SEP_RADIUS * SEP_RADIUS;
						separationAvg = Vector2.Add(separationAvg, Vector2.Negate(a.velocity));
						//// Weigh the separation component by the distance
						Vector2.Multiply(separationAvg, 1.0f + diff / (SEP_RADIUS * SEP_RADIUS));
						separationCount++;
					}
					else
					{
						// Add to the cohesion vector
						cohesionAvg = Vector2.Add(cohesionAvg, a.Origin);

						alignmentAvg = Vector2.Add(alignmentAvg, a.velocity);


						cohesionCount++;
					}
				}

				if (cohesionCount > 0)
				{
					cohesionAvg = Vector2.Divide(cohesionAvg, cohesionCount);
					alignmentAvg = Vector2.Divide(alignmentAvg, cohesionCount);
				}
				if(separationCount > 0) { separationAvg = Vector2.Divide(separationAvg, separationCount);}
				
			}

			// TODO Avoid blocks

			// TODO Avoid human

			Vector2 steerVec = Vector2.Zero;
			if(!alignmentAvg.Equals(Vector2.Zero))
			{
				steerVec = Vector2.Normalize(alignmentAvg) - velocity;
			}
			Vector2.Multiply(steerVec, ALIGNMENT_WT);

			velocity += Vector2.Multiply(cohesionAvg, COHESION_WT) +
						Vector2.Multiply(separationAvg, SEPARATION_WT) +
						steerVec;
			
			//string k = velocity.X + ", " + velocity.Y;
			
			if(!velocity.Equals(Vector2.Zero))velocity.Normalize();

			//Console.WriteLine("Before: " + k + "; After: " + velocity.X + ", " + velocity.Y);

			boundBox.X += (int)((velocity.X) * speed);
			boundBox.Y += (int)((velocity.Y) * speed);

			//// Keep from flying off the screen
			//boundBox.X = (int)MathHelper.Clamp(boundBox.X, 0, Game1.WIDTH-boundBox.Width);
			//boundBox.Y = (int)MathHelper.Clamp(boundBox.Y, 0, Game1.HEIGHT-boundBox.Height-Game1.BLOCK_DIM);

			// Wrap screen
			if (boundBox.X > Game1.WIDTH - boundBox.Width) boundBox.X = 0;
			if (boundBox.X < 0) boundBox.X = Game1.WIDTH - boundBox.Width;
			if (boundBox.Y > Game1.WIDTH - boundBox.Height) boundBox.Y = 0;
			if (boundBox.Y < 0) boundBox.Y =  Game1.HEIGHT - boundBox.Height;
			
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
	}

}
