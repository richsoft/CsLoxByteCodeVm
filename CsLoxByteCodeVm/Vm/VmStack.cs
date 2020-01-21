using System;
using System.Collections.Generic;
using System.Text;
using CsLoxByteCodeVm.Values;

namespace CsLoxByteCodeVm.Vm
{
    class VmStack
    {

        LoxValue[] _stack;
        public int Top { get; set; }

        public VmStack(int slots)
        {
            _stack = new LoxValue[slots];
            Top = 0;
        }

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
        /// Get top num item from the stack
        /// </summary>
        /// <param name="num">The numbe of items</param>
        /// <returns>The top items</returns>
        public LoxValue[] GetTop(int num)
        {
            LoxValue[] items = new LoxValue[num];
            Array.Copy(_stack, Top - num, items, 0, num);

            return items;
        }

        /// <summary>
        /// Reset the stack
        /// </summary>
        public void Reset()
        {
            Top = 0;
        }

        /// <summary>
        /// Push an item onto the stack
        /// </summary>
        /// <param name="value"></param>
        public void Push(LoxValue value)
        {
            _stack[Top] = value;
            Top++;
        }

        /// <summary>
        /// Pop an item off the stack
        /// </summary>
        /// <returns>The item on the top of the stack</returns>
        public LoxValue Pop()
        {
            Top--;
            return _stack[Top];
        }

        /// <summary>
        /// Peek an item on the stack
        /// </summary>
        /// <param name="distance">distance from the top</param>
        /// <returns>The value</returns>
        public LoxValue Peek(int distance)
        {
            return _stack[(Top - 1) - distance];
        }

        public LoxValue[] ToArray()
        {
            int length = Top;
            LoxValue[] r = new LoxValue[length];
            Array.Copy(_stack, 0, r, 0, length);
            return r;
        }

    }
}
