using System;
using System.Linq;
using System.Reflection;

namespace Auxide.Scripting
{
    internal static class InvokeBuilder
    {
        public static T GetInvoker<T>(RustScript instance, string methodName) where T : Delegate
        {
            Type objectType = instance.GetType();
            Type delegateType = typeof(T);

            MethodInfo signature = delegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance);
            if (signature == null)
            {
                return null;
            }

            Type[] parameterTypes = signature.GetParameters().Select(p => p.ParameterType).ToArray();
            MethodInfo method = objectType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, parameterTypes, null);
            if (method == null || method.ReturnType != signature.ReturnType || method.DeclaringType == typeof(RustScript))
            {
                return null;
            }

            return (T)method.CreateDelegate(delegateType, instance);
        }
    }
}
