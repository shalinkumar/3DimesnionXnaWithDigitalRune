using DigitalRune.Animation;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Game.UI.Rendering;
using DigitalRune.Geometry;
using DigitalRune.Geometry.Collisions;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.Rendering;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Mathematics.Statistics;
using DigitalRune.Particles;
using DigitalRune.Physics;
using DigitalRune.Physics.Constraints;
using DigitalRune.Physics.ForceEffects;
using JohnStriker.GameObjects.AmmoObject;
using JohnStriker.GameObjects.OpponentObjects;
using JohnStriker.GameObjects.PlayerObjects;
using JohnStriker.GraphicsScreen;
using JohnStriker.Sample_Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathHelper = DigitalRune.Mathematics.MathHelper;
using Plane = DigitalRune.Geometry.Shapes.Plane;

namespace JohnStriker.GameObjects
{
    [Sample(SampleCategory.Graphics,
        @"This sample shows how to render and infinite plane of water including waves.",
        @"",
        118)]
    [Controls(@"Sample
  Hold <H>/<Shift>+<H> to decrease/increase the wave height.
  Press <J> to switch water color.
  Press <K> to switch between skybox reflection and planar reflection.
  Press <L> to change caustic settings.")]
    public class MyGameComponent : GameManager
    {
        #region Fields
        private const float Timer = 10;

        private static readonly TimeSpan ExplosionInterval = TimeSpan.FromSeconds(1);

        //private readonly MissileObject.MissileObject MissileObject;   

        private readonly AmmoObject.AmmoObjectFour ammoObject;

        private readonly CameraNode _cameraNode;

        private readonly CollisionDomain _collisionDomain;

        private readonly IInputService _inputService;

        // A list of all used plant meshes.
        private readonly List<Mesh> _meshes = new List<Mesh>();

        private readonly DeferredGraphicsScreen _myGraphicsScreen;

        private readonly PlayerObjects.PlayerObjects _playerObjects;

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private readonly TextBlock _updateFpsTextBlock;

        private readonly WaterNode _waterNode;

        private readonly List<List<DynamicOpponents>> nestedListDynamicOpponents = new List<List<DynamicOpponents>>();

        private readonly List<List<TestHealthBar.TestHealthBar>> nestedListTestHealthBar = new List<List<TestHealthBar.TestHealthBar>>();


        private readonly Player _player;

        private int _causticType;

        private int _enemyCount;

        private StackPanel _fpsPanel;

        private GuiMissileScreen _guiGraphicsScreen;

        private bool _isOpponentObjects = true;

        private bool _isParticleSystemNode;

        private ParticleSystemNode _particleSystemNode;

        private BallJoint _spring;

        private float _springAttachmentDistanceFromObserver;

        private TimeSpan _timeUntilExplosion = TimeSpan.Zero;

        private int _waterColorType;

        private Pose cameraPose;

        private float timer = 10; //Initialize a 10 second timer

        private AudioListener _listener;

        private CameraNode cameraNodeAudio;

        private SoundEffectInstance[] _hitSoundInstances = new SoundEffectInstance[5];

        private AudioEmitter[] _hitEmitters = new AudioEmitter[5];

        private float _timeSinceLastHitSound;

        private SoundEffect _hitSound;

        private int numberOfRollingContacts = 0;

        private readonly SoundEffect missileSoundEffect;

        private static readonly TimeSpan AmmoInterval = TimeSpan.FromSeconds(1);

        private TimeSpan _timeUntilAmmo = TimeSpan.Zero;

        private readonly SoundEffect ammoSoundEffect;

        private const float MinHitForce = 20000;

        private int _hitCount = 0;

        private readonly List<List<KillerOpponents8>> nestedListKillerOpponents = new List<List<KillerOpponents8>>();

        private int _shipMissileHitCount = 0;

        private int _shipMissileHitCountTwo = 0;

        private readonly Image _arrowImage;

        private readonly Image mapImage;

        private readonly AnimatableProperty<Color> _animatableColorMap = new AnimatableProperty<Color> { Value = Color.Black };

        private readonly AnimatableProperty<Vector2F> _animatablePosition = new AnimatableProperty<Vector2F>();

        private SpriteBatch spriteBatch;

        private Texture2D mapTexture2D;

        private Texture2D shellPlayer;

        private Texture2D shellEnemy;

        private RenderTarget2D renderTarget;

        private Vector2 playerShell = new Vector2();

        private int height;

        private int width;

        private Map.Sprite radar;

        private Texture2D line;

        private TimeSpan _timeUntilHealthPArticle = TimeSpan.Zero;

        private static readonly TimeSpan HealthParticleInterval = TimeSpan.FromSeconds(1);

        private bool isNewChildrenHealth = true;

        //private HealthParticle.HealthParticle _healthPArticle;
        #endregion

        public MyGameComponent(Game game)
            : base(game)
        {
            SampleFramework.IsMouseVisible = false;

            _myGraphicsScreen = new DeferredGraphicsScreen(Services);

            _myGraphicsScreen.DrawReticle = true;

            GraphicsService.Screens.Insert(0, _myGraphicsScreen);

            GameObjectService.Objects.Add(new DeferredGraphicsOptionsObject(Services));

            _inputService = Services.GetInstance<IInputService>();

            //InitializeAudio(ContentManager);

            Services.Register(typeof(DebugRenderer), null, _myGraphicsScreen.DebugRenderer);

            Services.Register(typeof(IScene), null, _myGraphicsScreen.Scene);

            // We use one collision domain that computes collision info for all game objects.
            _collisionDomain = new CollisionDomain(new CollisionDetection());

            // Register CollisionDomain in service container.
            Services.Register(typeof(CollisionDomain), null, _collisionDomain);

            // Add gravity and damping to the physics Simulation.
            Simulation.ForceEffects.Add(new Gravity());

            Simulation.ForceEffects.Add(new Damping());

            //Flight Geametric object
            _player = new Player(Services);
            GameObjectService.Objects.Add(_player);

            ////Third Person Camera 
            var thirdPersonCameraObject = new ThirdPersonCameraObject.ThirdPersonCameraObject(_player, Services);
            GameObjectService.Objects.Add(thirdPersonCameraObject);
            _myGraphicsScreen.ActiveCameraNode = thirdPersonCameraObject.CameraNode;

            // Create the UIScreen which is rendered into the back buffer.
            var theme = ContentManager.Load<Theme>("UI Themes/Aero/Theme");
            var renderer = new UIRenderer(Game, theme);
            // Create the UIScreen that is rendered into a render target and mapped onto the 3D game objects.
            _inGameUIScreenRenderTarget = new RenderTarget2D(GraphicsService.GraphicsDevice, 600, 250, false, SurfaceFormat.Color, DepthFormat.None);
            var inGameScreen = new InGameUIScreen(Services, renderer)
            {
                InputEnabled = false,
                Width = _inGameUIScreenRenderTarget.Width,
                Height = _inGameUIScreenRenderTarget.Height,
            };
            UiService.Screens.Add(inGameScreen);


            // More standard objects.
            //GameObjectService.Objects.Add(new GrabObjects.GrabObjects(Services));
            GameObjectService.Objects.Add(new ObjectCreatorObject.ObjectCreatorObject(Services));

            var dynamicSkyObject = new DynamicSkyObject.DynamicSkyObject(Services, true, false, true);
            GameObjectService.Objects.Add(dynamicSkyObject);

            // Load three different plant models.
            // The palm tree consists of a single mesh. It uses the *Vegetation.fx effects.
            var palmModelNode = ContentManager.Load<ModelNode>("Vegetation/PalmTree/palm_tree");
            Mesh palmMesh = ((MeshNode)palmModelNode.Children[0]).Mesh;

            // The bird's nest plant consists of 2 LODs. It uses the *Vegetation.fx effects.
            var plantModelNode = ContentManager.Load<ModelNode>("Vegetation/BirdnestPlant/BirdnestPlant");
            LodGroupNode plantLodGroupNode = plantModelNode.GetDescendants().OfType<LodGroupNode>().First().Clone();

            // The grass model consists of one mesh. It uses the *Grass.fx effects.
            var grassModelNode = ContentManager.Load<ModelNode>("Vegetation/Grass/grass");
            Mesh grassMesh = ((MeshNode)grassModelNode.Children[0]).Mesh;

            // Store all used meshes in a list for use in UpdateMaterialEffectParameters.
            _meshes.Add(palmMesh);
            foreach (MeshNode meshNode in plantLodGroupNode.Levels.Select(lodEntry => lodEntry.Node).OfType<MeshNode>())
                _meshes.Add(meshNode.Mesh);
            _meshes.Add(grassMesh);

            // We can add individual plant instances to the scene like this:
            // (However, this is inefficient for large amounts of plants.)
            _myGraphicsScreen.Scene.Children.Add(new MeshNode(palmMesh)
            {
                PoseLocal = new Pose(new Vector3F(-2, 0, 0))
            });
            plantLodGroupNode.PoseLocal = Pose.Identity;
            _myGraphicsScreen.Scene.Children.Add(plantLodGroupNode);
            _myGraphicsScreen.Scene.Children.Add(new MeshNode(grassMesh)
            {
                PoseLocal = new Pose(new Vector3F(2, 0, 0))
            });

#if WINDOWS
            int numberOfInstancesPerCell = 100;
#else
      int numberOfInstancesPerCell = 10;
#endif


            // Add an island model.
            GameObjectService.Objects.Add(new StaticObject.StaticObject(Services, "Island/Island", new Vector3F(30),
                new Pose(new Vector3F(0, 0.75f, 0)), true, true));
            GameObjectService.Objects.Add(new DynamicObject.DynamicObject(Services, 1));
            GameObjectService.Objects.Add(new FogObject.FogObject(Services, true));


            // Define the appearance of the water.
            var waterOcean = new DigitalRune.Graphics.Water
            {
                SpecularColor = new Vector3F(20f),
                SpecularPower = 500,
                NormalMap0 = null,
                NormalMap1 = null,
                RefractionDistortion = 0.1f,
                ReflectionColor = new Vector3F(0.2f),
                RefractionColor = new Vector3F(0.6f),

                // Water is scattered in high waves and this makes the wave crests brighter.
                // ScatterColor defines the intensity of this effect.
                ScatterColor = new Vector3F(0.05f, 0.1f, 0.1f),

                // Foam is automatically rendered where the water intersects geometry and
                // where wave are high.
                FoamMap = ContentManager.Load<Texture2D>("Water/Foam"),
                FoamMapScale = 5,
                FoamColor = new Vector3F(1),
                FoamCrestMin = 0.3f,
                FoamCrestMax = 0.8f,

                // Approximate underwater caustics are computed in real-time from the waves.
                CausticsSampleCount = 3,
                CausticsIntensity = 3,
                CausticsPower = 100,
            };

            // If we do not specify a shape in the WaterNode constructor, we get an infinite
            // water plane.
            _waterNode = new WaterNode(waterOcean, null)
            {
                PoseWorld = new Pose(new Vector3F(0, 0.5f, 0)),
                SkyboxReflection = _myGraphicsScreen.Scene.GetDescendants().OfType<SkyboxNode>().First(),

                // ExtraHeight must be set to a value greater than the max. wave height. 
                ExtraHeight = 2,
            };
            _myGraphicsScreen.Scene.Children.Add(_waterNode);

            // OceanWaves can be set to displace water surface using a displacement map.
            // The displacement map is computed by the WaterWaveRenderer (see DeferredGraphicsScreen)
            // using FFT and a statistical ocean model.
            _waterNode.Waves = new OceanWaves
            {
                TextureSize = 256,
                HeightScale = 0.004f,
                Wind = new Vector3F(10, 0, 10),
                Directionality = 1,
                Choppiness = 1,
                TileSize = 20,

                // If we enable CPU queries, we can call OceanWaves.GetDisplacement()
                // (see Update() method below).
                EnableCpuQueries = true,
            };

            // Optional: Use a planar reflection instead of the skybox reflection.
            // We add a PlanarReflectionNode as a child of the WaterNode.
            var renderToTexture = new RenderToTexture
            {
                Texture =
                    new RenderTarget2D(GraphicsService.GraphicsDevice, 512, 512, false, SurfaceFormat.HdrBlendable,
                        DepthFormat.None),
            };
            var planarReflectionNode = new PlanarReflectionNode(renderToTexture)
            {
                Shape = _waterNode.Shape,
                NormalLocal = new Vector3F(0, 1, 0),
                IsEnabled = false,
            };
            _waterNode.PlanarReflection = planarReflectionNode;
            _waterNode.Children = new SceneNodeCollection(1) { planarReflectionNode };

            // To let rigid bodies swim, we add a Buoyancy force effect. This force effect
            // computes buoyancy of a flat water surface.
            Simulation.ForceEffects.Add(new Buoyancy
            {
                Surface = new Plane(new Vector3F(0, 1, 0), _waterNode.PoseWorld.Position.Y),
                Density = 1500,
                AngularDrag = 0.3f,
                LinearDrag = 3,
            });

            _waterNode.PlanarReflection.IsEnabled = true;

            //ammo
            ammoObject = new AmmoObjectFour(Services);
            GameObjectService.Objects.Add(ammoObject);

            GraphicsService = Services.GetInstance<IGraphicsService>();
            // Add the GuiGraphicsScreen to the graphics service.
            _guiGraphicsScreen = new GuiMissileScreen(Services);
            GraphicsService.Screens.Add(_guiGraphicsScreen);

            // We add another graphics screen on top which renders the GUI.
            var graphicsScreen = new DelegateGraphicsScreen(GraphicsService)
            {
                RenderCallback = Render,
            };
            GraphicsService.Screens.Insert(1, graphicsScreen);

            height = GraphicsService.GraphicsDevice.PresentationParameters.BackBufferHeight;

            width = GraphicsService.GraphicsDevice.PresentationParameters.BackBufferWidth;

            playerShell = new Vector2(width - 150, height - 100);

            renderTarget = new RenderTarget2D(GraphicsService.GraphicsDevice, GraphicsService.GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsService.GraphicsDevice.PresentationParameters.BackBufferHeight);

            spriteBatch = new SpriteBatch(GraphicsService.GraphicsDevice);

            mapTexture2D = ContentManager.Load<Texture2D>("Map/radar");

            shellPlayer = ContentManager.Load<Texture2D>("Map/ArrowPlayers/airplane");

            shellEnemy = ContentManager.Load<Texture2D>("Map/ArrowPlayers/airplane");

            //line drawing point texture
            line = ContentManager.Load<Texture2D>("Map/ArrowPlayers/dot");

            //create radar image
            radar = new Map.Sprite(spriteBatch);
            radar.Load(ContentManager, "Map/radar");
            radar.position = new Vector2(width - radar.size.X, height -
                radar.size.Y);

            Sound.Sound.Initialize(ContentManager);

            // Start gear sound
            Sound.Sound.StartGearSound();

            // Play game music
            Sound.Sound.Play(Sound.Sound.Sounds.GameMusic);

            List<DynamicOpponents> dynamicOpponents;
            dynamicOpponents = new List<DynamicOpponents>();
            dynamicOpponents.Add(new DynamicOpponents(Services, 0));
            nestedListDynamicOpponents.Add(dynamicOpponents);
            GameObjectService.Objects.Add(nestedListDynamicOpponents[0][0]);


            dynamicOpponents = new List<DynamicOpponents>();
            dynamicOpponents.Add(new DynamicOpponents(Services, 1));
            nestedListDynamicOpponents.Add(dynamicOpponents);
            GameObjectService.Objects.Add(nestedListDynamicOpponents[1][0]);

            dynamicOpponents = new List<DynamicOpponents>();
            dynamicOpponents.Add(new DynamicOpponents(Services, 2));
            nestedListDynamicOpponents.Add(dynamicOpponents);
            GameObjectService.Objects.Add(nestedListDynamicOpponents[2][0]);

            List<KillerOpponents8> killerOpponents;
            killerOpponents = new List<KillerOpponents8>();
            killerOpponents.Add(new KillerOpponents8(Services, 0));
            nestedListKillerOpponents.Add(killerOpponents);
            GameObjectService.Objects.Add(nestedListKillerOpponents[0][0]);          



        }

        private int countHealth = 0;

        private List<HealthParticle.HealthParticle> _healthParticles = new List<HealthParticle.HealthParticle>();

        private readonly RenderTarget2D _inGameUIScreenRenderTarget;



        private static RigidBody AddBody(Simulation simulation, string name, Pose pose, Shape shape,
            MotionType motionType)
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

        public override void Update(GameTime gameTime)
        {
            if (!_inputService.EnableMouseCentering)
                return;

            DebugRenderer debugRenderer = _myGraphicsScreen.DebugRenderer;
            debugRenderer.Clear();

            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            List<DynamicOpponents> dynamicOpponents;
            timer -= deltaTime;


            KeyboardState keyboardState = _inputService.KeyboardState;
            //if (keyboardState.IsKeyDown(Keys.N))
            //{
            //    playerShell.X += 0.5f;
            //}
            //if (keyboardState.IsKeyDown(Keys.M))
            //{
            //    playerShell.X -= 0.5f;
            //}
            //if (keyboardState.IsKeyDown(Keys.K))
            //{
            //    playerShell.Y += 0.5f;
            //}
            //if (keyboardState.IsKeyDown(Keys.L))
            //{
            //    playerShell.Y -= 0.5f;
            //}





            // Change wave height.
            if (InputService.IsDown(Keys.H))
            {
                bool isShiftDown = (InputService.ModifierKeys & ModifierKeys.Shift) != 0;
                float sign = isShiftDown ? +1 : -1;
                float delta = sign * deltaTime * 0.01f;
                var oceanWaves = ((OceanWaves)_waterNode.Waves);
                oceanWaves.HeightScale = Math.Max(0, oceanWaves.HeightScale + delta);
            }

            // Switch water color.
            if (InputService.IsPressed(Keys.J, true))
            {
                if (_waterColorType == 0)
                {
                    _waterColorType = 1;
                    _waterNode.Water.UnderwaterFogDensity = new Vector3F(12, 8, 8) * 0.04f;
                    _waterNode.Water.WaterColor = new Vector3F(10, 30, 79) * 0.002f;
                }
                else
                {
                    _waterColorType = 0;
                    _waterNode.Water.UnderwaterFogDensity = new Vector3F(1, 0.8f, 0.6f);
                    _waterNode.Water.WaterColor = new Vector3F(0.2f, 0.4f, 0.5f);
                }
            }

            // Switch caustics.
            if (InputService.IsPressed(Keys.L, true))
            {
                if (_causticType == 0)
                {
                    _causticType = 1;
                    _waterNode.Water.CausticsSampleCount = 5;
                    _waterNode.Water.CausticsIntensity = 10;
                    _waterNode.Water.CausticsPower = 200;
                }
                else if (_causticType == 1)
                {
                    // Disable caustics
                    _causticType = 2;
                    _waterNode.Water.CausticsIntensity = 0;
                }
                else
                {
                    _causticType = 0;
                    _waterNode.Water.CausticsSampleCount = 3;
                    _waterNode.Water.CausticsIntensity = 3;
                    _waterNode.Water.CausticsPower = 100;
                }
            }

            // Move rigid bodies with the waves:
            // The Buoyancy force effect is only designed for a flat water surface.
            // This code applies some impulses to move the bodies. It is not physically 
            // correct but looks ok.
            // The code tracks 3 arbitrary positions on each body. Info for the positions
            // are stored in RigidBody.UserData. The wave displacements of the previous
            // frame and the current frame are compared an impulse proportional to the 
            // displacement change is applied.
            foreach (RigidBody body in Simulation.RigidBodies)
            {
                if (body.MotionType != MotionType.Dynamic)
                    continue;

                // Check how much the body penetrates the water using a simple AABB check.
                Aabb aabb = body.Aabb;
                var waterPenetration = (float)Math.Pow(
                    MathHelper.Clamp((_waterNode.PoseWorld.Position.Y - aabb.Minimum.Y) / aabb.Extent.Y, 0, 1),
                    3);

                if (waterPenetration < 0)
                {
                    body.UserData = null;
                    continue;
                }

                // 3 displacement vectors are stored in the UserData.
                var previousDisplacements = body.UserData as Vector3F[];
                if (previousDisplacements == null)
                {
                    previousDisplacements = new Vector3F[3];
                    body.UserData = previousDisplacements;
                }

                for (int i = 0; i < 3; i++)
                {
                    // Get an arbitrary position on or near the body.
                    var position = new Vector3F(
                        (i < 2) ? aabb.Minimum.X : aabb.Maximum.X,
                        aabb.Minimum.Y,
                        (i % 2 == 0) ? aabb.Minimum.Z : aabb.Maximum.Z);

                    // Get wave displacement of this position.
                    var waves = (OceanWaves)_waterNode.Waves;
                    Vector3F displacement, normal;
                    waves.GetDisplacement(position.X, position.Z, out displacement, out normal);

                    // Compute velocity from displacement change.
                    Vector3F currentVelocity = body.GetVelocityOfWorldPoint(position);
                    Vector3F desiredVelocity = (displacement - previousDisplacements[i]) / deltaTime;

                    // Apply impulse proportional to the velocity change of the water.
                    Vector3F velocityDelta = desiredVelocity - currentVelocity;
                    body.ApplyImpulse(
                        velocityDelta * body.MassFrame.Mass * waterPenetration * 0.1f,
                        position);

                    previousDisplacements[i] = displacement;
                }
            }



            //Launch missile
            var cameraGameObject =
                (ThirdPersonCameraObject.ThirdPersonCameraObject)GameObjectService.Objects["ThirdPersonCamera"];
            CameraNode cameraNode = cameraGameObject.CameraNode;
            cameraPose = cameraNode.PoseWorld;
            //forward = cameraPose.ToWorldDirection(Vector3F.Forward);
            //Pose worldPose = new Pose(cameraPose.ToWorldDirection(Vector3F.Forward));


            Vector3F cameraPosition = cameraNode.PoseWorld.Position;
            Vector3F cameraDirection = cameraNode.PoseWorld.ToWorldDirection(Vector3F.Forward);

            // Create a ray for picking.
            var ray = new RayShape(cameraPosition, cameraDirection, 1000);

            // The ray should stop at the first hit. We only want the first object.
            ray.StopsAtFirstHit = true;

            // The collision detection requires a CollisionObject.
            var rayCollisionObject = new CollisionObject(new GeometricObject(ray, Pose.Identity));

            // Assign the collision object to collision group 2. (In SampleGame.cs a
            // collision filter based on collision groups was set. Objects for hit-testing
            // are in group 2.)
            rayCollisionObject.CollisionGroup = 2;

            _spring = null;

            // Get the first object that has contact with the ray.
            ContactSet contactSet = Simulation.CollisionDomain.GetContacts(rayCollisionObject).FirstOrDefault();
            //if (contactSet != null && contactSet.Count > 0)
            //{
            //    // The ray has hit something.

            //    // The contact set contains all detected contacts between the ray and the rigid body.
            //    // Get the first contact in the contact set. (A ray hit usually contains exactly 1 contact.)
            //    Contact contact = contactSet[0];

            //    // The contact set contains the object pair of the collision. One object is the ray.
            //    // The other is the object we want to grab.
            //    CollisionObject hitCollisionObject = (contactSet.ObjectA == rayCollisionObject)
            //        ? contactSet.ObjectB
            //        : contactSet.ObjectA;

            //    // Check whether a dynamic rigid body was hit.
            //    var hitBody = hitCollisionObject.GeometricObject as RigidBody;
            //    if (hitBody != null && hitBody.MotionType == MotionType.Static ||
            //        hitBody != null && hitBody.MotionType == MotionType.Dynamic)
            //    {
            //        // Attach the rigid body at the cursor position using a ball-socket joint.
            //        // (Note: We could also use a FixedJoint, if we don't want any rotations.)

            //        // The penetration depth tells us the distance from the ray origin to the rigid body.
            //        _springAttachmentDistanceFromObserver = contact.PenetrationDepth;

            //        // Get the position where the ray hits the other object.
            //        // (The position is defined in the local space of the object.)
            //        Vector3F hitPositionLocal = (contactSet.ObjectA == rayCollisionObject)
            //            ? contact.PositionBLocal
            //            : contact.PositionALocal;

            //        _spring = new BallJoint
            //        {
            //            BodyA = hitBody,
            //            AnchorPositionALocal = hitPositionLocal,

            //            // We need to attach the grabbed object to a second body. In this case we just want to
            //            // anchor the object at a specific point in the world. To achieve this we can use the
            //            // special rigid body "World", which is defined in the simulation.
            //            BodyB = Simulation.World,
            //            // AnchorPositionBLocal is set below.

            //            // Some constraint adjustments.
            //            ErrorReduction = 0.3f,

            //            // We set a softness > 0. This makes the joint "soft" and it will act like
            //            // damped spring. 
            //            Softness = 0.00001f,

            //            // We limit the maximal force. This reduces the ability of this joint to violate
            //            // other constraints. 
            //            MaxForce = 1e6f
            //        };

            //        // Add the spring to the simulation.
            //        Simulation.Constraints.Add(_spring);
            //    }
            //}

            if (_spring != null)
            {
                // User has grabbed something.

                // Update the position of the object by updating the anchor position of
                // the ball-socket joint.
                var cameraGameObjects =
                    (ThirdPersonCameraObject.ThirdPersonCameraObject)GameObjectService.Objects["ThirdPersonCamera"];
                CameraNode cameraNodes = cameraGameObjects.CameraNode;
                Vector3F cameraPositions = cameraNodes.PoseWorld.Position;
                Vector3F cameraDirections = cameraNodes.PoseWorld.ToWorldDirection(-Vector3F.UnitZ);

                _spring.AnchorPositionBLocal = cameraPositions + cameraDirections * _springAttachmentDistanceFromObserver;

                // Reduce the angular velocity by a certain factor. (This acts like a damping because we
                // do not want the object to rotate like crazy.)
                _spring.BodyA.AngularVelocity *= 0.9f;
            }

            //if (_inputService.IsPressed(MouseButtons.Left, true))
            //{
            //    if (_spring != null)
            //    {
            //        Pose pose = _spring.BodyA.Pose;
            //        MissileObject.MissileObject missileObjectOne =
            //            GameObjectService.Objects.OfType<MissileObject.MissileObject>().FirstOrDefault();
            //        if (missileObjectOne != null)
            //        {
            //            missileObjectOne.Spawn(player.Pose, _spring.BodyA.Pose, cameraPose);                   
            //            Sound.Sound.PlayMissileSound(true);
            //        }

            //    }
            //}

            _timeUntilAmmo -= gameTime.ElapsedGameTime;
            _timeSinceLastHitSound += deltaTime;

            //if (_inputService.IsDoubleClick(MouseButtons.Right))
            //{
            //    AmmoObjectFour ammoObject =
            //        GameObjectService.Objects.OfType<AmmoObjectFour>().FirstOrDefault();
            //    if (ammoObject != null)
            //    {
            //        ammoObject.Spawn(player.Pose, cameraPose);
            //        Sound.Sound.PlayAmmoSound(true);
            //    }
            //}

            // Update collision domain. - This will compute collisions.
            _collisionDomain.Update(deltaTime);
            _timeUntilExplosion -= gameTime.ElapsedGameTime;


            foreach (List<DynamicOpponents> t in nestedListDynamicOpponents)
            {
                // Now we could, for example, ask the collision domain if the ships are colliding.
                bool shipsAreColliding = _collisionDomain.HaveContact(
                    t[0].CollisionObject,
                    _player.CollisionObject);

                bool attachedMissileCollision = false;
                if (_player.AttachedMissileCollisionObject != null)
                    attachedMissileCollision = _collisionDomain.HaveContact(t[0].CollisionObject, _player.AttachedMissileCollisionObject);


                bool attachedAmmoCollision = false;
                if (_player.AttachedAmmoCollisionObject != null)
                    attachedAmmoCollision = _collisionDomain.HaveContact(t[0].CollisionObject, _player.AttachedAmmoCollisionObject);

                //bool shipandMissileCollision = _collisionDomain.HaveContact(t[0].CollisionObject,
                //    MissileObject.CollisionObject);

                bool shipandAmmoCollision = _collisionDomain.HaveContact(t[0].CollisionObject,
                 ammoObject.CollisionObject);

                //Vector3F evadeVector3F = EvadeBehavior.OnUpdateSteeringForce(deltaTime, t[0].Pose.Position, player.Pose.Position, t[0]._modelPrototype, player._cameraNode);             

                if (attachedMissileCollision)
                {
                    if (_timeUntilExplosion <= TimeSpan.Zero)
                    {
                        _shipMissileHitCountTwo = _shipMissileHitCountTwo + 49;
                        _shipMissileHitCountTwo++;
                        _isParticleSystemNode = true;
                        var _explosion = new Explosion.Explosion(ContentManager,
                            new Pose(t[0].CollisionObject.GeometricObject.Pose.Position));
                        ParticleSystemService.ParticleSystems.Add(_explosion);
                        _particleSystemNode = new ParticleSystemNode(_explosion);
                        var scene = Services.GetInstance<IScene>();
                        scene.Children.Add(_particleSystemNode);

                        _explosion.Explode();

                        Sound.Sound.PlayExplosionSound(true);
                        _timeUntilExplosion = ExplosionInterval;

                        //t[0].Dispose();

                        t[0].DisposeByMissile(_shipMissileHitCountTwo);
                    }
                }

                if (attachedAmmoCollision)
                {
                    if (_timeUntilExplosion <= TimeSpan.Zero)
                    {
                        _isParticleSystemNode = true;
                        var _explosion = new Explosion.Explosion(ContentManager,
                            new Pose(t[0].CollisionObject.GeometricObject.Pose.Position));
                        ParticleSystemService.ParticleSystems.Add(_explosion);
                        _particleSystemNode = new ParticleSystemNode(_explosion);
                        var scene = Services.GetInstance<IScene>();
                        scene.Children.Add(_particleSystemNode);

                        _explosion.Explode();

                        Sound.Sound.PlayExplosionSound(true);
                        _timeUntilExplosion = ExplosionInterval;

                        //t[0].Dispose();
                    }
                }

                if (shipandAmmoCollision)
                {
                    if (_timeUntilExplosion <= TimeSpan.Zero)
                    {
                        _isParticleSystemNode = true;
                        var _explosion = new Explosion.AmmoExplosion(ContentManager,
                          new Pose(t[0].CollisionObject.GeometricObject.Pose.Position));
                        ParticleSystemService.ParticleSystems.Add(_explosion);
                        _particleSystemNode = new ParticleSystemNode(_explosion);
                        var scene = Services.GetInstance<IScene>();
                        scene.Children.Add(_particleSystemNode);

                        _explosion.Explode();

                        Sound.Sound.PlayExplosionSound(true);
                        _timeUntilExplosion = ExplosionInterval;
                    }
                }

                //if (shipandMissileCollision)
                //{
                //    if (_timeUntilExplosion <= TimeSpan.Zero)
                //    {
                //        _isParticleSystemNode = true;
                //        var _explosion = new Explosion.Explosion(ContentManager,
                //            new Pose(t[0].CollisionObject.GeometricObject.Pose.Position));
                //        ParticleSystemService.ParticleSystems.Add(_explosion);
                //        _particleSystemNode = new ParticleSystemNode(_explosion);
                //        var scene = Services.GetInstance<IScene>();
                //        scene.Children.Add(_particleSystemNode);

                //        _explosion.Explode();

                //        Sound.Sound.PlayExplosionSound(true);
                //        _timeUntilExplosion = ExplosionInterval;

                //        MissileObject.Dispose();
                //        t[0].Dispose();
                //    }
                //}

                Vector3F forward = Vector3F.Zero;

                if (shipsAreColliding)
                {
                    if (_timeUntilExplosion <= TimeSpan.Zero)
                    {
                        _isParticleSystemNode = true;
                        var _explosion = new Explosion.Explosion(ContentManager,
                            new Pose(t[0].CollisionObject.GeometricObject.Pose.Position));
                        ParticleSystemService.ParticleSystems.Add(_explosion);
                        _particleSystemNode = new ParticleSystemNode(_explosion);
                        var scene = Services.GetInstance<IScene>();
                        scene.Children.Add(_particleSystemNode);

                        _explosion.Explode();

                        Sound.Sound.PlayExplosionSound(true);
                        _timeUntilExplosion = ExplosionInterval;
                        t[0].Dispose();
                    }
                    //}              
                }
            }

            foreach (List<KillerOpponents8> killer in nestedListKillerOpponents)
            {
                bool shipandKillerCollision = _collisionDomain.HaveContact(killer[0].CollisionObject,
          _player.CollisionObject);

                bool shipandMissileCollision = false;
                if (killer[0].AttachedMissileCollisionObject != null)
                    shipandMissileCollision = _collisionDomain.HaveContact(_player.CollisionObject,
                           killer[0].AttachedMissileCollisionObject);

                bool attachedAmmoCollision = false;
                if (_player.AttachedAmmoCollisionObject != null)
                    attachedAmmoCollision = _collisionDomain.HaveContact(killer[0].CollisionObject, _player.AttachedAmmoCollisionObject);

                if (shipandKillerCollision)
                {
                    if (_timeUntilExplosion <= TimeSpan.Zero)
                    {
                        _isParticleSystemNode = true;
                        var _explosion = new Explosion.Explosion(ContentManager,
                            new Pose(killer[0].CollisionObject.GeometricObject.Pose.Position));
                        ParticleSystemService.ParticleSystems.Add(_explosion);
                        _particleSystemNode = new ParticleSystemNode(_explosion);
                        var scene = Services.GetInstance<IScene>();
                        scene.Children.Add(_particleSystemNode);

                        _explosion.Explode();

                        Sound.Sound.PlayExplosionSound(true);
                        _timeUntilExplosion = ExplosionInterval;
                        // player.Dispose();
                    }
                }


                if (shipandMissileCollision)
                {
                    if (_timeUntilExplosion <= TimeSpan.Zero)
                    {
                        _shipMissileHitCount = _shipMissileHitCount + 19;
                        _shipMissileHitCount++;
                        _isParticleSystemNode = true;
                        var _explosion = new Explosion.Explosion(ContentManager,
                            new Pose(_player.CollisionObject.GeometricObject.Pose.Position));
                        ParticleSystemService.ParticleSystems.Add(_explosion);
                        _particleSystemNode = new ParticleSystemNode(_explosion);
                        var scene = Services.GetInstance<IScene>();
                        scene.Children.Add(_particleSystemNode);

                        _explosion.Explode();

                        Sound.Sound.PlayExplosionSound(true);
                        _timeUntilExplosion = ExplosionInterval;

                        _player.DisposeByMissile(_shipMissileHitCount);
                        //if (_shipMissileHitCount == 5)
                        //{
                        //    player.Dispose();
                        //    //GameObjectService.Objects.Remove(player);
                        //}
                    }
                }


                if (attachedAmmoCollision)
                {
                    //if (_timeUntilExplosion <= TimeSpan.Zero)
                    //{
                    _hitCount = _hitCount + 1;
                    _hitCount++;
                    _isParticleSystemNode = true;
                    var explosion = new Explosion.Explosion(ContentManager,
                        new Pose(killer[0].CollisionObject.GeometricObject.Pose.Position));
                    ParticleSystemService.ParticleSystems.Add(explosion);
                    _particleSystemNode = new ParticleSystemNode(explosion);
                    var scene = Services.GetInstance<IScene>();
                    scene.Children.Add(_particleSystemNode);

                    explosion.Explode();

                    Sound.Sound.PlayExplosionSound(true);
                    _timeUntilExplosion = ExplosionInterval;

                    killer[0].DisposeByBullet(_hitCount);
                    //}
                }
            }

            _timeUntilHealthPArticle -= gameTime.ElapsedGameTime;

            if (_timeUntilHealthPArticle <= TimeSpan.Zero)
            {
                if (countHealth <= 0)
                {               
                    _isParticleSystemNode = true;
                    //_healthParticles = new List<HealthParticle.HealthParticle>();

                    Vector3F randomPosition;
                    randomPosition.X = RandomHelper.Random.NextFloat(0, 5);
                    randomPosition.Y = RandomHelper.Random.NextFloat(1, 3);
                    randomPosition.Z = RandomHelper.Random.NextFloat(0, -40);

                    _healthParticles.Add(new HealthParticle.HealthParticle(ContentManager, Services, isNewChildrenHealth));

                    ParticleSystemService.ParticleSystems.Add(_healthParticles[countHealth]);
                    _particleSystemNode = new ParticleSystemNode(_healthParticles[countHealth]);
                    var scene = Services.GetInstance<IScene>();
                    scene.Children.Add(_particleSystemNode);
                    countHealth++;
                    isNewChildrenHealth = false;
                }
             
              
                _timeUntilHealthPArticle = HealthParticleInterval;
            }

            if (_healthParticles[0].CollisionObject != null)
            {
                for (int index = 0; index < _healthParticles[0].Children.Count; index++)
                {
                    bool healthbarCollision = _collisionDomain.HaveContact(_healthParticles[0].CollisionObject[index],
                        _player.CollisionObject);
                    if (healthbarCollision)
                    {
                        _healthParticles[0].Children.RemoveAt(index);
                        _healthParticles[0].ModelNode[index].Parent.Children.Remove(_healthParticles[0].ModelNode[index]);
                         _healthParticles[0].ModelNode[index].Dispose(false);                     
                        _healthParticles[0].ModelNode[index].IsEnabled = false;
                        _healthParticles[0]._collisionObject.RemoveAt(index);
                        _healthParticles[0].CountHealth--;
                        //_healthParticles[0]._collisionObject[index].Enabled = false;
                        _healthParticles[0].ModelNode.RemoveAt(index);
                    }
                }
            }
           


            //GameObjectService.Objects.Add(healthPArticle);

            if (_isParticleSystemNode)
            {
                _particleSystemNode.Synchronize(GraphicsService);
            }
        }


        private ParticleSystemCollection _childrenParticleSystemCollection = new ParticleSystemCollection();

        //private void Render(RenderContext context)
        //{
        //    var graphicsDevice = context.GraphicsService.GraphicsDevice;

        //    // Set the device to the render target
        //    graphicsDevice.SetRenderTarget(context.RenderTarget);

        //    //graphicsDevice.Clear(Color.Red);


        //    // Draw sprite centered at the animated position.
        //    Vector2 position = new Vector2(mapTexture2D.Width, mapTexture2D.Height);
        //    var origin = new Vector2(0, 0);
        //    var rx = width - 150;
        //    var ry = height - 150;

        //    int x = 0;
        //    int y = 0;

        //    foreach (List<DynamicOpponents> t in nestedListDynamicOpponents)
        //    {
        //        x = rx + (int)t[0].Pose.Position.X / 8;
        //        y = ry + (int)t[0].Pose.Position.Y / 8;

        //        spriteBatch.Begin();
        //        spriteBatch.Draw(shellEnemy, new Vector2(x, y), new Rectangle(0, 0, shellEnemy.Width, shellEnemy.Height),
        //            Color.Blue, t[0].angle, origin, 0.2f, SpriteEffects.None, 1);
        //        spriteBatch.End();
        //    }

        //     spriteBatch.Begin();
        //    DrawRadar();
        //         spriteBatch.End();

        //    spriteBatch.Begin();
        //    Vector2 pos = Vector2.Zero;
        //    spriteBatch.Draw(mapTexture2D, new Rectangle(width - 320, height - 180, mapTexture2D.Width, mapTexture2D.Height), _animatableColorMap.Value);
        //    spriteBatch.End();


        //    var px = width - 150;
        //    var py = height - 100;

        //    int ax = px + (int)_player.Pose.Position.X / 8;
        //    int by = py + (int)_player.Pose.Position.Y / 8;

        //    var location = new Vector2((int)playerShell.X, (int)playerShell.Y);
        //    var sourceRectangle = new Rectangle(0, 0, shellPlayer.Width, shellPlayer.Height);


        //    spriteBatch.Begin();
        //    spriteBatch.Draw(shellPlayer, new Vector2(px, py), sourceRectangle, Color.DarkGreen, _player.angle, origin, 0.2f, SpriteEffects.None, 1);
        //    //spriteBatch.Draw(shellPlayer, new Vector2(_playerSprite._drawRectangle.X, _playerSprite._drawRectangle.Y), _playerSprite._sourceRectangle, Color.DarkGreen, _player.angle, origin, 0.2f, SpriteEffects.None, 1);
        //    spriteBatch.End();

        //    // Reset the device to the back buffer
        //    graphicsDevice.SetRenderTarget(context.RenderTarget);

        //    //graphicsDevice.Clear(Color.Red);

        //    //spriteBatch.Begin();
        //    //spriteBatch.Draw((Texture2D)renderTarget,
        //    //    new Vector2(200, 50),         
        //    //    new Rectangle(0, 0, 200, 200), 
        //    //    Color.White                  
        //    //    );
        //    //spriteBatch.End();

        //}


        private void Render(RenderContext context)
        {

            if (!_inputService.EnableMouseCentering)
                return;

            var graphicsDevice = context.GraphicsService.GraphicsDevice;

            // Set the device to the render target
            graphicsDevice.SetRenderTarget(context.RenderTarget);


            spriteBatch.Begin();
            DrawRadar();
            spriteBatch.End();

        }

        private void DrawRadar()
        {
            radar.Draw();

            var rx = (int)radar.position.X;
            var ry = (int)radar.position.Y;

            var mx = (int)mapTexture2D.Width;
            var my = (int)mapTexture2D.Height;

            //show player's position
            var x = rx + (int)_player.Pose.Position.X / 8;
            var y = ry + (int)_player.Pose.Position.Z / 8;
            if (x >= width - 180)
            {
                x = width - 180;
            }

            if (x <= 800)
            {
                x = 800;
            }

            if (y >= 605)
            {
                y = 605;
            }
            if (y <= height - 270)
            {
                y = height - 270;
            }
            DrawBox(x, y, x + 10, y + 10, Color.Green, mx, my);

            //show enemy  position
            foreach (List<DynamicOpponents> t in nestedListDynamicOpponents)
            {

                x = rx + (int)t[0].Pose.Position.X / 8;
                y = ry + (int)t[0].Pose.Position.Z / 8;


                if (x >= width - 180)
                {
                    x = width - 180;
                }

                if (x <= 800)
                {
                    x = 800;
                }

                if (y >= 605)
                {
                    y = 605;
                }
                if (y <= height - 270)
                {
                    y = height - 270;
                }


                DrawBox(x, y, x + 10, y + 10, Color.Blue, mx, my);
            }


            foreach (List<KillerOpponents8> killer in nestedListKillerOpponents)
            {
                x = rx + (int)killer[0].Pose.Position.X / 8;
                y = ry + (int)killer[0].Pose.Position.Z / 8;


                if (x >= width - 180)
                {
                    x = width - 180;
                }

                if (x <= 800)
                {
                    x = 800;
                }

                if (y >= 605)
                {
                    y = 605;
                }
                if (y <= height - 270)
                {
                    y = height - 270;
                }


                DrawBox(x, y, x + 10, y + 10, Color.Red, mx, my);
            }

            //x = rx + (int)_player.Pose.Position.X / 8 - 4;
            //y = ry + (int)_player.Pose.Position.Z / 8 - 4;
            //DrawBox(x, y, x + 1, y + 1, Color.Red, mx, my);



            //foreach (List<DynamicOpponents> t in nestedListDynamicOpponents)
            //{
            //    if (t[0].CollisionObject.Enabled)
            //    {
            //        x = rx + (int)t[0].Pose.Position.X / 8 - 4;
            //        y = ry + (int)t[0].Pose.Position.Z / 8 - 4;
            //        DrawBox(x, y, x + 1, y + 1, Color.Red, mx, my);
            //    }
            //}
        }

        private void DrawBox(int x1, int y1, int x2, int y2, Color color, int mx, int my)
        {
            Draw(x1, y1, y2, color, mx, my);
            //DrawVLine(x1, y1, y2, color, mx, my);
            //DrawVLine(x2, y1, y2, color, mx, my);
            //DrawHLine(x1, x2, y1, color, mx, my);
            //DrawHLine(x1, x2, y2, color, mx, my);
        }

        private void DrawHLine(int x1, int x2, int y, Color color, int mx, int my)
        {
            spriteBatch.Draw(line, new Rectangle(x1 + mx / 2, y + my / 2, 1 + x2 - x1, 1), color);
        }

        private void DrawVLine(int x, int y1, int y2, Color color, int mx, int my)
        {
            spriteBatch.Draw(line, new Rectangle(x + mx / 2, y1 + my / 2, 1, 1 + y2 - y1), color);
        }

        private void Draw(int x, int y1, int y2, Color color, int mx, int my)
        {
            spriteBatch.Draw(shellPlayer, new Vector2(x + mx / 2, y1 + my / 2), new Rectangle(0, 0, shellPlayer.Width, shellPlayer.Height), color, 0f, new Vector2(0, 0), 0.2f, SpriteEffects.None, 1);
        }
    }
}