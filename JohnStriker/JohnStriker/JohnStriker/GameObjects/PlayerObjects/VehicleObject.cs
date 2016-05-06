using System;
using System.Linq;
using System.Text;
using DigitalRune;
using DigitalRune.Animation;
using DigitalRune.Animation.Easing;
using DigitalRune.Diagnostics;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Geometry;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.Effects;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Mathematics.Statistics;
using DigitalRune.Particles;
using DigitalRune.Particles.Effectors;
using DigitalRune.Physics;
using DigitalRune.Physics.Materials;
using DigitalRune.Physics.Specialized;
using DigitalRune.Text;
using JohnStriker.GameObjects.Spark;
using JohnStriker.GraphicsScreen;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects.VehicleObject
{
    public enum ProjectileType
    {
        Blaster = 0,              // blaster projectile
        Missile                   // missile projectile
    }

    public class VehicleObject : GameObject
    {
        // Note:
        // To reset the vehicle position, simply call:
        //  _vehicle.Chassis.Pose = myPose;
        //  _vehicle.Chassis.LinearVelocity = Vector3F.Zero;
        //  _vehicle.Chassis.AngularVelocity = Vector3F.Zero;


        //--------------------------------------------------------------
        #region Fields
        //--------------------------------------------------------------

        private readonly IServiceLocator _services;
        private readonly IInputService _inputService;

        private readonly Simulation _simulation;
        private IScene _scene;

        // Models for rendering.
        private readonly ModelNode _vehicleModelNode;
        private readonly ModelNode[] _wheelModelNodes;
        private readonly ModelNode _missileModelNodes;
        private readonly ModelNode _missileModelNodesOne;
        // Jet values.
        private float _steeringAngle;
        private float _motorForce;
        private float _direction = 0;
        public float _fPitch;
        public float _fRotation;
        private Matrix33F pitch;
        private string _fRotationPosition;

        private StackPanel _fpsPanel;
        private TextBlock _updateFpsTextBlock;
        private TextBlock _drawFpsTextBlock;
        private TextBlock _RotationTextBlock;
        private TextBlock _OrientationTextBlock;
        private TextBlock _fRotationPositionBlock;
        private GuiCustomScreen _guiGraphicsScreen;
        private readonly IGraphicsService _graphicsService;
        private readonly IGameObjectService _gameObjectService;
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private Texture2D sparkTexture2D;
        private bool SpawnMissile = false;
        private int increment = 0;
        private float _destinationZ;
        private PointLight _pointLight;
        private ConstParameterBinding<Vector3> _emissiveColorBinding;
        private AnimatableProperty<float> _glowIntensity;

        private const float Tolerance = 60.0f;


        private bool FirstTime = true;
        private float _destinationsZ;
        private float _destinationsZPosition;
        private float _missileForce;
        private float _speedMissile = 0f;
        private float _speedInitialize = 0f;
        #endregion


        //--------------------------------------------------------------
        #region Properties & Events
        //--------------------------------------------------------------

        public Vehicle Jet { get; private set; }
        #endregion


        //--------------------------------------------------------------
        #region Creation & Cleanup & RigidBody
        //--------------------------------------------------------------

        public VehicleObject()
        {
        }


        public VehicleObject(IServiceLocator services)
        {
            _services = services;
            Name = "Vehicle";
            _gameObjectService = services.GetInstance<IGameObjectService>();
            _inputService = _services.GetInstance<IInputService>();
            _simulation = _services.GetInstance<Simulation>();

            //_graphicsService = _services.GetInstance<IGraphicsService>();
            //// Add the GuiGraphicsScreen to the graphics service.
            //_guiGraphicsScreen = new GuiCustomScreen(_services);
            //_graphicsService.Screens.Add(_guiGraphicsScreen);

            //// ----- FPS Counter (top right)
            //_fpsPanel = new StackPanel
            //{
            //    Margin = new Vector4F(10),
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    VerticalAlignment = VerticalAlignment.Top,
            //};
            //_guiGraphicsScreen.UIScreen.Children.Add(_fpsPanel);
            //_updateFpsTextBlock = new TextBlock
            //{
            //    Font = "DejaVuSans",
            //    Foreground = Color.Black,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    Text = "Position",
            //};
            //_fpsPanel.Children.Add(_updateFpsTextBlock);
            //_OrientationTextBlock = new TextBlock
            //{
            //    Font = "DejaVuSans",
            //    Foreground = Color.Black,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    Text = "Orientation",
            //};
            //_fpsPanel.Children.Add(_OrientationTextBlock);
            //_drawFpsTextBlock = new TextBlock
            //{
            //    Font = "DejaVuSans",
            //    Foreground = Color.Black,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    Text = "Pitch",
            //};
            //_fpsPanel.Children.Add(_drawFpsTextBlock);
            //_RotationTextBlock = new TextBlock
            //{
            //    Font = "DejaVuSans",
            //    Foreground = Color.Black,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    Text = "Rotation",
            //};
            //_fpsPanel.Children.Add(_RotationTextBlock);

            //_fRotationPositionBlock = new TextBlock
            //{
            //    Font = "DejaVuSans",
            //    Foreground = Color.Black,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    Text = "_fRotationPosition",
            //};
            //_fpsPanel.Children.Add(_fRotationPositionBlock);

            // Load models for rendering.
            var contentManager = _services.GetInstance<ContentManager>();
            _vehicleModelNode = contentManager.Load<ModelNode>("M16D/skyfighter fbx").Clone();
            _vehicleModelNode.ScaleLocal = new Vector3F(0.3f);


          

            //_vehicleModelNode.ScaleLocal = new Vector3F(0.0009f);

            _missileModelNodes = contentManager.Load<ModelNode>("Missile/Missile").Clone();
            _vehicleModelNode.Children.Add(_missileModelNodes);
            _missileModelNodes.ScaleLocal = new Vector3F(0.004f);


            _missileModelNodesOne = contentManager.Load<ModelNode>("Missile/Missile").Clone();
            _vehicleModelNode.Children.Add(_missileModelNodesOne);
            _missileModelNodesOne.ScaleLocal = new Vector3F(0.004f);

            _pointLight = new PointLight
            {
                Color = new Vector3F(1, 1, 1),
                DiffuseIntensity = 2,
                SpecularIntensity = 2,
                Range = 1.5f,
                Attenuation = 0.5f,
                //Texture = content.Load<TextureCube>("LavaBall/LavaCubeMap"),
            };
            var pointLightNode = new LightNode(_pointLight);
            _missileModelNodesOne.Children.Add(pointLightNode);

            // Get the emissive color binding of the material because the emissive color
            // will be animated.
            // The model contains one mesh node with a single material.
            var meshNodes = (MeshNode)_missileModelNodes.Children[0];
            var meshs = meshNodes.Mesh;
            var materials = meshs.Materials[0];

            // The material contains several effect bindings. The "EmissiveColor" is applied
            // in the "Material" pass. 
            // (For reference see material definition file: Samples\Media\LavaBall\Lava.drmat)
            _emissiveColorBinding = (ConstParameterBinding<Vector3>)materials["Material"].ParameterBindings["EmissiveColor"];

            // Use the animation service to animate glow intensity of the lava.
            var animationService = _services.GetInstance<IAnimationService>();

            // Create an AnimatableProperty<float>, which stores the animation value.
            _glowIntensity = new AnimatableProperty<float>();

            // Create sine animation and play the animation back-and-forth.
            var animation = new SingleFromToByAnimation
            {
                From = 0.3f,
                To = 3.0f,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new SineEase { Mode = EasingMode.EaseInOut },
            };
            var clip = new AnimationClip<float>
            {
                Animation = animation,
                Duration = TimeSpan.MaxValue,
                LoopBehavior = LoopBehavior.Oscillate
            };
            animationService.StartAnimation(clip, _glowIntensity).AutoRecycle();





            var meshNode = _vehicleModelNode.GetDescendants()
                                            .OfType<MeshNode>()
                                            .First(mn => mn.Name == "Cube_003");

            //ARC_170_LEE_RAY:polySurface1394   Cube_008
            var mesh = MeshHelper.ToTriangleMesh(meshNode.Mesh);
            // Apply the transformation of the mesh node.
            mesh.Transform(meshNode.PoseWorld);


            var convexHull = GeometryHelper.CreateConvexHull(mesh.Vertices, 64, -0.04f);

            // 3. Create convex polyhedron shape using the vertices of the convex hull.
            var chassisShape = new ConvexPolyhedron(convexHull.Vertices.Select(v => v.Position));



            // The mass properties of the car. We use a mass of 800 kg.
            var mass = MassFrame.FromShapeAndMass(chassisShape, Vector3F.One, 800, 0.1f, 1);


            var pose = mass.Pose;
            pose.Position.Y -= 0.5f; // Lower the center of mass.
            pose.Position.Z = -0.5f; // The center should be below the driver. 
            //pose.Position.X = 0.0f;
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
                Pose = new Pose(new Vector3F(0, 5, 0)),  // Start position
                UserData = "NoDraw",                     // (Remove this line to render the collision model.)        

            };

            // ----- Create the vehicle.
            Jet = new Vehicle(_simulation, chassis);
          
            // Add 4 wheels.
            Jet.Wheels.Add(new Wheel { Offset = new Vector3F(-1.8f, 0.6f, -2.0f), Radius = 0.36f, SuspensionRestLength = 0.55f, MinSuspensionLength = 0.25f, Friction = 200 });  // Front left
            Jet.Wheels.Add(new Wheel { Offset = new Vector3F(0.9f, 0.6f, -2.0f), Radius = 0.36f, SuspensionRestLength = 0.55f, MinSuspensionLength = 0.25f, Friction = 200 });   // Front right
            Jet.Wheels.Add(new Wheel { Offset = new Vector3F(-0.9f, 0.6f, 0.98f), Radius = 0.36f, SuspensionRestLength = 0.55f, MinSuspensionLength = 0.25f, Friction = 200 });// Back left
            Jet.Wheels.Add(new Wheel { Offset = new Vector3F(0.9f, 0.6f, 0.98f), Radius = 0.36f, SuspensionRestLength = 0.55f, MinSuspensionLength = 0.25f, Friction = 200 }); // Back right

            // Vehicles are disabled per default. This way we can create the vehicle and the simulation
            // objects are only added when needed.
            Jet.Enabled = false;

            RigidBody rigidBody = AddBody(_simulation, "M16D/skyfighter fbx", Jet.Chassis.Pose, new PlaneShape(Vector3F.UnitY, 0), MotionType.Static);
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
        #endregion


        //--------------------------------------------------------------
        #region Methods
        //--------------------------------------------------------------


        protected override void OnLoad()
        {
            // Enable vehicle. (This adds the necessary objects to the physics simulation.)
            Jet.Enabled = false;

            // Add graphics model to scene graph.
            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(_vehicleModelNode);

            SpawnMissile = true;

        }


        protected override void OnUnload()
        {
            // Disable vehicle. (This removes the vehicle objects from the physics simulation.)
            Jet.Enabled = false;

            // Remove graphics model from scene graph.
            _vehicleModelNode.Parent.Children.Remove(_vehicleModelNode);
            _vehicleModelNode.Children.Remove(_missileModelNodesOne);
            _vehicleModelNode.Children.Remove(_missileModelNodes);
        }


        protected override void OnUpdate(TimeSpan deltaTime)
        {
            // Mouse centering (controlled by the MenuComponent) is disabled if the game
            // is inactive or if the GUI is active. In these cases, we do not want to move
            // the player.
            Jet.Enabled = true;

            if (!_inputService.EnableMouseCentering)
                return;

            float deltaTimeF = (float)deltaTime.TotalSeconds;

            // Update steering direction from left/right arrow keys.
            UpdateSteeringAngle(deltaTimeF);

            // Update acceleration from up/down arrow keys.
            UpdateAcceleration(deltaTimeF);

            UpdateProfiler();

            if (_inputService.IsPressed(Keys.M, true))
            {

                //_gameObjectService.Objects.Add(new MissileObject.MissileObject(_services, _fRotation, Jet));           

            }

            //MissileObject.MissileObject missileObject = new MissileObject.MissileObject(Jet);


            if (_inputService.IsDown(Keys.Q) && _inputService.IsDown(Keys.W))
            {
                _direction += 0.005f;
                _fPitch += 0.005f;
            }
            if (_inputService.IsDown(Keys.E) && _inputService.IsDown(Keys.W))
            {
                _fPitch -= 0.005f;
                _direction -= 0.005f;
            }

            UpdatePitch();

            // Update poses of graphics models.
            // Update SceneNode.LastPoseWorld (required for optional effects, like motion blur).
            _vehicleModelNode.SetLastPose(true);
            _vehicleModelNode.PoseWorld = Jet.Chassis.Pose;

            Matrix33F orientation;
            if (_direction > 0)
            {
                pitch = Matrix33F.CreateRotationX(_fPitch);
                Matrix33F yaw = Matrix33F.CreateRotationY(_fRotation);
                orientation = Jet.Chassis.Pose.Orientation * pitch;
                float directions = Jet.Chassis.Pose.Position.Y + _direction;
                _vehicleModelNode.PoseWorld = new Pose(new Vector3F(Jet.Chassis.Pose.Position.X, directions, Jet.Chassis.Pose.Position.Z), orientation);
            }
            else if (_direction < 0)
            {
                pitch = Matrix33F.CreateRotationX(_fPitch);
                Matrix33F yaw = Matrix33F.CreateRotationY(_fRotation);
                orientation = Jet.Chassis.Pose.Orientation * pitch;
                float directions = Jet.Chassis.Pose.Position.Y + _direction;
                _vehicleModelNode.PoseWorld = new Pose(new Vector3F(Jet.Chassis.Pose.Position.X, directions, Jet.Chassis.Pose.Position.Z), orientation);
            }

            if (_inputService.IsDown(Keys.Space))
            {
                SpawnMissile = false;
            }


            if (SpawnMissile)
            {
                var poses = new Pose(new Vector3F(Jet.Chassis.Pose.Position.X, Jet.Chassis.Pose.Position.Y, Jet.Chassis.Pose.Position.Z), Jet.Chassis.Pose.Orientation);
                _missileModelNodes.SetLastPose(true);
                _missileModelNodes.PoseWorld = poses;

                var posesOne = new Pose(new Vector3F(Jet.Chassis.Pose.Position.X, Jet.Chassis.Pose.Position.Y, Jet.Chassis.Pose.Position.Z), Jet.Chassis.Pose.Orientation);
                _missileModelNodesOne.SetLastPose(true);
                _missileModelNodesOne.PoseWorld = posesOne;
                increment = 0;
                _destinationZ = 0.0f;
                _destinationsZ = 0.0f;
            }
            else if (!SpawnMissile)
            {

                if (FirstTime)
                {

                    _destinationsZ = -2;
                    _destinationsZPosition = _destinationsZ - Tolerance;
                    _speedMissile = Math.Abs(_destinationsZ);

                    #region

                    //if (_speedMissile <= 10)
                    //{
                    //    _speedInitialize = 2f;
                    //}
                    //else if (_speedMissile >= 10 && _speedMissile <= 20)
                    //{
                    //    _speedInitialize = 3f;
                    //}
                    //else if (_speedMissile >= 20 && _speedMissile <= 30)
                    //{
                    //    _speedInitialize = 4f;
                    //}
                    //else if (_speedMissile >= 30 && _speedMissile <= 40)
                    //{
                    //    _speedInitialize = 5f;

                    //}
                    //else if (_speedMissile >= 40 && _speedMissile <= 50)
                    //{
                    //    _speedInitialize = 6f;
                    //}
                    //else if (_speedMissile >= 50 && _speedMissile <= 60)
                    //{
                    //    _speedInitialize = 7f;
                    //}
                    //else if (_speedMissile >= 60 && _speedMissile <= 70)
                    //{
                    //    _speedInitialize = 8f;
                    //}
                    //else if (_speedMissile >= 80 && _speedMissile <= 90)
                    //{
                    //    _speedInitialize = 9f;
                    //}
                    //else if (_speedMissile >= 90 && _speedMissile <= 100)
                    //{
                    //    _speedInitialize = 10f;
                    //}
                    //else if (_speedMissile >= 100 && _speedMissile <= 110)
                    //{
                    //    _speedInitialize = 11f;
                    //}
                    //else if (_speedMissile >= 110 && _speedMissile <= 120)
                    //{
                    //    _speedInitialize = 12f;
                    //}
                    //else if (_speedMissile >= 120 && _speedMissile <= 130)
                    //{
                    //    _speedInitialize = 13f;
                    //}
                    //else if (_speedMissile >= 130 && _speedMissile <= 140)
                    //{
                    //    _speedInitialize = 14f;
                    //}
                    //else if (_speedMissile >= 140 && _speedMissile <= 150)
                    //{
                    //    _speedInitialize = 15f;
                    //}
                    //else if (_speedMissile >= 160 && _speedMissile <= 170)
                    //{
                    //    _speedInitialize = 16f;
                    //}

                    #endregion

                }
                FirstTime = false;
                _destinationsZ += -_speedMissile;
                _missileModelNodes.SetLastPose(true);
                _missileModelNodes.PoseWorld = new Pose(new Vector3F(Jet.Chassis.Pose.Position.X, Jet.Chassis.Pose.Position.Y, _destinationsZ), Jet.Chassis.Pose.Orientation);


                _missileModelNodesOne.SetLastPose(true);
                _missileModelNodesOne.PoseWorld = new Pose(new Vector3F(Jet.Chassis.Pose.Position.X, Jet.Chassis.Pose.Position.Y, _destinationsZ), Jet.Chassis.Pose.Orientation);


                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (Math.Round(_destinationsZ) == Math.Round(_destinationsZPosition))
                {
                    //_missileModelNodes.IsEnabled = false;
                    //_missileModelNodesOne.IsEnabled = false;
                    //_missileModelNodes.SafeDispose();
                    //_missileModelNodesOne.SafeDispose();
                    _vehicleModelNode.Children.Remove(_missileModelNodesOne);
                    _vehicleModelNode.Children.Remove(_missileModelNodes);
                }
            }
            // Animate emissive color of material and point light intensity.
            _emissiveColorBinding.Value = new Vector3(_glowIntensity.Value);
            _pointLight.DiffuseIntensity = _glowIntensity.Value;
            _pointLight.SpecularIntensity = _glowIntensity.Value;
        }

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

        private void UpdateRotation()
        {
            // Limit rotation
            if (_fRotation > 0.01f)
            {
                _fRotation = 0.01f;
                _fRotationPosition = "1";
            }
            else if (_fRotation < -0.01f)
            {
                _fRotation = -0.01f;
                _fRotationPosition = "2";
            }

            // Slow rotation
            if (_fRotation > 0.0f)
            {
                _fRotation -= 0.0005f;
                _fRotationPosition = "3";
            }
            else if (_fRotation < 0.0f)
            {
                _fRotation += 0.0005f;
                _fRotationPosition = "4";
            }

            // Apply rotation
            //v3FRotation.Y += fRotation;

        }

        private void UpdateSteeringAngle(float deltaTime)
        {
            // TODO: Reduce max steering angle at high speeds.
                     
            if (_inputService.IsDown(Keys.A))
                //direction = 1;
                _fRotation += 0.01f;
            if (_inputService.IsDown(Keys.D))
                _fRotation -= 0.01f;
            //direction = -1;

            UpdateRotation();
                 

            Vehicle.SetCarSteeringAngle(_fRotation, Jet.Wheels[0], Jet.Wheels[1], Jet.Wheels[2], Jet.Wheels[3]);
        }


        private void UpdateAcceleration(float deltaTime)
        {
            const float MaxForce = 2000;
            const float AccelerationRate = 10000;

            // We limit the amount of change per frame.
            float change = AccelerationRate * deltaTime;

            float direction = 0;
            if (_inputService.IsDown(Keys.W))
                direction += 1;
            if (_inputService.IsDown(Keys.S))
                direction -= 1;

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
            Jet.Wheels[0].MotorForce = _motorForce;
            Jet.Wheels[1].MotorForce = _motorForce;
            Jet.Wheels[2].MotorForce = _motorForce;
            Jet.Wheels[3].MotorForce = _motorForce;
        }
        #endregion


        private void UpdateProfiler()
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("Position: " + Jet.Chassis.Pose.Position);
            _updateFpsTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("Orientation: " + Jet.Chassis.Pose.Orientation);
            _OrientationTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("Pitch: " + _fPitch);
            _drawFpsTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("Rotation: " + _fRotation);
            _RotationTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("_fRotationPosition: " + _fRotationPosition);
            _fRotationPositionBlock.Text = _stringBuilder.ToString();
        }

        internal static void InitializeDefaultXnaLights(Scene scene)
        {
            var ambientLight = new AmbientLight
            {
                Color = new Vector3F(0.05333332f, 0.09882354f, 0.1819608f),
                Intensity = 1,
                HemisphericAttenuation = 0,
            };
            scene.Children.Add(new LightNode(ambientLight));

            var keyLight = new DigitalRune.Graphics.DirectionalLight
            {
                Color = new Vector3F(1, 0.9607844f, 0.8078432f),
                DiffuseIntensity = 1,
                SpecularIntensity = 1,
            };
            var keyLightNode = new LightNode(keyLight)
            {
                Name = "KeyLight",
                Priority = 10,   // This is the most important light.
                PoseWorld = new Pose(QuaternionF.CreateRotation(Vector3F.Forward, new Vector3F(-0.5265408f, -0.5735765f, -0.6275069f))),
            };
            scene.Children.Add(keyLightNode);

            var fillLight = new DigitalRune.Graphics.DirectionalLight
            {
                Color = new Vector3F(0.9647059f, 0.7607844f, 0.4078432f),
                DiffuseIntensity = 1,
                SpecularIntensity = 0,
            };
            var fillLightNode = new LightNode(fillLight)
            {
                Name = "FillLight",
                PoseWorld = new Pose(QuaternionF.CreateRotation(Vector3F.Forward, new Vector3F(0.7198464f, 0.3420201f, 0.6040227f))),
            };
            scene.Children.Add(fillLightNode);

            var backLight = new DigitalRune.Graphics.DirectionalLight
            {
                Color = new Vector3F(0.3231373f, 0.3607844f, 0.3937255f),
                DiffuseIntensity = 1,
                SpecularIntensity = 1,
            };
            var backLightNode = new LightNode(backLight)
            {
                Name = "BackLight",
                PoseWorld = new Pose(QuaternionF.CreateRotation(Vector3F.Forward, new Vector3F(0.4545195f, -0.7660444f, 0.4545195f))),
            };
            scene.Children.Add(backLightNode);
        }
        #region


        #endregion
    }
}
