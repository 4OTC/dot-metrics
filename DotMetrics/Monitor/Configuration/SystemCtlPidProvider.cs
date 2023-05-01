using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotMetrics.Monitor.ProcessUtil;

namespace DotMetrics.Monitor.Configuration
{
    public class SystemCtlPidProvider : IPidProvider
    {
        private const string SystemctlExecutable = "/bin/systemctl";
        private readonly string[] _targetServiceNames;
        private readonly IProcessExecutor _processExecutor;

        public SystemCtlPidProvider(string[] targetServiceNames, IProcessExecutor processExecutor)
        {
            _targetServiceNames = targetServiceNames;
            _processExecutor = processExecutor;
        }

        public ProcessInfo[] GetMonitoredProcesses()
        {
            var pids = new ProcessInfo[_targetServiceNames.Length];
            for (int index = 0; index < _targetServiceNames.Length; index++)
            {
                string serviceName = _targetServiceNames[index];
                if (!_processExecutor.Execute(
                    SystemctlExecutable,
                    new[] { "status", serviceName },
                    out var stdOut,
                    out var stdErr,
                    out Exception exception))
                {
                    throw new Exception($"Failed to execute {SystemctlExecutable}, stdout: {PrintLines(stdOut)}, " +
                                        $"stderr: {PrintLines(stdErr)}, exception: {exception}");
                }

                if (!ParseMainPid(stdOut, out int pid))
                {
                    throw new Exception($"Could not parse Main PID from stdout: {PrintLines(stdOut)}");
                }

                pids[index] = new ProcessInfo(pid, serviceName);
            }

            return pids;
        }

        private static string PrintLines(List<string> lines)
        {
            return string.Join("\n", lines);
        }

        private static bool ParseMainPid(List<string> stdOut, out int pid)
        {
            string pidLine = stdOut.Find(line => line.Contains("Main PID"));
            if (pidLine != null)
            {
                Match match = new Regex("\\s+Main PID:\\s(\\d+)\\s.*").Match(pidLine);
                if (match.Success)
                {
                    pid = int.Parse(match.Groups[1].Value);
                    return true;
                }
            }

            pid = -1;
            return false;
        }
    }
}