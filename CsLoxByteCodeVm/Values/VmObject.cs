using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Values
{
    public class VmObject
    {
        public ObjectType Type { get; set; }
        public VmObject Next { get; set; }
        private Object _value;

        /// <summary>
        /// Check is object is a string
        /// </summary>
        /// <returns>True if a string</returns>
        public bool IsString()
        {
            return Type == ObjectType.OBJ_STRING;
        }

        /// <summary>
        /// Return object as a string
        /// </summary>
        /// <returns></returns>
        public string AsString()
        {
            return (string)_value;
        }

        /// <summary>
        /// Print an object
        /// </summary>
        public void PrintObject()
        {
            switch (Type)
            {
                case ObjectType.OBJ_STRING:
                    Console.Write(AsString());
                    break;
            }
        }

        /// <summary>
        /// Create a string object
        /// </summary>
        public static VmObject StringObject(string s)
        {
            VmObject obj = new VmObject()
            {
                Type = ObjectType.OBJ_STRING
            };

            obj._value = s;

            return obj;
        }

        public enum ObjectType
        {
            OBJ_STRING
        }
    }


}
