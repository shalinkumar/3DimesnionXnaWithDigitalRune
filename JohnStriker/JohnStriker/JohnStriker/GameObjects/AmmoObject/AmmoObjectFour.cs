using System;
using System.Collections.Generic;
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
using DigitalRune.Geometry.Collisions;
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

namespace JohnStriker.GameObjects.AmmoObject
{

    public class AmmoObjectFour : GameObject
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

        private readonly ModelNode[] _wheelModelNodes;
        private readonly ModelNode _missileModelNodes;

        // Missile values.
        private float _steeringAngle;
        private float _motorForce;
        private float _direction = 0;
        public float _fPitch;
        private float _fRotation;
        private Matrix33F pitch;
        private string _fRotationPosition;

        private StackPanel _fpsPanel;
        private TextBlock _updateFpsTextBlock;
        private TextBlock _drawFpsTextBlock;
        private TextBlock _RotationTextBlock;
        private TextBlock _OrientationTextBlock;
        private TextBlock _fRotationPositionBlock;
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
        private readonly float _vehicleRotation;
        private bool isVehicleRotation;


        private bool FirstTime = true;
        private float _destinationsZ;
        private float _destinationsZPosition;
        private float _missileForce;
        private float _speedMissile = 0f;
        private float _speedInitialize = 5f;
        private Pose jetPose;
        private GuiMissileScreen _guiGraphicsScreen;
        private Vehicle vehicleObject;
        private Pose missilePose { get; set; }
        private ModelNode _modelPrototype;
        private RigidBody _bodyPrototype;
        // The individual instances:
        private readonly List<ModelNode> _models = new List<ModelNode>();
        private readonly List<RigidBody> _bodies = new List<RigidBody>();

        private GeometricObject _geometricObject;

        private CollisionObject _collisionObject;

        private Pose CameraPose;

        // Only sounds within MaxDistance will be played.
        private const float MaxDistance = 90;

        private TimeSpan _timeUntilAmmo = TimeSpan.Zero;

        private static readonly TimeSpan AmmoInterval = TimeSpan.FromSeconds(3);
        #endregion


        //--------------------------------------------------------------
        #region Properties & Events
        //--------------------------------------------------------------
        public CollisionObject CollisionObject
        {
            get { return _collisionObject; }
        }

        public Pose Pose
        {
            //get { return _modelPrototype.PoseWorld; }
            set
            {
                //foreach (ModelNode t in _models)
                //{
                //    t.PoseWorld = value;
                //    _geometricObject.Pose = value;
                //}     
                foreach (ModelNode t in _models)
                {
                    _geometricObject.Pose = t.PoseWorld;
                }
            }
        }

        private Pose StartPose { get; set; }
     
        #endregion


        //--------------------------------------------------------------
        #region Creation & Cleanup & RigidBody
        //--------------------------------------------------------------


        public AmmoObjectFour(IServiceLocator services)
        {
            _services = services;

            Name = "AmmoObjectFour";

            _gameObjectService = services.GetInstance<IGameObjectService>();

            _inputService = _services.GetInstance<IInputService>();

            _simulation = _services.GetInstance<Simulation>();
                
            // Load models for rendering.
            var contentManager = _services.GetInstance<ContentManager>();
       
            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));

            _modelPrototype = contentManager.Load<ModelNode>("Ammo/BulletModelOne").Clone();
            //_modelPrototype.ScaleLocal = new Vector3F(0.06f);

            // The collision shape is stored in the UserData.
            var shape = (Shape)_modelPrototype.UserData;
        
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
        }

        protected override void OnLoad()
        {
            _timeUntilAmmo = AmmoInterval;
        }




        #endregion



        #region Update Methods

        public void Spawn(Pose startPose, Pose cameraPose)
        {
            var scene = _services.GetInstance<IScene>();

            var simulation = _services.GetInstance<Simulation>();

            _collisionObject.Enabled = true;
      
            StartPose = startPose;

            CameraPose = cameraPose;

            var model = _modelPrototype.Clone();

            var body = _bodyPrototype.Clone();           

            body.Pose = startPose;

            _bodyPrototype.Pose = body.Pose;
        
            model.PoseWorld = _bodyPrototype.Pose;

            scene.Children.Add(model);
            simulation.RigidBodies.Add(body);

            _models.Add(model);
            _bodies.Add(body);
        }


        protected override void OnUpdate(TimeSpan deltaTime)
        {
            if (!_inputService.EnableMouseCentering)
                return;

            var deltaTimeF = (float)deltaTime.TotalSeconds;

            var cameraGameObject =
         (ThirdPersonCameraObject.ThirdPersonCameraObject)_gameObjectService.Objects["ThirdPersonCamera"];
            var cameraNodeAudio = cameraGameObject.CameraNodeMissile;

            for (int i = 0; i < _models.Count; i++)
            {
                if (_models[i].Parent != null)
                {
                   
                    if (_bodies[i].Simulation != null)
                    {
                        _simulation.RigidBodies.Remove(_bodies[i]);
                    }
                  
                    _models[i].SetLastPose(true);
                
                    Vector3F forwardCameraPose = CameraPose.ToWorldDirection(Vector3F.Forward);              
                  
                    _bodies[i].LinearVelocity = forwardCameraPose * 100;

                    _models[i].PoseWorld = _bodies[i].Pose;

                    _simulation.RigidBodies.Add(_bodies[i]);


                    Pose = _models[i].PoseWorld;

                    _timeUntilAmmo -= deltaTime;

                    if (_timeUntilAmmo <= TimeSpan.Zero)
                    {

                        var body = _bodies[i];

                        if (body.Simulation != null)
                        {
                            _simulation.RigidBodies.Remove(body);
                        }

                        if (_models[i].Parent != null)
                        {
                            _models[i].Parent.Children.Remove(_models[i]);
                            _models[i].Dispose(false);
                        }

                        _models.RemoveAt(i);
                        _bodies.RemoveAt(i);

                        _timeUntilAmmo = AmmoInterval;
                    }                 
                }
            }

           
        }

        protected override void OnUnload()
        {
            // Remove models from scene.           
            _models.Clear();
            _bodies.Clear();

            // Remove prototype.
            _modelPrototype.Dispose(false);
            _modelPrototype = null;
            _bodyPrototype = null;
        }
        #endregion


        public void Dispose()
        {
            for (int i = 0; i < _models.Count; i++)
            {

                var model = _models[i];
                var body = _bodies[i];

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

    }
}
