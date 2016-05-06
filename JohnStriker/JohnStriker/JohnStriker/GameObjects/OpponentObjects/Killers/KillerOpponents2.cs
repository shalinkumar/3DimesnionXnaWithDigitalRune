using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DigitalRune.Animation;
using DigitalRune.Game;
using DigitalRune.Game.Input;
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
        public class KillerOpponents1 : GameObject
        {
            #region Fields

            //--------------------------------------------------------------

            private const float Tolerance = 60.0f;

            private const float Timer = 1;

            private const float TimerModel = 45;

            private const float M16DSpeed = 10.0f;

            private const int FrameStringAndEnemyCount = 10;

            private const float LinearVelocityMagnitude = 5f;

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

            private CameraNode cameraNodeAudio;

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

            private BallJoint _spring;

            private float _springAttachmentDistanceFromObserver;

            public readonly CameraNode cameraNodeKillerOpponents;

            private readonly MyGraphicsScreen _graphicsScreen;
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


            public KillerOpponents1(IServiceLocator services, int objectCount)
            {
                _services = services;

                Name = "KillerOpponents" + objectCount;
                _gameObjectService = services.GetInstance<IGameObjectService>();
                _inputService = _services.GetInstance<IInputService>();
                _simulation = _services.GetInstance<Simulation>();

                _graphicsService = _services.GetInstance<IGraphicsService>();
                _graphicsScreen = new MyGraphicsScreen(services) { DrawReticle = true };

                //// Add the GuiGraphicsScreen to the graphics service.
                //_guiGraphicsScreen = new GuiMissileScreen(_services);
                //_graphicsService.Screens.Add(_guiGraphicsScreen);

                //// ----- FPS Counter (top right)
                //_fpsPanel = new StackPanel
                //{
                //    Margin = new Vector4F(10),
                //    HorizontalAlignment = HorizontalAlignment.Right,
                //    VerticalAlignment = VerticalAlignment.Bottom,
                //};
                //_guiGraphicsScreen.UIScreen.Children.Add(_fpsPanel);
                //_updateFpsTextBlock = new TextBlock
                //{
                //    Font = "DejaVuSans",
                //    Foreground = Color.Black,
                //    HorizontalAlignment = HorizontalAlignment.Right,
                //    Text = "Position",
                //};
                //_fpsPanel.Children.Add(_updateFpsTextBlock);

                // Create a camera.
                var projection = new PerspectiveProjection();
                projection.SetFieldOfView(
                    ConstantsF.PiOver4,
                    _graphicsService.GraphicsDevice.Viewport.AspectRatio,
                    0.1f,
                    1000.0f);
                cameraNodeKillerOpponents = new CameraNode(new Camera(projection));
                _graphicsScreen.ActiveCameraNode = cameraNodeKillerOpponents;

                // A simple cube.

                // Load models for rendering.
                var contentManager = _services.GetInstance<ContentManager>();
                InitializeAudio(contentManager);
                //_bodyPrototype = new RigidBody(modelPShape);

                _bodyPrototype = new RigidBody(new BoxShape(5, 0, 5));

                //flightPassByClose = contentManager.Load<SoundEffect>("Audio/Jet_FA18s_Pass05_low");

                //flightPassByDistance = contentManager.Load<SoundEffect>("Audio/Jet_FA18s_Pass05");

                _modelPrototype = contentManager.Load<ModelNode>("M16D/skyfighter fbx").Clone();
                //_modelPrototype.ScaleLocal = new Vector3F(0.06f);

                //CreateRigidBody();

                _bodyPrototype.Pose = new Pose(new Vector3F(0, 3, 3), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

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

                //Sound.Sound.StartPassBySound();
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


                //////////////////////////////
                // 
                
                /////////////////////////////
                _randomcounts = 0;

                TimePassed += TimeSpan.FromSeconds(deltaTime.TotalSeconds);

                _timeUntilExplosion -= TimeSpan.FromSeconds(deltaTime.TotalSeconds);

                string timeString = TimePassed.Minutes.ToString("00") + ":" + TimePassed.Seconds.ToString("00");



                if (_enemyCount <= _randomcounts)
                {
                    _enemyCount++;

                    var randomPosition = new Vector3F(
                        RandomHelper.Random.NextFloat(-10, 10),
                        RandomHelper.Random.NextFloat(2, 5),
                        RandomHelper.Random.NextFloat(-30, -50));
                    //var pose = new Pose(randomPosition, RandomHelper.Random.NextQuaternionF());
                    var pose = new Pose(new Vector3F(0, 3, -5), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

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

                    Position = _nestedListanimation[i][_nestedListanimation[i].Count - 1].ValPosition;

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

                    //Control(Position);

                    //Control(Rotation);

                    UpdateThrusters();

                    UpdateRotation();

                    UpdateRoll();

                    UpdatePitch();

                    QuaternionF cameraOrientation = QuaternionF.CreateRotationY(_shipRotation.Y) *
                                                    QuaternionF.CreateRotationX(_shipRotation.X) *
                                                    QuaternionF.CreateRotationZ(MathHelper.ToRadians(_shipRotation.Z));

                    _shipRotation = new Vector3F(0, 0, _shipMovement.Z);

                    _shipMovement = cameraOrientation.Rotate(_shipRotation);

                    // Multiply the velocity by time to get the translation for this frame.
                    Vector3F translation = _shipMovement * LinearVelocityMagnitude * deltaTimeF;

                    _models[i].SetLastPose(true);

                    _bodies[i].Pose = new Pose(_models[i].PoseWorld.Position + translation, cameraOrientation);

                    _models[i].PoseWorld = _bodies[i].Pose;

                    Pose = _models[i].PoseWorld;

                    cameraNodeKillerOpponents.PoseWorld = new Pose
                    {
                        Position = _models[i].PoseWorld.Position // Floor position of character
                                   + new Vector3F(0, 1.6f, 0) ,                                 
                        Orientation = cameraOrientation.ToRotationMatrix33()
                    };

                    if ((_models[i].PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared >= 1000.0 && (_models[i].PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared <= 3600.0)
                    {

                        if (_timeUntilExplosion <= TimeSpan.Zero)
                        {
                            Sound.Sound.PlayPassbySound(Sound.Sound.Sounds.Beep);
                            _timeUntilExplosion = ExplosionInterval;
                        }
                    }
                    else if ((_models[i].PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared >= 500.0 && (_models[i].PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared <= 1000.0)
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

                    if (_spring != null)
                    {

                        Vector3F cameraPositions = cameraNode.PoseWorld.Position;
                        Vector3F cameraDirections = cameraNode.PoseWorld.ToWorldDirection(-Vector3F.UnitZ);

                        _spring.AnchorPositionBLocal = cameraPositions + cameraDirections * _springAttachmentDistanceFromObserver;

                        // Reduce the angular velocity by a certain factor. (This acts like a damping because we
                        // do not want the object to rotate like crazy.)
                        _spring.BodyA.AngularVelocity *= 0.9f;                    

                        Vector3F forwardCameraPose = _spring.BodyA.Pose.ToWorldDirection(Vector3F.Forward);

                        _bodies[i].LinearVelocity = forwardCameraPose * 100;

                        _models[i].PoseWorld = _bodies[i].Pose;
                    }
                     
                    //////////////
                }

                //UpdateProfiler();
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

                // ----- Load sounds, create instances and emitters.
                //_hitSound = contentManager.Load<SoundEffect>("Audio/Jet_FA18s_Pass05_low");
                //for (int i = 0; i < _hitSoundInstances.Length; i++)
                //{
                //    _hitSoundInstances[i] = _hitSound.CreateInstance();
                //    // Change pitch. Our instance sounds better this way.
                //    _hitSoundInstances[i].Pitch = -1;
                //    _hitEmitters[i] = new AudioEmitter();
                //}


                //_rollSound = contentManager.Load<SoundEffect>("Audio/Jet_FA18s_Pass05");
                //_rollSoundInstance = _rollSound.CreateInstance();
                //_rollEmitter = new AudioEmitter();

                //_rollSoundInstance.IsLooped = true;


            }

            public void PlayAudio(float deltaTime)
            {

                if (_soundIcrement >= 1.0f)
                {
                    _soundIcrement = 1.0f;
                }
                _timeSinceLastHitSound += deltaTime;
                if (numberOfRollingContacts > 0 && _timeSinceLastHitSound > 0.1f)
                {
                    // ----- Play hit sounds.

                    // Find a not playing hit sound effect instance.
                    int index = -1;
                    for (int i = 0; i < _hitSoundInstances.Length; i++)
                    {
                        if (_hitSoundInstances[i].State != SoundState.Playing)
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index != -1)
                    {

                        var newPosition = _modelPrototype.PoseWorld.Position / numberOfRollingContacts;

                        // Set the sound emitter to the average hit position.
                        _hitEmitters[index].Position = (Vector3)newPosition;

                        // Make the volume proportional to the collision force.
                        var newVolume = (MinHitForce - Math.Abs(_fThrusters)) / (200000 - MinHitForce);
                        _hitSoundInstances[index].Volume = Math.Min(newVolume, 0.4f);

                        // Play 3D sound.
                        _hitSoundInstances[index].Apply3D(_listener, _hitEmitters[index]);
                        _hitSoundInstances[index].Play();
                        _timeSinceLastHitSound = 0;
                    }
                }

            }

            // Continually changes the volume, avoiding sudden changes.



            // Continually changes the pitch. Similar to ChangeVolume().



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
            }

            #endregion
        }
    
}
