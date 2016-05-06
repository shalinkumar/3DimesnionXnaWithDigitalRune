using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace JohnStriker.GameScreen
{
    public interface IGameScreen
    {
        /// <summary>
        /// Run game screen. Called each frame. Returns true if we want to exit it.
        /// </summary>
        bool Render();

        /// <summary>
        /// Process logic for this screen. Note that this method is called before
        /// the draw (or render) method in XNA.
        /// </summary>
        void Update(GameTime gameTime);
    }
}
