using System;
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
using DigitalRune.Particles;
using DigitalRune.Physics;
using DigitalRune.Physics.Constraints;
using JohnStriker.GraphicsScreen;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using MathHelper = DigitalRune.Mathematics.MathHelper;


namespace JohnStriker.GameObjects.OpponentObjects.Killers
{
    public class KillerOpponents : GameObject
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
        private const float MaxDistance = 20;

        // Contact forces below MinHitForce do not make a sound.
        private const float MinHitForce = 20000;
        private readonly IParticleSystemService ParticleSystemService;
        private readonly TextBlock _OrientationTextBlock;
        private readonly TextBlock _PitchTextBlock;
        private readonly TextBlock _RotationTextBlock;

        public readonly CameraNode _cameraNodeMissile;
        private readonly CollisionObject _collisionObject;
        private readonly TextBlock _drawFpsTextBlock;
        private readonly TextBlock _fRotationPositionBlock;
        private readonly GeometricObject _geometricObject;

        private readonly MyGraphicsScreen _graphicsScreen;
        private readonly ParticleSystem _jetFlame;
        public readonly ModelNode _modelNode;
        private readonly ParticleSystemNode _particleSystemNode;
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly TextBlock _updateFpsTextBlock;
        private readonly IGraphicsService graphicsService;
        private readonly Vector3F rollCenter = Vector3F.Zero;
        public Pose FlamePose;
        public CameraNode _cameraNode;
        private Vector3F _desiredPosition;
        private bool _drawDebugInfo; // true if the collision shapes should be drawn for debugging.
        private float _fRoll;
        private float _fRotation;

        // Movement thrusters
        private float _fThrusters;
        private Vector3F _gravityVelocity;

        private SoundEffectInstance _jetmovingSoundInstances;
        private SoundEffect _jetMovingSound;
        private AudioEmitter _jetmovingEmitter;

        private SoundEffect _jetMovingSpeedSound;
        private SoundEffectInstance _jetmovingSpeedSoundInstances;
        private AudioEmitter _jetmovingSpeedEmitter;


        // Current velocity from jumping.
        private Vector3F _jumpVelocity;
        private AudioListener _listener;

        // The last valid position (set at the beginning of Move()).
        private Vector3F _oldPosition;
        private float _pitch;

        private Vector3F _shipMovement;

        private Vector3F _shipRotation;
        private float _soundIcrement;
        private float _timeSinceLastHitSound;
        private float _timeSinceLastjetSound;
        private float _yaw;

        // Sine wave information (floating behavior)
        private float fFloatStep = 0.0f;

        private float fLastFloat = 0.0f;
        private float fPitch;

        //jet moving sound with array

        private readonly IGameObjectService _gameObjectService;

        private BallJoint _spring;

        private float _springAttachmentDistanceFromObserver;

        private readonly Simulation _simulation;

        private GuiKiller _guiKiller;

        private StackPanel _fpsPanel;

        private RigidBody _bodyPrototype;

        private readonly MissileObject.MissileObject MissileObject;

        protected readonly IGameObjectService GameObjectService;

        private TimeSpan _timeUntilLaunchMissile = TimeSpan.Zero;

