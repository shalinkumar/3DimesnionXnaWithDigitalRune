using System;
using System.Text;
using DigitalRune.Animation;
using DigitalRune.Animation.Easing;
using DigitalRune.Game.Input;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Game.UI.Rendering;
using DigitalRune.Graphics;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Threading;
using JohnStriker.GameObjects.VehicleObject;
using JohnStriker.Sample_Framework;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;



namespace JohnStriker.GameScreen
{
    // Display the start screen.
    // This component loads content in the background.
    // When background loading is finished, the component listens to START buttons 
    // presses. This is necessary to find out which gamepad the user is using.
    // After that, this component is replaced by the MainMenuComponent.
    public class LoadingScreen : GameComponent
    {
        private readonly IServiceLocator _services;
        private readonly IInputService _inputService;
        private readonly IGraphicsService _graphicsService;
        private readonly IUIService _uiService;
        private readonly IAnimationService AnimationService;

        private readonly SampleGraphicsScreen _graphicsScreen;
        private Task _loadStuffTask;
        private UIScreen _uiScreen;

        private StackPanel _fpsPanel;
        private TextBlock _loadingTextBlock;    // Shows the text "Loading...".
        private readonly GuiLoadingScreen _guiLoadingScreen;
        private readonly StringBuilder _stringBuilder = new StringBuilder();


        public LoadingScreen(Microsoft.Xna.Framework.Game game, IServiceLocator services)
            : base(game)
        {
            _services = services;
            _inputService = services.GetInstance<IInputService>();
            _graphicsService = services.GetInstance<IGraphicsService>();
            _uiService = _services.GetInstance<IUIService>();
            AnimationService = services.GetInstance<IAnimationService>();

            // Add a GraphicsScreen to draw some text. In a real game we would draw
            // a spectacular start screen image instead.
            _graphicsScreen = new SampleGraphicsScreen(services);
            _graphicsScreen.ClearBackground = true;
            _graphicsService.Screens.Insert(0, _graphicsScreen);

            // Add the GuiGraphicsScreen to the graphics service.
            _guiLoadingScreen = new GuiLoadingScreen(_services);
            _graphicsService.Screens.Add(_guiLoadingScreen);

            // ----- FPS Counter (top right)
            _fpsPanel = new StackPanel
            {
                Margin = new Vector4F(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _guiLoadingScreen.UIScreen.Children.Add(_fpsPanel);
            _loadingTextBlock = new TextBlock
            {
                Font = "DejaVuSans",
                Name = "LoadingTextBlock",    // Control names are optional - but very helpful for debugging!
                Text = "JohnStriker Achieved With XNA...",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _fpsPanel.Children.Add(_loadingTextBlock);

            // The text should pulse to indicate that a user interaction is required.
            // To achieve this we can animate the opacity of the TextBlock.
            var opacityAnimation = new SingleFromToByAnimation
            {
                From = 1,                             // Animate from opaque (Opacity == 1)
                To = 0.25f,                           // to nearly transparent (Opacity == 0.25)
                Duration = TimeSpan.FromSeconds(0.5), // over a duration of 0.5 seconds.
                EasingFunction = new SineEase { Mode = EasingMode.EaseInOut }
            };

            // A SingleFromToByAnimation plays only once, but the animation should be 
            // played back-and-forth until the user presses a button.
            // We need wrap the SingleFromToByAnimation in an AnimationClip or TimelineClip.
            // Animation clips can be used to cut and loop other animations.
            var loopingOpacityAnimation = new AnimationClip<float>(opacityAnimation)
            {
                LoopBehavior = LoopBehavior.Oscillate,  // Play back-and-forth.
                Duration = TimeSpan.MaxValue            // Loop forever.
            };

            // We want to apply the animation to the "Opacity" property of the TextBlock.
            // All "game object properties" of a UIControl can be made "animatable".      
            // First, get a handle to the "Opacity" property.
            var opacityProperty = _loadingTextBlock.Properties.Get<float>(TextBlock.OpacityPropertyId);

            // Then cast the "Opacity" property to an IAnimatableProperty. 
            var animatableOpacityProperty = opacityProperty.AsAnimatable();

            // Start the pulse animation.
            var animationController = AnimationService.StartAnimation(loopingOpacityAnimation, animatableOpacityProperty);

            // Enable "automatic recycling". This step is optional. It ensures that the
            // associated resources are recycled when either the animation is stopped or
            // the target object (the TextBlock) is garbage collected.
            // (The associated resources will be reused by future animations, which will
            // reduce the number of required memory allocations at runtime.)
            animationController.AutoRecycle();
            // Load stuff in a parallel task.
            _loadStuffTask = Parallel.Start(LoadStuff);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _graphicsService.Screens.Remove(_graphicsScreen);

                _graphicsService.Screens.Remove(_guiLoadingScreen);
            }

            base.Dispose(disposing);
        }


        // Loads stuff. This method is executed in parallel, therefore we can only do 
        // thread-safe things.
        public void LoadStuff()
        {
            // Load a UI theme, which defines the appearance and default values of UI controls.
            var contentManager = _services.GetInstance<ContentManager>();
            var theme = contentManager.Load<Theme>("UI Themes/Neoforce/ThemeRed");

            // Create a UI renderer, which uses the theme info to renderer UI controls.
            UIRenderer renderer = new UIRenderer(Game, theme);

            // Create a UIScreen and add it to the UI service. The screen is the root of the 
            // tree of UI controls. Each screen can have its own renderer. 
            _uiScreen = new UIScreen("SampleUI", renderer)
            {
                // Make the screen transparent.
                //Background = new Color(0, 0, 0, 0),
                Background = Color.Black,
                ZIndex = int.MinValue,
            };

            // Simulate more loading time.
#if NETFX_CORE
      System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(2)).Wait();
#else
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(3));
#endif
        }


