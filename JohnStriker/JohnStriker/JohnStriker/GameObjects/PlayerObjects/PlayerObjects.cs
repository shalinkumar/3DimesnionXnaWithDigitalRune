using System;
using System.Text;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Geometry;
using DigitalRune.Geometry.Collisions;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Particles;
using DigitalRune.Physics;
using JohnStriker.GraphicsScreen;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects.PlayerObjects
{

    public class PlayerObjects : GameObject
    {
        //--------------------------------------------------------------
        #region Constants
        //--------------------------------------------------------------

        // The dimensions of the character controller capsule (in meter).
        private const float Height = 1.8f;
        private const float Width = 1;

        // The max. number of iterations of the outer loops, where new positions are tested.
        private const int IterationLimit = 2;

        // The max. number of iterations for finding a position in the permitted convex space.
        private const int SolverIterationLimit = 5;

        // We allow small penetrations between capsule and other objects. The reason is: If we do 
        // not allow penetrations then at the end of the character controller movement the capsule
        // does not touch anything. This is bad because then we do not know if the character 
        // is standing on the ground or if it is in the air.
        // So it is better to allow small penetrations so that ground and wall contacts are always
        // visible for the program.
        private const float AllowedPenetration = 0.01f;

        // The character can move up inclined planes. If the inclination is higher than this value
        // the character will not move up.
        private static readonly float SlopeLimit = MathHelper.ToRadians(45);

        // Height limit for stepping up/down.
        // Up steps: The character automatically tries to move up low obstacles/steps. To move up onto 
        // a step it is necessary that the obstacle is not higher than this value and that there is 
        // enough space for the character to stand on. 
        // Down steps: If the character loses contact with the ground it tries to step down onto solid
        // ground. If it cannot find ground within the step height, it  will simply fall in a ballistic
        // curve (defined by gravity). Example, where this is useful: If the character moves 
        // horizontally on an inclined plane, it will always touch the plane. But, if the step height is 
        // set to <c>0</c>, the character will not try to step down and instead will "fall" down the
        // plane on short ballistic curves.    
        private const float StepHeight = 0.3f;
        #endregion


        //--------------------------------------------------------------
        #region Fields
        //--------------------------------------------------------------

   

        // Current velocity from gravity.
        private Vector3F _gravityVelocity;

        // Current velocity from jumping.
        private Vector3F _jumpVelocity;

        // The last valid position (set at the beginning of Move()).
        private Vector3F _oldPosition;

        // The desired target position (set in Move()).
        private Vector3F _desiredPosition;

        private ModelNode _modelNode;

        private float _yaw;

        private float _pitch;

        private const float LinearVelocityMagnitude = 5f;

        public readonly CameraNode _cameraNode;

        private float _fRotation;

        // Pitch information
        private float fPitch = 0.0f;

        private float _fRoll;

        private bool _drawDebugInfo;   // true if the collision shapes should be drawn for debugging.

        private readonly MyGraphicsScreen _graphicsScreen;

        // Movement thrusters
        private float _fThrusters = 0.0f;

        private const float M16DSpeed = 10.0f;

        private Vector3F _shipMovement;

        private Vector3F _shipRotation;

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly TextBlock _updateFpsTextBlock;

        private readonly TextBlock _drawFpsTextBlock;

        private TextBlock _RotationTextBlock;

        private TextBlock _OrientationTextBlock;

        private TextBlock _fRotationPositionBlock;

        private TextBlock _PitchTextBlock;
        // Sine wave information (floating behavior)
        private float fFloatStep = 0.0f;

        private float fLastFloat = 0.0f;

        public Pose FlamePose;

        private readonly ParticleSystem _jetFlame;

        private readonly ParticleSystemNode _particleSystemNode;

        private readonly IParticleSystemService ParticleSystemService;

        private readonly IGraphicsService graphicsService;

        private float ScaleModel = 0.0f;

        private RigidBody _rigidBody;

        private IServiceLocator _services;

        private GeometricObject _geometricObject;

        private CollisionObject _collisionObject;

        #endregion


        //--------------------------------------------------------------
        #region Properties
        //--------------------------------------------------------------

        // The geometric object of the character.
        public GeometricObject GeometricObject { get; private set; }


        // The collision object used for collision detection. 
        public CollisionObject CollisionObject
        {
            get { return _collisionObject; }
        }

        public Pose Pose
        {
            get { return _modelNode.PoseWorld; }
            set
            {
                _modelNode.PoseWorld = value;
                _geometricObject.Pose = value;
            }
        }

        private readonly IInputService _inputService;


        // The bottom position (the lowest point on the capsule).
        public Vector3F Position
        {
            get
            {
                return GeometricObject.Pose.Position - new Vector3F(0, Height / 2, 0);
            }
            set
            {
                Pose oldPose = GeometricObject.Pose;
                Pose newPose = new Pose(value + new Vector3F(0, Height / 2, 0), oldPose.Orientation);
                GeometricObject.Pose = newPose;

                // Note: GeometricObject.Pose is a struct. That means we cannot simply set
                //   GeometricObject.Pose.Position = value + new Vector3F(0, Height / 2, 0);
                // We need to set the whole struct.
            }
        }
        #endregion


        //--------------------------------------------------------------
        #region Creation & Cleanup
        //--------------------------------------------------------------

        public PlayerObjects(IServiceLocator services)
        {         
            _services = services;
            // Create a game object for the character controller.
            //      GeometricObject = new GeometricObject(
            //        new CapsuleShape(Width / 2, Height),
            //        new Pose(new Vector3F(0, Height / 2, 0)));
            var contentManager = services.GetInstance<ContentManager>();
            _inputService = services.GetInstance<IInputService>();
            _graphicsScreen = new MyGraphicsScreen(services) { DrawReticle = true };

            graphicsService = services.GetInstance<IGraphicsService>();
            // Add the GuiGraphicsScreen to the graphics service.
            GuiScreenBoat _guiGraphicsScreen = new GuiScreenBoat(services);
            graphicsService.Screens.Add(_guiGraphicsScreen);

            ParticleSystemService = services.GetInstance<IParticleSystemService>();

            _jetFlame = JetFlame.JetFlame.Create(contentManager);

            // ----- FPS Counter (top right)
            StackPanel fpsPanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            _guiGraphicsScreen.UIScreen.Children.Add(fpsPanel);
            _updateFpsTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Position",
            };
            fpsPanel.Children.Add(_updateFpsTextBlock);

            _drawFpsTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "FThrusters",
            };
            fpsPanel.Children.Add(_drawFpsTextBlock);
            _RotationTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "FRotation",
            };
            fpsPanel.Children.Add(_RotationTextBlock);
            _fRotationPositionBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "_fRotationPosition",
            };
            fpsPanel.Children.Add(_fRotationPositionBlock);
            _OrientationTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Froll",
            };
            fpsPanel.Children.Add(_OrientationTextBlock);
            _PitchTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "FPitch",
            };
            fpsPanel.Children.Add(_PitchTextBlock);

            // Create a camera.
            var projection = new PerspectiveProjection();
            projection.SetFieldOfView(
              ConstantsF.PiOver4,
              graphicsService.GraphicsDevice.Viewport.AspectRatio,
              0.1f,
              1000.0f);
            _cameraNode = new CameraNode(new Camera(projection));
            _graphicsScreen.ActiveCameraNode = _cameraNode;
      
            // A simple cube.
            _rigidBody = new RigidBody(new BoxShape(5, 2, 5));

            // ----- Graphics
            // Load a graphics model and add it to the scene for rendering.   
            int modelNumber = 4;
            switch (modelNumber)
            {
                case 1: //hovercraft
                    _modelNode = contentManager.Load<ModelNode>("1381968_hovercraft/hovercraft").Clone();
                    _modelNode.ScaleLocal = new Vector3F(0.003f);
                    break;

                case 2: //hydro plane
                    _modelNode = contentManager.Load<ModelNode>("3734734_hydro plane/hydro plane").Clone();
                    _modelNode.ScaleLocal = new Vector3F(0.01f);
                    break;

                case 3: //SpeedBoat
                    _modelNode = contentManager.Load<ModelNode>("1336214_Speed Boat/SpeedBoat").Clone();
                    _modelNode.ScaleLocal = new Vector3F(0.002f);
                    break;

                case 4: //CabinCruiser
                    _modelNode = contentManager.Load<ModelNode>("1263879_Cabin Cruiser/CabinCruiser").Clone();
                    _modelNode.ScaleLocal = new Vector3F(0.002f);
                    break;

            }
        

            //_modelNode = contentManager.Load<ModelNode>("5086_Boat/5086Boat").Clone();
            //_modelNode = contentManager.Load<ModelNode>("uscg_rb_hs_fast_boat/boat").Clone();

            //   _modelNode.ScaleLocal = new Vector3F(0.3299999f); //5086Boat
            //_modelNode.ScaleLocal = new Vector3F(1.00445569F);
            _rigidBody.Pose = new Pose(new Vector3F(0, 0, 10), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));
            _modelNode.PoseWorld = _rigidBody.Pose;

         

            // Add rigid body to physics simulation and model to scene.
            var simulation = _services.GetInstance<Simulation>();
            simulation.RigidBodies.Add(_rigidBody);

            var scene = services.GetInstance<IScene>();
            scene.Children.Add(_modelNode);

            // Load collision shape from a separate model (created using the CollisionShapeProcessor).
            var shape = contentManager.Load<Shape>("1263879_Cabin Cruiser/CabinCruiser_Collision");

            _geometricObject = new GeometricObject(shape, _modelNode.PoseWorld);
            // Create a collision object for the game object and add it to the collision domain.
            _collisionObject = new CollisionObject(_geometricObject);

            // Important: We do not need detailed contact information when a collision
            // is detected. The information of whether we have contact or not is sufficient.
            // Therefore, we can set the type to "Trigger". This increases the performance 
            // dramatically.
            _collisionObject.Type = CollisionObjectType.Trigger;

            // Add the collision object to the collision domain of the game.
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject);

            _shipMovement = Vector3F.Zero;
            _shipRotation = Vector3F.Zero;

        }
        #endregion


        //--------------------------------------------------------------
        #region Methods
        //--------------------------------------------------------------

        // Move the character to the new desired position, sliding along obstacles and stepping 
        // automatically up and down. Gravity is applied.
        protected override void OnUpdate(TimeSpan timeSpan)
        {
            // Get elapsed time.
            if (!_inputService.EnableMouseCentering)
                return;

            float deltaTime = (float)timeSpan.TotalSeconds;

            float deltaYaw = -_inputService.MousePositionDelta.X;

            _yaw += deltaYaw * deltaTime * 0.1f;
            float deltaPitch = -_inputService.MousePositionDelta.Y;
            //_pitch += deltaPitch * deltaTime * 0.1f;

            // Limit the pitch angle.
            //_pitch = MathHelper.Clamp(_pitch, -ConstantsF.PiOver2, ConstantsF.PiOver2);
            // _yaw = MathHelper.Clamp(_yaw, -ConstantsF.PiOver2, ConstantsF.PiOver2);
            // Compute new orientation of the camera.

         

            // Move second ship with arrow keys.         
            KeyboardState keyboardState = _inputService.KeyboardState;
            if (keyboardState.IsKeyDown(Keys.A))
                _fThrusters -= 0.05f;
            if (keyboardState.IsKeyDown(Keys.Z))
                _fThrusters += 0.05f;
            if (_inputService.IsDown(Keys.Left))
            {
                _fRotation += 0.01f;
                _fRoll += 0.04f;
            }
            if (_inputService.IsDown(Keys.Right))
            {
                _fRotation -= 0.01f;
                _fRoll -= 0.04f;
            }
            if (keyboardState.IsKeyDown(Keys.Q))
                fPitch += 0.005f;
            if (keyboardState.IsKeyDown(Keys.E))
                fPitch -= 0.005f;
            //if (keyboardState.IsKeyDown(Keys.R))
            //{
            //    ScaleModel += 0.005f;
            //}
            //if (keyboardState.IsKeyDown(Keys.T))
            //{
            //    ScaleModel -= 0.005f;
            //}

            UpdateThrusters();

            UpdateRotation();

            UpdateRoll();

            UpdateFloat();

            UpdatePitch();

            //            //shipMovement.X = _fRotation;
            QuaternionF cameraOrientation = QuaternionF.CreateRotationY(_shipRotation.Y) * QuaternionF.CreateRotationX(_shipRotation.X) * QuaternionF.CreateRotationZ(MathHelper.ToRadians(_shipRotation.Z));
            //            QuaternionF cameraOrientation = QuaternionF.CreateRotationY(_shipRotation.Y) * QuaternionF.CreateRotationX(_shipRotation.X) ;


            _shipRotation = new Vector3F(0, 0, _shipMovement.Z);



            _shipMovement = cameraOrientation.Rotate(_shipRotation);

            // Multiply the velocity by time to get the translation for this frame.
            Vector3F translation = _shipMovement * LinearVelocityMagnitude * deltaTime;

            UpdateProfiler();

       
            // Set the new camera pose.
            //_rigidBody.Pose = new Pose(_modelNode.PoseWorld.Position + translation, cameraOrientation);
            _rigidBody.Pose = new Pose(_rigidBody.Pose.Position + translation, cameraOrientation);
            Pose = _rigidBody.Pose;
            // ----- Set view matrix for graphics.
            // For third person we move the eye position back, behind the body (+z direction is 
            // the "back" direction).
            Vector3F thirdPersonDistance = cameraOrientation.Rotate(new Vector3F(0, 0, 10));

            // Compute camera pose (= position + orientation). 
            _cameraNode.PoseWorld = new Pose
            {
                Position = _rigidBody.Pose.Position         // Floor position of character
                           + new Vector3F(0, 2.6f, 0)  // + Eye height
                           + thirdPersonDistance,
                Orientation = cameraOrientation.ToRotationMatrix33()
            };


        }

        private void UpdateThrusters()
        {

            // Limit thrusters
            if (_fThrusters > M16DSpeed)
            {
                //fThrusters = 2.0f;
                _fThrusters = M16DSpeed;
            }
            else if (_fThrusters < -M16DSpeed)
            {
                //fThrusters = -2.0f;
                _fThrusters = -M16DSpeed;
            }

            // Slow thrusters
            if (_fThrusters > 0.0f)
            {
                _fThrusters -= 0.0025f;
            }
            else if (_fThrusters < 0.0f)
            {
                _fThrusters += 0.0025f;
            }

            // Stop thrusters
            if (Math.Abs(_fThrusters) < 0.0005)
            {
                _fThrusters = 0.0f;
            }

            // Apply thrusters
            //v3Position.X += -(float)Math.Sin(v3Rotation.Y) * _fThrusters;
            //v3Position.Z += -(float)Math.Cos(v3Rotation.Y) * _fThrusters;


            //_shipMovement.X = -(float)Math.Sin(_shipRotation.Y) * _fThrusters;
            //_shipMovement.Z = -(float)Math.Sin(_shipRotation.Y) * _fThrusters;

            _shipMovement.X = _fThrusters;
            _shipMovement.Z = _fThrusters;

        }

        private void UpdateRotation()
        {

            // Limit rotation
            //if (_fRotation >= 10f)
            //{
            //    _fRotation = 10f;
            //}
            //else if (_fRotation <= -10f)
            //{
            //    _fRotation = -10f;
            //}

            //// Slow rotation
            //if (_fRotation > 0.0f)
            //{
            //    _fRotation -= 0.0015f;                
            //}
            //else if (_fRotation < 0.0f)
            //{
            //    _fRotation += 0.0015f;               
            //}

            // Apply rotation
            _shipRotation.Y += _fRotation;
        }

        private void UpdateRoll()
        {
            if (_fRoll >= 6)
            {
                _fRoll = 6;
            }
            else if (_fRoll <= -6)
            {
                _fRoll = -6;
            }

            // Slow rotation
            if (_fRoll > 0)
            {
                _fRoll -= 0.01f;
            }
            else if (_fRoll < 0)
            {
                _fRoll += 0.01f;
            }
            //original 
            //v3Rotation.Z = (fRotation * fThrusters) * 15.0f;
            //my changes --- i changed this for the terrain vibration when it turning from high speed
            //_shipRotation.Z = (_fRoll * _fRotation);
            _shipRotation.Z = -(_fRoll * _fThrusters);
            //_shipRotation.Z = -(_fRoll * _fThrusters);
            //_shipRotation.Z = -(_fRotation * _fThrusters);
        }

        private void UpdatePitch()
        {

            if (fPitch > 0.10f)
            {
                fPitch = 0.10f;
            }
            else if (fPitch < -0.10f)
            {
                fPitch = -0.10f;
            }

            // Slow rate of pitch
            if (fPitch > 0.0f)
            {
                fPitch -= 0.0005f;
            }
            else if (fPitch < 0.0f)
            {
                fPitch += 0.0005f;
            }

            // Update position and pitch
            _shipMovement.Y += fPitch;
            //Original lines
            //_shipRotation.X = (fPitch * fThrusters) / 4.0f;
            //My changes
            _shipRotation.X = (fPitch);

        }

        private void UpdateFloat()
        {

            // Increase the step
            //fFloatStep += 0.01f;

            //// Store new sine wave value
            //float fVariation = 10.0f * (float)Math.Sin(fFloatStep);

            //// Alter the dirigible's position
            //_shipMovement.Y -= fLastFloat;
            //_shipMovement.Y += fVariation;

            //// Store old sine wave value
            //fLastFloat = fVariation;

        }

        private void UpdateProfiler()
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("Position: " + _shipMovement);
            _updateFpsTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("FThrusters: " + _fThrusters);
            _drawFpsTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("FRotation: " + _fRotation);
            _RotationTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("ShipRotation.Z: " + _shipRotation.Z);
            _fRotationPositionBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("FRoll: " + _fRoll);
            _OrientationTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("FPitch: " + fPitch);
            _PitchTextBlock.Text = _stringBuilder.ToString();
        }

        #endregion

    }
}
