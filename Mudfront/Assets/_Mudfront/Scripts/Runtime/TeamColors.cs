using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mudfront
{
    public static class TeamColors
    {
        // matches art/palette/palette.json faction "main" colours
        static readonly Color[] C =
        {
            new Color32(0x3F, 0x6E, 0x9E, 255), // 0 Tidewatch blue
            new Color32(0xB5, 0x45, 0x2E, 255), // 1 Ironclads red
            new Color32(0x7A, 0x8A, 0x3A, 255), // 2 Muddogs green
        };
        public static readonly string[] Names = { "Tidewatch", "Ironclads", "Muddogs" };
        public static Color Get(int team) => team >= 0 && team < C.Length ? C[team] : Color.white;
    }
}
