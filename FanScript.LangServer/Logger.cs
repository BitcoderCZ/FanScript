﻿using LanguageServer.Client;
using LanguageServer.Parameters.Window;
using LanguageServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanScript.LangServer
{
    public class Logger
    {
        public static Logger Instance { get; } = new Logger();

        private Proxy? _proxy;

        public void Attach(Connection connection)
        {
            if (connection == null)
                _proxy = null;
            else
                _proxy = new Proxy(connection);
        }

        public void Error(string message) => Send(MessageType.Error, message);
        public void Warn(string message) => Send(MessageType.Warning, message);
        public void Info(string message) => Send(MessageType.Info, message);
        public void Log(string message) => Send(MessageType.Log, message);

        private void Send(MessageType type, string message)
        {
            _proxy?.Window.LogMessage(new LogMessageParams
            {
                type = type,
                message = message
            });
        }
    }
}
