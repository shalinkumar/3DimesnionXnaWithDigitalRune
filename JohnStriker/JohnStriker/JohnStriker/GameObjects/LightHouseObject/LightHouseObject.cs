using System;
using System.Collections.Generic;
using DigitalRune.Animation;
using DigitalRune.Animation.Easing;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Geometry;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.Effects;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Mathematics.Statistics;
using DigitalRune.Physics;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace JohnStriker.GameObjects.LightHouseObject
{
    public class LightHouseObject : GameObject
    {
        private readonly IServiceLocator _services;
        private ModelNode _modelPrototype;
        private RigidBody _bodyPrototype;
        private PointLight _pointLight;
        private AnimatableProperty<float> _glowIntensity;
        private ConstParameterBinding<Vector3> _emissiveColorBinding;

        // The individual instances:
        private readonly List<ModelNode> _models = new List<ModelNode>();
        private readonly List<RigidBody> _bodies = new List<RigidBody>();

        private Vector3F randomPosition;
        private   Random randomGen = new Random(12345);

        public LightHouseObject(IServiceLocator services)
        {
            _services = services;
            Name = "LightHouse";
        }


        // OnLoad() is called when the GameObject is added to the IGameObjectService.
        protected override void OnLoad()
        {
            // ----- Create prototype of a lava ball:

            // Use a sphere for physics simulation.
            _bodyPrototype = new RigidBody(new SphereShape(0.0f));
            // Load the graphics model.
            var content = _services.GetInstance<ContentManager>();
            _modelPrototype = content.Load<ModelNode>("LightTower/LightTower").Clone();
            _modelPrototype.ScaleLocal = new Vector3F(3f);

            var scene = _services.GetInstance<IScene>();
            var simulation = _services.GetInstance<Simulation>();

            // Create a new instance by cloning the prototype.

            Matrix33F orientation = Matrix33F.CreateRotationY(randomGen.NextFloat(0, ConstantsF.TwoPi));
            for (int i = 0; i < 1; i++)
            {
                var model = _modelPrototype.Clone();
                var body = _bodyPrototype.Clone();
                var count = i * 200;

                randomPosition = new Vector3F(-18, 0,0);

                // Spawn at random position.



                body.Pose = new Pose(randomPosition, orientation);
                model.PoseWorld = body.Pose;

                SampleHelper.EnablePerPixelLighting(model);


                scene.Children.Add(model);
                //simulation.RigidBodies.Add(body);

                _models.Add(model);
                _bodies.Add(body);

                //RigidBody rigidBody = AddBody(simulation, "LightTower/LightTower", body.Pose, new PlaneShape(Vector3F.UnitY, 0), MotionType.Static);
            }

        }

      
     

        private static RigidBody AddBody(Simulation simulation, string name, Pose pose, Shape shape, MotionType motionType)
        {
            var rigidBody = new RigidBody(shape)
            {
                Name = name,
                Pose = pose,
                MotionType = motionType,
            };

            simulation.RigidBodies.Add(rigidBody);
            return rigidBody;
        }

        // OnUnload() is called when the GameObject is removed from the IGameObjectService.
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
                //body.Simulation.RigidBodies.Remove(body);

            _models.Clear();
            _bodies.Clear();

            // Remove prototype.
            _modelPrototype.Dispose(false);
            _modelPrototype = null;
            _bodyPrototype = null;

            // Stop animation.
            var animationService = _services.GetInstance<IAnimationService>();
            animationService.StopAnimation(_glowIntensity);
            _glowIntensity = null;
        }


        // OnUpdate() is called once per frame.
        protected override void OnUpdate(TimeSpan deltaTime)
        {
            // Synchronize graphics <--> physics.
            //NOT REQUIRED UPDATE METHOD
            //for (int i = 0; i < _models.Count; i++)
            //{
            //    var model = _models[i];
            //    var body = _bodies[i];

            //    // Update SceneNode.LastPoseWorld - this is required for some effects, 
            //    // like object motion blur. 
            //    model.SetLastPose(true);

            //    model.PoseWorld = body.Pose;
            //}
        }
    }
}
