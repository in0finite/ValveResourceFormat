namespace GUI.Types.ParticleRenderer.Operators
{
    class SpinUpdate : ParticleFunctionOperator
    {
        public SpinUpdate(ParticleDefinitionParser parse) : base(parse)
        {
        }

        // This is the only place that will update Rotation based on RotationSpeed
        public override void Operate(ParticleCollection particles, float frameTime, ParticleSystemRenderState particleSystemState)
        {
            foreach (ref var particle in particles.Current)
            {
                var rotationRadians = particle.RotationSpeed * MathF.PI / 180f;
                particle.Rotation += rotationRadians * frameTime;
            }
        }
    }
}
