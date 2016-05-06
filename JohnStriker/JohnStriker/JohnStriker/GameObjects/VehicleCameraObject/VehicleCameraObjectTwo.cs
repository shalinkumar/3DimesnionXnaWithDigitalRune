using System;
using System.Linq;
using System.Text;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Geometry;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Physics;
using DigitalRune.Physics.Materials;
using DigitalRune.Physics.Specialized;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects.VehicleCameraObject
{
    public class VehicleCameraObjectTwo : GameObject
    {
        #region Fields

        //--------------------------------------------------------------

        private readonly IInputService _inputService;
        private readonly VehicleObject.VehicleObject _jet;
        private readonly IServiceLocator _services;
        private float _pitch;
        private bool _useSpectatorView;
        private float _yaw;


        private readonly ModelNode _missileModelNodes;

        private readonly Simulation _simulation;
        public float _fPitch;
        private float _fRotation;
        private bool SpawnMissile;
        private float _destinationsZ;
        private float _speedMissile = 0f;
        private float _motorForce;
        private float _angle;
        private Vector3F target;
        private Vector3F position;
        #endregion

        //--------------------------------------------------------------

        private readonly IGraphicsService _graphicsService;

        //--------------------------------------------------------------

        //--------------------------------------------------------------

        //--------------------------------------------------------------

        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private readonly TextBlock _updateFpsTextBlock;
        private StackPanel _fpsPanel;
        private GuiMissileScreen _guiGraphicsScreen;
        private TextBlock _OrientationTextBlock;
        #region Properties & Events

        //--------------------------------------------------------------

        public CameraNode CameraNode { get; private set; }

        public Vehicle Missile { get; private set; }
        #endregion

        #region Creation & Cleanup

        //--------------------------------------------------------------

        public VehicleCameraObjectTwo(VehicleObject.VehicleObject jet, IServiceLocator services)
        {
            Name = "Camerass";

            _jet = jet;
            _services = services;
            _inputService = services.GetInstance<IInputService>();
            _simulation = _services.GetInstance<Simulation>();

            _graphicsService = _services.GetInstance<IGraphicsService>();
            _guiGraphicsScreen = new GuiMissileScreen(_services);
            _graphicsService.Screens.Add(_guiGraphicsScreen);

            // ----- FPS Counter (top right)
            _fpsPanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            _guiGraphicsScreen.UIScreen.Children.Add(_fpsPanel);
            _updateFpsTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Position",
            };
            _fpsPanel.Children.Add(_updateFpsTextBlock);
            _OrientationTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Orientation",
            };
            _fpsPanel.Children.Add(_OrientationTextBlock);
            var contentManager = _services.GetInstance<ContentManager>();


            _missileModelNodes = contentManager.Load<ModelNode>("M16DMissileOne/skyfighter fbx").Clone();
            //_missileModelNodes.ScaleLocal = new Vector3F(0.004f);          
            _missileModelNodes.ScaleLocal = new Vector3F(0.2f);


            //_vehicleModelNode.ScaleLocal = new Vector3F(2f);
            var meshNode = _missileModelNodes.GetDescendants()
                                            .OfType<MeshNode>()
                                            .First(mn => mn.Name == "Cube_003");
            //Cube_003   Cylinder001 missile Circle_005
            var mesh = MeshHelper.ToTriangleMesh(meshNode.Mesh);
            // Apply the transformation of the mesh node.
            mesh.Transform(meshNode.PoseWorld);


            var convexHull = GeometryHelper.CreateConvexHull(mesh.Vertices, 64, -0.04f);

            // 3. Create convex polyhedron shape using the vertices of the convex hull.
            var chassisShape = new ConvexPolyhedron(convexHull.Vertices.Select(v => v.Position));


            var mass = MassFrame.FromShapeAndMass(chassisShape, Vector3F.One, 800, 0.1f, 1);
            //var mass = MassFrame.MassLimit
            // Trick: We artificially modify the center of mass of the rigid body. Lowering the center
            // of mass makes the car more stable against rolling in tight curves. 
            // We could also modify mass.Inertia for other effects.
            var pose = mass.Pose;
            // pose.Position.Y = 5; // Lower the center of mass.
            //pose.Position.Z = 0; // The center should be below the driver. 
            //pose.Position.X = 0;
            pose.Position.Y -= 0.5f; // Lower the center of mass.
            pose.Position.Z = -0.5f; // The center should be below the driver. 
            // (Note: The car model is not exactly centered.)
            mass.Pose = pose;

            // Material for the chassis.
            var material = new UniformMaterial
            {
                Restitution = 0.1f,
                StaticFriction = 0.2f,
                DynamicFriction = 0.2f
            };

            var chassis = new RigidBody(chassisShape, mass, material)
            {
                //Pose = new Pose(new Vector3F(jet.Chassis.Pose.Position.X, jet.Chassis.Pose.Position.Y, jet.Chassis.Pose.Position.Z-5)),  // Start position
                //Pose = new Pose(new Vector3F(0, 5, -15)),  // Start position



                Pose = new Pose(new Vector3F(jet.Jet.Chassis.Pose.Position.X, jet.Jet.Chassis.Pose.Position.Y-3, jet.Jet.Chassis.Pose.Position.Z  ), jet.Jet.Chassis.Pose.Orientation),  // Start position
                UserData = "NoDraw",                     // (Remove this line to render the collision model.)        

            };

            // ----- Create the vehicle.
            Missile = new Vehicle(_simulation, chassis);

            // Add 4 wheels.
            //Missile.Wheels.Add(new Wheel { Offset = new Vector3F(-1.8f, 0.6f, -2.0f), Radius = 0.36f, SuspensionRestLength = 0.55f, MinSuspensionLength = 0.25f, Friction = 200 });  // Front left
            //Missile.Wheels.Add(new Wheel { Offset = new Vector3F(0.9f, 0.6f, -2.0f), Radius = 0.36f, SuspensionRestLength = 0.55f, MinSuspensionLength = 0.25f, Friction = 200 });   // Front right
            //Missile.Wheels.Add(new Wheel { Offset = new Vector3F(-0.9f, 0.6f, 0.98f), Radius = 0.36f, SuspensionRestLength = 0.55f, MinSuspensionLength = 0.25f, Friction = 200 });// Back left
            //Missile.Wheels.Add(new Wheel { Offset = new Vector3F(0.9f, 0.6f, 0.98f), Radius = 0.36f, SuspensionRestLength = 0.55f, MinSuspensionLength = 0.25f, Friction = 200 }); // Back right        


            // Vehicles are disabled per default. This way we can create the vehicle and the simulation
            // objects are only added when needed.
            Missile.Enabled = true;

            RigidBody rigidBody = AddBody(_simulation, "M16DMissileOne/skyfighter fbx", Missile.Chassis.Pose, new PlaneShape(Vector3F.UnitY, 0), MotionType.Static);
        }

        #endregion

        #region Methods

        //--------------------------------------------------------------

        protected override void OnLoad()
        {
            var graphicsService = _services.GetInstance<IGraphicsService>();
            // Create a camera node.

            // Define camera projection.
            var projection = new PerspectiveProjection();
            projection.SetFieldOfView(
                ConstantsF.PiOver4,
                graphicsService.GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                10000f);

            // Create a camera node.
            CameraNode = new CameraNode(new Camera(projection))
            {
                Name = "PlayerCamerass"
            };


            var scene = _services.GetInstance<IScene>();
            if (scene != null)
                scene.Children.Add(CameraNode);
                scene.Children.Add(_missileModelNodes);
                SpawnMissile = true;
        }


        private static RigidBody AddBody(Simulation simulation, string name, Pose pose, Shape shape, MotionType motionType)
        {
            var rigidBody = new RigidBody(shape)
            {
                Name = name,
                Pose = pose,
                MotionType = motionType,
            };

            simulation.RigidBodies.Add(rigidBody);
            return rigidBody;
        }

        protected override void OnUnload()
        {
            CameraNode.Dispose(false);
            CameraNode = null;
        }
        public GeometricObject GeometricObject { get; private set; }

        protected override void OnUpdate(TimeSpan deltaTime)
        {
            var deltaTimeF = (float)deltaTime.TotalSeconds;
            // Mouse centering (controlled by the MenuComponent) is disabled if the game
            // is inactive or if the GUI is active. In these cases, we do not want to move
            // the player.
            if (!_inputService.EnableMouseCentering)
                return;

            if (_inputService.IsPressed(Keys.Enter, true))
            {
                // Toggle between player camera and spectator view.
                _useSpectatorView = !_useSpectatorView;
            }
            else
            {
               

                // Compute new yaw and pitch from mouse movement.
                float deltaYaw = 0;
                //deltaYaw -= _inputService.MousePositionDelta.X;
                deltaYaw -= _inputService.GetGamePadState(LogicalPlayerIndex.One).ThumbSticks.Right.X*10;
                _yaw += deltaYaw*deltaTimeF*0.1f;

                float deltaPitch = 0;
                //deltaPitch -= _inputService.MousePositionDelta.Y;
                deltaPitch += _inputService.GetGamePadState(LogicalPlayerIndex.One).ThumbSticks.Right.Y*10;
                _pitch += deltaPitch*deltaTimeF*0.1f;

                // Limit the pitch angle to less than +/- 90°.
                float limit = ConstantsF.PiOver2 - 0.01f;
                _pitch = MathHelper.Clamp(_pitch, -limit, limit);
            }

            // Update SceneNode.LastPoseWorld - this is required for some effects, like
            // camera motion blur. 
            CameraNode.LastPoseWorld = CameraNode.PoseWorld;

            Pose vehiclePose = _jet.Jet.Chassis.Pose;
            UpdateProfiler();
            if (_useSpectatorView)
            {
            }


            Missile.Enabled = true;

            if (!_inputService.EnableMouseCentering)
                return;
          


            // UpdateSteeringAngle();

            // UpdateAcceleration(deltaTimeF);                     

            UpdateProfiler();

            UpdatePitch();

            //UpdateAcceleration(deltaTimeF);

            // Update poses of graphics models.
            // Update SceneNode.LastPoseWorld (required for optional effects, like motion blur).

        
            //VehicleObject.VehicleObject vehicleObject=new VehicleObject.VehicleObject();
          //  _missileModelNodes.PoseWorld = new Pose(new Vector3F(_jet.Jet.Chassis.Pose.Position.X, _jet.Jet.Chassis.Pose.Position.Y - 3, _jet.Jet.Chassis.Pose.Position.Z), _jet.Jet.Chassis.Pose.Orientation);

            _angle += (float)deltaTime.TotalSeconds * 0.6f;

            //_missileModelNodes.SetLastPose(true);
            //_missileModelNodes.PoseWorld = new Pose(new Vector3F(_jet.Jet.Chassis.Pose.Position.X, _jet.Jet.Chassis.Pose.Position.Y - 3, _jet.Jet.Chassis.Pose.Position.Z), _jet.Jet.Chassis.Pose.Orientation);

           // _missileModelNodes.PoseWorld = new Pose(new Vector3F(6, 4, 0)) * new Pose(Matrix33F.CreateRotationY(_angle)) * new Pose(new Vector3F(6, 4, 0));

        
            float zPosition = 1.5f;
            Matrix33F yaw = Matrix33F.CreateRotationY(0.0f);
            Matrix33F pitch = Matrix33F.CreateRotationX(0.0f);
            //Matrix33F pitch = Matrix33F.CreateRotationX(0.1f);
            Matrix33F orientation = vehiclePose.Orientation * yaw * pitch;
            Vector3F forward = orientation * -new Vector3F(0, 0, zPosition);
            Vector3F up = Vector3F.UnitZ;
            position = vehiclePose.Position - 0 * forward + 0 * up;

             target = vehiclePose.Position + 0 * up;
            target=target-new Vector3F(0,1,1);
            _missileModelNodes.PoseWorld = new Pose(position + target);

            GeometricObject.Pose = new Pose(position + target);

//            if (_inputService.IsDown(Keys.Space))
//            {
//                SpawnMissile = false;
//            }
//
//            if (SpawnMissile)
//            {
//                _missileModelNodes.SetLastPose(true);
//                _missileModelNodes.PoseWorld = new Pose(new Vector3F(_jet.Jet.Chassis.Pose.Position.X, _jet.Jet.Chassis.Pose.Position.Y - 3, _jet.Jet.Chassis.Pose.Position.Z), _jet.Jet.Chassis.Pose.Orientation);
//
//            }
//            else if (!SpawnMissile)
//            {
//
//                // Camera moves with the car. The look direction can be changed by moving the mouse.
//                Matrix33F yaw = Matrix33F.CreateRotationY(_yaw);
//                Matrix33F pitch = Matrix33F.CreateRotationX(_jet._fPitch);
//                //Matrix33F pitch = Matrix33F.CreateRotationX(0.1f);
//                Matrix33F orientation = vehiclePose.Orientation  * pitch;
//                Vector3F forward = _jet.Jet.Chassis.Pose.Orientation * -new Vector3F(0, 0, _jet.Jet.Chassis.Pose.Position.Z);
//                Vector3F up = Vector3F.UnitY;
//                Vector3F position = _jet.Jet.Chassis.Pose.Position + 30 * Vector3F.Forward;
//                Vector3F target = vehiclePose.Position + 1 * up;
//                                     
//                _missileModelNodes.SetLastPose(true);
//                _missileModelNodes.PoseWorld = new Pose(new Vector3F(position.X, position.Y, position.Z), orientation);
//            }
        }

        #endregion

        private void UpdatePitch()
        {

            // Slow rate of pitch
            if (_fPitch > 0.0f)
            {
                _fPitch -= 0.0025f;
            }
            else if (_fPitch < 0.0f)
            {
                _fPitch += 0.0025f;
            }

            // Limit rate of pitch
            if (_fPitch >= 0.20f)
            {
                _fPitch = 0.20f;
            }
            else if (_fPitch < -0.20f)
            {
                _fPitch = -0.20f;
            }

            // Update position and pitch
            //v3FPosition.Y += fPitch;
            //v3FRotation.X = (fPitch * fThrusters) / 3f;

        }


        private void UpdateAcceleration(float deltaTime)
        {
            const float MaxForce = 2000;
            const float AccelerationRate = 10000;

            // We limit the amount of change per frame.
            float change = AccelerationRate * deltaTime;
            float direction = 0;
            if (_inputService.IsDown(Keys.Space))
            {
                SpawnMissile = false;
            }

            if (SpawnMissile)
            {
               
                if (_inputService.IsDown(Keys.W))
                    direction += 1;
                if (_inputService.IsDown(Keys.S))
                    direction -= 1;
            }
            else if (!SpawnMissile)
            {
                if (_inputService.IsUp(Keys.W))
                    direction += 1;
                if (_inputService.IsDown(Keys.S))
                    direction -= 1;
            }
          

            var gamePadState = _inputService.GetGamePadState(LogicalPlayerIndex.One);
            direction += gamePadState.Triggers.Right - gamePadState.Triggers.Left;

            if (direction != 0)
            {

                // Increase motor force.
                _motorForce = MathHelper.Clamp(_motorForce + direction * change, -MaxForce, +MaxForce);
            }
            else
            {

                // No acceleration. Bring motor force down to 0.
                if (_motorForce > 0)
                    _motorForce = MathHelper.Clamp(_motorForce - change, 0, +MaxForce);
                //    _steeringAngle = 0.1f;
                else if (_motorForce < 0)
                    _motorForce = MathHelper.Clamp(_motorForce + change, -MaxForce, 0);
                //    _steeringAngle = -0.1f;
            }


            // We can decide which wheels are motorized. Here we use an all wheel drive:
            Missile.Wheels[0].MotorForce = _motorForce;
            Missile.Wheels[1].MotorForce = _motorForce;
            Missile.Wheels[2].MotorForce = _motorForce;
            Missile.Wheels[3].MotorForce = _motorForce;
        }

        private void UpdateProfiler()
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("Position: " + position);
            _updateFpsTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("Target: " + target);
            _OrientationTextBlock.Text = _stringBuilder.ToString();
        }
    }
}