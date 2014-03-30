using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FunnelCake
{
	class Player : GameObject
	{
		bool jumpState;
		float curJumpVel;
		public Player(Rectangle bound)
			: base(bound)
		{
			jumpState = false;
			curJumpVel = 0;
		}

		public override GOType Type
		{
			get { return GOType.PLAYER; }
		}

		public bool isJumping
		{
			get { return jumpState; }
			// If the player is no longer jumping, also set jump velocity to 0
			set { jumpState = value; if (!jumpState) curJumpVel = 0; }
		}
		public float JumpVel
		{
			get { return curJumpVel; }
			set { curJumpVel = value; }
		}
	}
}
