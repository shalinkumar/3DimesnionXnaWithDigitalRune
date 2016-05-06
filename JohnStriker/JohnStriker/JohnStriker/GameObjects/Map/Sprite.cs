using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace JohnStriker.GameObjects.Map
{
    public class Sprite
    {
        private SpriteBatch spriteBatch;
        private double starttime;
        public Texture2D image;
        public bool image_loaded;
        public Vector2 size;
        public int columns;
        public int frame, totalframes;
        public Vector2 position;
        public Vector2 velocity;
        public float rotation;
        public float scale;
        public Vector2 pivot;
        public bool visible;
        public bool alive;

        public Sprite(SpriteBatch sb)
        {
            spriteBatch = sb;
            image = null;
            image_loaded = false;
            size.X = size.Y = 0;
            columns = 1;
            frame = 0;
            totalframes = 1;
            position = Vector2.Zero;
            velocity = Vector2.Zero;
            starttime = 0;
            rotation = 0.0f;
            scale = 1.0f;
            pivot = new Vector2(0, 0);
            visible = true;
            alive = true;
        }

        public Rectangle getBounds()
        {
            return new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
        }

        /**
         * This should only be called from Load_Content
         **/
        public bool Load(ContentManager Content, string filename)
        {
            image = Content.Load<Texture2D>(filename);
            image_loaded = true;
            size.X = image.Width;
            size.Y = image.Height;
            return true;
        }

        public void Draw()
        {
            Rectangle src_rect = new Rectangle();
            src_rect.X = (frame % columns) * (int)size.X;
            src_rect.Y = (frame / columns) * (int)size.Y;
            src_rect.Width = (int)size.X;
            src_rect.Height = (int)size.Y;
            spriteBatch.Draw(image, position, src_rect, Color.White, rotation, pivot, scale, SpriteEffects.None, 0.0f);
        }

        public void Animate(double elapsedTime)
        {
            Animate(0, totalframes, elapsedTime, 30);
        }

        public void Animate(int startframe, int endframe, double elapsedTime, double animrate)
        {
            double time = starttime + elapsedTime;
            if (time > animrate)
            {
                starttime = time;
                if (++frame > endframe) frame = startframe;
            }
        }

    }
}
