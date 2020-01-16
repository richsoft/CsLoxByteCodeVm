using System;
using System.Collections.Generic;
using System.Text;
using CsLoxByteCodeVm.Values;

namespace CsLoxByteCodeVm.Vm
{
    class VmStack
    {
        private const int MAX_STACK = 256;
        VmValue[] _stack = new VmValue[MAX_STACK];
        private int _top;

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
        public void Push(VmValue value)
        {
            _stack[_top] = value;
            _top++;
        }

        /// <summary>
        /// Pop an item off the stack
        /// </summary>
        /// <returns>The item on the top of the stack</returns>
        public VmValue Pop()
        {
            _top--;
            return _stack[_top];
        }

        /// <summary>
        /// Peek an item on the stack
        /// </summary>
        /// <param name="distance">distance from the top</param>
        /// <returns>The value</returns>
        public VmValue Peek(int distance)
        {
            return _stack[(_top - 1) - distance];
        }

        public VmValue[] ToArray()
        {
            int length = _top;
            VmValue[] r = new VmValue[length];
            Array.Copy(_stack, 0, r, 0, length);
            return r;
        }

    }
}
