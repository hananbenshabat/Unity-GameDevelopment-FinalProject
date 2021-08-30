using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.FPSController
{
    public class Explosion : MonoBehaviour
    {
        [SerializeField] float m_Force = 15f;
        [SerializeField] float m_Radius = 5f;
        [SerializeField] float m_UpwardsModifier = 1f;
        [SerializeField] float m_ParticleEffectSize = 1f;

        Collider[] m_EffectedColliders;
        Rigidbody m_CurrentRigidbody;

        IEnumerator Start()
        {
            ParticleSystem();

            yield return null;

            // Apply explosion force on next frame if effecting spawned debris is needed
            ExplosionEffect();
        }

        void ParticleSystem()
        {
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem s in particleSystems)
            {
                ParticleSystem.MainModule mainModule = s.main;

                mainModule.startSizeMultiplier *= m_ParticleEffectSize;
                mainModule.startSpeedMultiplier *= m_ParticleEffectSize;

                s.Play();
            }
        }

        void ExplosionEffect()
        {
            m_EffectedColliders = Physics.OverlapSphere(transform.position, m_Radius);

            foreach (Collider c in m_EffectedColliders)
            {
                m_CurrentRigidbody = c.attachedRigidbody;

                if (m_CurrentRigidbody != null)
                {
                    m_CurrentRigidbody.AddExplosionForce(m_Force, transform.position, m_Radius, m_UpwardsModifier, ForceMode.Impulse);
                }
            }
        }
    }
}