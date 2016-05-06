using System;
using System.Linq;
using System.Text;
using DigitalRune.Animation;
using DigitalRune.Animation.Easing;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Geometry;
using DigitalRune.Geometry.Collisions;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.Effects;
using DigitalRune.Graphics.Rendering;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Mathematics.Statistics;
using DigitalRune.Storages;
using JohnStriker.GraphicsScreen;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework.Content;
using DigitalRune.Mathematics.Interpolation;
using Microsoft.Xna.Framework;

namespace JohnStriker.GameObjects.LightHouseObject
{



    public class PathGeometric : GameObject
    {
        private readonly IServiceLocator _services;
        private ModelNode _modelNode;
        private ModelNode _shipNode;
        private GeometricObject _geometricObject;
        private CollisionObject _collisionObject;

        // The original 3D path.
        private Path3F _path;

        // The elapsed time, used to animate an object along the path.
        private float _time;

        private MyGraphicsScreen _graphicsScreen;

        private PointLight _pointLight;
        private ConstParameterBinding<Vector3> _emissiveColorBinding;
        private AnimatableProperty<float> _glowIntensity;

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private TextBlock _updateFpsTextBlock;

        private IInputService _inputService;

        // The collision object which can be used for contact queries.
        public CollisionObject CollisionObject
        {
            get { return _collisionObject; }
        }


        // The position and orientation of the ship.
        public Pose Pose
        {
            get { return _modelNode.PoseWorld; }
            set
            {
                _modelNode.PoseWorld = value;
                _geometricObject.Pose = value;
            }
        }


        public PathGeometric(IServiceLocator services)
        {
            _services = services;
        }
        private Random randomGen = new Random(12345);

        protected override void OnLoad()
        {
            var contentManager = _services.GetInstance<ContentManager>();

            _graphicsScreen = new MyGraphicsScreen(_services) { DrawReticle = true };
            _inputService = _services.GetInstance<IInputService>();
            // Add the GuiGraphicsScreen to the graphics service.
            IGraphicsService graphicsService = _services.GetInstance<IGraphicsService>();
            GuiPathScreen _GuiPathScreen = new GuiPathScreen(_services);
            graphicsService.Screens.Add(_GuiPathScreen);

            // ----- FPS Counter (top right)
            StackPanel fpsPanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            _GuiPathScreen.UIScreen.Children.Add(fpsPanel);
            _updateFpsTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Position",
            };
            fpsPanel.Children.Add(_updateFpsTextBlock);

            // Create a cyclic path.
            _path = new Path3F
            {
                PreLoop = DigitalRune.Mathematics.Interpolation.CurveLoopType.Cycle,
                PostLoop = DigitalRune.Mathematics.Interpolation.CurveLoopType.Cycle,
                SmoothEnds = true
            };

            // Add random path key points.
            for (int k = 0; k < 8; k++)
            {
                var ZIncrement = k * 800;
                var XIncrement = k * 400;
                int x = RandomHelper.Random.Next(XIncrement);
                int y = RandomHelper.Random.Next(2);
                int z = RandomHelper.Random.Next(ZIncrement);
                var key = new PathKey3F
                {
                    Parameter = k,
                    Point = new Vector3F(-x, 0, -z),
                    Interpolation = SplineInterpolation.CatmullRom
                };
                _path.Add(key);
            }

            // The last key uses the same position as the first key to create a closed path.
            var lastKey = new PathKey3F
            {
                Parameter = _path.Count,
                Point = _path[0].Point,
                Interpolation = SplineInterpolation.CatmullRom,
            };
            _path.Add(lastKey);

            _path.ParameterizeByLength(10, 0.0001f);


            // ----- Graphics
            // Load a graphics model and add it to the scene for rendering.
            _modelNode = contentManager.Load<ModelNode>("LavaBall/LavaBall").Clone();
            _modelNode.ScaleLocal = new Vector3F(1f);

            _shipNode = contentManager.Load<ModelNode>("M16D/skyfighter fbx").Clone();
            _shipNode.ScaleLocal = new Vector3F(0.3f);
           
            float _angle = 0;
            var scene = _services.GetInstance<IScene>();

            scene.Children.Add(_shipNode);

