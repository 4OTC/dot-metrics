using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DotMetrics.Monitor.ProcessUtil
{
    public class ProcessExecutor : IProcessExecutor
    {
        private const int ExitCodeSuccess = 0;

        public bool Execute(
            string executable,
            string[] args,
            out List<string> standardOutput,
            out List<string> standardError,
            out Exception exception)
        {
            standardOutput = new List<string>();
            standardError = new List<string>();
            using Process process = new();
            try
            {
                foreach (string arg in args)
                {
                    process.StartInfo.ArgumentList.Add(arg);
                }
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = executable;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                bool started = process.Start();
                process.WaitForExit();
                CopyOutput(process.StandardOutput, standardOutput);
                CopyOutput(process.StandardOutput, standardError);
                exception = null;
                return process.ExitCode == ExitCodeSuccess;
            }
            catch (Exception e)
            {
                exception = e;
                return false;
            }
        }

        private static void CopyOutput(StreamReader streamReader, List<string> container)
        {
            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                container.Add(line);
            }
        }
    }
}