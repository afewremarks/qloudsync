using System;

namespace GreenQloud
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException (string Message) : base(Message)
        {
            Logger.LogInfo ("ConfigurationException", this.Message);
        }
    }
}

