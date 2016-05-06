using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;

namespace JohnStriker.GraphicsScreen
{
    /// <summary>
    /// Collects all scene nodes which need some work done before the actual scene is rendered:
    /// CloudLayerNodes, WaterNodes, SceneCaptureNodes, PlanarReflectionNodes
    /// </summary>
    public class PreprocessingSceneQuery : ISceneQuery
    {
        public SceneNode ReferenceNode { get; private set; }
        public List<SceneNode> CloudLayerNodes { get; private set; }
        public List<SceneNode> WaterNodes { get; private set; }
        public List<SceneNode> SceneCaptureNodes { get; private set; }
        public List<SceneNode> PlanarReflectionNodes { get; private set; }


        public PreprocessingSceneQuery()
        {
            CloudLayerNodes = new List<SceneNode>();
            WaterNodes = new List<SceneNode>();
            SceneCaptureNodes = new List<SceneNode>();
            PlanarReflectionNodes = new List<SceneNode>();
        }


        public void Reset()
        {
            ReferenceNode = null;
            CloudLayerNodes.Clear();
            WaterNodes.Clear();
            SceneCaptureNodes.Clear();
            PlanarReflectionNodes.Clear();
        }


        public void Set(SceneNode referenceNode, IList<SceneNode> nodes, RenderContext context)
        {
            Reset();
            ReferenceNode = referenceNode;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] is CloudLayerNode)
                    CloudLayerNodes.Add(nodes[i]);
                else if (nodes[i] is WaterNode)
                    WaterNodes.Add(nodes[i]);
                else if (nodes[i] is SceneCaptureNode)
                    SceneCaptureNodes.Add(nodes[i]);
                else if (nodes[i] is PlanarReflectionNode)
                    PlanarReflectionNodes.Add(nodes[i]);
            }
        }
    }
}
