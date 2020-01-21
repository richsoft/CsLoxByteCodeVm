using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Values
{
    public abstract class LoxObject
    {
        public ObjectType Type { get; }
        public LoxObject Next { get; set; }

        public LoxObject() { }

        public LoxObject(ObjectType type)
        {
            Type = type;
        }


        /// <summary>
        /// Check is object is a string
        /// </summary>
        /// <returns>True if a string</returns>
        public bool IsString()
        {
            return Type == ObjectType.OBJ_STRING;
        }

        /// <summary>
        /// Check if object is a function
        /// </summary>
        /// <returns>Tryue if a function</returns>
        public bool IsFunction()
        {
            return Type == ObjectType.OBJ_FUNCTION;
        }

        /// <summary>
        /// Check if objects is a native function
        /// </summary>
        /// <returns>True if a native function</returns>
        public bool IsNative()
        {
            return Type == ObjectType.OBJ_NATIVE;
        }

        /// <summary>
        /// Print an object
        /// </summary>
        public abstract void PrintObject();

        public enum ObjectType
        {
            OBJ_FUNCTION,
            OBJ_NATIVE,
            OBJ_STRING
        }
    }


}
