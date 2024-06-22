using System;

namespace Auxide
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class PluginReferenceAttribute : Attribute
    {
        public string[] Names
        {
            get;
        }

        public PluginReferenceAttribute()
        {
        }

        public PluginReferenceAttribute(string[] names)
        {
            Names = names;
        }
    }
}