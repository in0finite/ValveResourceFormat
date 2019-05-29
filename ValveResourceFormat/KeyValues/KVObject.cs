// <auto-generated/>
// Make stylecop ignore this file because we're rewriting KV3 in separate project.
using System;
using System.Collections.Generic;
using System.Linq;

namespace ValveResourceFormat.KeyValues
{
    //Datastructure for a KV Object
    public class KVObject : IKeyValueCollection
    {
        public string Key { get; private set; }
        public Dictionary<string, KVValue> Properties { get; private set; }
        private bool IsArray;
        public int Count { get; private set; }

        public KVObject(string name)
        {
            Key = name;
            Properties = new Dictionary<string, KVValue>();
            IsArray = false;
            Count = 0;
        }

        public KVObject(string name, bool isArray)
            : this(name)
        {
            IsArray = isArray;
        }

        //Add a property to the structure
        public virtual void AddProperty(string name, KVValue value)
        {
            if (IsArray)
            {
                // Make up a key for the dictionary
                Properties.Add(Count.ToString(), value);
            }
            else
            {
                Properties.Add(name, value);
            }

            Count++;
        }

        public void Serialize(IndentedTextWriter writer)
        {
            if (IsArray)
            {
                SerializeArray(writer);
            }
            else
            {
                SerializeObject(writer);
            }
        }

        //Serialize the contents of the KV object
        private void SerializeObject(IndentedTextWriter writer)
        {
            //Don't enter the top-most object
            if (Key != null)
            {
                writer.WriteLine();
            }

            writer.WriteLine("{");
            writer.Indent++;

            foreach (var pair in Properties)
            {
                writer.Write(pair.Key);
                writer.Write(" = ");

                pair.Value.PrintValue(writer);

                writer.WriteLine();
            }

            writer.Indent--;
            writer.Write("}");
        }

        private void SerializeArray(IndentedTextWriter writer)
        {
            //Need to preserve the order
            writer.WriteLine();
            writer.WriteLine("[");
            writer.Indent++;
            for (var i = 0; i < Count; i++)
            {
                Properties[i.ToString()].PrintValue(writer);

                writer.WriteLine(",");
            }

            writer.Indent--;
            writer.Write("]");
        }

        public IEnumerable<string> Keys => Properties.Keys;

        public bool ContainsKey(string name) => Properties.ContainsKey(name);

        public T GetProperty<T>(string name)
        {
            if (Properties.TryGetValue(name, out var value))
            {
                return (T)value.Value;
            }
            else
            {
                return default(T);
            }
        }

        public T[] GetArray<T>(string name)
        {
            if (Properties.TryGetValue(name, out var value))
            {
                if (value.Type != KVType.ARRAY && value.Type != KVType.ARRAY_TYPED)
                {
                    throw new InvalidOperationException($"Tried to cast non-array property {name} to array. Actual type: {value.Type}");
                }

                return ((KVObject)value.Value).Properties.Values.Select(v => (T)v.Value).ToArray();
            }
            else
            {
                return default(T[]);
            }
        }
    }
}
