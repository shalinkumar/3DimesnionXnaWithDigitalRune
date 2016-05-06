using System;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Geometry;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using JohnStriker.GameObjects.PlayerObjects;
using JohnStriker.GameObjects.VehicleObject;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework.Input;


namespace JohnStriker.GameObjects.ThirdPersonCameraObject
{
    public class ThirdPersonCameraObject : GameObject
    {
        //--------------------------------------------------------------
        #region Fields
        //--------------------------------------------------------------

        private readonly IServiceLocator _services;
        private readonly IInputService _inputService;

        // The player to which the camera is attached.
        private readonly Player _vehicleGeametric;

        // Distance of camera to player's head. Set to 0 for first-person mode.
        private float _thirdPersonDistance = 3;
        #endregion


        //--------------------------------------------------------------
        #region Properties & Events
        //--------------------------------------------------------------

        public CameraNode CameraNode { get; private set; }

        public CameraNode CameraNodeMissile { get; private set; }
        #endregion


        //--------------------------------------------------------------
        #region Creation & Cleanup
        //--------------------------------------------------------------

        public ThirdPersonCameraObject(Player vehicleGeametric, IServiceLocator services)
        {
            Name = "ThirdPersonCamera";

            _vehicleGeametric = vehicleGeametric;

            _services = services;
            _inputService = services.GetInstance<IInputService>();
        }
        #endregion


        //--------------------------------------------------------------
        #region Methods
        //--------------------------------------------------------------

        protected override void OnLoad()
        {
            var graphicsService = _services.GetInstance<IGraphicsService>();

            // Define camera projection.
            var projection = new PerspectiveProjection();
            projection.SetFieldOfView(
              ConstantsF.PiOver4,
              graphicsService.GraphicsDevice.Viewport.AspectRatio,
              0.1f,
              1000.0f);

            CameraNode = new CameraNode(new Camera(projection))
            {
                Name = "PlayerCamera",

            };

            CameraNodeMissile = new CameraNode(new Camera(projection))
                    {
                        Name = "MissileCamera",

                    };
            // Create a camera node.
            //CameraNode = new CameraNode(new Camera(projection));

            // Add to scene.
            // (This is usually optional. Since cameras do not have a visual representation,
            // it  makes no difference if the camera is actually part of the scene graph or
            // not. - Except when other scene nodes are attached to the camera. In this case
            // the camera needs to be in the scene.)
            var scene = _services.GetInstance<IScene>();
            if (scene != null)
            {
                scene.Children.Add(CameraNode);
                scene.Children.Add(CameraNodeMissile);
            }
              


        }


        protected override void OnUnload()
        {
            CameraNode.Dispose(false);
            CameraNodeMissile.Dispose(false);
            CameraNode = null;
            CameraNodeMissile = null;
        }


        protected override void OnUpdate(TimeSpan deltaTime)
        {
            // Mouse centering (controlled by the MenuComponent) is disabled if the game
            // is inactive or if the GUI is active. In these cases, we do not want to move
            // the player.
            if (!_inputService.EnableMouseCentering)
                return;

            // Mouse wheel, DPad up/down --> Change third-person camera distance.
            _thirdPersonDistance -= _inputService.MouseWheelDelta * 0.01f;
            if (_inputService.IsDown(Buttons.DPadLeft, LogicalPlayerIndex.One))
                _thirdPersonDistance -= 0.2f;
            if (_inputService.IsDown(Buttons.DPadRight, LogicalPlayerIndex.One))
                _thirdPersonDistance += 0.2f;

            _thirdPersonDistance = Math.Max(0, _thirdPersonDistance);

            // Get pose of the player. (This is the ground position, not the head position.)
            Pose pose = _vehicleGeametric._cameraNode.PoseWorld;

            // Create offset vector from player to the camera.
            Matrix33F orientation = pose.Orientation;
            Vector3F thirdPersonDistance = orientation * new Vector3F(0, 0, _thirdPersonDistance);

            // Compute camera position. 
         
            Vector3F position = pose.Position +  thirdPersonDistance;

            // Update SceneNode.LastPoseWorld - this is required for some effects, like
            // camera motion blur. 
            CameraNode.LastPoseWorld = CameraNode.PoseWorld;

            // Set the new camera pose.
            //CameraNode.PoseWorld = _vehicleGeametric._cameraNode.PoseWorld;
            CameraNode.PoseWorld = new Pose(position, orientation);


            CameraNodeMissile.LastPoseWorld = CameraNodeMissile.PoseWorld;
            //CameraNodeMissile.PoseWorld = _vehicleGeametric._cameraNodeMissile.PoseWorld;
            CameraNodeMissile.PoseWorld = new Pose(position, _vehicleGeametric._cameraNodeMissile.PoseWorld.Orientation);
        }
        #endregion
    }
}
