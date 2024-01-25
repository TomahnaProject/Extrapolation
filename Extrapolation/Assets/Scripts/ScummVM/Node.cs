using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 414

namespace Myst3
{
    public class Face {
        // bool textureDirty;
        // Rect textureDirtyRect;
        Myst3 vm;

        public Texture2D texture;

        public Face(Myst3 vm)
        {
            this.vm = vm;
            // textureDirty = true;
            texture = null;
        }

        public void setTextureFromJPEG(DirectorySubEntry jpegDesc, Texture2D tex, int faceIndex)
        {
            vm.decodeJpeg(jpegDesc, tex, faceIndex);
            texture = tex;
            // addTextureDirtyRect(Common::Rect(_bitmap->w, _bitmap->h));
        }

        public void setTexture(Texture2D tex)
        {
            texture = tex;
            // addTextureDirtyRect(Common::Rect(_bitmap->w, _bitmap->h));
        }

        // public void addTextureDirtyRect(const Common::Rect &rect);
        // public bool isTextureDirty() { return _textureDirty; }

        // public void uploadTexture();
    };

    public class SpotItemFace {
        Face face;
        bool drawn;
        ushort fadeValue;
        ushort posX;
        ushort posY;

        // Graphics::Surface *_bitmap;
        // Graphics::Surface *_notDrawnBitmap;

        public SpotItemFace(Face face, ushort posX, ushort posY)
        {
            this.face = face;
            this.posX = posX;
            this.posY = posY;
            drawn = false;
            // bitmap = 0;
            // notDrawnBitmap = 0;
            fadeValue = 0;
        }
        // ~SpotItemFace();

        // void initBlack(uint16 width, uint16 height);
        // void loadData(const DirectorySubEntry *jpegDesc);
        // void updateData(const Graphics::Surface *surface);
        // void clear();

        // void draw();
        // void undraw();
        // void fadeDraw();

        // bool isDrawn() { return _drawn; }
        // void setDrawn(bool drawn) { _drawn = drawn; }
        // uint16 getFadeValue() { return _fadeValue; }
        // void setFadeValue(uint16 value) { _fadeValue = value; }

        // Rect getFaceRect();

        // void initNotDrawn(uint16 width, uint16 height)
        // {}
    };

    public class SpotItem {
        Myst3 vm;

        short condition;
        ushort fadeVar;
        bool enableFade;

        List<SpotItemFace> faces;


        public SpotItem(Myst3 vm)
        {
            faces = new List<SpotItemFace>();
        }
        // ~SpotItem();

        // void setCondition(int16 condition) { _condition = condition; }
        // void setFade(bool fade) { _enableFade = fade; }
        // void setFadeVar(uint16 var) { _fadeVar = var; }
        // void addFace(SpotItemFace *face) { _faces.push_back(face); }

        // void updateUndraw();
        // void updateDraw();
    };

    public class SunSpot {
        public ushort pitch;
        public ushort heading;
        public float intensity;
        public uint color;
        public ushort var;
        public bool variableIntensity;
        public float radius;
    };



    public class Node
    {
        // virtual bool isFaceVisible(uint faceId) = 0;

        protected Myst3 vm;
        public Face[] faces;
        // List<SpotItem> spotItems;
        // Subtitles subtitles;
        // List<Effect> effects;

        public Node(Myst3 vm, ushort id)
        {
            // TODO
            faces = new Face[6];
            // spotItems = new List<SpotItem>();
            // effects = new List<Effect>();
            this.vm = vm;

            if (vm.state.getVar("WaterEffects") != 0)
            {
                // Effect effect = WaterEffect.create(vm, id);
                // if (effect)
                // {
                    // effects.Add(effect);
                    // vm.state.setVar("WaterEffectActive", 1);
                // }
            }

            // Effect effect = MagnetEffect::create(vm, id);
            // if (effect) {
                // _effects.push_back(effect);
                // _vm->_state->setMagnetEffectActive(true);
            // }

            // effect = LavaEffect::create(vm, id);
            // if (effect) {
                // _effects.push_back(effect);
                // _vm->_state->setLavaEffectActive(true);
            // }

            // effect = ShieldEffect::create(vm, id);
            // if (effect) {
                // _effects.push_back(effect);
                // _vm->_state->setShieldEffectActive(true);
            // }
        }

        // virtual ~Node();

        // void update();
        // void drawOverlay();

        // void loadSpotItem(uint16 id, int16 condition, bool fade);
        // SpotItemFace *loadMenuSpotItem(int16 condition, const Common::Rect &rect);

        // void loadSubtitles(uint32 id);
        // bool hasSubtitlesToDraw();
    }
}
