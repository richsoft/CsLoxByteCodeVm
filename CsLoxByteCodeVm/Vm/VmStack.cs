using System;
using System.Collections.Generic;
using System.Text;
using CsLoxByteCodeVm.Values;

namespace CsLoxByteCodeVm.Vm
{
    class VmStack
    {
        private const int MAX_STACK = 256;
        LoxValue[] _stack = new LoxValue[MAX_STACK];
        private int _top;

        public LoxValue this[int index]
        {
            get
            {
                return _stack[index];
            }
            set
            {
                _stack[index] = value;
            }
        }

        /// <summary>
        /// Reset the stack
        /// </summary>
        public void Reset()
        {
            _top = 0;
        }

        /// <summary>
        /// Push an item onto the stack
        /// </summary>
        /// <param name="value"></param>
        public void Push(LoxValue value)
        {
            _stack[_top] = value;
            _top++;
        }

        /// <summary>
        /// Pop an item off the stack
        /// </summary>
        /// <returns>The item on the top of the stack</returns>
        public LoxValue Pop()
        {
            _top--;
            return _stack[_top];
        }

        /// <summary>
        /// Peek an item on the stack
        /// </summary>
        /// <param name="distance">distance from the top</param>
        /// <returns>The value</returns>
        public LoxValue Peek(int distance)
        {
            return _stack[(_top - 1) - distance];
        }

        public LoxValue[] ToArray()
        {
            int length = _top;
            LoxValue[] r = new LoxValue[length];
            Array.Copy(_stack, 0, r, 0, length);
            return r;
        }

    }
}
