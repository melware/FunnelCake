using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

/*
 * Samir Mohamed Shannon Li
 * CS3113 Game Programming
 * Final project Halfway Deliverable
 * 
 */

namespace FunnelCake
{

	public class Game1 : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
        KeyboardState oldKey;
		const int LEVEL_COUNTDOWN = 20000; // Milliseconds
		int countdown;

		int curLevel;
		const int MAX_LEVELS = 2;

		// Screen size
		const int HEIGHT = 750;
		const int WIDTH = 1000;
		const int ROWS = 15;
		const int COLS = 20;
		const int BLOCK_DIM = 50;   // Block dimension in pixels (width == height)
        const int PORTAL_COLLISION = 6;

		enum GameState { START, PLAY, LOSE, WIN };
		GameState gameState;

		// Sprites
		List<Animal> animals;
		Player player;
		Tile[,] gameScreen;   // Array of tiles to display

		Texture2D blockSolid;
		Texture2D blockPlank	;
		Texture2D crawlerSprite	;
		Texture2D playerSprite	;

        Texture2D portaloff, portalup, portaldown, portalleft, 
            portalright, portalhalf, portaldouble;

		// Fonts
		SpriteFont titleFont;
		SpriteFont subTitleFont;

		Vector2 CRAWLER_SPEED = new Vector2(2,0);

		const float PLAYER_SPEED = 4;
		const float PLAYER_JUMP = 300 / 0.5f; // jump height / time to reach height
		const float GRAVITY = 350 / 0.25f;
		const float OBJ_SPEED = 0.5f;
        const float HOLD_UP = 10;

		int score;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			graphics.PreferredBackBufferHeight = HEIGHT;
			graphics.PreferredBackBufferWidth = WIDTH;
			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			gameState = GameState.START;
			gameScreen = new Tile[ROWS, COLS];
			animals = new List<Animal>();
			score = 0;
			curLevel = 1;
            oldKey = Keyboard.GetState();

