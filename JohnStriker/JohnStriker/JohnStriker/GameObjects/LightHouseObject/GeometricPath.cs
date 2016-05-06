using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DigitalRune.Animation;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Geometry;
using DigitalRune.Geometry.Collisions;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.Effects;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Mathematics.Interpolation;
using DigitalRune.Mathematics.Statistics;
using JohnStriker.GraphicsScreen;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using Console = System.Console;
using CurveLoopType = DigitalRune.Mathematics.Interpolation.CurveLoopType;

namespace JohnStriker.GameObjects.LightHouseObject
{
    public class GeometricPath : GameObject
    {
        private readonly IServiceLocator _services;
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private readonly Random randomGen = new Random(12345);
        private CollisionObject _collisionObject;

        // The original 3D path.
        private ConstParameterBinding<Vector3> _emissiveColorBinding;
        private GeometricObject _geometricObject;
        private AnimatableProperty<float> _glowIntensity;
        private MyGraphicsScreen _graphicsScreen;

        private IInputService _inputService;
        private ModelNode _modelNode;
        private Path3F _path;
        private PointLight _pointLight;
        private ModelNode _shipNode;
        private float _time;
        private TextBlock _updateFpsTextBlock;

        private GuiPathScreen _guiPathScreen;

        private IGraphicsService _graphicsService;

        private ModelNode _modelLightHouse;

        public CameraNode _cameraNode;

        float _fRotation = 0.0f;

        float rotX = 0.0f;

        public GeometricPath(IServiceLocator services)
        {
            _services = services;
        }

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


        protected override void OnLoad()
        {
            var contentManager = _services.GetInstance<ContentManager>();

            _graphicsScreen = new MyGraphicsScreen(_services) {DrawReticle = true};
            _inputService = _services.GetInstance<IInputService>();
            // Add the GuiGraphicsScreen to the graphics service.
             _graphicsService = _services.GetInstance<IGraphicsService>();
             _guiPathScreen = new GuiPathScreen(_services);
            _graphicsService.Screens.Add(_guiPathScreen);

            // ----- FPS Counter (top right)
            var fpsPanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            _guiPathScreen.UIScreen.Children.Add(fpsPanel);
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
                PreLoop = CurveLoopType.Cycle,
                PostLoop = CurveLoopType.Cycle,
                SmoothEnds = true
            };

            // Create a camera.
            var projection = new PerspectiveProjection();
            projection.SetFieldOfView(
              ConstantsF.PiOver4,
              _graphicsService.GraphicsDevice.Viewport.AspectRatio,
              0.1f,
              1000.0f);
            _cameraNode = new CameraNode(new Camera(projection));
            _graphicsScreen.ActiveCameraNode = _cameraNode;

            var randomList = new List<int>();

