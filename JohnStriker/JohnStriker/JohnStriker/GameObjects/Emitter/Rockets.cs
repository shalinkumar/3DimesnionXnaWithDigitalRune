using DigitalRune.Game;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Mathematics.Statistics;
using DigitalRune.Particles;
using DigitalRune.Particles.Effectors;
using Microsoft.Practices.ServiceLocation;


namespace JohnStriker.GameObjects.Emitter
{
  // A particle system that simulates rocket paths. A custom RocketEffector is used
  // to create child particle systems for explosions and rocket trails.
  public class Rockets : ParticleSystem
  {

      private readonly IGameObjectService _gameObjectService;
      public Rockets(IServiceLocator services)
    {
      Name = "Rockets";
      MaxNumberOfParticles = 1;

      // The RocketEffector will add child particle systems.
      Children = new ParticleSystemCollection();

      Parameters.AddUniform<float>(ParticleParameterNames.Lifetime).DefaultValue = 2;

      Effectors.Add(new StreamEmitter
      {
        DefaultEmissionRate = 2,
      });

      Parameters.AddVarying<Vector3F>(ParticleParameterNames.Position);
      Effectors.Add(new StartPositionEffector
      {
        Parameter = ParticleParameterNames.Position,
        Distribution = new BoxDistribution { MinValue = new Vector3F(-5, 0, -5), MaxValue = new Vector3F(5, 0, 0) },
      });

      Parameters.AddVarying<Vector3F>(ParticleParameterNames.Direction);
      Effectors.Add(new StartDirectionEffector
      {
        Parameter = ParticleParameterNames.Direction,
        Distribution = new DirectionDistribution { Deviation = 0.5f, Direction = Vector3F.UnitY },
      });

      Parameters.AddVarying<float>(ParticleParameterNames.LinearSpeed);
      Effectors.Add(new StartValueEffector<float>
      {
        Parameter = ParticleParameterNames.LinearSpeed,
        Distribution = new UniformDistributionF(2, 6),
      });

      Effectors.Add(new LinearVelocityEffector());

      Parameters.AddUniform<Vector3F>(ParticleParameterNames.LinearAcceleration).DefaultValue = new Vector3F(0, -2f, 0);
      Effectors.Add(new LinearAccelerationEffector());

      Parameters.AddUniform<float>(ParticleParameterNames.Alpha).DefaultValue = 0;

      // The RocketEffector creates and controls nested particle systems for the rocket trails
      // and explosions.
   
      Effectors.Add(new RocketEffector(services));

//      ParticleSystemValidator.Validate(this);
    }
  }
}
