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
        /// Print an object
        /// </summary>
        public abstract void PrintObject();

        public enum ObjectType
        {
            OBJ_STRING
        }
    }


}
