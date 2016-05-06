using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.Xna.Framework.Input;
using MathHelper = DigitalRune.Mathematics.MathHelper;

namespace JohnStriker.GameObjects.CameraObject
{
    public class CameraObject : GameObject
    {
        // Some constants for motion control.
        private const float LinearVelocityMagnitude = 5f;
        private const float AngularVelocityMagnitude = 0.1f;
        private const float ThumbStickFactor = 15;
        private const float SpeedBoost = 20;

        private readonly IServiceLocator _service;
        private readonly IInputService _inputService;

        private float _farDistance;

        //position and orientation.
        private Vector3F _defaultPosition=new Vector3F(0,2,5);
        private float _defaultYaw;
        private float _defaultPitch;
        private float _currentYaw;
        private float _currentPitch;

        // This property is null while the CameraObject is not added to the game
        // object service.
        public CameraNode CameraNode { get; private set; }

        public bool IsEnabled { get; set; }

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly TextBlock _updateFpsTextBlock;

        private readonly IGraphicsService graphicsService;

        private GuiCameraScreen _guiGraphicsScreen;
        public CameraObject()
        {
            
        }

        public CameraObject(IServiceLocator services)
            :this(services,1000)
        {

        }

        public CameraObject(IServiceLocator services, float farDistance)
        {
            Name = "ThirdPersonCamera";
            _service = services;
            _inputService = services.GetInstance<IInputService>();

            IsEnabled = true;
            _farDistance = farDistance;

            //graphicsService = services.GetInstance<IGraphicsService>();
            //_guiGraphicsScreen = new GuiCameraScreen(services);
            //graphicsService.Screens.Add(_guiGraphicsScreen);
            //// ----- FPS Counter (top right)
            //StackPanel fpsPanel = new StackPanel
            //{
            //    Margin = new Vector4F(10),
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    VerticalAlignment = VerticalAlignment.Top,
            //};
            //_guiGraphicsScreen.UIScreen.Children.Add(fpsPanel);
            //_updateFpsTextBlock = new TextBlock
            //{
            //    Font = "DejaVuSans",
            //    Foreground = Color.Black,
            //    HorizontalAlignment = HorizontalAlignment.Left,
            //    Text = "Position",
            //};
            //fpsPanel.Children.Add(_updateFpsTextBlock);
        }

        protected override void OnLoad()
        {
            // Create a camera node.
            CameraNode = new CameraNode(new Camera(new PerspectiveProjection()))
            {
                Name = "PlayerCamera"
            };

            // Add to scene.
            // (This is usually optional. Since cameras do not have a visual representation,
            // it  makes no difference if the camera is actually part of the scene graph or
            // not. - Except when other scene nodes are attached to the camera. In this case
            // the camera needs to be in the scene.)
            var scene = _service.GetInstance<IScene>();
            if(scene !=null)
                scene.Children.Add(CameraNode);

            ResetPose();
            ResetProjection();

            base.OnLoad();
        }

        protected override void OnUnload()
        {
            if (CameraNode.Parent != null)
                CameraNode.Parent.Children.Remove(CameraNode);

            CameraNode.Dispose(false);
            CameraNode = null;

            //graphicsService.Screens.Remove(_guiGraphicsScreen);

            base.OnUnload();
        }

        private void ResetPose(Vector3F position, float yaw, float pitch)
        {
            _defaultPosition = position;
            _defaultYaw = yaw;
            _defaultPitch = pitch;

            ResetPose();
        }


        private void ResetPose()
        {
            _currentYaw = _defaultYaw;
            _currentPitch = _defaultPitch;

            if (IsLoaded)
            {
                // Also update SceneNode.LastPose - this is required for some effect, like
                // object motion blur. 
                CameraNode.SetLastPose(true);

                CameraNode.PoseWorld = new Pose(_defaultPosition, QuaternionF.CreateRotationY(_currentYaw) * QuaternionF
                    .CreateRotationX(_currentPitch));
            }
        }

