using System;
using System.Collections.Generic;
using System.Text;

namespace ModuleReaderManager.HttpServer
{
    class ConsoleLogger: ILogger
    {
        public void Log(object message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}
