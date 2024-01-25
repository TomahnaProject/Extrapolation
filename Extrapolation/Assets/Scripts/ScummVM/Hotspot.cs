using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Myst3
{
    public class Opcode {
        public byte op;
        public List<short> args;
    };

    public struct CondScript {
        public ushort condition;
        public List<Opcode> script;
    };

    public struct PolarRect {
        public short centerPitch;
        public short centerHeading;
        public short height;
        public short width;
    };

    public class HotSpot {
        public short condition = 0;
        public List<PolarRect> rects;
        public short cursor = 0;
        public List<Opcode> script;

        public int isPointInRectsCube(float pitch, float heading)
        {
            if (rects == null)
                return -1;

            // remap pitch and heading so they don't fall outside computable range
            pitch *= -1;
            while (heading < 0)     heading += 360;
            while (heading > 360)   heading -= 360;
            while (pitch < 90)      pitch += 180;
            while (pitch > 90)      pitch -= 180;

            int j=-1;
            foreach (PolarRect pRect in rects)
            {
                j++;
                Rect rect = new Rect(
                        pRect.centerHeading-pRect.width/2,
                        pRect.centerPitch-pRect.height/2,
                        pRect.width,
                        pRect.height);
                // Debug.Log(rect);

                // Make sure heading is in the correct range
                if (rect.xMax > 360 && heading <= rect.xMax - 360)
                    heading += 360;

                // Debug.Log(new Vector2(heading, pitch));

                if (pitch > rect.yMax || pitch < rect.yMin)
                {
                    // Pitch not in rect
                    // Debug.Log("Pitch not in rect");
                    continue;
                }

                // Debug.Log(new Vector2(rect.xMin, rect.xMax));
                if (heading > rect.xMax || heading < rect.xMin)
                {
                    // Heading not in rect
                    // Debug.Log("Heading not in rect");
                    continue;
                }

                // Point in rect
                return j;
            }

            return -1;
        }

        // public int isPointInRectsFrame(GameState state, Point p)
        // {
            // TODO
        // }

        public bool isEnabled(GameState state, ushort var)
        {
            if (!state.evaluate(condition))
                return false;

            if (isZip()) {
                // if (!ConfMan.getBool("zip_mode") || !isZipDestinationAvailable(state)) // TODO
                if (isZipDestinationAvailable(state))
                    return false;
            }

            if (var == 0)
                return cursor <= 13;
            else
                return cursor == var;
        }


        bool isZip() { return cursor == 7; }

        bool isZipDestinationAvailable(GameState state)
        {
            if (!(isZip() && script.Count != 0))
                throw new Exception("assert(isZip() && script.size() != 0)");

            ushort node;
            ushort room = (ushort)state.getVar("LocationRoom");
            int age = state.getVar("LocationAge");

            // Get the zip destination from the script
            Opcode op = script[0];
            switch (op.op) {
                case 140:
                case 142:
                    node = (ushort)op.args[0];
                    break;
                case 141:
                case 143:
                    node = (ushort)op.args[1];
                    room = (ushort)op.args[0];
                    break;
                default:
                    throw new Exception("Expected zip action");
            }

            return state.isZipDestinationAvailable(node, room, (uint)age);
        }
    };
}
