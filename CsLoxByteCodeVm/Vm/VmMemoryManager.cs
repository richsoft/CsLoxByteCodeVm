using CsLoxByteCodeVm.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Vm
{
    class VmMemoryManager : IDisposable
    {
        LoxObject _objects_head;
        VmHashTable _strings;

        public VmMemoryManager()
        {
            _objects_head = null;
            _strings = new VmHashTable();
        }

        /// <summary>
        /// Allocate a new string
        /// </summary>
        /// <param name="s">The string</param>
        /// <returns>The string </returns>
        public LoxString AllocateString(string s)
        {
            // Check if we have already interned this string
            UInt32 hash = LoxString.HashString(s);
            LoxString interned = _strings.FindStringKey(s, hash);
            if (interned != null) return interned;

            // Create a new string
            LoxString s_obj = new LoxString(s);

            // Intern the string
            // Add it to the hash table
            _strings.Set(s_obj, LoxValue.NilValue());

            // Put it at the head of the list
            s_obj.Next = _objects_head;
            _objects_head = s_obj;

            return s_obj;

        }

        /// <summary>
        /// Free all objects
        /// </summary>
        public void FreeObjects()
        {
            LoxObject obj = _objects_head;
            while (obj != null)
            {
                LoxObject next = obj.Next;
                FreeObject(obj);
                obj = next;
            }

        }

        /// <summary>
        /// Free an object
        /// </summary>
        /// <param name="obj">The object to free</param>
        public void FreeObject(LoxObject obj)
        {
            switch (obj.Type)
            {
                case LoxObject.ObjectType.OBJ_STRING:
                    break;
            }
        }

        public void Dispose()
        {
            FreeObjects();
            _strings.FreeTable();
        }
    }
}
