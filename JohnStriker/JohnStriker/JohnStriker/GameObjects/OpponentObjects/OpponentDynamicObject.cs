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
using DigitalRune.Mathematics.Interpolation;
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

namespace JohnStriker.GameObjects.OpponentObjects
{
    public class OpponentDynamicObject : GameObject
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

        private float timer = 1;         //Initialize a 10 second timer

        private const float Timer = 1;

        private float timerModel = 45;         //Initialize a 10 second timer

        private const float TimerModel = 45;

        // Movement thrusters
        private float _fThrusters = 0.0f;

        private const float M16DSpeed = 10.0f;

        private Vector3F _shipMovement;

        private Vector3F _shipRotation;

        // Pitch information
        private float fPitch = 0.0f;

        private float _fRoll;

        //key frame animation     
        private List<KeyframedObjectAnimations> _animation = new List<KeyframedObjectAnimations>();

        private const int FrameStringAndEnemyCount = 10;

        private const float LinearVelocityMagnitude = 5f;

        private int _enemyCount = 0;

        public TimeSpan TimePassed;

        private bool Loop = true;

        private TimeSpan ElapsedTime = TimeSpan.FromSeconds(0);

        private List<TimeSpan> ElapsedTimeSpan =new List<TimeSpan>();     

        private List<List<TimeSpan>> nestedListTimeSpan = new List<List<TimeSpan>>();

        private Vector3F Rotation { get; set; }
        private Vector3F Position { get; set; }




        private List<ObjectAnimationFrames> FrameStrings2 = new List<ObjectAnimationFrames>();

        private List<ObjectAnimationFrames> FrameStrings3 = new List<ObjectAnimationFrames>();

        private List<List<ObjectAnimationFrames>> _nestedListanimation = new List<List<ObjectAnimationFrames>>();

        private int _randomcounts;
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
                    value = t.PoseWorld;
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