            var insertList = new List<Vector3F>
            {
                ////1
                //new Vector3F(0f, 0f, 0f),
                //new Vector3F(-37f, 0f, -766f),
                //new Vector3F(-777f, 0f, -545f),
                //new Vector3F(-310f, 0f, -1903f),
                //new Vector3F(-1554f, 0f, -1678f),
                //new Vector3F(-858f, 0f, -1895f),
                //new Vector3F(-1956f, 0f, -4627f),
                //new Vector3F(-336f, 0f, -2517f),
                ////2
                //new Vector3F(0f, 0f, 0f),
                //new Vector3F(-326f, 0f, -95f),
                //new Vector3F(-542f, 0f, -1572f),
                //new Vector3F(-700f, 0f, -284f),
                //new Vector3F(-1595f, 0f, -2245f),
                //new Vector3F(-1700f, 0f, -826f),
                //new Vector3F(-700f, 0f, -4023f),
                //new Vector3F(-1219f, 0f, -2977f),
                ////3               
                //new Vector3F(0f, 0f, 0f),
                //new Vector3F(59f, 0f, -223f),
                //new Vector3F(-235f, 0f, -383f),
                //new Vector3F(-548f, 0f, -1944f),
                //new Vector3F(-1549f, 0f, -2645f),
                //new Vector3F(-1859f, 0f, -2386f),
                //new Vector3F(-1072f, 0f, -1947f),
                //new Vector3F(-728f, 0f, -2299f),
                ////4
                //new Vector3F(0f, 0f, 0f),
                //new Vector3F(47f, 0f, -623),
                //new Vector3F(-258f, 0f, -84f),
                //new Vector3F(-1081f, 0f, -791f),
                //new Vector3F(-541f, 0f, -2025f),
                //new Vector3F(-439f, 0f, -2772f),
                //new Vector3F(-1944f, 0f, -486f),
                //new Vector3F(-231f, 0f, -1901f),
                ////5
                //new Vector3F(0f, 0f, 0f),
                //new Vector3F(-141f, 0f, -799f),
                //new Vector3F(-37f, 0f, -884f),
                //new Vector3F(-352f, 0f, -1379f),
                //new Vector3F(-973f, 0f, -1899f),
                //new Vector3F(-722f, 0f, -3120f),
                //new Vector3F(-1636f, 0f, -2242f),
                //new Vector3F(-1647f, 0f, -1183f),
                ////6
//new Vector3F  (0f, 0f, 0f),
//new Vector3F(-198f, 0f, -354f),
//new Vector3F(-612f, 0f, -1400f),
//new Vector3F(-1029f, 0f, -1404f),
//new Vector3F(-477f, 0f, -1751f),
//new Vector3F(-1247f, 0f, -258f),
//new Vector3F(-1782f, 0f, -2353f),
//new Vector3F(-2638f, 0f, -3234f),
////7
//new Vector3F(0f, 0f, 0f),
//new Vector3F(-30f, 0f, -728f),
//new Vector3F(641f, 0f, -212f),
//new Vector3F(284f, 0f, -794f),
//new Vector3F(-1097f, 0f, -2237f),
//new Vector3F(-802f, 0f, -1860f),
//new Vector3F(-908f, 0f, -1611f),
//new Vector3F(-714f, 0f, -5082f),
////8
//new Vector3F(0f, 0f, 0f),
//new Vector3F(-214f, 0f, -73f),
//new Vector3F(-634f, 0f, -520f),
//new Vector3F(-989f, 0f, -292f),
//new Vector3F(-621f, 0f, -1821f),
//new Vector3F(-60f, 0f, -661f),
//new Vector3F(-865f, 0f, -4599f),
//new Vector3F(-2260f, 0f, -5519f),
////9
//new Vector3F(0f, 0f, 0f),
//new Vector3F(-11f, 0f, -660f),
//new Vector3F(-79f, 0f, -846f),
//new Vector3F(-553f, 0f, -923f),
//new Vector3F(-796f, 0f, -1649f),
//new Vector3F(-1351f, 0f, -2451f),
//new Vector3F(-1730f, 0f, -2413f),
//new Vector3F(-1627f, 0f, -4140f),
////10
new Vector3F(0f, 0f, 0f),
new Vector3F(-296f, 1f, -592f),
new Vector3F(-329f, 1f, -282f),
new Vector3F(-1149f, 1f, -579f),
new Vector3F(-678f, 1f, -3173f),
new Vector3F(-779f, 1f, -3832f),
new Vector3F(-2075f, 1f, -3026f),
new Vector3F(-1547f, 1f, -1253f),

            };

            int seed = 0, increment = 0;
            int n = 10;

            int l = seed;
            for (int ii = 0; ii < n; ii++)
            {
                l = (l + increment)%n;
                randomList.Add(l);
            }

            var r = new Random();
            IEnumerable<int> threeRandom = randomList.OrderBy(y => r.Next()).Take(1);
            List<int> ll = threeRandom.ToList();


