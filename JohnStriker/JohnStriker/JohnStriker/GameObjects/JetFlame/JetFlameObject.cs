using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DigitalRune.Game;
using DigitalRune.Geometry;
using DigitalRune.Graphics;
using DigitalRune.Graphics.SceneGraph;
using DigitalRune.Mathematics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Particles;
using JohnStriker.GameObjects.PlayerObjects;
using JohnStriker.GraphicsScreen;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework.Content;

namespace JohnStriker.GameObjects.JetFlame
{
    public class JetFlameObject : GameObject
    {
        private readonly ParticleSystem _jetFlame;
        private readonly ParticleSystemNode _particleSystemNode;
        private readonly MyGraphicsScreen _graphicsScreen;
        private readonly IParticleSystemService ParticleSystemService;
        private readonly IGraphicsService GraphicsService;
        private Player _vehicleGeametric;

        public JetFlameObject(IServiceLocator service, Player vehicleGeametric)
        {
            _vehicleGeametric = vehicleGeametric;

            var contentManager = service.GetInstance<ContentManager>();
            _graphicsScreen = new MyGraphicsScreen(service);


            _jetFlame = JetFlame.Create(contentManager);

            ParticleSystemService = service.GetInstance<IParticleSystemService>();

            GraphicsService = service.GetInstance<IGraphicsService>();

            //_jetFlame.Pose = new Pose(new Vector3F(0, 3, 0), Matrix33F.CreateRotationY(3.1f));
            //_jetFlame.Pose = vehicleGeametric._cameraNode.PoseWorld;
            _jetFlame.Pose = new Pose(new Vector3F(vehicleGeametric.FlamePose.Position.X
                , vehicleGeametric.FlamePose.Position.Y
                , vehicleGeametric.FlamePose.Position.Z), Matrix33F.CreateRotationY(3.1f));
            ParticleSystemService.ParticleSystems.Add(_jetFlame);

            _particleSystemNode = new ParticleSystemNode(_jetFlame);

            var scene = service.GetInstance<IScene>();
            scene.Children.Add(_particleSystemNode);

//            _graphicsScreen.Scene.Children.Add(_particleSystemNode);
        }


        protected override void OnUpdate(TimeSpan timeSpan)
        {
            //_jetFlame.Pose = _vehicleGeametric._cameraNode.PoseWorld;
            _jetFlame.AddParticles(3);
            //_jetFlame.Pose = new Pose(new Vector3F(_vehicleGeametric._cameraNode.PoseLocal.Position.X
            // , _vehicleGeametric._cameraNode.PoseLocal.Position.Y
            // , _vehicleGeametric._cameraNode.PoseLocal.Position.Z), Matrix33F.CreateRotationY(3.1f));
            // Synchronize particles <-> graphics.
            _particleSystemNode.Synchronize(GraphicsService);
        }

    }
}
