﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScriptEngine.Machine;

namespace ScriptEngine.Compiler
{
    class SymbolScope
    {
        readonly Dictionary<string, int> _variableNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        readonly List<VariableInfo> _variables = new List<VariableInfo>();

        readonly Dictionary<string, int> _methodsNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        readonly List<MethodInfo> _methods = new List<MethodInfo>();

        public MethodInfo GetMethod(string name)
        {
            var num = GetMethodNumber(name);
            return _methods[num];
        }

        public MethodInfo GetMethod(int number)
        {
            return _methods[number];
        }

        public int GetVariableNumber(string name)
        {
            int varNumber;
            if(_variableNumbers.TryGetValue(name, out varNumber))
            {
                return varNumber;
            }
            else
            {
                throw new SymbolNotFoundException(name);
            }
        }

        public VariableInfo GetVariable(int number)
        {
            return _variables[number];
        }

        public int GetMethodNumber(string name)
        {
            try
            {
                return _methodsNumbers[name];
            }
            catch (KeyNotFoundException)
            {
                throw new SymbolNotFoundException(name);
            }
        }

        public bool IsVarDefined(string name)
        {
            return _variableNumbers.ContainsKey(name);
        }

        public bool IsMethodDefined(string name)
        {
            return _methodsNumbers.ContainsKey(name);
        }

        public int DefineVariable(string name)
        {
            return DefineVariable(name, SymbolType.Variable);
        }

        public int DefineModule(string name, object moduleSourceIdentifier)
        {
            int index;
            if (_variableNumbers.TryGetValue(name, out index))
            {
                if (_variables[index].SourceIdentifier == null
                    || !_variables[index].SourceIdentifier.Equals(moduleSourceIdentifier))
                {
                    throw new InvalidOperationException(string.Format("Symbol already defined in the scope ({0}) [loaded `{1}`, new `{2}`]",
                                                                      name, _variables[index].SourceIdentifier, moduleSourceIdentifier));
                }
                return index;
            }
            var newIdx = _variables.Count;
            _variableNumbers[name] = newIdx;

            _variables.Add(new VariableInfo() {
                Index = newIdx,
                Identifier = name,
                Type = SymbolType.ContextProperty,
                SourceIdentifier = moduleSourceIdentifier
            });

            return newIdx;
        }

        public int DefineVariable(string name, SymbolType symbolType)
        {
            if (!IsVarDefined(name))
            {

                var newIdx = _variables.Count;
                _variableNumbers[name] = newIdx;

                _variables.Add(new VariableInfo()
                {
                    Index = newIdx,
                    Identifier = name,
                    Type = symbolType
                });

                return newIdx;
            }
            else
            {
                throw new InvalidOperationException($"Symbol already defined in the scope ({name})");
            }
        }

        public int DefineMethod(MethodInfo method)
        {
            if (!IsMethodDefined(method.Name))
            {
                int newIdx = _methods.Count;
                _methods.Add(method);
                _methodsNumbers[method.Name] = newIdx;

                if (method.Alias != null)
                    _methodsNumbers[method.Alias] = newIdx;

                return newIdx;
            }
            else
            {
                throw new InvalidOperationException("Symbol already defined in the scope");
            }
        }

        public string GetVariableName(int number)
        {
            return _variableNumbers.First(x => x.Value == number).Key;
        }

        public int VariableCount 
        {
            get
            {
                return _variables.Count;
            }
        }

        public int MethodCount
        {
            get
            {
                return _methods.Count;
            }
        }

        public bool IsDynamicScope 
        { 
            get; 
            set; 
        }
    }

    class SymbolNotFoundException : CompilerException
    {
        public SymbolNotFoundException(string symbol) : base(string.Format("Неизвестный символ: {0}", symbol))
        {

        }
    }

}
