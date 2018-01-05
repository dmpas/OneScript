﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptEngine.Machine.Contexts
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ContextMethodAttribute : Attribute
    {
        private readonly string _name;
        private readonly string _alias;

        public ContextMethodAttribute(string name, string alias = null)
        {
            if(!Utils.IsValidIdentifier(name))
                throw new ArgumentException("Name must be a valid identifier");

            if(!string.IsNullOrEmpty(alias) && !Utils.IsValidIdentifier(alias))
                throw new ArgumentException("Alias must be a valid identifier");

            _name = name;
            _alias = alias;
        }

        public string GetName()
        {
            return _name;
        }

        public string GetAlias()
        {
            return _alias;
        }
        
        public string GetAlias(string nativeMethodName)
        {
            if (!string.IsNullOrEmpty(_alias))
            {
                return _alias;
            }
            if (!IsDeprecated)
            {
                return nativeMethodName;
            }
            return null;
        }
        
        public bool IsDeprecated { get; set; }

        public bool ThrowOnUse { get; set; }

        public bool IsFunction { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class ByRefAttribute : Attribute
    {
    }

    public delegate IValue ContextCallableDelegate<TInstance>(TInstance instance, IValue[] args);

    public class ContextMethodsMapper<TInstance>
    {
        private List<InternalMethInfo> _methodPtrs;
        
        private void Init()
        {
            if (_methodPtrs == null)
            {
                lock (this)
                {
                    if (_methodPtrs == null)
                    {
                        _methodPtrs = new List<InternalMethInfo>();
                        MapType(typeof(TInstance));
                    }
                }
            }
        }

        public ContextCallableDelegate<TInstance> GetMethod(int number)
        {
            Init();
            return _methodPtrs[number].method;
        }

        public ScriptEngine.Machine.MethodInfo GetMethodInfo(int number)
        {
            Init();
            return _methodPtrs[number].methodInfo;
        }

        public IEnumerable<MethodInfo> GetMethods()
        {
            Init();
            return _methodPtrs.Select(x => x.methodInfo);
        }

        public int FindMethod(string name)
        {
            Init();

            // поскольку этот метод вызывается довольно часто, то тут
            // возможна некоторая просадка по производительности 
            // за счет сравнения IgnoreCase вместо обычного "числового" сравнения
            // Надо будет понаблюдать или вообще замерить
            //
            var idx = _methodPtrs.FindIndex(x => 
                String.Compare(x.methodInfo.Name, name, StringComparison.OrdinalIgnoreCase) == 0 
                || String.Compare(x.methodInfo.Alias, name, StringComparison.OrdinalIgnoreCase) == 0 );
            if (idx < 0)
            {
                throw RuntimeException.MethodNotFoundException(name);
            }

            return idx;
        }

        public int Count
        {
            get
            {
                Init();
                return _methodPtrs.Count;
            }
        }

        private void MapType(Type type)
        {
            var methods = type.GetMethods()
                .SelectMany(method => method.GetCustomAttributes(typeof(ContextMethodAttribute), false)
                    .Select(attr => new {
                        Method = method,
                        Binding = (ContextMethodAttribute) attr
                    })
                );
            
            foreach (var item in methods)
            {
                const int MAX_ARG_SUPPORTED = 8;
                var parameters = item.Method.GetParameters();
                var paramTypes = parameters.Select(x=>x.ParameterType).ToList();
                var isFunc = item.Method.ReturnType != typeof(void);
                if (isFunc)
                {
                    paramTypes.Add(item.Method.ReturnType);
                }
                var argNum = paramTypes.Count;
                
                if (argNum <= MAX_ARG_SUPPORTED)
                {
                    var action = ResolveGeneric(argNum, paramTypes.ToArray(), isFunc);
                    var methPtr = (ContextCallableDelegate<TInstance>)action.Invoke(this, new object[] { item.Method });

                    if (isFunc)
                        argNum--;

                    var paramDefs = new ParameterDefinition[argNum];
                    for (int i = 0; i < argNum; i++)
                    {
                        var pd = new ParameterDefinition();
                        if (parameters[i].GetCustomAttributes(typeof(ByRefAttribute), false).Length != 0)
                        {
                            if (paramTypes[i] != typeof(IVariable))
                            {
                                throw new InvalidOperationException("Attribute ByRef can be applied only on IVariable parameters");
                            }
                            pd.IsByValue = false;
                        }
                        else
                        {
                            pd.IsByValue = true;
                        }

                        if (parameters[i].IsOptional)
                        {
                            pd.HasDefaultValue = true;
                            pd.DefaultValueIndex = ParameterDefinition.UNDEFINED_VALUE_INDEX;
                        }
                        
                        paramDefs[i] = pd;

                    }

                    var scriptMethInfo = new ScriptEngine.Machine.MethodInfo();
                    scriptMethInfo.IsFunction = isFunc;
                    scriptMethInfo.IsDeprecated = item.Binding.IsDeprecated;
                    scriptMethInfo.ThrowOnUseDeprecated = item.Binding.ThrowOnUse;
                    scriptMethInfo.Name = item.Binding.GetName();
                    scriptMethInfo.Alias = item.Binding.GetAlias(item.Method.Name);

                    scriptMethInfo.Params = paramDefs;

                    _methodPtrs.Add(new InternalMethInfo()
                    {
                        method = methPtr,
                        methodInfo = scriptMethInfo
                    });

                }
                else
                    throw new NotSupportedException(string.Format("Only {0} parameters supported", MAX_ARG_SUPPORTED));
                
            }

        }

        private System.Reflection.MethodInfo ResolveGeneric(int argNum, Type[] typeArgs, bool asFunc)
        {
            string methName = asFunc ? "CreateFunction" : "CreateAction";
            
            var method = this.GetType().GetMembers(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Where(x => x.MemberType == System.Reflection.MemberTypes.Method && x.Name == methName)
                    .Select(x => (System.Reflection.MethodInfo)x)
                    .Where(x => x.GetGenericArguments().Length == argNum)
                    .First();

            if (argNum > 0)
                return method.MakeGenericMethod(typeArgs);
            else
                return method;

        }

        private ContextCallableDelegate<TInstance> CreateAction(System.Reflection.MethodInfo target)
        {
            var method = (Action<TInstance>)Delegate.CreateDelegate(typeof(Action<TInstance>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) => 
                {
                    method(inst);
                    return null;
                });
                
        }

        private ContextCallableDelegate<TInstance> CreateAction<T>(System.Reflection.MethodInfo target)
        {
            var method = (Action<TInstance, T>)Delegate.CreateDelegate(typeof(Action<TInstance, T>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) => 
                {
                    method(inst, ConvertParam<T>(args[0]));
                    return null;
                });
        }

        private ContextCallableDelegate<TInstance> CreateAction<T1, T2>(System.Reflection.MethodInfo target)
        {
            var method = (Action<TInstance, T1, T2>)Delegate.CreateDelegate(typeof(Action<TInstance, T1, T2>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                method(inst, ConvertParam<T1>(args[0]), ConvertParam<T2>(args[1]));
                return null;
            });
        }

        private ContextCallableDelegate<TInstance> CreateAction<T1, T2, T3>(System.Reflection.MethodInfo target)
        {
            var method = (Action<TInstance, T1, T2, T3>)Delegate.CreateDelegate(typeof(Action<TInstance, T1, T2, T3>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                method(inst, ConvertParam<T1>(args[0]), ConvertParam<T2>(args[1]), ConvertParam<T3>(args[2]));
                return null;
            });
        }

        private ContextCallableDelegate<TInstance> CreateAction<T1, T2, T3, T4>(System.Reflection.MethodInfo target)
        {
            var method = (Action<TInstance, T1,T2,T3,T4>)Delegate.CreateDelegate(typeof(Action<TInstance, T1,T2,T3,T4>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                method(inst, ConvertParam<T1>(args[0]), ConvertParam<T2>(args[1]), ConvertParam<T3>(args[2]), ConvertParam<T4>(args[3]));
                return null;
            });
        }

        private ContextCallableDelegate<TInstance> CreateAction<T1, T2, T3, T4, T5>(System.Reflection.MethodInfo target)
        {
            var method = (Action<TInstance, T1, T2, T3, T4, T5>)Delegate.CreateDelegate(typeof(Action<TInstance, T1, T2, T3, T4, T5>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                method(inst, 
                    ConvertParam<T1>(args[0]),
                    ConvertParam<T2>(args[1]),
                    ConvertParam<T3>(args[2]),
                    ConvertParam<T4>(args[3]),
                    ConvertParam<T5>(args[4]));
                return null;
            });
        }

        private ContextCallableDelegate<TInstance> CreateAction<T1, T2, T3, T4, T5, T6>(System.Reflection.MethodInfo target)
        {
            var method = (Action<TInstance, T1, T2, T3, T4, T5, T6>)Delegate.CreateDelegate(typeof(Action<TInstance, T1, T2, T3, T4, T5, T6>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                method(inst,
                    ConvertParam<T1>(args[0]),
                    ConvertParam<T2>(args[1]),
                    ConvertParam<T3>(args[2]),
                    ConvertParam<T4>(args[3]),
                    ConvertParam<T5>(args[4]),
                    ConvertParam<T6>(args[5]));
                return null;
            });
        }

        private ContextCallableDelegate<TInstance> CreateAction<T1, T2, T3, T4, T5, T6, T7>(System.Reflection.MethodInfo target)
        {
            var method = (Action<TInstance, T1, T2, T3, T4, T5, T6, T7>)Delegate.CreateDelegate(typeof(Action<TInstance, T1, T2, T3, T4, T5, T6, T7>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                method(inst,
                    ConvertParam<T1>(args[0]),
                    ConvertParam<T2>(args[1]),
                    ConvertParam<T3>(args[2]),
                    ConvertParam<T4>(args[3]),
                    ConvertParam<T5>(args[4]),
                    ConvertParam<T6>(args[5]),
                    ConvertParam<T7>(args[6]));
                return null;
            });
        }

        private ContextCallableDelegate<TInstance> CreateAction<T1, T2, T3, T4, T5, T6, T7, T8>(System.Reflection.MethodInfo target)
        {
            var method = (Action<TInstance, T1, T2, T3, T4, T5, T6, T7, T8>)Delegate.CreateDelegate(typeof(Action<TInstance, T1, T2, T3, T4, T5, T6, T7, T8>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                method(inst,
                    ConvertParam<T1>(args[0]),
                    ConvertParam<T2>(args[1]),
                    ConvertParam<T3>(args[2]),
                    ConvertParam<T4>(args[3]),
                    ConvertParam<T5>(args[4]),
                    ConvertParam<T6>(args[5]),
                    ConvertParam<T7>(args[6]),
                    ConvertParam<T8>(args[7]));
                return null;
            });
        }

        private ContextCallableDelegate<TInstance> CreateFunction<TRet>(System.Reflection.MethodInfo target)
        {
            var method = (Func<TInstance, TRet>)Delegate.CreateDelegate(typeof(Func<TInstance, TRet>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                return ConvertReturnValue(method(inst));
            });

        }

        private ContextCallableDelegate<TInstance> CreateFunction<T, TRet>(System.Reflection.MethodInfo target)
        {
            var method = (Func<TInstance, T, TRet>)Delegate.CreateDelegate(typeof(Func<TInstance, T, TRet>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                return ConvertReturnValue(method(inst, ConvertParam<T>(args[0])));
            });
        }

        private ContextCallableDelegate<TInstance> CreateFunction<T1, T2, TRet>(System.Reflection.MethodInfo target)
        {
            var method = (Func<TInstance, T1, T2, TRet>)Delegate.CreateDelegate(typeof(Func<TInstance, T1, T2, TRet>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                return ConvertReturnValue(method(inst, ConvertParam<T1>(args[0]), ConvertParam<T2>(args[1])));
            });
        }

        private ContextCallableDelegate<TInstance> CreateFunction<T1, T2, T3, TRet>(System.Reflection.MethodInfo target)
        {
            var method = (Func<TInstance, T1, T2, T3, TRet>)Delegate.CreateDelegate(typeof(Func<TInstance, T1, T2, T3, TRet>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                return ConvertReturnValue(method(inst, 
                    ConvertParam<T1>(args[0]),
                    ConvertParam<T2>(args[1]), 
                    ConvertParam<T3>(args[2])));
            });
        }

        private ContextCallableDelegate<TInstance> CreateFunction<T1, T2, T3, T4, TRet>(System.Reflection.MethodInfo target)
        {
            var method = (Func<TInstance, T1, T2, T3, T4, TRet>)Delegate.CreateDelegate(typeof(Func<TInstance, T1, T2, T3, T4, TRet>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                return ConvertReturnValue(method(inst, 
                    ConvertParam<T1>(args[0]),
                    ConvertParam<T2>(args[1]),
                    ConvertParam<T3>(args[2]), 
                    ConvertParam<T4>(args[3])));
            });

        }

        private ContextCallableDelegate<TInstance> CreateFunction<T1, T2, T3, T4, T5, TRet>(System.Reflection.MethodInfo target)
        {
            var method = (Func<TInstance, T1, T2, T3, T4, T5, TRet>)Delegate.CreateDelegate(typeof(Func<TInstance, T1, T2, T3, T4, T5, TRet>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                return ConvertReturnValue(method(inst,
                    ConvertParam<T1>(args[0]),
                    ConvertParam<T2>(args[1]),
                    ConvertParam<T3>(args[2]),
                    ConvertParam<T4>(args[3]),
                    ConvertParam<T5>(args[4])));
            });

        }

        private ContextCallableDelegate<TInstance> CreateFunction<T1, T2, T3, T4, T5, T6, TRet>(System.Reflection.MethodInfo target)
        {
            var method = (Func<TInstance, T1, T2, T3, T4, T5, T6, TRet>)Delegate.CreateDelegate(typeof(Func<TInstance, T1, T2, T3, T4, T5, T6, TRet>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                return ConvertReturnValue(method(inst,
                    ConvertParam<T1>(args[0]),
                    ConvertParam<T2>(args[1]),
                    ConvertParam<T3>(args[2]),
                    ConvertParam<T4>(args[3]),
                    ConvertParam<T5>(args[4]),
                    ConvertParam<T6>(args[5])));
            });

        }

        private ContextCallableDelegate<TInstance> CreateFunction<T1, T2, T3, T4, T5, T6, T7, TRet>(System.Reflection.MethodInfo target)
        {
            var method = (Func<TInstance, T1, T2, T3, T4, T5, T6, T7, TRet>)Delegate.CreateDelegate(typeof(Func<TInstance, T1, T2, T3, T4, T5, T6, T7, TRet>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                return ConvertReturnValue(method(inst,
                    ConvertParam<T1>(args[0]),
                    ConvertParam<T2>(args[1]),
                    ConvertParam<T3>(args[2]),
                    ConvertParam<T4>(args[3]),
                    ConvertParam<T5>(args[4]),
                    ConvertParam<T6>(args[5]),
                    ConvertParam<T7>(args[6])));
            });

        }

        private ContextCallableDelegate<TInstance> CreateFunction<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(System.Reflection.MethodInfo target)
        {
            var method = (Func<TInstance, T1, T2, T3, T4, T5, T6, T7, T8, TRet>)Delegate.CreateDelegate(typeof(Func<TInstance, T1, T2, T3, T4, T5, T6, T7, T8, TRet>), target);

            return new ContextCallableDelegate<TInstance>((inst, args) =>
            {
                return ConvertReturnValue(method(inst,
                    ConvertParam<T1>(args[0]),
                    ConvertParam<T2>(args[1]),
                    ConvertParam<T3>(args[2]),
                    ConvertParam<T4>(args[3]),
                    ConvertParam<T5>(args[4]),
                    ConvertParam<T6>(args[5]),
                    ConvertParam<T7>(args[6]),
                    ConvertParam<T8>(args[7])));
            });

        }

        private T ConvertParam<T>(IValue value)
        {
            return ContextValuesMarshaller.ConvertParam<T>(value);
        }

        private IValue ConvertReturnValue<TRet>(TRet param)
        {
            return ContextValuesMarshaller.ConvertReturnValue<TRet>(param);
        }


        private struct InternalMethInfo
        {
            public ContextCallableDelegate<TInstance> method;
            public ScriptEngine.Machine.MethodInfo methodInfo;
        }

    }
}
