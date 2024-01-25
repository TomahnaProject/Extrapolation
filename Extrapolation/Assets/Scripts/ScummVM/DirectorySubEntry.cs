using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Myst3
{
    public class DirectorySubEntry
    {
        public enum ResourceType {
                kCubeFace = 0,
                kWaterEffectMask = 1,
                kLavaEffectMask = 2,
                kMagneticEffectMask = 3,
                kShieldEffectMask = 4,
                kSpotItem = 5,
                kFrame = 6,
                kRawData = 7,
                kMovie = 8,
                kStillMovie = 10,
                kText = 11,
                kTextMetadata = 12,
                kNumMetadata = 13,
                kLocalizedSpotItem = 69,
                kLocalizedFrame = 70,
                kMultitrackMovie = 72,
                kDialogMovie = 74
            };


        public struct SpotItemData
        {
            public uint u;
            public uint v;
        };

        public struct VideoData
        {
            public Vector3 v1;
            public Vector3 v2;
            public int u;
            public int v;
            public int width;
            public int height;
        };


        uint offset;
        uint size;
        ushort metadataSize;
        byte face;
        ResourceType type;

        // Metadata
        SpotItemData spotItemData;
        VideoData videoData;
        uint[] miscData;

        Archive archive;

        public DirectorySubEntry(Archive archive)
        {
            this.archive = archive;
            miscData = new uint[22];
        }

        public void readFromStream(BinaryReader inStream)
        {
            offset = inStream.ReadUInt32();
            size = inStream.ReadUInt32();
            metadataSize = inStream.ReadUInt16();
            face = inStream.ReadByte();
            type = (ResourceType)inStream.ReadByte();

            if (metadataSize == 2 && (type == ResourceType.kSpotItem || type == ResourceType.kLocalizedSpotItem))
            {
                spotItemData.u = inStream.ReadUInt32();
                spotItemData.v = inStream.ReadUInt32();
            }
            else if (metadataSize == 10 && (type == ResourceType.kMovie || type == ResourceType.kMultitrackMovie))
            {
                videoData.v1 = new Vector3(
                    inStream.ReadInt32() * 0.000001f,
                    inStream.ReadInt32() * 0.000001f,
                    inStream.ReadInt32() * 0.000001f
                );
                videoData.v2 = new Vector3(
                    inStream.ReadInt32() * 0.000001f,
                    inStream.ReadInt32() * 0.000001f,
                    inStream.ReadInt32() * 0.000001f
                );

                videoData.u = inStream.ReadInt32();
                videoData.v = inStream.ReadInt32();
                videoData.width = inStream.ReadInt32();
                videoData.height = inStream.ReadInt32();
            }
            else if (type == ResourceType.kNumMetadata || type == ResourceType.kTextMetadata)
            {
                if (metadataSize > 20)
                {
                    Debug.LogWarning("Too much metadata, skipping");
                    inStream.BaseStream.Position += metadataSize * sizeof(uint);
                    return;
                }

                miscData[0] = offset;
                miscData[1] = size;

                for (uint i = 0; i < metadataSize; i++)
                    miscData[i + 2] = inStream.ReadUInt32();
            }
            else if (metadataSize != 0)
            {
                Debug.LogWarning("Metadata not read for type " + type + ", size " + metadataSize);
                inStream.BaseStream.Position += metadataSize * sizeof(uint);
            }
        }

        public MemoryStream getData()
        {
            return archive.dumpToMemory(offset, size);
        }

        public ushort getFace() { return face; }
        public ResourceType getType() { return type; }
    }
}
