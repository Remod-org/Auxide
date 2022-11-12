using System;
using Auxide;
using Auxide.Scripting;

public abstract class RustScript : IDisposable
{
    // Pointer to an external unmanaged resource.
    //private IntPtr handle;
    // Other managed resource this class uses.
    //private readonly Component component = new Component();
    // Track whether Dispose has been called.
    //private bool disposed = false;
    internal ScriptManager Manager { get; set; }
    public DynamicConfigFile config { get; set; }
    public DataFileSystem data { get; set; }

    private string name;

    public string Title { get; protected set; }

    public string Description { get; protected set; }

    public string Author { get; protected set; }

    public VersionNumber Version { get; protected set; }

    public RustScript()
    {
        Name = GetType().Name;
        Title = Name.Substring(0,1).ToUpper() + Name.Substring(1);
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

    //public LangFileSystem lang { get; set; }

    public virtual void Initialize() { }

    public virtual void Dispose() { }
    //{
    //    Dispose(disposing: true);
    //    GC.SuppressFinalize(this);
    //}

    //protected virtual void Dispose(bool disposing)
    //{
    //    // Check to see if Dispose has already been called.
    //    if (!disposed)
    //    {
    //        // If disposing equals true, dispose all managed
    //        // and unmanaged resources.
    //        if (disposing)
    //        {
    //            // Dispose managed resources.
    //            component.Dispose();
    //        }

    //        // Call the appropriate methods to clean up
    //        // unmanaged resources here.
    //        // If disposing is false,
    //        // only the following code is executed.
    //        CloseHandle(handle);
    //        handle = IntPtr.Zero;

    //        // Note disposing has been done.
    //        disposed = true;
    //    }
    //}

    //[System.Runtime.InteropServices.DllImport("Kernel32")]
    ////private extern static bool CloseHandle(IntPtr handle);
    //private static extern bool CloseHandle(IntPtr handle);

    //~RustScript()
    //{
    //    // Do not re-create Dispose clean-up code here.
    //    // Calling Dispose(disposing: false) is optimal in terms of
    //    // readability and maintainability.
    //    Dispose(disposing: false);
    //}

    protected void Broadcast(string methodName) => Manager?.Broadcast(methodName);

    protected void Broadcast<T0>(string methodName, T0 arg0) => Manager?.Broadcast(methodName, arg0);

    protected void Broadcast<T0, T1>(string methodName, T0 arg0, T1 arg1) => Manager?.Broadcast(methodName, arg0, arg1);

    protected void Broadcast<T0, T1, T2>(string methodName, T0 arg0, T1 arg1, T2 arg2) => Manager?.Broadcast(methodName, arg0, arg1, arg2);

    protected void Broadcast<T0, T1, T2, T3>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3) => Manager?.Broadcast(methodName, arg0, arg1, arg2, arg3);

    protected object BroadcastReturn(string methodName) => Manager?.BroadcastReturn(methodName);

    protected object BroadcastReturn<T0>(string methodName, T0 arg0) => Manager?.BroadcastReturn(methodName, arg0);

    protected object BroadcastReturn<T0, T1>(string methodName, T0 arg0, T1 arg1) => Manager?.BroadcastReturn(methodName, arg0, arg1);

    protected object BroadcastReturn<T0, T1, T2>(string methodName, T0 arg0, T1 arg1, T2 arg2) => Manager?.BroadcastReturn(methodName, arg0, arg1, arg2);
}