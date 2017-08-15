using System;

namespace SharpRaven.Log4Net.Extra
{
    public class EnvironmentExtra
    {
        public EnvironmentExtra()
        {
            MachineName = Environment.MachineName;
            Version = Environment.Version.ToString();
            OSVersion = Environment.OSVersion.ToString();
        }

        public string MachineName { get; }
        public string Version { get; }
        public string OSVersion { get; }
    }
}