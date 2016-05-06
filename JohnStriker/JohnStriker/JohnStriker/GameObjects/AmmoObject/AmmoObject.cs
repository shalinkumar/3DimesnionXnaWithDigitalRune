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

    public class AmmoObject : GameObject
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
                foreach (ModelNode t in _models)
                {
                    t.PoseWorld = value;
                    _geometricObject.Pose = value;
                }
            }
        }

        private Pose StartPose { get; set; }
        private Pose TargetPose { get; set; }
        #endregion


        //--------------------------------------------------------------
        #region Creation & Cleanup & RigidBody
        //--------------------------------------------------------------


        public AmmoObject(IServiceLocator services)
        {
            _services = services;

            Name = "AmmoObject";
            _gameObjectService = services.GetInstance<IGameObjectService>();
            _inputService = _services.GetInstance<IInputService>();
            _simulation = _services.GetInstance<Simulation>();           



            // A simple cube.

            // Load models for rendering.
            var contentManager = _services.GetInstance<ContentManager>();

            //_bodyPrototype = new RigidBody(modelPShape);

            _bodyPrototype = new RigidBody(new BoxShape(5, 0, 5));



            _modelPrototype = contentManager.Load<ModelNode>("Ammo/Bullet1").Clone();
            //_modelPrototype.ScaleLocal = new Vector3F(0.1f);

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

        }

        protected override void OnLoad()
        {

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

            //body.Pose = startPose;
            //model.PoseWorld = _bodyPrototype.Pose;

            body.Pose = startPose;
            _bodyPrototype.Pose = body.Pose;
            //body.Pose = new Pose(new Vector3F(startPose.Position.X, startPose.Position.Y, cameraPose.Position.Z + (-3)));
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

           

            for (int i = 0; i < _models.Count; i++)
            {

                var model = _models[i];
                var body = _bodies[i];

                if (body.Simulation != null)
                {
                    _simulation.RigidBodies.Remove(body);
                }
              
                model.SetLastPose(true);                              
                Vector3F forwardCameraPose = CameraPose.ToWorldDirection(Vector3F.Forward);           
                //body.Pose = CameraPose;
                body.LinearVelocity = forwardCameraPose * 100;
                model.PoseWorld = body.Pose;

                _simulation.RigidBodies.Add(body);


                Pose = model.PoseWorld;
            }

            //UpdateProfiler();
        }

        protected override void OnUnload()
        {

            // Remove models from scene.
            foreach (var model in _models)
            {
                model.Parent.Children.Remove(_modelPrototype);
                model.Dispose(false);
            }

            // Remove rigid bodies from physics simulation.
            foreach (var body in _bodies)
                _simulation.RigidBodies.Remove(body);

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


        private void UpdateProfiler()
        {
            _stringBuilder.Clear();
            foreach (ModelNode t in _models)
            {
                _stringBuilder.Append("Position: " + t.PoseWorld.Position);
            }
            _updateFpsTextBlock.Text = _stringBuilder.ToString();

        }


    }
}
