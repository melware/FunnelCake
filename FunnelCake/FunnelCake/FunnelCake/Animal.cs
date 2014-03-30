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
		public abstract void doWander(Tile[,] gameScreen);
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
					Rectangle intersect = Intersects(b);
					if (intersect.Width > 0) intersectedWidth += intersect.Width;
				}
			}
			if (intersectedWidth < walkingBox.Width)
			{
				velocity.X *= -1;
			}
		}

		// Only use this function to find ground
		public override Rectangle Intersects(GameObject otherObj)
		{
			Rectangle other = new Rectangle((int)otherObj.X, (int)otherObj.Y, otherObj.Width, otherObj.Height);
			return Rectangle.Intersect(walkingBox, other);
		}
	}


}