        public OpponentDynamicObject(IServiceLocator services,int objectCount)
        {
            _services = services;

            Name = "OpponentDynamicObject" + objectCount;
            _gameObjectService = services.GetInstance<IGameObjectService>();
            _inputService = _services.GetInstance<IInputService>();
            _simulation = _services.GetInstance<Simulation>();

            //_graphicsService = _services.GetInstance<IGraphicsService>();
            //// Add the GuiGraphicsScreen to the graphics service.
            //_guiGraphicsScreen = new GuiMissileScreen(_services);
            //_graphicsService.Screens.Add(_guiGraphicsScreen);

            //// ----- FPS Counter (top right)
            //_fpsPanel = new StackPanel
            //{
            //    Margin = new Vector4F(10),
            //    HorizontalAlignment = HorizontalAlignment.Right,
            //    VerticalAlignment = VerticalAlignment.Bottom,
            //};
            //_guiGraphicsScreen.UIScreen.Children.Add(_fpsPanel);
            //_updateFpsTextBlock = new TextBlock
            //{
            //    Font = "DejaVuSans",
            //    Foreground = Color.Black,
            //    HorizontalAlignment = HorizontalAlignment.Right,
            //    Text = "Position",
            //};
            //_fpsPanel.Children.Add(_updateFpsTextBlock);



            // A simple cube.

            // Load models for rendering.
            var contentManager = _services.GetInstance<ContentManager>();

            //_bodyPrototype = new RigidBody(modelPShape);

            _bodyPrototype = new RigidBody(new BoxShape(5, 0, 5));



            _modelPrototype = contentManager.Load<ModelNode>("M16D/skyfighter fbx").Clone();
            //_modelPrototype.ScaleLocal = new Vector3F(0.06f);

            _bodyPrototype.Pose = new Pose(new Vector3F(0, 2, -50), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

            _modelPrototype.PoseWorld = _bodyPrototype.Pose;

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

            List<ObjectAnimationFrames> FrameStrings;
            FrameStrings = new List<ObjectAnimationFrames>();
            FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "", TimeSpan.FromSeconds(RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")), 15))));
            FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "rotate left", "", TimeSpan.FromSeconds(RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 5, 20))));
            FrameStrings.Add(new ObjectAnimationFrames("", "rotate left", "", TimeSpan.FromSeconds(RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 10, 25))));
            FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "", TimeSpan.FromSeconds(RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 15, 30))));
            FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "rotate left", "", TimeSpan.FromSeconds(RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 20, 35))));
            FrameStrings.Add(new ObjectAnimationFrames("", "rotate left", "", TimeSpan.FromSeconds(RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 25, 40))));
            FrameStrings.Add(new ObjectAnimationFrames("thrusters forward", "", "", TimeSpan.FromSeconds(RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 30, 45))));
            _nestedListanimation.Add(FrameStrings);
        }

        protected override void OnLoad()
        {
            //_randomcounts = RandomHelper.Random.NextInteger(FrameStringAndEnemyCount, 20);
            _randomcounts = 0;
        }




        #endregion



        #region Update Methods



        protected override void OnUpdate(TimeSpan deltaTime)
        {
            if (!_inputService.EnableMouseCentering)
                return;

            var deltaTimeF = (float)deltaTime.TotalSeconds;
            TimePassed += TimeSpan.FromSeconds(deltaTime.TotalSeconds);
            string timeString = TimePassed.Minutes.ToString("00") + ":" + TimePassed.Seconds.ToString("00");

            timerModel -= deltaTimeF;
            if (timerModel < 0)
            {
                timerModel = TimerModel;
            }

            timer -= deltaTimeF;
            if (timer < 0 && _enemyCount <= _randomcounts)
            {
                _enemyCount++;
                //Timer expired, execute action
                timer = Timer;   //Reset Timer               

                var randomPosition = new Vector3F(
                    RandomHelper.Random.NextFloat(-10, 10),
                    RandomHelper.Random.NextFloat(2, 5),
                    RandomHelper.Random.NextFloat(-30, -50));
                var pose = new Pose(randomPosition, RandomHelper.Random.NextQuaternionF());
                var poses = new Pose(new Vector3F(0, 2, -50), Matrix33F.CreateRotationY(-ConstantsF.PiOver2));

                var scene = _services.GetInstance<IScene>();
                var simulation = _services.GetInstance<Simulation>();
                _collisionObject.Enabled = true;

                StartPose = pose;


                ModelNode model = _modelPrototype.Clone();
                RigidBody body = _bodyPrototype.Clone();

                //body.Pose = startPose;
                //model.PoseWorld = _bodyPrototype.Pose;

                body.Pose = StartPose;
                _bodyPrototype.Pose = body.Pose;
                //body.Pose = new Pose(new Vector3F(startPose.Position.X, startPose.Position.Y, cameraPose.Position.Z + (-3)));
                model.PoseWorld = _bodyPrototype.Pose;

                scene.Children.Add(model);
                simulation.RigidBodies.Add(body);

                _models.Add(model);
                _bodies.Add(body);

              
           
             
          

                List<TimeSpan> listTimeSpan;
                listTimeSpan = new List<TimeSpan>();
                //listTimeSpan.Add(TimeSpan.FromSeconds(RandomHelper.Random.NextDouble(Convert.ToInt32(TimePassed.Seconds.ToString("00")) + 30, 45)));
                listTimeSpan.Add(_nestedListanimation[_enemyCount-1][0].Time);
                nestedListTimeSpan.Add(listTimeSpan);

                ElapsedTimeSpan.Add(TimeSpan.FromSeconds(0));
            }


            for (int i = 0; i < _models.Count; i++)
            {

                //ElapsedTime += deltaTime;
                //ElapsedTime += TimeSpan.FromSeconds(timerModel);
                //ElapsedTimeSpan[i] += TimeSpan.FromSeconds(nestedListTimeSpan[i][0].TotalSeconds);
                ElapsedTimeSpan[i] += deltaTime;
                TimeSpan totalTime = ElapsedTimeSpan[i];
                TimeSpan End = _nestedListanimation[i][_nestedListanimation[i].Count - 1].Time;

                //loop ariound the total time if necessary
                if (Loop)
                {
                    while (totalTime > End)
                        totalTime -= End;
                }
                else // Otherwise, clamp to the end values
                {
                    Position = _nestedListanimation[i][_nestedListanimation[i].Count - 1].ValPosition;
                    Rotation = _nestedListanimation[i][_nestedListanimation[i].Count - 1].ValRotation;
                    return;
                }

                Position = _nestedListanimation[i][_nestedListanimation[i].Count - 1].ValPosition;

                int j = 0;

                //find the index of the current frame
                while (_nestedListanimation[i][j + 1].Time < totalTime)
                {
                    j++;
                }

                // Find the time since the beginning of this frame
                totalTime -= _nestedListanimation[i][j].Time;

                // Find how far we are between the current and next frame (0 to 1)
                float amt = (float)((totalTime.TotalSeconds) /
                    (_nestedListanimation[i][j + 1].Time - _nestedListanimation[i][j].Time).TotalSeconds);

                // Interpolate position and rotation values between frames

                Position = InterpolationHelper.Lerp(_nestedListanimation[i][j].ValPosition, _nestedListanimation[i][j + 1].ValPosition, amt);
                Rotation = InterpolationHelper.Lerp(_nestedListanimation[i][j].ValRotation, _nestedListanimation[i][j + 1].ValRotation, amt);

                //System.Console.WriteLine("i-" + i + "," + "Seconds-" + TimePassed.Seconds.ToString("00") + "," + "Position-" + _nestedListanimation[i][j].ValPosition + "," + "Rotation-" + _nestedListanimation[i][j].ValRotation);

                Control(Position);
                Control(Rotation);

                UpdateThrusters();

                UpdateRotation();

                UpdateRoll();

                UpdatePitch();


                QuaternionF cameraOrientation = QuaternionF.CreateRotationY(_shipRotation.Y) * QuaternionF.CreateRotationX(_shipRotation.X) * QuaternionF.CreateRotationZ(MathHelper.ToRadians(_shipRotation.Z));



                _shipRotation = new Vector3F(0, 0, _shipMovement.Z);



                _shipMovement = cameraOrientation.Rotate(_shipRotation);

                // Multiply the velocity by time to get the translation for this frame.
                Vector3F translation = _shipMovement * LinearVelocityMagnitude * deltaTimeF;

                _models[i].SetLastPose(true);

                _bodies[i].Pose = new Pose(_models[i].PoseWorld.Position + translation, cameraOrientation);



                _models[i].PoseWorld = _bodies[i].Pose;
                //System.Console.WriteLine("i-" + i + "," + "_models Position-" + _models[i].PoseWorld.Position);

                Pose = _models[i].PoseWorld;
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

        public void Control(Vector3F sCommand)
        {
            string sCommands = " ";
            if (sCommand == new Vector3F(1, 1, 1))
            {
                sCommands = "thrusters forward";
            }
            else if (sCommand == new Vector3F(2, 2, 2))
            {
                sCommands = "rotate left";
            }
            else if (sCommand == new Vector3F(3, 3, 3))
            {
                sCommands = "rotate right";
            }
            else if (sCommand == new Vector3F(4, 4, 4))
            {
                sCommands = "ascend";
            }
            // Switch statement to evaluate our command
            switch (sCommands)
            {

                case "rotate right":
                    _fRotation -= 0.01f;
                    _fRoll -= 0.04f;
                    break;
                case "rotate left":
                    _fRotation += 0.01f;
                    _fRoll += 0.04f;
                    break;
                case "thrusters forward":
                    _fThrusters -= 0.05f;
                    break;
                case "thrusters backward":
                    _fThrusters += 0.05f;
                    break;
            }

        }

        private void UpdateThrusters()
        {

            // Limit thrusters
            if (_fThrusters > M16DSpeed)
            {
                //fThrusters = 2.0f;
                _fThrusters = M16DSpeed;
            }
            else if (_fThrusters < -M16DSpeed)
            {
                //fThrusters = -2.0f;
                _fThrusters = -M16DSpeed;
            }

            // Slow thrusters
            if (_fThrusters > 0.0f)
            {
                _fThrusters -= 0.0025f;
            }
            else if (_fThrusters < 0.0f)
            {
                _fThrusters += 0.0025f;
            }

            // Stop thrusters
            if (Math.Abs(_fThrusters) < 0.0005)
            {
                _fThrusters = 0.0f;
            }

            _shipMovement.X = _fThrusters;
            _shipMovement.Z = _fThrusters;

        }

        private void UpdateRotation()
        {

            // Apply rotation
            _shipRotation.Y += _fRotation;
        }

        private void UpdateRoll()
        {
            if (_fRoll >= 6)
            {
                _fRoll = 6;
            }
            else if (_fRoll <= -6)
            {
                _fRoll = -6;
            }

            // Slow rotation
            if (_fRoll > 0)
            {
                _fRoll -= 0.01f;
            }
            else if (_fRoll < 0)
            {
                _fRoll += 0.01f;
            }

            _shipRotation.Z = -(_fRoll * _fThrusters);

        }

        private void UpdatePitch()
        {

            if (fPitch > 0.10f)
            {
                fPitch = 0.10f;
            }
            else if (fPitch < -0.10f)
            {
                fPitch = -0.10f;
            }

            // Slow rate of pitch
            if (fPitch > 0.0f)
            {
                fPitch -= 0.0005f;
            }
            else if (fPitch < 0.0f)
            {
                fPitch += 0.0005f;
            }

            // Update position and pitch
            _shipMovement.Y += fPitch;

            _shipRotation.X = (fPitch);

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
