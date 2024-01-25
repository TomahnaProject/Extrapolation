using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Myst3
{
    public class Archive
    {
        public string roomName {get; private set; }

        bool multipleRoom;
        FileStream file;
        BinaryReader fileBR;
        List<DirectoryEntry> directories;

        public Archive()
        {
            file = null;
            fileBR = null;
            directories = new List<DirectoryEntry>();
        }

        public bool open(string fileName, string room)
        {
            // Copy the room name if provided
            // If the room name is not provided, it is assumed that
            // we are opening a multi-room archive
            multipleRoom = (room == null);
            if (room != null)
                roomName = room;

            file = FileManager.getNodes(fileName);
            if (file != null)
            {
                fileBR = new BinaryReader(file);
                readDirectory();
                return true;
            }

            return false;
        }

        public void close()
        {
            if (fileBR != null)
                ((IDisposable)fileBR).Dispose();
            if (file != null)
                file.Close();
            file = null;
            directories.Clear();
        }

        ~Archive()
        {
            close();
        }

        void decryptHeader(BinaryWriter outStream)
        {
            uint addKey = 0x3C6EF35F;
            uint multKey = 0x0019660D;

            uint size = fileBR.ReadUInt32();
            bool encrypted = size > 1000000;

            file.Position = 0;

            if (encrypted)
            {
                uint decryptedSize = size ^ addKey;

                uint currentKey = 0;
                for (uint i = 0; i < decryptedSize; i++)
                {
                    currentKey += addKey;
                    outStream.Write((uint)(fileBR.ReadUInt32() ^ currentKey));
                    currentKey *= multKey;
                }
            }
            else
            {
                for (uint i = 0; i < size; i++)
                    outStream.Write((uint)(fileBR.ReadUInt32()));
            }
        }

        void readDirectory()
        {
            MemoryStream directory = new MemoryStream();
            using (BinaryWriter dirWriter = new BinaryWriter(directory))
            {
                decryptHeader(dirWriter);
                directory.Position = sizeof(uint);
                using (BinaryReader dirReader = new BinaryReader(directory))
                {
                    while (directory.Position + 4 < directory.Length)
                    {
                        DirectoryEntry entry = new DirectoryEntry(this);

                        if (multipleRoom)
                            entry.readFromStream(dirReader, null);
                        else
                            entry.readFromStream(dirReader, roomName);

                        directories.Add(entry);
                    }
                }
            }
        }

        public DirectorySubEntry getDescription(string room, uint index, ushort face, DirectorySubEntry.ResourceType type)
        {
            for (int i = 0; i < directories.Count; i++)
                if (directories[i].getIndex() == index && directories[i].getRoom() == room)
                    return directories[i].getItemDescription(face, type);

            return null;
        }

        public MemoryStream dumpToMemory(uint offset, uint size)
        {
            file.Position = offset;
            byte[] buf = new byte[size];
            file.Read(buf, 0, (int)size);
            return new MemoryStream(buf);
        }

    }
}
