using System;

namespace Auxide.Scripting
{
    internal static class ScriptInvoker
    {
        private static readonly MruDictionary<InvokeTarget, Action> _procedureCache =
            new MruDictionary<InvokeTarget, Action>(250);

        public static void Procedure(RustScript script, string method)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Action invoker;
            lock (_procedureCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_procedureCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Action>(script, method);
                    _procedureCache.Add(target, invoker);
                }
            }

            invoker?.Invoke();
        }
    }

    internal static class ScriptInvoker<T0>
    {
        private static readonly MruDictionary<InvokeTarget, Action<T0>> _procedureCache =
            new MruDictionary<InvokeTarget, Action<T0>>(100);

        private static readonly MruDictionary<InvokeTarget, Func<T0>> _functionCache =
            new MruDictionary<InvokeTarget, Func<T0>>(250);

        public static void Procedure(RustScript script, string method, T0 arg0)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Action<T0> invoker;
            lock (_procedureCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_procedureCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Action<T0>>(script, method);
                    _procedureCache.Add(target, invoker);
                }
            }

            invoker?.Invoke(arg0);
        }

        public static T0 Function(RustScript script, string method)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Func<T0> invoker;
            lock (_functionCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_functionCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Func<T0>>(script, method);
                    _functionCache.Add(target, invoker);
                }
            }

            return invoker != null ? invoker() : default;
        }
    }

    internal static class ScriptInvoker<T0, T1>
    {
        private static readonly MruDictionary<InvokeTarget, Action<T0, T1>> _procedureCache =
            new MruDictionary<InvokeTarget, Action<T0, T1>>(50);

        private static readonly MruDictionary<InvokeTarget, Func<T0, T1>> _functionCache =
            new MruDictionary<InvokeTarget, Func<T0, T1>>(100);

        public static void Procedure(RustScript script, string method, T0 arg0, T1 arg1)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Action<T0, T1> invoker;
            lock (_procedureCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_procedureCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Action<T0, T1>>(script, method);
                    _procedureCache.Add(target, invoker);
                }
            }

            invoker?.Invoke(arg0, arg1);
        }

        public static T1 Function(RustScript script, string method, T0 arg0)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Func<T0, T1> invoker;
            lock (_functionCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_functionCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Func<T0, T1>>(script, method);
                    _functionCache.Add(target, invoker);
                }
            }

            return invoker != null ? invoker(arg0) : default;
        }
    }

    internal static class ScriptInvoker<T0, T1, T2>
    {
        private static readonly MruDictionary<InvokeTarget, Action<T0, T1, T2>> _procedureCache =
            new MruDictionary<InvokeTarget, Action<T0, T1, T2>>(50);

        private static readonly MruDictionary<InvokeTarget, Func<T0, T1, T2>> _functionCache =
            new MruDictionary<InvokeTarget, Func<T0, T1, T2>>(100);

        public static void Procedure(RustScript script, string method, T0 arg0, T1 arg1, T2 arg2)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Action<T0, T1, T2> invoker;
            lock (_procedureCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_procedureCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Action<T0, T1, T2>>(script, method);
                    _procedureCache.Add(target, invoker);
                }
            }

            invoker?.Invoke(arg0, arg1, arg2);
        }

        public static T2 Function(RustScript script, string method, T0 arg0, T1 arg1)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Func<T0, T1, T2> invoker;
            lock (_functionCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_functionCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Func<T0, T1, T2>>(script, method);
                    _functionCache.Add(target, invoker);
                }
            }

            return invoker != null ? invoker(arg0, arg1) : default;
        }
    }

    internal static class ScriptInvoker<T0, T1, T2, T3>
    {
        private static readonly MruDictionary<InvokeTarget, Action<T0, T1, T2, T3>> _procedureCache =
            new MruDictionary<InvokeTarget, Action<T0, T1, T2, T3>>(50);

        private static readonly MruDictionary<InvokeTarget, Func<T0, T1, T2, T3>> _functionCache =
            new MruDictionary<InvokeTarget, Func<T0, T1, T2, T3>>(100);

        public static void Procedure(RustScript script, string method, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Action<T0, T1, T2, T3> invoker;
            lock (_procedureCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_procedureCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Action<T0, T1, T2, T3>>(script, method);
                    _procedureCache.Add(target, invoker);
                }
            }

            invoker?.Invoke(arg0, arg1, arg2, arg3);
        }

        public static T3 Function(RustScript script, string method, T0 arg0, T1 arg1, T2 arg2)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Func<T0, T1, T2, T3> invoker;
            lock (_functionCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_functionCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Func<T0, T1, T2, T3>>(script, method);
                    _functionCache.Add(target, invoker);
                }
            }

            return invoker != null ? invoker(arg0, arg1, arg2) : default;
        }
    }

    internal static class ScriptInvoker<T0, T1, T2, T3, T4>
    {
        private static readonly MruDictionary<InvokeTarget, Action<T0, T1, T2, T3, T4>> _procedureCache =
            new MruDictionary<InvokeTarget, Action<T0, T1, T2, T3, T4>>(50);

        private static readonly MruDictionary<InvokeTarget, Func<T0, T1, T2, T3, T4>> _functionCache =
            new MruDictionary<InvokeTarget, Func<T0, T1, T2, T3, T4>>(100);

        public static void Procedure(RustScript script, string method, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Action<T0, T1, T2, T3, T4> invoker;
            lock (_procedureCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_procedureCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Action<T0, T1, T2, T3, T4>>(script, method);
                    _procedureCache.Add(target, invoker);
                }
            }

            invoker?.Invoke(arg0, arg1, arg2, arg3, arg4);
        }

        public static T4 Function(RustScript script, string method, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Func<T0, T1, T2, T3, T4> invoker;
            lock (_functionCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_functionCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Func<T0, T1, T2, T3, T4>>(script, method);
                    _functionCache.Add(target, invoker);
                }
            }

            return invoker != null ? invoker(arg0, arg1, arg2, arg3) : default;
        }
    }

    internal static class ScriptInvoker<T0, T1, T2, T3, T4, T5>
    {
        private static readonly MruDictionary<InvokeTarget, Action<T0, T1, T2, T3, T4, T5>> _procedureCache =
            new MruDictionary<InvokeTarget, Action<T0, T1, T2, T3, T4, T5>>(50);

        private static readonly MruDictionary<InvokeTarget, Func<T0, T1, T2, T3, T4, T5>> _functionCache =
            new MruDictionary<InvokeTarget, Func<T0, T1, T2, T3, T4, T5>>(100);

        public static void Procedure(RustScript script, string method, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Action<T0, T1, T2, T3, T4, T5> invoker;
            lock (_procedureCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_procedureCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Action<T0, T1, T2, T3, T4, T5>>(script, method);
                    _procedureCache.Add(target, invoker);
                }
            }

            invoker?.Invoke(arg0, arg1, arg2, arg3, arg4, arg5);
        }

        public static T5 Function(RustScript script, string method, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentNullException(nameof(method));

            Func<T0, T1, T2, T3, T4, T5> invoker;
            lock (_functionCache)
            {
                InvokeTarget target = new InvokeTarget(script, method);
                if (!_functionCache.TryGetValue(target, out invoker))
                {
                    invoker = InvokeBuilder.GetInvoker<Func<T0, T1, T2, T3, T4, T5>>(script, method);
                    _functionCache.Add(target, invoker);
                }
            }

            return invoker != null ? invoker(arg0, arg1, arg2, arg3, arg4) : default;
        }
    }
}
