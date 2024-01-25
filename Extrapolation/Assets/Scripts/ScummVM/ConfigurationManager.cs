using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Myst3
{
    public class ConfigurationManager
    {
        static Dictionary<string, int> config;

        public ConfigurationManager()
        {
            config = new Dictionary<string, int>();
        }

        public void registerDefault(string var, int value)
        {
            config[var] = value;
        }
        public void registerDefault(string var, bool value)
        {
            config[var] = value ? 1 : 0;
        }

        public int getInt(string var)
        {
            if (config.ContainsKey(var))
                return config[var];
            throw new Exception("Unknown variable " + var);
        }
    }
}
