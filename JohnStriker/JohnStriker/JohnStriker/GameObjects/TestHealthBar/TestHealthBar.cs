using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DigitalRune.Animation;
using DigitalRune.Animation.Easing;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Geometry;
using DigitalRune.Geometry.Collisions;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Mathematics.Interpolation;
using DigitalRune.Mathematics.Statistics;
using DigitalRune.Physics;
using DigitalRune.Physics.Specialized;
using JohnStriker.GameObjects.OpponentObjects;
using JohnStriker.GameObjects.PlayerObjects;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects.TestHealthBar
{
    internal class TestHealthBar : GameObject
    {
        #region Fields

        //--------------------------------------------------------------

        private const float Tolerance = 60.0f;

        private const float Timer = 1;

        private const float TimerModel = 45;

        private const float M16DSpeed = 10.0f;

        private const int FrameStringAndEnemyCount = 10;
        private const float MaxSoundChangeSpeed = 3f;
        private const float MaxDistance = 60;

        // Contact forces below MinHitForce do not make a sound.
        private const float MinHitForce = 20000;
        private const float KillerChaseDistance = 600.0f;

        private const float KillerCaughtDistance = 10.0f;

        private const float KillerHysteresis = 15.0f;

        private static float LinearVelocityMagnitude;
        private static readonly TimeSpan ExplosionInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan SpeedInterval = TimeSpan.FromSeconds(30);

        private static float _linearVelocityMagnitude = 30f;
        private static readonly TimeSpan MissileInterval = TimeSpan.FromSeconds(1);

        private TimeSpan ElapsedTimeSpan = TimeSpan.FromSeconds(0);
        private readonly IAnimationService _animationService;
        private readonly RigidBody _bodyPrototype;

        //private readonly List<RigidBody> _bodyPrototype = new List<RigidBody>();

        private readonly CollisionObject _collisionObject;

        private readonly IGameObjectService _gameObjectService;

        private readonly GeometricObject _geometricObject;

        private readonly IGraphicsService _graphicsService;
        private readonly AnimatableProperty<float> _healthBarPoseAngle = new AnimatableProperty<float>();

        private readonly IInputService _inputService;
        private readonly List<RigidBody> _missileAttachedPrototypes;

        private readonly List<SceneNode> _missileAttachedSceneNode;

        private readonly ModelNode _missileModelNodes;

        private readonly List<ModelNode> _models = new List<ModelNode>();

        private readonly List<List<ObjectAnimationFrames>> _nestedListanimation =
            new List<List<ObjectAnimationFrames>>();

        private readonly TextBlock _rotationTextBlock;

        private readonly IServiceLocator _services;

        private readonly Simulation _simulation;

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly float _vehicleRotation;

        // Models for rendering.
        private readonly ModelNode[] _wheelModelNodes;
        private readonly SoundEffect flightPassByClose;

        private readonly SoundEffect flightPassByDistance;

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
        private CollisionObject _attachedMissileCollisionObject;

        private float _destinationZ;

        private float _destinationsZ;

        private float _destinationsZPosition;

        private float _distanceForMissile;

        private int _enemyCount;

        private float _fRoll;

        private float _fRotation;

        private float _fThrusters;
        private float _flee;
        private Vector3F _forwardModelPose = new Vector3F();

        private GeometricObject _geometricObjectMissile;

        private GuiMissileScreen _guiGraphicsScreen;
        private Pose _healthBarPose0;
        private Pose _healthBarPose1;
        private Pose _healthBarPose2;
        private Pose _healthBarPose3;
        private Pose _healthBarPose4;
        private Pose _healthBarPose5;
        private Pose _healthBarPose6;
        private Pose _healthBarPose7;
        private Pose _healthBarPose8;
        private Pose _healthBarPose9;
        private SceneNode _healthBarSceneNode0;
        private SceneNode _healthBarSceneNode1;
        private SceneNode _healthBarSceneNode2;
        private SceneNode _healthBarSceneNode3;
        private SceneNode _healthBarSceneNode4;
        private SceneNode _healthBarSceneNode5;
        private SceneNode _healthBarSceneNode6;
        private SceneNode _healthBarSceneNode7;
        private SceneNode _healthBarSceneNode8;
        private SceneNode _healthBarSceneNode9;
        private List<SceneNode> _healthBarSceneNodeList;
        private AudioEmitter[] _hitEmitters = new AudioEmitter[5];
        private SoundEffect _hitSound;
        private SoundEffectInstance[] _hitSoundInstances = new SoundEffectInstance[5];
        private string _identifyChasingWanderCaught = string.Empty;
        private bool _isMissile = true;
        private bool _isMissileAttacked;
        private AudioListener _listener;
        private Pose _missileAttachedSceneNodePose;

        private float _missileForce;

        public ModelNode _modelPrototype;

        private float _motorForce;
        private Pose _newPose = new Pose();

        private PointLight _pointLight;

        private int _randomcounts;
        private AudioEmitter _rollEmitter;
        private SoundEffect _rollSound;

        private SoundEffectInstance _rollSoundInstance;

        private IScene _scene;

        private Vector3F _shipMovement;

        private Vector3F _shipRotation;
        private float _soundIcrement;
        private float _speedIncrement = 48.0f;

        private float _speedInitialize = 5f;

        private float _speedMissile = 0f;

        private float _steeringAngle;
        private float _timeSinceLastHitSound;
        private TimeSpan _timeUntilExplosion = TimeSpan.Zero;
        private TimeSpan _timeUntilLaunchMissile = TimeSpan.Zero;
        private TimeSpan _timeUntilSpeed = TimeSpan.Zero;

        private TextBlock _updateFpsTextBlock;
        private CameraNode cameraNodeAudio;
        private CameraNode cameraNodeMissile;

        // Pitch information
        private float fPitch;

        private int increment = 0;

        private bool isVehicleRotation;
        private int j;

        private Pose jetPose;
        private int numberOfRollingContacts = 0;

        private Matrix33F pitch;
        private Vector3F rollCenter = Vector3F.Zero;

        private float rollSpeed = 0;

        private Texture2D sparkTexture2D;

        private float timer = 1; //Initialize a 10 second timer

        private float timerModel = 45; //Initialize a 10 second timer

        private Vehicle vehicleObject;

        private Pose missilePose { get; set; }

        private Vector3F Rotation { get; set; }
        private Vector3F Position { get; set; }

        // A sound of a rolling object.

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
            get { return _missileAttachedSceneNodePose; }
            set
            {
                foreach (SceneNode sceneNode in _missileAttachedSceneNode)
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

        public TestHealthBar(IServiceLocator services, int objectCount)
        {
            _services = services;

            Name = "KillerOpponents8s" + objectCount;
            _gameObjectService = services.GetInstance<IGameObjectService>();
            _inputService = _services.GetInstance<IInputService>();
            _simulation = _services.GetInstance<Simulation>();
            _animationService = services.GetInstance<IAnimationService>();
            _graphicsService = _services.GetInstance<IGraphicsService>();

            var contentManager = _services.GetInstance<ContentManager>();

            _bodyPrototype = new RigidBody(new BoxShape(5, 0, 5));
            _modelPrototype = contentManager.Load<ModelNode>("SlowKiller/skyfighter fbx").Clone();
            var randomPosition = new Vector3F(
                RandomHelper.Random.NextFloat(-10, 10),
                RandomHelper.Random.NextFloat(2, 5),
                RandomHelper.Random.NextFloat(-30, -50));

            _bodyPrototype.Pose = new Pose(randomPosition, RandomHelper.Random.NextQuaternionF());
            _modelPrototype.PoseWorld = _bodyPrototype.Pose;

            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(_modelPrototype);
          

            var shape = (Shape) _modelPrototype.UserData;
            _geometricObject = new GeometricObject(shape, _modelPrototype.PoseWorld);
            _collisionObject = new CollisionObject(_geometricObject);
            _collisionObject.Enabled = true;
            _collisionObject.Type = CollisionObjectType.Trigger;
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

            _missileAttachedPrototypes = new List<RigidBody>();

            _missileAttachedSceneNode = new List<SceneNode>();

            CreateAndStartAnimations(0.3f, 0.3f);

            var cameraGameObject =
               (ThirdPersonCameraObject.ThirdPersonCameraObject)_gameObjectService.Objects["ThirdPersonCamera"];
            cameraNodeAudio = cameraGameObject.CameraNodeMissile;

            var listTimeSpan = new List<TimeSpan> { _nestedListanimation[0][0].Time };
            nestedListTimeSpan.Add(listTimeSpan);
        
        }

        private static bool IsOdd(int value)
        {
            return value%2 != 0;
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
                    EasingFunction = new CircleEase {Mode = EasingMode.EaseInOut},
                })
            {
                Duration = TimeSpan.MaxValue,
                LoopBehavior = LoopBehavior.Oscillate,
            };
            _animationService.StartAnimation(healthBarSceneNodeAnimation, _healthBarPoseAngle)
                .AutoRecycle();

            Matrix33F healthBarPoseAngleRotation = Matrix33F.CreateRotationZ(_healthBarPoseAngle.Value);
            _healthBarSceneNodeList = new List<SceneNode>();
            _healthBarSceneNode0.PoseLocal = _healthBarPose0*new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode0);
            _healthBarSceneNode1.PoseLocal = _healthBarPose1*new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode1);
            _healthBarSceneNode2.PoseLocal = _healthBarPose2*new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode2);
            _healthBarSceneNode3.PoseLocal = _healthBarPose3*new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode3);
            _healthBarSceneNode4.PoseLocal = _healthBarPose4*new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode4);
            _healthBarSceneNode5.PoseLocal = _healthBarPose5*new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode5);
            _healthBarSceneNode6.PoseLocal = _healthBarPose6*new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode6);
            _healthBarSceneNode7.PoseLocal = _healthBarPose7*new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode7);
            _healthBarSceneNode8.PoseLocal = _healthBarPose8*new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode8);
            _healthBarSceneNode9.PoseLocal = _healthBarPose9*new Pose(healthBarPoseAngleRotation);
            _healthBarSceneNodeList.Add(_healthBarSceneNode9);
        }

        protected override void OnUpdate(TimeSpan deltaTime)
        {
            if (!_inputService.EnableMouseCentering)
                return;

            var deltaTimeF = (float) deltaTime.TotalSeconds;

            _randomcounts = 0;

            TimePassed += TimeSpan.FromSeconds(deltaTime.TotalSeconds);

            _timeUntilExplosion -= TimeSpan.FromSeconds(deltaTime.TotalSeconds);

            string timeString = TimePassed.Minutes.ToString("00") + ":" + TimePassed.Seconds.ToString("00");

            bool timeStrings = IsOdd(Convert.ToInt32(TimePassed.Seconds.ToString("00")));

            SceneNode listHealth = _healthBarSceneNodeList.LastOrDefault();

            if (listHealth != null)
                listHealth.IsEnabled = timeStrings != true;
            else
                Dispose();

            _isMissileAttacked = false;

            ElapsedTimeSpan += deltaTime;

            TimeSpan totalTime = ElapsedTimeSpan;

            TimeSpan end = _nestedListanimation[0][_nestedListanimation[0].Count - 1].Time;

            var playerGameObjects = (Player) _gameObjectService.Objects["VehicleOpponent"];

            CollisionObject collisionObject = playerGameObjects.AttachedMissileCollisionObject;

            List<SceneNode> listSceneNode = playerGameObjects._missileAttachedSceneNode;

            //loop ariound the total time if necessary
            if (Loop)
            {
                while (totalTime > end)
                    totalTime -= end;
            }
            else // Otherwise, clamp to the end values
            {
                Position = _nestedListanimation[0][_nestedListanimation[0].Count - 1].ValPosition;
                Rotation = _nestedListanimation[0][_nestedListanimation[0].Count - 1].ValRotation;
                return;
            }

            Position = _nestedListanimation[0][_nestedListanimation[0].Count - 1].ValPosition;

            int j = 0;

            //find the index of the current frame
            while (_nestedListanimation[0][j + 1].Time < totalTime)
            {
                j++;
            }

            // Find the time since the beginning of this frame
            totalTime -= _nestedListanimation[0][j].Time;

            // Find how far we are between the current and next frame (0 to 1)
            var amt = (float) ((totalTime.TotalSeconds)/
                               (_nestedListanimation[0][j + 1].Time - _nestedListanimation[0][j].Time).TotalSeconds);

            // Interpolate position and rotation values between frames

            Position = InterpolationHelper.Lerp(_nestedListanimation[0][j].ValPosition,
                _nestedListanimation[0][j + 1].ValPosition, amt);

            Rotation = InterpolationHelper.Lerp(_nestedListanimation[0][j].ValRotation,
                _nestedListanimation[0][j + 1].ValRotation, amt);

            if (listSceneNode != null)
            {
                for (int l = 0; l < listSceneNode.Count; l++)
                {
                    if (listSceneNode[l] != null)
                    {
                        _flee = (_modelPrototype.PoseWorld.Position - listSceneNode[l].PoseWorld.Position).LengthSquared;

                        if ((_modelPrototype.PoseWorld.Position - listSceneNode[l].PoseWorld.Position).LengthSquared >=
                            1000.0 &&
                            (_modelPrototype.PoseWorld.Position - listSceneNode[l].PoseWorld.Position).LengthSquared <=
                            3600.0)
                        {
                            _isMissileAttacked = true;
                        }
                    }
                }
            }

            if (_isMissileAttacked)
            {
                Control(new Vector3F(1, 1, 1));

                int y = RandomHelper.Random.NextInteger(2, 4);

                if (y == 2)
                {
                    Control(new Vector3F(2, 2, 2));
                }
                else
                {
                    Control(new Vector3F(3, 3, 3));
                }
            }
            else
            {
                Control(Position);

                Control(Rotation);
            }


            UpdateThrusters();

            UpdateRotation();

            UpdateRoll();

            UpdatePitch();

            QuaternionF cameraOrientation = QuaternionF.CreateRotationY(_shipRotation.Y)*
                                            QuaternionF.CreateRotationX(_shipRotation.X)*
                                            QuaternionF.CreateRotationZ(MathHelper.ToRadians(_shipRotation.Z));

            _shipRotation = new Vector3F(0, 0, _shipMovement.Z);

            _shipMovement = cameraOrientation.Rotate(_shipRotation);

            // Multiply the velocity by time to get the translation for this frame.
            Vector3F translation = _shipMovement*LinearVelocityMagnitude*deltaTimeF;

            _modelPrototype.SetLastPose(true);

            _bodyPrototype.Pose = new Pose(_modelPrototype.PoseWorld.Position + translation, cameraOrientation);

            _modelPrototype.PoseWorld = _bodyPrototype.Pose;

            Pose = _modelPrototype.PoseWorld;

            if ((_modelPrototype.PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared >= 1000.0 &&
                (_modelPrototype.PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared <= 3600.0)
            {
                if (_timeUntilExplosion <= TimeSpan.Zero)
                {
                    Sound.Sound.PlayPassbySound(Sound.Sound.Sounds.Beep);
                    _timeUntilExplosion = ExplosionInterval;
                }
            }
            else if ((_modelPrototype.PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared >= 500.0 &&
                     (_modelPrototype.PoseWorld.Position - cameraNodeAudio.PoseWorld.Position).LengthSquared <= 1000.0)
            {
                if (_timeUntilExplosion <= TimeSpan.Zero)
                {
                    Sound.Sound.PlayPassbySound(Sound.Sound.Sounds.Beep);
                    _timeUntilExplosion = ExplosionInterval;
                }
            }


            //if (collisionObject != null)
            //{
            //    _flee = (_models[i].PoseWorld.Position - collisionObject.GeometricObject.Pose.Position).LengthSquared;

            //    if ((_models[i].PoseWorld.Position - collisionObject.GeometricObject.Pose.Position).LengthSquared >= 1000.0 &&
            //        (_models[i].PoseWorld.Position - collisionObject.GeometricObject.Pose.Position).LengthSquared <= 3600.0)
            //    {

            //    }
            //}


            //UpdateProfiler();
        }

        #region Dispose

        private void Dispose()
        {
            for (int i = 0; i < _models.Count; i++)
            {
                ModelNode model = _models[i];
                RigidBody body = _bodyPrototype;

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


            _collisionObject.Enabled = false;
        }

        public void DisposeByMissile(int hitCount)
        {
            HitCount = hitCount;

            string percent = (100 - HitCount).ToString();

            percent = percent + "%";

            if (percent.IndexOf("0", StringComparison.Ordinal) == 1 || percent == "0%")
            {
                SceneNode listHealth = _healthBarSceneNodeList.LastOrDefault();
                if (listHealth != null) listHealth.IsEnabled = false;
                _healthBarSceneNodeList.Remove(listHealth);
            }
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

            _shipRotation.Z = -(_fRoll*_fThrusters);
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
            //foreach (ModelNode t in _models)
            //{
            //    _stringBuilder.Append("Position: " + t.PoseWorld.Position);
            //}
            _stringBuilder.Clear();
            _stringBuilder.Append("Position: " + _flee);
            _updateFpsTextBlock.Text = _stringBuilder.ToString();
        }

        #endregion
    }
}