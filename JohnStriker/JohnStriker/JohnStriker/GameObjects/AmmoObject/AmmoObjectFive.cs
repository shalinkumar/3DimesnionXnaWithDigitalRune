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
  


    public class AmmoObjectFive : GameObject
    {
        private readonly IServiceLocator _services;
        private ModelNode _modelPrototype;
        private RigidBody _bodyPrototype;
        private IInputService _inputService;
        private PointLight _pointLight;
        private AnimatableProperty<float> _glowIntensity;
        private ConstParameterBinding<Vector3> _emissiveColorBinding;

        // The individual instances:
        private readonly List<ModelNode> _models = new List<ModelNode>();
        private readonly List<RigidBody> _bodies = new List<RigidBody>();

        private Simulation _simulation;

        private Pose CameraPose;

        private GeometricObject _geometricObject;

        private CollisionObject _collisionObject;

        private IGameObjectService _gameObjectService;

        // Only sounds within MaxDistance will be played.
        private const float MaxDistance = 8100;
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
                for (int i = 0; i < _models.Count; i++)
                {
                    _geometricObject.Pose = _models[i].PoseWorld;
                }
            }
        }

        private Pose StartPose { get; set; }
        private Pose TargetPose { get; set; }
        #endregion



        public AmmoObjectFive(IServiceLocator services)
        {
            _services = services;
            Name = "AmmoObjectFour";
        }


        // OnLoad() is called when the GameObject is added to the IGameObjectService.
        protected override void OnLoad()
        {
            _simulation = _services.GetInstance<Simulation>();
            _inputService = _services.GetInstance<IInputService>();
            _gameObjectService = _services.GetInstance<IGameObjectService>();
            // Use a sphere for physics simulation.
            //_bodyPrototype = new RigidBody(new SphereShape(0.5f));
            _bodyPrototype = new RigidBody(new BoxShape(5, 0, 5));

            // Load the graphics model.
            var content = _services.GetInstance<ContentManager>();
            //_modelPrototype = content.Load<ModelNode>("Ammo/BulletModelOne").Clone();

            _modelPrototype = content.Load<ModelNode>("Missile/missile").Clone();

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


        public void Spawn(Pose startPose, Pose cameraPose)
        {
            var scene = _services.GetInstance<IScene>();
            var simulation = _services.GetInstance<Simulation>();

            StartPose = startPose;
            CameraPose = cameraPose;

            // Create a new instance by cloning the prototype.
            var model = _modelPrototype.Clone();
            var body = _bodyPrototype.Clone();

            // Spawn at random position.
            body.Pose = startPose;
            _bodyPrototype.Pose = body.Pose;
            model.PoseWorld = _bodyPrototype.Pose;

            scene.Children.Add(model);
            simulation.RigidBodies.Add(body);

            _models.Add(model);
            _bodies.Add(body);
        }


        // OnUnload() is called when the GameObject is removed from the IGameObjectService.
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


        // OnUpdate() is called once per frame.
        protected override void OnUpdate(TimeSpan deltaTime)
        {

            if (!_inputService.EnableMouseCentering)
                return;



            var cameraGameObject =
            (ThirdPersonCameraObject.ThirdPersonCameraObject)_gameObjectService.Objects["ThirdPersonCamera"];
            var cameraNodeAudio = cameraGameObject.CameraNodeMissile;

            // Synchronize graphics <--> physics.
            for (int i = 0; i < _models.Count; i++)
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

                float distance = GraphicsHelper.GetViewNormalizedDistance(_models[i], cameraNodeAudio);

                if (distance >= 37.2802849)
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
                }

            }
        }

        private float GetDistance(Vector3F A, Vector3F B)
        {
            float xD = A.X - B.X;
            float yD = A.Y - B.Y;
            float zD = A.Z - B.Z;
            return (float)Math.Sqrt(Math.Pow(xD, 2) + Math.Pow(yD, 2) + Math.Pow(zD, 2));
        }
    }
}