        public override void Update(GameTime gameTime)
        {
            var debugRenderer = _graphicsScreen.DebugRenderer2D;
            debugRenderer.Clear();
            //debugRenderer.DrawText("MY GAME", new Vector2F(575, 100), Color.Black);
            //debugRenderer.DrawText(
            //  "This is the start screen.\n\nThis sample shows how to create menus for Xbox games.\nIt must be controlled with a gamepad.",
            //  new Vector2F(375, 200),
            //  Color.Black);

            if (!_loadStuffTask.IsComplete)
            {               
                //debugRenderer.DrawText("Loading...", new Vector2F(575, 400), Color.IndianRed);
                UpdateProfiler();
            }
            else
            {
                if (_uiScreen.UIService == null)
                {
                    // This is the first frame where the LoadStuff() was completed.
                    // Add the UIScreen to the UI service. 
                    _uiService.Screens.Add(_uiScreen);
                }

                //debugRenderer.DrawText("Press START to continue...", new Vector2F(475, 400), Color.IndianRed);

                // Check if the user presses START on any connected gamepad.
                //for (var controller = PlayerIndex.One; controller <= PlayerIndex.Four; controller++)
                //{
                    //if (_inputService.IsPressed(Keys.Space, false))
                    //{
                        // A or START was pressed. Assign this controller to the first "logical player".
                        // If no logical player is assigned, the UI controls will not react to the gamepad.
                        //_inputService.SetLogicalPlayer(LogicalPlayerIndex.One, controller);

                        // Remove this StartScreenComponent. And load the next components.
                        Game.Components.Remove(this);
                        Dispose();
                        Game.Components.Add(new MainMenu(Game, _services));
                    //}
                //}
            }

            base.Update(gameTime);
        }

        private void UpdateProfiler()
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("JohnStriker Achieved With XNA...");
            _loadingTextBlock.Text = _stringBuilder.ToString();

          
        }
    }
}
