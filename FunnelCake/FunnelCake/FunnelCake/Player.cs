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
        bool upWhileJump;
		float curJumpVel;
        public Rectangle oldRec;
		public float speed;
        
		public Player(Rectangle bound, float vel)
			: base(bound)
		{
			jumpState = false;
			curJumpVel = 0;
			speed = vel;
            oldRec = bound;
            holdingUp = false;
            pt1 = portalType1.NORMAL;
            pt2 = portalType2.NORMAL;
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

        public bool holdingUp
        {
            get { return upWhileJump; }
            set 
            {
                if (pt2 == portalType2.HALF)
                    upWhileJump = false;
                else
                    upWhileJump = value; 
            }
        }
		public float JumpVel
		{
			get { return curJumpVel; }
            set { curJumpVel = value; }
		}

        public void UpdateOldRec()
        {
            oldRec = this.boundBox;
        }
	}
}
