using System;
using System.Collections.Generic;
using System.Text;
using DigitalRune;
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
using DigitalRune.Mathematics.Statistics;
using DigitalRune.Particles;
using DigitalRune.Physics;
using JohnStriker.GraphicsScreen;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects.OpponentObjects
{
    public class OpponentsDynamicObjects : GameObject
    {
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
        private static readonly float SlopeLimit = MathHelper.ToRadians(45);

        #endregion

        #region Fields

        //--------------------------------------------------------------

        // Current velocity from gravity.

        private const float LinearVelocityMagnitude = 5f;

        private const float M16DSpeed = 10.0f;
        private const int FrameStringAndEnemyCount = 1;
        private readonly IParticleSystemService ParticleSystemService;

        private readonly TextBlock _OrientationTextBlock;

        private readonly TextBlock _PitchTextBlock;
        private readonly TextBlock _RotationTextBlock;
        private readonly List<KeyframedObjectAnimations> _animation = new List<KeyframedObjectAnimations>();
        private readonly List<RigidBody> _bodies = new List<RigidBody>();
        private readonly RigidBody _bodyPrototype;
        public readonly CameraNode _cameraNode;
        private readonly CollisionObject _collisionObject;
        private readonly TextBlock _drawFpsTextBlock;
        private readonly TextBlock _fRotationPositionBlock;
        private readonly GeometricObject _geometricObject;
        private readonly MyGraphicsScreen _graphicsScreen;
        // Sine wave information (floating behavior)

        private readonly ParticleSystem _jetFlame;
        private readonly ModelNode _modelPrototype;
        private readonly List<ModelNode> _models = new List<ModelNode>();

        private readonly ParticleSystemNode _particleSystemNode;
        private readonly Simulation _simulation;
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly TextBlock _updateFpsTextBlock;
        private readonly CollisionDomain collisionDomain;

        private readonly IGraphicsService graphicsService;
        public Pose FlamePose;
        private int ModelNumber;

        private float ScaleModel = 0.0f;
        private Vector3F _desiredPosition;
        private bool _drawDebugInfo; // true if the collision shapes should be drawn for debugging.
        private float _fRoll;
        private float _fRotation;
        private float _fThrusters;
        private Vector3F _gravityVelocity;

        // Current velocity from jumping.
        private Vector3F _jumpVelocity;

        // The last valid position (set at the beginning of Move()).
        private Vector3F _oldPosition;
        private float _pitch;

        private IServiceLocator _services;
        private Vector3F _shipMovement;

        private Vector3F _shipRotation;
        private float _yaw;
        private float fFloatStep = 0.0f;

        private float fLastFloat = 0.0f;
        private float fPitch;

        private IScene scene;


        #endregion

        #region Properties

        //--------------------------------------------------------------

        // The collision object used for collision detection. 
        private readonly IInputService _inputService;

        public CollisionObject CollisionObject
        {
            get { return _collisionObject; }
        }

        public Pose Pose
        {
            get { return _modelPrototype.PoseWorld; }
            set
            {
                if (_modelPrototype.Parent != null)
                {
                    _modelPrototype.PoseWorld = value;
                    _geometricObject.Pose = value;
                }
            }
        }


        // The bottom position (the lowest point on the capsule).

        #endregion

        #region Creation & Cleanup

        //--------------------------------------------------------------

        public OpponentsDynamicObjects(IServiceLocator services)
        {
            _services = services;
          

            var contentManager = services.GetInstance<ContentManager>();
            _inputService = services.GetInstance<IInputService>();
            _graphicsScreen = new MyGraphicsScreen(services) { DrawReticle = true };
            _simulation = _services.GetInstance<Simulation>();
            graphicsService = services.GetInstance<IGraphicsService>();
            // Add the GuiGraphicsScreen to the graphics service.
            var _guiGraphicsScreen = new GuiScreenOpponent(services);
            graphicsService.Screens.Add(_guiGraphicsScreen);


            // ----- FPS Counter (top right)
            var fpsPanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
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


            var FrameString = new List<ObjectAnimationFrames>();
            //for (int i = 0; i < FrameStringAndEnemyCount; i++)
            //{
            //    FrameString.Add(new ObjectAnimationFrames("thrusters forward", "", "", TimeSpan.FromSeconds(0)));
            //    FrameString.Add(new ObjectAnimationFrames("thrusters forward", "rotate left", "",
            //        TimeSpan.FromSeconds(15)));
            //    FrameString.Add(new ObjectAnimationFrames("", "rotate left", "", TimeSpan.FromSeconds(20)));
            //    FrameString.Add(new ObjectAnimationFrames("thrusters forward", "", "", TimeSpan.FromSeconds(23)));
            //    FrameString.Add(new ObjectAnimationFrames("thrusters forward", "rotate right", "",
            //        TimeSpan.FromSeconds(25)));
            //    FrameString.Add(new ObjectAnimationFrames("", "rotate right", "", TimeSpan.FromSeconds(28)));
            //    FrameString.Add(new ObjectAnimationFrames("thrusters forward", "", "", TimeSpan.FromSeconds(30)));

            //    _animation.Add(new KeyframedObjectAnimations(FrameString, true));
            //}
            //FrameString.RemoveRange(7, FrameString.Count - 7);



            _bodyPrototype = new RigidBody(new BoxShape(5, 2, 5));

            _bodyPrototype.MotionType = MotionType.Dynamic;

            _modelPrototype = contentManager.Load<ModelNode>("M16D/skyfighter fbx").Clone();

         

            // The collision shape is stored in the UserData.
            var shape = (Shape)_modelPrototype.UserData;
            //var shape = contentManager.Load<Shape>("Ship/Ship_CollisionModel");

            _geometricObject = new GeometricObject(shape, _modelPrototype.PoseWorld);
            // Create a collision object for the game object and add it to the collision domain.
            _collisionObject = new CollisionObject(_geometricObject);

            // Important: We do not need detailed contact information when a collision
            // is detected. The information of whether we have contact or not is sufficient.
            // Therefore, we can set the type to "Trigger". This increases the performance 
            // dramatically.
            _collisionObject.Type = CollisionObjectType.Trigger;

            // Add the collision object to the collision domain of the game.      
            collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject);

            //var poses = new Pose(new Vector3F(0, 2, -50), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));
            //Spawn(poses);

            _shipMovement = Vector3F.Zero;
            _shipRotation = Vector3F.Zero;
        }

        #endregion

        #region Methods

        //--------------------------------------------------------------

        // Move the character to the new desired position, sliding along obstacles and stepping 
        // automatically up and down. Gravity is applied.
        protected override void OnUpdate(TimeSpan timeSpan)
        {
            if (!_inputService.EnableMouseCentering)
                return;

            // Get elapsed time.
            var deltaTime = (float)timeSpan.TotalSeconds;

            for (int i = 0; i < _models.Count; i++)
            {
                ModelNode model = _models[i];
                RigidBody body = _bodies[i];

                if (body.Simulation != null)
                {
                    _simulation.RigidBodies.Remove(body);
                }


                _animation[0].Update(timeSpan);

                //Control(_animation[0].Position);
                //Control(_animation[0].Rotation);

                UpdateThrusters();

                UpdateRotation();

                UpdateRoll();

                UpdateFloat();

                UpdatePitch();


                QuaternionF cameraOrientation = QuaternionF.CreateRotationY(_shipRotation.Y) *
                                                QuaternionF.CreateRotationX(_shipRotation.X) *
                                                QuaternionF.CreateRotationZ(MathHelper.ToRadians(_shipRotation.Z));


                _shipRotation = new Vector3F(0, 0, _shipMovement.Z);


                _shipMovement = cameraOrientation.Rotate(_shipRotation);

                // Multiply the velocity by time to get the translation for this frame.
                Vector3F translation = _shipMovement * LinearVelocityMagnitude * deltaTime;

                model.SetLastPose(true);

                //body.Pose = CameraPose;
                body.Pose = new Pose(_modelPrototype.PoseWorld.Position + translation, cameraOrientation);
                model.PoseWorld = body.Pose;

                _simulation.RigidBodies.Add(body);


                Pose = model.PoseWorld;

                UpdateProfiler(Pose);
            }
        }

        public void Spawn(Pose startPose)
        {
            var scenes = _services.GetInstance<IScene>();
            _collisionObject.Enabled = true;

            ModelNode model = _modelPrototype.Clone();

            RigidBody body = _bodyPrototype.Clone();

            body.Pose = startPose;

            _bodyPrototype.Pose = body.Pose;

            model.PoseWorld = _bodyPrototype.Pose;

            scenes.Children.Add(model);

            _simulation.RigidBodies.Add(body);

            _models.Add(model);

            _bodies.Add(body);
        }

        public void Control(Vector3F sCommand)
        {
            string sCommands = " ";
            if (sCommand == new Vector3F(1, 1, 1))
            {
                sCommands = "thrusters forward";
            }
            else if (sCommand == new Vector3F(2, 2, 2))
            {
                sCommands = "rotate left";
            }
            else if (sCommand == new Vector3F(3, 3, 3))
            {
                sCommands = "rotate right";
            }
            else if (sCommand == new Vector3F(4, 4, 4))
            {
                sCommands = "ascend";
            }
            // Switch statement to evaluate our command
            switch (sCommands)
            {
                case "rotate right":
                    _fRotation -= 0.01f;
                    _fRoll -= 0.04f;
                    break;
                case "rotate left":
                    _fRotation += 0.01f;
                    _fRoll += 0.04f;
                    break;
                case "thrusters forward":
                    _fThrusters -= 0.05f;
                    break;
                case "thrusters backward":
                    _fThrusters += 0.05f;
                    break;
            }
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

        private void UpdateProfiler(Pose pose)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("Position: " + pose.Position);
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

        public void Dispose()
        {
            if (_modelPrototype.Parent != null)
            {
                _modelPrototype.Parent.Children.Remove(_modelPrototype);
                scene.Children.Remove(_modelPrototype);
                _modelPrototype.PoseWorld = new Pose();
                collisionDomain.CollisionObjects.Clear();
                _collisionObject.Enabled = false;
                _collisionObject.Domain.SafeDispose();
                _modelPrototype.Dispose(false);
            }
        }

        #endregion

        //--------------------------------------------------------------

        //--------------------------------------------------------------

        //--------------------------------------------------------------

        //--------------------------------------------------------------

        //--------------------------------------------------------------
    }
}