        private static readonly TimeSpan MissileInterval = TimeSpan.FromSeconds(1);
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
            set
            {
              
                _geometricObject.Pose = _modelNode.PoseWorld;
            }
        }

        #endregion

        #region Creation & Cleanup

        //--------------------------------------------------------------

        public KillerOpponents(IServiceLocator services)
        {
            Name = "KillerOpponents1";
            // Create a game object for the character controller.
            //      GeometricObject = new GeometricObject(
            //        new CapsuleShape(Width / 2, Height),
            //        new Pose(new Vector3F(0, Height / 2, 0)));
            var contentManager = services.GetInstance<ContentManager>();
            //InitializeAudio(contentManager);
            _inputService = services.GetInstance<IInputService>();
            _graphicsScreen = new MyGraphicsScreen(services) { DrawReticle = true };
            _gameObjectService = services.GetInstance<IGameObjectService>();
            _simulation = services.GetInstance<Simulation>();
            graphicsService = services.GetInstance<IGraphicsService>();
            GameObjectService = services.GetInstance<IGameObjectService>();
            // Add the GuiGraphicsScreen to the graphics service.
            _guiKiller = new GuiKiller(services);
            graphicsService.Screens.Add(_guiKiller);

            // ----- FPS Counter (top right)
            _fpsPanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            _guiKiller.UIScreen.Children.Add(_fpsPanel);
            _updateFpsTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Foreground = Color.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = "Position",
            };
            _fpsPanel.Children.Add(_updateFpsTextBlock);

         

            // Create a camera.
            var projection = new PerspectiveProjection();
            projection.SetFieldOfView(
                ConstantsF.PiOver4,
                graphicsService.GraphicsDevice.Viewport.AspectRatio,
                0.1f,
                1000.0f);
            _cameraNode = new CameraNode(new Camera(projection));
            _graphicsScreen.ActiveCameraNode = _cameraNode;

            _bodyPrototype = new RigidBody(new BoxShape(5, 0, 5));

            // ----- Graphics
            // Load a graphics model and add it to the scene for rendering.
            _modelNode = contentManager.Load<ModelNode>("M16D/skyfighter fbx").Clone();

            CreateRigidBody();

            _bodyPrototype.Pose = new Pose(new Vector3F(0, 3, 12), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

            _modelNode.PoseWorld = _bodyPrototype.Pose;


            //_modelNode.PoseWorld = new Pose(new Vector3F(0, 3, 3), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

            var scene = services.GetInstance<IScene>();
            scene.Children.Add(_modelNode);                       

            // Load collision shape from a separate model (created using the CollisionShapeProcessor).
            var shape = (Shape)_modelNode.UserData;
            //var shape = contentManager.Load<Shape>("Ship/Ship_CollisionModel");

            _geometricObject = new GeometricObject(shape, _modelNode.PoseWorld);
            // Create a collision object for the game object and add it to the collision domain.
            _collisionObject = new CollisionObject(_geometricObject);


            var collisionDomain = services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(CollisionObject);

            _collisionObject.Enabled = true;

            _shipMovement = Vector3F.Zero;
            _shipRotation = Vector3F.Zero;

            MissileObject = new MissileObject.MissileObject(services, "KillerMissile");
            GameObjectService.Objects.Add(MissileObject);    
        }

        private void CreateRigidBody()
        {
            var triangleMesh = new TriangleMesh();

            foreach (var meshNode in _modelNode.GetSubtree().OfType<MeshNode>())
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
         
            var deltaTime = (float)timeSpan.TotalSeconds;
          

            // Move second ship with arrow keys.         
            KeyboardState keyboardState = _inputService.KeyboardState;
            if (keyboardState.IsKeyDown(Keys.NumPad8))
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
            if (keyboardState.IsKeyDown(Keys.NumPad5))
            {
                _fThrusters += 0.05f;
            }

            if (_inputService.IsDown(Keys.NumPad4))
            {
                _fRotation += 0.01f;
                _fRoll += 0.04f;
            }
            if (_inputService.IsDown(Keys.NumPad6))
            {
                _fRotation -= 0.01f;
                _fRoll -= 0.04f;
            }
            if (keyboardState.IsKeyDown(Keys.NumPad7))
                fPitch += 0.005f;
            if (keyboardState.IsKeyDown(Keys.NumPad9))
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

            //UpdateProfiler();

            _bodyPrototype.Pose = new Pose(_modelNode.PoseWorld.Position + translation, cameraOrientation);

            _modelNode.PoseWorld = _bodyPrototype.Pose;

            Pose = _modelNode.PoseWorld;             

            // Set the new camera pose.
            //_modelNode.PoseWorld = new Pose(_modelNode.PoseWorld.Position + translation, cameraOrientation);
            //Pose = _modelNode.PoseWorld;
           

         
            Vector3F thirdPersonDistance = cameraOrientation.Rotate(new Vector3F(0, 1, 15));

            // Compute camera pose (= position + orientation). 
            _cameraNode.PoseWorld = new Pose
            {
                Position = _modelNode.PoseWorld.Position // Floor position of character
                           + new Vector3F(0, 1.6f, 0) // + Eye height
                           + thirdPersonDistance,
                Orientation = cameraOrientation.ToRotationMatrix33()
            };


        
         


            _timeSinceLastHitSound += deltaTime;



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

            if (Math.Abs(_fThrusters) > 0.0f)
            {
                Sound.Sound.UpdateGearSound(Math.Abs(_fThrusters), Math.Abs(_fThrusters), timeSpan);
            }

        }
       

        #endregion



        #region Private Methods

        //--------------------------------------------------------------

        // Move the character to the new desired position, sliding along obstacles and stepping 
        // automatically up and down. Gravity is applied.


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
            if (_spring != null) _stringBuilder.Append("Position: " + _spring.AnchorPositionBLocal);
            _updateFpsTextBlock.Text = _stringBuilder.ToString();
        }

        #endregion

        //--------------------------------------------------------------

        //--------------------------------------------------------------

        //--------------------------------------------------------------

        //--------------------------------------------------------------

        //--------------------------------------------------------------



     
    }
}
