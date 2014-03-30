using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FunnelCake
{
	enum GOType 
	{
		EMPTY = '.', 
		BSOLID = 'x', BPLANK = '=', 
		PLAYER = 'p', 
		CRAWLER = 'c', FLYER = 'f'
	};
	abstract class GameObject
	{
		private Rectangle boundBox;

		public GameObject(Rectangle b)
		{
			boundBox = b;
		}

		public abstract GOType Type { get; }

		public Rectangle Bounds
		{
			get { return boundBox; }
			set { boundBox = value; }
		}

		public Vector2 Location
		{
			get { return new Vector2(boundBox.X, boundBox.Y); }
			set { boundBox.X = (int)value.X; boundBox.Y = (int)value.Y; }
		}

		public float X { get { return boundBox.X; } set { boundBox.X = (int)value; } }
		public float Y { get { return boundBox.Y; } set { boundBox.Y = (int)value; } }
		public int Width { get { return boundBox.Width; } set { boundBox.Width = value; } }
		public int Height { get { return boundBox.Height; } set { boundBox.Height = value; } }

		public virtual Rectangle Intersects(GameObject otherObj)
		{
			Rectangle me = new Rectangle((int)X, (int)Y, Width, Height);
			Rectangle other = new Rectangle((int)otherObj.X, (int)otherObj.Y, otherObj.Width, otherObj.Height);
			return Rectangle.Intersect(me, other);
		}
	}
}