            int iii = 0;
            int incrementCount = 5 + ll[0];
            // Add random path key points.
            for (int i = ll[0]; i < incrementCount; i++)
            {
                //var ZIncrement = k * 800;
                //var XIncrement = k * 400;
                //int x = RandomHelper.Random.Next(XIncrement);
                //int y = RandomHelper.Random.Next(2);
                //int z = RandomHelper.Random.Next(ZIncrement);
                var key = new PathKey3F
                {
                    Parameter = i,
                    Point = new Vector3F(insertList[i].X, insertList[i].Y, insertList[i].Z),
                    Interpolation = SplineInterpolation.CatmullRom
                };
                _path.Add(key);
                Console.WriteLine(key.Point);
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


            // Draw the path.
            for (float lll = 0; lll < _path.Last().Parameter; lll += 10f)
            {
                ModelNode modelOne = _modelNode.Clone();
                ModelNode modelTwo = _modelNode.Clone();
                scene.Children.Add(modelOne);
                scene.Children.Add(modelTwo);


                SampleHelper.EnablePerPixelLighting(_modelNode);


                Vector3F point0 = _path.GetPoint(lll);
                Vector3F point1 = _path.GetPoint((lll + 20f));
                modelOne.PoseWorld = new Pose(new Vector3F(point0.X, point0.Y, point0.Z));

                modelOne.PoseWorld = new Pose(new Vector3F(point1.X, point1.Y, point1.Z));

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

            // Draw the path key points.
            foreach (var point in _path.Select(key => key.Point))
            {

                //_modelLightHouse = contentManager.Load<ModelNode>("LightTower/LightTower").Clone();

                //_modelLightHouse.ScaleLocal = new Vector3F(2f);

                //ModelNode modelOne = _modelLightHouse.Clone();
                //scene.Children.Add(modelOne);

                //SampleHelper.EnablePerPixelLighting(_modelLightHouse);

                //modelOne.PoseWorld = new Pose(new Vector3F(point.X, 0, point.Z));

                //Console.WriteLine(point);
                // Load collision shape from a separate model (created using the CollisionShapeProcessor).
                //var shape = contentManager.Load<Shape>("LavaBall/LavaBall_Collision");

                //// Create a GeometricObject (= Shape + Pose + Scale).
                //_geometricObject = new GeometricObject(shape, _modelLightHouse.PoseWorld);

                //// Create a collision object and add it to the collision domain.
                //_collisionObject = new CollisionObject(_geometricObject);

                //// Important: We do not need detailed contact information when a collision
                //// is detected. The information of whether we have contact or not is sufficient.
                //// Therefore, we can set the type to "Trigger". This increases the performance 
                //// dramatically.
                //_collisionObject.Type = CollisionObjectType.Trigger;

                //// Add the collision object to the collision domain of the game.
                //var collisionDomain = _services.GetInstance<CollisionDomain>();
                //collisionDomain.CollisionObjects.Add(_collisionObject);
            
            }
        }

        protected override void OnUpdate(TimeSpan deltaTime)
        {
            if (!_inputService.EnableMouseCentering)
                return;

            // Update _time.
            //_time += (float) deltaTime.TotalSeconds*10f;
            ////matrix.M02 += (float)deltaTime.TotalSeconds * (1) * 0.002f * matrix.M00;
            //const float speed = 0.3f;
            //float traveledDistance = _time*speed;
            //// Get path parameter where the path length is equal to traveledDistance.
            //float parameter = _path.GetParameterFromLength(traveledDistance, 10, 0.01f);
            //// Get path point at the traveledDistance.
            //Vector3F position = _path.GetPoint(parameter);
            //// Get the path tangent at traveledDistance and use it as the forward direction.
            //Vector3F forward = _path.GetTangent(parameter).Normalized;
            //// Draw an object on the path.
            //DrawObject(position, forward, _time);

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
            Vector3F cusp = position + forward*length/2;

         
            if (_inputService.IsDown(Keys.Right))
            {
                _fRotation += 0.01f;
              
            }
            if (_inputService.IsDown(Keys.Left))
            {
                _fRotation -= 0.01f;
            
            }
       
            if (rotX > cusp.X)
            {
                rotX = cusp.X;
            }
            if (rotX < cusp.X)
            {
                rotX = cusp.X;
            }
            //Matrix33F rotation = Matrix33F.CreateRotationY(_time*0.3f);
            //QuaternionF Orientation = QuaternionF.CreateRotationY(0.3699999f);
            QuaternionF Orientation = QuaternionF.CreateRotationY(3.4f);
         
        
            _shipNode.PoseWorld = new Pose(cusp+new Vector3F(5,1,0), Orientation);

            // ----- Set view matrix for graphics.
            // For third person we move the eye position back, behind the body (+z direction is 
            // the "back" direction).
            Vector3F thirdPersonDistance = Orientation.Rotate(new Vector3F(0, 1, 15));

            // Compute camera pose (= position + orientation). 
            _cameraNode.PoseWorld = new Pose
            {
                Position = _shipNode.PoseWorld.Position         // Floor position of character
                           + new Vector3F(0, 1.6f, 0)  // + Eye height
                           + thirdPersonDistance,
                Orientation = Orientation.ToRotationMatrix33()
            };

            UpdateProfiler();
        }

        protected override void OnUnload()
        {
            // Remove the collision object from the collision domain.
            CollisionDomain collisionDomain = _collisionObject.Domain;
            collisionDomain.CollisionObjects.Remove(_collisionObject);

            // Detach objects to avoid any "memory leaks".
            _collisionObject.GeometricObject = null;
            _geometricObject.Shape = Shape.Empty;

            // Remove the model from the scene.                                                          
            for (int j = 0; j < _modelNode.Children.Count; j++)
            {
                _modelNode.Children[j].Parent.Children.Remove(_modelNode.Children[j]);
            }

            _graphicsService.Screens.Remove(_guiPathScreen);
        }

        private void UpdateProfiler()
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("Position: " + _shipNode.PoseWorld.Position);
            _updateFpsTextBlock.Text = _stringBuilder.ToString();

            //Console.WriteLine(_shipNode.PoseWorld.Position);
        }
    }
}