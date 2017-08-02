/*----------------------------------------------------------
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
    class LabelInfo
    {
        public LabelInfo(string labelName, CodeBatchHierarchy hierarchy = null)
        {
            LabelName = labelName;
            Hierarchy = hierarchy;
        }

        public string LabelName { get; }
        public CodeBatchHierarchy Hierarchy;
        public List<CodeBatchHierarchy> ForwardCalls { get; } = new List<CodeBatchHierarchy>();

        public bool IsDefined()
        {
            return Hierarchy != null;
        }
    }

    class SymbolScope
    {
        readonly Dictionary<string, int> _variableNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        readonly List<VariableInfo> _variables = new List<VariableInfo>();

        readonly Dictionary<string, int> _methodsNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        readonly List<MethodInfo> _methods = new List<MethodInfo>();

        readonly Dictionary<string, LabelInfo> _labels = new Dictionary<string, LabelInfo>(StringComparer.OrdinalIgnoreCase);

        public bool HasLabel(string labelName, out CodeBatchHierarchy labelPosition)
        {
            LabelInfo labelInfo;
            if (_labels.TryGetValue(labelName, out labelInfo))
            {
                if (labelInfo.IsDefined())
                {
                    labelPosition = labelInfo.Hierarchy;
                    return true;
                }
            }
            labelPosition = null;
            return false;
        }

        public void RegisterLabel(string labelName, CodeBatchHierarchy hierarchy)
        {
            LabelInfo labelInfo;
            if (!_labels.TryGetValue(labelName, out labelInfo))
            {
                labelInfo = new LabelInfo(labelName, hierarchy);
                _labels[labelName] = labelInfo;
            }
            labelInfo.Hierarchy = hierarchy;
        }

        public int GetLabelPosition(string labelName)
        {
            return _labels[labelName].Hierarchy.CodePosition;
        }

        public void RegisterForwardCall(string labelName, CodeBatchHierarchy callerPosition)
        {
            LabelInfo labelInfo;
            if (!_labels.TryGetValue(labelName, out labelInfo))
            {
                labelInfo = new LabelInfo(labelName);
                _labels[labelName] = labelInfo;
            }
            labelInfo.ForwardCalls.Add(callerPosition);
        }

        public void CheckUndefinedLabels()
        {
            foreach (var label in _labels)
            {
                foreach (var call in label.Value.ForwardCalls)
                {
                    throw CompilerException.UndefinedLabelCall(label.Key, call.LineNumber);
                }
            }
        }

        public IEnumerable<CodeBatchHierarchy> GetCallPositionsForLabel(string labelName)
        {
            LabelInfo labelInfo;
            if (!_labels.TryGetValue(labelName, out labelInfo))
            {
                return new List<CodeBatchHierarchy>();
            }
            return labelInfo.ForwardCalls;
        }

        public void ClearForwardCallsForLabel(string labelName)
        {
            _labels[labelName].ForwardCalls.Clear();
        }

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
