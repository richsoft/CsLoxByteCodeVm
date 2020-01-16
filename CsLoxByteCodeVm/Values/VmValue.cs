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
    }

    public enum ValueType
    {
        VAL_BOOL,
        VAL_NIL,
        VAL_NUMBER
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Value
    {
        [FieldOffset(0)]
        public bool Boolean;

        [FieldOffset(0)]
        public double Number;
    }
}
