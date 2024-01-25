using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Myst3
{
    public class DirectoryEntry
    {
        string roomName;
        uint index;
        List<DirectorySubEntry> subentries = new List<DirectorySubEntry>();
        Archive archive;

        public DirectoryEntry(Archive archive)
        { this.archive = archive; }

        public void readFromStream(BinaryReader inStream, string room)
        {
            if (room == null)
                roomName = new string(inStream.ReadChars(4));
            else
                roomName = room;

            // The index is stored as a 24-bits number
            index = inStream.ReadUInt16();
            index = (uint)((int)index | (inStream.ReadByte() << 16));

            byte subItemCount = inStream.ReadByte();

            subentries.Clear();
            for (uint i = 0; i < subItemCount; i++)
            {
                DirectorySubEntry subEntry = new DirectorySubEntry(archive);
                subEntry.readFromStream(inStream);
                subentries.Add(subEntry);
            }
        }

        public DirectorySubEntry getItemDescription(ushort face, DirectorySubEntry.ResourceType type)
        {
            for (int i = 0; i < subentries.Count; i++)
                if (subentries[i].getFace() == face && subentries[i].getType() == type)
                    return subentries[i];
            return null;
        }

        public List<DirectorySubEntry> listItemsMatching(ushort face, DirectorySubEntry.ResourceType type)
        {
            List<DirectorySubEntry> list = new List<DirectorySubEntry>();

            for (int i = 0; i < subentries.Count; i++)
                if (subentries[i].getFace() == face && subentries[i].getType() == type)
                    list.Add(subentries[i]);

            return list;
        }

        public uint getIndex() { return index; }
        public string getRoom() { return roomName; }
    }
}
