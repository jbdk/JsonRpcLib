using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

namespace JsonRpcLib.Server
{
    public static class InvokeHelpers
    {
        private const string TooManyArgsMessage = "Invokes for more than 10 args are not yet implemented";

        private static readonly Type VoidType = typeof(void);

        private static readonly ConcurrentDictionary<Tuple<string, object>, DelegatePair> DelegateMap
            = new ConcurrentDictionary<Tuple<string, object>, DelegatePair>();

        public static object EfficientInvoke(object obj, string methodName, params object[] args)
        {
            var key = Tuple.Create(methodName, obj);
            var delPair = DelegateMap.GetOrAdd(key, CreateDelegate);

            if (delPair.HasReturnValue)
            {
                switch (delPair.ArgumentCount)
                {
                    case 0:
                        return delPair.Delegate();
                    case 1:
                        return delPair.Delegate((dynamic)args[0]);
                    case 2:
                        return delPair.Delegate((dynamic)args[0], (dynamic)args[1]);
                    case 3:
                        return delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2]);
                    case 4:
                        return delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3]);
                    case 5:
                        return delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4]);
                    case 6:
                        return delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5]);
                    case 7:
                        return delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5], (dynamic)args[6]);
                    case 8:
                        return delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5], (dynamic)args[6], (dynamic)args[7]);
                    case 9:
                        return delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5], (dynamic)args[6], (dynamic)args[7], (dynamic)args[8]);
                    case 10:
                        return delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5], (dynamic)args[6], (dynamic)args[7], (dynamic)args[8], (dynamic)args[9]);
                    default:
                        throw new NotImplementedException(TooManyArgsMessage);
                }
            }

            switch (delPair.ArgumentCount)
            {
                case 0:
                    delPair.Delegate();
                    break;
                case 1:
                    delPair.Delegate((dynamic)args[0]);
                    break;
                case 2:
                    delPair.Delegate((dynamic)args[0], (dynamic)args[1]);
                    break;
                case 3:
                    delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2]);
                    break;
                case 4:
                    delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3]);
                    break;
                case 5:
                    delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4]);
                    break;
                case 6:
                    delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5]);
                    break;
                case 7:
                    delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5], (dynamic)args[6]);
                    break;
                case 8:
                    delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5], (dynamic)args[6], (dynamic)args[7]);
                    break;
                case 9:
                    delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5], (dynamic)args[6], (dynamic)args[7], (dynamic)args[8]);
                    break;
                case 10:
                    delPair.Delegate((dynamic)args[0], (dynamic)args[1], (dynamic)args[2], (dynamic)args[3], (dynamic)args[4], (dynamic)args[5], (dynamic)args[6], (dynamic)args[7], (dynamic)args[8], (dynamic)args[9]);
                    break;
                default:
                    throw new NotImplementedException(TooManyArgsMessage);
            }

            return null;
        }

        private static DelegatePair CreateDelegate(Tuple<string, object> key)
        {
            var method = key.Item2
                .GetType()
                .GetMethod(key.Item1);

            var argTypes = method
                .GetParameters()
                .Select(p => p.ParameterType)
                .Concat(new[] { method.ReturnType })
                .ToArray();

            var newDelType = Expression.GetDelegateType(argTypes);
            var newDel = Delegate.CreateDelegate(newDelType, key.Item2, method);

            return new DelegatePair(newDel, argTypes.Length - 1, method.ReturnType != VoidType);
        }

        private class DelegatePair
        {
            public DelegatePair(dynamic del, int argumentCount, bool hasReturnValue)
            {
                Delegate = del;
                ArgumentCount = argumentCount;
                HasReturnValue = hasReturnValue;
            }

            public readonly dynamic Delegate;
            public readonly int ArgumentCount;
            public readonly bool HasReturnValue;
        }
    }
}
