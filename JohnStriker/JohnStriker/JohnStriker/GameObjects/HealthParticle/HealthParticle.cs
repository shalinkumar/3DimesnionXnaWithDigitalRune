using System;
using System.Collections.Generic;
using System.Linq;
using DigitalRune.Geometry;
using DigitalRune.Geometry.Collisions;
using DigitalRune.Geometry.Meshes;
using DigitalRune.Geometry.Partitioning;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Mathematics.Statistics;
using DigitalRune.Particles;
using DigitalRune.Particles.Effectors;
using DigitalRune.Physics;
using JohnStriker.Annotations;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace JohnStriker.GameObjects.HealthParticle
{
    public class HealthParticle : ParticleSystem
    {
        private static readonly TimeSpan HealthParticleInterval = TimeSpan.FromSeconds(30);
        public readonly List<CollisionObject> _collisionObject;
        private readonly ContentManager _contentManager;
        public readonly List<ModelNode> ModelNode = new List<ModelNode>();
        private readonly IServiceLocator _services;
        private RigidBody _bodyPrototype;
        public int CountHealth;
        private GeometricObject _geometricObject;
        //private Pose _pose;
        private Simulation _simulation;
        private TimeSpan _timeUntilHealthPArticle = TimeSpan.Zero;

        public HealthParticle(ContentManager contentManager, IServiceLocator services,bool isNew)
        {
            _collisionObject = new List<CollisionObject>();
            _contentManager = contentManager;
            Children = new ParticleSystemCollection();
            _services = services;
            if (isNew)
            {
            }
            //Children = new ParticleSystemCollection
            //{
            //    CreateHealthOne(contentManager, count),
            //    CreateHealthTwo(contentManager, count),
            //    CreateHealthThree(contentManager, count),
            //    CreateHealthFour(contentManager, count),
            //    CreateHealthFive(contentManager, count),
            //    CreateHealthSix(contentManager, count),
            //    CreateHealthSeven(contentManager, count),
            //    CreateHealthEight(contentManager, count),
            //    CreateHealthNine(contentManager, count),
            //    CreateHealthTen(contentManager, count),
            //};
        }

        private new Pose Pose
        {
            set
            {
                //_pose = value;
                foreach (ModelNode t in ModelNode)
                {
                    _geometricObject.Pose = t.PoseWorld;
                }
                //_geometricObject.Pose = _modelNode.PoseWorld;
            }
        }

        public List<CollisionObject> CollisionObject
        {
            get { return _collisionObject; }
        }

        private ParticleSystem CreateHealthTen(ContentManager contentManager, int count)
        {
            Vector3F randomPosition;
            randomPosition.X = RandomHelper.Random.NextFloat(0, 5);
            randomPosition.Y = RandomHelper.Random.NextFloat(1, 3);
            randomPosition.Z = RandomHelper.Random.NextFloat(0, -40);
            var ps = new ParticleSystem
            {
                Name = "MyFirstParticleSystem" + count,
                MaxNumberOfParticles = 200,
                ReferenceFrame = ParticleReferenceFrame.World,
                Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z)),
                Random = Random,
            };
            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));
            ModelNode.Add(contentManager.Load<ModelNode>("HeartHealthbar/HeartHealthbar").Clone());
            _bodyPrototype.Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z - (-1)));
            ModelNode[count].PoseWorld = _bodyPrototype.Pose;
            _simulation = _services.GetInstance<Simulation>();
            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(ModelNode[count]);
            CreateRigidBody(ModelNode[count]);
            var shape = (Shape)ModelNode[count].UserData;
            _geometricObject = new GeometricObject(shape, ModelNode[count].PoseWorld);
            _collisionObject.Add(new CollisionObject(_geometricObject));
            _collisionObject[count].Type = CollisionObjectType.Trigger;
            _collisionObject[count].Enabled = true;
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject[count]);
            IParticleParameter<float> lifetimeParameter = ps.Parameters.AddUniform<float>("Lifetime");
            lifetimeParameter.DefaultValue = 1f;
            ps.Parameters.AddVarying<Vector3F>("Position");
            ps.Effectors.Add(new StartPositionEffector
            {
                Distribution = new SphereDistribution { OuterRadius = 2 }
            });
            ps.Parameters.AddVarying<float>("Alpha");
            ps.Effectors.Add(new SingleFadeEffector
            {
                ValueParameter = "Alpha",
                FadeInStart = 0.0f,
                FadeInEnd = 0.3f,
                FadeOutStart = 0.5f,
                FadeOutEnd = 1.0f,
            });
            IParticleParameter<Texture2D> textureParameter = ps.Parameters.AddUniform<Texture2D>("Texture");
            textureParameter.DefaultValue = contentManager.Load<Texture2D>("Particles/LensFlare");
            IParticleParameter<float> blendModeParameter = ps.Parameters.AddUniform<float>("BlendMode");
            blendModeParameter.DefaultValue = 0.0f;
            return ps;
        }

        private ParticleSystem CreateHealthNine(ContentManager contentManager, int count)
        {
            Vector3F randomPosition;
            randomPosition.X = RandomHelper.Random.NextFloat(0, 5);
            randomPosition.Y = RandomHelper.Random.NextFloat(1, 3);
            randomPosition.Z = RandomHelper.Random.NextFloat(0, -40);
            var ps = new ParticleSystem
            {
                Name = "MyFirstParticleSystem" + count,
                MaxNumberOfParticles = 200,
                ReferenceFrame = ParticleReferenceFrame.World,
                Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z)),
                Random = Random,
            };
            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));
            ModelNode.Add(contentManager.Load<ModelNode>("HeartHealthbar/HeartHealthbar").Clone());
            _bodyPrototype.Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z - (-1)));
            ModelNode[count].PoseWorld = _bodyPrototype.Pose;
            _simulation = _services.GetInstance<Simulation>();
            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(ModelNode[count]);
            CreateRigidBody(ModelNode[count]);
            var shape = (Shape)ModelNode[count].UserData;
            _geometricObject = new GeometricObject(shape, ModelNode[count].PoseWorld);
            _collisionObject.Add(new CollisionObject(_geometricObject));
            _collisionObject[count].Type = CollisionObjectType.Trigger;
            _collisionObject[count].Enabled = true;
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject[count]);
            IParticleParameter<float> lifetimeParameter = ps.Parameters.AddUniform<float>("Lifetime");
            lifetimeParameter.DefaultValue = 1f;
            ps.Parameters.AddVarying<Vector3F>("Position");
            ps.Effectors.Add(new StartPositionEffector
            {
                Distribution = new SphereDistribution { OuterRadius = 2 }
            });
            ps.Parameters.AddVarying<float>("Alpha");
            ps.Effectors.Add(new SingleFadeEffector
            {
                ValueParameter = "Alpha",
                FadeInStart = 0.0f,
                FadeInEnd = 0.3f,
                FadeOutStart = 0.5f,
                FadeOutEnd = 1.0f,
            });
            IParticleParameter<Texture2D> textureParameter = ps.Parameters.AddUniform<Texture2D>("Texture");
            textureParameter.DefaultValue = contentManager.Load<Texture2D>("Particles/LensFlare");
            IParticleParameter<float> blendModeParameter = ps.Parameters.AddUniform<float>("BlendMode");
            blendModeParameter.DefaultValue = 0.0f;
            return ps;
        }

        private ParticleSystem CreateHealthEight(ContentManager contentManager, int count)
        {
            Vector3F randomPosition;
            randomPosition.X = RandomHelper.Random.NextFloat(0, 5);
            randomPosition.Y = RandomHelper.Random.NextFloat(1, 3);
            randomPosition.Z = RandomHelper.Random.NextFloat(0, -40);
            var ps = new ParticleSystem
            {
                Name = "MyFirstParticleSystem" + count,
                MaxNumberOfParticles = 200,
                ReferenceFrame = ParticleReferenceFrame.World,
                Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z)),
                Random = Random,
            };
            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));
            ModelNode.Add(contentManager.Load<ModelNode>("HeartHealthbar/HeartHealthbar").Clone());
            _bodyPrototype.Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z - (-1)));
            ModelNode[count].PoseWorld = _bodyPrototype.Pose;
            _simulation = _services.GetInstance<Simulation>();
            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(ModelNode[count]);
            CreateRigidBody(ModelNode[count]);
            var shape = (Shape)ModelNode[count].UserData;
            _geometricObject = new GeometricObject(shape, ModelNode[count].PoseWorld);
            _collisionObject.Add(new CollisionObject(_geometricObject));
            _collisionObject[count].Type = CollisionObjectType.Trigger;
            _collisionObject[count].Enabled = true;
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject[count]);
            IParticleParameter<float> lifetimeParameter = ps.Parameters.AddUniform<float>("Lifetime");
            lifetimeParameter.DefaultValue = 1f;
            ps.Parameters.AddVarying<Vector3F>("Position");
            ps.Effectors.Add(new StartPositionEffector
            {
                Distribution = new SphereDistribution { OuterRadius = 2 }
            });
            ps.Parameters.AddVarying<float>("Alpha");
            ps.Effectors.Add(new SingleFadeEffector
            {
                ValueParameter = "Alpha",
                FadeInStart = 0.0f,
                FadeInEnd = 0.3f,
                FadeOutStart = 0.5f,
                FadeOutEnd = 1.0f,
            });
            IParticleParameter<Texture2D> textureParameter = ps.Parameters.AddUniform<Texture2D>("Texture");
            textureParameter.DefaultValue = contentManager.Load<Texture2D>("Particles/LensFlare");
            IParticleParameter<float> blendModeParameter = ps.Parameters.AddUniform<float>("BlendMode");
            blendModeParameter.DefaultValue = 0.0f;
            return ps;
        }

        private ParticleSystem CreateHealthSeven(ContentManager contentManager, int count)
        {
            Vector3F randomPosition;
            randomPosition.X = RandomHelper.Random.NextFloat(0, 5);
            randomPosition.Y = RandomHelper.Random.NextFloat(1, 3);
            randomPosition.Z = RandomHelper.Random.NextFloat(0, -40);
            var ps = new ParticleSystem
            {
                Name = "MyFirstParticleSystem" + count,
                MaxNumberOfParticles = 200,
                ReferenceFrame = ParticleReferenceFrame.World,
                Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z)),
                Random = Random,
            };
            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));
            ModelNode.Add(contentManager.Load<ModelNode>("HeartHealthbar/HeartHealthbar").Clone());
            _bodyPrototype.Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z - (-1)));
            ModelNode[count].PoseWorld = _bodyPrototype.Pose;
            _simulation = _services.GetInstance<Simulation>();
            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(ModelNode[count]);
            CreateRigidBody(ModelNode[count]);
            var shape = (Shape)ModelNode[count].UserData;
            _geometricObject = new GeometricObject(shape, ModelNode[count].PoseWorld);
            _collisionObject.Add(new CollisionObject(_geometricObject));
            _collisionObject[count].Type = CollisionObjectType.Trigger;
            _collisionObject[count].Enabled = true;
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject[count]);
            IParticleParameter<float> lifetimeParameter = ps.Parameters.AddUniform<float>("Lifetime");
            lifetimeParameter.DefaultValue = 1f;
            ps.Parameters.AddVarying<Vector3F>("Position");
            ps.Effectors.Add(new StartPositionEffector
            {
                Distribution = new SphereDistribution { OuterRadius = 2 }
            });
            ps.Parameters.AddVarying<float>("Alpha");
            ps.Effectors.Add(new SingleFadeEffector
            {
                ValueParameter = "Alpha",
                FadeInStart = 0.0f,
                FadeInEnd = 0.3f,
                FadeOutStart = 0.5f,
                FadeOutEnd = 1.0f,
            });
            IParticleParameter<Texture2D> textureParameter = ps.Parameters.AddUniform<Texture2D>("Texture");
            textureParameter.DefaultValue = contentManager.Load<Texture2D>("Particles/LensFlare");
            IParticleParameter<float> blendModeParameter = ps.Parameters.AddUniform<float>("BlendMode");
            blendModeParameter.DefaultValue = 0.0f;
            return ps;
        }

        private ParticleSystem CreateHealthSix(ContentManager contentManager, int count)
        {
            Vector3F randomPosition;
            randomPosition.X = RandomHelper.Random.NextFloat(0, 5);
            randomPosition.Y = RandomHelper.Random.NextFloat(1, 3);
            randomPosition.Z = RandomHelper.Random.NextFloat(0, -40);
            var ps = new ParticleSystem
            {
                Name = "MyFirstParticleSystem" + count,
                MaxNumberOfParticles = 200,
                ReferenceFrame = ParticleReferenceFrame.World,
                Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z)),
                Random = Random,
            };
            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));
            ModelNode.Add(contentManager.Load<ModelNode>("HeartHealthbar/HeartHealthbar").Clone());
            _bodyPrototype.Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z - (-1)));
            ModelNode[count].PoseWorld = _bodyPrototype.Pose;
            _simulation = _services.GetInstance<Simulation>();
            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(ModelNode[count]);
            CreateRigidBody(ModelNode[count]);
            var shape = (Shape)ModelNode[count].UserData;
            _geometricObject = new GeometricObject(shape, ModelNode[count].PoseWorld);
            _collisionObject.Add(new CollisionObject(_geometricObject));
            _collisionObject[count].Type = CollisionObjectType.Trigger;
            _collisionObject[count].Enabled = true;
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject[count]);
            IParticleParameter<float> lifetimeParameter = ps.Parameters.AddUniform<float>("Lifetime");
            lifetimeParameter.DefaultValue = 1f;
            ps.Parameters.AddVarying<Vector3F>("Position");
            ps.Effectors.Add(new StartPositionEffector
            {
                Distribution = new SphereDistribution { OuterRadius = 2 }
            });
            ps.Parameters.AddVarying<float>("Alpha");
            ps.Effectors.Add(new SingleFadeEffector
            {
                ValueParameter = "Alpha",
                FadeInStart = 0.0f,
                FadeInEnd = 0.3f,
                FadeOutStart = 0.5f,
                FadeOutEnd = 1.0f,
            });
            IParticleParameter<Texture2D> textureParameter = ps.Parameters.AddUniform<Texture2D>("Texture");
            textureParameter.DefaultValue = contentManager.Load<Texture2D>("Particles/LensFlare");
            IParticleParameter<float> blendModeParameter = ps.Parameters.AddUniform<float>("BlendMode");
            blendModeParameter.DefaultValue = 0.0f;
            return ps;
        }

        private ParticleSystem CreateHealthFive(ContentManager contentManager, int count)
        {
            Vector3F randomPosition;
            randomPosition.X = RandomHelper.Random.NextFloat(0, 5);
            randomPosition.Y = RandomHelper.Random.NextFloat(1, 3);
            randomPosition.Z = RandomHelper.Random.NextFloat(0, -40);
            var ps = new ParticleSystem
            {
                Name = "MyFirstParticleSystem" + count,
                MaxNumberOfParticles = 200,
                ReferenceFrame = ParticleReferenceFrame.World,
                Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z)),
                Random = Random,
            };
            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));
            ModelNode.Add(contentManager.Load<ModelNode>("HeartHealthbar/HeartHealthbar").Clone());
            _bodyPrototype.Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z - (-1)));
            ModelNode[count].PoseWorld = _bodyPrototype.Pose;
            _simulation = _services.GetInstance<Simulation>();
            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(ModelNode[count]);
            CreateRigidBody(ModelNode[count]);
            var shape = (Shape)ModelNode[count].UserData;
            _geometricObject = new GeometricObject(shape, ModelNode[count].PoseWorld);
            _collisionObject.Add(new CollisionObject(_geometricObject));
            _collisionObject[count].Type = CollisionObjectType.Trigger;
            _collisionObject[count].Enabled = true;
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject[count]);
            IParticleParameter<float> lifetimeParameter = ps.Parameters.AddUniform<float>("Lifetime");
            lifetimeParameter.DefaultValue = 1f;
            ps.Parameters.AddVarying<Vector3F>("Position");
            ps.Effectors.Add(new StartPositionEffector
            {
                Distribution = new SphereDistribution { OuterRadius = 2 }
            });
            ps.Parameters.AddVarying<float>("Alpha");
            ps.Effectors.Add(new SingleFadeEffector
            {
                ValueParameter = "Alpha",
                FadeInStart = 0.0f,
                FadeInEnd = 0.3f,
                FadeOutStart = 0.5f,
                FadeOutEnd = 1.0f,
            });
            IParticleParameter<Texture2D> textureParameter = ps.Parameters.AddUniform<Texture2D>("Texture");
            textureParameter.DefaultValue = contentManager.Load<Texture2D>("Particles/LensFlare");
            IParticleParameter<float> blendModeParameter = ps.Parameters.AddUniform<float>("BlendMode");
            blendModeParameter.DefaultValue = 0.0f;
            return ps;
        }

        private ParticleSystem CreateHealthFour(ContentManager contentManager, int count)
        {
            Vector3F randomPosition;
            randomPosition.X = RandomHelper.Random.NextFloat(0, 5);
            randomPosition.Y = RandomHelper.Random.NextFloat(1, 3);
            randomPosition.Z = RandomHelper.Random.NextFloat(0, -40);
            var ps = new ParticleSystem
            {
                Name = "MyFirstParticleSystem" + count,
                MaxNumberOfParticles = 200,
                ReferenceFrame = ParticleReferenceFrame.World,
                Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z)),
                Random = Random,
            };
            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));
            ModelNode.Add(contentManager.Load<ModelNode>("HeartHealthbar/HeartHealthbar").Clone());
            _bodyPrototype.Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z - (-1)));
            ModelNode[count].PoseWorld = _bodyPrototype.Pose;
            _simulation = _services.GetInstance<Simulation>();
            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(ModelNode[count]);
            CreateRigidBody(ModelNode[count]);
            var shape = (Shape)ModelNode[count].UserData;
            _geometricObject = new GeometricObject(shape, ModelNode[count].PoseWorld);
            _collisionObject.Add(new CollisionObject(_geometricObject));
            _collisionObject[count].Type = CollisionObjectType.Trigger;
            _collisionObject[count].Enabled = true;
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject[count]);
            IParticleParameter<float> lifetimeParameter = ps.Parameters.AddUniform<float>("Lifetime");
            lifetimeParameter.DefaultValue = 1f;
            ps.Parameters.AddVarying<Vector3F>("Position");
            ps.Effectors.Add(new StartPositionEffector
            {
                Distribution = new SphereDistribution { OuterRadius = 2 }
            });
            ps.Parameters.AddVarying<float>("Alpha");
            ps.Effectors.Add(new SingleFadeEffector
            {
                ValueParameter = "Alpha",
                FadeInStart = 0.0f,
                FadeInEnd = 0.3f,
                FadeOutStart = 0.5f,
                FadeOutEnd = 1.0f,
            });
            IParticleParameter<Texture2D> textureParameter = ps.Parameters.AddUniform<Texture2D>("Texture");
            textureParameter.DefaultValue = contentManager.Load<Texture2D>("Particles/LensFlare");
            IParticleParameter<float> blendModeParameter = ps.Parameters.AddUniform<float>("BlendMode");
            blendModeParameter.DefaultValue = 0.0f;
            return ps;
        }

        private ParticleSystem CreateHealthThree(ContentManager contentManager, int count)
        {
            Vector3F randomPosition;
            randomPosition.X = RandomHelper.Random.NextFloat(0, 5);
            randomPosition.Y = RandomHelper.Random.NextFloat(1, 3);
            randomPosition.Z = RandomHelper.Random.NextFloat(0, -40);
            var ps = new ParticleSystem
            {
                Name = "MyFirstParticleSystem" + count,
                MaxNumberOfParticles = 200,
                ReferenceFrame = ParticleReferenceFrame.World,
                Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z)),
                Random = Random,
            };
            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));
            ModelNode.Add(contentManager.Load<ModelNode>("HeartHealthbar/HeartHealthbar").Clone());
            _bodyPrototype.Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z - (-1)));
            ModelNode[count].PoseWorld = _bodyPrototype.Pose;
            _simulation = _services.GetInstance<Simulation>();
            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(ModelNode[count]);
            CreateRigidBody(ModelNode[count]);
            var shape = (Shape)ModelNode[count].UserData;
            _geometricObject = new GeometricObject(shape, ModelNode[count].PoseWorld);
            _collisionObject.Add(new CollisionObject(_geometricObject));
            _collisionObject[count].Type = CollisionObjectType.Trigger;
            _collisionObject[count].Enabled = true;
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject[count]);
            IParticleParameter<float> lifetimeParameter = ps.Parameters.AddUniform<float>("Lifetime");
            lifetimeParameter.DefaultValue = 1f;
            ps.Parameters.AddVarying<Vector3F>("Position");
            ps.Effectors.Add(new StartPositionEffector
            {
                Distribution = new SphereDistribution { OuterRadius = 2 }
            });
            ps.Parameters.AddVarying<float>("Alpha");
            ps.Effectors.Add(new SingleFadeEffector
            {
                ValueParameter = "Alpha",
                FadeInStart = 0.0f,
                FadeInEnd = 0.3f,
                FadeOutStart = 0.5f,
                FadeOutEnd = 1.0f,
            });
            IParticleParameter<Texture2D> textureParameter = ps.Parameters.AddUniform<Texture2D>("Texture");
            textureParameter.DefaultValue = contentManager.Load<Texture2D>("Particles/LensFlare");
            IParticleParameter<float> blendModeParameter = ps.Parameters.AddUniform<float>("BlendMode");
            blendModeParameter.DefaultValue = 0.0f;
            return ps;
        }

        private ParticleSystem CreateHealthOne(ContentManager contentManager, int count)
        {
            Vector3F randomPosition;
            randomPosition.X = RandomHelper.Random.NextFloat(0, 5);
            randomPosition.Y = RandomHelper.Random.NextFloat(1, 3);
            randomPosition.Z = RandomHelper.Random.NextFloat(0, -40);
            var ps = new ParticleSystem
            {
                Name = "MyFirstParticleSystem" + count,
                MaxNumberOfParticles = 200,
                ReferenceFrame = ParticleReferenceFrame.World,
                Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z)),
                Random = Random,
            };
            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));
            ModelNode.Add(contentManager.Load<ModelNode>("HeartHealthbar/HeartHealthbar").Clone());
            _bodyPrototype.Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z - (-1)));
            ModelNode[count].PoseWorld = _bodyPrototype.Pose;
            _simulation = _services.GetInstance<Simulation>();
            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(ModelNode[count]);
            CreateRigidBody(ModelNode[count]);
            var shape = (Shape)ModelNode[count].UserData;
            _geometricObject = new GeometricObject(shape, ModelNode[count].PoseWorld);
            _collisionObject.Add(new CollisionObject(_geometricObject));
            _collisionObject[count].Type = CollisionObjectType.Trigger;
            _collisionObject[count].Enabled = true;
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject[count]);
            IParticleParameter<float> lifetimeParameter = ps.Parameters.AddUniform<float>("Lifetime");
            lifetimeParameter.DefaultValue = 1f;
            ps.Parameters.AddVarying<Vector3F>("Position");
            ps.Effectors.Add(new StartPositionEffector
            {
                Distribution = new SphereDistribution { OuterRadius = 2 }
            });
            ps.Parameters.AddVarying<float>("Alpha");
            ps.Effectors.Add(new SingleFadeEffector
            {
                ValueParameter = "Alpha",
                FadeInStart = 0.0f,
                FadeInEnd = 0.3f,
                FadeOutStart = 0.5f,
                FadeOutEnd = 1.0f,
            });
            IParticleParameter<Texture2D> textureParameter = ps.Parameters.AddUniform<Texture2D>("Texture");
            textureParameter.DefaultValue = contentManager.Load<Texture2D>("Particles/LensFlare");
            IParticleParameter<float> blendModeParameter = ps.Parameters.AddUniform<float>("BlendMode");
            blendModeParameter.DefaultValue = 0.0f;
            return ps;
        }

        private ParticleSystem CreateHealthTwo(ContentManager contentManager, int count)
        {
            Vector3F randomPosition;
            randomPosition.X = RandomHelper.Random.NextFloat(0, 5);
            randomPosition.Y = RandomHelper.Random.NextFloat(1, 3);
            randomPosition.Z = RandomHelper.Random.NextFloat(0, -40);
            var ps = new ParticleSystem
            {
                Name = "MyFirstParticleSystem" + count,
                MaxNumberOfParticles = 200,
                ReferenceFrame = ParticleReferenceFrame.World,
                Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z)),
                // Optimization tip: Use same random number generator as parent.
                Random = Random,
            };
            _bodyPrototype = new RigidBody(new CapsuleShape(0f, 0.001f));
            ModelNode.Add(contentManager.Load<ModelNode>("HeartHealthbar/HeartHealthbar").Clone());
            _bodyPrototype.Pose = new Pose(new Vector3F(randomPosition.X, randomPosition.Y, randomPosition.Z - (-1)));
            ModelNode[count].PoseWorld = _bodyPrototype.Pose;
            _simulation = _services.GetInstance<Simulation>();
            var scene = _services.GetInstance<IScene>();
            scene.Children.Add(ModelNode[count]);
            CreateRigidBody(ModelNode[count]);
            var shape = (Shape)ModelNode[count].UserData;
            _geometricObject = new GeometricObject(shape, ModelNode[count].PoseWorld);
            // Create a collision object for the game object and add it to the collision domain.
            _collisionObject.Add(new CollisionObject(_geometricObject));
            _collisionObject[count].Type = CollisionObjectType.Trigger;
            _collisionObject[count].Enabled = true;
            // Add the collision object to the collision domain of the game.      
            var collisionDomain = _services.GetInstance<CollisionDomain>();
            collisionDomain.CollisionObjects.Add(_collisionObject[count]);
            // The properties of the particles in the particle system are defined using 
            // "particle parameters" (in the collection _particleSystem.Parameters).
            // Per default, there is only one parameter: "NormalizedAge" - which is managed
            // by the particle system itself and is the age of a particle in the range 0 - 1.

            // All our particles should live for 1 second after they have been created. Therefore,
            // we add a "uniform" parameter called "Lifetime" and set it to 1.
            IParticleParameter<float> lifetimeParameter = ps.Parameters.AddUniform<float>("Lifetime");
            lifetimeParameter.DefaultValue = 1f;
            // Each particle should have a position value. Therefore, we add a "varying" parameter
            // called "Position". "Varying" means that each particle has its own position value.
            // The particle system will internally allocate a Vector3F array to store all particle
            // positions.
            ps.Parameters.AddVarying<Vector3F>("Position");
            // When particles are created, we want them to appear at random position in a spherical
            // volume. We add an effector which initializes the particle "Positions" of newly created
            // particles.
            ps.Effectors.Add(new StartPositionEffector
            {
                // This effector should initialize the "Position" parameter.
                // Parameter = "Position",     // "Position" is the default value anyway.

                // The start values should be chosen from this random value distribution:
                Distribution = new SphereDistribution { OuterRadius = 2 }
            });
            // The particles should slowly fade in and out to avoid sudden appearance and disappearance.
            // We add a varying particle parameter called "Alpha" to store the alpha value per particle.
            ps.Parameters.AddVarying<float>("Alpha");
            // The SingleFadeEffector animates a float parameter from 0 to a target value and
            // back to 0.
            ps.Effectors.Add(new SingleFadeEffector
            {
                // If TargetValueParameter is not set, then the target value is 1.
                //TargetValueParameter = 1,

                // The fade-in/out times are relative to a time parameter. 
                // By default the "NormalizedAge" of the particles is used.
                //TimeParameter = "NormalizedAge",

                // The Alpha value should be animated.
                ValueParameter = "Alpha",

                // The fade-in/out times relative to the normalized age.
                FadeInStart = 0.0f,
                FadeInEnd = 0.3f,
                FadeOutStart = 0.5f,
                FadeOutEnd = 1.0f,
            });
            // Next, we choose a texture for the particles. All particles use the same texture 
            // parameter, which means the parameter is "uniform".
            IParticleParameter<Texture2D> textureParameter = ps.Parameters.AddUniform<Texture2D>("Texture");
            textureParameter.DefaultValue = contentManager.Load<Texture2D>("Particles/LensFlare");
            // The blend mode is a value between 0 and 1, where 0 means additive blending
            // 1 means alpha blending. Values between 0 and 1 are allowed. The particles in
            // this example should be drawn using additive alpha blending. 
            IParticleParameter<float> blendModeParameter = ps.Parameters.AddUniform<float>("BlendMode");
            blendModeParameter.DefaultValue = 0.0f;

            return ps;
        }

        private void CreateRigidBody(ModelNode modelNode)
        {
            var triangleMesh = new TriangleMesh();

            foreach (MeshNode meshNode in modelNode.GetSubtree().OfType<MeshNode>())
            {
                var subTriangleMesh = new TriangleMesh();
                foreach (Submesh submesh in meshNode.Mesh.Submeshes)
                {
                    submesh.ToTriangleMesh(subTriangleMesh);
                }
                subTriangleMesh.Transform(meshNode.PoseWorld * Matrix44F.CreateScale(meshNode.ScaleWorld));
                triangleMesh.Add(subTriangleMesh);
            }


            var triangleMeshShape = new TriangleMeshShape(triangleMesh);
            triangleMeshShape.Partition = new CompressedAabbTree
            {
                BottomUpBuildThreshold = 0,
            };

            _bodyPrototype = new RigidBody(triangleMeshShape, new MassFrame(), null)
            {
                Pose = modelNode.PoseWorld,
                Scale = modelNode.ScaleLocal,
                MotionType = MotionType.Static
            };

            _simulation.RigidBodies.Add(_bodyPrototype);
        }

        protected override void OnUpdate(TimeSpan deltaTime)
        {
            _timeUntilHealthPArticle -= deltaTime;

            if (_timeUntilHealthPArticle <= TimeSpan.Zero)
            {

                if (CountHealth <= 9)
                {
                    if (CountHealth == 0)
                        Children.Add(CreateHealthOne(_contentManager, CountHealth));

                    if (CountHealth == 1)
                        Children.Add(CreateHealthTwo(_contentManager, CountHealth));

                    if (CountHealth == 2)
                        Children.Add(CreateHealthThree(_contentManager, CountHealth));

                    if (CountHealth == 3)
                        Children.Add(CreateHealthFour(_contentManager, CountHealth));

                    if (CountHealth == 4)
                        Children.Add(CreateHealthFive(_contentManager, CountHealth));

                    if (CountHealth == 5)
                        Children.Add(CreateHealthSix(_contentManager, CountHealth));

                    if (CountHealth == 6)
                        Children.Add(CreateHealthSeven(_contentManager, CountHealth));

                    if (CountHealth == 7)
                        Children.Add(CreateHealthEight(_contentManager, CountHealth));

                    if (CountHealth == 8)
                        Children.Add(CreateHealthNine(_contentManager, CountHealth));

                    if (CountHealth == 9)
                        Children.Add(CreateHealthTen(_contentManager, CountHealth));

                    CountHealth++;
                }

                _timeUntilHealthPArticle = HealthParticleInterval;
            }


            foreach (ParticleSystem child in Children)
            {
                child.AddParticles(2);
            }
            foreach (ModelNode t in ModelNode)
            {
                Pose = t.PoseWorld;
            }
            base.OnUpdate(deltaTime);
        }
    }
}