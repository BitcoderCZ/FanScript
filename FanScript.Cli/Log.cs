using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.Cli
{
    internal static class Log
    {
        public static void Debug(string msg)
            => log(new Message(msg, Level.Debug));

        public static void Info(string msg)
            => log(new Message(msg, Level.Info));

        public static int Error(string msg, ErrorCode errCode, Exception? exception = null)
        {
            Message mes;

            if (exception is null)
                mes = new Message(msg, Level.Error, errCode);
            else
                mes = new Message(msg, errCode, exception);

            log(mes);

            return (int)errCode;
        }

        private static void log(in Message msg)
        {
            StringBuilder builder = new StringBuilder();

            if (msg.ErrCode != ErrorCode.None)
                builder.Append($"Err: {msg.ErrCode}, ErrCode: {(int)msg.ErrCode}, ");

            builder.Append(msg.Value);

            if (msg.Exception is not null)
                builder.Append(" Exception: " + msg.Exception);

            Console.WriteLine($"[{msg.CreatedAt:dd.MM.yy HH:mm:ss}] [{msg.LogLevel, -5}] {builder}");
        }

        public enum Level
        {
            Debug,
            Info,
            Error,
        }

        private readonly struct Message
        {
            public readonly string Value;
            public readonly Level LogLevel;
            public readonly ErrorCode ErrCode;
            public readonly Exception? Exception;

            public readonly DateTime CreatedAt;

            public Message(string value, Level logLevel)
                : this(value, logLevel, ErrorCode.None)
            {
            }
            public Message(string value, Level logLevel, ErrorCode errCode)
            {
                Value = value;
                LogLevel = logLevel;
                ErrCode = errCode;

                CreatedAt = DateTime.UtcNow;
            }
            public Message(string value, ErrorCode errCode, Exception ex)
            {
                ArgumentNullException.ThrowIfNull(ex);

                Value = value;
                LogLevel = Level.Error;
                ErrCode = errCode;
                Exception = ex;

                CreatedAt = DateTime.UtcNow;
            }
        }
    }
}