        public void ResetProjection()
        {
            if (IsLoaded)
            {
                var graphicsService = _service.GetInstance<IGraphicsService>();
                var Projection = (PerspectiveProjection) CameraNode.Camera.Projection;
                Projection.SetFieldOfView(ConstantsF.PiOver4,graphicsService.GraphicsDevice.Viewport.AspectRatio,0.1f,_farDistance);
            }
        }

        protected override void OnUpdate(TimeSpan deltaTime)
        {
            // Mouse centering (controlled by the MenuComponent) is disabled if the game
            // is inactive or if the GUI is active. In these cases, we do not want to move
            // the player.
            if (!_inputService.EnableMouseCentering)
                return;

            if (!IsEnabled)
                return;

            float deltaTimeF = (float)deltaTime.TotalSeconds;

            // Compute new orientation from mouse movement, gamepad and touch.
            Vector2F mousePositionDelta = _inputService.MousePositionDelta;
            GamePadState gamePadState = _inputService.GetGamePadState(LogicalPlayerIndex.One);
            Vector2F touchDelta = Vector2F.Zero;


            float deltaYaw = -mousePositionDelta.X - touchDelta.X - gamePadState.ThumbSticks.Right.X * ThumbStickFactor;
            _currentYaw += deltaYaw * deltaTimeF * AngularVelocityMagnitude;

            float deltaPitch = -mousePositionDelta.Y - touchDelta.Y + gamePadState.ThumbSticks.Right.Y * ThumbStickFactor;
            _currentPitch += deltaPitch * deltaTimeF * AngularVelocityMagnitude;

            // Limit the pitch angle to +/- 90°.
            _currentPitch = MathHelper.Clamp(_currentPitch, -ConstantsF.PiOver2, ConstantsF.PiOver2);

            // Reset camera position if <Home> or <Right Stick> is pressed.
            if (_inputService.IsPressed(Keys.Home, false)
                || _inputService.IsPressed(Buttons.RightStick, false, LogicalPlayerIndex.One))
            {
                ResetPose();
            }

            // Compute new orientation of the camera.
            QuaternionF orientation = QuaternionF.CreateRotationY(_currentYaw) * QuaternionF.CreateRotationX(_currentPitch);

            // Create velocity from <W>, <A>, <S>, <D> and <R>, <F> keys. 
            // <R> or DPad up is used to move up ("rise"). 
            // <F> or DPad down is used to move down ("fall").
            Vector3F velocity = Vector3F.Zero;
            KeyboardState keyboardState = _inputService.KeyboardState;
            if (keyboardState.IsKeyDown(Keys.NumPad8))
                velocity.Z--;
            if (keyboardState.IsKeyDown(Keys.NumPad2))
                velocity.Z++;
            if (keyboardState.IsKeyDown(Keys.NumPad4))
                velocity.X--;
            if (keyboardState.IsKeyDown(Keys.NumPad6))
                velocity.X++;
            if (keyboardState.IsKeyDown(Keys.R) || gamePadState.DPad.Up == ButtonState.Pressed)
                velocity.Y++;
            if (keyboardState.IsKeyDown(Keys.F) || gamePadState.DPad.Down == ButtonState.Pressed)
                velocity.Y--;

            // Add velocity from gamepad sticks.
            velocity.X += gamePadState.ThumbSticks.Left.X;
            velocity.Z -= gamePadState.ThumbSticks.Left.Y;

            // Rotate the velocity vector from view space to world space.
            velocity = orientation.Rotate(velocity);

            if (keyboardState.IsKeyDown(Keys.LeftShift))
                velocity *= SpeedBoost;

            // Multiply the velocity by time to get the translation for this frame.
            Vector3F translation = velocity * LinearVelocityMagnitude * deltaTimeF;

            // Update SceneNode.LastPoseWorld - this is required for some effects, like
            // camera motion blur. 
            CameraNode.LastPoseWorld = CameraNode.PoseWorld;

            // Set the new camera pose.
            CameraNode.PoseWorld = new Pose(
              CameraNode.PoseWorld.Position + translation,
              orientation);

            //UpdateProfiler();

            base.OnUpdate(deltaTime);
        }


        private void UpdateProfiler()
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("Position: " + CameraNode.PoseWorld.Position);
            _updateFpsTextBlock.Text = _stringBuilder.ToString();
        }
    }
}
