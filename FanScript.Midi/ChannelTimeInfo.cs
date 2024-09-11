using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Midi
{
    internal sealed class ChannelTimeInfo
    {
        private readonly TimeSpan frameLength;

        public TimeSpan CurrentTime;
        public TimeSpan FrameTime;
        public int CurrentFrame;

        public ChannelTimeInfo(int frameStep)
        {
            frameLength = TimeSpan.FromSeconds(1d / 60d) * frameStep; // 60 FPS
        }

        public void AddDeltaTime(TimeSpan deltaTime)
        {
            CurrentTime += deltaTime;
        }

        public void StepFrame()
        {
            do
            {
                FrameTime = FrameTime.Add(frameLength);
                CurrentFrame++;
            } while (FrameTime < CurrentTime);
        }
    }
}
