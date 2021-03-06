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
 * Final project
 * 
 */

namespace FunnelCake
{

	public class Game1 : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;
        KeyboardState oldKey;
		const int LEVEL_COUNTDOWN = 25000; // Milliseconds
		int countdown;

		int curLevel;
		const int MAX_LEVELS = 5;

		// Screen size
		public const int HEIGHT = 750;
		public const int WIDTH = 1000;
		public const int ROWS = 15;
		public const int COLS = 20;
        public const int COLS2 = 5;
		public const int BLOCK_DIM = 50;   // Block dimension in pixels (width == height)
        public const int HALF_BLOCK_DIM = 25;
        public const int PORTAL_COLLISION = 8;
        public const int TRANSITION_PIXELS = 5;
        public const int TRANSITION_FRAMES = (WIDTH + (BLOCK_DIM * COLS2))/ TRANSITION_PIXELS;
        int curFrames;
        int animalsLeft;
        Vector3 cameraPosition = new Vector3(1000f, 50.0f, 10000f);

        enum GameState { START, PLAY, LOSE, WIN, PAUSE, TRANSITION };
		GameState gameState;
        //Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up);
        Matrix world = Matrix.Identity;
        Matrix rotations = Matrix.Identity;
        float ROTATION_SPEED = .1f;
        Matrix translation = Matrix.Identity;

		// Sprites
		List<Animal> animals;
        List<Animal> animals2; //Use for transitioning levels
		Player player;
		Tile[,] gameScreen;   // Array of tiles to display
        Tile[,] gameScreen2; // used to hold next level
        Tile[,] transScreen; // used to hold transition

		Texture2D blockSolid;
		Texture2D blockPlank;
		Texture2D crawlerSprite;
		Texture2D flyerSprite;
		Texture2D jumperSprite;
        Model theb;

        Texture2D portaloff, portalup, portaldown, portalleft, 
            portalright, portalhalf, portaldouble, portalnormal;
		Texture2D playerSprite;
        Texture2D winRight, winLeft;

		// Fonts
		SpriteFont titleFont;
		SpriteFont subTitleFont;

		float CRAWLER_SPEED = 3;
		float FLYER_SPEED = 3;

		public const float PLAYER_SPEED = 4;
		public const float PLAYER_JUMP = 330 / 0.5f; // jump height / time to reach height
        public const float HOLD_UP = 10;
		public const float GRAVITY = 350 / 0.25f;
		public const float OBJ_SPEED = 0.5f;

