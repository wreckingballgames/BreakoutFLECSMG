using System;
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
    private readonly int _brickScoreValue = 1;

    private readonly int _paddleWidth = 128;
    private readonly int _paddleHeight = 48;
    private readonly int _paddleSpeed = 150;
    private readonly Color _paddleColor = Color.Blue;

    private readonly int _ballWidth = 32;
    private readonly int _ballHeight = 32;
    private readonly int _ballSpeed = 200;
    private readonly Color _ballColor = Color.MonoGameOrange;

    // ECS
    // World
    private World world = World.Create();

    // Entities
    private Entity _brick; // Prefab
    private Entity[] Bricks {get; set;}
    private Entity Paddle {get; set;}
    private Entity Ball {get; set;}

    // Components
    private record struct Position(float X, float Y);
    private record struct Size(float Width, float Height);
    private record struct Speed(float Value);
    private record struct Direction(float X, float Y);
    private record struct Score(int Value);
    private record struct Board(float Width, float Height);
    private record struct Sprite(Texture2D Texture);
    private record struct DeltaTime(double Value);

    // Tags
    private struct PaddleTag;
    private struct BrickTag;

    // Systems
    private Routine BrickCollision {get; set;}
    private Routine BallMovement {get; set;}
    private Routine PaddleMovement {get; set;}
    private Routine PaddleCollision {get; set;}
    private Routine DrawEntities {get; set;}
    private Routine AlignRectangles {get; set;}

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
                .Set(new Board(_windowWidth, _windowHeight))
                .Set(new DeltaTime(0.0f));

        // Prefabs
        _brick = world.Prefab("Brick")
                .Set(new Position(0, 0))
                .Set(new Size(_brickWidth, _brickHeight))
                .Set(Color.White)
                .Set(new Rectangle(0, 0, _brickWidth, _brickHeight))
                .Add<BrickTag>();

        // Entities
        Paddle = world.Entity("Paddle")
                .Set(new Position(320, 400))
                .Set(new Size(_paddleWidth, _paddleHeight))
                .Set(new Speed(_paddleSpeed))
                .Set(new Direction(0, 0))
                .Set(new Rectangle(320, 400, _paddleWidth, _paddleHeight))
                .Add<PaddleTag>();
        
        Ball = world.Entity("Ball")
                .Set(new Position(300, 320))
                .Set(new Size(_ballWidth, _ballHeight))
                .Set(new Speed(_ballSpeed))
                .Set(new Direction(1, 1))
                .Set(new Rectangle(300, 320, _ballWidth, _ballHeight));

        // Systems
        BrickCollision = world.Routine<Position, Rectangle, BrickTag>()
                .Each((Entity entity, ref Position position, ref Rectangle rectangle) =>
                {
                    // Check for collision between bricks and ball
                    if (rectangle.Intersects(Ball.Get<Rectangle>()))
                    {
                        entity.Disable();
                        Direction ballDirection = Ball.Get<Direction>();

                        Direction newBallDirection = new();
                        newBallDirection.X = ballDirection.X * -1;
                        newBallDirection.Y = ballDirection.Y * -1;

                        world.Set(new Score(world.Get<Score>().Value + _brickScoreValue));
                        Ball.Set(newBallDirection);
                    }
                }
                );

        BallMovement = world.Routine<Position, Speed, Direction>()
                .Each((Entity entity, ref Position position, ref Speed speed, ref Direction direction) =>
                {
                    if (entity.Has<PaddleTag>())
                    {
                        return;
                    }

                    float deltaTime = (float)world.Get<DeltaTime>().Value;
                    Board board = world.Get<Board>();
                    
                    // Movement
                    position.X += direction.X * speed.Value * deltaTime;
                    position.Y += direction.Y * speed.Value * deltaTime;

                    // Bounds checking
                    if (position.X < 0 || position.X + _ballWidth > board.Width)
                    {
                        direction.X *= -1;
                    }

                    if (position.Y < 0)
                    {
                        direction.Y *= -1;
                    }
                    else if (position.Y + _ballHeight > board.Height)
                    {
                        // Handle losing a life and resetting the ball
                    }
                }
                );

        PaddleMovement = world.Routine<Position, Speed, Direction, PaddleTag>()
                .Each((ref Position position, ref Speed speed, ref Direction direction) =>
                {
                    position.X += speed.Value * direction.X * (float)world.Get<DeltaTime>().Value;
                }
                );

        PaddleCollision = world.Routine<Rectangle, Direction, PaddleTag>()
                .Each((ref Rectangle rectangle, ref Direction direction) =>
                {
                    if (rectangle.Intersects(Ball.Get<Rectangle>()))
                    {
                        Direction ballDirection = Ball.Get<Direction>();

                        Direction newBallDirection = new();
                        newBallDirection.X = direction.X;
                        newBallDirection.Y = ballDirection.Y * -1;

                        Ball.Set(newBallDirection);
                    }
                }
                );

        DrawEntities = world.Routine<Position, Sprite>()
                .Each((Entity entity, ref Position position, ref Sprite sprite) =>
                {
                    if (entity.Name() == "Paddle")
                    {
                        _spriteBatch.Draw(sprite.Texture, new Vector2(position.X, position.Y), _paddleColor);
                    }
                    else if (entity.Name() == "Ball")
                    {
                        _spriteBatch.Draw(sprite.Texture, new Vector2(position.X, position.Y), _ballColor);
                    }
                    else
                    {
                        _spriteBatch.Draw(sprite.Texture, new Vector2(position.X, position.Y), entity.Get<Color>());
                    }
                }
                );

        AlignRectangles = world.Routine<Position, Rectangle>()
                .Each((ref Position position, ref Rectangle rectangle) =>
                {
                    rectangle.X = (int)position.X;
                    rectangle.Y = (int)position.Y;
                }
                );

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        Background = Content.Load<Texture2D>("Images/background");
        BrickSprite = Content.Load<Texture2D>("Images/brick");
        PaddleSprite = Content.Load<Texture2D>("Images/paddle");
        BallSprite = Content.Load<Texture2D>("Images/ball");

        // Earliest time to set entity sprites
        _brick.Set(new Sprite(BrickSprite));
        Paddle.Set(new Sprite(PaddleSprite));
        Ball.Set(new Sprite(BallSprite));

        // Earliest time to spawn bricks
        SpawnBricks();
    }

    protected override void Update(GameTime gameTime)
    {
        // Store delta time for Routines to use
        world.Set(new DeltaTime(gameTime.ElapsedGameTime.TotalSeconds));

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        HandleInput();

        PaddleMovement.Run();
        BallMovement.Run();
        AlignRectangles.Run();
        PaddleCollision.Run();
        BrickCollision.Run();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        _spriteBatch.Draw(Background, WindowRect, Color.White);
        DrawEntities.Run();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void HandleInput()
    {
        if (Keyboard.GetState().IsKeyDown(Keys.A))
        {
            Paddle.Set(new Direction(-1, 0));
        }
        else if (Keyboard.GetState().IsKeyDown(Keys.D))
        {
            Paddle.Set(new Direction(1, 0));
        }
        else
        {
            Paddle.Set(new Direction(0, 0));
        }
    }

    private void SpawnBricks()
    {
        int columnOffset = 0;
        int rowOffset = 0;
        // Instantiate bricks, position starting at the origin
        // After each brick, move over by _brickWidth until hitting the end of the row
        // Then reset X position and add _brickHeight * number of rows completed to Y position; repeat
        for (int i = 0;i < _totalBricks;i++)
        {
            world.Entity($"Brick{i}").IsA(_brick)
                    .Set(new Position(columnOffset * _brickWidth, rowOffset * _brickHeight));

            columnOffset++;

            if (columnOffset == 10)
            {
                columnOffset = 0;
            }

            if (i != 0 && i % 10 == 0)
            {
                rowOffset++;
            }
        }
    }
}
