using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Midi
{
    public struct ChannelFrame
    {
        public const byte ByteUsedFlag = 0b_1000_0000;

        public bool StopCurrentNote;

        public bool StartNewNote => (newNote & ByteUsedFlag) == ByteUsedFlag;
        private byte newNote;
        public byte NewNote
        {
            get => (byte)(newNote ^ ByteUsedFlag);
            set => newNote = (byte)(value | ByteUsedFlag);
        }

        public bool SetNewSound => (newSound & ByteUsedFlag) == ByteUsedFlag;
        private byte newSound;
        public byte NewSound
        {
            get => (byte)(newSound ^ ByteUsedFlag);
            set => newSound = (byte)(value | ByteUsedFlag);
        }

        public ChannelFrame(bool stopCurrentNote, byte newNote, byte newSound)
        {
            StopCurrentNote = stopCurrentNote;
            NewNote = newNote;
            NewSound = newSound;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append('{');
            if (StopCurrentNote)
            {
                builder.Append("Stop current note ");
            }
            if (StartNewNote)
            {
                builder.Append("Start note: ");
                builder.Append(NewNote);
                builder.Append(" ");
            }
            if (SetNewSound)
            {
                builder.Append("Set new sound: ");
                builder.Append(NewSound);
                builder.Append(" ");
            }
            builder.Append('}');

            return builder.ToString();
        }
    }
}
