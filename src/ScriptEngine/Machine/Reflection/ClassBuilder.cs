﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SysReflection = System.Reflection;
using ScriptEngine.Machine.Contexts;

namespace ScriptEngine.Machine.Reflection
{
    public class ClassBuilder<T> where T: ScriptDrivenObject
    {
        private List<SysReflection.MethodInfo> _methods = new List<SysReflection.MethodInfo>();
        private List<SysReflection.PropertyInfo> _properties = new List<SysReflection.PropertyInfo>();
        private List<SysReflection.FieldInfo> _fields = new List<SysReflection.FieldInfo>();
        private List<SysReflection.ConstructorInfo> _constructors = new List<SysReflection.ConstructorInfo>();

        public string TypeName { get; set; }
        public LoadedModule Module { get; set; }

        public ClassBuilder<T> SetTypeName(string typeName)
        {
            TypeName = typeName;
            return this;
        }

        public ClassBuilder<T> SetModule(LoadedModule module)
        {
            Module = module;
            return this;
        }

        public ClassBuilder<T> ExportClassMethod(string methodName)
        {
            var mi = typeof(T).GetMethod(methodName);
            if(mi == null)
                throw new InvalidOperationException($"Method '{methodName}' not found in {typeof(T)}");

            ExportClassMethod(mi);
            return this;
        }

        public ClassBuilder<T> ExportClassMethod(SysReflection.MethodInfo nativeMethod)
        {
            if(nativeMethod == null)
                throw new ArgumentNullException(nameof(nativeMethod));

            if (nativeMethod.ReflectedType != typeof(T))
                throw new InvalidOperationException($"Method '{nativeMethod.Name}' not found in {typeof(T)}");

            _methods.Add(nativeMethod);
            return this;
        }

        public ClassBuilder<T> ExportProperty(string propName)
        {
            var mi = typeof(T).GetProperty(propName);
            if (mi == null)
                throw new InvalidOperationException($"Method '{propName}' not found in {typeof(T)}");

            return this;
        }

        public ClassBuilder<T> ExportMethods(bool includeDeprecations = false)
        {
            var methods = typeof(T).GetMethods()
                                   .Where(x => MarkedAsContextMethod(x, includeDeprecations));
            _methods.AddRange(methods);
            return this;
        }

        public ClassBuilder<T> ExportProperties(bool includeDeprecations = false)
        {
            var props = typeof(T).GetProperties()
                                   .Where(MarkedAsContextProperty);
            _properties.AddRange(props);
            return this;
        }

        private bool MarkedAsContextMethod(SysReflection.MemberInfo member, bool includeDeprecations)
        {
            return member
                  .GetCustomAttributes(typeof(ContextMethodAttribute), false)
                  .Any(x => includeDeprecations || (x as ContextMethodAttribute)?.IsDeprecated == false);
        }

        private bool MarkedAsContextProperty(SysReflection.MemberInfo member)
        {
            return member.GetCustomAttributes(typeof(ContextPropertyAttribute), false).Any();
        }

        public ClassBuilder<T> ExportConstructor(SysReflection.ConstructorInfo info)
        {
            if (info.DeclaringType != typeof(T))
            {
                throw new ArgumentException("info must belong to the current class");
            }

            _constructors.Add(info);
            return this;
        }

        public ClassBuilder<T> ExportConstructor(Func<object[], IRuntimeContextInstance> creator)
        {
            var info = new ReflectedConstructorInfo(creator);
            info.SetDeclaringType(typeof(T));
            _constructors.Add(info);
            return this;
        }

        public ClassBuilder<T> ExportScriptVariables()
        {
            if(Module == null)
                throw new InvalidOperationException("Module is not set");

            foreach (var variable in Module.Variables)
            {
                var exported = Module.ExportedProperies.FirstOrDefault(x => x.SymbolicName.Equals(variable.Identifier) || x.SymbolicName.Equals(variable.Alias));
                bool exportFlag = exported.SymbolicName != null;
                if(exportFlag)
                    System.Diagnostics.Debug.Assert(variable.Index == exported.Index, "indices of vars and exports are equal");

                var fieldInfo = new ReflectedFieldInfo(variable, exportFlag);
                _fields.Add(fieldInfo);
            }

            return this;
        }

        public ClassBuilder<T> ExportScriptMethods()
        {
            if (Module == null)
                throw new InvalidOperationException("Module is not set");

            for (int i = 0; i < Module.Methods.Length; i++)
            {
                var methodDescriptor = Module.Methods[i];
                var methInfo = CreateMethodInfo(methodDescriptor.Signature);
                _methods.Add(methInfo);
            }

            return this;
        }

        private ReflectedMethodInfo CreateMethodInfo(MethodInfo methInfo)
        {
            var reflectedMethod = new ReflectedMethodInfo(methInfo.Name);
            reflectedMethod.SetPrivate(!methInfo.IsExport);
            reflectedMethod.IsFunction = methInfo.IsFunction;

            for (int i = 0; i < methInfo.Params.Length; i++)
            {
                var currentParam = methInfo.Params[i];
                var reflectedParam = new ReflectedParamInfo(currentParam.Name, currentParam.IsByValue);
                reflectedParam.SetOwner(reflectedMethod);
                reflectedParam.SetPosition(i);
                reflectedMethod.Parameters.Add(reflectedParam);
            }

            return reflectedMethod;

        }

        public ClassBuilder<T> ExportScriptConstructors()
        {
            var statics = typeof(T).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                                   .Where(x => x.GetCustomAttributes(false).Any(y => y is ScriptConstructorAttribute));

            foreach (var staticConstructor in statics)
            {
                var action = new Func<object[], IRuntimeContextInstance>((parameters) =>
                {
                    return (IRuntimeContextInstance)staticConstructor.Invoke(null, SysReflection.BindingFlags.InvokeMethod, null, parameters, CultureInfo.CurrentCulture);
                });

                ExportConstructor(action);
            }

            return this;
        }

        public ClassBuilder<T> ExportDefaults()
        {
            ExportMethods();
            ExportProperties();
            ExportScriptVariables();
            ExportScriptMethods();
            ExportScriptConstructors();

            return this;
        }

        public Type Build()
        {
            var classDelegator = new ReflectedClassType<T>();
            classDelegator.SetName(TypeName);
            classDelegator.SetFields(_fields);
            classDelegator.SetProperties(_properties);
            classDelegator.SetMethods(_methods);
            classDelegator.SetConstructors(_constructors);

            return classDelegator;
        }
        
    }
}
