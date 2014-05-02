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
                    JumpVel = Game1.PLAYER_JUMP * 2 / 3;
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
                    tempRec = new Rectangle((int)(base.X + base.Width), (int)(base.Y), 1, (int)base.Height);
                    break;
                case portalType1.RIGHTSIDE:
                    tempRec = new Rectangle((int)(base.X), (int)(base.Y), 1, (int)base.Height);
                    break;
                case portalType1.UPSIDE:
                    tempRec = new Rectangle((int)(base.X), (int)(base.Y - 1), (int)base.Width, 1);
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
}
