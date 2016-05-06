using System;
using System.Text;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Geometry;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects.VehicleCameraObject
{
    public class VehicleCameraObject : GameObject
    {
        //--------------------------------------------------------------
        #region Fields
        //--------------------------------------------------------------

        private readonly IServiceLocator _services;
        private readonly IInputService _inputService;
        private readonly VehicleObject.VehicleObject _jet;
        private bool _useSpectatorView;
        private float _yaw;
        private float _pitch;
        #endregion

 
        private readonly IGraphicsService _graphicsService;
    

        //--------------------------------------------------------------
        #region Properties & Events
        //--------------------------------------------------------------

        public CameraNode CameraNode { get; private set; }
        #endregion


        //--------------------------------------------------------------
        #region Creation & Cleanup
        //--------------------------------------------------------------

        public VehicleCameraObject(VehicleObject.VehicleObject jet, IServiceLocator services)
        {
            Name = "Camera";

            _jet = jet;
            _services = services;
            _inputService = services.GetInstance<IInputService>();


            _graphicsService = _services.GetInstance<IGraphicsService>();       
          
          
        }
        #endregion


        //--------------------------------------------------------------
        #region Methods
        //--------------------------------------------------------------

        protected override void OnLoad()
        {
            var graphicsService = _services.GetInstance<IGraphicsService>();
            // Create a camera node.
          
            // Define camera projection.
            var projection = new PerspectiveProjection();
            projection.SetFieldOfView(
              ConstantsF.PiOver4,
              graphicsService.GraphicsDevice.Viewport.AspectRatio,
              0.1f,
              10000f);

            // Create a camera node.
            CameraNode = new CameraNode(new Camera(projection))
            {
                Name = "PlayerCamera"
            };


            var scene = _services.GetInstance<IScene>();
            if (scene != null)
                scene.Children.Add(CameraNode);
        }


        protected override void OnUnload()
        {
            CameraNode.Dispose(false);
            CameraNode = null;
        }


        protected override void OnUpdate(TimeSpan deltaTime)
        {
            // Mouse centering (controlled by the MenuComponent) is disabled if the game
            // is inactive or if the GUI is active. In these cases, we do not want to move
            // the player.
            if (!_inputService.EnableMouseCentering)
                return;

            if (_inputService.IsPressed(Keys.Enter, true))
            {
                // Toggle between player camera and spectator view.
                _useSpectatorView = !_useSpectatorView;
            }
            else
            {
                float deltaTimeF = (float)deltaTime.TotalSeconds;

                // Compute new yaw and pitch from mouse movement.
                float deltaYaw = 0;
                //deltaYaw -= _inputService.MousePositionDelta.X;
                deltaYaw -= _inputService.GetGamePadState(LogicalPlayerIndex.One).ThumbSticks.Right.X * 10;
                _yaw += deltaYaw * deltaTimeF * 0.1f;

                float deltaPitch = 0;
                //deltaPitch -= _inputService.MousePositionDelta.Y;
                deltaPitch += _inputService.GetGamePadState(LogicalPlayerIndex.One).ThumbSticks.Right.Y * 10;
                _pitch += deltaPitch * deltaTimeF * 0.1f;

                // Limit the pitch angle to less than +/- 90°.
                float limit = ConstantsF.PiOver2 - 0.01f;
                _pitch = MathHelper.Clamp(_pitch, -limit, limit);
            }

            // Update SceneNode.LastPoseWorld - this is required for some effects, like
            // camera motion blur. 
            CameraNode.LastPoseWorld = CameraNode.PoseWorld;

            var vehiclePose = _jet.Jet.Chassis.Pose;
 
            if (_useSpectatorView)
            {
                // Spectator Mode:
                // Camera is looking at the car from a fixed location in the level.
                Vector3F position = new Vector3F(10, 8, 10);
                Vector3F target = vehiclePose.Position;
                Vector3F up = Vector3F.UnitY;

                // Set the new camera view matrix. (Setting the View matrix changes the Pose. 
                // The pose is simply the inverse of the view matrix). 
                CameraNode.View = Matrix44F.CreateLookAt(position, target, up);

               // CameraNode.PoseWorld = new Pose(vehiclePose.Position + target);
            }
            else
            {
               float zPosition = 1.5f;

                // Player Camera:
                // Camera moves with the car. The look direction can be changed by moving the mouse.
                Matrix33F yaw = Matrix33F.CreateRotationY(_yaw);
                Matrix33F pitch = Matrix33F.CreateRotationX(_jet._fPitch);            
                //Matrix33F pitch = Matrix33F.CreateRotationX(0.1f);
                Matrix33F orientation = vehiclePose.Orientation * yaw * pitch;
                Vector3F forward = orientation * -new Vector3F(0, 0, zPosition);
                Vector3F up = Vector3F.UnitY;
                Vector3F position = vehiclePose.Position - 20 * forward + 5 * up;
                Vector3F target = vehiclePose.Position + 0 * up;

                CameraNode.View = Matrix44F.CreateLookAt(position, target, up);
         
                //CameraNode.PoseWorld = new Pose(vehiclePose.Position + target, orientation);
            }
        }
        #endregion
        private readonly StringBuilder _stringBuilder = new StringBuilder();


    }
}
