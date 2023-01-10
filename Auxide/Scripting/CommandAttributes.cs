﻿using System;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string[] Commands
    {
        get;
    }

    public CommandAttribute(params string[] commands)
    {
        Commands = commands;
    }
}
