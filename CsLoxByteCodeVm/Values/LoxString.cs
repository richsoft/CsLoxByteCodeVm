using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Values
{
    class LoxString : LoxObject
    {
        public string Value { get; }
        public int Length => Value.Length;
        public UInt32 Hash { get; }

        public LoxString(string value) : base(ObjectType.OBJ_STRING)
        {
            Value = value;
            Hash = HashString(Value);
        }

        public LoxString(string value, UInt32 hash) : base(ObjectType.OBJ_STRING)
        {
            Value = value;
            Hash = hash;
        }

        /// <summary>
        /// Calculate the hash of the string using FNV-1a
        /// </summary>
        /// <param name="s">The string to hash</param>
        /// <returns>The hash</returns>
        public static UInt32 HashString(string s)
        {
            UInt32 hash = 2166136261u;

            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= 16777619;
            }

            return hash;
        }

        /// <summary>
        /// Return the string value
        /// </summary>
        /// <returns></returns>
        public string AsString()
        {
            return Value;
        }

        public override void PrintObject()
        {
            Console.Write(Value);
        }
    }
}
