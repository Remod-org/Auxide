using System;

namespace Auxide
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ChatAttribute : Attribute
    {
        public string[] Commands
        {
            get;
        }

        public ChatAttribute(params string[] commands)
        {
            Commands = commands;
        }
    }
}