        int score;
        bool firstLevel, boss;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			graphics.PreferredBackBufferHeight = HEIGHT;
			graphics.PreferredBackBufferWidth = WIDTH;
			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			IsMouseVisible = true;
			gameState = GameState.START;
			gameScreen = new Tile[ROWS, COLS];
			animals = new List<Animal>();
			score = 0;
			curLevel = 1;
            oldKey = Keyboard.GetState();
            firstLevel = true;
            boss = false;
            curFrames = 0;
            RasterizerState rs = new RasterizerState();
            rs.CullMode = CullMode.CullCounterClockwiseFace;
            GraphicsDevice.RasterizerState = rs;
            world = Matrix.CreateRotationY(MathHelper.PiOver2);
			base.Initialize();
		}

		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);

			blockSolid		= Content.Load<Texture2D>(@"Sprites/block_solid");
			blockPlank = Content.Load<Texture2D>(@"Sprites/block_plank");
			crawlerSprite = Content.Load<Texture2D>(@"Sprites/pet");
			flyerSprite = Content.Load<Texture2D>(@"Sprites/pet");
			jumperSprite = Content.Load<Texture2D>(@"Sprites/pet");
			playerSprite	= Content.Load<Texture2D>(@"Sprites/player");
            winRight = Content.Load<Texture2D>(@"Sprites/winRight");
            winLeft = Content.Load<Texture2D>(@"Sprites/winLeft");

			titleFont = Content.Load<SpriteFont>(@"Fonts\Titles");
			subTitleFont = Content.Load<SpriteFont>(@"Fonts\Sub_titles");
            theb = Content.Load<Model>(@"Models/p1_wedge");
            portaloff      = Content.Load<Texture2D>(@"Sprites/portaloff");
            portalleft     = Content.Load<Texture2D>(@"Sprites/portalleft");
            portalright    = Content.Load<Texture2D>(@"Sprites/portalright");
            portalup       = Content.Load<Texture2D>(@"Sprites/portalup");
            portaldown     = Content.Load<Texture2D>(@"Sprites/portaldown");
            portalhalf     = Content.Load<Texture2D>(@"Sprites/portalhalf");
            portaldouble = Content.Load<Texture2D>(@"Sprites/portaldouble");
            portalnormal = Content.Load<Texture2D>(@"Sprites/portalneutral");



            loadLevel(curLevel);

		}
        private void handlePlayerMovement(KeyboardState curKey, GameTime gameTime)
        {
            // Move player
            float x = 0;
            float y = 0;

            if (curKey.IsKeyDown(Keys.Left)) x -= PLAYER_SPEED;
            if (curKey.IsKeyDown(Keys.Right)) x += PLAYER_SPEED;
            if (!player.isJumping && curKey.IsKeyDown(Keys.Up))
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
                    player.JumpVel -= (GRAVITY * (float)gameTime.ElapsedGameTime.TotalSeconds) + 4;
                else if (player.pt2 == portalType2.HALF)
                    player.JumpVel -= (GRAVITY / 2.4f) * (float)gameTime.ElapsedGameTime.TotalSeconds;
                else if (player.pt2 == portalType2.DOUBLE)
                    player.JumpVel -= (GRAVITY * 1.5f * (float)gameTime.ElapsedGameTime.TotalSeconds) + 5;
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

        private void catchAnimal()
        {// Collision with pets
			foreach (Animal p in animals)
			{
				Rectangle intersect = player.Intersect(p);
				if (intersect.Width > 0 || intersect.Height > 0)
				{
					score += 1;
					animals.Remove(p);
                    animalsLeft--;
					break;
				}
			}
        }
		private void handlePlatCollisions(Player player)
		{
			bool collided = false;
			// Collision with blocks
			foreach (Tile b in gameScreen)
			{
				if (b != null)
				{
					Rectangle intersect = player.Intersect(b);
					if (intersect.Width > 0 || intersect.Height > 0)
					{
                        if (b.Type == GOType.NORMAL)
                        {
                            if (intersect.Width > BLOCK_DIM - PORTAL_COLLISION && intersect.Height > BLOCK_DIM - PORTAL_COLLISION)
                                player.pt2 = portalType2.NORMAL;
                        }
                        else if (b.Type == GOType.UP)
                        {
                            if (intersect.Width > BLOCK_DIM - PORTAL_COLLISION && intersect.Height > BLOCK_DIM - PORTAL_COLLISION)
                            {
                                player.pt1 = portalType1.UPSIDE;
                            }
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
                        else if (player.Type == GOType.PLAYER && b.Type == GOType.WINR)
                        {

                            if (countdown <= 0 || animalsLeft == 0)
                            {
                                if (!boss)
                                {
                                    loadLevel(++curLevel);
                                    player.Y = b.Y;
                                }
                            }
                            
                        }
						else if (player.Type == GOType.PLAYER && b.Type == GOType.WINL)
                        {
                            
                            if (boss)
                            {
                                curLevel--;
                                
                                    loadLevel(curLevel);
                                    player.Y = b.Y;
                            }
                        }
                        else
                        {
                            collided = true;
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
                                        if (!(player.oldRec.Y + player.Height <= b.Y))
                                        {
                                            if (player.X < b.X)
                                            {
                                                player.X = b.X - player.Width;
                                            }
                                            else
                                            {
                                                player.X = b.X + b.Width;
                                            }
                                        }
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
                                        if (player.Y + player.Height > b.Y)
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
                                            if (player.Y < b.Y)
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
                                        if (!(player.oldRec.X >= b.X + b.Width))
                                        {
                                            if (player.Y < b.Y)
                                                player.Y = b.Y - player.Height;
                                            else
                                                player.Y = b.Y + b.Height;
                                        }
                                    }
                                    else
                                    {

                                        // the intersection lies below the player
                                        if (player.X < b.X)
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
                                        if (player.oldRec.Y + player.Height <= b.Y)
                                            if (player.Y < b.Y)
                                            {
                                                player.Y = b.Y - player.Height;
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
                                        if (!(player.oldRec.Y >= b.Y + b.Height))
                                        {
                                            if (player.X < b.X)
                                                player.X = b.X - player.Width;
                                            else
                                                player.X = b.X + b.Width;
                                        }
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
                                        if (player.oldRec.Y + player.Height <= b.Y)
                                            if (player.Y < b.Y)
                                            {
                                                player.Y = b.Y - player.Height;
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
                                        if (!(player.X + player.Width <= b.X))
                                        {
                                            if (player.Y < b.Y)
                                                player.Y = b.Y - player.Height;
                                            else
                                                player.Y = b.Y + b.Height;
                                        }
                                    }
                                    else
                                    {

                                        // the intersection lies below the player
                                        if (player.X < b.X)//player.Y + player.Height > b.Y && 
                                        {

                                            if (player.isJumping) player.isJumping = false;
                                            player.X = b.X - b.Width;
                                        }
                                        // intersection is above player
                                        if (player.X + player.Height > b.X)//player.Y < b.Y + b.Height && 
                                        {
                                            // Reset the jump velocity

                                            player.X = b.X + player.Width;

                                            player.JumpVel = 0;
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
                                        if (player.oldRec.Y + player.Height <= b.Y)
                                            if (player.Y < b.Y)
                                            {
                                                player.Y = b.Y - player.Height;
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
                if(curKey.IsKeyDown(Keys.P) && (!oldKey.IsKeyDown(Keys.P))){gameState = GameState.PAUSE;}

                if (boss)
                {
                    translation = translation * Matrix.CreateTranslation(-2, 0, 0);
                    if (curLevel == 4)
                        rotations *= Matrix.CreateRotationX(ROTATION_SPEED);
                    else if (curLevel == 3)
                        rotations *= Matrix.CreateRotationX(-ROTATION_SPEED);
                    else if (curLevel == 1)
                        rotations *= Matrix.CreateRotationZ(ROTATION_SPEED);
                }
                if (curLevel == MAX_LEVELS && countdown < 5000)
                {
                    boss = true;
                    translation = translation * Matrix.CreateTranslation(-100, 0, 0);
                }
				// Move automated objects
				Random rand = new Random();
				foreach (Animal e in animals)
				{
					if (e.Type == GOType.CRAWLER) e.doWander(gameScreen);
					else if (e.Type == GOType.FLYER)
					{
						e.doWander(gameScreen, animals, player);
					} 
					else if (e.Type == GOType.JUMPER)
					{
						e.doWander(gameScreen, animals, player, gameTime);
					}
					handlePlatCollisions(e);
				}

                handlePlayerMovement(curKey, gameTime);
				handlePlatCollisions(player);
                if(!boss)
                catchAnimal();
                if (countdown < 0)
                {
                    gameState = GameState.LOSE;
                }
                player.UpdateOldRec();
				countdown -= gameTime.ElapsedGameTime.Milliseconds;
			}
            else if (gameState == GameState.PAUSE)
            {
                if (curKey.IsKeyDown(Keys.P) && (!oldKey.IsKeyDown(Keys.P))) { gameState = GameState.PLAY; }
            }
            else if (gameState == GameState.TRANSITION)
            {
                if (curFrames <= 0)
                {
                    gameState = GameState.PLAY;
                    animalsLeft = animals.Count;
                    if (curLevel == MAX_LEVELS)
                        countdown = 10000;
                }
                else
                {
                    transitionLevel();
                }
            }

            if (boss)
            {
                countdown = 50000;
            }
            oldKey = curKey;
			base.Update(gameTime);
		}
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			spriteBatch.Begin();

			if (gameState != GameState.PLAY && gameState != GameState.TRANSITION)
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
                        boss = false;
						spriteBatch.DrawString(titleFont, "FIN.\n You scored " + score+"!", Vector2.Zero, Color.White);
						break;
                    case GameState.PAUSE:
                        spriteBatch.DrawString(titleFont, "PAUSED", Vector2.Zero, Color.White);
                        break;  
					default:
						break;
				}
			}
			else
			{
				// Draw the game objects
                if(!boss)
				foreach (Animal p in animals)
				{
                    float rotation = 0;
                    if (p.pt1 == portalType1.NORMAL)
                        rotation = 0;
                    else if (p.pt1 == portalType1.RIGHTSIDE)
                        rotation = MathHelper.PiOver2;
                    else if (p.pt1 == portalType1.UPSIDE)
                        rotation = MathHelper.Pi;
                    else if (p.pt1 == portalType1.LEFTSIDE)
                        rotation = MathHelper.Pi + MathHelper.PiOver2;
					
					//// TEST CODE
					//rotation = (float)Math.Atan2(p.velocity.X, -p.velocity.Y);
						
                    if (p.Type == GOType.CRAWLER) spriteBatch.Draw(crawlerSprite, new Vector2(p.Location.X + HALF_BLOCK_DIM, p.Location.Y + HALF_BLOCK_DIM),
                                                                    null, Color.White, rotation, new Vector2(HALF_BLOCK_DIM, HALF_BLOCK_DIM), 1, SpriteEffects.None, 0);
					else if (p.Type == GOType.FLYER) spriteBatch.Draw(flyerSprite, new Vector2(p.Location.X + HALF_BLOCK_DIM, p.Location.Y + HALF_BLOCK_DIM),
                                                                    null, Color.White, rotation, new Vector2(HALF_BLOCK_DIM, HALF_BLOCK_DIM), 1, SpriteEffects.None, 0);
					else if (p.Type == GOType.JUMPER) spriteBatch.Draw(jumperSprite, new Vector2(p.Location.X + HALF_BLOCK_DIM, p.Location.Y + HALF_BLOCK_DIM),
																	null, Color.White, rotation, new Vector2(HALF_BLOCK_DIM, HALF_BLOCK_DIM), 1, SpriteEffects.None, 0);
				}
				foreach (Tile b in gameScreen)
				{
					if (b != null)                                                                   
					{                                                                                
						if (b.Type == GOType.BSOLID)        spriteBatch.Draw(blockSolid, b.Location, Color.White);
						else if (b.Type == GOType.BPLANK)   spriteBatch.Draw(blockPlank, b.Location, Color.White);
                        else if (b.Type == GOType.UP)       spriteBatch.Draw(portalup,   b.Location, Color.White);
                        else if (b.Type == GOType.DOWN)     spriteBatch.Draw(portaldown, b.Location, Color.White);
                        else if (b.Type == GOType.LEFT)     spriteBatch.Draw(portalleft, b.Location, Color.White);
                        else if (b.Type == GOType.RIGHT)    spriteBatch.Draw(portalright,b.Location, Color.White);
                        else if (b.Type == GOType.OFF)      spriteBatch.Draw(portaloff,  b.Location, Color.White);
                        else if (b.Type == GOType.HALF)     spriteBatch.Draw(portalhalf, b.Location, Color.White);
                        else if (b.Type == GOType.DOUBLE)   spriteBatch.Draw(portaldouble, b.Location, Color.White);
                        else if (b.Type == GOType.NORMAL)   spriteBatch.Draw(portalnormal, b.Location, Color.White);
                        else if (b.Type == GOType.WINR)     spriteBatch.Draw(winRight, b.Location, Color.White);
                        else if (b.Type == GOType.WINL)     if(boss)spriteBatch.Draw(winLeft, b.Location, Color.White);
					}
				}
                if (gameState == GameState.TRANSITION)
                {
                    foreach (Tile b in transScreen)
                    {
                        if (b != null)
                        {
                            if (b.Type == GOType.BSOLID) spriteBatch.Draw(blockSolid, b.Location, Color.White);
                            else if (b.Type == GOType.BPLANK) spriteBatch.Draw(blockPlank, b.Location, Color.White);
                        }
                    }
                    foreach (Tile b in gameScreen2)
                    if (b != null)
                    {
                        if (b.Type == GOType.BSOLID) spriteBatch.Draw(blockSolid, b.Location, Color.White);
                        else if (b.Type == GOType.BPLANK) spriteBatch.Draw(blockPlank, b.Location, Color.White);
                        else if (b.Type == GOType.UP) spriteBatch.Draw(portalup, b.Location, Color.White);
                        else if (b.Type == GOType.DOWN) spriteBatch.Draw(portaldown, b.Location, Color.White);
                        else if (b.Type == GOType.LEFT) spriteBatch.Draw(portalleft, b.Location, Color.White);
                        else if (b.Type == GOType.RIGHT) spriteBatch.Draw(portalright, b.Location, Color.White);
                        else if (b.Type == GOType.OFF) spriteBatch.Draw(portaloff, b.Location, Color.White);
                        else if (b.Type == GOType.HALF) spriteBatch.Draw(portalhalf, b.Location, Color.White);
                        else if (b.Type == GOType.DOUBLE) spriteBatch.Draw(portaldouble, b.Location, Color.White);
                        else if (b.Type == GOType.NORMAL) spriteBatch.Draw(portalnormal, b.Location, Color.White);
                        else if (b.Type == GOType.WINR) spriteBatch.Draw(winRight, b.Location, Color.White);
                        else if (b.Type == GOType.WINL) if (boss) spriteBatch.Draw(winLeft, b.Location, Color.White);
                    }
                    if(!boss)
                    foreach (Animal p in animals2)
                    {
                        float rotation = 0;
                        if (p.pt1 == portalType1.NORMAL)
                            rotation = 0;
                        else if (p.pt1 == portalType1.RIGHTSIDE)
                            rotation = MathHelper.PiOver2;
                        else if (p.pt1 == portalType1.UPSIDE)
                            rotation = MathHelper.Pi;
                        else if (p.pt1 == portalType1.LEFTSIDE)
                            rotation = MathHelper.Pi + MathHelper.PiOver2;
                        if (p.Type == GOType.CRAWLER) spriteBatch.Draw(crawlerSprite, new Vector2(p.Location.X + HALF_BLOCK_DIM, p.Location.Y + HALF_BLOCK_DIM),
                                                                        null, Color.White, rotation, new Vector2(HALF_BLOCK_DIM, HALF_BLOCK_DIM), 1, SpriteEffects.None, 0);
						else if (p.Type == GOType.FLYER) spriteBatch.Draw(flyerSprite, new Vector2(p.Location.X + HALF_BLOCK_DIM, p.Location.Y + HALF_BLOCK_DIM),
																		null, Color.White, 0, new Vector2(HALF_BLOCK_DIM, HALF_BLOCK_DIM), 1, SpriteEffects.None, 0);
						else if (p.Type == GOType.JUMPER) spriteBatch.Draw(jumperSprite, new Vector2(p.Location.X + HALF_BLOCK_DIM, p.Location.Y + HALF_BLOCK_DIM),
                                                                        null, Color.White, rotation, new Vector2(HALF_BLOCK_DIM, HALF_BLOCK_DIM), 1, SpriteEffects.None, 0);
                }
                }
                float rotationp = 0;
                if (player.pt1 == portalType1.NORMAL)
                    rotationp = 0;
                else if (player.pt1 == portalType1.LEFTSIDE)
                    rotationp = MathHelper.PiOver2;
                else if (player.pt1 == portalType1.UPSIDE)
                    rotationp = MathHelper.Pi;
                else if (player.pt1 == portalType1.RIGHTSIDE)
                    rotationp = MathHelper.Pi + MathHelper.PiOver2;
                spriteBatch.Draw(playerSprite, new Vector2 (player.Location.X + player.Width/2, player.Location.Y + player.Height/2),
                                null, Color.White, rotationp, new Vector2(player.Width/2, player.Height/2), 1, SpriteEffects.None, 0);

                
			}
            if (!boss)
            {
                // Score
                spriteBatch.DrawString(subTitleFont, "" + score, Vector2.Zero, Color.White);
                // Time left
                spriteBatch.DrawString(subTitleFont, "" + countdown / 1000, new Vector2(950, 0), Color.White);
            }
			spriteBatch.End();

            
            if(boss)
            {
                foreach (ModelMesh mesh in theb.Meshes)
                {

                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.EnableDefaultLighting();
                        effect.World = world*rotations * translation;
                        effect.View = Matrix.CreateLookAt(cameraPosition, Vector3.Zero, Vector3.Up);
                        effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f),
                            .75f, 1.0f, 50000.0f);
                    }
                    mesh.Draw();
                }
            }

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
         * 7 = normal portal
         * 0 = off portal
         * c = crawler
         * f = flyer
         * j = jumper
         * p = player
         * x = platform
         * = = plank (thin platform)
         * ! = win left
         * @ = win right
         */
            //Reset countdown
            countdown = LEVEL_COUNTDOWN;
            System.IO.Stream stream;
            //Load the next level
            if (curLevel != 0)
            {
                if (level == -1)
                {
                    gameState = GameState.WIN;
                    return;
                }
                stream = TitleContainer.OpenStream("Content/Levels/" + level + ".txt");
            }
            else 
                stream = TitleContainer.OpenStream("Content/Levels/" +1+ ".txt");

			System.IO.StreamReader sreader = new System.IO.StreamReader(stream);
			string line;
			int r = 0;
			gameScreen2 = new Tile[ROWS, COLS];
			animals2 = new List<Animal>();
			while ((line = sreader.ReadLine()) != null)
			{
				for (int c = 0; c < line.Length; c++)
				{
                    GOType temp = (GOType)line.ElementAt<char>(c);
					switch (temp)
					{

                        case GOType.EMPTY:
                            break;
                        case GOType.WINL:
                            gameScreen2[r, c] = new Tile(GOType.WINL, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.WINR:
                            gameScreen2[r, c] = new Tile(GOType.WINR, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
						case GOType.BSOLID:
							gameScreen2[r, c] = new Tile(GOType.BSOLID, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
							break;
						case GOType.BPLANK:
							gameScreen2[r, c] = new Tile(GOType.BPLANK, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
							break;
						case GOType.PLAYER:
                            if(!boss)
							player = new Player(new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM), PLAYER_SPEED);
							break;
						case GOType.CRAWLER:
							animals2.Add(new Crawler(new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM), CRAWLER_SPEED));
							break;
						case GOType.FLYER:
							animals2.Add(new Flyer(new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM), FLYER_SPEED));
							break;
						case GOType.JUMPER:
							animals2.Add(new Jumper(new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM), CRAWLER_SPEED));
							break;
                        case GOType.UP:
                            gameScreen2[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.DOWN:
                            gameScreen2[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.LEFT:
                            gameScreen2[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.RIGHT:
                            gameScreen2[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.HALF:
                            gameScreen2[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.DOUBLE:
                            gameScreen2[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.OFF:
                            gameScreen2[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
                        case GOType.NORMAL:
                            gameScreen2[r, c] = new Tile(temp, new Rectangle(c * BLOCK_DIM, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                            break;
						default:
							break;
					}
				}
				r++;
			}
			sreader.Close();

            //Decide whether to pan the screen or just display it (if it's the firt level displayed)
            if (firstLevel)
            {
                gameScreen = gameScreen2;
                animals = animals2;
                firstLevel = false;
	            animalsLeft = animals.Count;
            }
            else if (!(boss && level == 1))
            {
                
                //Load Transition Level
                
                if(!boss)
                stream = TitleContainer.OpenStream("Content/Levels/"+(level-1)+"x.txt");
                else
                    if(curLevel != 0)
                        stream = TitleContainer.OpenStream("Content/Levels/" + (level) + "x.txt");
                    else
                        stream = TitleContainer.OpenStream("Content/Levels/" + 1 + "x.txt");
			    sreader = new System.IO.StreamReader(stream);
			    r = 0;
                int w = WIDTH;
                if (boss)
                    w = -(COLS2 * BLOCK_DIM);
                
			    transScreen = new Tile[ROWS, COLS2];
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
                                transScreen[r, c] = new Tile(GOType.BSOLID, new Rectangle(c *BLOCK_DIM + w, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                                break;
                            case GOType.BPLANK:
                                transScreen[r, c] = new Tile(GOType.BPLANK, new Rectangle(c *BLOCK_DIM + w, r * BLOCK_DIM, BLOCK_DIM, BLOCK_DIM));
                                break;
                            default:
                                break;
                        }
                    }
                    r++;
                }

                if (boss)
                    w = WIDTH * -1;
                //Transition
                foreach (Tile b in gameScreen2)
                {
                    if(b != null)
                        if(!boss)
                            b.X += w + (BLOCK_DIM * COLS2);
                        else
                            b.X += w - (BLOCK_DIM * COLS2);
                }
                foreach (Animal b in animals2)
                {
                    if (b != null)
                        if (!boss)
                            b.X += w + (BLOCK_DIM * COLS2);
                        else
                            b.X += w - (BLOCK_DIM * COLS2);
                }
                gameState = GameState.TRANSITION;
                curFrames = TRANSITION_FRAMES;
            }
		}
        private void transitionLevel()
        {
            int t = TRANSITION_PIXELS;
            if(boss)
            {
                t = -t;
            }
            foreach (Tile b in gameScreen)
            {
                if (b != null)
                    b.X -= t;
            }
            foreach (Animal b in animals)
            {
                if (b != null)
                    b.X -= t;
            }
            foreach (Animal b in animals2)
            {
                if (b != null)
                    b.X -= t;
            }
            foreach (Tile b in transScreen)
            {
                if (b != null)
                    b.X -= t;
            }
            foreach (Tile b in gameScreen2)
            {
                if (b != null)
                    b.X -= t;
            }
            if(curFrames <190) 
            player.X -= t;
            curFrames--;
            if (curFrames <= 0)
            {

                gameScreen = gameScreen2;
                animals = animals2;
            }
            if (boss)
                if (curLevel == 4)
                    translation = translation * Matrix.CreateTranslation(18, 0, 0);
                else
                {
                    translation = translation * Matrix.CreateTranslation(7, 0, 0);
                }

        }
	}
}
