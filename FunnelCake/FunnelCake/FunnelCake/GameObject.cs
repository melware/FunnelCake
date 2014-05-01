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
        CRAWLER = 'c', FLYER = 'f',
        UP = '1', DOWN = '2', LEFT = '3', RIGHT = '4',
        HALF = '5', DOUBLE = '6', NORMAL = '7', OFF = '0'
    };

    enum portalType1 { NORMAL, UPSIDE, LEFTSIDE, RIGHTSIDE };
    enum portalType2 { NORMAL, HALF, DOUBLE };

	abstract class GameObject
	{
		public Rectangle boundBox;
        public portalType1 pt1;
        public portalType2 pt2;

		public GameObject(Rectangle b)
		{
			boundBox = b;

            pt1 = portalType1.NORMAL;
            pt2 = portalType2.NORMAL;
		}

		public abstract GOType Type { get; }

        public void SetPortal(portalType1 pt)
        {
            pt1 = pt;
        }

        public void SetPortal(portalType2 pt)
        {
            pt2 = pt;
        }

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

		public Vector2 Origin
		{
			get { return new Vector2(boundBox.X + Width / 2, boundBox.Y + Height / 2); }
		}

		public float X { get { return boundBox.X; } set { boundBox.X = (int)value; } }
		public float Y { get { return boundBox.Y; } set { boundBox.Y = (int)value; } }
		public float OriginX { get { return boundBox.X+Width/2; } }
		public float OriginY { get { return boundBox.Y+Height/2; } }
		public int Width { get { return boundBox.Width; } set { boundBox.Width = value; } }
		public int Height { get { return boundBox.Height; } set { boundBox.Height = value; } }

		public virtual Rectangle Intersect(GameObject otherObj)
		{
			Rectangle me = new Rectangle((int)X, (int)Y, Width, Height);
			Rectangle other = new Rectangle((int)otherObj.X, (int)otherObj.Y, otherObj.Width, otherObj.Height);
			return Rectangle.Intersect(me, other);
		}
		public bool Intersects(GameObject otherObj)
		{
			Rectangle me = new Rectangle((int)X, (int)Y, Width, Height);
			Rectangle other = new Rectangle((int)otherObj.X, (int)otherObj.Y, otherObj.Width, otherObj.Height);
			return me.Intersects(other);
		}
	}
}
