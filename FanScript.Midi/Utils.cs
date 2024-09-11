using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Midi
{
    internal static class Utils
    {
        public static int InstrumentToFcSound(int instrument)
        {
            const int defaultSound = 6; // piano

            if (instrument < 0)
                return defaultSound;

            switch (instrument)
            {
                case >= 0 and < 8: // Piano Timbres
                    return 6; // piano
                case >= 8 and < 16: // Chromatic Percussion
                    return 15; // clang, ig?
                case >= 16 and < 24: // Organ Timbres
                    return defaultSound;
                case >= 24 and < 32: // Guitar Timbres
                    return defaultSound;
                case >= 32 and < 40: // Bass Timbres
                    return defaultSound;
                case >= 40 and < 48: // String Timbres
                    return defaultSound;
                case >= 48 and < 56: // Ensemble Timbres
                    return defaultSound;
                case >= 56 and < 64: // Brass Timbres
                    return defaultSound;
                case >= 64 and < 72: // Reed Timbres
                    return defaultSound;
                case >= 72 and < 80: // Pipe Timbres
                    return defaultSound;
                case >= 80 and < 88: // Synth Lead
                    return 8; // pad
                case >= 88 and < 96: // Synth Pad
                    return 8; // pad
                case 123: // Bird Tweet
                    return 0; // chirp
                case 127: // Gun Shot
                    return 13; // boom
                default:
                    return defaultSound;
            }
        }
    }
}
