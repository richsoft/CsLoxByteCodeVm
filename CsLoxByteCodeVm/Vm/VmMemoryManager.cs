using CsLoxByteCodeVm.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Vm
{
    class VmMemoryManager : IDisposable
    {
        private LoxObject _objects_head;
        private VmHashTable _strings;

        public VmHashTable Globals;

        public VmMemoryManager()
        {
            _objects_head = null;
            _strings = new VmHashTable();
            Globals = new VmHashTable();
        }

        /// <summary>
        /// Allocate a new string
        /// </summary>
        /// <param name="s">The string</param>
        /// <returns>The string </returns>
        public LoxString AllocateString(string s)
        {
            // Check if we have already interned this string
            uint hash = LoxString.HashString(s);
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
        /// Free all objects - not really necessary as C# will garbage collect itself
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
                case LoxObject.ObjectType.OBJ_FUNCTION:
                    {
                        LoxFunction function = (LoxFunction)obj;
                        break;
                    }
                case LoxObject.ObjectType.OBJ_NATIVE:
                    {
                        LoxNativeFunction native = (LoxNativeFunction)obj;
                        break;
                    }
                case LoxObject.ObjectType.OBJ_STRING:
                    {
                        LoxString s = (LoxString)obj;
                        break;
                    }

            }
        }

        public void Dispose()
        {
            FreeObjects();
            _strings.FreeTable();
            Globals.FreeTable();
        }
    }
}
