using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace JsonRpcLib.Server
{
    public partial class JsonRpcServer
    {
        private class HandlerInfo
        {
            public object Instance { get; internal set; }
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
                RegisterMethod(handler.GetType().Name, prefix, m, handler);
        }

        public void RegisterHandlers(Type type, string prefix = "")
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (prefix.Any(c => char.IsWhiteSpace(c)))
                throw new ArgumentException("Prefix string can not contain any whitespace");

            foreach (var m in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public))
                RegisterMethod(type.Name, prefix, m);
        }

        private void RegisterMethod(string className, string prefix, MethodInfo m, object instance = null)
        {
            var name = prefix + m.Name;
            if (_handlers.TryGetValue(name, out var existing))
            {
                throw new JsonRpcException($"The method '{name}' is already handled by the class {existing.Instance.GetType().Name}");
            }

            var info = new HandlerInfo {
                Instance = instance,
                Method = m,
                Call = instance != null ? Reflection.CreateDelegate(instance, m) : Reflection.CreateDelegate(m)
            };
            _handlers.TryAdd(name, info);
            Debug.WriteLine($"Added handler for '{name}' on {className}.{m.Name}");
        }

        internal void ExecuteHandler(ClientConnection client, int id, string method, object[] args)
        {
            if (_handlers.TryGetValue(method, out var info))
            {
                try
                {
                    // Make sure arguments are correct for the function call
                    bool hasOptionalParameters = false;
                    if (args != null)
                        PrepareArguments(info.Method, ref args, out hasOptionalParameters);

                    // Now actually do the actual function call on the users class
                    object result = Invoke(args, info, hasOptionalParameters);
                    if (id == -1)
                        return;     // Was a notify, so don't reply

                    // Reply to client
                    if (info.Method.ReturnParameter.ParameterType != typeof(void))
                    {
                        SendResponse(client, id, result);
                    }
                    else
                    {
                        SendResponse(client, id);
                    }
                }
                catch (Exception ex)
                {
                    SendError(client, id, $"Handler '{method}' threw an exception: {ex.Message}");
                }
            }
            else
            {
                SendUnknownMethodError(client, id, method);
            }
        }

        private static void SendError(ClientConnection client, int id, string message)
        {
            var response = new Response() {
                Id = id,
                Error = new Error() { Code = -1, Message = message }
            };
            var json = Serializer.Serialize(response);
            client.Write(json);
        }

        private static void SendUnknownMethodError(ClientConnection client, int id, string method)
        {
            var response = new Response() {
                Id = id,
                Error = new Error() { Code = -32601, Message = $"Unknown method '{method}'" }
            };
            var json = Serializer.Serialize(response);
            client.Write(json);
        }

        private static void SendResponse(ClientConnection client, int id)
        {
            var response = new Response() {
                Id = id
            };
            var json = Serializer.Serialize(response);
            client.Write(json);
        }

        private static void SendResponse(ClientConnection client, int id, object result)
        {
            var response = new Response<object>() {
                Id = id,
                Result = result
            };
            var json = Serializer.Serialize(response);
            client.Write(json);
        }

        private static object Invoke(object[] args, HandlerInfo info, bool hasOptionalParameters)
        {
            object result = null;
            if (hasOptionalParameters)
            {
                // Use reflection invoke instead of delegate because we have optional parameters
                if (info.Instance != null)
                {
                    // Instance function
                    result = info.Method.Invoke(info.Instance,
                        BindingFlags.OptionalParamBinding | BindingFlags.InvokeMethod | BindingFlags.CreateInstance, null, args, null);
                }
                else
                {
                    // Static function
                    result = info.Method.Invoke(null, BindingFlags.OptionalParamBinding | BindingFlags.InvokeMethod, null, args, null);
                }
            }
            else
            {
                result = info.Call.DynamicInvoke(args);
            }

            return result;
        }

        private void PrepareArguments(MethodInfo method, ref object[] args, out bool hasOptionalParameters)
        {
            hasOptionalParameters = false;
            var p = method.GetParameters();
            int neededArgs = p.Count(x => !x.HasDefaultValue);
            if (neededArgs > args.Length)
                throw new JsonRpcException($"Argument count mismatch (Expected at least {neededArgs}, but got only {args.Length}");

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                    continue;  // Skip

                var at = args[i].GetType();
                if (at == p[i].ParameterType)
                    continue;  // Already the right type

                if (at.IsPrimitive)
                {
                    // Cast primitives
                    args[i] = Convert.ChangeType(args[i], p[i].ParameterType);
                }
                else if (at == typeof(string))
                {
                    // Convert string to TimeSpan
                    if (p[i].ParameterType == typeof(TimeSpan))
                        args[i] = TimeSpan.Parse((string)args[i]);
                }
                else if (at == typeof(JArray))
                {
                    // Convert array to a real typed array
                    var a = args[i] as JArray;
                    args[i] = a.ToObject(p[i].ParameterType);
                }
            }

            if (args.Length < p.Length)
            {
                // Missing optional arguments are set to Type.Missing
                args = args.Concat(Enumerable.Repeat(Type.Missing, p.Length - args.Length)).ToArray();
                hasOptionalParameters = true;
            }
        }
    }
}
