using System;

namespace DotMetrics.Monitor.Configuration
{
    public class ProcessInfo
    {
        public string Label { get; }
        public int Pid { get; }

        public ProcessInfo(int pid, string label)
        {
            Label = label;
            Pid = pid;
        }

        protected bool Equals(ProcessInfo other)
        {
            return Label == other.Label && Pid == other.Pid;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProcessInfo)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Label, Pid);
        }
    }
}