﻿using System;

public interface IScriptReference
{
    /// <summary>
    /// Returns the name of the script.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns true when the script is currently loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Returns the type for the script class, for use with reflection. This will be null if the script is not loaded.
    /// </summary>
    Type ReflectionType { get; }

    /// <summary>
    /// Returns the instance of the script class, for use with reflection. This will be null if the script is not loaded.
    /// </summary>
    object ReflectionInstance { get; }

    void InvokeProcedure(string methodName);
    T0 InvokeFunction<T0>(string methodName);

    void InvokeProcedure<T0>(string methodName, T0 arg0);
    T1 InvokeFunction<T0, T1>(string methodName, T0 arg0);

    void InvokeProcedure<T0, T1>(string methodName, T0 arg0, T1 arg1);
    T2 InvokeFunction<T0, T1, T2>(string methodName, T0 arg0, T1 arg1);

    void InvokeProcedure<T0, T1, T2>(string methodName, T0 arg0, T1 arg1, T2 arg2);
    T3 InvokeFunction<T0, T1, T2, T3>(string methodName, T0 arg0, T1 arg1, T2 arg2);

    void InvokeProcedure<T0, T1, T2, T3>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3);
    T4 InvokeFunction<T0, T1, T2, T3, T4>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3);
}
