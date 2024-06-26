﻿using System;

namespace Auxide
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HookMethodAttribute : Attribute
    {
        public string Name
        {
            get;
        }

        public HookMethodAttribute(string name)
        {
            Name = name;
        }
    }
}
