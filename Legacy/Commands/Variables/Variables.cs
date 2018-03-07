using System;
using System.Collections.Generic;
using System.Text;
using WamWooWam.Core.Serialisation;

namespace Variables
{
    internal class Variables
    {
        internal static JsonDictionary<string, object> _variableDictionary = new JsonDictionary<string, object>("variables.json", true);

        public static bool VariableExists(string name) => _variableDictionary.ContainsKey(name);

        public static void SetVariable(string name, object value)
        {
            _variableDictionary[name] = value;
            _variableDictionary.Save();
        }

        public static bool GetVariable(string name, out object variable)
        {
            if(_variableDictionary.TryGetValue(name, out object obj))
            {
                variable = obj;
                return true;
            }

            variable = null;
            return false;
        }
        public static IDictionary<string, object> GetVariables() => _variableDictionary;

        public static void RemoveVariable(string name)
        {
            if (_variableDictionary.ContainsKey(name))
            {
                _variableDictionary.Remove(name);
            }
        }
    }
}
