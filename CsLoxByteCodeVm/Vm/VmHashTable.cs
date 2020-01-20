using CsLoxByteCodeVm.Values;
using System;
using System.Collections.Generic;
using System.Text;

namespace CsLoxByteCodeVm.Vm
{
    class VmHashTable
    {
        public int Count { get; private set; }
        public int Capacity { get; private set; }
        private Entry[] _entries;
        private const double MAX_LOAD = 0.75;

        public VmHashTable()
        {
            Count = 0;
            Capacity = 0;
            _entries = null;
        }

        public VmHashTable(VmHashTable source)
        {
            Count = 0;
            Capacity = 0;
            _entries = null;
            AddAll(source);
        }

        /// <summary>
        /// Reset the Hashtable
        /// </summary>
        public void FreeTable()
        {
            Count = 0;
            Capacity = 0;
            _entries = null;
        }

        /// <summary>
        /// Add an item into the hash table
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        /// <returns>True if item was added</returns>
        public bool Set(LoxString key, LoxValue value)
        {
            // Grow if needed
            if (Count + 1 > Capacity * MAX_LOAD)
            {
                int new_capacity = Capacity < 8 ? 8 : (Capacity * 2);
                AdjustCapacity(new_capacity);
            }

            // Look up the key
            Entry entry = FindEntry(ref _entries, key);

            // If te key is null, its a new item
            bool is_new_key = entry.Key == null;
            // Only increment the count if we didn't reuse a tombstone
            if (is_new_key && entry.Value.IsNil()) Count++;

            // Set the new key/value pair
            entry.Key = key;
            entry.Value = value;
            return is_new_key;
        }

        /// <summary>
        /// Add all entries from another hash table
        /// </summary>
        /// <param name="source">The source hashtable</param>
        public void AddAll(VmHashTable source) {
            for (int i = 0; i < source.Capacity; i++)
            {
                Entry entry = source._entries[i];
                if (entry.Key != null)
                {
                    Set(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// Get a entry
        /// </summary>
        /// <param name="key">The key to find</param>
        /// <param name="value">The output value</param>
        /// <returns>True if the key exists</returns>
        public bool Get(LoxString key, out LoxValue value)
        {
            value = null;

            if (Count == 0) return false;

            Entry entry = FindEntry(ref _entries, key);
            if (entry.Key == null) return false;

            value = entry.Value;
            return true;

        }

        /// <summary>
        /// Get a entry
        /// </summary>
        /// <param name="key">The key to find</param>
        /// <returns>The value if the key exists, or else null</returns>
        public LoxValue Get(LoxString key)
        {

            if (Count == 0) return null;

            Entry entry = FindEntry(ref _entries, key);
            if (entry.Key == null) return null;

            return entry.Value;
        }

        /// <summary>
        /// Find a string key
        /// </summary>
        /// <param name="s">The string</param>
        /// <param name="hash">The string hash</param>
        /// <returns>The key if found</returns>
        public LoxString FindStringKey(string s, UInt32 hash)
        {
            if (Count == 0) return null;

            UInt32 index = (UInt32)(hash % Capacity);
            while (true)
            {
                Entry entry = _entries[index];

                if (entry.Key == null)
                {
                    // Stop if we find a empty non-tombstone entry
                    if (entry.Value.IsNil()) return null;
                }
                else if (entry.Key.Hash == hash && string.Equals(entry.Key.Value, s)) {
                    // Found it
                    return entry.Key;
                }

                index = (UInt32)((index + 1) % Capacity);
            }

        }


        /// <summary>
        /// Delete an entry from the table
        /// </summary>
        /// <param name="key">The key to delete</param>
        /// <returns>True if the entry was deleted</returns>
        public bool Delete(LoxString key)
        {
            if (Count == 0) return false;

            Entry entry = FindEntry(ref _entries, key);
            if (entry.Key == null)
            {
                return false;
            }

            // Place a tombstone in the entry
            entry.Key = null;
            entry.Value = LoxValue.BooleanValue(true);

            return true;
        }

        /// <summary>
        /// Find an entry in a set of entries
        /// </summary>
        /// <param name="key">The key to find</param>
        /// <param name="entries">The entries to search</param>
        /// <returns></returns>
        private Entry FindEntry(ref Entry[] entries, LoxString key)
        {
            UInt32 index = (UInt32)(key.Hash % entries.Length);

            Entry tombstone = null;

            // Look down the slots until we find the entry
            // When adding if calculated slor is used, 
            // it is placed in the next available slow.
            while (true)
            {
                Entry entry = entries[index];

                if (entry.Key == null)
                {
                    if (entry.Value.IsNil())
                    {
                        // Empty entry
                        // If we have passed a tombstone, we can use it
                        return tombstone != null ? tombstone : entry;
                    }
                    else
                    {
                        // We found a tombstone
                        if (tombstone == null) tombstone = entry;
                    }
                } else
                {
                    // We found the key
                    return entry;
                }

                index = (UInt32)((index + 1) % entries.Length);
            }
        }

        /// <summary>
        /// Adjust the size of the array
        /// </summary>
        /// <param name="new_capacity"></param>
        private void AdjustCapacity(int new_capacity)
        {
            // Create a new array
            Entry[] new_entries = new Entry[new_capacity];

            // Load it with empty entries
            for (int i = 0; i < new_capacity; i++)
            {
                new_entries[i] = new Entry()
                {
                    Key = null,
                    Value = LoxValue.NilValue()
                };
            }

            // Copy over the existing items
            Count = 0;
            for (int i = 0; i < Capacity; i++)
            {
                Entry entry = _entries[i];
                if (entry.Key == null) continue;

                Entry dest = FindEntry(ref new_entries, entry.Key);
                dest.Key = entry.Key;
                dest.Value = entry.Value;
                Count++;
            }

            _entries = new_entries;
            Capacity = new_capacity;
        }

        private class Entry
        {
            public LoxString Key { get; set; }
            public LoxValue Value { get; set; }
        }

    }
}
