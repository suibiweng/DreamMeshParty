using UnityEngine;
using MoonSharp.Interpreter;

namespace LuaIntegration
{
    [MoonSharpUserData]
    public class ParticleSystemProxy
    {
        private ParticleSystem particleSystem;

        public ParticleSystemProxy(ParticleSystem ps)
        {
            this.particleSystem = ps;
        }

        public void Play()
        {
            particleSystem.Play();
        }

        public void Stop()
        {
            particleSystem.Stop();
        }

        public void SetRate(float rate)
        {
            var emission = particleSystem.emission;
            emission.rateOverTime = rate;
        }
    }
}