            Matrix33F orientation = Matrix33F.CreateRotationX(randomGen.NextFloat(0, ConstantsF.TwoPi));
            //            for (int i = 1; i < 2; i++)
            //            {
            // Draw the path.
            for (float l = 0; l < _path.Last().Parameter; l += 10f)
            {
                //                var ZIncrement = i * 400;
                //                var XIncrement = (i + 16) + 25;
                var modelOne = _modelNode.Clone();
                var modelTwo = _modelNode.Clone();
                scene.Children.Add(modelOne);
                scene.Children.Add(modelTwo);
                //            scene.Children.Add(modelTwo);

                SampleHelper.EnablePerPixelLighting(_modelNode);

                //                _angle += (float)i * 0.06f;

                var point0 = _path.GetPoint(l);
                var point1 = _path.GetPoint((l + 20f));
                modelOne.PoseWorld = new Pose(new Vector3F(point0.X, point0.Y, point0.Z));

                modelOne.PoseWorld = new Pose(new Vector3F(point1.X, point1.Y, point1.Z));




                //  modelTwo.PoseWorld = new Pose(new Vector3F(point1.X, point1.Y, point1.Z));

                // modelOne.PoseWorld =  new Pose(Matrix33F.CreateRotationZ(_angle)) * new Pose(new Vector3F(25, 1, -ZIncrement));

                //modelOne.PoseWorld = new Pose(new Vector3F(25, 0, 80)) * new Pose(Matrix33F.CreateRotationY(i)) * new Pose(new Vector3F(25, 0, 80));

                //modelTwo.PoseWorld = new Pose(new Vector3F(-25, 0, 80)) * new Pose(Matrix33F.CreateRotationY(i)) * new Pose(new Vector3F(-25, 0, 80));
                //                if (i <= 1)
                //                {
                //                    modelOne.PoseWorld = new Pose(new Vector3F(25, 1, -ZIncrement), orientation);
                //                    modelTwo.PoseWorld = new Pose(new Vector3F(-25, 1, -ZIncrement), orientation);
                //                }
                //                else if (i > 1 && i <=5)
                //                {
                //                    int ii = i + 15;
                //                    int jj = 25 + ii;
                //
                //                    int iii = i - 15;
                //                    int jjj = 25 + iii;
                //
                //
                //                    modelOne.PoseWorld = new Pose(new Vector3F(jj, 1, -ZIncrement), orientation);
                //                    modelTwo.PoseWorld = new Pose(new Vector3F(-jjj, 1, -ZIncrement), orientation);
                //                }
                //                else if (i > 5)
                //                {
                //                    int ii = i + 20;
                //                    int jj = 25 + ii;
                //
                //                    int iii = i - 20;
                //                    int jjj = 25 + ii;
                //
                //
                //                    modelOne.PoseWorld = new Pose(new Vector3F(jj, 1, -ZIncrement), orientation);
                //                    modelTwo.PoseWorld = new Pose(new Vector3F(-jjj, 1, -ZIncrement), orientation);
                //                }




                // ----- Collision Detection
                // Create a collision object and add it to the collision domain.

                // Load collision shape from a separate model (created using the CollisionShapeProcessor).
                var shape = contentManager.Load<Shape>("LavaBall/LavaBall_Collision");

                // Create a GeometricObject (= Shape + Pose + Scale).
                _geometricObject = new GeometricObject(shape, _modelNode.PoseWorld);

                // Create a collision object and add it to the collision domain.
                _collisionObject = new CollisionObject(_geometricObject);

                // Important: We do not need detailed contact information when a collision
                // is detected. The information of whether we have contact or not is sufficient.
                // Therefore, we can set the type to "Trigger". This increases the performance 
                // dramatically.
                _collisionObject.Type = CollisionObjectType.Trigger;

                // Add the collision object to the collision domain of the game.
                var collisionDomain = _services.GetInstance<CollisionDomain>();
                collisionDomain.CollisionObjects.Add(_collisionObject);

            }

            //            }


        }

        protected override void OnUpdate(TimeSpan deltaTime)
        {

            if (!_inputService.EnableMouseCentering)
                return;

            // Update _time.
            _time += (float)deltaTime.TotalSeconds * 10f;
            //matrix.M02 += (float)deltaTime.TotalSeconds * (1) * 0.002f * matrix.M00;
            const float speed = 2;
            var traveledDistance = _time * speed;
            // Get path parameter where the path length is equal to traveledDistance.
            var parameter = _path.GetParameterFromLength(traveledDistance, 10, 0.01f);
            // Get path point at the traveledDistance.
            Vector3F position = _path.GetPoint(parameter);
            // Get the path tangent at traveledDistance and use it as the forward direction.
            Vector3F forward = _path.GetTangent(parameter).Normalized;
            // Draw an object on the path.
            DrawObject(position, forward, _time);
         
            base.OnUpdate(deltaTime);
        }

        private void DrawObject(Vector3F position, Vector3F forward, float _time)
        {
            // Compute two vectors that are orthogonal to the forward direction.
            Vector3F right, up;
            if (Vector3F.AreNumericallyEqual(forward, Vector3F.Up))
            {
                // The forward direction is close to the up vector (0, 1, 0). In this case we 
                // simply set the default directions right (1, 0, 0) and backward (0, 0, 1).
                right = Vector3F.Right;
                up = Vector3F.Backward;
            }
            else
            {
                // Use the cross product calculate the orthogonal directions.
                right = Vector3F.Cross(forward, Vector3F.Up).Normalized;
                up = Vector3F.Cross(right, forward);
            }

            // Length of the object.
            const float length = 3f;
            // Width of the object.
            const float width = 0.1f;
            // Position of the tip of the object.
            Vector3F cusp = position + forward * length / 2;

            var rotation = Matrix33F.CreateRotationY((float)_time * 0.3f);
            QuaternionF Orientation = QuaternionF.CreateRotationY(forward.Y) * QuaternionF.CreateRotationX(forward.X);
             //We draw the object with 4 lines.
//            _shipNode.PoseWorld = new Pose(cusp);

            _shipNode.PoseWorld = new Pose(cusp - length * forward + width * up + width * right, rotation);

            UpdateProfiler();
        }

        protected override void OnUnload()
        {
            // Remove the collision object from the collision domain.
            var collisionDomain = _collisionObject.Domain;
            collisionDomain.CollisionObjects.Remove(_collisionObject);

            // Detach objects to avoid any "memory leaks".
            _collisionObject.GeometricObject = null;
            _geometricObject.Shape = Shape.Empty;

            // Remove the model from the scene.                                                          
            for (int j = 0; j < _modelNode.Children.Count; j++)
            {
                _modelNode.Children[j].Parent.Children.Remove(_modelNode.Children[j]);
            }                     
        }

        private void UpdateProfiler()
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("Position: " + _shipNode.PoseWorld.Position);
            _updateFpsTextBlock.Text = _stringBuilder.ToString();          
        }
    }
}
