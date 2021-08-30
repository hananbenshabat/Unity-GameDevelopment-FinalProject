using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    public class GameStatus : MonoBehaviour
    {
        // -1 - Didn't start, 0 - Ongoing, 1 - Lose, 2 - Win
        public int Status = -1;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }

}
