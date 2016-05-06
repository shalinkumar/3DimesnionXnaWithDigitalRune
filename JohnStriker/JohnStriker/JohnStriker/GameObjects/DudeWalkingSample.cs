using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DigitalRune.Animation;
using DigitalRune.Animation.Character;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Geometry;
using DigitalRune.Geometry.Collisions;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Particles;
using DigitalRune.Physics;
using JohnStriker.GraphicsScreen;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects
{
    public class DudeWalkingSample : GameObject
    {
        private AnimationController _animationController;

        private readonly MyGraphicsScreen _graphicsScreen;

        private readonly IInputService _inputService;

        protected readonly IAnimationService AnimationService;

        public readonly CameraNode _cameraNode;

        private readonly IGraphicsService graphicsService;

        private MeshNode meshNode;

        // Movement thrusters
        private float _fThrusters = 0.0f;

        private float _fRotation;

        private Vector3F _shipMovement;

        private Vector3F _shipRotation;

        private const float LinearVelocityMagnitude = 5f;

        private readonly Simulation _simulation;

        private RigidBody _rigidBody;

        private GeometricObject _geometricObject;

        private CollisionObject _collisionObject;

        // Orientation of camera.
        private float _yaw;
        private float _pitch;

        private const float AngularVelocityMagnitude = 0.1f;

        public GeometricObject GeometricObject { get; private set; }


        // The collision object used for collision detection. 
        public CollisionObject CollisionObject
        {
            get { return _collisionObject; }
        }
        public DudeWalkingSample(IServiceLocator services)         
        {
            var contentManager = services.GetInstance<ContentManager>();
            _inputService = services.GetInstance<IInputService>();
            AnimationService = services.GetInstance<IAnimationService>();
            graphicsService = services.GetInstance<IGraphicsService>();
            _graphicsScreen = new MyGraphicsScreen(services) { DrawReticle = true };
            _simulation = services.GetInstance<Simulation>();

            var shape = contentManager.Load<Shape>("Dude/Dude_Collision");

            // A simple cube.
            _rigidBody = new RigidBody(new BoxShape(2,2,2));

            // Create a camera.
            var projection = new PerspectiveProjection();
            projection.SetFieldOfView(
              ConstantsF.PiOver4,
              graphicsService.GraphicsDevice.Viewport.AspectRatio,
              0.1f,
              1000.0f);
            _cameraNode = new CameraNode(new Camera(projection));

            _graphicsScreen.ActiveCameraNode = _cameraNode;

            var modelNode = contentManager.Load<ModelNode>("Dude/Dude");

            meshNode = modelNode.GetSubtree().OfType<MeshNode>().First().Clone();

            _rigidBody.Pose = new Pose(new Vector3F(0, 0, 10));
            meshNode.PoseLocal = _rigidBody.Pose;

            // Add rigid body to physics simulation and model to scene.
          
            _simulation.RigidBodies.Add(_rigidBody);

            var scene = services.GetInstance<IScene>();
            scene.Children.Add(meshNode);

            // Load collision shape from a separate model (created using the CollisionShapeProcessor).
            //var shape = contentManager.Load<Shape>("Dude/Dude_Collision");

            _geometricObject = new GeometricObject(shape, meshNode.PoseLocal);
            // Create a collision object for the game object and add it to the collision domain.
            _collisionObject = new CollisionObject(_geometricObject);

            // Important: We do not need detailed contact information when a collision
            // is detected. The information of whether we have contact or not is sufficient.
            // Therefore, we can set the type to "Trigger". This increases the performance 
            // dramatically.
            _collisionObject.Type = CollisionObjectType.Trigger;

            // Add the collision object to the collision domain of the game.
            var collisionDomain = services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject);

            SampleHelper.EnablePerPixelLighting(meshNode);



        

            // The imported animations are stored in the mesh.
            Dictionary<string, SkeletonKeyFrameAnimation> animations = meshNode.Mesh.Animations;

            // The Dude model contains only one animation, which is a SkeletonKeyFrameAnimation with 
            // a walk cycle.
            SkeletonKeyFrameAnimation walkAnimation = animations.Values.First();

            // Wrap the walk animation in an animation clip that loops the animation forever.
            AnimationClip<SkeletonPose> loopingAnimation = new AnimationClip<SkeletonPose>(walkAnimation)
            {
                LoopBehavior = LoopBehavior.Cycle,
                Duration = TimeSpan.MaxValue,
            };

            // Start the animation and keep the created AnimationController.
            // We must cast the SkeletonPose to IAnimatableProperty because SkeletonPose implements
            // IAnimatableObject and IAnimatableProperty. We must tell the AnimationService if we want
            // to animate an animatable property of the SkeletonPose (IAnimatableObject), or if we want to
            // animate the whole SkeletonPose (IAnimatableProperty).
            _animationController = AnimationService.StartAnimation(loopingAnimation, (IAnimatableProperty)meshNode.SkeletonPose);

            // The animation will be applied the next time AnimationManager.ApplyAnimations() is called
            // in the main loop. ApplyAnimations() is called before this method is called, therefore
            // the model will be rendered in the bind pose in this frame and in the first animation key
            // frame in the next frame - this creates an annoying visual popping effect. 
            // We can avoid this if we call AnimationController.UpdateAndApply(). This will immediately
            // change the model pose to the first key frame pose.
            _animationController.UpdateAndApply();

            // (Optional) Enable Auto-Recycling: 
            // After the animation is stopped, the animation service will recycle all
            // intermediate data structures. 
            _animationController.AutoRecycle();
        }



        protected override void OnUnload()
        {
              // Clean up.
                _animationController.Stop();
           
        }
      

        protected override void OnUpdate(TimeSpan timeSpan)
        {

            var deltaTime = (float)timeSpan.TotalSeconds;
         
            Vector3F moveDirection = Vector3F.Zero;
            if (_inputService.IsDown(Keys.A))
            {
                _animationController.Speed = 2f;
                moveDirection.Z--;
            }
            else
            {
                moveDirection.Z = 0;
                _animationController.Speed = 0;
            }
         
            // ----- Update orientation 
            // Update _yaw and _pitch.
            UpdateOrientation(deltaTime);

            // Compute the new orientation of the camera.
            QuaternionF cameraOrientation = QuaternionF.CreateRotationY(_yaw) * QuaternionF.CreateRotationX(_pitch);


            _shipMovement = cameraOrientation.Rotate(moveDirection);

            Vector3F translation = _shipMovement * LinearVelocityMagnitude * deltaTime;


            _rigidBody.Pose = new Pose(_rigidBody.Pose.Position + translation, cameraOrientation);
            meshNode.PoseLocal = _rigidBody.Pose;

            
            Vector3F thirdPersonDistance = cameraOrientation.Rotate(new Vector3F(0, 0, 5));

            // Compute camera pose (= position + orientation). 
            _cameraNode.PoseWorld = new Pose
            {
                Position = _rigidBody.Pose.Position         // Floor position of character
                           + new Vector3F(0, 1.6f, 0)  // + Eye height
                           + thirdPersonDistance,
                Orientation = cameraOrientation.ToRotationMatrix33()
            };

            base.Update(timeSpan);
        }

        private void UpdateOrientation(float deltaTime)
        {
    
            // Compute new yaw and pitch from mouse movement and gamepad.
            float deltaYaw = -_inputService.MousePositionDelta.X;
           
            _yaw += deltaYaw * deltaTime * AngularVelocityMagnitude;
        
        }

     
    }
}
