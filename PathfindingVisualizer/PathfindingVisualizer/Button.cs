using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace PathfindingVisualizer
{
    public class Button
    {
        public Vector2 Position;
        public int Width;
        public int Height;
        public Color BackgroundColor;

        public Texture2D Texture;

        public SpriteFont Font;
        public Color FontColor;
        public string Text;

        public Action Action;
        public bool IsHovering;
        public bool Clicked;

        public Rectangle Hitbox
        {
            get
            {
                return new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
            }
        }

        public Button(Texture2D texture, SpriteFont font)
        {
            Texture = texture;
            Font = font;
        }
        public Button(Texture2D texture, SpriteFont font, int width, int height, Action action, string text, Nullable<Color> fontColor, Nullable<Color> backgroundColor)
        {
            Texture = texture;
            Font = font;
            Width = width;
            Height = height;
            Action = action;
            Text = text;
            FontColor = fontColor == null ? Color.Black : (Color)fontColor;
            BackgroundColor = backgroundColor == null ? Color.White : (Color)backgroundColor;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Hitbox, IsHovering ? Color.Lerp(BackgroundColor, Color.Black, 0.25f) : BackgroundColor);

            if (string.IsNullOrEmpty(Text)) return;

            var pos = new Vector2(
                Hitbox.X + Hitbox.Width * 1.25f,
                Hitbox.Y + Hitbox.Height / 2 - Font.MeasureString(Text).Y / 2);

            spriteBatch.DrawString(Font, Text, pos, FontColor);
        }

        public void Update(MouseState mouseState, MouseState prevMouseState)
        {
            var mouseHitbox = new Rectangle(mouseState.X, mouseState.Y, 1, 1);
            IsHovering = false;

            if (Hitbox.Intersects(mouseHitbox))
            {
                IsHovering = true;

                if (mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed && Action != null)
                {
                    Action.Invoke();
                }
            }
        }
    }
}
