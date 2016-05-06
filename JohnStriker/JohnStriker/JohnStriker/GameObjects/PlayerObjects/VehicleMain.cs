
using DigitalRune.Graphics;
using DigitalRune.Graphics.Rendering;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Physics.ForceEffects;
using JohnStriker.GraphicsScreen;
using JohnStriker.Sample_Framework;
using Microsoft.Xna.Framework;

namespace JohnStriker.GameObjects.VehicleObject
{
//      [Sample(SampleCategory.PhysicsSpecialized,
//    @"This sample shows how to implement vehicle physics.",
//    @"A controllable car is created using a ray-car method where each wheel is implemented
//by a short ray that senses the ground. The car supports suspension with damping, wheel
//friction and sliding, etc.",
//    50)]
    public class VehicleMain : GameManager
    {
          private SampleGraphicsScreen _GraphicsScreen;
          private MyGraphicsScreen _myGraphicsScreen;
          private CameraObject.CameraObject _cameraObject;
        public VehicleMain(Game game)
            : base(game)
        {
            SampleFramework.IsMouseVisible = false;

            _GraphicsScreen = new SampleGraphicsScreen(Services) { ClearBackground = true, }; ;

            // The order of the graphics screens is back-to-front. Add the screen at index 0,
            // i.e. behind all other screens. The screen should be rendered first and all other
            // screens (menu, GUI, help, ...) should be on top.
            GraphicsService.Screens.Insert(0, _GraphicsScreen);

            // GameObjects that need to render stuff will retrieve the DebugRenderers or
            // Scene through the service provider.
            Services.Register(typeof(DebugRenderer), null, _GraphicsScreen.DebugRenderer);
            Services.Register(typeof(DebugRenderer), null, _GraphicsScreen.DebugRenderer);
            Services.Register(typeof(IScene), null, _GraphicsScreen.Scene);

            SetCamera(new Vector3F(0, 2, 10), 0, 0);


            Simulation.ForceEffects.Add(new Gravity());
            Simulation.ForceEffects.Add(new Damping());

            // Add a game object which loads the test obstacles.
            GameObjectService.Objects.Add(new VehicleLevelObject(Services));

            //             Add a game object which controls a vehicle.
//            var vehicleObject = new VehicleObject(Services);
//            GameObjectService.Objects.Add(vehicleObject);

            // Add a camera that is attached to chassis of the vehicle.
//            var vehicleCameraObject = new VehicleCameraObject.VehicleCameraObject(vehicleObject.Vehicle.Chassis, Services);
//            GameObjectService.Objects.Add(vehicleCameraObject);

        }


        protected void SetCamera(Vector3F position, float yaw, float pitch)
        {
            if (_cameraObject == null)
            {
                _cameraObject = new CameraObject.CameraObject(Services);
                GameObjectService.Objects.Add(_cameraObject);
                _GraphicsScreen.ActiveCameraNode = _cameraObject.CameraNode;
            }

            //               _cameraObject.ResetPose(position, yaw, pitch);
        }
      
    }
}
