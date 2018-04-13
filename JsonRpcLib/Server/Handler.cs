using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace JsonRpcLib.Server
{
    public partial class JsonRpcServer
    {
        private class HandlerInfo
        {
            public object Object { get; internal set; }
            public MethodInfo Method { get; internal set; }
            public Delegate Call { get; internal set; }
        }

        readonly ConcurrentDictionary<string, HandlerInfo> _handlers = new ConcurrentDictionary<string, HandlerInfo>();

        public void RegisterHandlers(object handler, string prefix = "")
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (prefix.Any(c => char.IsWhiteSpace(c)))
                throw new ArgumentException("Prefix string can not contain any whitespace");

            foreach (var m in handler.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
            {
                var name = prefix + m.Name;
                if (_handlers.TryGetValue(name, out var existing))
                {
                    throw new JsonRpcException($"The method '{name}' is already handled by the class {existing.Object.GetType().Name}");
                }

                var info = new HandlerInfo {
                    Object = handler,
                    Method = m,
                    Call = Reflection.CreateMethod(handler, m)
                };
                _handlers.TryAdd(name, info);
                Debug.WriteLine($"Added handler for '{name}' as {handler.GetType().Name}.{m.Name}");
            }
        }

        public void RegisterHandlers(Type type, string prefix = "")
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (prefix.Any(c => char.IsWhiteSpace(c)))
                throw new ArgumentException("Prefix string can not contain any whitespace");

            foreach (var m in type.GetMethods())
            {
                var name = prefix + m.Name;
                if (_handlers.TryGetValue(name, out var existing))
                {
                    throw new JsonRpcException($"The method '{name}' is already handled by the class {existing.Object.GetType().Name}");
                }

                var info = new HandlerInfo {
                    Method = m,
                    Call = Reflection.CreateMethod(m)
                };
                _handlers.TryAdd(name, info);
                Debug.WriteLine($"Added handler for '{name}' as static {type.Name}.{m.Name}");
            }
        }

        internal void ExecuteHandler(ClientConnection client, int id, string method, object[] args)
        {
            if (_handlers.TryGetValue(method, out var info))
            {
                try
                {
                    FixupArgs(info.Method, ref args, out bool notAllArgsAreThere);

                    object result = null;
                    if (notAllArgsAreThere)
                    {
#if !NET40
                        // We will never get here on NET40
                        result = info.Method.Invoke(info.Object,
                            BindingFlags.OptionalParamBinding | BindingFlags.InvokeMethod | BindingFlags.CreateInstance,
                            null, args, null);
#endif
                    }
                    else
                    {
                        result = info.Call.DynamicInvoke(args);
                    }

                    if (info.Method.ReturnParameter.ParameterType != typeof(void))
                    {
                        var response = new Response<object>() {
                            Id = id,
                            Result = result
                        };
                        var json = Serializer.Serialize(response);
                        client.Write(json);
                    }
                    else
                    {
                        var response = new Response() {
                            Id = id
                        };
                        var json = Serializer.Serialize(response);
                        client.Write(json);
                    }
                }
                catch (Exception ex)
                {
                    var response = new Response() {
                        Id = id,
                        Error = new Error() { Code = -1, Message = $"Handler '{method}' threw an exception: {ex.Message}" }
                    };
                    var json = Serializer.Serialize(response);
                    client.Write(json);
                }
            }
            else
            {
                //
                // Unknown method
                //
                var response = new Response() {
                    Id = id,
                    Error = new Error() { Code = -32601, Message = $"Unknown method '{method}'" }
                };
                var json = Serializer.Serialize(response);
                client.Write(json);
            }
        }

        private void FixupArgs(MethodInfo method, ref object[] args, out bool notAllArgsAreThere)
        {
            notAllArgsAreThere = false;
            var p = method.GetParameters();
#if NET40
            int neededArgs = p.Length;
#else
            int neededArgs = p.Count(x => !x.HasDefaultValue);
#endif
            if (neededArgs > args.Length)
                throw new JsonRpcException($"Argument count mismatch (Expected at least {neededArgs}, but got only {args.Length}");

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                    continue;
                var at = args[i].GetType();
                if (at == p[i].ParameterType)
                    continue;

                if (at.IsPrimitive)
                    args[i] = Convert.ChangeType(args[i], p[i].ParameterType);
            }

            if (args.Length < p.Length)
            {
                args = args.Concat(Enumerable.Repeat(Type.Missing, p.Length - args.Length)).ToArray();
                notAllArgsAreThere = true;
            }
        }
    }
}
