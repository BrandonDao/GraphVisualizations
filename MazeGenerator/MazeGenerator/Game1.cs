using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MazeGenerator
{
    public class Game1 : Game
    {
        readonly GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Texture2D blankTexture;
        Texture2D zoomInTexture;
        Texture2D zoomOutTexture;
        SpriteFont font;

        MouseState prevMouseState;
        TileGraph tileGraph;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            IsMouseVisible = true;

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            zoomInTexture = Content.Load<Texture2D>("plus");
            zoomOutTexture = Content.Load<Texture2D>("minus");
            blankTexture = new Texture2D(GraphicsDevice, 1, 1);
            blankTexture.SetData(new Color[]
            {
                Color.White
            });
            font = Content.Load<SpriteFont>("font");

            tileGraph = new TileGraph(GraphicsDevice, blankTexture, blankTexture, zoomInTexture, zoomOutTexture, font);

            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            var mouseState = Mouse.GetState();

            tileGraph.Update(mouseState, prevMouseState, Keyboard.GetState(), GraphicsDevice.Viewport);

            prevMouseState = mouseState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            tileGraph.Draw(GraphicsDevice.Viewport, spriteBatch, Mouse.GetState().Position);

            base.Draw(gameTime);
        }
    }
}
