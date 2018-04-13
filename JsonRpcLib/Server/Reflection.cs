using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace JsonRpcLib.Server
{
    internal static class Reflection
    {
        public static Delegate CreateDelegate(object instance, MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (method.IsGenericMethod)
                throw new ArgumentException("The provided method must not be generic.", nameof(method));

            var parameters = method.GetParameters()
                .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                .ToArray();
            var call = Expression.Call(Expression.Constant(instance), method, parameters);
            return Expression.Lambda(call, parameters).Compile();
        }

        public static Delegate CreateDelegate(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (method.IsGenericMethod)
                throw new ArgumentException("The provided method must not be generic.", nameof(method));
            if (!method.IsStatic)
                throw new ArgumentException("The provided method must be static.", nameof(method));

            var parameters = method.GetParameters()
                .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                .ToArray();
            var call = Expression.Call(null, method, parameters);
            return Expression.Lambda(call, parameters).Compile();
        }
    }
}
