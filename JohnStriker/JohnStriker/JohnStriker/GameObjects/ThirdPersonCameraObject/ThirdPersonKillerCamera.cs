using System;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Geometry;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using JohnStriker.GameObjects.OpponentObjects;
using JohnStriker.GameObjects.PlayerObjects;
using JohnStriker.GameObjects.VehicleObject;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework.Input;


namespace JohnStriker.GameObjects.ThirdPersonCameraObject
{
    public class ThirdPersonKillerCamera : GameObject
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

    

   

        public CameraNode CameraNodeKillers { get; private set; }

        private readonly KillerOpponents8 _killerOpponents;
        #endregion


        //--------------------------------------------------------------
        #region Creation & Cleanup
        //--------------------------------------------------------------

        public ThirdPersonKillerCamera(IServiceLocator services, KillerOpponents8 killerOpponents)
        {
            Name = "ThirdPersonKillerCamera";
         
            _killerOpponents = killerOpponents;

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
         

            CameraNodeKillers = new CameraNode(new Camera(projection))
            {
                Name = "CameraNodeKillers",

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
                scene.Children.Add(CameraNodeKillers);
            }



        }


        protected override void OnUnload()
        {
          
            CameraNodeKillers.Dispose(false);
           
            CameraNodeKillers = null;
        }


        protected override void OnUpdate(TimeSpan deltaTime)
        {
            // Mouse centering (controlled by the MenuComponent) is disabled if the game
            // is inactive or if the GUI is active. In these cases, we do not want to move
            // the player.
            if (!_inputService.EnableMouseCentering)
                return;


            CameraNodeKillers.LastPoseWorld = CameraNodeKillers.PoseWorld;

            //CameraNodeKillers.PoseWorld = _killerOpponents.CameraNode.PoseWorld;
        }
        #endregion
    }
}
