using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data.Common;

namespace Myst3
{
    public class FileManager
    {
        static string dataPath = null;
        static string dbPath = null;
        static Dictionary<string, string> knownFiles = new Dictionary<string, string>(); // filename: filepath+filename

        public static void Initialize(string path, string databasePath)
        {
            dataPath = path;
            dbPath = databasePath;

            foreach (string file in Directory.GetFiles(path))
                Debug.Log("found first level file: "+file);
            foreach (string file in Directory.GetDirectories(path))
                Debug.Log("found first level directory: "+file);

            parse(dataPath);
        }

        static void parse(string path)
        {
            foreach (string file in Directory.GetFiles(path))
                knownFiles[Path.GetFileName(file).ToLower()] = file;
            foreach (string directory in Directory.GetDirectories(path))
                parse(directory);
        }

        public static FileStream find(string filename)
        {
            if (dataPath == null)
                return null;

            // string onlyFilename = Path.GetFileName(filename);
            // if (onlyFilename != filename)
                // return Path.Combine(Path.Combine(dataPath, "m3exile"), filename);

            if (knownFiles.ContainsKey(filename.ToLower()))
                return File.Open(knownFiles[filename.ToLower()], FileMode.Open);

            return null;
        }
        public static bool hasFile(string filename)
        {
            if (dataPath == null)
                return false;

            return knownFiles.ContainsKey(filename.ToLower());
        }

        public static FileStream getDatabase()
        {
            if (dataPath == null)
                return null;

            string fullpath = dbPath;
            if (!fullpath.EndsWith("myst3.dat"))
                fullpath = Path.Combine(dbPath, "myst3.dat");
            if (File.Exists(fullpath))
                return File.Open(fullpath, FileMode.Open);
            return null;
        }

        public static FileStream getNodes(string filename)
        {
            string fullpath = Path.Combine(dataPath, "Data", filename);
            if (File.Exists(fullpath))
                return File.Open(fullpath, FileMode.Open);
            else if (hasFile(filename))
                return find(filename);
            return null;
        }

        /**
        * Add all members of the Archive matching the specified pattern to list.
        * Must only append to list, and not remove elements from it.
        *
        * @return the number of members added to list
        */
        public static string[] listMatchingMembers(string pattern)
        {
            string[] found = Directory.GetFiles(dataPath, pattern, SearchOption.AllDirectories);

            return found;
        }
    }
}

