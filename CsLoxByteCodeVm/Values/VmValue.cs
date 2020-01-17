using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CsLoxByteCodeVm.Values
{
    class VmValue
    {
        public ValueType Type { get; set; }
        public Value _value;

        private VmValue() { }

        public VmValue(double value)
        {
            Type = ValueType.VAL_NUMBER;
            _value.Number = value;
        }

        public VmValue(bool value)
        {
            Type = ValueType.VAL_BOOL;
            _value.Boolean = value;
        }

        /// <summary>
        /// Get the value as a boolean
        /// </summary>
        /// <returns>The boolean value</returns>
        public bool AsBoolean()
        {
            return _value.Boolean;
        }

        /// <summary>
        /// Get value as a number
        /// </summary>
        /// <returns>The number value</returns>
        public double AsNumber()
        {
            return _value.Number;
        }

        /// <summary>
        /// Get the value as an object
        /// </summary>
        /// <returns>The object value</returns>
        public VmObject AsObject()
        {
            return _value.Object;
        }

        /// <summary>
        /// Check if value is a boolean
        /// </summary>
        /// <returns>True if a boolean</returns>
        public bool IsBoolean()
        {
            return Type == ValueType.VAL_BOOL;
        }

        /// <summary>
        /// Check if value is a number
        /// </summary>
        /// <returns>True if a number</returns>
        public bool IsNumber()
        {
            return Type == ValueType.VAL_NUMBER;
        }

        /// <summary>
        /// Check if value is nil
        /// </summary>
        /// <returns>True if a nil</returns>
        public bool IsNil()
        {
            return Type == ValueType.VAL_NIL;
        }

        /// <summary>
        /// Chek if value is an object
        /// </summary>
        /// <returns>True if an object</returns>
        public bool IsObject()
        {
            return Type == ValueType.VAL_OBJ;
        }

        /// <summary>
        /// Check if is string
        /// </summary>
        /// <returns>True if the value is a string</returns>
        public bool IsString()
        {
            return IsObject() && AsObject().IsString();
        }

        /// <summary>
        /// Test equality with another value
        /// </summary>
        /// <param name="other">The other value</param>
        /// <returns>True if equal</returns>
        public bool ValueEquals(VmValue other)
        {
            if (Type != other.Type) return false;

            switch (Type)
            {
                case ValueType.VAL_BOOL: return AsBoolean() == other.AsBoolean();
                case ValueType.VAL_NIL: return true;
                case ValueType.VAL_NUMBER: return AsNumber() == other.AsNumber();
                case ValueType.VAL_OBJ:
                    VmObject a = AsObject();
                    VmObject b = other.AsObject();
                    return string.Equals(a.AsString(), b.AsString());

            }

            return false;
        }

        /// <summary>
        /// Check if value is falsy (NIL or FALSE)
        /// </summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if falsy</returns>
        public bool IsFalsy()
        {
            return IsNil() || (IsBoolean() && !AsBoolean());
        }

        /// <summary>
        /// Print the value
        /// </summary>
        public void PrintValue()
        {
            switch (Type)
            {
                case ValueType.VAL_BOOL:
                    Console.Write(AsBoolean() ? "true" : "false");
                    break;
                case ValueType.VAL_NIL:
                    Console.Write("nil");
                    break;
                case ValueType.VAL_NUMBER:
                    Console.Write($"{AsNumber()}");
                    break;
                case ValueType.VAL_OBJ:
                    AsObject().PrintObject();
                    break;

            }


        }

        /// <summary>
        /// Create a boolean value
        /// </summary>
        /// <param name="value">The vlaue</param>
        /// <returns>The new value</returns>
        public static VmValue BooleanValue(bool value)
        {
            return new VmValue(value);
        }

        /// <summary>
        /// Create a boolean value
        /// </summary>
        /// <param name="value">The vlaue</param>
        /// <returns>The new value</returns>
        public static VmValue NumberValue(double value)
        {
            return new VmValue(value);
        }

        /// <summary>
        /// Create a nil value
        /// </summary>
        /// <param name="value">The vlaue</param>
        /// <returns>The new value</returns>
        public static VmValue NilValue()
        {
            VmValue val = new VmValue()
            {
                Type = ValueType.VAL_NIL
            };

            val._value.Number = 0;

            return val;

        }

        /// <summary>
        /// Create a string object
        /// </summary>
        /// <param name="s">The string</param>
        /// <returns>The new value</returns>
        public static VmValue StringObject(string s)
        {
            VmValue val = new VmValue()
            {
                Type = ValueType.VAL_OBJ
            };

            val._value.Object = VmObject.StringObject(s);

            return val;
        }
    }

    public enum ValueType
    {
        VAL_BOOL,
        VAL_NIL,
        VAL_NUMBER,
        VAL_OBJ
    }

    //[StructLayout(LayoutKind.Explicit)]
    public struct Value
    {
        public bool Boolean;

        public double Number;

        public VmObject Object;
    }
}
