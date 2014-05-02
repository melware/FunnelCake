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



}
