using DigitalRune.Animation;
using DigitalRune.Animation.Easing;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Geometry;
using DigitalRune.Geometry.Collisions;
using DigitalRune.Geometry.Meshes;
using DigitalRune.Geometry.Partitioning;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Particles;
using DigitalRune.Physics;
using DigitalRune.Physics.Constraints;
using JohnStriker.GameObjects.OpponentObjects;
using JohnStriker.GraphicsScreen;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects.PlayerObjects
{
    public class Player : GameObject
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
        private const float MaxSoundChangeSpeed = 3f;
        private const float MaxDistance = 140;

        // Contact forces below MinHitForce do not make a sound.
        private const float MinHitForce = 20000;
        private readonly IAnimationService _animationService;
        private readonly IGameObjectService GameObjectService;
        private readonly IParticleSystemService ParticleSystemService;
        private readonly TextBlock _OrientationTextBlock;
        private readonly TextBlock _PitchTextBlock;
        private readonly TextBlock _RotationTextBlock;
        private readonly List<RigidBody> _bodies = new List<RigidBody>();

        public readonly CameraNode _cameraNodeMissile;
        private readonly CollisionObject _collisionObject;

        private CollisionObject _attachedMissileCollisionObject;

        private readonly TextBlock _drawFpsTextBlock;
        private readonly TextBlock _fRotationPositionBlock;
        private readonly AnimatableProperty<float> _frontWheelSteeringAngle = new AnimatableProperty<float>();
        private readonly GeometricObject _geometricObject;
        private GeometricObject _geometricObjectMissile;
        private readonly MyGraphicsScreen _graphicsScreen;
        private readonly ParticleSystem _jetFlame;
        private readonly RigidBody _missileAttachedPrototype;
        public readonly ModelNode _modelNode;
        private readonly List<SceneNode> _models = new List<SceneNode>();
        private ParticleSystemNode _particleSystemNode;
        private readonly IServiceLocator _services;
        private readonly Simulation _simulation;
        private readonly StringBuilder _stringBuilder = new StringBuilder();


        private readonly IGraphicsService _graphicsService;
        private readonly Vector3F rollCenter = Vector3F.Zero;
        public Pose FlamePose;
        private RigidBody _bodyPrototype;
        public CameraNode _cameraNode;
        private Vector3F _desiredPosition;
        private bool _drawDebugInfo; // true if the collision shapes should be drawn for debugging.
        private float _fRoll;
        private float _fRotation;
        public float angle = 0;
        // Movement thrusters
        private float _fThrusters;
        private SceneNode _frontWheelLeft;
        private Pose _frontWheelLeftRestPose;
        private SceneNode _frontWheelRight;
        private Pose _frontWheelRightRestPose;
        private Vector3F _gravityVelocity;

        private SoundEffect _jetMovingSound;

        private SoundEffect _jetMovingSpeedSound;
        private AudioEmitter _jetmovingEmitter;
        private SoundEffectInstance _jetmovingSoundInstances;
        private AudioEmitter _jetmovingSpeedEmitter;
        private SoundEffectInstance _jetmovingSpeedSoundInstances;


        // Current velocity from jumping.
        private Vector3F _jumpVelocity;
        private AudioListener _listener;
        private List<RigidBody> _missileAttachedPrototypes;

        internal List<SceneNode> _missileAttachedSceneNode;

        private Pose _missileAttachedSceneNodePose;

        // The last valid position (set at the beginning of Move()).
        private Vector3F _oldPosition;
        private float _pitch;

        private Vector3F _shipMovement;

        private Vector3F _shipRotation;
        private float _soundIcrement;
        private float _timeSinceLastHitSound;
        private float _timeSinceLastjetSound;
        private bool _update = true;
        private float _yaw;

        private readonly IInputService _inputService;

        // Sine wave information (floating behavior)
        private float fFloatStep = 0.0f;

        private float fLastFloat = 0.0f;

        private float fPitch;

        private RayShape ray = new RayShape();

        private BallJoint _spring;

        private float _springAttachmentDistanceFromObserver;

        private float _animationKeyValuePlus = 0.0f;

        private float AnimationKeyValueMinus = 0.0f;

        private int j;

        private int k = 0;

        private List<RigidBody> _ammoAttachedRigidBody;

        private List<SceneNode> _ammoAttachedSceneNode;

        private Pose _ammoAttachedSceneNodePose;

        private GeometricObject _geometricObjectAmmo;

        private CollisionObject _attachedAmmoCollisionObject;

        private bool _isParticleSystemNode;

        private readonly ContentManager _contentManager;

        private float _zIncrement = 0.0f;

        private Vector3F thirdPersonDistance = new Vector3F();

        private PackedTexture _packedTexture;

        private ImageBillboard billboard;

        private BillboardNode billboardNode;

        private List<SceneNode> _healthBarSceneNodeList;

        private SceneNode _healthBarSceneNode0;

        private Pose _healthBarPose0;

        private SceneNode _healthBarSceneNode1;

        private Pose _healthBarPose1;

        private SceneNode _healthBarSceneNode2;

        private Pose _healthBarPose2;

        private SceneNode _healthBarSceneNode3;

        private Pose _healthBarPose3;

        private SceneNode _healthBarSceneNode4;

        private Pose _healthBarPose4;

        private SceneNode _healthBarSceneNode5;

        private Pose _healthBarPose5;

        private SceneNode _healthBarSceneNode6;

        private Pose _healthBarPose6;

        private SceneNode _healthBarSceneNode7;

        private Pose _healthBarPose7;

        private SceneNode _healthBarSceneNode8;

        private Pose _healthBarPose8;

        private SceneNode _healthBarSceneNode9;

        private Pose _healthBarPose9;

        private readonly AnimatableProperty<float> _healthBarPoseAngle = new AnimatableProperty<float>();

        private TimeSpan _timePassed;

        private readonly TextBlock _updateFpsTextBlock;

        private readonly TextBlock ammoTextBlock;

        private readonly ProgressBar ammoProgressBar;

        private readonly TextBlock missileTextBlock;

        private readonly ProgressBar missileProgressBar;

        private readonly TextBlock _MissileAlertTextBlock;

        private readonly ProgressBar thrustProgressBar;

        private readonly ProgressBar pitchProgressBar;

        private readonly TextBlock _MissileLockTextBlock;

        private readonly Slider _slider;

        private TimeSpan _timeUntilAddHealth = TimeSpan.Zero;

        private static readonly TimeSpan AddHealthInterval = TimeSpan.FromSeconds(30);

        #endregion

        #region Properties

        //--------------------------------------------------------------


        // The collision object used for collision detection. 
        // The collision object which can be used for contact queries.
        private int HitCount { get; set; }



        public CollisionObject CollisionObject
        {
            get { return _collisionObject; }
        }

        public CollisionObject AttachedMissileCollisionObject
        {
            get { return _attachedMissileCollisionObject; }
        }


        // The bottom position (the lowest point on the capsule).


        public Pose Pose
        {
            get { return _modelNode.PoseWorld; }
            set { _geometricObject.Pose = _modelNode.PoseWorld; }
        }

        private Pose PoseMissile
        {
            get
            {
                return _missileAttachedSceneNodePose;
            }
            set
            {
                foreach (var sceneNode in _missileAttachedSceneNode)
                {
                    if (sceneNode != null)
                        _geometricObjectMissile.Pose = sceneNode.PoseWorld;
                }

            }
        }


        public CollisionObject AttachedAmmoCollisionObject
        {
            get { return _attachedAmmoCollisionObject; }
        }

        private Pose PoseAmmo
        {
            get
            {
                return _ammoAttachedSceneNodePose;
            }
            set
            {
                foreach (var sceneNode in _ammoAttachedSceneNode)
                {
                    _geometricObjectAmmo.Pose = sceneNode.PoseWorld;
                }

            }
        }
        #endregion

        #region Creation & Cleanup

        //--------------------------------------------------------------

        public Player(IServiceLocator services)
        {
            Name = "VehicleOpponent";
            _services = services;
            _contentManager = services.GetInstance<ContentManager>();
            _simulation = services.GetInstance<Simulation>();
            _inputService = services.GetInstance<IInputService>();
            _graphicsScreen = new MyGraphicsScreen(services) { DrawReticle = true };
            _animationService = services.GetInstance<IAnimationService>();
            _graphicsService = services.GetInstance<IGraphicsService>();
            GameObjectService = services.GetInstance<IGameObjectService>();
            // Add the GuiGraphicsScreen to the graphics service.
            var _guiGraphicsScreen = new GuiCustomScreen(services, 3);
            _graphicsService.Screens.Add(_guiGraphicsScreen);

            ParticleSystemService = services.GetInstance<IParticleSystemService>();

            _jetFlame = JetFlame.JetFlame.Create(_contentManager);

            // ----- FPS Counter (top right)

            var fpsPanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            _guiGraphicsScreen.UIScreen.Children.Add(fpsPanel);
            var fpsMisilePanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
            };
            _guiGraphicsScreen.UIScreen.Children.Add(fpsMisilePanel);

            var fpsLockPanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            _guiGraphicsScreen.UIScreen.Children.Add(fpsLockPanel);
            _updateFpsTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Position",
            };
            fpsPanel.Children.Add(_updateFpsTextBlock);
            missileTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Missile",
            };
            fpsPanel.Children.Add(missileTextBlock);
            missileProgressBar = new ProgressBar
            {
                Value = 14,
                Maximum = 14,
                Width = 130,
                Height = 10,
            };
            fpsPanel.Children.Add(missileProgressBar);
            ammoTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Bullet",
            };
            fpsPanel.Children.Add(ammoTextBlock);
            ammoProgressBar = new ProgressBar
            {
                Value = 10,
                Maximum = 10,
                Width = 130,
                Height = 10,
            };
            fpsPanel.Children.Add(ammoProgressBar);

            _drawFpsTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Thrusters",
            };
            fpsPanel.Children.Add(_drawFpsTextBlock);
            thrustProgressBar = new ProgressBar
            {
                Value = 0,
                Maximum = 10,
                Width = 130,
                Height = 10,
            };
            fpsPanel.Children.Add(thrustProgressBar);
            _PitchTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Pitch",
            };
            fpsPanel.Children.Add(_PitchTextBlock);
            //pitchProgressBar = new ProgressBar
            //{
            //    Value = 0,
            //    Maximum = 6,
            //    Width = 130,
            //    Height = 10,
            //};
            _slider = new Slider
            {
                SmallChange = 0.01f,
                LargeChange = 0.1f,
                Minimum = -0.10f,
                Maximum = 0.10f,
                Width = 130,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            fpsPanel.Children.Add(_slider);
            //_RotationTextBlock = new TextBlock
            //{
            //    Font = "DejaVuSans",
            //    Foreground = Color.Black,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    Text = "FRotation",
            //};
            //fpsPanel.Children.Add(_RotationTextBlock);
            //_fRotationPositionBlock = new TextBlock
            //{
            //    Font = "DejaVuSans",
            //    Foreground = Color.Black,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    Text = "_fRotationPosition",
            //};
            //fpsPanel.Children.Add(_fRotationPositionBlock);
            //_OrientationTextBlock = new TextBlock
            //{
            //    Font = "DejaVuSans",
            //    Foreground = Color.Black,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    Text = "Froll",
            //};
            //fpsPanel.Children.Add(_OrientationTextBlock);



            _MissileAlertTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = "Incoming Missile is hot",
            };
            fpsMisilePanel.Children.Add(_MissileAlertTextBlock);

            _MissileLockTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            fpsLockPanel.Children.Add(_MissileLockTextBlock);
            // Create a camera.
            var projection = new PerspectiveProjection();
            projection.SetFieldOfView(
                ConstantsF.PiOver4,
                _graphicsService.GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                1000.0f);
            _cameraNode = new CameraNode(new Camera(projection));
            _graphicsScreen.ActiveCameraNode = _cameraNode;


            _cameraNodeMissile = new CameraNode(new Camera(projection));
            _graphicsScreen.ActiveCameraNode = _cameraNodeMissile;

            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));

            _missileAttachedPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));

            //_missileAttachedPrototypes = new List<RigidBody> { new RigidBody(new CapsuleShape(0f, 0.001f)) };

            //_bodyPrototype = new RigidBody(new BoxShape(5, 0, 5));


            // ----- Graphics
            // Load a graphics model and add it to the scene for rendering.
            //_modelNode = contentManager.Load<ModelNode>("M16D/skyfighter fbx").Clone();
            _modelNode = _contentManager.Load<ModelNode>("Player/PlayerFighter").Clone();
            //_modelNode = _contentManager.Load<ModelNode>("SEAPLANE A6M2N_L.3DS/SeaPlane_1").Clone();
            //_modelNode = _contentManager.Load<ModelNode>("SeaPlane_2/SeaPlane_2").Clone();
            //_missileAttachedSceneNode = new List<SceneNode> {_modelNode.GetSceneNode("Wasp_IRSwarm")};

            //CreateRigidBody();

            _bodyPrototype.Pose = new Pose(new Vector3F(15, 3, -5), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

            _modelNode.PoseWorld = _bodyPrototype.Pose;

            //_modelNode.PoseWorld = new Pose(new Vector3F(0, 3, -5), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

            var scene = services.GetInstance<IScene>();
            scene.Children.Add(_modelNode);

            //var simulation = _services.GetInstance<Simulation>();

            //simulation.RigidBodies.Add(_missileAttachedPrototypes[0]);
            //_jetFlame.Pose = new Pose(new Vector3F(0, 17, -5), Matrix33F.CreateRotationY(3.1f));

            //_jetFlame.Pose = new Pose(new Vector3F(0, 3,0), Matrix33F.CreateRotationY(-3.1f));

            ParticleSystemService.ParticleSystems.Add(_jetFlame);

            _particleSystemNode = new ParticleSystemNode(_jetFlame);


            //scene.Children.Add(_particleSystemNode);

            // Load collision shape from a separate model (created using the CollisionShapeProcessor).
            var shape = (Shape)_modelNode.UserData;
            //var shape = contentManager.Load<Shape>("Ship/Ship_CollisionModel");

            _geometricObject = new GeometricObject(shape, _modelNode.PoseWorld);
            // Create a collision object for the game object and add it to the collision domain.
            _collisionObject = new CollisionObject(_geometricObject);


            var collisionDomain = services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(CollisionObject);

            _shipMovement = Vector3F.Zero;
            _shipRotation = Vector3F.Zero;

            CreateAndStartAnimations(0.3f, 0.3f);

            _missileAttachedPrototypes = new List<RigidBody> { };

            _missileAttachedSceneNode = new List<SceneNode> { };


            _ammoAttachedRigidBody = new List<RigidBody> { };

            _ammoAttachedSceneNode = new List<SceneNode> { };


            //_packedTexture = new PackedTexture(_contentManager.Load<Texture2D>("Billboard/BillboardReference"));
            //billboard = new ImageBillboard(_packedTexture);
            //billboard.BlendMode = 0.333f;
            //billboard.AlphaTest = 0.9f;
            //billboardNode = new BillboardNode(billboard);
            //billboardNode.Name = "View plane-aligned\nVarying color\nVarying alpha";
            //billboardNode.Color = new Vector3F(0, 1, 0);
            //billboardNode.ScaleLocal = new Vector3F(3f);
            //scene.Children.Add(billboardNode);

        }

        private void CreateRigidBody()
        {
            var triangleMesh = new TriangleMesh();

            foreach (MeshNode meshNode in _modelNode.GetSubtree().OfType<MeshNode>())
            {
                // Extract the triangle mesh from the DigitalRune Graphics Mesh instance. 
                var subTriangleMesh = new TriangleMesh();
                foreach (Submesh submesh in meshNode.Mesh.Submeshes)
                {
                    submesh.ToTriangleMesh(subTriangleMesh);
                }

                // Apply the transformation of the mesh node.
                subTriangleMesh.Transform(meshNode.PoseWorld * Matrix44F.CreateScale(meshNode.ScaleWorld));

                // Combine into final triangle mesh.
                triangleMesh.Add(subTriangleMesh);
            }

            // Create a collision shape that uses the mesh.
            var triangleMeshShape = new TriangleMeshShape(triangleMesh);

            // Optional: Assign a spatial partitioning scheme to the triangle mesh. (A spatial partition
            // adds an additional memory overhead, but it improves collision detection speed tremendously!)
            triangleMeshShape.Partition = new CompressedAabbTree
            {
                BottomUpBuildThreshold = 0,
            };

            _bodyPrototype = new RigidBody(triangleMeshShape, new MassFrame(), null)
            {
                Pose = _modelNode.PoseWorld,
                Scale = _modelNode.ScaleLocal,
                MotionType = MotionType.Static
            };

            // Add rigid body to physics simulation and model to scene.           
            _simulation.RigidBodies.Add(_bodyPrototype);
        }

        private void CreateAndStartAnimations(float animationKeyValuePlus, float animationKeyValueminus)
        {
            // Get the scene nodes that we want to animate using their names (as defined 
            // in the .fbx file).
            _frontWheelLeft = _modelNode.GetSceneNode("PBE");
            _frontWheelLeftRestPose = _frontWheelLeft.PoseLocal;
            _frontWheelRight = _modelNode.GetSceneNode("PBE001");
            _frontWheelRightRestPose = _frontWheelRight.PoseLocal;

            _healthBarSceneNode0 = _modelNode.GetSceneNode("Box0");
            _healthBarPose0 = _healthBarSceneNode0.PoseLocal;
            _healthBarSceneNode1 = _modelNode.GetSceneNode("Box1");
            _healthBarPose1 = _healthBarSceneNode1.PoseLocal;
            _healthBarSceneNode2 = _modelNode.GetSceneNode("Box2");
            _healthBarPose2 = _healthBarSceneNode2.PoseLocal;
            _healthBarSceneNode3 = _modelNode.GetSceneNode("Box3");
            _healthBarPose3 = _healthBarSceneNode3.PoseLocal;
            _healthBarSceneNode4 = _modelNode.GetSceneNode("Box4");
            _healthBarPose4 = _healthBarSceneNode4.PoseLocal;
            _healthBarSceneNode5 = _modelNode.GetSceneNode("Box5");
            _healthBarPose5 = _healthBarSceneNode5.PoseLocal;
            _healthBarSceneNode6 = _modelNode.GetSceneNode("Box6");
            _healthBarPose6 = _healthBarSceneNode6.PoseLocal;
            _healthBarSceneNode7 = _modelNode.GetSceneNode("Box7");
            _healthBarPose7 = _healthBarSceneNode7.PoseLocal;
            _healthBarSceneNode8 = _modelNode.GetSceneNode("Box8");
            _healthBarPose8 = _healthBarSceneNode8.PoseLocal;
            _healthBarSceneNode9 = _modelNode.GetSceneNode("Box9");
            _healthBarPose9 = _healthBarSceneNode9.PoseLocal;
            // Create and start some animations. For general information about the DigitalRune Animation
            // system, please check out the user documentation and the DigitalRune Animation samples.

            // The front wheel should rotate left/right; oscillating endlessly.
            var frontWheelSteeringAnimation = new AnimationClip<float>(
                new SingleFromToByAnimation
                {
                    From = -animationKeyValueminus,
                    To = animationKeyValuePlus,
                    Duration = TimeSpan.FromSeconds(3),
                    EasingFunction = new SineEase { Mode = EasingMode.EaseInOut }
                })
            {
                Duration = TimeSpan.MaxValue,
                LoopBehavior = LoopBehavior.Oscillate,
            };
            _animationService.StartAnimation(frontWheelSteeringAnimation, _frontWheelSteeringAngle)
                .AutoRecycle();


            var healthBarSceneNodeAnimation = new AnimationClip<float>(
                new SingleFromToByAnimation
                {
                    From = -animationKeyValueminus,
                    To = animationKeyValuePlus,
                    Duration = TimeSpan.FromSeconds(5),
                    EasingFunction = new CircleEase { Mode = EasingMode.EaseInOut },
                })
            {
                Duration = TimeSpan.MaxValue,
                LoopBehavior = LoopBehavior.Oscillate,
            };
            _animationService.StartAnimation(healthBarSceneNodeAnimation, _healthBarPoseAngle)
               .AutoRecycle();


            Matrix33F healthBarPoseAngleRotation = Matrix33F.CreateRotationZ(_healthBarPoseAngle.Value);
            _healthBarSceneNodeList = new List<SceneNode>();
            _healthBarSceneNode0.PoseLocal = _healthBarPose0 * new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode0);
            _healthBarSceneNode1.PoseLocal = _healthBarPose1 * new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode1);
            _healthBarSceneNode2.PoseLocal = _healthBarPose2 * new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode2);
            _healthBarSceneNode3.PoseLocal = _healthBarPose3 * new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode3);
            _healthBarSceneNode4.PoseLocal = _healthBarPose4 * new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode4);
            _healthBarSceneNode5.PoseLocal = _healthBarPose5 * new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode5);
            _healthBarSceneNode6.PoseLocal = _healthBarPose6 * new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode6);
            _healthBarSceneNode7.PoseLocal = _healthBarPose7 * new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode7);
            _healthBarSceneNode8.PoseLocal = _healthBarPose8 * new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode8);
            _healthBarSceneNode9.PoseLocal = _healthBarPose9 * new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode9);

        }

        private static bool IsOdd(int value)
        {
            return value % 2 != 0;
        }


        protected override void OnUpdate(TimeSpan timeSpan)
        {
            if (!_inputService.EnableMouseCentering)
                return;

            if (!_update)
                return;

            var deltaTime = (float)timeSpan.TotalSeconds;

            _timePassed += TimeSpan.FromSeconds(timeSpan.TotalSeconds);

            _timeUntilAddHealth -= TimeSpan.FromSeconds(timeSpan.TotalSeconds);

            //if (_inputService.IsPressed(Keys.O, true))
            //{
            //    _animationKeyValuePlus += 0.01f;
            //    AnimationKeyValueMinus = 0.3f;
            //    CreateAndStartAnimations(_animationKeyValuePlus, AnimationKeyValueMinus);
            //}

            //if (_inputService.IsPressed(Keys.P, true))
            //{
            //    _animationKeyValuePlus -= 0.01f;
            //    AnimationKeyValueMinus = 0.3f;
            //    CreateAndStartAnimations(_animationKeyValuePlus, AnimationKeyValueMinus);
            //}

            var KillerOpponents = (DynamicOpponents)GameObjectService.Objects["OpponentDynamicObject0"];
            ModelNode killerOpponents = KillerOpponents._modelNode;
            Matrix44F lookAtMatrix = Matrix44F.CreateLookAt(_modelNode.PoseWorld.Position, killerOpponents.PoseWorld.Position, Vector3F.UnitY);
            Pose newPose = Pose.FromMatrix(lookAtMatrix).Inverse;

            Matrix33F frontWheelSteeringRotation = Matrix33F.CreateRotationZ(_frontWheelSteeringAngle.Value);
            _frontWheelLeft.PoseLocal = _frontWheelLeftRestPose;
            _frontWheelRight.PoseLocal = _frontWheelRightRestPose;

            GunFire(_frontWheelLeft.PoseLocal, _frontWheelRight.PoseLocal);




            bool timeStrings = IsOdd(Convert.ToInt32(_timePassed.Seconds.ToString("00")));

            var listHealth = _healthBarSceneNodeList.LastOrDefault();

            if (listHealth != null)
                listHealth.IsEnabled = timeStrings != true;
            else
                Dispose();


            if (_healthBarSceneNodeList.Count != 10)
            {
                if (_timeUntilAddHealth <= TimeSpan.Zero)
                {

                    using (var listHealths = _healthBarSceneNodeList.LastOrDefault())
                    {
                        //if (listHealths != null) listHealths.IsEnabled = true;
                        //_healthBarSceneNodeList.Remove(listHealths);
                        switch (listHealths.Name)
                        {
                            case "Box1":

                                break;
                            case "Box2":
                                _healthBarSceneNodeList.Add(_healthBarSceneNode3);
                                break;
                            case "Box3":
                                _healthBarSceneNodeList.Add(_healthBarSceneNode3);
                                break;
                            case "Box4":
                                _healthBarSceneNodeList.Add(_healthBarSceneNode4);
                                break;
                            case "Box5":
                                _healthBarSceneNodeList.Add(_healthBarSceneNode5);
                                break;
                            case "Box6":
                                _healthBarSceneNodeList.Add(_healthBarSceneNode7);
                                break;
                            case "Box7":                             
                                _healthBarSceneNode8 = _modelNode.GetSceneNode("Box8");
                                _healthBarSceneNode8.IsEnabled = true;
                                _healthBarPose8 = _healthBarSceneNode8.PoseLocal;

                                _healthBarSceneNodeList.Add(_healthBarSceneNode8);
                                break;
                            case "Box8":
                                _healthBarSceneNodeList.Add(_healthBarSceneNode9);
                                break;
                            case "Box9":

                                break;
                        }
                    }
                    _timeUntilAddHealth = AddHealthInterval;
                }
            }


            //Vector3F scale = _healthBarSceneNode.ScaleWorld;
            //float deltaYaw = -_inputService.MousePositionDelta.X;

            //_yaw += deltaYaw * deltaTime * 0.1f;
            //float deltaPitch = -_inputService.MousePositionDelta.Y;
            //_pitch += deltaPitch * deltaTime * 0.1f;

            // Limit the pitch angle.
            //_pitch = MathHelper.Clamp(_pitch, -ConstantsF.PiOver2, ConstantsF.PiOver2);
            // _yaw = MathHelper.Clamp(_yaw, -ConstantsF.PiOver2, ConstantsF.PiOver2);
            // Compute new orientation of the camera.


            // Move second ship with arrow keys.         
            KeyboardState keyboardState = _inputService.KeyboardState;
            if (keyboardState.IsKeyDown(Keys.W))
            {
                _fThrusters -= 0.05f;
            }
            else
            {
                if (_soundIcrement > 0.0f)
                {
                    _soundIcrement -= 0.01f;
                }
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                _fThrusters += 0.05f;
            }

            if (_inputService.IsDown(Keys.A))
            {
                _fRotation += 0.01f;
                _fRoll += 0.04f;
                angle -= 0.005f;
            }
            if (_inputService.IsDown(Keys.D))
            {
                _fRotation -= 0.01f;
                _fRoll -= 0.04f;
                angle += 0.005f;
            }
            if (keyboardState.IsKeyDown(Keys.Q))
                fPitch += 0.005f;
            if (keyboardState.IsKeyDown(Keys.E))
                fPitch -= 0.005f;

            UpdateThrusters();

            UpdateRotation();

            UpdateRoll();

            UpdateFloat();

            UpdatePitch();

            //            //shipMovement.X = _fRotation;
            QuaternionF cameraOrientation = QuaternionF.CreateRotationY(_shipRotation.Y) *
                                            QuaternionF.CreateRotationX(_shipRotation.X) *
                                            QuaternionF.CreateRotationZ(MathHelper.ToRadians(_shipRotation.Z));
            //            QuaternionF cameraOrientation = QuaternionF.CreateRotationY(_shipRotation.Y) * QuaternionF.CreateRotationX(_shipRotation.X) ;
            // The movement is relative to the view of the user. We must rotate the movement vector
            // into world space.
            //  shipMovement = GraphicsScreen.CameraNode.PoseWorld.ToWorldDirection(shipMovement);

            _shipRotation = new Vector3F(0, 0, _shipMovement.Z);


            _shipMovement = cameraOrientation.Rotate(_shipRotation);

            // Multiply the velocity by time to get the translation for this frame.
            Vector3F translation = _shipMovement * LinearVelocityMagnitude * deltaTime;

            UpdateProfiler();


            // Set the new camera pose.
            //_modelNode.PoseWorld = new Pose(_modelNode.PoseWorld.Position + translation, cameraOrientation);


            _bodyPrototype.Pose = new Pose(_modelNode.PoseWorld.Position + translation, cameraOrientation);

            _modelNode.PoseWorld = _bodyPrototype.Pose;

            Pose = _modelNode.PoseWorld;

            //billboardNode.SetLastPose(true);

            //if (keyboardState.IsKeyDown(Keys.O))
            //{

            //    billboardNode.ScaleLocal += new Vector3F(0.005f);
            //}

            //if (keyboardState.IsKeyDown(Keys.P))
            //{
            //    billboardNode.Color = new Vector3F(1, 0, 0);

            //    billboardNode.ScaleLocal -= new Vector3F(0.005f);
            //}

            //billboardNode.PoseWorld =  new Pose(Pose.Position + new Vector3F(0,3,0),Pose.Orientation);

            //billboardNode.PoseWorld = new Pose(new Vector3F(_modelNode.PoseWorld.Position.X, _modelNode.PoseWorld.Position.Y + 3.0f, _modelNode.PoseWorld.Position.Z), _modelNode.PoseWorld.Orientation);          
            //_jetFlame.Pose = new Pose(_modelNode.PoseWorld.Position + translation, cameraOrientation);
            //      // Update pose of second ship.
            //      var shipBPose = _shipObjectB.Pose;
            //      _shipObjectB.Pose = new Pose(shipBPose.Position + shipMovement, cameraOrientation);

            // Toggle debug drawing with Space key.
            //if (_inputService.IsPressed(Keys.Space, true))
            //    _drawDebugInfo = !_drawDebugInfo;

            // Update collision domain. - This will compute collisions.
            //_collisionDomain.Update(deltaTime);

            // Now we could, for example, ask the collision domain if the ships are colliding.
            //            bool shipsAreColliding = _collisionDomain.HaveContact(
            //              _shipObjectA.CollisionObject,
            //              _shipObjectB.CollisionObject);

            // Use the debug renderer of the graphics screen to draw debug info and collision shapes.
            //var debugRenderer = _graphicsScreen.DebugRenderer;
            //debugRenderer.Clear();

            //if (_collisionDomain.ContactSets.Count > 0)
            //    debugRenderer.DrawText("\n\nCOLLISION DETECTED");
            //else
            //    debugRenderer.DrawText("\n\nNo collision detected");

            //if (_drawDebugInfo)
            //{
            //    foreach (var collisionObject in _collisionDomain.CollisionObjects)
            //        debugRenderer.DrawObject(collisionObject.GeometricObject, Color.Gray, false, false);
            //}

            // ----- Set view matrix for graphics.
            // For third person we move the eye position back, behind the body (+z direction is 
            // the "back" direction).



            _zIncrement++;
            if (Math.Abs(_zIncrement) >= 40.00f)
            {
                _zIncrement = 40.00f;
                thirdPersonDistance = cameraOrientation.Rotate(new Vector3F(0, 3, _zIncrement));

            }
            else if (Math.Abs(_zIncrement) <= 40.00f)
            {
                thirdPersonDistance = cameraOrientation.Rotate(new Vector3F(0, 3, _zIncrement));
            }



            //Vector3F thirdPersonDistance = cameraOrientation.Rotate(new Vector3F(0, 1, 30));
            // Compute camera pose (= position + orientation). 
            _cameraNode.PoseWorld = new Pose
            {
                Position = _modelNode.PoseWorld.Position // Floor position of character
                           + new Vector3F(0, 1.6f, 0) // + Eye height
                           + thirdPersonDistance,
                Orientation = cameraOrientation.ToRotationMatrix33()
            };


            Vector3F thirdPersonDistanceMissile = cameraOrientation.Rotate(new Vector3F(0, 1, 30));
            //Vector3F thirdPersonDistanceMissile = cameraOrientation.Rotate(new Vector3F(0, 1, 15));
            // Compute camera pose (= position + orientation). 
            _cameraNodeMissile.PoseWorld = new Pose
            {
                Position = _modelNode.PoseWorld.Position // Floor position of character
                           + new Vector3F(0, 0.6f, 0) // + Eye height
                           + thirdPersonDistanceMissile,
                Orientation = cameraOrientation.ToRotationMatrix33()
            };
            //_jetFlame.AddParticles(6);
            //_particleSystemNode.Synchronize(graphicsService);

            //PlaySound(timeSpan);

            //rollCenter += _cameraNode.PoseWorld.Position;


            //_timeSinceLastHitSound += deltaTime;


            // The explosion is created at the position that is targeted with the cross-hair.
            // We can perform a ray hit-test to find the position. The ray starts at the camera
            // position and shoots forward (-z direction).
            var cameraGameObject =
                (ThirdPersonCameraObject.ThirdPersonCameraObject)GameObjectService.Objects["ThirdPersonCamera"];
            CameraNode cameraNode = cameraGameObject.CameraNode;
            Vector3F cameraPosition = _modelNode.PoseWorld.Position;
            Vector3F cameraDirection = _modelNode.PoseWorld.ToWorldDirection(Vector3F.Forward);

            // Create a ray for hit-testing.
            ray = new RayShape(cameraPosition, cameraDirection, 1000);

            // The ray should stop at the first hit. We only want the first object.
            ray.StopsAtFirstHit = true;

            // The collision detection requires a CollisionObject.
            var rayCollisionObject = new CollisionObject(new GeometricObject(ray, Pose.Identity))
            {
                // In SampleGame.ResetPhysicsSimulation() a collision filter was set:
                //   CollisionGroup = 0 ... objects that support hit-testing
                //   CollisionGroup = 1 ... objects that are ignored during hit-testing
                //   CollisionGroup = 2 ... objects (rays) for hit-testing
                CollisionGroup = 2,
            };

            _spring = null;

            // Get the first object that has contact with the ray.
            ContactSet contactSet = _simulation.CollisionDomain.GetContacts(rayCollisionObject).FirstOrDefault();
            if (contactSet != null && contactSet.Count > 0)
            {
                // The ray has hit something.

                // The contact set contains all detected contacts between the ray and the rigid body.
                // Get the first contact in the contact set. (A ray hit usually contains exactly 1 contact.)
                Contact contact = contactSet[0];

                // The contact set contains the object pair of the collision. One object is the ray.
                // The other is the object we want to grab.
                CollisionObject hitCollisionObject = (contactSet.ObjectA == rayCollisionObject)
                    ? contactSet.ObjectB
                    : contactSet.ObjectA;

                // Check whether a dynamic rigid body was hit.
                var hitBody = hitCollisionObject.GeometricObject as RigidBody;
                if (hitBody != null && hitBody.MotionType == MotionType.Static ||
                    hitBody != null && hitBody.MotionType == MotionType.Dynamic)
                {
                    // Attach the rigid body at the cursor position using a ball-socket joint.
                    // (Note: We could also use a FixedJoint, if we don't want any rotations.)

                    // The penetration depth tells us the distance from the ray origin to the rigid body.
                    _springAttachmentDistanceFromObserver = contact.PenetrationDepth;

                    // Get the position where the ray hits the other object.
                    // (The position is defined in the local space of the object.)
                    Vector3F hitPositionLocal = (contactSet.ObjectA == rayCollisionObject)
                        ? contact.PositionBLocal
                        : contact.PositionALocal;

                    _spring = new BallJoint
                    {
                        BodyA = hitBody,
                        AnchorPositionALocal = hitPositionLocal,

                        // We need to attach the grabbed object to a second body. In this case we just want to
                        // anchor the object at a specific point in the world. To achieve this we can use the
                        // special rigid body "World", which is defined in the simulation.
                        BodyB = _simulation.World,
                        // AnchorPositionBLocal is set below.

                        // Some constraint adjustments.
                        ErrorReduction = 0.3f,

                        // We set a softness > 0. This makes the joint "soft" and it will act like
                        // damped spring. 
                        Softness = 0.00001f,

                        // We limit the maximal force. This reduces the ability of this joint to violate
                        // other constraints. 
                        MaxForce = 1e6f
                    };

                    // Add the spring to the simulation.
                    _simulation.Constraints.Add(_spring);
                }

            }

            if (_spring != null)
            {

                var cameraGameObjects =
                    (ThirdPersonCameraObject.ThirdPersonCameraObject)GameObjectService.Objects["ThirdPersonCamera"];
                CameraNode cameraNodes = cameraGameObjects.CameraNode;
                Vector3F cameraPositions = cameraNodes.PoseWorld.Position;
                Vector3F cameraDirections = cameraNodes.PoseWorld.ToWorldDirection(-Vector3F.UnitZ);

                _spring.AnchorPositionBLocal = cameraPositions + cameraDirections * _springAttachmentDistanceFromObserver;

                _spring.BodyA.AngularVelocity *= 0.9f;

                if (_inputService.IsPressed(MouseButtons.Left, true))
                {

                    var simulation = _services.GetInstance<Simulation>();

                    _missileAttachedPrototypes.Add(new RigidBody(new CapsuleShape(0f, 0.001f)));

                    _missileAttachedSceneNode.Add(_modelNode.GetSceneNode("Wasp_IRSwarm" + j));

                    _missileAttachedSceneNodePose = _missileAttachedSceneNode[j].PoseWorld;

                    _missileAttachedPrototypes[j].Pose = _missileAttachedSceneNodePose;

                    simulation.RigidBodies.Add(_missileAttachedPrototypes[j]);

                    _geometricObjectMissile = new GeometricObject(new BoxShape(1, 1, 1), _missileAttachedSceneNode[j].PoseWorld);

                    _attachedMissileCollisionObject = new CollisionObject(_geometricObjectMissile);

                    var collisionDomain = _services.GetInstance<CollisionDomain>();
                    collisionDomain.CollisionObjects.Add(AttachedMissileCollisionObject);

                    _missileAttachedPrototypes[j].LinearVelocity = ray.Direction * 100;

                    Sound.Sound.PlayMissileSound(true);

                    j++;

                    missileProgressBar.Value -= 1;
                }
            }



            if (_missileAttachedSceneNode != null)
                for (int i = 0; i < _missileAttachedSceneNode.Count; i++)
                {
                    if (_missileAttachedSceneNode[i] != null)
                    {
                        _missileAttachedSceneNode[i].PoseWorld = _missileAttachedPrototypes[i].Pose;
                        PoseMissile = _missileAttachedSceneNode[i].PoseWorld;
                        if ((_modelNode.PoseWorld.Position - _missileAttachedSceneNode[i].PoseWorld.Position).LengthSquared >=
                            MaxDistance * MaxDistance)
                        {
                            _missileAttachedPrototypes[i].Pose = new Pose();
                            _simulation.RigidBodies.Remove(_missileAttachedPrototypes[i]);

                            //_missileAttachedSceneNode[i].Parent.Children.Remove(_missileAttachedSceneNode[i]);
                            //_missileAttachedSceneNode[i].Dispose(true);
                            //_missileAttachedSceneNode[i].GetSceneNode(_missileAttachedSceneNode[i].Name).Dispose(true);
                            _missileAttachedSceneNode[i].IsEnabled = false;
                            _missileAttachedSceneNode[i].PoseWorld = new Pose();
                            _missileAttachedSceneNode[i] = null;
                            //_missileAttachedSceneNode.Remove(_missileAttachedSceneNode[i]);

                            //_graphicsScreen.Scene.Children.Remove(_missileAttachedSceneNode[i]);



                            //j--;
                        }
                    }

                }

            if (Math.Abs(_fThrusters) > 0.0f)
            {
                Sound.Sound.UpdateGearSound(Math.Abs(_fThrusters), Math.Abs(_fThrusters), timeSpan);
            }


        }

        #endregion

        private bool _newScenNode;


        #region Private Methods



        private void GunFire(Pose leftPose, Pose rightPose)
        {
            if (_inputService.IsPressed(MouseButtons.Right, true))
            {
                Vector3F cameraDirection = _cameraNode.PoseWorld.ToWorldDirection(Vector3F.Backward);
                _isParticleSystemNode = true;

                var simulation = _services.GetInstance<Simulation>();
                _ammoAttachedRigidBody.Add(new RigidBody(new CapsuleShape(0f, 0.001f)));
                _ammoAttachedSceneNode.Add(_modelNode.GetSceneNode("SmallBullet00" + k));
                _ammoAttachedSceneNodePose = _ammoAttachedSceneNode[k].PoseWorld;
                _ammoAttachedRigidBody[k].Pose = _ammoAttachedSceneNodePose;
                simulation.RigidBodies.Add(_ammoAttachedRigidBody[k]);
                _geometricObjectAmmo = new GeometricObject(new BoxShape(0.5f, 0.5f, 0.5f), _ammoAttachedSceneNode[k].PoseWorld);
                _attachedAmmoCollisionObject = new CollisionObject(_geometricObjectAmmo);
                var collisionDomain = _services.GetInstance<CollisionDomain>();
                collisionDomain.CollisionObjects.Add(AttachedAmmoCollisionObject);
                _ammoAttachedRigidBody[k].LinearVelocity = cameraDirection * 100;
                var flash = new Flash.Flash(_contentManager,
                  new Pose(_ammoAttachedSceneNode[k].PoseWorld.Position));
                ParticleSystemService.ParticleSystems.Add(flash);
                _particleSystemNode = new ParticleSystemNode(flash);
                var scene = _services.GetInstance<IScene>();
                scene.Children.Add(_particleSystemNode);

                flash.Explode();
                Sound.Sound.PlayAmmoSound(true);
                k++;
                ammoProgressBar.Value -= 1;
            }
            if (_isParticleSystemNode)
            {
                _particleSystemNode.Synchronize(_graphicsService);
            }
            if (_ammoAttachedSceneNode != null)
                for (int i = 0; i < _ammoAttachedSceneNode.Count; i++)
                {
                    _ammoAttachedSceneNode[i].PoseWorld = _ammoAttachedRigidBody[i].Pose;

                    PoseAmmo = _ammoAttachedSceneNode[i].PoseWorld;
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
                _soundIcrement += 0.01f;
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
            _stringBuilder.Append("Position: " + _modelNode.PoseWorld.Position);
            _updateFpsTextBlock.Text = _stringBuilder.ToString();

            _MissileLockTextBlock.Text = _spring != null ? "Locked" : " ";

            thrustProgressBar.Value = Math.Abs(_fThrusters);
            _slider.Value = fPitch;

        }

        #endregion

        #region Dispose

        private void Dispose()
        {
            ModelNode model = _modelNode;
            RigidBody body = _bodyPrototype;

            if (body.Simulation != null)
            {
                _simulation.RigidBodies.Remove(body);
            }

            if (model.Parent != null)
            {
                model.Parent.Children.Remove(model);
                _graphicsScreen.Scene.Children.Remove(model);
                model.Dispose(false);
                model.IsEnabled = false;
            }
            _update = false;
            _collisionObject.Enabled = false;
        }

        public void DisposeByMissile(int hitCount)
        {
            HitCount = hitCount;

            string percent = (100 - HitCount).ToString();

            percent = percent + "%";

            if (percent.IndexOf("0", System.StringComparison.Ordinal) == 1 || percent == "0%")
            {
                for (int i = 1; i <= 2; i++)
                {
                    var listHealth = _healthBarSceneNodeList.LastOrDefault();
                    if (listHealth != null)
                    {
                        listHealth.IsEnabled = false;
                        //listHealth.Dispose(true);
                        _healthBarSceneNodeList.Remove(listHealth);
                    }
                }
            }
        }

        #endregion

    }
}