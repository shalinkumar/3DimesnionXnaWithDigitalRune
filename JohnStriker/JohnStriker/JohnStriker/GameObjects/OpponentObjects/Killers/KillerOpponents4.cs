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
using DigitalRune.Physics;
using DigitalRune.Physics.Constraints;
using DigitalRune.Physics.Specialized;
using JohnStriker.GraphicsScreen;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using DigitalRune.Geometry.Meshes;
using DigitalRune.Geometry.Partitioning;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects.OpponentObjects.Killers
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

    public enum Behavior { chase, flee, patrol }

    public class KillerOpponents4 : GameObject
    {
        #region Fields

        //--------------------------------------------------------------

        private const float Tolerance = 60.0f;

        private const float Timer = 1;

        private const float TimerModel = 45;

        private const float M16DSpeed = 10.0f;

        private const int FrameStringAndEnemyCount = 10;

        private static float _linearVelocityMagnitude = 30f;

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

        private GuiKiller _guiGraphicsScreen;

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

        private CameraNode cameraNodeAudio;

        private const float MaxSoundChangeSpeed = 3f;

        private float _soundIcrement;

        private int numberOfRollingContacts = 0;

        Vector3F rollCenter = Vector3F.Zero;

        float rollSpeed = 0;

        // Only sounds within MaxDistance will be played.
        private const float MaxDistance = 40;

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

        public CameraNode _cameraNode;

        private readonly IGraphicsService graphicsService;

        private TimeSpan _timeUntilLaunchMissile = TimeSpan.Zero;

        private static readonly TimeSpan MissileInterval = TimeSpan.FromSeconds(1);

        private BallJoint _spring;

        private float _springAttachmentDistanceFromObserver;

        private readonly MissileObject.MissileObject MissileObject;

        private readonly IGameObjectService GameObjectService;

        // this value controls the distance at which the tank will start to chase the
        // cat.
        private const float KillerChaseDistance = 500.0f;

        // TankCaughtDistance controls the distance at which the tank will stop because
        // he has "caught" the cat.
        private const float KillerCaughtDistance = 10.0f;

        private KillerAiState killerState = KillerAiState.Wander;

        // this constant is used to avoid hysteresis, which is common in ai programming.
        // see the doc for more details.
        private const float KillerHysteresis = 15.0f;

        // how fast can the tank move?
        private const float MaxKillerSpeed = 9.0f;

        private float killerOrientation;

        // how fast can he turn?
        private const float KillerTurnSpeed = 0.10f;

        private Vector3F killerWanderDirection;

        private Random random = new Random();

        private Behavior behavior = Behavior.patrol;

        private string currentTarget;

        private int waypoint = 0;

        private float _targetSpeed;

        private const float maxSpeed = 9f;

        private const float ScaleFactor = 5f;

        private bool patrolling = true;

        private static readonly Vector3[] waypoints = { 
                                        new Vector3(-5000*ScaleFactor,0, 5000*ScaleFactor),
                                        new Vector3( 5000*ScaleFactor,0, 5000*ScaleFactor),
                                        new Vector3( 5000*ScaleFactor,0,-5000*ScaleFactor),
                                        new Vector3(-5000*ScaleFactor,0,-5000*ScaleFactor)
                                      };

        private Nullable<Vector3> randomPos = null;

        private static readonly float changeEvasive = MathHelper.ToRadians(1f); //evasive not working, change direction

        private static readonly float accurateTurn = 500 * 500 * ScaleFactor * ScaleFactor;// turn accurately only within 500m

        private static readonly float maxDev = MathHelper.ToRadians(2f);

        private float turnSpeed = 0.0963421762f;

        private static readonly bool UseNewtonian = false;

        private Vector3 SpeedVector3 = new Vector3(9f);

        private static readonly float nextDist = 250 * 250 * ScaleFactor * ScaleFactor;

        private static readonly float radarRange = 10000 * ScaleFactor;
        private static readonly float radarRangeSq = radarRange * radarRange;

        private int fleeTimer = 0;

        private Vector3F position = Vector3F.Zero;

        private static readonly float combatMinDist = 250 * 250 * ScaleFactor * ScaleFactor;

        private static readonly float EvasiveManeuvers = MathHelper.ToRadians(10f); //evasive maneuvers if target is facing this ship when fleeing

        private static readonly int maxFleeTime = 1 * 60 * 60; //have been fleeing for 1 minute, turn around for another attack pass

        protected Quaternion orientation = Quaternion.Identity;

        public Vector3 Forward
        {
            get
            {
                return Vector3.Transform(Vector3.Forward, Matrix.CreateFromQuaternion(orientation));
            }
        }

        private float spd = 0.5f;

        private float acceleration = 0.038194444f;

        private float accelLimit;

        private Vector3 strafeSpeed = Vector3.Zero;

        // how fast can the tank move?
        const float MaxTankSpeed = 5.0f;

        private TimeSpan _timeUntilThrust = TimeSpan.Zero;

        private static TimeSpan ThrustInterval;

        private TimeSpan _timeUntilLeft = TimeSpan.Zero;

        private static TimeSpan LeftInterval;

        private TimeSpan _timeUntilRight = TimeSpan.Zero;

        private static TimeSpan RightInterval;


     
        #endregion

        #region Properties & Events

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

        #endregion

        #region Creation & Cleanup & RigidBody

        //--------------------------------------------------------------


        public KillerOpponents4(IServiceLocator services, int objectCount)
        {
            _services = services;

            Name = "KillerOpponents4" + objectCount;
            _gameObjectService = services.GetInstance<IGameObjectService>();
            _inputService = _services.GetInstance<IInputService>();
            _simulation = _services.GetInstance<Simulation>();
            graphicsService = services.GetInstance<IGraphicsService>();
            var graphicsScreen = new MyGraphicsScreen(services) { DrawReticle = true };
            GameObjectService = services.GetInstance<IGameObjectService>();
            _graphicsService = _services.GetInstance<IGraphicsService>();
            // Add the GuiGraphicsScreen to the graphics service.
            _guiGraphicsScreen = new GuiKiller(_services);
            _graphicsService.Screens.Add(_guiGraphicsScreen);

            // ----- FPS Counter (top right)
            _fpsPanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            _guiGraphicsScreen.UIScreen.Children.Add(_fpsPanel);
            _updateFpsTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Right,
                Text = "Position",
            };
            _fpsPanel.Children.Add(_updateFpsTextBlock);
            _RotationTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Right,
                Text = "FRotation",
            };
            _fpsPanel.Children.Add(_RotationTextBlock);
            _fRotationPositionBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Right,
                Text = "_fRotationPosition",
            };
            _fpsPanel.Children.Add(_fRotationPositionBlock);
            _drawFpsTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "FThrusters",
            };
            _fpsPanel.Children.Add(_drawFpsTextBlock);

            // A simple cube.

            // Load models for rendering.
            var contentManager = _services.GetInstance<ContentManager>();
            InitializeAudio(contentManager);
            //_bodyPrototype = new RigidBody(modelPShape);

            // Create a camera.
            var projection = new PerspectiveProjection();
            projection.SetFieldOfView(
                ConstantsF.PiOver4,
                graphicsService.GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                1000.0f);
            _cameraNode = new CameraNode(new Camera(projection));
            graphicsScreen.ActiveCameraNode = _cameraNode;

            _bodyPrototype = new RigidBody(new BoxShape(5, 0, 5));

            //flightPassByClose = contentManager.Load<SoundEffect>("Audio/Jet_FA18s_Pass05_low");

            //flightPassByDistance = contentManager.Load<SoundEffect>("Audio/Jet_FA18s_Pass05");

            _modelPrototype = contentManager.Load<ModelNode>("M16D/skyfighter fbx").Clone();
            //_modelPrototype.ScaleLocal = new Vector3F(0.06f);

            CreateRigidBody();

            _bodyPrototype.Pose = new Pose(new Vector3F(0, 3, -10), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

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

            if (IsOdd(objectCount))
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


            MissileObject = new MissileObject.MissileObject(services, "KillerMissile");
            GameObjectService.Objects.Add(MissileObject);

            accelLimit = acceleration * .6f;
            acceleration = Math.Min(accelLimit + (float)(((random.NextDouble() * 4) - 2) / (60 * 60)) * ScaleFactor, acceleration);
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

        public static bool IsOdd(int value)
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

            _timeUntilThrust -= TimeSpan.FromSeconds(deltaTime.TotalSeconds);

            _timeUntilLeft -= TimeSpan.FromSeconds(deltaTime.TotalSeconds);

            _timeUntilRight -= TimeSpan.FromSeconds(deltaTime.TotalSeconds);

            string timeString = TimePassed.Minutes.ToString("00") + ":" + TimePassed.Seconds.ToString("00");



            if (_enemyCount <= _randomcounts)
            {
                _enemyCount++;

                var randomPosition = new Vector3F(
                    RandomHelper.Random.NextFloat(-10, 10),
                    RandomHelper.Random.Next(3, 3),
                    RandomHelper.Random.NextFloat(-30, -50));
                //var pose = new Pose(randomPosition, RandomHelper.Random.NextQuaternionF());
                var pose = new Pose(new Vector3F(5, 3, 50), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

                var scene = _services.GetInstance<IScene>();

                _collisionObject.Enabled = true;

                StartPose = pose;


                ModelNode model = _modelPrototype.Clone();

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

                var cameraGameObjects =
                    (PlayerObjects.Player)GameObjectService.Objects["VehicleOpponent"];
                ModelNode playerNode = cameraGameObjects._modelNode;
                //CameraNode cameraNodes = cameraGameObjects.CameraNode;
                Quaternion killerOrientationQuaternion = new Quaternion();
                if ((_models[i].PoseWorld.Position - playerNode.PoseWorld.Position).Length <= 500)
                {

                    var translation = new Vector3F();
                    var cameraOrientation = new QuaternionF();
                    Pose newPose = new Pose
                    {
                        Position = playerNode.PoseWorld.Position,
                        Orientation = playerNode.PoseWorld.Orientation
                    };


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
                    float distanceFromCat = (_models[i].PoseWorld.Position - playerNode.PoseWorld.Position).Length;
                    if (distanceFromCat > tankChaseThreshold)
                    {
                        // just like the mouse, if the tank is far away from the cat, it should
                        // idle.
                        killerState = KillerAiState.Wander;
                    }
                    else if (distanceFromCat > tankCaughtThreshold)
                    {
                        killerState = KillerAiState.Chasing;
                    }
                    else
                    {
                        killerState = KillerAiState.Caught;
                    }

               
                    // Third, once we know what state we're in, act on that state.
                    float currentTankSpeed;
                    if (killerState == KillerAiState.Chasing)
                    {                      
                        var killerlength = _models[i].PoseWorld.Position.Length;

                        var playerLength = playerNode.PoseWorld.Position.Length;

                        if (playerLength > killerlength)
                        {
                            
                        }
                        else
                        {
                            
                        }
                  

                        float distanceFromkiller = (_models[i].PoseWorld.Position - playerNode.PoseWorld.Position).Length;                                                                                                  

                        if (_models[i].PoseWorld.Position.Z > playerNode.PoseWorld.Position.Z)
                        {
                            _linearVelocityMagnitude += 0.5f;
                        }
                        else if (_models[i].PoseWorld.Position.Z < playerNode.PoseWorld.Position.Z)
                        {
                            _linearVelocityMagnitude -= 0.5f;
                        }

                        if (_linearVelocityMagnitude > 59)
                        {
                            _linearVelocityMagnitude -= 0.5f;

                        }else if (_linearVelocityMagnitude < 0.0f)
                        {
                            _linearVelocityMagnitude = 0.0f;
                        }

                        Vector3F forwardCameraPose = newPose.ToWorldDirection(Vector3F.Forward);                      
                      
                        // Multiply the velocity by time to get the translation for this frame.
                        translation = forwardCameraPose * _linearVelocityMagnitude * deltaTimeF;

                        _models[i].SetLastPose(true);

                        _bodies[i].Pose = new Pose(_models[i].PoseWorld.Position + translation, newPose.Orientation);
                       
                    }
                    else if (killerState == KillerAiState.Wander)
                    {


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
                                           (_nestedListanimation[i][j + 1].Time - _nestedListanimation[i][j].Time).TotalSeconds);

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

                        QuaternionF cameraOrientations = QuaternionF.CreateRotationY(_shipRotation.Y) *
                                                        QuaternionF.CreateRotationX(_shipRotation.X) *
                                                        QuaternionF.CreateRotationZ(MathHelper.ToRadians(_shipRotation.Z));

                        _shipRotation = new Vector3F(0, 0, _shipMovement.Z);

                        _shipMovement = cameraOrientation.Rotate(_shipRotation);

                        _linearVelocityMagnitude = 5f;

                        // Multiply the velocity by time to get the translation for this frame.
                        translation = _shipMovement * _linearVelocityMagnitude * deltaTimeF;                     

                        _models[i].SetLastPose(true);

                        _bodies[i].Pose = new Pose(_models[i].PoseWorld.Position + translation, cameraOrientations);
                                           
                    }
                    else
                    {
                        _linearVelocityMagnitude = 0.0f;                   
                    }
                   
                    _models[i].PoseWorld = _bodies[i].Pose;

                    Pose = _models[i].PoseWorld;

                    Vector3F thirdPersonDistance = cameraOrientation.Rotate(new Vector3F(0, 1, 15));

                    // Compute camera pose (= position + orientation). 
                    _cameraNode.PoseWorld = new Pose
                    {
                        Position = Pose.Position // Floor position of character
                                   + new Vector3F(0, 1.6f, 0) // + Eye height
                                   + thirdPersonDistance,
                        Orientation = cameraOrientation.ToRotationMatrix33()
                    };
                }
                else
                {

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
                                       (_nestedListanimation[i][j + 1].Time - _nestedListanimation[i][j].Time).TotalSeconds);

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

                    QuaternionF cameraOrientation = QuaternionF.CreateRotationY(_shipRotation.Y) *
                                                    QuaternionF.CreateRotationX(_shipRotation.X) *
                                                    QuaternionF.CreateRotationZ(MathHelper.ToRadians(_shipRotation.Z));

                    _shipRotation = new Vector3F(0, 0, _shipMovement.Z);

                    _shipMovement = cameraOrientation.Rotate(_shipRotation);

                    _linearVelocityMagnitude = 5f;

                    // Multiply the velocity by time to get the translation for this frame.
                    Vector3F translation = _shipMovement * _linearVelocityMagnitude * deltaTimeF;

                    _models[i].SetLastPose(true);

                    _bodies[i].Pose = new Pose(_models[i].PoseWorld.Position + translation, cameraOrientation);

                    _models[i].PoseWorld = _bodies[i].Pose;

                    Pose = _models[i].PoseWorld;



                    Vector3F thirdPersonDistance = cameraOrientation.Rotate(new Vector3F(0, 1, 15));

                    // Compute camera pose (= position + orientation). 
                    _cameraNode.PoseWorld = new Pose
                    {
                        Position = Pose.Position // Floor position of character
                                   + new Vector3F(0, 1.6f, 0) // + Eye height
                                   + thirdPersonDistance,
                        Orientation = cameraOrientation.ToRotationMatrix33()
                    };

                    if ((_models[i].PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared >= 1000.0 &&
                        (_models[i].PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared <= 3600.0)
                    {

                        if (_timeUntilExplosion <= TimeSpan.Zero)
                        {
                            Sound.Sound.PlayPassbySound(Sound.Sound.Sounds.Beep);
                            _timeUntilExplosion = ExplosionInterval;
                        }
                    }
                    else if ((_models[i].PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared >= 500.0 &&
                             (_models[i].PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared <= 1000.0)
                    {
                        if (_timeUntilExplosion <= TimeSpan.Zero)
                        {
                            Sound.Sound.PlayPassbySound(Sound.Sound.Sounds.Beep);
                            _timeUntilExplosion = ExplosionInterval;
                        }
                    }


                    ///////////////
                    var cameraGameObject =
                 (ThirdPersonCameraObject.ThirdPersonKillerCamera)_gameObjectService.Objects["ThirdPersonKillerCamera"];
                    CameraNode cameraNode = cameraGameObject.CameraNodeKillers;

                    Pose cameraPose = cameraNode.PoseWorld;


                    Vector3F cameraPosition = cameraNode.PoseWorld.Position;
                    Vector3F cameraDirection = cameraNode.PoseWorld.ToWorldDirection(Vector3F.Forward);

                    // Create a ray for picking.
                    var ray = new RayShape(cameraPosition, cameraDirection, 1000);

                    // The ray should stop at the first hit. We only want the first object.
                    ray.StopsAtFirstHit = true;

                    // The collision detection requires a CollisionObject.
                    var rayCollisionObject = new CollisionObject(new GeometricObject(ray, Pose.Identity));

                    // Assign the collision object to collision group 2. (In SampleGame.cs a
                    // collision filter based on collision groups was set. Objects for hit-testing
                    // are in group 2.)
                    rayCollisionObject.CollisionGroup = 2;

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
                    _timeUntilLaunchMissile -= TimeSpan.FromSeconds(deltaTime.TotalSeconds);
                    if (_spring != null)
                    {

                        Vector3F cameraPositions = cameraNode.PoseWorld.Position;
                        Vector3F cameraDirections = cameraNode.PoseWorld.ToWorldDirection(-Vector3F.UnitZ);

                        _spring.AnchorPositionBLocal = cameraPositions + cameraDirections * _springAttachmentDistanceFromObserver;

                        // Reduce the angular velocity by a certain factor. (This acts like a damping because we
                        // do not want the object to rotate like crazy.)
                        _spring.BodyA.AngularVelocity *= 0.9f;

                        //Vector3F forwardCameraPose = _spring.BodyA.Pose.ToWorldDirection(Vector3F.Forward);

                        //_bodies[i].LinearVelocity = forwardCameraPose * 100;

                        //_models[i].PoseWorld = _bodies[i].Pose;
                        if (_timeUntilLaunchMissile <= TimeSpan.Zero)
                        {
                            MissileObject.MissileObject missileObjectOne =
                                GameObjectService.Objects.OfType<MissileObject.MissileObject>().FirstOrDefault();
                            if (missileObjectOne != null)
                            {
                                missileObjectOne.Spawn(_models[i].PoseWorld, ray, cameraPose);
                                Sound.Sound.PlayMissileSound(true);
                            }

                            _timeUntilLaunchMissile = MissileInterval;
                        }
                    }
                    //////////////

                }

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

        #region Audio

        private void InitializeAudio(ContentManager contentManager)
        {

            // The camera defines the position of the audio listener.
            _listener = new AudioListener();


            var cameraGameObject =
              (ThirdPersonCameraObject.ThirdPersonCameraObject)_gameObjectService.Objects["ThirdPersonCamera"];
            cameraNodeAudio = cameraGameObject.CameraNodeMissile;

            // Set a distance scale that is suitable for our demo.
            SoundEffect.DistanceScale = 10;

          
        }   
        #endregion

        #region Dispose

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
                _stringBuilder.Append("Position: " + t.PoseWorld.Position);
            }
            _updateFpsTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("FThrusters: " + _fThrusters);
            _drawFpsTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("FRotation: " + _fRotation);
            _RotationTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("FRoll: " + _fRoll);
            _fRotationPositionBlock.Text = _stringBuilder.ToString();
        }

        #endregion

    }



}
