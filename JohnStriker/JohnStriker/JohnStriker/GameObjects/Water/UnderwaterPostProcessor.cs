using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DigitalRune.Graphics;
using DigitalRune.Graphics.PostProcessing;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace JohnStriker.GameObjects.Water
{
    public class UnderwaterPostProcessor : EffectPostProcessor
    {
        public UnderwaterPostProcessor(IGraphicsService graphicsService, ContentManager content)
            : base(graphicsService, content.Load<Effect>("Water/UnderWater"))
        {

        }
    }
}
