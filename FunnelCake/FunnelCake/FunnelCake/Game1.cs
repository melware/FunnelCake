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

		// Fonts
		SpriteFont titleFont;
		SpriteFont subTitleFont;

		Vector2 CRAWLER_SPEED = new Vector2(2,0);

		const float PLAYER_SPEED = 4;
		const float PLAYER_JUMP = 350 / 0.5f; // jump height / time to reach height
		const float GRAVITY = 350 / 0.25f;
		const float OBJ_SPEED = 0.5f;

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
				// Move player
				if (curKey.IsKeyDown(Keys.Left)) player.X -= PLAYER_SPEED;
				if (curKey.IsKeyDown(Keys.Right)) player.X += PLAYER_SPEED;
				if (!player.isJumping && (curKey.IsKeyDown(Keys.Up) || curKey.IsKeyDown(Keys.Space)))
				{
					player.isJumping = true;
					player.JumpVel = PLAYER_JUMP;
				}
				if (player.isJumping)
				{
					player.JumpVel -= GRAVITY * (float)gameTime.ElapsedGameTime.TotalSeconds;
					player.Y -= player.JumpVel * (float)gameTime.ElapsedGameTime.TotalSeconds;
				}
				player.X = MathHelper.Clamp(player.X, 0, WIDTH - player.Width);
				// Move automated objects
				foreach (Crawler e in animals) e.doWander(gameScreen);

				handlePlayerCollisions();
				countdown -= gameTime.ElapsedGameTime.Milliseconds;
			}
			base.Update(gameTime);
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
						if (b.Type == GOType.BSOLID)
						{
							// the intersection lies below the player
							if (player.Y + player.Height > b.Y && player.Y < b.Y)
							{
								if (player.isJumping) player.isJumping = false;
								player.Y = b.Y - player.Height;
							}
							// intersection is above player
							if (player.Y < b.Y + b.Height && player.Y + player.Height > b.Y)
							{
								// Reset the jump velocity
								player.JumpVel = 0;
								player.Y = b.Y + b.Height;
							}
						}
						else // if plank
						{
							if (player.JumpVel <= 0)
							{// the intersection lies below the player
								if (player.Y + player.Height > b.Y && player.Y < b.Y)
								{
									if (player.isJumping) player.isJumping = false;
									player.Y = b.Y - player.Height;
								}

							}
						}
						// Check for side collision
						if (intersect.Height > intersect.Width)
						{
							if (player.X + player.Width > b.X && player.X < b.X)
							{
								player.X = b.X - player.Width;
							}
							if (player.X + player.Width > b.X + b.Width && player.X < b.X + b.Width)
							{
								player.X = b.X + b.Height;
							}
						}
					}
				}
			}
			if (!collided) player.isJumping = true;

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
					switch ((GOType)line.ElementAt<char>(c))
					{
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
						case GOType.EMPTY:
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
