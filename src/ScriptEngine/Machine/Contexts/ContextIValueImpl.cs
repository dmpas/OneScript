﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;

namespace ScriptEngine.Machine.Contexts
{
    public abstract class ContextIValueImpl : IRuntimeContextInstance, IValue
    {
        private TypeDescriptor _type;

        public ContextIValueImpl()
        {

        }

        public ContextIValueImpl(TypeDescriptor type)
        {
            DefineType(type);
        }

        protected void DefineType(TypeDescriptor type)
        {
            _type = type;
        }

        public override string ToString()
        {
            return _type.Name ?? base.ToString();
        }
        
        #region IValue Members

        public DataType DataType
        {
            get { return Machine.DataType.Object; }
        }

        public TypeDescriptor SystemType
        {
            get
            {
                if (_type.Name == null)
                {
                    if (TypeManager.IsKnownType(this.GetType()))
                    {
                        _type = TypeManager.GetTypeByFrameworkType(this.GetType());
                    }
                    else
                    {
                        throw new InvalidOperationException($"Type {GetType()} is not defined");
                    }
                }

                return _type;
            }
        }

        public decimal AsNumber()
        {
            throw RuntimeException.ConvertToNumberException();
        }

        public DateTime AsDate()
        {
            throw RuntimeException.ConvertToDateException();
        }

        public bool AsBoolean()
        {
            throw RuntimeException.ConvertToBooleanException();
        }

        public virtual string AsString()
        {
            return SystemType.Name;
        }

        public IRuntimeContextInstance AsObject()
        {
            return this;
        }

        public IValue GetRawValue()
        {
            return this;
        }

        #endregion

        #region IComparable<IValue> Members

        public int CompareTo(IValue other)
        {
            if (other.SystemType.Equals(this.SystemType))
            {
                if (this.Equals(other))
                {
                    return 0;
                }
                else
                {
                    throw RuntimeException.ComparisonNotSupportedException();
                }
            }
            else
            {
                return this.SystemType.ToString().CompareTo(other.SystemType.ToString());
            }
        }

        #endregion

        #region IEquatable<IValue> Members

        public virtual bool Equals(IValue other)
        {
            if (other.SystemType.Equals(this.SystemType))
            {
                return Object.ReferenceEquals(this.AsObject(), other.AsObject());
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region IRuntimeContextInstance Members

        public virtual bool IsIndexed
        {
            get { return false; }
        }

        public virtual bool DynamicMethodSignatures
        {
            get { return false; }
        }

        public virtual IValue GetIndexedValue(IValue index)
        {
            throw new NotImplementedException();
        }

        public virtual void SetIndexedValue(IValue index, IValue val)
        {
            throw new NotImplementedException();
        }

        public virtual int FindProperty(string name)
        {
            throw RuntimeException.PropNotFoundException(name);
        }
        public virtual bool IsPropReadable(int propNum)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsPropWritable(int propNum)
        {
            throw new NotImplementedException();
        }
        public virtual IValue GetPropValue(int propNum)
        {
            throw new NotImplementedException();
        }
        public virtual void SetPropValue(int propNum, IValue newVal)
        {
            throw new NotImplementedException();
        }

        public virtual int GetPropCount()
        {
            throw new NotImplementedException();
        }

        public virtual string GetPropName(int propNum)
        {
            throw new NotImplementedException();
        }

        public virtual int GetMethodsCount()
        {
            throw new NotImplementedException();
        }

        public virtual int FindMethod(string name)
        {
            throw RuntimeException.MethodNotFoundException(name);
        }
        public virtual MethodInfo GetMethodInfo(int methodNumber)
        {
            throw new NotImplementedException();
        }
        public virtual void CallAsProcedure(int methodNumber, IValue[] arguments)
        {
            throw new NotImplementedException();
        }
        public virtual void CallAsFunction(int methodNumber, IValue[] arguments, out IValue retValue)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ScriptConstructorAttribute : Attribute
    {
        public string Name { get; set; }
        public bool ParametrizeWithClassName { get; set; }
    }
}
