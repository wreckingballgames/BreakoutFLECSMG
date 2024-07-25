using System;
using System.Reflection.Metadata;
using Flecs.NET.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BreakoutFLECSMG;

public class Game1 : Game
{
    // Properties

    // MonoGame-essential
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // Content
    private Texture2D Background {get; set;}
    private Texture2D BrickSprite {get; set;}
    private Texture2D BallSprite {get; set;}
    private Texture2D PaddleSprite {get; set;}

    // Game Data
    private readonly int _windowWidth = 640;
    private readonly int _windowHeight = 480;
    private Rectangle WindowRect {get; set;}

    private readonly int _maxLives = 3;
    private int CurrentLives {get; set;}

    private readonly int _brickWidth = 64;
    private readonly int _brickHeight = 48;
    private readonly int _totalBricks = 50;
    private int BricksRemaining {get; set;}

    private readonly int _paddleWidth = 128;
    private readonly int _paddleHeight = 48;
    private readonly int _paddleSpeed = 100;
    private readonly Color _paddleColor = Color.Blue;

    private readonly int _ballWidth = 32;
    private readonly int _ballHeight = 32;
    private readonly int _ballSpeed = 100;
    private readonly Color _ballColor = Color.MonoGameOrange;

    // ECS
    // World
    private World world = World.Create();

    // Entities
    private Entity _brick; // Prefab

    private Entity Paddle {get; set;}

    private Entity Ball {get; set;}

    // Components
    private record struct Position(float X, float Y);

    private record struct Size(float Width, float Height);

    private record struct Speed(float Value);

    private record struct Direction(float X, float Y);

    private record struct Score(int Value);

    private record struct Board(float Width, float Height);

    // Tags
    private struct PaddleTag;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = _windowWidth;
        _graphics.PreferredBackBufferHeight = _windowHeight;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Game Data
        WindowRect = new Rectangle(new Point(0, 0), new Point(_windowWidth, _windowHeight));
        BricksRemaining = _totalBricks;
        CurrentLives = _maxLives;

        // ECS
        // Singletons
        world.Set(new Score(0))
                .Set(new Board(_windowWidth, _windowHeight));

        // Prefabs
        _brick = world.Prefab("Brick")
                .Set(new Position(0, 0))
                .Set(new Size(_brickWidth, _brickHeight))
                .Set(Color.White);

        // Entities
        Paddle = world.Entity("Paddle")
                .Set(new Position(320, 400))
                .Set(new Size(_paddleWidth, _paddleHeight))
                .Set(new Speed(_paddleSpeed))
                .Set(new Direction(0, 0))
                .Add<PaddleTag>();
        
        Ball = world.Entity("Ball")
                .Set(new Position(320, 360))
                .Set(new Size(_ballWidth, _ballHeight))
                .Set(new Speed(_ballSpeed))
                .Set(new Direction(0, 0));

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        Background = Content.Load<Texture2D>("Images/background");
        BrickSprite = Content.Load<Texture2D>("Images/brick");
        BallSprite = Content.Load<Texture2D>("Images/ball");
        PaddleSprite = Content.Load<Texture2D>("Images/paddle");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        _spriteBatch.Draw(Background, WindowRect, Color.White);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // Use the Brick prefab, total bricks variable, and the size of the board to fill the board with brick entities.
    private void CreateBricks()
    {
        // Example of how to make an entity instance from a prefab
        Entity brick = world.Entity("name").IsA(_brick);
    }
}
