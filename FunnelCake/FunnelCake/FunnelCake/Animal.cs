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
		protected static float VIEW_RADIUS;
		protected static float SEP_RADIUS;
		protected static float COHESION_WT;
		protected static float SEPARATION_WT;
		protected static float ALIGNMENT_WT;
		protected static float HUMAN_WT;
		protected static float BLOCK_WT;
		public bool flee;
		protected static float fleeMultiplier = 2;

		protected Rectangle walkingBox; // For detecting ground to wander on

		public Animal(Rectangle bound, float vel)
			: base(bound, vel)
		{
			// Create a box right below the sprite to find ground
			walkingBox = new Rectangle((int)(base.X), (int)(base.Y + base.Height), (int)base.Width, 1);

			pt1 = portalType1.NORMAL;
			pt2 = portalType2.NORMAL;
			flee = false;
		}

		public virtual void doWander(Tile[,] gameScreen, List<Animal> flock = null, Player player = null, GameTime gameTime = null) { }


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
					distanceSq > 0)
					//!me.Equals(a))
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

		public Crawler(Rectangle bound, float vel)
			: base(bound, vel)  
		{
			velocity = new Vector2(vel,0);
		}
		public override GOType Type { get { return GOType.CRAWLER; } }

		public override void doWander(Tile[,] gameScreen, List<Animal> flock = null, Player player = null, GameTime gameTime = null)
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
			} else if (intersectedWidth == 0)
			{
				isJumping = true;
				JumpVel = 0;
			}
            this.X = MathHelper.Clamp(this.X, 0, Game1.WIDTH - Game1.BLOCK_DIM);
			this.UpdateOldRec();
		}

		// Only use this function to find ground
		public override Rectangle Intersect(GameObject otherObj)
		{
			Rectangle other = new Rectangle((int)otherObj.X, (int)otherObj.Y, otherObj.Width, otherObj.Height);
			return Rectangle.Intersect(walkingBox, other);
		}
	}

	class Jumper : Animal
	{
		public Jumper(Rectangle bound, float vel)
			: base(bound, vel)
		{
			isJumping = false;
			JumpVel = 0;
			VIEW_RADIUS = 2 * Game1.BLOCK_DIM;
			velocity = new Vector2(1, 0);
		}

		public override GOType Type { get { return GOType.JUMPER; } }

		// Flock algorithm
		public override void doWander(Tile[,] gameScreen, List<Animal> animals, Player player, GameTime gameTime)
		{
			Vector2 tmpVector = velocity;
			if (Vector2.Distance(player.Origin, this.Origin) < VIEW_RADIUS)
			{
				flee = true;
                bool shouldJump = true;


				if (!isJumping && shouldJump)
				{
					isJumping = true;
					JumpVel = Game1.PLAYER_JUMP*2/3;
				}
				tmpVector.X = this.Origin.X - player.Origin.X;
                if (tmpVector.X == 0)
                    tmpVector.X = .01f;
			}

			if (!tmpVector.Equals(Vector2.Zero)) tmpVector.Normalize();
			tmpVector.X += tmpVector.X * speed * (flee ? fleeMultiplier : 1);
			// Wandering 
			int intersectedWidth = 0;
			Rectangle tempRec;
			switch (this.pt1)
			{
				case portalType1.NORMAL:
					tempRec = new Rectangle((int)(base.X), (int)(base.Y + base.Height), (int)base.Width, 1);
					break;
				case portalType1.LEFTSIDE:
					tempRec = new Rectangle((int)(base.X+base.Width), (int)(base.Y), 1, (int)base.Height);
					break;
				case portalType1.RIGHTSIDE:
					tempRec = new Rectangle((int)(base.X), (int)(base.Y), 1, (int)base.Height);
					break;
				case portalType1.UPSIDE:
					tempRec = new Rectangle((int)(base.X), (int)(base.Y -1), (int)base.Width, 1);
					break;
				default:
					tempRec = new Rectangle((int)(base.X), (int)(base.Y + base.Height), (int)base.Width, 1);
					break;

			}
			foreach (Tile b in gameScreen)
			{
				if (b != null)
				{
					Rectangle intersect = Rectangle.Intersect(b.boundBox, tempRec);
					switch (this.pt1)
					{
						case portalType1.NORMAL:
						case portalType1.UPSIDE:
							if (intersect.Width > 0) intersectedWidth += intersect.Width;
							break;
						case portalType1.LEFTSIDE:
						case portalType1.RIGHTSIDE:
							if (intersect.Height > 0) intersectedWidth += intersect.Height;
							break;
						default:
							break;
					}
				}
			}
            this.X += tmpVector.X;
            /*
			switch (this.pt1)
			{
				case portalType1.NORMAL:
					this.X += tmpVector.X;
					break;
				case portalType1.LEFTSIDE:
					this.Y -= tmpVector.X;
					break;
				case portalType1.UPSIDE:
					this.X -= tmpVector.X;
					break;
				case portalType1.RIGHTSIDE:
					this.Y += tmpVector.X;
					break;
				default:
					break;
			}*/
			if (intersectedWidth != 0 && intersectedWidth < this.Width)
			{
				tmpVector.X *= -1;
				switch (this.pt1)
				{
					case portalType1.NORMAL:
						this.X += tmpVector.X;
						break;
					case portalType1.LEFTSIDE:
						this.Y -= tmpVector.X;
						break;
					case portalType1.UPSIDE:
						this.X -= tmpVector.X;
						break;
					case portalType1.RIGHTSIDE:
						this.Y += tmpVector.X;
						break;
					default:
						break;
				}
			}
			if (isJumping)
			{
				if (this.pt2 == portalType2.NORMAL)
					this.JumpVel -= (Game1.GRAVITY * (float)gameTime.ElapsedGameTime.TotalSeconds) + 4;
				else if (this.pt2 == portalType2.HALF)
					this.JumpVel -= (Game1.GRAVITY / 2.4f) * (float)gameTime.ElapsedGameTime.TotalSeconds;
				else if (this.pt2 == portalType2.DOUBLE)
					this.JumpVel -= (Game1.GRAVITY * 1.5f * (float)gameTime.ElapsedGameTime.TotalSeconds) + 5;
				float val = this.JumpVel * (float)gameTime.ElapsedGameTime.TotalSeconds;
				tmpVector.Y -= val;

				switch (this.pt1)
				{
					case portalType1.NORMAL:
						tempRec.Height += (int)val + 1;
						this.Y += tmpVector.Y;
						break;
					case portalType1.LEFTSIDE:
						tempRec.Height -= ((int)val + 1);
						this.X -= tmpVector.Y;
						break;
					case portalType1.UPSIDE:
						tempRec.Width += ((int)val + 1);
						this.Y -= tmpVector.Y;
						break;
					case portalType1.RIGHTSIDE:
						tempRec.Width -= ((int)val + 1);
						this.X += tmpVector.Y;
						break;
					default:
						break;
				}
			}
			flee = false;
			// Keep from flying off the screen
			boundBox.X = (int)MathHelper.Clamp(boundBox.X, 0, Game1.WIDTH - boundBox.Width);
			boundBox.Y = (int)MathHelper.Clamp(boundBox.Y, 0, Game1.HEIGHT - boundBox.Height - Game1.BLOCK_DIM);
			this.UpdateOldRec();
		}
	}

	// Animal that flocks
	class Flyer : Animal
	{
		//public Vector2 wanderDir; // UNNECESSARY ?
		public bool track;
		public Flyer(Rectangle bound, float vel, bool t = false)
			: base(bound, vel) 
		{
			//wanderDir = new Vector2((float)rand.NextDouble()*2-1.5f, (float)rand.NextDouble()*2-1.5f);
			//if(wanderDir != Vector2.Zero) wanderDir.Normalize();
			
			Random rand = new Random();
			velocity = Vector2.Zero;
			VIEW_RADIUS = 2f * Game1.BLOCK_DIM;
			SEP_RADIUS = 1.5f*Game1.BLOCK_DIM;
			ALIGNMENT_WT = 0.11f;
			COHESION_WT = 0.35f;
			SEPARATION_WT = 0.35f;
			HUMAN_WT = 1.5f;
			BLOCK_WT = 0.4f;
			track = t;
		}

		public override GOType Type { get { return GOType.FLYER; } }

		public override void doWander(Tile[,] gameScreen, List<Animal> flock, Player player, GameTime gameTime = null)
		{
			List<Animal> neighbors = getNeighbors(this, flock);
			List<Tile> blocks = getNearbyBlocks(this, gameScreen);

			Vector2 cohesionAvg = Vector2.Zero;
			Vector2 separationAvg = Vector2.Zero;
			Vector2 alignmentAvg = Vector2.Zero;
			Vector2 humanDirAvg = Vector2.Zero;
			Vector2 blockDirAvg = Vector2.Zero;

			// First, check for human
			float playerDist = Vector2.Distance(player.Origin, this.Origin);
			if (playerDist < VIEW_RADIUS*1.5)
			{
				Vector2 humanSteerDir = this.Origin - player.Origin;
				humanSteerDir.Normalize();
				humanDirAvg = humanSteerDir;
				flee = true;
			}

			// Then, check for blocks
			if (blocks.Count > 0)
			{
				foreach (Tile b in blocks)
				{
					float blockDist = Vector2.Distance(b.Origin, this.Origin);
					Vector2 blockSteerDir = this.Origin - b.Origin;
					blockDirAvg += blockSteerDir;
				}
			}
			// Check boundaries
			if(this.OriginX < VIEW_RADIUS)			{ blockDirAvg.X += this.OriginX; }
			if(Game1.WIDTH - this.OriginX < VIEW_RADIUS)	{ blockDirAvg.X -= (Game1.WIDTH - this.OriginX); }
			if(this.OriginY < VIEW_RADIUS)			{ blockDirAvg.Y += this.OriginY; }
			if(Game1.HEIGHT - this.OriginY < VIEW_RADIUS)	{ blockDirAvg.Y -= (Game1.HEIGHT - this.OriginY); }
			if(!blockDirAvg.Equals(Vector2.Zero)) {blockDirAvg.Normalize();}

			if (neighbors.Count > 0)
			{
				// Find the average component locations
				foreach (Animal a in neighbors)
				{
					float dist = Vector2.Distance(this.Origin, a.Origin);

				// SEPARATION
					// neighbor is too close, need to separate
					if (dist < SEP_RADIUS)
					{
						Vector2 separationSteerDir = a.Origin - this.Origin;
						separationSteerDir.Normalize();
						// Weigh the separation component by the distance
						// Smaller distance = stronger push
						float weight = 1 - (dist/(SEP_RADIUS));
						separationAvg += Vector2.Multiply(Vector2.Negate(separationSteerDir), weight);

					}

				// COHESION
					// Add to the cohesion location
					cohesionAvg = Vector2.Add(cohesionAvg, a.Origin);

				// ALIGNMENT
					// Add to the alignment direction
					alignmentAvg = Vector2.Add(alignmentAvg, a.velocity);

				}

				if (neighbors.Count > 0)
				{
					alignmentAvg = Vector2.Divide(alignmentAvg, neighbors.Count);
					cohesionAvg = Vector2.Divide(cohesionAvg, neighbors.Count);
					//if(!alignmentAvg.Equals(Vector2.Zero)) alignmentAvg.Normalize();
				}
				//if(separationCount > 0) { separationAvg = Vector2.Divide(separationAvg, separationCount);}
			}

			// TODO Avoid blocks

			// Apply a weight to the Cohesion steering direction
			Vector2 cohSteerVec = cohesionAvg - this.Origin;
			if(!cohSteerVec.Equals(Vector2.Zero)) cohSteerVec.Normalize();
			float cohWeight = (Vector2.Distance(cohesionAvg,this.Origin)/ (VIEW_RADIUS));
			cohSteerVec = Vector2.Multiply(cohSteerVec, cohWeight);


			velocity += Vector2.Multiply(cohSteerVec, COHESION_WT) +
			            Vector2.Multiply(separationAvg, SEPARATION_WT) +
						Vector2.Multiply(alignmentAvg, ALIGNMENT_WT) +
						Vector2.Multiply(humanDirAvg, HUMAN_WT) +
						Vector2.Multiply(blockDirAvg, BLOCK_WT);

			//string k = velocity.X + ", " + velocity.Y;
			
			if(!velocity.Equals(Vector2.Zero))velocity.Normalize();

			//Console.WriteLine("Before: " + k + "; After: " + velocity.X + ", " + velocity.Y);
			
			boundBox.X += (int)((velocity.X) * speed * (flee ? fleeMultiplier : 1));
			boundBox.Y += (int)((velocity.Y) * speed * (flee ? fleeMultiplier : 1));
			flee = false;

			// Keep from flying off the screen
			boundBox.X = (int)MathHelper.Clamp(boundBox.X, 0, Game1.WIDTH - boundBox.Width);
			boundBox.Y = (int)MathHelper.Clamp(boundBox.Y, 0, Game1.HEIGHT - boundBox.Height - Game1.BLOCK_DIM);

			//// Wrap screen
			//if (boundBox.X > Game1.WIDTH - boundBox.Width) boundBox.X = 0;
			//if (boundBox.X < 0) boundBox.X = Game1.WIDTH - boundBox.Width;
			//if (boundBox.Y > Game1.WIDTH - boundBox.Height) boundBox.Y = 0;
			//if (boundBox.Y < 0) boundBox.Y =  Game1.HEIGHT - boundBox.Height;
            this.X = MathHelper.Clamp(this.X, 0, Game1.WIDTH - Game1.BLOCK_DIM);
			this.UpdateOldRec();
			
		}

	}

}
