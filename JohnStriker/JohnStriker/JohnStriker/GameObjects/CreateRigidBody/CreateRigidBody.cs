using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DigitalRune.Geometry.Meshes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics.Algebra;
using Microsoft.Xna.Framework.Content;

namespace JohnStriker.GameObjects.CreateRigidBody
{
    public class CreateRigidBody
    {

        public CreateRigidBody(ContentManager contentManager)
        {
            CreateRigidBodys(contentManager);
        }

        public static TriangleMesh TriangleMeshP { get; set; }

        private static void CreateRigidBodys(ContentManager contentManager)
        {

            var triangleMesh = new TriangleMesh();
            ModelNode _modelPrototype = contentManager.Load<ModelNode>("M16D/skyfighter fbx").Clone();

            foreach (var meshNode in _modelPrototype.GetSubtree().OfType<MeshNode>())
            {
                // Extract the triangle mesh from the DigitalRune Graphics Mesh instance. 
                var subTriangleMesh = new TriangleMesh();
                foreach (var submesh in meshNode.Mesh.Submeshes)
                {
                    submesh.ToTriangleMesh(subTriangleMesh);
                }

                // Apply the transformation of the mesh node.
                subTriangleMesh.Transform(meshNode.PoseWorld * Matrix44F.CreateScale(meshNode.ScaleWorld));

                // Combine into final triangle mesh.
                triangleMesh.Add(subTriangleMesh);
            }
            TriangleMeshP = triangleMesh;

        }
    }
}
