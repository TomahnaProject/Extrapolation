using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Myst3
{
    public class NodeCube : Node
    {
        public NodeCube(Myst3 vm, ushort id) : base(vm, id)
        {
            // is3D = true;

            for (int i = 0; i < 6; i++) {
                DirectorySubEntry jpegDesc = vm.getFileDescription("", id, (ushort)(i + 1), DirectorySubEntry.ResourceType.kCubeFace);

                if (jpegDesc == null)
                    throw new Exception("Face " + id + " does not exist");

                faces[i] = new Face(vm);
                faces[i].setTextureFromJPEG(jpegDesc, vm.getNextNodeCubeTex(i), i);
            }
        }
    }
}
