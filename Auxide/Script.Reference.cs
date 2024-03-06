using Auxide.Exceptions;
using Auxide.Scripting;
using System;

namespace Auxide
{
    internal partial class Script : IScriptReference
    {
        public bool IsLoaded => Instance != null;
        public Type ReflectionType => Instance?.GetType();
        public object ReflectionInstance => Instance;
        public void InvokeProcedure(string methodName)
        {
            if (Instance == null)
            {
                return;
            }
            if (Instance.GetType().GetMethod(methodName) == null)
            {
                return;
            }

            try
            {
                ScriptInvoker.Procedure(Instance, methodName);
            }
            catch (Exception e)
            {
                ReportError($"InvokeProcedure('{methodName}')", e);
            }
        }

        public T0 InvokeFunction<T0>(string methodName)
        {
            if (Instance == null)
            {
                throw new ScriptInvocationException($"Script '{Name}' is not initialized.");
            }

            try
            {
                return ScriptInvoker<T0>.Function(Instance, methodName);
            }
            catch (Exception e)
            {
                throw new ScriptInvocationException($"Function '{Name}::{methodName}()' threw an exception.", e);
            }
        }

        public void InvokeProcedure<T0>(string methodName, T0 arg0)
        {
            if (Instance == null)
            {
                return;
            }

            try
            {
                ScriptInvoker<T0>.Procedure(Instance, methodName, arg0);
            }
            catch (Exception e)
            {
                ReportError($"InvokeProcedure('{methodName}', {typeof(T0).FullName})", e);
            }
        }

        public T1 InvokeFunction<T0, T1>(string methodName, T0 arg0)
        {
            if (Instance == null)
            {
                throw new ScriptInvocationException($"Script '{Name}' is not initialized.");
            }

            try
            {
                return ScriptInvoker<T0, T1>.Function(Instance, methodName, arg0);
            }
            catch (Exception e)
            {
                throw new ScriptInvocationException($"Function '{Name}::{methodName}({typeof(T0).FullName})' threw an exception.", e);
            }
        }

        public void InvokeProcedure<T0, T1>(string methodName, T0 arg0, T1 arg1)
        {
            if (Instance == null)
            {
                return;
            }

            try
            {
                ScriptInvoker<T0, T1>.Procedure(Instance, methodName, arg0, arg1);
            }
            catch (Exception e)
            {
                ReportError($"InvokeProcedure('{methodName}', {typeof(T0).FullName}, {typeof(T1).FullName})", e);
            }
        }

        public T2 InvokeFunction<T0, T1, T2>(string methodName, T0 arg0, T1 arg1)
        {
            if (Instance == null)
            {
                throw new ScriptInvocationException($"Script '{Name}' is not initialized.");
            }

            try
            {
                return ScriptInvoker<T0, T1, T2>.Function(Instance, methodName, arg0, arg1);
            }
            catch (Exception e)
            {
                throw new ScriptInvocationException($"Function '{Name}::{methodName}({typeof(T0).FullName}, {typeof(T1).FullName})' threw an exception.", e);
            }
        }

        public void InvokeProcedure<T0, T1, T2>(string methodName, T0 arg0, T1 arg1, T2 arg2)
        {
            if (Instance == null)
            {
                return;
            }

            try
            {
                ScriptInvoker<T0, T1, T2>.Procedure(Instance, methodName, arg0, arg1, arg2);
            }
            catch (Exception e)
            {
                ReportError($"InvokeProcedure('{methodName}', {typeof(T0).FullName}, {typeof(T1).FullName})", e);
            }
        }

        public T3 InvokeFunction<T0, T1, T2, T3>(string methodName, T0 arg0, T1 arg1, T2 arg2)
        {
            if (Instance == null)
            {
                throw new ScriptInvocationException($"Script '{Name}' is not initialized.");
            }

            try
            {
                return ScriptInvoker<T0, T1, T2, T3>.Function(Instance, methodName, arg0, arg1, arg2);
            }
            catch (Exception e)
            {
                throw new ScriptInvocationException($"Function '{Name}::{methodName}({typeof(T0).FullName}, {typeof(T1).FullName}, {typeof(T2).FullName})' threw an exception.", e);
            }
        }

        public void InvokeProcedure<T0, T1, T2, T3>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            if (Instance == null)
            {
                throw new ScriptInvocationException($"Script '{Name}' is not initialized.");
            }

            try
            {
                ScriptInvoker<T0, T1, T2, T3>.Procedure(Instance, methodName, arg0, arg1, arg2, arg3);
            }
            catch (Exception e)
            {
                ReportError($"InvokeProcedure('{methodName}', {typeof(T0).FullName}, {typeof(T1).FullName}, {typeof(T2).FullName})", e);
            }
        }

        public T4 InvokeFunction<T0, T1, T2, T3, T4>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            if (Instance == null)
            {
                throw new ScriptInvocationException($"Script '{Name}' is not initialized.");
            }

            try
            {
                return ScriptInvoker<T0, T1, T2, T3, T4>.Function(Instance, methodName, arg0, arg1, arg2, arg3);
            }
            catch (Exception e)
            {
                throw new ScriptInvocationException($"Function '{Name}::{methodName}({typeof(T0).FullName}, {typeof(T1).FullName}, {typeof(T2).FullName}, {typeof(T3).FullName})' threw an exception.", e);
            }
        }

        public void InvokeProcedure<T0, T1, T2, T3, T4>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (Instance == null)
            {
                throw new ScriptInvocationException($"Script '{Name}' is not initialized.");
            }

            try
            {
                ScriptInvoker<T0, T1, T2, T3, T4>.Procedure(Instance, methodName, arg0, arg1, arg2, arg3, arg4);
            }
            catch (Exception e)
            {
                ReportError($"InvokeProcedure('{methodName}', {typeof(T0).FullName}, {typeof(T1).FullName}, {typeof(T2).FullName})", e);
            }
        }

        public T5 InvokeFunction<T0, T1, T2, T3, T4, T5>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (Instance == null)
            {
                throw new ScriptInvocationException($"Script '{Name}' is not initialized.");
            }

            try
            {
                return ScriptInvoker<T0, T1, T2, T3, T4, T5>.Function(Instance, methodName, arg0, arg1, arg2, arg3, arg4);
            }
            catch (Exception e)
            {
                throw new ScriptInvocationException($"Function '{Name}::{methodName}({typeof(T0).FullName}, {typeof(T1).FullName}, {typeof(T2).FullName}, {typeof(T3).FullName}, {typeof(T4).FullName})' threw an exception.", e);
            }
        }

        public void InvokeProcedure<T0, T1, T2, T3, T4, T5>(string methodName, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (Instance == null)
            {
                throw new ScriptInvocationException($"Script '{Name}' is not initialized.");
            }

            try
            {
                ScriptInvoker<T0, T1, T2, T3, T4, T5>.Procedure(Instance, methodName, arg0, arg1, arg2, arg3, arg4, arg5);
            }
            catch (Exception e)
            {
                throw new ScriptInvocationException($"Function '{Name}::{methodName}({typeof(T0).FullName}, {typeof(T1).FullName}, {typeof(T2).FullName}, {typeof(T3).FullName}, {typeof(T4).FullName}, {typeof(T5).FullName})' threw an exception.", e);
            }
        }
    }
}
