using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JohnStriker.GraphicsScreen
{
    public enum DeferredGraphicsDebugMode
    {
        /// <summary>
        /// Normal rendering.
        /// </summary>
        None,

        /// <summary>
        /// Render the diffuse light buffer instead of the shaded materials.
        /// </summary>
        VisualizeDiffuseLightBuffer,

        /// <summary>
        /// Render the specular light buffer instead of the shaded materials.
        /// </summary>
        VisualizeSpecularLightBuffer,
    };
}
