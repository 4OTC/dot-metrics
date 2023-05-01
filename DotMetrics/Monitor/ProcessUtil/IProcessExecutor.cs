using System;
using System.Collections.Generic;

namespace DotMetrics.Monitor.ProcessUtil
{
    public interface IProcessExecutor
    {
        bool Execute(string executable, string[] args, out List<string> standardOutput, out List<string> standardError, out Exception exception);
    }
}