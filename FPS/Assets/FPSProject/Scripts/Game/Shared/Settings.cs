using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class Settings : MonoBehaviour
    {
        public bool m_IsInfiniteAmmo, m_IsInfiniteGrenade;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}