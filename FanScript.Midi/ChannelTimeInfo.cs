namespace FanScript.Midi
{
    internal struct ChannelTimeInfo
    {
        public static readonly TimeSpan FrameLength = TimeSpan.FromSeconds(1d / 60d);

        public TimeSpan CurrentTime;
        public long CurrentFrame;

        private TimeSpan deltaLeftOver;

        public long AddDelta(TimeSpan deltaTime)
        {
            CurrentTime += deltaTime;

            double frameDelta = ((double)deltaTime.Ticks + (double)deltaLeftOver.Ticks) / (double)FrameLength.Ticks;

            deltaLeftOver = new TimeSpan((long)((frameDelta % 1d) * FrameLength.Ticks));

            long wholeFrameDelta = (long)frameDelta;

            CurrentFrame += wholeFrameDelta;

            return wholeFrameDelta;
        }
    }
}
