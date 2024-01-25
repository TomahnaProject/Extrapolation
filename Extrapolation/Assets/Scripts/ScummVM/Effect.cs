using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Myst3
{
    public class Effect {
        /*
        public class FaceMask {
            FaceMask();
            ~FaceMask();

            static Rect getBlockRect(uint x, uint y);

            Graphics::Surface *surface;
            bool block[10][10];
        };

        virtual ~Effect();

        virtual bool update() = 0;
        virtual void applyForFace(uint face, Graphics::Surface *src, Graphics::Surface *dst) = 0;

        bool hasFace(uint face) { return _facesMasks.contains(face); }
        Common::Rect getUpdateRectForFace(uint face);

        // Public and static for use by the debug console
        static FaceMask *loadMask(Common::SeekableReadStream *maskStream);

    protected:
        Effect(Myst3Engine *vm);

        bool loadMasks(const Common::String &room, uint32 id, DirectorySubEntry::ResourceType type);

        Myst3Engine *_vm;

        typedef Common::HashMap<uint, FaceMask *> FaceMaskMap;
        FaceMaskMap _facesMasks;
        //*/

    }
}
