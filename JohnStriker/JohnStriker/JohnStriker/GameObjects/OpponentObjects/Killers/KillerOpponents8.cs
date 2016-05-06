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
using DigitalRune.Graphics.Effects;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Mathematics.Interpolation;
using DigitalRune.Mathematics.Statistics;
using DigitalRune.Physics;
using DigitalRune.Physics.Specialized;
using JohnStriker.GameObjects.PlayerObjects;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects.OpponentObjects
{
    public class KillerOpponents8 : GameObject
    {

        enum KillerAiState
        {
            // chasing the cat
            Chasing,
            // the tank has gotten close enough that the cat that it can stop chasing it
            Caught,
            // the tank can't "see" the cat, and is wandering around.
            Wander
        }

        #region Fields

        //--------------------------------------------------------------

        private const float Tolerance = 60.0f;

        private const float Timer = 1;

        private const float TimerModel = 45;

        private const float M16DSpeed = 10.0f;

        private const int FrameStringAndEnemyCount = 10;

        private static float LinearVelocityMagnitude;

        private readonly List<TimeSpan> ElapsedTimeSpan = new List<TimeSpan>();

        private readonly List<RigidBody> _bodies = new List<RigidBody>();

        private readonly CollisionObject _collisionObject;

        private readonly IGameObjectService _gameObjectService;

        private readonly GeometricObject _geometricObject;

        private readonly IGraphicsService _graphicsService;

        private readonly IInputService _inputService;

        private readonly ModelNode _missileModelNodes;

        private readonly List<ModelNode> _models = new List<ModelNode>();

        private readonly List<List<ObjectAnimationFrames>> _nestedListanimation =
            new List<List<ObjectAnimationFrames>>();

        private readonly IServiceLocator _services;

        private readonly Simulation _simulation;

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly float _vehicleRotation;

        // Models for rendering.
        private readonly ModelNode[] _wheelModelNodes;

        private readonly List<List<TimeSpan>> nestedListTimeSpan = new List<List<TimeSpan>>();

        private Pose CameraPose;

        private TimeSpan ElapsedTime = TimeSpan.FromSeconds(0);

        private bool FirstTime = true;

        private List<ObjectAnimationFrames> FrameStrings2 = new List<ObjectAnimationFrames>();

        private List<ObjectAnimationFrames> FrameStrings3 = new List<ObjectAnimationFrames>();

        private bool Loop = true;

        private bool SpawnMissile = false;

        public TimeSpan TimePassed;

        private TextBlock _OrientationTextBlock;

        private TextBlock _RotationTextBlock;

        private List<KeyframedObjectAnimations> _animation = new List<KeyframedObjectAnimations>();

        private RigidBody _bodyPrototype;

        private float _destinationZ;

        private float _destinationsZ;

        private float _destinationsZPosition;

        private float _direction = 0;

        private TextBlock _drawFpsTextBlock;

        private ConstParameterBinding<Vector3> _emissiveColorBinding;

        private int _enemyCount;

        public float _fPitch;

        private float _fRoll;

        private float _fRotation;

        private string _fRotationPosition;

        private TextBlock _fRotationPositionBlock;

        private float _fThrusters;

        private StackPanel _fpsPanel;

        private AnimatableProperty<float> _glowIntensity;

        private GuiMissileScreen _guiGraphicsScreen;

        private float _missileForce;

        public ModelNode _modelPrototype;

        private float _motorForce;

        private PointLight _pointLight;

        private int _randomcounts;

        private IScene _scene;

        private Vector3F _shipMovement;

        private Vector3F _shipRotation;

        private float _speedInitialize = 5f;

        private float _speedMissile = 0f;

        private float _steeringAngle;

        private TextBlock _updateFpsTextBlock;

        // Pitch information
        private float fPitch;

        private int increment = 0;

        private bool isVehicleRotation;

        private Pose jetPose;

        private Matrix33F pitch;

        private Texture2D sparkTexture2D;

        private float timer = 1; //Initialize a 10 second timer

        private float timerModel = 45; //Initialize a 10 second timer

        private Vehicle vehicleObject;

        private Pose missilePose { get; set; }

        private Vector3F Rotation { get; set; }
        private Vector3F Position { get; set; }

        // A sound of a rolling object.
        private SoundEffect _rollSound;

        private SoundEffectInstance _rollSoundInstance;

        private AudioEmitter _rollEmitter;

        private AudioListener _listener;

        private CameraNode cameraNodeMissile;

        private const float MaxSoundChangeSpeed = 3f;

        private float _soundIcrement;

        private int numberOfRollingContacts = 0;

        Vector3F rollCenter = Vector3F.Zero;

        float rollSpeed = 0;

        // Only sounds within MaxDistance will be played.
        private const float MaxDistance = 60;

        // Contact forces below MinHitForce do not make a sound.
        private const float MinHitForce = 20000;

        private SoundEffectInstance[] _hitSoundInstances = new SoundEffectInstance[5];

        private AudioEmitter[] _hitEmitters = new AudioEmitter[5];

        private float _timeSinceLastHitSound;

        private SoundEffect _hitSound;

        private readonly SoundEffect flightPassByClose;

        private readonly SoundEffect flightPassByDistance;

        private TimeSpan _timeUntilExplosion = TimeSpan.Zero;

        private static readonly TimeSpan ExplosionInterval = TimeSpan.FromSeconds(1);

        private KillerAiState killerState = KillerAiState.Wander;

        private const float KillerChaseDistance = 600.0f;

        private const float KillerCaughtDistance = 10.0f;

        private const float KillerHysteresis = 15.0f;

        private string _identifyChasingWanderCaught = string.Empty;

        private float _speedIncrement = 48.0f;

        private TimeSpan _timeUntilSpeed = TimeSpan.Zero;

        private static readonly TimeSpan SpeedInterval = TimeSpan.FromSeconds(30);

        private static float _linearVelocityMagnitude = 30f;

        private float _distanceForMissile;

        private readonly TextBlock _rotationTextBlock;

        private TimeSpan _timeUntilLaunchMissile = TimeSpan.Zero;

        private static readonly TimeSpan MissileInterval = TimeSpan.FromSeconds(1);

        private readonly List<RigidBody> _missileAttachedPrototypes;

        private List<SceneNode> _missileAttachedSceneNode;

        private Pose _missileAttachedSceneNodePose;

        private CollisionObject _attachedMissileCollisionObject;

        private GeometricObject _geometricObjectMissile;

        private int j;

        Pose _newPose = new Pose();

        Vector3F _forwardModelPose = new Vector3F();

        private bool _isMissile = true;

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

        private readonly IAnimationService _animationService;
        #endregion

        #region Properties & Events
        private int HitCount { get; set; }
        //--------------------------------------------------------------
        public CollisionObject CollisionObject
        {
            get { return _collisionObject; }
        }

        public Pose Pose
        {
            get { return _modelPrototype.PoseWorld; }
            set
            {
                //foreach (ModelNode t in _models)
                //{
                //    value = t.PoseWorld;
                //    _geometricObject.Pose = value;
                //}
                foreach (ModelNode t in _models)
                {
                    _geometricObject.Pose = t.PoseWorld;
                }
            }
        }

        private Pose StartPose { get; set; }
        private Pose TargetPose { get; set; }

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
                    _geometricObjectMissile.Pose = sceneNode.PoseWorld;
                }

            }
        }

        public CollisionObject AttachedMissileCollisionObject
        {
            get { return _attachedMissileCollisionObject; }
        }
        #endregion

        #region Creation & Cleanup & RigidBody

        //--------------------------------------------------------------


        public KillerOpponents8(IServiceLocator services, int objectCount)
        {
            _services = services;

            Name = "KillerOpponents8" + objectCount;
            _gameObjectService = services.GetInstance<IGameObjectService>();
            _inputService = _services.GetInstance<IInputService>();
            _simulation = _services.GetInstance<Simulation>();
            _animationService = services.GetInstance<IAnimationService>();
            _graphicsService = _services.GetInstance<IGraphicsService>();
            //// Add the GuiGraphicsScreen to the graphics service.
            var guiKiller = new GuiKiller(services);
            _graphicsService.Screens.Add(guiKiller);

            //// ----- FPS Counter (top right)
            var _fpsPanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            guiKiller.UIScreen.Children.Add(_fpsPanel);
            _updateFpsTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "Position",
            };
            _fpsPanel.Children.Add(_updateFpsTextBlock);
            _rotationTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "FRotation",
            };
            _fpsPanel.Children.Add(_rotationTextBlock);

            _drawFpsTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "FThrusters",
            };
            _fpsPanel.Children.Add(_drawFpsTextBlock);
            _fRotationPositionBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "_fRotationPosition",
            };
            _fpsPanel.Children.Add(_fRotationPositionBlock);


            // A simple cube.

            // Load models for rendering.
            var contentManager = _services.GetInstance<ContentManager>();

            InitializeAudio();

            //_bodyPrototype = new RigidBody(modelPShape);

            _bodyPrototype = new RigidBody(new BoxShape(5, 0, 5));

            //flightPassByClose = contentManager.Load<SoundEffect>("Audio/Jet_FA18s_Pass05_low");

            //flightPassByDistance = contentManager.Load<SoundEffect>("Audio/Jet_FA18s_Pass05");

            _modelPrototype = contentManager.Load<ModelNode>("Killer/KillerFighter").Clone();
            //_modelPrototype.ScaleLocal = new Vector3F(0.06f);

            CreateRigidBody();

            //_bodyPrototype.Pose = new Pose(new Vector3F(0, 2, -50), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

            _modelPrototype.PoseWorld = _bodyPrototype.Pose;

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
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject);

            if (IsEven(objectCount))
            {
                List<ObjectAnimationFrames> FrameStrings;
                FrameStrings = new List<ObjectAnimationFrames>();
                FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")), 15))));
                FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 5, 20))));
                FrameStrings.Add(new ObjectAnimationFrames("", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 10, 25))));
                FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 15, 30))));
                FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 20, 35))));
                FrameStrings.Add(new ObjectAnimationFrames("", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 25, 40))));
                FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 30, 45))));
                _nestedListanimation.Add(FrameStrings);
            }
            else
            {
                List<ObjectAnimationFrames> FrameStrings;
                FrameStrings = new List<ObjectAnimationFrames>();
                FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")), 15))));
                FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "rotate right", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 5, 20))));
                FrameStrings.Add(new ObjectAnimationFrames("", "rotate right", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 10, 25))));
                FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 15, 30))));
                FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 20, 35))));
                FrameStrings.Add(new ObjectAnimationFrames("", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 25, 40))));
                FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 30, 45))));
                _nestedListanimation.Add(FrameStrings);
            }

            _missileAttachedPrototypes = new List<RigidBody> { };

            _missileAttachedSceneNode = new List<SceneNode> { };

            CreateAndStartAnimations(0.3f, 0.3f);
        }

        private void CreateAndStartAnimations(float animationKeyValuePlus, float animationKeyValueminus)
        {

            _healthBarSceneNode0 = _modelPrototype.GetSceneNode("Box0");
            _healthBarPose0 = _healthBarSceneNode0.PoseLocal;
            _healthBarSceneNode1 = _modelPrototype.GetSceneNode("Box1");
            _healthBarPose1 = _healthBarSceneNode1.PoseLocal;
            _healthBarSceneNode2 = _modelPrototype.GetSceneNode("Box2");
            _healthBarPose2 = _healthBarSceneNode2.PoseLocal;
            _healthBarSceneNode3 = _modelPrototype.GetSceneNode("Box3");
            _healthBarPose3 = _healthBarSceneNode3.PoseLocal;
            _healthBarSceneNode4 = _modelPrototype.GetSceneNode("Box4");
            _healthBarPose4 = _healthBarSceneNode4.PoseLocal;
            _healthBarSceneNode5 = _modelPrototype.GetSceneNode("Box5");
            _healthBarPose5 = _healthBarSceneNode5.PoseLocal;
            _healthBarSceneNode6 = _modelPrototype.GetSceneNode("Box6");
            _healthBarPose6 = _healthBarSceneNode6.PoseLocal;
            _healthBarSceneNode7 = _modelPrototype.GetSceneNode("Box7");
            _healthBarPose7 = _healthBarSceneNode7.PoseLocal;
            _healthBarSceneNode8 = _modelPrototype.GetSceneNode("Box8");
            _healthBarPose8 = _healthBarSceneNode8.PoseLocal;
            _healthBarSceneNode9 = _modelPrototype.GetSceneNode("Box9");
            _healthBarPose9 = _healthBarSceneNode9.PoseLocal;

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

        private void CreateRigidBody()
        {
            var triangleMesh = new TriangleMesh();

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
                Pose = StartPose,
                Scale = _modelPrototype.ScaleLocal,
                MotionType = MotionType.Static
            };

            // Add rigid body to physics simulation and model to scene.           
            _simulation.RigidBodies.Add(_bodyPrototype);
        }

        private static bool IsEven(int value)
        {
            return value % 2 != 0;
        }

        private static bool IsEvenSeconds(int value)
        {
            return value % 2 != 0;
        }
        #endregion

        #region Update Methods

        protected override void OnUpdate(TimeSpan deltaTime)
        {
            if (!_inputService.EnableMouseCentering)
                return;

            var deltaTimeF = (float)deltaTime.TotalSeconds;

            _randomcounts = 0;

            TimePassed += TimeSpan.FromSeconds(deltaTime.TotalSeconds);

            _timeUntilExplosion -= TimeSpan.FromSeconds(deltaTime.TotalSeconds);

            _timeUntilSpeed -= TimeSpan.FromSeconds(deltaTime.TotalSeconds);

            _timeUntilLaunchMissile -= TimeSpan.FromSeconds(deltaTime.TotalSeconds);

            string timeString = TimePassed.Minutes.ToString("00") + ":" + TimePassed.Seconds.ToString("00");

            bool timeStrings = IsEvenSeconds(Convert.ToInt32(TimePassed.Seconds.ToString("00")));

            var listHealth = _healthBarSceneNodeList.LastOrDefault();

            if (listHealth != null)
                listHealth.IsEnabled = timeStrings != true;
            else
                Dispose();

            var modelOrientation = new QuaternionF();

            if (_enemyCount <= _randomcounts)
            {
                _enemyCount++;

                var randomPosition = new Vector3F(
                    RandomHelper.Random.NextFloat(-10, 10),
                    RandomHelper.Random.NextFloat(2, 5),
                    RandomHelper.Random.NextFloat(-30, -50));
                //var pose = new Pose(randomPosition, RandomHelper.Random.NextQuaternionF());
                //var pose = new Pose(new Vector3F(0, 2, -650), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));
                var pose = new Pose(new Vector3F(0, 2, -450), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

                var scene = _services.GetInstance<IScene>();

                _collisionObject.Enabled = true;

                StartPose = pose;


                ModelNode model = _modelPrototype;

                RigidBody body = _bodyPrototype;

                body.Pose = StartPose;

                _bodyPrototype.Pose = body.Pose;

                model.PoseWorld = _bodyPrototype.Pose;

                scene.Children.Add(model);

                _models.Add(model);

                _bodies.Add(body);

                List<TimeSpan> listTimeSpan;

                listTimeSpan = new List<TimeSpan>();

                listTimeSpan.Add(_nestedListanimation[_enemyCount - 1][0].Time);

                nestedListTimeSpan.Add(listTimeSpan);

                ElapsedTimeSpan.Add(TimeSpan.FromSeconds(0));
            }


            for (int i = 0; i < _models.Count; i++)
            {
                var cameraGameObjects = (Player)_gameObjectService.Objects["VehicleOpponent"];
                ModelNode playerNode = cameraGameObjects._modelNode;
                //CameraNode cameraNodes = cameraGameObjects.CameraNode;
                var killerOrientationQuaternion = new Quaternion();
                //if ((_models[i].PoseWorld.Position - playerNode.PoseWorld.Position).Length <= 1000)
                //{
                float tankChaseThreshold = KillerChaseDistance;
                float tankCaughtThreshold = KillerCaughtDistance;
                // if the tank is idle, he prefers to stay idle. we do this by making the
                // chase distance smaller, so the tank will be less likely to begin chasing
                // the cat.
                if (killerState == KillerAiState.Wander)
                {
                    tankChaseThreshold -= KillerHysteresis / 2;
                }
                // similarly, if the tank is active, he prefers to stay active. we
                // accomplish this by increasing the range of values that will cause the
                // tank to go into the active state.
                else if (killerState == KillerAiState.Chasing)
                {
                    tankChaseThreshold += KillerHysteresis / 2;
                    tankCaughtThreshold -= KillerHysteresis / 2;
                }
                // the same logic is applied to the finished state.
                else if (killerState == KillerAiState.Caught)
                {
                    tankCaughtThreshold += KillerHysteresis / 2;
                }

                // Second, now that we know what the thresholds are, we compare the tank's 
                // distance from the cat against the thresholds to decide what the tank's
                // current state is.
                float distanceFromPlayer = (_models[i].PoseWorld.Position - playerNode.PoseWorld.Position).Length;
                if (distanceFromPlayer > tankChaseThreshold)
                {
                    // just like the mouse, if the tank is far away from the cat, it should
                    // idle.
                    killerState = KillerAiState.Wander;
                }
                else if (distanceFromPlayer > tankCaughtThreshold)
                {
                    killerState = playerNode.IsEnabled ? KillerAiState.Chasing : KillerAiState.Wander;

                }
                else
                {
                    killerState = playerNode.IsEnabled ? KillerAiState.Caught : KillerAiState.Wander;
                }

                // Third, once we know what state we're in, act on that state.                
                switch (killerState)
                {
                    case KillerAiState.Chasing:
                        {
                            _identifyChasingWanderCaught = "Chasing";

                            if (_timeUntilExplosion <= TimeSpan.Zero)
                            {
                                Sound.Sound.PlayPassbySound(Sound.Sound.Sounds.Bleep);
                                _timeUntilExplosion = ExplosionInterval;
                            }

                            if (_timeUntilSpeed <= TimeSpan.Zero)
                            {
                                _speedIncrement += 2.0f;
                                _timeUntilSpeed = SpeedInterval;
                            }
                            //if (Math.Abs(_models[i].PoseWorld.Position.Z) > Math.Abs(playerNode.PoseWorld.Position.Z))
                            //{
                            //    _linearVelocityMagnitude -= 0.5f;
                            //}
                            //else if (Math.Abs(_models[i].PoseWorld.Position.Z) < Math.Abs(playerNode.PoseWorld.Position.Z))
                            //{
                            //    _linearVelocityMagnitude += 0.5f;
                            //}

                            //if (_models[i].PoseWorld.Position.Length > playerNode.PoseWorld.Position.Length)
                            //{
                            //    _linearVelocityMagnitude -= 0.5f;
                            //}
                            //if (_models[i].PoseWorld.Position.Length < playerNode.PoseWorld.Position.Length)
                            //{
                            //_linearVelocityMagnitude += 0.5f;
                            //}

                            _linearVelocityMagnitude += 0.5f;

                            if (_linearVelocityMagnitude >= _speedIncrement)
                            {
                                _linearVelocityMagnitude -= 0.5f;
                            }
                            else if (_linearVelocityMagnitude < 0.0f)
                            {
                                _linearVelocityMagnitude = 0.0f;
                            }

                            //Multiply the velocity by time to get the translation for this frame.

                            _models[i].SetLastPose(true);

                            Matrix44F lookAtMatrix = Matrix44F.CreateLookAt(_models[i].PoseWorld.Position,
                                playerNode.PoseWorld.Position, Vector3F.UnitY);

                            _newPose = Pose.FromMatrix(lookAtMatrix).Inverse;

                            _forwardModelPose = _newPose.ToWorldDirection(Vector3F.Forward);

                            LinearVelocityMagnitude = _linearVelocityMagnitude;

                            Vector3F translation = _forwardModelPose * LinearVelocityMagnitude * deltaTimeF;

                            _bodies[i].Pose = new Pose(_models[i].PoseWorld.Position + translation, _newPose.Orientation);

                            //_bodies[i].Pose = new Pose(_models[i].PoseWorld.Position + newPose.Position, modelOrientation);

                            _models[i].PoseWorld = _bodies[i].Pose;

                            Pose = _models[i].PoseWorld;

                            _distanceForMissile = (_models[i].PoseWorld.Position - playerNode.PoseWorld.Position).Length;
                            if (_distanceForMissile > 15.0f && _distanceForMissile < 40.0f)
                            {
                                if (_isMissile)
                                    if (_timeUntilLaunchMissile <= TimeSpan.Zero)
                                    {

                                        if (_models[i].GetSceneNode("Wasp_IRSwarm" + j) == null)
                                        {
                                            _isMissile = false;
                                            break;
                                        }
                                        cameraNodeMissile.PoseWorld = new Pose
                                        {
                                            Position = _models[i].PoseWorld.Position
                                                       + new Vector3F(0, -1.6f, 0),
                                            Orientation = _models[i].PoseWorld.Orientation
                                        };

                                        //MissileObject.MissileObject missileObjectOne =
                                        //    _gameObjectService.Objects.OfType<MissileObject.MissileObject>().FirstOrDefault();
                                        //if (missileObjectOne != null)
                                        //{
                                        //    missileObjectOne.Spawn(cameraNodeMissile.PoseWorld, _models[i].PoseWorld, newPose);
                                        //    Sound.Sound.PlayMissileSound(true);
                                        //}

                                        var simulation = _services.GetInstance<Simulation>();

                                        _missileAttachedPrototypes.Add(new RigidBody(new CapsuleShape(0f, 0.001f)));


                                        _missileAttachedSceneNode.Add(_models[i].GetSceneNode("Wasp_IRSwarm" + j));

                                        _missileAttachedSceneNodePose = _missileAttachedSceneNode[j].PoseWorld;

                                        _missileAttachedPrototypes[j].Pose = _missileAttachedSceneNodePose;

                                        simulation.RigidBodies.Add(_missileAttachedPrototypes[j]);

                                        _geometricObjectMissile = new GeometricObject(new BoxShape(1, 1, 1),
                                            _missileAttachedSceneNode[j].PoseWorld);

                                        _attachedMissileCollisionObject = new CollisionObject(_geometricObjectMissile);

                                        var collisionDomain = _services.GetInstance<CollisionDomain>();
                                        collisionDomain.CollisionObjects.Add(AttachedMissileCollisionObject);

                                        Sound.Sound.PlayMissileSound(true);

                                        _missileAttachedPrototypes[j].LinearVelocity = _forwardModelPose * 100;

                                        j++;

                                        _timeUntilLaunchMissile = MissileInterval;

                                    }
                            }
                        }
                        break;
                    case KillerAiState.Wander:
                        {
                            _identifyChasingWanderCaught = "Wander";

                            ElapsedTimeSpan[i] += deltaTime;

                            TimeSpan totalTime = ElapsedTimeSpan[i];

                            TimeSpan End = _nestedListanimation[i][_nestedListanimation[i].Count - 1].Time;

                            //loop ariound the total time if necessary
                            if (Loop)
                            {
                                while (totalTime > End)
                                    totalTime -= End;
                            }
                            else // Otherwise, clamp to the end values
                            {
                                Position = _nestedListanimation[i][_nestedListanimation[i].Count - 1].ValPosition;
                                Rotation = _nestedListanimation[i][_nestedListanimation[i].Count - 1].ValRotation;
                                return;
                            }

                            int j = 0;

                            //find the index of the current frame
                            while (_nestedListanimation[i][j + 1].Time < totalTime)
                            {
                                j++;
                            }

                            // Find the time since the beginning of this frame
                            totalTime -= _nestedListanimation[i][j].Time;

                            // Find how far we are between the current and next frame (0 to 1)
                            var amt = (float)((totalTime.TotalSeconds) /
                                              (_nestedListanimation[i][j + 1].Time - _nestedListanimation[i][j].Time)
                                                  .TotalSeconds);

                            // Interpolate position and rotation values between frames

                            Position = InterpolationHelper.Lerp(_nestedListanimation[i][j].ValPosition,
                                _nestedListanimation[i][j + 1].ValPosition, amt);

                            Rotation = InterpolationHelper.Lerp(_nestedListanimation[i][j].ValRotation,
                                _nestedListanimation[i][j + 1].ValRotation, amt);
                            Control(Position);

                            Control(Rotation);

                            UpdateThrusters();

                            UpdateRotation();

                            UpdateRoll();

                            UpdatePitch();

                            modelOrientation = QuaternionF.CreateRotationY(_shipRotation.Y) *
                                               QuaternionF.CreateRotationX(_shipRotation.X) *
                                               QuaternionF.CreateRotationZ(MathHelper.ToRadians(_shipRotation.Z));

                            _shipRotation = new Vector3F(0, 0, _shipMovement.Z);

                            _shipMovement = modelOrientation.Rotate(_shipRotation);

                            LinearVelocityMagnitude = 5f;

                            // Multiply the velocity by time to get the translation for this frame.
                            Vector3F translation = _shipMovement * LinearVelocityMagnitude * deltaTimeF;

                            _models[i].SetLastPose(true);

                            _bodies[i].Pose = new Pose(_models[i].PoseWorld.Position + translation, modelOrientation);

                            _models[i].PoseWorld = _bodies[i].Pose;

                            Pose = _models[i].PoseWorld;

                        }
                        break;
                    default:
                        _identifyChasingWanderCaught = "Caught";
                        _linearVelocityMagnitude = 0.0f;
                        break;
                }

                if (_missileAttachedSceneNode != null)
                    for (var k = 0; k < _missileAttachedSceneNode.Count; k++)
                    {
                        //_missileAttachedPrototypes[k].LinearVelocity = forwardModelPose * 100;

                        _missileAttachedSceneNode[k].PoseWorld = _missileAttachedPrototypes[k].Pose;

                        PoseMissile = _missileAttachedSceneNode[k].PoseWorld;
                    }

                //_models[i].PoseWorld = _bodies[i].Pose;

                //Pose = _models[i].PoseWorld;


                //}
                UpdateProfiler();
            }


        }





        private float speed = 0.0f;
        protected override void OnUnload()
        {
            // Remove models from scene.
            foreach (ModelNode model in _models)
            {
                model.Parent.Children.Remove(_modelPrototype);
                model.Dispose(false);
            }

            // Remove rigid bodies from physics simulation.
            foreach (RigidBody body in _bodies)
                _simulation.RigidBodies.Remove(body);

            _models.Clear();
            _bodies.Clear();

            // Remove prototype.
            _modelPrototype.Dispose(false);
            _modelPrototype = null;
            _bodyPrototype = null;
        }

        #endregion


        #region Dispose

        public void DisposeByBullet(int hitCount)
        {
            HitCount = hitCount;     

            string percent = (100 - HitCount).ToString();

            percent = percent + "%";

            if (percent.IndexOf("0", System.StringComparison.Ordinal) == 1)
            {

            }                           
        }

        public void Dispose()
        {

            for (int i = 0; i < _models.Count; i++)
            {
                ModelNode model = _models[i];
                RigidBody body = _bodies[i];

                if (body.Simulation != null)
                {
                    _simulation.RigidBodies.Remove(body);
                }

                if (model.Parent != null)
                {
                    model.Parent.Children.Remove(model);
                    model.Dispose(false);
                }
            }

            _models.Clear();
            _bodies.Clear();
            _collisionObject.Enabled = false;
        }
        #endregion

        #region Private Methods

        private void Control(Vector3F sCommand)
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
                _soundIcrement += 0.01f;
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

            _shipRotation.Z = -(_fRoll * _fThrusters);
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

            _shipRotation.X = (fPitch);
        }

        private void UpdateProfiler()
        {
            _stringBuilder.Clear();
            foreach (ModelNode t in _models)
            {
                _stringBuilder.Append("Position: " + HitCount);
            }
            _updateFpsTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("LinearVelocityMagnitude: " + LinearVelocityMagnitude);
            _drawFpsTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("Identification: " + _identifyChasingWanderCaught);
            _rotationTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("DistanceForMissile: " + _distanceForMissile);
            _fRotationPositionBlock.Text = _stringBuilder.ToString();
        }

        #endregion


        private void InitializeAudio()
        {
            var cameraGameObject =
              (ThirdPersonCameraObject.ThirdPersonCameraObject)_gameObjectService.Objects["ThirdPersonCamera"];
            cameraNodeMissile = cameraGameObject.CameraNodeMissile;
        }
    }
}
