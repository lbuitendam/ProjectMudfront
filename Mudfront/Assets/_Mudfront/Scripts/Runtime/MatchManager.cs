using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mudfront
{
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }
        public int TeamCount = 3;
        public float ScoreToWin = 100f;
        public float[] Scores;
        public int Winner { get; private set; } = -1;

        void Awake()
        {
            Instance = this;
            Scores = new float[TeamCount];
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public static void AddScore(int team, float amount)
        {
            var m = Instance;
            if (m == null || m.Winner >= 0 || team < 0 || team >= m.Scores.Length) return;
            m.Scores[team] += amount;
            if (m.Scores[team] >= m.ScoreToWin) m.Winner = team;
        }
    }
}
