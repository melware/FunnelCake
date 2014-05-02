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
    class Crawler : Animal
    {

        public Crawler(Rectangle bound, float vel)
            : base(bound, vel)
        {
            velocity = new Vector2(vel, 0);
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
            }
            else if (intersectedWidth == 0)
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

}
