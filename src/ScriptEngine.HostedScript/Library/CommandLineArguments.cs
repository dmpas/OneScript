﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;

using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;

namespace ScriptEngine.HostedScript.Library
{
    /// <summary>
    /// Коллекция позволяет получить аргументы, переданные сценарию в командной строке.
    /// Коллекция представляет собой массив в режиме "ТолькоДляЧтения". Обращение к аргументам осуществляется по порядковому индексу.
    /// </summary>
    [ContextClass("КоллекцияАргументовКоманднойСтроки", "CommandLineArgumentsCollection")]
    public class CommandLineArguments : ContextIValueImpl, ICollectionContext
    {
        private readonly string[] _values;

        public CommandLineArguments(string[] arguments)
        {
            _values = arguments;
        }

        public override bool IsIndexed
        {
            get
            {
                return true;
            }
        }

        public override IValue GetIndexedValue(IValue index)
        {
            var arrIdx = (int)index.AsNumber();
            return ValueFactory.Create(_values[arrIdx]);
        }

        public override void SetIndexedValue(IValue index, IValue val)
        {
            throw new RuntimeException("Коллекция предназначена только для чтения");
        }

        public override int FindMethod(string name)
        {
            if (name.ToLower() == "количество" || name.ToLower() == "count")
            {
                return 0;
            }
            else
                throw RuntimeException.MethodNotFoundException(name);
        }

        public override IEnumerable<MethodInfo> GetMethods()
        {
            yield return GetMethodInfo(0);
        }

        public override void CallAsFunction(int methodNumber, IValue[] arguments, out IValue retValue)
        {
            if (methodNumber == 0)
                retValue = ValueFactory.Create(this.Count());
            else
                retValue = null;
        }

        public override void CallAsProcedure(int methodNumber, IValue[] arguments)
        {
        }

        public override MethodInfo GetMethodInfo(int methodNumber)
        {
            if (methodNumber == 0)
                return new MethodInfo()
                {
                    Name = "количество",
                    Alias = "count",
                    IsFunction = true,
                    Params = new ParameterDefinition[0]
                };
            else
                throw new InvalidOperationException();
        }


        #region ICollectionContext Members

        public int Count()
        {
            return _values.Length;
        }

        public void Clear()
        {
            throw new RuntimeException("Коллекция предназначена только для чтения");
        }

        public CollectionEnumerator GetManagedIterator()
        {
            return new CollectionEnumerator(_values.Select(x=>ValueFactory.Create(x)));
        }

        #endregion


        private static readonly CommandLineArguments _empty = new CommandLineArguments(new string[0]);

        public static CommandLineArguments Empty
        {
            get { return _empty; }
        }
    }
}
