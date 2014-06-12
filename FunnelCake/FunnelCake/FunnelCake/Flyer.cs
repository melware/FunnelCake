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

    // Animal that flocks
    class Flyer : Animal
    {

        static float VIEW_RADIUS = 2f * Game1.BLOCK_DIM;
        static float SEP_RADIUS = 1.5f * Game1.BLOCK_DIM;
        static float ALIGNMENT_WT = 0.11f;
        static float COHESION_WT = 0.35f;
        static float SEPARATION_WT = 0.35f;
        static float HUMAN_WT = 1.5f;
        static float BLOCK_WT = 0.4f;
        public bool track;
        public Flyer(Rectangle bound, float vel, bool t = false)
            : base(bound, vel)
        {
            //wanderDir = new Vector2((float)rand.NextDouble()*2-1.5f, (float)rand.NextDouble()*2-1.5f);
            //if(wanderDir != Vector2.Zero) wanderDir.Normalize();

            Random rand = new Random();
            velocity = Vector2.Zero;
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
            if (playerDist < VIEW_RADIUS * 1.5)
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
            if (this.OriginX < VIEW_RADIUS) { blockDirAvg.X += this.OriginX; }
            if (Game1.WIDTH - this.OriginX < VIEW_RADIUS) { blockDirAvg.X -= (Game1.WIDTH - this.OriginX); }
            if (this.OriginY < VIEW_RADIUS) { blockDirAvg.Y += this.OriginY; }
            if (Game1.HEIGHT - this.OriginY < VIEW_RADIUS) { blockDirAvg.Y -= (Game1.HEIGHT - this.OriginY); }
            if (!blockDirAvg.Equals(Vector2.Zero)) { blockDirAvg.Normalize(); }

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
                        float weight = 1 - (dist / (SEP_RADIUS));
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
                }
            }

            // Apply a weight to the Cohesion steering direction based on the distance from this animal
            Vector2 cohSteerVec = cohesionAvg - this.Origin;
            if (!cohSteerVec.Equals(Vector2.Zero)) cohSteerVec.Normalize();
            float cohWeight = (Vector2.Distance(cohesionAvg, this.Origin) / (VIEW_RADIUS));
            cohSteerVec = Vector2.Multiply(cohSteerVec, cohWeight);

            // Sum up all the different directions to steer towards
            velocity += Vector2.Multiply(cohSteerVec, COHESION_WT) +
                        Vector2.Multiply(separationAvg, SEPARATION_WT) +
                        Vector2.Multiply(alignmentAvg, ALIGNMENT_WT) +
                        Vector2.Multiply(humanDirAvg, HUMAN_WT) +
                        Vector2.Multiply(blockDirAvg, BLOCK_WT);

            if (!velocity.Equals(Vector2.Zero)) velocity.Normalize();

            boundBox.X += (int)((velocity.X) * speed * (flee ? fleeMultiplier : 1));
            boundBox.Y += (int)((velocity.Y) * speed * (flee ? fleeMultiplier : 1));
            flee = false;

            // Keep from flying off the screen
            boundBox.X = (int)MathHelper.Clamp(boundBox.X, 0, Game1.WIDTH - boundBox.Width);
            boundBox.Y = (int)MathHelper.Clamp(boundBox.Y, 0, Game1.HEIGHT - boundBox.Height - Game1.BLOCK_DIM);

            //// Wrap around the screen
            //if (boundBox.X > Game1.WIDTH - boundBox.Width) boundBox.X = 0;
            //if (boundBox.X < 0) boundBox.X = Game1.WIDTH - boundBox.Width;
            //if (boundBox.Y > Game1.WIDTH - boundBox.Height) boundBox.Y = 0;
            //if (boundBox.Y < 0) boundBox.Y =  Game1.HEIGHT - boundBox.Height;

            this.X = MathHelper.Clamp(this.X, 0, Game1.WIDTH - Game1.BLOCK_DIM);
            this.UpdateOldRec();
        }

    }
}
