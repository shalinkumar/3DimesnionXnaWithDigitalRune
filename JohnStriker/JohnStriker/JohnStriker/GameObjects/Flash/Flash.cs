using DigitalRune.Geometry;
using DigitalRune.Mathematics.Algebra;
using DigitalRune.Particles;
using DigitalRune.Particles.Effectors;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace JohnStriker.GameObjects.Flash
{
    public class Flash : ParticleSystem
    {
        private readonly Pose _pose;

        public Flash(ContentManager contentManager, Pose pose)
        {
            _pose = pose;
            Children = new ParticleSystemCollection
            {
                CreateFlash(contentManager),
            };
        }


        // Emit a few particles.
        public void Explode()
        {
            Children[0].AddParticles(1);
        }


        // Creates a particle system that display a single particle: a bright billboard 
        // for a "flash" effect.
        private ParticleSystem CreateFlash(ContentManager contentManager)
        {
            var ps = new ParticleSystem
            {
                Name = "Flash",
                MaxNumberOfParticles = 1,
                ReferenceFrame = ParticleReferenceFrame.World,
                Pose = _pose,
                // Optimization tip: Use same random number generator as parent.
            };

            ps.Parameters.AddUniform<float>(ParticleParameterNames.Lifetime).DefaultValue = 0.3f;

            ps.Parameters.AddVarying<Vector3F>(ParticleParameterNames.Position);
            ps.Effectors.Add(new StartPositionEffector
            {
                Parameter = ParticleParameterNames.Position,
                DefaultValue = Vector3F.Zero,
            });

            ps.Parameters.AddVarying<float>(ParticleParameterNames.Size);
            ps.Parameters.AddUniform<float>("StartSize").DefaultValue = 0.0f;
            ps.Parameters.AddUniform<float>("EndSize").DefaultValue = 5.0f;
            ps.Effectors.Add(new SingleLerpEffector
            {
                ValueParameter = ParticleParameterNames.Size,
                FactorParameter = ParticleParameterNames.NormalizedAge,
                StartParameter = "StartSize",
                EndParameter = "EndSize",
            });

            ps.Parameters.AddVarying<float>(ParticleParameterNames.Alpha);
            ps.Parameters.AddUniform<float>("TargetAlpha").DefaultValue = 0.8f;
            ps.Effectors.Add(new SingleFadeEffector
            {
                ValueParameter = ParticleParameterNames.Alpha,
                TargetValueParameter = "TargetAlpha",
                TimeParameter = ParticleParameterNames.NormalizedAge,
                FadeInStart = 0.0f,
                FadeInEnd = 0.2f,
                FadeOutStart = 0.20f,
                FadeOutEnd = 0.40f,
            });

            ps.Parameters.AddUniform<Vector3F>(ParticleParameterNames.Color).DefaultValue =
                new Vector3F(1, 1, 216.0f/255.0f);

            ps.Parameters.AddUniform<Texture2D>(ParticleParameterNames.Texture).DefaultValue =
                contentManager.Load<Texture2D>("Particles/Flash");

            ps.Parameters.AddUniform<float>(ParticleParameterNames.BlendMode).DefaultValue = 0;

            return ps;
        }
    }
}