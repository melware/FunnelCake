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
	class Tile : GameObject
	{
		private GOType type;

		public Tile(GOType t, Rectangle bound)
			: base(bound) { type = t; }

		public override GOType Type { get { return type; } }
	}
}
