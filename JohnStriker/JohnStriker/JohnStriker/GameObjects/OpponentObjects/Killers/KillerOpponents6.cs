﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using DigitalRune.Mathematics.Interpolation;
using DigitalRune.Mathematics.Statistics;
using DigitalRune.Physics;
using DigitalRune.Physics.Constraints;
using JohnStriker.Annotations;
using JohnStriker.GameObjects.PlayerObjects;
using JohnStriker.GameObjects.ThirdPersonCameraObject;
using JohnStriker.GraphicsScreen;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects.OpponentObjects.Killers
{
    public class KillerOpponents6 : GameObject
    {
        #region Constants

        //--------------------------------------------------------------
        private const float M16DSpeed = 10.0f;

        #endregion

        #region Fields

        //--------------------------------------------------------------   
        // Current velocity from gravity.

        private const float KillerChaseDistance = 1000.0f;


        private const float KillerCaughtDistance = 10.0f;
        private const float KillerHysteresis = 15.0f;
        private static float LinearVelocityMagnitude;
        private static readonly TimeSpan MissileInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan ExplosionInterval = TimeSpan.FromSeconds(1);
        private static float _linearVelocityMagnitude = 30f;
        public readonly CameraNode CameraNode;
        private readonly IGameObjectService GameObjectService;
        private readonly TextBlock _rotationTextBlock;
        private readonly List<RigidBody> _bodies = new List<RigidBody>();

        private readonly CollisionObject _collisionObject;
        private readonly TextBlock _drawFpsTextBlock;
        private readonly List<TimeSpan> _elapsedTimeSpan = new List<TimeSpan>();
        private readonly TextBlock _fRotationPositionBlock;
        private readonly IGameObjectService _gameObjectService;

        private readonly GeometricObject _geometricObject;

        private readonly ModelNode _modelNode;
        private readonly List<ModelNode> _models = new List<ModelNode>();
        private readonly List<List<TimeSpan>> _nestedListTimeSpan = new List<List<TimeSpan>>();

        private readonly List<List<ObjectAnimationFrames>> _nestedListanimation =
            new List<List<ObjectAnimationFrames>>();

        private readonly IServiceLocator _services;
        private readonly Simulation _simulation;


        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly TextBlock _updateFpsTextBlock;
        private bool Loop = true;
        private RigidBody _bodyPrototype;
        private CameraNode _cameraNodeAudio;
        private int _enemyCount;
        private float _fPitch;


        private float _fRoll;

        private float _fRotation;

        // Movement thrusters
        private float _fThrusters;
        private int _randomcounts;

        private Vector3F _shipMovement;

        private Vector3F _shipRotation;

        // Sine wave information (floating behavior)

        private BallJoint _spring;

        private float _springAttachmentDistanceFromObserver;

        [UsedImplicitly]
        private TimeSpan _timePassed;
        private TimeSpan _timeUntilExplosion = TimeSpan.Zero;
        private TimeSpan _timeUntilLaunchMissile = TimeSpan.Zero;
        private KillerAiState killerState = KillerAiState.Wander;

        private Pose StartPose { get; set; }

        private Vector3F Rotation { get; set; }
        private Vector3F Position { get; set; }

        private string _identifyChasingWanderCaught = string.Empty;

        private float _speedIncrement = 48.0f;

        private TimeSpan _timeUntilSpeed = TimeSpan.Zero;

        private static readonly TimeSpan SpeedInterval = TimeSpan.FromSeconds(30);
        #endregion

        #region Properties

        //--------------------------------------------------------------


        // The collision object used for collision detection. 
        // The collision object which can be used for contact queries.

        private readonly IInputService _inputService;

        public CollisionObject CollisionObject
        {
            get { return _collisionObject; }
        }


        // The bottom position (the lowest point on the capsule).


        public Pose Pose
        {
            get { return _modelNode.PoseWorld; }
            // ReSharper disable ValueParameterNotUsed
            private set
            // ReSharper restore ValueParameterNotUsed
            {
                _geometricObject.Pose = _modelNode.PoseWorld;
            }
        }

        private float SoundIcrement { get; set; }

        #endregion

        #region Creation & Cleanup

        //--------------------------------------------------------------

        public KillerOpponents6(IServiceLocator services, int objectCount)
        {
            Name = "KillerOpponents5" + objectCount;
            _services = services;
            var contentManager = services.GetInstance<ContentManager>();

            _gameObjectService = services.GetInstance<IGameObjectService>();

            InitializeAudio();

            _inputService = services.GetInstance<IInputService>();

            var graphicsScreen = new MyGraphicsScreen(services) { DrawReticle = true };

            _simulation = services.GetInstance<Simulation>();

            var graphicsService = services.GetInstance<IGraphicsService>();

            GameObjectService = services.GetInstance<IGameObjectService>();

            // Add the GuiGraphicsScreen to the graphics service.
            var guiKiller = new GuiKiller(services);

            graphicsService.Screens.Add(guiKiller);

            // ----- FPS Counter (top right)

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
                Foreground = Color.DarkBlue,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = "_fRotationPosition",
            };
            _fpsPanel.Children.Add(_fRotationPositionBlock);

            // Create a camera.
            var projection = new PerspectiveProjection();
            projection.SetFieldOfView(
                ConstantsF.PiOver4,
                graphicsService.GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                1000.0f);
            CameraNode = new CameraNode(new Camera(projection));
            graphicsScreen.ActiveCameraNode = CameraNode;

            _bodyPrototype = new RigidBody(new BoxShape(5, 0, 5));

            // ----- Graphics
            // Load a graphics model and add it to the scene for rendering.
            _modelNode = contentManager.Load<ModelNode>("M16D/skyfighter fbx").Clone();

            CreateRigidBody();

            //_bodyPrototype.Pose = new Pose(new Vector3F(0, 3, 12), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

            _modelNode.PoseWorld = _bodyPrototype.Pose;

            // Load collision shape from a separate model (created using the CollisionShapeProcessor).
            var shape = (Shape)_modelNode.UserData;
            //var shape = contentManager.Load<Shape>("Ship/Ship_CollisionModel");

            _geometricObject = new GeometricObject(shape, _modelNode.PoseWorld);
            // Create a collision object for the game object and add it to the collision domain.
            _collisionObject = new CollisionObject(_geometricObject);


            var collisionDomain = services.GetInstance<CollisionDomain>();

            collisionDomain.CollisionObjects.Add(CollisionObject);

            _collisionObject.Enabled = true;

            if (IsOdd(objectCount))
            {
                var frameStrings = new List<ObjectAnimationFrames>();
                frameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")), 15))));
                frameStrings.Add(new ObjectAnimationFrames("thrusters forward", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 5, 20))));
                frameStrings.Add(new ObjectAnimationFrames("", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 10, 25))));
                frameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 15, 30))));
                frameStrings.Add(new ObjectAnimationFrames("thrusters forward", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 20, 35))));
                frameStrings.Add(new ObjectAnimationFrames("", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 25, 40))));
                frameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 30, 45))));
                _nestedListanimation.Add(frameStrings);
            }
            else
            {
                var frameStrings = new List<ObjectAnimationFrames>();
                frameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")), 15))));
                frameStrings.Add(new ObjectAnimationFrames("thrusters forward", "rotate right", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 5, 20))));
                frameStrings.Add(new ObjectAnimationFrames("", "rotate right", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 10, 25))));
                frameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 15, 30))));
                frameStrings.Add(new ObjectAnimationFrames("thrusters forward", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 20, 35))));
                frameStrings.Add(new ObjectAnimationFrames("", "rotate left", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 25, 40))));
                frameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "",
                    TimeSpan.FromSeconds(
                        RandomHelper.Random.NextDouble(Convert.ToInt32(_timePassed.Seconds.ToString("00")) + 30, 45))));
                _nestedListanimation.Add(frameStrings);
            }

            var missileObject = new MissileObject.MissileObject(services, "KillerMissile");
            GameObjectService.Objects.Add(missileObject);
        }

        public static bool IsOdd(int value)
        {
            return value % 2 != 0;
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
                Pose = new Pose(new Vector3F(0, 3, 5), Matrix33F.CreateRotationY(-ConstantsF.PiOver2)),
                Scale = _modelNode.ScaleLocal,
                MotionType = MotionType.Static
            };

            // Add rigid body to physics simulation and model to scene.           
            _simulation.RigidBodies.Add(_bodyPrototype);
        }

        protected override void OnUpdate(TimeSpan timeSpan)
        {
            // Get elapsed time.
            if (!_inputService.EnableMouseCentering)
            {
                return;
            }

            var deltaTimeF = (float)timeSpan.TotalSeconds;

            _timeUntilExplosion -= TimeSpan.FromSeconds(timeSpan.TotalSeconds);
            _timeUntilSpeed -= TimeSpan.FromSeconds(timeSpan.TotalSeconds);
            _randomcounts = 0;

            var modelOrientation = new QuaternionF();

            if (_enemyCount <= _randomcounts)
            {
                _enemyCount++;

                var randomPosition = new Vector3F(
                    RandomHelper.Random.NextFloat(-10, 10),
                    RandomHelper.Random.NextFloat(2, 5),
                    RandomHelper.Random.NextFloat(-30, -50));
                //var pose = new Pose(randomPosition, RandomHelper.Random.NextQuaternionF());
                var pose = new Pose(new Vector3F(0, 3, 50), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));
                var scene = _services.GetInstance<IScene>();

                _collisionObject.Enabled = true;

                StartPose = pose;


                ModelNode model = _modelNode.Clone();

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

                _nestedListTimeSpan.Add(listTimeSpan);

                _elapsedTimeSpan.Add(TimeSpan.FromSeconds(0));
            }


            for (int i = 0; i < _models.Count; i++)
            {
                var cameraGameObjects = (Player)GameObjectService.Objects["VehicleOpponent"];
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
                    killerState = KillerAiState.Chasing;
                }
                else
                {
                    killerState = KillerAiState.Caught;
                }


                // Third, once we know what state we're in, act on that state.                
                if (killerState == KillerAiState.Chasing)
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

                    Pose newPose = Pose.FromMatrix(lookAtMatrix).Inverse;

                    Vector3F forwardModelPose = newPose.ToWorldDirection(Vector3F.Forward);

                    LinearVelocityMagnitude = _linearVelocityMagnitude;

                    Vector3F translation = forwardModelPose * LinearVelocityMagnitude * deltaTimeF;

                    _bodies[i].Pose = new Pose(_models[i].PoseWorld.Position + translation, newPose.Orientation);

                    //_bodies[i].Pose = new Pose(_models[i].PoseWorld.Position + newPose.Position, modelOrientation);

                    _models[i].PoseWorld = _bodies[i].Pose;

                    Pose = _models[i].PoseWorld;

                    Vector3F thirdPersonDistances = modelOrientation.Rotate(new Vector3F(0, 1, 15));

                    // Compute camera pose (= position + orientation). 
                    CameraNode.PoseWorld = new Pose
                    {
                        Position = _models[i].PoseWorld.Position ,// Floor position of character
                                 
                        Orientation = modelOrientation.ToRotationMatrix33()
                    };
                }
                else if (killerState == KillerAiState.Wander)
                {
                    _identifyChasingWanderCaught = "Wander";

                    _elapsedTimeSpan[i] += timeSpan;

                    TimeSpan totalTime = _elapsedTimeSpan[i];

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

                    Vector3F thirdPersonDistances = modelOrientation.Rotate(new Vector3F(0, 1, 15));

                    // Compute camera pose (= position + orientation). 
                    CameraNode.PoseWorld = new Pose
                    {
                        Position = Pose.Position // Floor position of character
                                   + new Vector3F(0, 1.6f, 0) // + Eye height
                                   + thirdPersonDistances,
                        Orientation = Pose.Orientation
                    };
                }
                else
                {
                    _identifyChasingWanderCaught = "Caught";
                    _linearVelocityMagnitude = 0.0f;
                }

                //_models[i].PoseWorld = _bodies[i].Pose;

                //Pose = _models[i].PoseWorld;

           
                //}
            }


            //Vector3F thirdPersonDistance = modelOrientation.Rotate(new Vector3F(0, 1, 15));         
            //CameraNode.PoseWorld = new Pose
            //{
            //    Position = Pose.Position // Floor position of character
            //               + new Vector3F(0, 1.6f, 0) // + Eye height
            //               + thirdPersonDistance,
            //    Orientation = modelOrientation.ToRotationMatrix33()
            //};


            ///////////////

            var cameraGameObject =
                (ThirdPersonKillerCamera)_gameObjectService.Objects["ThirdPersonKillerCamera"];
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
            _timeUntilLaunchMissile -= TimeSpan.FromSeconds(timeSpan.TotalSeconds);
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
                        missileObjectOne.Spawn(_modelNode.PoseWorld, ray, cameraPose);
                        Sound.Sound.PlayMissileSound(true);
                    }

                    _timeUntilLaunchMissile = MissileInterval;
                }
            }

            //////////////

            UpdateProfiler();
        }

        #endregion

        private void InitializeAudio()
        {
            var cameraGameObject =
                (ThirdPersonCameraObject.ThirdPersonCameraObject)_gameObjectService.Objects["ThirdPersonCamera"];
            _cameraNodeAudio = cameraGameObject.CameraNodeMissile;
        }

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
                _fThrusters += 0.0025f;
                SoundIcrement += 0.01f;
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
            if (_fPitch > 0.10f)
            {
                _fPitch = 0.10f;
            }
            else if (_fPitch < -0.10f)
            {
                _fPitch = -0.10f;
            }

            // Slow rate of pitch
            if (_fPitch > 0.0f)
            {
                _fPitch -= 0.0005f;
            }
            else if (_fPitch < 0.0f)
            {
                _fPitch += 0.0005f;
            }

            // Update position and pitch
            _shipMovement.Y += _fPitch;
            //Original lines
            //_shipRotation.X = (fPitch * fThrusters) / 4.0f;
            //My changes
            _shipRotation.X = (_fPitch);
        }

        //private void UpdateFloat()
        //{
        // Increase the step
        //fFloatStep += 0.01f;

        //// Store new sine wave value
        //float fVariation = 10.0f * (float)Math.Sin(fFloatStep);

        //// Alter the dirigible's position
        //_shipMovement.Y -= fLastFloat;
        //_shipMovement.Y += fVariation;

        //// Store old sine wave value
        //fLastFloat = fVariation;
        //}

        private void UpdateProfiler()
        {
            _stringBuilder.Clear();
            foreach (ModelNode t in _models)
            {
                _stringBuilder.Append("Position: " + t.PoseWorld.Position);
            }
            _updateFpsTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("LinearVelocityMagnitude: " + LinearVelocityMagnitude);
            _drawFpsTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            _stringBuilder.Append("Identification: " + _identifyChasingWanderCaught);
            _rotationTextBlock.Text = _stringBuilder.ToString();

            _stringBuilder.Clear();
            if (_spring != null) _stringBuilder.Append("Missile: " + _spring.AnchorPositionBLocal);
            _fRotationPositionBlock.Text = _stringBuilder.ToString();
        }

        #endregion

     
    }
}