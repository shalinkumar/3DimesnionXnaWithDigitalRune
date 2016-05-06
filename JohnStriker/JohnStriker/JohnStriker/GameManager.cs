using System.Linq;
using DigitalRune;
using DigitalRune.Animation;
using DigitalRune.Game;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Graphics;
using DigitalRune.Particles;
using DigitalRune.Physics;
using DigitalRune.ServiceLocation;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace JohnStriker
{
    public abstract class GameManager : GameComponent
    {
        // Services which can be used in derived classes.
        protected readonly ServiceContainer Services;
        protected readonly ContentManager ContentManager;
        protected readonly ContentManager _uiContentManager;
        protected readonly IInputService InputService;
        protected readonly IAnimationService _animationService;
        protected readonly Simulation Simulation;
        private readonly IParticleSystemService _particleSystemService;
        protected IGraphicsService GraphicsService;
        protected readonly IGameObjectService GameObjectService;
        protected readonly IUIService UiService;
        protected readonly SampleFramework SampleFramework;

        private readonly DigitalRune.Graphics.GraphicsScreen[] _originalGraphicsScreens;

        protected readonly IParticleSystemService ParticleSystemService;         

        protected GameManager(Game game)
            : base(game)
        {
            // Get services from the global service container.
            var services = (ServiceContainer)ServiceLocator.Current;
            SampleFramework = services.GetInstance<SampleFramework>();
            ContentManager = services.GetInstance<ContentManager>();
            _uiContentManager = services.GetInstance<ContentManager>("UIContent");
            InputService = services.GetInstance<IInputService>();
            _animationService = services.GetInstance<IAnimationService>();
            Simulation = services.GetInstance<Simulation>();
            _particleSystemService = services.GetInstance<IParticleSystemService>();
            GraphicsService = services.GetInstance<IGraphicsService>();
            GameObjectService = services.GetInstance<IGameObjectService>();
            UiService = services.GetInstance<IUIService>();
            ParticleSystemService = services.GetInstance<IParticleSystemService>();
            // Create a local service container which can be modified in samples:
            // The local service container is a child container, i.e. it inherits the
            // services of the global service container. Samples can add new services
            // or override existing entries without affecting the global services container
            // or other samples.
            Services = services.CreateChildContainer();

            // Store a copy of the original graphics screens.
            _originalGraphicsScreens = GraphicsService.Screens.ToArray();

            // Mouse is visible by default.
            SampleFramework.IsMouseVisible = true;


         
        }       

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // ----- Clean up
                // Remove all game objects.
                GameObjectService.Objects.Clear();

                // Dispose all graphics screens which were added by the sample. We
                // must not remove graphics screens from the menu or help component.
                foreach (var screen in GraphicsService.Screens.ToArray())
                {
                    if (!_originalGraphicsScreens.Contains(screen))
                    {
                        GraphicsService.Screens.Remove(screen);
                        screen.SafeDispose();
                    }
                }

                // Remove all rigid bodies, constraints, force-effects.
                // Restore original simulation settings.
                ((Game1)Game).ResetPhysicsSimulation();

                // Remove all particle systems.
                _particleSystemService.ParticleSystems.Clear();

                // Dispose the local service container.
                Services.Dispose();
            }

            base.Dispose(disposing);
        }
    }

}