			base.Initialize();
		}

		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);

			blockSolid		= Content.Load<Texture2D>(@"Sprites/block_solid");
			blockPlank		= Content.Load<Texture2D>(@"Sprites/block_plank");
			crawlerSprite	= Content.Load<Texture2D>(@"Sprites/pet");
			playerSprite	= Content.Load<Texture2D>(@"Sprites/player");
			loadLevel(1);

			titleFont = Content.Load<SpriteFont>(@"Fonts\Titles");
			subTitleFont = Content.Load<SpriteFont>(@"Fonts\Sub_titles");

            portaloff      = Content.Load<Texture2D>(@"Sprites/portaloff");
            portalleft     = Content.Load<Texture2D>(@"Sprites/portalleft");
            portalright    = Content.Load<Texture2D>(@"Sprites/portalright");
            portalup       = Content.Load<Texture2D>(@"Sprites/portalup");
            portaldown     = Content.Load<Texture2D>(@"Sprites/portaldown");
            portalhalf     = Content.Load<Texture2D>(@"Sprites/portalhalf");
            portaldouble = Content.Load<Texture2D>(@"Sprites/portaldouble");

		}

		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}

		protected override void Update(GameTime gameTime)
		{
			// Allows the game to exit
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
				this.Exit();

			KeyboardState curKey = Keyboard.GetState();
			if (gameState == GameState.START && curKey.IsKeyDown(Keys.Space)) { gameState = GameState.PLAY; countdown = LEVEL_COUNTDOWN; }
			else if (gameState == GameState.PLAY)
			{
				// Check for when to change to next level
				if (animals.Count == 0 || countdown <= 0)
				{
					curLevel ++;
					countdown = LEVEL_COUNTDOWN;
					if (curLevel <= MAX_LEVELS) loadLevel(curLevel);
					else gameState = GameState.WIN;
					return;
				}
				
				// Move automated objects
				foreach (Crawler e in animals) e.doWander(gameScreen);

                handlePlayerMovement(curKey, gameTime);
				handlePlayerCollisions();
				countdown -= gameTime.ElapsedGameTime.Milliseconds;
			}
            

            player.UpdateOldRec();
            oldKey = curKey;
			base.Update(gameTime);
		}
        private void handlePlayerMovement(KeyboardState curKey, GameTime gameTime)
        {
            // Move player
            float x = 0;
            float y = 0;

            if (curKey.IsKeyDown(Keys.Left)) x -= PLAYER_SPEED;
            if (curKey.IsKeyDown(Keys.Right)) x += PLAYER_SPEED;
            if (!player.isJumping && (curKey.IsKeyDown(Keys.Up) || curKey.IsKeyDown(Keys.Space)))
            {
                player.isJumping = true;
                player.JumpVel = PLAYER_JUMP;
                player.holdingUp = true;
            }
            if (player.isJumping)
            {
                //check if they've been holding up since the jump started
                if (!curKey.IsKeyDown(Keys.Up))
                    player.holdingUp = false;
                if (player.holdingUp)
                    player.JumpVel += HOLD_UP;

                if (player.pt2 == portalType2.NORMAL)
                    player.JumpVel -= GRAVITY * (float)gameTime.ElapsedGameTime.TotalSeconds;
                else if (player.pt2 == portalType2.HALF)
                    player.JumpVel -= (GRAVITY / 2) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                else if (player.pt2 == portalType2.DOUBLE)
                    player.JumpVel -= GRAVITY * 2 * (float)gameTime.ElapsedGameTime.TotalSeconds;
                y -= player.JumpVel * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            switch (player.pt1)
            {
                case portalType1.NORMAL:
                    player.X += x;
                    player.Y += y;
                    break;
                case portalType1.LEFTSIDE:
                    player.X -= y;
                    player.Y += x;
                    break;
                case portalType1.UPSIDE:
                    player.X -= x;
                    player.Y -= y;
                    break;
                case portalType1.RIGHTSIDE:
                    player.X += y;
                    player.Y -= x;
                    break;
                default:
                    break;
            }

            player.X = MathHelper.Clamp(player.X, 0, WIDTH - player.Width);

        }
		private void handlePlayerCollisions()
		{
			bool collided = false;
			// Collision with blocks
			foreach (Tile b in gameScreen)
			{
				if (b != null)
				{
					Rectangle intersect = player.Intersects(b);
					if (intersect.Width > 0 || intersect.Height > 0)
					{
						collided = true;
                        if (b.Type == GOType.UP)
                        {
                            if (intersect.Width > BLOCK_DIM - PORTAL_COLLISION && intersect.Height > BLOCK_DIM - PORTAL_COLLISION)
                                player.pt1 = portalType1.UPSIDE;
                        }
                        else if (b.Type == GOType.DOWN)
                        {
                            if (intersect.Width > BLOCK_DIM - PORTAL_COLLISION && intersect.Height > BLOCK_DIM - PORTAL_COLLISION)
                                player.pt1 = portalType1.NORMAL;
                        }
                        else if (b.Type == GOType.LEFT)
                        {
                            if (intersect.Width > BLOCK_DIM - PORTAL_COLLISION && intersect.Height > BLOCK_DIM - PORTAL_COLLISION)
                                player.pt1 = portalType1.LEFTSIDE;
                        }
                        else if (b.Type == GOType.RIGHT)
                        {
                            if (intersect.Width > BLOCK_DIM - PORTAL_COLLISION && intersect.Height > BLOCK_DIM - PORTAL_COLLISION)
                                player.pt1 = portalType1.RIGHTSIDE;
                        }
                        else if (b.Type == GOType.HALF)
                        {
                            if (intersect.Width > BLOCK_DIM - PORTAL_COLLISION && intersect.Height > BLOCK_DIM - PORTAL_COLLISION)
                                player.pt2 = portalType2.HALF;
                        }
                        else if (b.Type == GOType.DOUBLE)
                        {
                            if (intersect.Width > BLOCK_DIM - PORTAL_COLLISION && intersect.Height > BLOCK_DIM - PORTAL_COLLISION)
                                player.pt2 = portalType2.DOUBLE;
                        }
                        else
                        {

                            //Where portal types start deciding things
                            //////////////////////////////////////////////////////////////////
                            ///////////////////NORMAL/////////////////////////////////////////
                            //////////////////////////////////////////////////////////////////
                            if (player.pt1 == portalType1.NORMAL)
                            {
                                if (b.Type == GOType.BSOLID)
                                {

                                    if (intersect.Width <= PLAYER_SPEED)
                                    {
                                        if (player.X < b.X)
                                            player.X = b.X - player.Width;
                                        else
                                            player.X = b.X + b.Width;
                                    }
                                    else
                                    {
                                        // the intersection lies below the player
                                        if (player.Y < b.Y)//player.Y + player.Height > b.Y && 
                                        {
                                            if (player.isJumping) player.isJumping = false;
                                            player.Y = b.Y - player.Height;
                                        }
                                        // intersection is above player
                                        if (player.Y + player.Height > b.Y)//player.Y < b.Y + b.Height && 
                                        {
                                            // Reset the jump velocity
                                            player.JumpVel = 0;
                                            player.Y = b.Y + b.Height;
                                        }
                                    }
                                }
                                else // if plank
                                {
                                    if (intersect.Width <= PLAYER_SPEED)
                                    {

                                    }
                                    else
                                    {
                                        if (player.JumpVel <= 0)
                                        {// the intersection lies below the player
                                            if (player.Y < b.Y)//player.Y + player.Height > b.Y && 
                                            {
                                                if (player.oldRec.Y + player.Height <= b.Y)
                                                {
                                                    if (player.isJumping) player.isJumping = false;
                                                    player.Y = b.Y - player.Height;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            //////////////////////////////////////////////////////////////////
                            ///////////////////LEFTSIDE///////////////////////////////////////
                            //////////////////////////////////////////////////////////////////
                            // x = -y     y = x
                            else if (player.pt1 == portalType1.LEFTSIDE)
                            {
                                if (b.Type == GOType.BSOLID)
                                {

                                    if (intersect.Height <= PLAYER_SPEED)
                                    {
                                        if (player.Y < b.Y)
                                            player.Y = b.Y - player.Height;
                                        else
                                            player.Y = b.Y + b.Height;
                                    }
                                    else
                                    {

                                        // the intersection lies below the player
                                        if (player.X < b.X)//player.Y + player.Height > b.Y && 
                                        {

                                            player.JumpVel = 0;
                                            player.X = b.X - b.Width;
                                        }
                                        // intersection is above player
                                        if (player.X + player.Height > b.X)//player.Y < b.Y + b.Height && 
                                        {
                                            // Reset the jump velocity
                                            if (player.isJumping) player.isJumping = false;
                                            player.X = b.X + player.Width;
                                        }
                                    }
                                }
                                else // if plank
                                {
                                    if (intersect.Width <= PLAYER_SPEED)
                                    {

                                    }
                                    else
                                    {
                                        if (player.JumpVel <= 0)
                                        {// the intersection lies below the player
                                            if (player.Y < b.Y)//player.Y + player.Height > b.Y && 
                                            {
                                                if (player.oldRec.Y + player.Height <= b.Y)
                                                {
                                                    if (player.isJumping) player.isJumping = false;
                                                    player.Y = b.Y - player.Height;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            //////////////////////////////////////////////////////////////////
                            ///////////////////UPSIDE/////////////////////////////////////////
                            //////////////////////////////////////////////////////////////////
                            else if (player.pt1 == portalType1.UPSIDE)
                            {
                                if (b.Type == GOType.BSOLID)
                                {

                                    if (intersect.Width <= PLAYER_SPEED)
                                    {
                                        if (player.X < b.X)
                                            player.X = b.X - player.Width;
                                        else
                                            player.X = b.X + b.Width;
                                    }
                                    else
                                    {
                                        // the intersection lies below the player
                                        if (player.Y < b.Y)//player.Y + player.Height > b.Y && 
                                        {

                                            player.JumpVel = 0;
                                            player.Y = b.Y - player.Height;
                                        }
                                        // intersection is above player
                                        if (player.Y + player.Height > b.Y)//player.Y < b.Y + b.Height && 
                                        {
                                            // Reset the jump velocity
                                            if (player.isJumping) player.isJumping = false;
                                            player.Y = b.Y + b.Height;
                                        }
                                    }

                                }
                                else // if plank
                                {
                                    if (intersect.Width <= PLAYER_SPEED)
                                    {

                                    }
                                    else
                                    {
                                        if (player.JumpVel <= 0)
                                        {// the intersection lies below the player
                                            if (player.Y < b.Y)//player.Y + player.Height > b.Y && 
                                            {
                                                if (player.oldRec.Y + player.Height <= b.Y)
                                                {
                                                    if (player.isJumping) player.isJumping = false;
                                                    player.Y = b.Y - player.Height;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            //////////////////////////////////////////////////////////////////
                            ///////////////////RIGHTSIDE///////////////////////////////////////
                            //////////////////////////////////////////////////////////////////
                            else if (player.pt1 == portalType1.RIGHTSIDE)
                            {
                                if (b.Type == GOType.BSOLID)
                                {

                                    if (intersect.Height <= PLAYER_SPEED)
                                    {
                                        if (player.Y > b.Y)
                                            player.Y = b.Y - player.Height;
                                        else
                                            player.Y = b.Y + b.Height;
                                    }
                                    else
                                    {

                                        // the intersection lies below the player
                                        if (player.X > b.X)//player.Y + player.Height > b.Y && 
                                        {

                                            player.JumpVel = 0;
                                            player.X = b.X - b.Width;
                                        }
                                        // intersection is above player
                                        if (player.X + player.Height > b.X)//player.Y < b.Y + b.Height && 
                                        {
                                            // Reset the jump velocity
                                            if (player.isJumping) player.isJumping = false;
                                            player.X = b.X + player.Width;
                                        }
                                    }
                                }
                                else // if plank
                                {
                                    if (intersect.Width <= PLAYER_SPEED)
                                    {

                                    }
                                    else
                                    {
                                        if (player.JumpVel <= 0)
                                        {// the intersection lies below the player
                                            if (player.Y > b.Y)//player.Y + player.Height > b.Y && 
                                            {
                                                if (player.oldRec.Y + player.Height >= b.Y)
                                                {
                                                    if (player.isJumping) player.isJumping = false;
                                                    player.Y = b.Y - player.Height;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
					}
				}
			}
			if (!collided) player.isJumping = true;
            if (player.pt1 == portalType1.NORMAL)
            {
            }
            else
            {
                if (player.pt1 == portalType1.LEFTSIDE)
                {
                    if (player.X <= 0)
                    {
                        player.isJumping = false;
                    }
                }
                else if (player.pt1 == portalType1.UPSIDE)
                {
                    if (player.Y <= 0)
                    {
                        player.isJumping = false;
                        player.Y = 0;
                    }
                }
                else if (player.pt1 == portalType1.RIGHTSIDE)
                {
                    if (player.X == WIDTH - BLOCK_DIM)
                    {
                        player.isJumping = false;
                    }
                    if (player.Y < 0)
                    {
                        player.Y = 0;
                    }
                }
            }

			// Collision with pets
			foreach (Crawler p in animals)
			{
				Rectangle intersect = player.Intersects(p);
				if (intersect.Width > 0 || intersect.Height > 0)
				{
					score += 1;
					animals.Remove(p);
					break;
				}
			}
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			spriteBatch.Begin();

			if (gameState != GameState.PLAY)
			{
				switch (gameState)
				{
					case GameState.START:
						spriteBatch.DrawString(titleFont, "Save the Animals!", Vector2.Zero, Color.White);
						spriteBatch.DrawString(subTitleFont, "Press SPACE to start", new Vector2(0, HEIGHT / 2), Color.White);
						break;
					case GameState.LOSE:
						spriteBatch.DrawString(titleFont, "GAME OVER", Vector2.Zero, Color.White);
						break;
					case GameState.WIN:
						spriteBatch.DrawString(titleFont, "You win!", Vector2.Zero, Color.White);
						break;
					default:
						break;
				}
			}
			else
			{
				// Draw the game objects
				foreach (Crawler p in animals) spriteBatch.Draw(crawlerSprite, p.Location, Color.White);
				foreach (Tile b in gameScreen)
				{
					if (b != null)
					{
						if (b.Type == GOType.BSOLID) spriteBatch.Draw(blockSolid, b.Location, Color.White);
						else if (b.Type == GOType.BPLANK) spriteBatch.Draw(blockPlank, b.Location, Color.White);
					}
				}
				spriteBatch.Draw(playerSprite, player.Location, Color.White);
				// Score
				spriteBatch.DrawString(subTitleFont, "" + score, Vector2.Zero, Color.White);
				// Time left
				spriteBatch.DrawString(subTitleFont, "" + countdown / 1000, new Vector2(950, 0), Color.White);
			}

			spriteBatch.End();

			base.Draw(gameTime);
		}



		private void loadLevel(int level)
		{

            /* 1 = up portal
             * 2 = down portal
             * 3 = left portal
             * 4 = right portal
             * 5 = half gravity portal
             * 6 = double gravity portal
             * 0 = off portal
             * c = critter 1
             * v = critter 2
             * b = critter 3 ect.
             * p = player
             * x = platform
             * = = plank (think platform)
             */
			System.IO.Stream stream = TitleContainer.OpenStream("Content/Levels/"+level+".txt");
			System.IO.StreamReader sreader = new System.IO.StreamReader(stream);
			string line;
			int r = 0;
			gameScreen = new Tile[ROWS, COLS];
			animals = new List<Animal>();
			while ((line = sreader.ReadLine()) != null)
			{
				for (int c = 0; c < line.Length; c++)
				{
                    GOType temp = (GOType)line.ElementAt<char>(c);
					switch (temp)
					{

                        case GOType.EMPTY:
                            break;
						case GOType.BSOLID:
							gameScreen[r, c] = new Tile(GOType.BSOLID, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
							break;
						case GOType.BPLANK:
							gameScreen[r, c] = new Tile(GOType.BPLANK, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
							break;
						case GOType.PLAYER:
							player = new Player(new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
							break;
						case GOType.CRAWLER:
							animals.Add(new Crawler(new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM),CRAWLER_SPEED));
							break;
                        case GOType.UP:
                            gameScreen[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.DOWN:
                            gameScreen[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.LEFT:
                            gameScreen[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.RIGHT:
                            gameScreen[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.HALF:
                            gameScreen[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.DOUBLE:
                            gameScreen[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.OFF:
                            gameScreen[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
						default:
							break;
					}
				}
				r++;
			}
			sreader.Close();
		}
	}
}
