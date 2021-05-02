using JotunnLib.Entities;
using UnityEngine;

namespace SeedTotem
{
    class DisperseEffectPrefabConfig : PrefabConfig
    {
        public DisperseEffectPrefabConfig() : base("DisperseSeeds", "shaman_heal_aoe")
        {

        }

        public override void Register()
        {
            GameObject particlesEffect = Prefab.transform.Find("flames_world").gameObject;
            ParticleSystem particleSystem = particlesEffect.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule psMain = particleSystem.main;

            SeedTotem.m_disperseEffects.m_effectPrefabs = new EffectList.EffectData[]{
                new EffectList.EffectData()
            {
                m_enabled = true,
                m_prefab = particlesEffect
            }}; 
        }
    }
}
