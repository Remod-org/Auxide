﻿using Auxide;
using Auxide.Scripting;
using System;

public abstract class RustScript : IDisposable
{
    internal ScriptManager Manager { get; set; }
    public DynamicConfigFile config { get; set; }
    public DataFileSystem data { get; set; }
    public LangFileSystem lang { get; set; }

    private string name;

    public string Title { get; protected set; }

    public string Description { get; protected set; }

    public string Author { get; protected set; }

    public VersionNumber Version { get; protected set; }

    public Timer timer;

    //public Assembly Load(string file)
    //{
    //    return Assembly.LoadFile(file);
    //}

    protected RustScript()
    {
        timer = new Timer();
        Name = GetType().Name;
        //Title = Name.Substring(0,1).ToUpper() + Name.Substring(1);
        Title = Name.Titleize();
        Author = "UNKNOWN";
        Version = new VersionNumber(1, 0, 0);
    }

    public string Name
    {
        get
        {
            return name;
        }
        set
        {
            if (string.IsNullOrEmpty(Name) || name == GetType().Name)
            {
                name = value;
            }
        }
    }

    public virtual void Initialize() { }
    //public virtual void LoadDefaultMessages() { }
    public virtual void Dispose() { }

    public virtual string Lang(string input, params object[] args)
    {
        return string.Format(lang.Get(input), args);
    }

    protected virtual void LoadDefaultConfig()
    {
        CallHook("LoadDefaultConfig", null);
    }

    protected virtual void LoadDefaultMessages()
    {
        CallHook("LoadDefaultMessages", null);
    }

    //protected virtual object Call(string hook, params object[] args)
    //{
    //    return ("hook", this.scr);
    //}

    protected virtual object CallHook(string hook, params object[] args)
    {
        return Manager?.CallHook(hook, args);
    }

    public virtual void Message(BasePlayer player, string input, params object[] args)
    {
        Utils.SendReply(player, string.Format(lang.Get(input), args));
    }

    protected virtual void DoLog(string text) => Utils.DoLog(text, false, false);

    protected void LogWarning(string text) => Utils.DoLog(text, false, true);

    protected void LogToFile(string filename, string text, RustScript plugin, bool timeStamp = true) => Utils.LogToFile(filename, text, plugin, timeStamp);

    protected void NextFrame(Action callback)
    {
        Auxide.Auxide.NextTick(callback);
    }

    protected void NextTick(Action callback)
    {
        Auxide.Auxide.NextTick(callback);
    }

    protected void Broadcast(string methodName) => Manager?.Broadcast(methodName);

    protected void Narrowcast(string methodName, IScriptReference script) => Manager?.Narrowcast(methodName, script);

    protected void Broadcast<T0>(string methodName, T0 arg0) => Manager?.Broadcast(methodName, arg0);

    protected void Broadcast<T0, T1>(string methodName, T0 arg0, T1 arg1) => Manager?.Broadcast(methodName, arg0, arg1);

    protected void Broadcast<T0, T1, T2>(string methodName, T0 arg0, T1 arg1, T2 arg2) => Manager?.Broadcast(methodName, arg0, arg1, arg2);

    protected void Broadcast<T0, T1, T2, T3>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => Manager?.Broadcast(methodName, arg0, arg1, arg2, arg3);

    protected void Broadcast<T0, T1, T2, T3, T4>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Manager?.Broadcast(methodName, arg0, arg1, arg2, arg3, arg4);

    protected object BroadcastReturn(string methodName) => Manager?.BroadcastReturn(methodName);

    protected object NarrowcastReturn(string methodName, IScriptReference script) => Manager?.NarrowcastReturn(methodName, script);

    protected object BroadcastReturn<T0>(string methodName, T0 arg0) => Manager?.BroadcastReturn(methodName, arg0);

    protected object BroadcastReturn<T0, T1>(string methodName, T0 arg0, T1 arg1) => Manager?.BroadcastReturn(methodName, arg0, arg1);

    protected object BroadcastReturn<T0, T1, T2>(string methodName, T0 arg0, T1 arg1, T2 arg2) => Manager?.BroadcastReturn(methodName, arg0, arg1, arg2);

    protected object BroadcastReturn<T0, T1, T2, T3>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => Manager?.BroadcastReturn(methodName, arg0, arg1, arg2, arg3);

    protected object BroadcastReturn<T0, T1, T2, T3, T4>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Manager?.BroadcastReturn(methodName, arg0, arg1, arg2, arg3, arg4);
}