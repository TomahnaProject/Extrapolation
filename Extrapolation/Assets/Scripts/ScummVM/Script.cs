using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 414

namespace Myst3
{
    /**
    * Scripting engine for Myst 3.
    * M3's scriptengine is actually a list of command ids along with parameters. Command ids reference a builtin method of the scripting engine.
    * (Come on, wasn't integrating an existing scripting engine simpler ?...)
    */
    public class Script
    {
        struct Context {
            public bool endScript;
            public bool result;
            public List<Opcode> script;
            public int op;
        };
        enum ArgumentType {
            kUnknown   = 'u',
            kVar       = 'v',
            kValue     = 'i',
            kEvalValue = 'e',
            kCondition = 'c'
        };
        class Command
        {
            public Command(ushort o, Action<Context, Opcode> p, string d, string s)
            {
                op = o;
                proc = p;
                desc = d;
                signature = s;
            }
            public Command(ushort o, Action<Context, Opcode> p, string s)
            {
                op = o;
                proc = p;
                desc = "";
                signature = s;
            }

            public ushort op = 0;
            public Action<Context, Opcode> proc = null;
            public string desc = null;
            public string signature = null;
        };

        IMyst3Engine vm;
        Puzzles puzzles;
        List<Command> commands;

        public Script(IMyst3Engine vm)
        {
            this.vm = vm;
            puzzles = new Puzzles(vm);
            commands = new List<Command>();

            commands.Add(new Command(  0, badOpcode,                                ""      ));
            commands.Add(new Command(  4, nodeCubeInit,                             "e"     ));
            commands.Add(new Command(  6, nodeCubeInitIndex,                        "veeee" ));
            // commands.Add(new Command(  7, nodeFrameInit,                            "e"     ));
            // commands.Add(new Command(  8, nodeFrameInitCond,                        "cee"   ));
            // commands.Add(new Command(  9, nodeFrameInitIndex,                       "veeee" ));
            // commands.Add(new Command( 10, nodeMenuInit,                             "e"     ));
            // commands.Add(new Command( 11, stopWholeScript,                          ""      ));
            // commands.Add(new Command( 13, spotItemAdd,                              "i"     ));
            // commands.Add(new Command( 14, spotItemAddCond,                          "ic"    ));
            // commands.Add(new Command( 15, spotItemAddCondFade,                      "ic"    ));
            // commands.Add(new Command( 16, spotItemAddMenu,                          "iciii" )); // Six args
            commands.Add(new Command( 17, movieInitLooping,                         "e"     ));
            commands.Add(new Command( 18, movieInitCondLooping,                     "ec"    ));
            commands.Add(new Command( 19, movieInitCond,                            "ec"    ));
            commands.Add(new Command( 20, movieInitPreloadLooping,                  "e"     ));
            commands.Add(new Command( 21, movieInitCondPreloadLooping,              "ec"    ));
            commands.Add(new Command( 22, movieInitCondPreload,                     "ec"    ));
            commands.Add(new Command( 23, movieInitFrameVar,                        "ev"    ));
            commands.Add(new Command( 24, movieInitFrameVarPreload,                 "ev"    ));
            commands.Add(new Command( 25, movieInitOverrridePosition,               "ecii"  ));
            commands.Add(new Command( 26, movieInitScriptedPosition,                "evv"   ));
            commands.Add(new Command( 27, movieRemove,                              "e"     ));
            commands.Add(new Command( 28, movieRemoveAll,                           ""      ));
            commands.Add(new Command( 29, movieSetLooping,                          "i"     ));
            commands.Add(new Command( 30, movieSetNotLooping,                       "i"     ));
            commands.Add(new Command( 31, waterEffectSetSpeed,                      "i"     ));
            commands.Add(new Command( 32, waterEffectSetAttenuation,                "i"     ));
            commands.Add(new Command( 33, waterEffectSetWave,                       "ii"    ));
            // commands.Add(new Command( 34, shakeEffectSet,                           "ee"    ));
            commands.Add(new Command( 35, sunspotAdd,                               "ii"    ));
            commands.Add(new Command( 36, sunspotAddIntensity,                      "iii"   ));
            commands.Add(new Command( 37, sunspotAddVarIntensity,                   "iiiv"  ));
            commands.Add(new Command( 38, sunspotAddIntensityColor,                 "iiii"  ));
            commands.Add(new Command( 39, sunspotAddVarIntensityColor,              "iiiiv" ));
            commands.Add(new Command( 40, sunspotAddIntensityRadius,                "iiii"  ));
            commands.Add(new Command( 41, sunspotAddVarIntensityRadius,             "iiivi" ));
            commands.Add(new Command( 42, sunspotAddIntColorRadius,                 "iiiii" ));
            commands.Add(new Command( 43, sunspotAddVarIntColorRadius,              "iiiiv" )); // Six args
            // commands.Add(new Command( 44, inventoryAddFront,                        "vi"    ));
            // commands.Add(new Command( 45, inventoryAddBack,                         "vi"    ));
            // commands.Add(new Command( 46, inventoryRemove,                          "v"     ));
            // commands.Add(new Command( 47, inventoryReset,                           ""      ));
            // commands.Add(new Command( 48, inventoryAddSaavChapter,                  "v"     ));
            commands.Add(new Command( 49, varSetZero,                               "v"     ));
            commands.Add(new Command( 50, varSetOne,                                "v"     ));
            commands.Add(new Command( 51, varSetTwo,                                "v"     ));
            commands.Add(new Command( 52, varSetOneHundred,                         "v"     ));
            commands.Add(new Command( 53, varSetValue,                              "vi"    ));
            commands.Add(new Command( 54, varToggle,                                "v"     ));
            commands.Add(new Command( 55, varSetOneIfNotZero,                       "v"     ));
            commands.Add(new Command( 56, varOpposite,                              "v"     ));
            commands.Add(new Command( 57, varAbsolute,                              "v"     ));
            commands.Add(new Command( 58, varDereference,                           "v"     ));
            commands.Add(new Command( 59, varReferenceSetZero,                      "v"     ));
            commands.Add(new Command( 60, varReferenceSetValue,                     "vi"    ));
            commands.Add(new Command( 61, varRandRange,                             "vii"   ));
            commands.Add(new Command( 62, polarToRectSimple,                        "vviii" )); // Seven args
            commands.Add(new Command( 63, polarToRect,                              "vviii" )); // Ten args
            // commands.Add(new Command( 64, varSetDistanceToZone,                     "viii"  ));
            // commands.Add(new Command( 65, varSetMinDistanceToZone,                  "viii"  ));
            commands.Add(new Command( 67, varRemoveBits,                            "vi"    ));
            commands.Add(new Command( 68, varToggleBits,                            "vi"    ));
            commands.Add(new Command( 69, varCopy,                                  "vv"    ));
            commands.Add(new Command( 70, varSetBitsFromVar,                        "vv"    ));
            commands.Add(new Command( 71, varSetBits,                               "vi"    ));
            commands.Add(new Command( 72, varApplyMask,                             "vi"    ));
            commands.Add(new Command( 73, varSwap,                                  "vv"    ));
            commands.Add(new Command( 74, varIncrement,                             "v"     ));
            commands.Add(new Command( 75, varIncrementMax,                          "vi"    ));
            commands.Add(new Command( 76, varIncrementMaxLooping,                   "vii"   ));
            commands.Add(new Command( 77, varAddValueMaxLooping,                    "ivii"  ));
            commands.Add(new Command( 78, varDecrement,                             "v"     ));
            commands.Add(new Command( 79, varDecrementMin,                          "vi"    ));
            commands.Add(new Command( 80, varAddValueMax,                           "ivi"   ));
            commands.Add(new Command( 81, varSubValueMin,                           "ivi"   ));
            commands.Add(new Command( 82, varZeroRange,                             "vv"    ));
            commands.Add(new Command( 83, varCopyRange,                             "vvi"   ));
            commands.Add(new Command( 84, varSetRange,                              "vvi"   ));
            commands.Add(new Command( 85, varIncrementMaxTen,                       "v"     ));
            commands.Add(new Command( 86, varAddValue,                              "iv"    ));
            commands.Add(new Command( 87, varArrayAddValue,                         "ivv"   ));
            commands.Add(new Command( 88, varAddVarValue,                           "vv"    ));
            commands.Add(new Command( 89, varSubValue,                              "iv"    ));
            commands.Add(new Command( 90, varSubVarValue,                           "vv"    ));
            commands.Add(new Command( 91, varModValue,                              "vi"    ));
            commands.Add(new Command( 92, varMultValue,                             "vi"    ));
            commands.Add(new Command( 93, varMultVarValue,                          "vv"    ));
            commands.Add(new Command( 94, varDivValue,                              "vi"    ));
            commands.Add(new Command( 95, varDivVarValue,                           "vv"    ));
            commands.Add(new Command( 96, varCrossMultiplication,                   "viiii" ));
            commands.Add(new Command( 97, varMinValue,                              "vi"    ));
            commands.Add(new Command( 98, varClipValue,                             "vii"   ));
            commands.Add(new Command( 99, varClipChangeBound,                       "vii"   ));
            commands.Add(new Command(100, varAbsoluteSubValue,                      "vi"    ));
            commands.Add(new Command(101, varAbsoluteSubVar,                        "vv"    ));
            commands.Add(new Command(102, varRatioToPercents,                       "vii"   ));
            commands.Add(new Command(103, varRotateValue3,                          "viii"  ));
            commands.Add(new Command(104, ifElse,                                   ""      ));
            commands.Add(new Command(105, ifCondition,                              "c"     ));
            commands.Add(new Command(106, ifCond1AndCond2,                          "cc"    ));
            commands.Add(new Command(107, ifCond1OrCond2,                           "cc"    ));
            commands.Add(new Command(108, ifOneVarSetInRange,                       "vv"    ));
            commands.Add(new Command(109, ifVarEqualsValue,                         "vi"    ));
            commands.Add(new Command(110, ifVarNotEqualsValue,                      "vi"    ));
            commands.Add(new Command(111, ifVar1EqualsVar2,                         "vv"    ));
            commands.Add(new Command(112, ifVar1NotEqualsVar2,                      "vv"    ));
            commands.Add(new Command(113, ifVarSupEqValue,                          "vi"    ));
            commands.Add(new Command(114, ifVarInfEqValue,                          "vi"    ));
            commands.Add(new Command(115, ifVarInRange,                             "vii"   ));
            commands.Add(new Command(116, ifVarNotInRange,                          "vii"   ));
            commands.Add(new Command(117, ifVar1SupEqVar2,                          "vv"    ));
            commands.Add(new Command(118, ifVar1SupVar2,                            "vv"    ));
            commands.Add(new Command(119, ifVar1InfEqVar2,                          "vv"    ));
            commands.Add(new Command(120, ifVarHasAllBitsSet,                       "vi"    ));
            commands.Add(new Command(121, ifVarHasNoBitsSet,                        "vi"    ));
            commands.Add(new Command(122, ifVarHasSomeBitsSet,                      "vii"   ));
            // commands.Add(new Command(123, ifHeadingInRange,                         "ii"    ));
            // commands.Add(new Command(124, ifPitchInRange,                           "ii"    ));
            // commands.Add(new Command(125, ifHeadingPitchInRect,                     "iiii"  ));
            // commands.Add(new Command(126, ifMouseIsInRect,                          "iiii"  ));
            // commands.Add(new Command(127, leverDrag,                                "iiiiv" )); // Six args
            // commands.Add(new Command(130, leverDragXY,                              "vviii" ));
            // commands.Add(new Command(131, itemDrag,                                 "viiiv" ));
            // commands.Add(new Command(132, leverDragPositions,                       "vi"    )); // Variable args
            // commands.Add(new Command(134, runScriptWhileDragging,                   "vviiv" )); // Eight args
            // commands.Add(new Command(135, chooseNextNode,                           "cii"   ));
            commands.Add(new Command(136, goToNodeTransition,                       "ii"    ));
            commands.Add(new Command(137, goToNodeTrans2,                           "i"     ));
            commands.Add(new Command(138, goToNodeTrans1,                           "i"     ));
            commands.Add(new Command(139, goToRoomNode,                             "ii"    ));
            // commands.Add(new Command(140, zipToNode,                                "i"     ));
            // commands.Add(new Command(141, zipToRoomNode,                            "ii"    ));
            // commands.Add(new Command(144, drawTransition,                           ""      ));
            // commands.Add(new Command(145, reloadNode,                               ""      ));
            // commands.Add(new Command(146, redrawFrame,                              ""      ));
            // commands.Add(new Command(147, moviePlay,                                "e"     ));
            // commands.Add(new Command(148, moviePlaySynchronized,                    "e"     ));
            // commands.Add(new Command(149, moviePlayFullFrame,                       "e"     ));
            // commands.Add(new Command(150, moviePlayFullFrameTrans,                  "e"     ));
            // commands.Add(new Command(151, moviePlayChangeNode,                      "ee"    ));
            commands.Add(new Command(152, moviePlayChangeNodeTrans,                 "ee"    ));
            // commands.Add(new Command(153, lookAt,                                   "ii"    ));
            // commands.Add(new Command(154, lookAtInXFrames,                          "iii"   ));
            // commands.Add(new Command(155, lookAtMovieStart,                         "e"     ));
            // commands.Add(new Command(156, lookAtMovieStartInXFrames,                "ei"    ));
            // commands.Add(new Command(157, cameraLimitMovement,                      "iiii"  ));
            commands.Add(new Command(158, cameraFreeMovement,                       ""      ));
            // commands.Add(new Command(159, cameraLookAt,                             "ii"    ));
            // commands.Add(new Command(160, cameraLookAtVar,                          "v"     ));
            // commands.Add(new Command(161, cameraGetLookAt,                          "v"     ));
            // commands.Add(new Command(162, lookAtMovieStartImmediate,                "e"     ));
            commands.Add(new Command(163, cameraSetFOV,                             "e"     ));
            // commands.Add(new Command(164, changeNode,                               "i"     ));
            // commands.Add(new Command(165, changeNodeRoom,                           "ii"    ));
            // commands.Add(new Command(166, changeNodeRoomAge,                        "iii"   ));
            commands.Add(new Command(168, uselessOpcode,                            ""      ));
            // commands.Add(new Command(169, drawXTicks,                               "i"     ));
            // commands.Add(new Command(171, drawWhileCond,                            "c"     ));
            // commands.Add(new Command(172, whileStart,                               "c"     ));
            // commands.Add(new Command(173, whileEnd,                                 ""      ));
            // commands.Add(new Command(174, runScriptWhileCond,                       "ci"    ));
            // commands.Add(new Command(175, runScriptWhileCondEachXFrames,            "cii"   ));
            // commands.Add(new Command(176, runScriptForVar,                          "viii"  ));
            // commands.Add(new Command(177, runScriptForVarEachXFrames,               "viiii" ));
            // commands.Add(new Command(178, runScriptForVarStartVar,                  "vvii"  ));
            // commands.Add(new Command(179, runScriptForVarStartVarEachXFrames,       "vviii" ));
            // commands.Add(new Command(180, runScriptForVarEndVar,                    "vivi"  ));
            // commands.Add(new Command(181, runScriptForVarEndVarEachXFrames,         "vivii" ));
            // commands.Add(new Command(182, runScriptForVarStartEndVar,               "vvvi"  ));
            // commands.Add(new Command(183, runScriptForVarStartEndVarEachXFrames,    "vvvii" ));
            // commands.Add(new Command(184, drawFramesForVar,                         "viii"  ));
            // commands.Add(new Command(185, drawFramesForVarEachTwoFrames,            "vii"   ));
            // commands.Add(new Command(186, drawFramesForVarStartEndVarEachTwoFrames, "vvv"   ));
            // commands.Add(new Command(187, runScript,                                "e"     ));
            // commands.Add(new Command(188, runScriptWithVar,                         "ei"    ));
            commands.Add(new Command(189, runCommonScript,                          "i"     ));
            // commands.Add(new Command(190, runCommonScriptWithVar,                   "ei"    ));
            // commands.Add(new Command(194, runPuzzle1,                               "i"     ));
            // commands.Add(new Command(195, runPuzzle2,                               "ii"    ));
            // commands.Add(new Command(196, runPuzzle3,                               "iii"   ));
            // commands.Add(new Command(197, runPuzzle4,                               "iiii"  ));
            // commands.Add(new Command(198, ambientLoadNode,                          "iii"   ));
            // commands.Add(new Command(199, ambientReloadCurrentNode,                 "e"     ));
            // commands.Add(new Command(200, ambientPlayCurrentNode,                   "ii"    ));
            // commands.Add(new Command(201, ambientApply,                             ""      ));
            // commands.Add(new Command(202, ambientApplyWithFadeDelay,                "e"     ));
            // commands.Add(new Command(203, soundPlayBadClick,                        ""      ));
            // commands.Add(new Command(204, soundPlayBlocking,                        "eeeei" ));
            // commands.Add(new Command(205, soundPlay,                                "e"     ));
            // commands.Add(new Command(206, soundPlayVolume,                          "ee"    ));
            // commands.Add(new Command(207, soundPlayVolumeDirection,                 "eee"   ));
            // commands.Add(new Command(208, soundPlayVolumeDirectionAtt,              "eeee"  ));
            // commands.Add(new Command(209, soundStopEffect,                          "e"     ));
            // commands.Add(new Command(210, soundFadeOutEffect,                       "ee"    ));
            // commands.Add(new Command(212, soundPlayLooping,                         "e"     ));
            // commands.Add(new Command(213, soundPlayFadeInOut,                       "eeeee" ));
            // commands.Add(new Command(214, soundChooseNext,                          "viiee" ));
            // commands.Add(new Command(215, soundRandomizeNext,                       "viiee" ));
            // commands.Add(new Command(216, soundChooseNextAfterOther,                "viiee" )); // Seven args
            // commands.Add(new Command(217, soundRandomizeNextAfterOther,             "viiee" )); // Seven args
            // commands.Add(new Command(218, ambientSetFadeOutDelay,                   "i"     ));
            // commands.Add(new Command(219, ambientAddSound1,                         "ee"    ));
            // commands.Add(new Command(220, ambientAddSound2,                         "eei"   ));
            // commands.Add(new Command(222, ambientAddSound3,                         "eei"   ));
            // commands.Add(new Command(223, ambientAddSound4,                         "eeii"  ));
            // commands.Add(new Command(224, ambientAddSound5,                         "eee"   ));
            // commands.Add(new Command(225, ambientSetCue1,                           "ie"    ));
            // commands.Add(new Command(226, ambientSetCue2,                           "iei"   ));
            // commands.Add(new Command(227, ambientSetCue3,                           "ieii"  ));
            // commands.Add(new Command(228, ambientSetCue4,                           "ie"    ));
            // commands.Add(new Command(229, runAmbientScriptNode,                     "e"     ));
            // commands.Add(new Command(230, runAmbientScriptNodeRoomAge,              "eeee"  ));
            // commands.Add(new Command(231, runSoundScriptNode,                       "e"     ));
            // commands.Add(new Command(232, runSoundScriptNodeRoom,                   "ee"    ));
            // commands.Add(new Command(233, runSoundScriptNodeRoomAge,                "eee"   ));
            // commands.Add(new Command(234, soundStopMusic,                           "e"     ));
            // commands.Add(new Command(235, movieSetStartupSound,                     "e"     ));
            // commands.Add(new Command(236, movieSetStartupSoundVolume,               "ee"    ));
            // commands.Add(new Command(237, movieSetStartupSoundVolumeH,              "eee"   ));
            // commands.Add(new Command(239, drawOneFrame,                             ""      ));
            // commands.Add(new Command(240, cursorHide,                               ""      ));
            // commands.Add(new Command(241, cursorShow,                               ""      ));
            // commands.Add(new Command(242, cursorSet,                                "i"     ));
            // commands.Add(new Command(243, cursorLock,                               ""      ));
            // commands.Add(new Command(244, cursorUnlock,                             ""      ));
            // commands.Add(new Command(248, dialogOpen,                               "e"     ));
            // commands.Add(new Command(249, newGame,                                  ""      ));

            if (vm.getPlatform() == Platform.kPlatformXbox)
            {
                // The Xbox version inserted two new opcodes, one at position
                // 27, the other at position 77, shifting all the other opcodes
                shiftCommands(77, 1);
                // commands.Add(new Command(77, varDecrementMinLooping, "vii"));

                shiftCommands(27, 1);
                // commands.Add(new Command(27, movieInitCondScriptedPosition, "ecvv"));
            }
        }

        void log(string msg)
        {
            // Debug.Log(msg); // comment to avoid log spamming
        }

        Command findCommandByProc(Action<Context, Opcode> proc)
        {
            for (ushort i = 0; i < commands.Count; i++)
                if (commands[i].proc == proc)
                    return commands[i];

            // Return the invalid opcode if not found
            return findCommand(0);
        }

        void shiftCommands(ushort _base, int value)
        {
            for (int i = 0; i < commands.Count; i++)
                if (commands[i].op >= _base)
                    commands[i].op += (ushort)value;
        }

        public bool run(List<Opcode> script)
        {
            Context c;
            c.result = true;
            c.endScript = false;
            c.script = script;
            c.op = 0;

            while (c.op != script.Count)
            {
                runOp(c, script[c.op]);

                if (c.endScript || c.op == script.Count)
                    break;

                c.op++;
            }

            return c.result;
        }

        Command findCommand(ushort op)
        {
            foreach (Command c in commands)
                if (c.op == op)
                    return c;

            // Return the invalid opcode if not found
            return findCommand(0);
        }

        void runOp(Context c, Opcode op)
        {
            Command cmd = findCommand(op.op);

            if (cmd.op != 0)
                cmd.proc(c, op);
            else
                Debug.LogError("Trying to run invalid opcode " + op.op);
        }


        // ---------------------------------------------------------------------------
        // scripting functions

        void badOpcode(Context c, Opcode cmd)
        {
            Debug.LogError("Opcode " + cmd.op + ": Invalid opcode");
        }


        void uselessOpcode(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Useless opcode", cmd.op));

            // List of useless opcodes

            // 167 and 168 form a pair. 168 resets what 167 sets up.
            // Since 167 is never used, 168 is marked as useless.
        }


        void nodeCubeInit(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Node cube init {1}", cmd.op, cmd.args[0]));

            ushort nodeId = (ushort)vm.state.valueOrVarValue(cmd.args[0]);
            vm.loadNodeCubeFaces(nodeId);
        }

        void nodeCubeInitIndex(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Node cube init indexed {1}", cmd.op, cmd.args[0]));

            ushort var = (ushort)vm.state.getVar((ushort)cmd.args[0]);

            if (var >= cmd.args.Count - 1)
                throw new Exception(string.Format("Opcode {0}, invalid index {1}", cmd.op, var));

            ushort value = (ushort)cmd.args[var + 1];

            ushort nodeId = (ushort)vm.state.valueOrVarValue(value);
            vm.loadNodeCubeFaces(nodeId);
        }

        /*

        void nodeFrameInit(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Node frame init {1}", cmd.op, cmd.args[0]));

            ushort nodeId = (ushort)vm.state.valueOrVarValue(cmd.args[0]);
            vm.loadNodeFrame((ushort)nodeId);
        }

        void nodeFrameInitCond(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Node frame init condition {1} ? {2} : {3}",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            ushort value;
            if (vm.state.evaluate(cmd.args[0]))
                value = (ushort)cmd.args[1];
            else
                value = (ushort)cmd.args[2];

            ushort nodeId = (ushort)vm.state.valueOrVarValue(value);
            vm.loadNodeFrame((ushort)nodeId);
        }

        void nodeFrameInitIndex(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Node frame init indexed {1}",
                    cmd.op, cmd.args[0]));

            ushort var = (ushort)vm.state.getVar((ushort)cmd.args[0]);

            if (var >= cmd.args.Count - 1)
                throw new Exception(string.Format("Opcode {0}, invalid index {1}", cmd.op, var));

            ushort value = (ushort)cmd.args[var + 1];

            ushort nodeId = (ushort)vm.state.valueOrVarValue((ushort)value);
            vm.loadNodeFrame(nodeId);
        }

        void nodeMenuInit(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Node menu init {1}", cmd.op, cmd.args[0]));

            ushort nodeId = (ushort)vm.state.valueOrVarValue((ushort)cmd.args[0]);
            vm.loadNodeMenu(nodeId);
        }

        void stopWholeScript(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Stop whole script", cmd.op));

            c.result = false;
            c.endScript = true;
        }

        void spotItemAdd(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Draw spotitem {1}", cmd.op, cmd.args[0]));

            vm.addSpotItem(cmd.args[0], 1, false);
        }

        void spotItemAddCond(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add spotitem {1} with condition {2}", cmd.op, cmd.args[0], cmd.args[1]));

            vm.addSpotItem(cmd.args[0], cmd.args[1], false);
        }

        void spotItemAddCondFade(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add fading spotitem {1} for var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            vm.addSpotItem(cmd.args[0], cmd.args[1], true);
        }

        void spotItemAddMenu(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add menu spotitem {1} with condition {2}", cmd.op, cmd.args[0], cmd.args[1]));

            Rect rect = new Rect(cmd.args[2], cmd.args[3], cmd.args[4], cmd.args[5]);

            vm.addMenuSpotItem(cmd.args[0], cmd.args[1], rect);
        }

        //*/

        void movieInitLooping(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Init movie {1}, looping", cmd.op, cmd.args[0]));

            ushort movieid = (ushort)vm.state.valueOrVarValue((ushort)cmd.args[0]);
            vm.loadMovie(movieid, 1, false, true);
        }

        void movieInitCondLooping(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Init movie {1} with condition {2}, looping", cmd.op, cmd.args[0], cmd.args[1]));

            ushort movieid = (ushort)vm.state.valueOrVarValue((ushort)cmd.args[0]);
            vm.loadMovie(movieid, (ushort)cmd.args[1], false, true);
        }

        void movieInitCond(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Init movie {1} with condition {2}", cmd.op, cmd.args[0], cmd.args[1]));

            ushort movieid = (ushort)vm.state.valueOrVarValue((ushort)cmd.args[0]);
            vm.loadMovie(movieid, (ushort)cmd.args[1], true, false);
        }

        void movieInitPreloadLooping(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Preload movie {1}, looping", cmd.op, cmd.args[0]));

            vm.state.setVar("MoviePreloadToMemory", true);

            ushort movieid = (ushort)vm.state.valueOrVarValue((ushort)cmd.args[0]);
            vm.loadMovie(movieid, 1, false, true);
        }

        void movieInitCondPreloadLooping(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Preload movie {1} with condition {2}, looping", cmd.op, cmd.args[0], cmd.args[1]));

            vm.state.setVar("MoviePreloadToMemory", true);

            ushort movieid = (ushort)vm.state.valueOrVarValue((ushort)cmd.args[0]);
            vm.loadMovie(movieid, (ushort)cmd.args[1], false, true);
        }

        void movieInitCondPreload(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Preload movie {1} with condition {2}", cmd.op, cmd.args[0], cmd.args[1]));

            vm.state.setVar("MoviePreloadToMemory", 1);

            ushort movieid = (ushort)vm.state.valueOrVarValue((ushort)cmd.args[0]);
            vm.loadMovie(movieid, (ushort)cmd.args[1], true, false);
        }

        void movieInitFrameVar(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Init movie {1} with next frame var {2}",
                    cmd.op, cmd.args[0], cmd.args[1]));

            vm.state.setVar("MovieScriptDriven", true);
            vm.state.setVar("MovieNextFrameGetVar", cmd.args[1]);

            int condition = vm.state.getVar("MovieOverrideCondition");
            vm.state.setVar("MovieOverrideCondition", 0);

            if (condition == 0)
                condition = 1;

            ushort movieid = (ushort)vm.state.valueOrVarValue((ushort)cmd.args[0]);
            vm.loadMovie(movieid, (ushort)condition, false, true);
        }

        void movieInitFrameVarPreload(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Preload movie {1} with next frame var {2}",
                    cmd.op, cmd.args[0], cmd.args[1]));

            vm.state.setVar("MoviePreloadToMemory", true);
            vm.state.setVar("MovieScriptDriven", true);
            vm.state.setVar("MovieNextFrameGetVar", cmd.args[1]);

            int condition = vm.state.getVar("MovieOverrideCondition");
            vm.state.setVar("MovieOverrideCondition", 0);

            if (condition == 0)
                condition = 1;

            ushort movieid = (ushort)vm.state.valueOrVarValue((ushort)cmd.args[0]);
            vm.loadMovie(movieid, (ushort)condition, false, true);
        }

        void movieInitOverrridePosition(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Preload movie {1} with condition {2} and position U {3} V {4}",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]));

            vm.state.setVar("MoviePreloadToMemory", true);
            vm.state.setVar("MovieScriptDriven", true);
            vm.state.setVar("MovieOverridePosition", true);
            vm.state.setVar("MovieOverridePosU", cmd.args[2]);
            vm.state.setVar("MovieOverridePosV", cmd.args[3]);

            ushort movieid = (ushort)vm.state.valueOrVarValue((ushort)cmd.args[0]);
            vm.loadMovie(movieid, (ushort)cmd.args[1], false, true);
        }

        void movieInitScriptedPosition(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Preload movie {1} with position U-var {2} V-var {3}",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            vm.state.setVar("MoviePreloadToMemory", true);
            vm.state.setVar("MovieScriptDriven", true);
            vm.state.setVar("MovieUVar", cmd.args[1]);
            vm.state.setVar("MovieVVar", cmd.args[2]);

            ushort movieid = (ushort)vm.state.valueOrVarValue(cmd.args[0]);
            vm.loadMovie(movieid, 1, false, true);
        }

        void movieInitCondScriptedPosition(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Preload movie {1} with condition {2}, position U-var {3} V-var {4}",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]));

            vm.state.setVar("MoviePreloadToMemory", true);
            vm.state.setVar("MovieScriptDriven", true);
            vm.state.setVar("MovieUVar", cmd.args[2]);
            vm.state.setVar("MovieVVar", cmd.args[3]);

            ushort movieid = (ushort)vm.state.valueOrVarValue(cmd.args[0]);
            vm.loadMovie(movieid, (ushort)cmd.args[1], false, true);
        }

        void movieRemove(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Remove movie {1} ",
                    cmd.op, cmd.args[0]));

            ushort movieid = (ushort)vm.state.valueOrVarValue(cmd.args[0]);
            vm.removeMovie(movieid);
        }

        void movieRemoveAll(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Remove all movies",
                    cmd.op));

            vm.removeMovie(0);
        }

        void movieSetLooping(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set movie {0} to loop",
                    cmd.op, cmd.args[0]));

            vm.setMovieLooping((ushort)cmd.args[0], true);
        }

        void movieSetNotLooping(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set movie {1} not to loop",
                    cmd.op, cmd.args[0]));

            vm.setMovieLooping((ushort)cmd.args[0], false);
        }

        void waterEffectSetSpeed(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set water effect speed to {1}", cmd.op, cmd.args[0]));

            vm.state.setVar("WaterEffectSpeed", cmd.args[0]);
        }

        void waterEffectSetAttenuation(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set water effect attenuation to {1}", cmd.op, cmd.args[0]));

            vm.state.setVar("WaterEffectAttenuation", cmd.args[0]);
        }

        void waterEffectSetWave(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set water effect frequency to {1} and amplitude to {2}", cmd.op, cmd.args[0], cmd.args[1]));

            vm.state.setVar("WaterEffectFrequency", cmd.args[0]);
            vm.state.setVar("WaterEffectAmpl", cmd.args[1]);
        }

        /*

        void shakeEffectSet(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set shake effect amplitude to {1} and period to {2}",
                    cmd.op, cmd.args[0], cmd.args[1]));

            ushort ampl = vm.state.valueOrVarValue(cmd.args[0]);
            ushort period = vm.state.valueOrVarValue(cmd.args[1]);

            vm.state.setShakeEffectAmpl(ampl);
            vm.state.setShakeEffectTickPeriod(period);
        }

        //*/

        void sunspotAdd(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add sunspot: pitch {1} heading {2}", cmd.op, cmd.args[0], cmd.args[1]));

            ushort intensity = (ushort)vm.state.getVar("SunspotIntensity");
            ushort color = (ushort)vm.state.getVar("SunspotColor");
            ushort radius = (ushort)vm.state.getVar("SunspotRadius");

            vm.addSunSpot((ushort)cmd.args[0], (ushort)cmd.args[1], (ushort)intensity, (ushort)color, 1, false, (ushort)radius);
        }

        void sunspotAddIntensity(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add sunspot: pitch {1} heading {2}", cmd.op, cmd.args[0], cmd.args[1]));

            ushort intensity = (ushort)cmd.args[2];
            ushort color = (ushort)vm.state.getVar("SunspotColor");
            ushort radius = (ushort)vm.state.getVar("SunspotRadius");

            vm.addSunSpot((ushort)cmd.args[0], (ushort)cmd.args[1], (ushort)intensity, (ushort)color, 1, false, (ushort)radius);
        }

        void sunspotAddVarIntensity(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add sunspot: pitch {1} heading {2}", cmd.op, cmd.args[0], cmd.args[1]));

            ushort intensity = (ushort)cmd.args[2];
            ushort color = (ushort)vm.state.getVar("SunspotColor");
            ushort radius = (ushort)vm.state.getVar("SunspotRadius");

            vm.addSunSpot((ushort)cmd.args[0], (ushort)cmd.args[1], (ushort)intensity, (ushort)color, (ushort)cmd.args[3], true, (ushort)radius);
        }

        void sunspotAddIntensityColor(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add sunspot: pitch {1} heading {2}", cmd.op, cmd.args[0], cmd.args[1]));

            ushort intensity = (ushort)cmd.args[2];
            ushort color = (ushort)cmd.args[3];
            ushort radius = (ushort)vm.state.getVar("SunspotRadius");

            vm.addSunSpot((ushort)cmd.args[0], (ushort)cmd.args[1], (ushort)intensity, (ushort)color, 1, false, (ushort)radius);
        }

        void sunspotAddVarIntensityColor(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add sunspot: pitch {1} heading {2}", cmd.op, cmd.args[0], cmd.args[1]));

            ushort intensity = (ushort)cmd.args[2];
            ushort color = (ushort)cmd.args[3];
            ushort radius = (ushort)vm.state.getVar("SunspotRadius");

            vm.addSunSpot((ushort)cmd.args[0], (ushort)cmd.args[1], (ushort)intensity, (ushort)color, (ushort)cmd.args[4], true, (ushort)radius);
        }

        void sunspotAddIntensityRadius(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add sunspot: pitch {1} heading {2}", cmd.op, cmd.args[0], cmd.args[1]));

            ushort intensity = (ushort)cmd.args[2];
            ushort color = (ushort)vm.state.getVar("SunspotColor");
            ushort radius = (ushort)cmd.args[3];

            vm.addSunSpot((ushort)cmd.args[0], (ushort)cmd.args[1], (ushort)intensity, (ushort)color, 1, false, (ushort)radius);
        }

        void sunspotAddVarIntensityRadius(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add sunspot: pitch {1} heading {2}", cmd.op, cmd.args[0], cmd.args[1]));

            ushort intensity = (ushort)cmd.args[2];
            ushort color = (ushort)vm.state.getVar("SunspotColor");
            ushort radius = (ushort)cmd.args[4];

            vm.addSunSpot((ushort)cmd.args[0], (ushort)cmd.args[1], (ushort)intensity, (ushort)color, (ushort)cmd.args[3], true, (ushort)radius);
        }

        void sunspotAddIntColorRadius(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add sunspot: pitch {1} heading {2}", cmd.op, cmd.args[0], cmd.args[1]));

            ushort intensity = (ushort)cmd.args[2];
            ushort color = (ushort)cmd.args[3];
            ushort radius = (ushort)cmd.args[4];

            vm.addSunSpot((ushort)cmd.args[0], (ushort)cmd.args[1], (ushort)intensity, (ushort)color, (ushort)1, false, (ushort)radius);
        }

        void sunspotAddVarIntColorRadius(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add sunspot: pitch {1} heading {2}", cmd.op, cmd.args[0], cmd.args[1]));

            ushort intensity = (ushort)cmd.args[2];
            ushort color = (ushort)cmd.args[3];
            ushort radius = (ushort)cmd.args[5];

            vm.addSunSpot((ushort)cmd.args[0], (ushort)cmd.args[1], (ushort)intensity, (ushort)color, (ushort)cmd.args[4], true, (ushort)radius);
        }

        /*

        void inventoryAddFront(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Inventory add item {1} at front", cmd.op, cmd.args[0]));

            vm._inventory.addItem(cmd.args[0], false);
        }

        void inventoryAddBack(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Inventory add item {1} at back", cmd.op, cmd.args[0]));

            vm._inventory.addItem(cmd.args[0], true);
        }

        void inventoryRemove(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Inventory remove item {1}", cmd.op, cmd.args[0]));

            vm._inventory.removeItem(cmd.args[0]);
        }

        void inventoryReset(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Reset inventory", cmd.op));

            vm._inventory.reset();
        }

        void inventoryAddSaavChapter(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Get new Saavedro chapter {1}", cmd.op, cmd.args[0]));

            vm._inventory.addSaavedroChapter(cmd.args[0]);
        }

        //*/

        void varSetZero(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set var value {1} := 0", cmd.op, cmd.args[0]));

            vm.state.setVar(cmd.args[0], 0);
        }

        void varSetOne(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set var value {1} := 1", cmd.op, cmd.args[0]));

            vm.state.setVar(cmd.args[0], 1);
        }

        void varSetTwo(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set var value {1} := 2", cmd.op, cmd.args[0]));

            vm.state.setVar(cmd.args[0], 2);
        }

        void varSetOneHundred(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set var value {1} := 100", cmd.op, cmd.args[0]));

            vm.state.setVar(cmd.args[0], 100);
        }

        void varSetValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set var value {1} := {2}", cmd.op, cmd.args[0], cmd.args[1]));

            vm.state.setVar(cmd.args[0], cmd.args[1]);
        }

        void varToggle(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Toggle var {1}", cmd.op, cmd.args[0]));

            vm.state.setVar(cmd.args[0], vm.state.getVar(cmd.args[0]) == 0);
        }

        void varSetOneIfNotZero(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set var {1} to one if not zero", cmd.op, cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);
            if (value != 0)
                vm.state.setVar(cmd.args[0], 1);
        }

        void varOpposite(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Take the opposite of var {1}", cmd.op, cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);
            vm.state.setVar(cmd.args[0], -value);
        }

        void varAbsolute(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Take the absolute value of var {1}", cmd.op, cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);
            vm.state.setVar(cmd.args[0], (value < 0) ? -value : value);
        }

        void varDereference(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Dereference var {1}", cmd.op, cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);
            vm.state.setVar(cmd.args[0], vm.state.getVar(value));
        }

        void varReferenceSetZero(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set to zero the var referenced by var {1}", cmd.op, cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);
            if (value == 0)
                return;

            vm.state.setVar(value, 0);
        }

        void varReferenceSetValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set to {1} the var referenced by var {2}", cmd.op, cmd.args[1], cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);
            if (value == 0)
                return;

            vm.state.setVar(value, cmd.args[1]);
        }

        void varRandRange(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Randomize var {1} value between {2} and {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            int value;

            if (cmd.args[2] - cmd.args[1] > 0)
                value = (int)UnityEngine.Random.Range(cmd.args[1], cmd.args[2]);
            else
                value = cmd.args[1];

            vm.state.setVar(cmd.args[0], value);
        }

        void polarToRectSimple(Context c, Opcode cmd)    {
            log(string.Format("Opcode {0}: Polar to rect transformation for angle in var {1}", cmd.op, cmd.args[5]));

            int angleDeg = vm.state.getVar(cmd.args[5]);
            float angleRad = 2 * Mathf.PI / cmd.args[6] * angleDeg;
            float angleSin = Mathf.Sin(angleRad);
            float angleCos = Mathf.Cos(angleRad);

            int offsetX = cmd.args[2];
            int offsetY = cmd.args[3];

            float radius;
            if (cmd.args[4] >= 0)
                radius = cmd.args[4] - 0.1f;
            else
                radius = cmd.args[4] * -0.1f;

            int posX = (int)(offsetX + radius * angleSin);
            int posY = (int)(offsetY - radius * angleCos);

            vm.state.setVar(cmd.args[0], posX);
            vm.state.setVar(cmd.args[1], posY);
        }

        void polarToRect(Context c, Opcode cmd)    {
            log(string.Format("Opcode {0}: Complex polar to rect transformation for angle in var {1}", cmd.op, cmd.args[8]));

            int angleDeg = vm.state.getVar(cmd.args[8]);
            float angleRad = 2 * Mathf.PI / cmd.args[9] * angleDeg;
            float angleSin = Mathf.Sin(angleRad);
            float angleCos = Mathf.Cos(angleRad);

            float radiusX;
            float radiusY;
            if (angleSin < 0)
                radiusX = cmd.args[4];
            else
                radiusX = cmd.args[5];
            if (angleCos > 0)
                radiusY = cmd.args[6];
            else
                radiusY = cmd.args[7];

            int offsetX = cmd.args[2];
            int offsetY = cmd.args[3];

            int posX = (int)(offsetX + (radiusX - 0.1f) * angleSin);
            int posY = (int)(offsetY - (radiusY - 0.1f) * angleCos);

            vm.state.setVar(cmd.args[0], posX);
            vm.state.setVar(cmd.args[1], posY);
        }

        // void varSetDistanceToZone(Context c, Opcode cmd) {
            // log(string.Format("Opcode {0}: Set var {1} to distance to point {2} {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            // float heading = vm.state.getLookAtHeading();
            // float pitch = vm.state.getLookAtPitch();
            // short distance = (short)(100 * vm._scene.distanceToZone(cmd.args[2], cmd.args[1], cmd.args[3], heading, pitch));

            // vm.state.setVar(cmd.args[0], distance);
        // }

        // void varSetMinDistanceToZone(Context c, Opcode cmd) {
            // log(string.Format("Opcode {0}: Set var {1} to distance to point {2} {3} if lower", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            // float heading = vm.state.getLookAtHeading();
            // float pitch = vm.state.getLookAtPitch();
            // short distance = (short)(100 * vm._scene.distanceToZone(cmd.args[2], cmd.args[1], cmd.args[3], heading, pitch));
            // if (distance >= vm.state.getVar(cmd.args[0]))
                // vm.state.setVar(cmd.args[0], distance);
        // }

        void varRemoveBits(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Remove bits {1} from var {2}", cmd.op, cmd.args[1], cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);

            value &= ~cmd.args[1];

            vm.state.setVar(cmd.args[0], value);
        }

        void varToggleBits(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Toggle bits {1} from var {2}", cmd.op, cmd.args[1], cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);

            value ^= cmd.args[1];

            vm.state.setVar(cmd.args[0], value);
        }

        void varCopy(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Copy var {1} to var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            vm.state.setVar(cmd.args[1], vm.state.getVar(cmd.args[0]));
        }

        void varSetBitsFromVar(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set bits from var {1} on var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[1]);

            value |= vm.state.getVar(cmd.args[0]);

            vm.state.setVar(cmd.args[1], value);
        }

        void varSetBits(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set bits {1} on var {2}", cmd.op, cmd.args[1], cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);

            value |= (int)cmd.args[1];

            vm.state.setVar(cmd.args[0], value);
        }

        void varApplyMask(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Apply mask {1} on var {2}", cmd.op, cmd.args[1], cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);

            value &= cmd.args[1];

            vm.state.setVar(cmd.args[0], value);
        }

        void varSwap(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Swap var {1} and var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[0]);
            vm.state.setVar(cmd.args[0], vm.state.getVar(cmd.args[1]));
            vm.state.setVar(cmd.args[1], value);
        }

        void varIncrement(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Increment var {1}", cmd.op, cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);

            value++;

            vm.state.setVar(cmd.args[0], value);
        }

        void varIncrementMax(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Increment var {1} with max value {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[0]);

            value++;

            if (value > cmd.args[1])
                value = cmd.args[1];

            vm.state.setVar(cmd.args[0], value);
        }

        void varIncrementMaxLooping(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Increment var {1} in range [{2}, {3}]", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            int value = vm.state.getVar(cmd.args[0]);

            value++;

            if (value > cmd.args[2])
                value = cmd.args[1];

            vm.state.setVar(cmd.args[0], value);
        }

        void varAddValueMaxLooping(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add {1} to var {2} in range [{3}, {4}]", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]));

            int value = vm.state.getVar(cmd.args[1]);

            value += cmd.args[0];

            if (value > cmd.args[3])
                value = cmd.args[2];

            vm.state.setVar(cmd.args[1], value);
        }

        void varDecrement(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Decrement var {1}", cmd.op, cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);

            value--;

            vm.state.setVar(cmd.args[0], value);
        }

        void varDecrementMin(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Decrement var {1} with min value {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[0]);

            value--;

            if (value < cmd.args[1])
                value = cmd.args[1];

            vm.state.setVar(cmd.args[0], value);
        }

        void varDecrementMinLooping(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Decrement var {1} in range [{2}, {3}]", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            int value = vm.state.getVar(cmd.args[0]);

            value--;

            if (value < cmd.args[1])
                value = cmd.args[2];

            vm.state.setVar(cmd.args[0], value);
        }

        void varAddValueMax(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add value {1} to var {2} with max value {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            int value = vm.state.getVar(cmd.args[1]);

            value += cmd.args[0];

            if (value > cmd.args[2])
                value = cmd.args[2];

            vm.state.setVar(cmd.args[1], value);
        }

        void varSubValueMin(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Substract value {1} from var {2} with min value {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            int value = vm.state.getVar(cmd.args[1]);

            value -= cmd.args[0];

            if (value < cmd.args[2])
                value = cmd.args[2];

            vm.state.setVar(cmd.args[1], value);
        }

        void varZeroRange(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set vars from {1} to {2} to zero", cmd.op, cmd.args[0], cmd.args[1]));

            if (cmd.args[0] > cmd.args[1])
                throw new Exception(string.Format("Opcode {0}, Incorrect range, {1} . {2}", cmd.op, cmd.args[0], cmd.args[1]));

            for (short i = cmd.args[0]; i <= cmd.args[1]; i++)
                vm.state.setVar(i, 0);
        }

        void varCopyRange(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Copy vars from {1} to {2}, length: {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            if (cmd.args[2] <= 0)
                return;

            for (short i = 0; i < cmd.args[2]; i++)
                vm.state.setVar(cmd.args[1] + i, vm.state.getVar(cmd.args[0] + i));
        }

        void varSetRange(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set vars from {1} to {2} to val {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            if (cmd.args[0] > cmd.args[1])
                throw new Exception(string.Format("Opcode {0}, Incorrect range, {1} . {2}", cmd.op, cmd.args[0], cmd.args[1]));

            for (short i = cmd.args[0]; i <= cmd.args[1]; i++)
                vm.state.setVar(i, cmd.args[2]);
        }

        void varIncrementMaxTen(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Increment var {1} max 10", cmd.op, cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);

            value++;

            if (value == 10)
                value = 1;

            vm.state.setVar(cmd.args[0], value);
        }

        void varAddValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add value {1} to var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[1]);
            value += cmd.args[0];
            vm.state.setVar(cmd.args[1], value);
        }

        void varArrayAddValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add value {1} to array base var {2} item var {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            int value = vm.state.getVar(cmd.args[1] + vm.state.getVar(cmd.args[2]));
            value += cmd.args[0];
            vm.state.setVar(cmd.args[1] + vm.state.getVar(cmd.args[2]), value);
        }

        void varAddVarValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Add var {1} value to var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[1]);
            value += vm.state.getVar(cmd.args[0]);
            vm.state.setVar(cmd.args[1], value);
        }

        void varSubValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Substract value {1} to var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[1]);
            value -= cmd.args[0];
            vm.state.setVar(cmd.args[1], value);
        }

        void varSubVarValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Substract var {1} value to var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[1]);
            value -= vm.state.getVar(cmd.args[0]);
            vm.state.setVar(cmd.args[1], value);
        }

        void varModValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Apply modulo {1} to var {2}", cmd.op, cmd.args[1], cmd.args[0]));

            int value = vm.state.getVar(cmd.args[0]);
            value %= cmd.args[1];
            vm.state.setVar(cmd.args[0], value);
        }

        void varMultValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Multiply var {1} by value {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[0]);
            value *= cmd.args[1];
            vm.state.setVar(cmd.args[0], value);
        }

        void varMultVarValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Multiply var {1} by var {2} value", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[0]);
            value *= vm.state.getVar(cmd.args[1]);
            vm.state.setVar(cmd.args[0], value);
        }

        void varDivValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Divide var {1} by value {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[0]);
            value /= cmd.args[1];
            vm.state.setVar(cmd.args[0], value);
        }

        void varDivVarValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Divide var {1} by var {2} value", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[0]);
            value /= vm.state.getVar(cmd.args[1]);
            vm.state.setVar(cmd.args[0], value);
        }

        void varCrossMultiplication(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Cross multiply var {1} from range {2} {3} to range {4} {5}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3], cmd.args[4]));

            int value = vm.state.getVar(cmd.args[0]);

            if (value == 0)
                return;

            int temp = Mathf.Abs(value) - cmd.args[1];
            temp *= (cmd.args[4] - cmd.args[3]) / (cmd.args[2] - cmd.args[1]);
            temp += cmd.args[3];

            vm.state.setVar(cmd.args[0], value > 0 ? temp : -temp);
        }

        void varMinValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set var {1} to min between {2} and var value", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[0]);

            if (value > cmd.args[1])
                value = cmd.args[1];

            vm.state.setVar(cmd.args[0], value);
        }

        void varClipValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Clip var {1} value between {2} and {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            int value = vm.state.getVar(cmd.args[0]);

            value = (int)Mathf.Clamp(value, cmd.args[1], cmd.args[2]);

            vm.state.setVar(cmd.args[0], value);
        }

        void varClipChangeBound(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Clip var {1} value between {2} and {3} changing bounds", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            int value = vm.state.getVar(cmd.args[0]);

            if (value < cmd.args[1])
                value = cmd.args[2];

            if (value > cmd.args[2])
                value = cmd.args[1];

            vm.state.setVar(cmd.args[0], value);
        }

        void varAbsoluteSubValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Take absolute value of var {1} and substract {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[0]);

            value = Mathf.Abs(value) - cmd.args[1];

            vm.state.setVar(cmd.args[0], value);
        }

        void varAbsoluteSubVar(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Take absolute value of var {1} and substract var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[0]);

            value = Mathf.Abs(value) - vm.state.getVar(cmd.args[1]);

            vm.state.setVar(cmd.args[0], value);
        }

        void varRatioToPercents(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Convert var {1} to percents (max value {2}, tare weight {3})", cmd.op, cmd.args[0], cmd.args[2], cmd.args[1]));

            int value = vm.state.getVar(cmd.args[0]);

            value = 100 * (cmd.args[2] - Mathf.Abs(value - cmd.args[1])) / cmd.args[2];
            value = Mathf.Max(0, value);

            vm.state.setVar(cmd.args[0], value);
        }


        void varRotateValue3(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Var take next value, var {1} values {2} {3} {4}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]));

            int value = vm.state.getVar(cmd.args[0]);

            if (value == cmd.args[1]) {
                value = cmd.args[2];
            } else if (value == cmd.args[2]) {
                value = cmd.args[3];
            } else {
                value = cmd.args[1];
            }

            vm.state.setVar(cmd.args[0], value);
        }

        void ifElse(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Else", cmd.op));

            c.result = true;
            c.endScript = true;
        }

        void goToElse(Context c) {
            Command elseCommand = findCommandByProc(ifElse);

            // Go to next command until an else statement is met
            do {
                c.op++;
            } while (c.op != c.script.Count && c.script[c.op].op != elseCommand.op);
        }

        void ifCondition(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If condition {1}", cmd.op, cmd.args[0]));

            if (vm.state.evaluate(cmd.args[0]))
                return;

            goToElse(c);
        }

        void ifCond1AndCond2(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If cond {1} and cond {2}", cmd.op, cmd.args[0], cmd.args[1]));

            if (vm.state.evaluate(cmd.args[0])
                    && vm.state.evaluate(cmd.args[1]))
                return;

            goToElse(c);
        }

        void ifCond1OrCond2(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If cond {1} or cond {2}", cmd.op, cmd.args[0], cmd.args[1]));

            if (vm.state.evaluate(cmd.args[0])
                    || vm.state.evaluate(cmd.args[1]))
                return;

            goToElse(c);
        }

        void ifOneVarSetInRange(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If one var set int range {1} {2}", cmd.op, cmd.args[0], cmd.args[1]));

            ushort var = (ushort)cmd.args[0];
            ushort end = (ushort)cmd.args[1];

            if (end > var) {
                goToElse(c);
                return;
            }

            bool result = false;

            do {
                result |= vm.state.getVar(var) != 0;
                var++;
            } while (var <= end);

            if (result)
                return;

            goToElse(c);
        }

        void ifVarEqualsValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} equals value {2}", cmd.op, cmd.args[0], cmd.args[1]));

            if (vm.state.getVar(cmd.args[0]) == cmd.args[1])
                return;

            goToElse(c);
        }

        void ifVarNotEqualsValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} not equals value {2}", cmd.op, cmd.args[0], cmd.args[1]));

            if (vm.state.getVar(cmd.args[0]) != cmd.args[1])
                return;

            goToElse(c);
        }

        void ifVar1EqualsVar2(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} equals var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            if (vm.state.getVar(cmd.args[0]) == vm.state.getVar(cmd.args[1]))
                return;

            goToElse(c);
        }

        void ifVar1NotEqualsVar2(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} not equals var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            if (vm.state.getVar(cmd.args[0]) != vm.state.getVar(cmd.args[1]))
                return;

            goToElse(c);
        }

        void ifVarSupEqValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} >= value {2}", cmd.op, cmd.args[0], cmd.args[1]));

            if (vm.state.getVar(cmd.args[0]) >= cmd.args[1])
                return;

            goToElse(c);
        }

        void ifVarInfEqValue(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} <= value {2}", cmd.op, cmd.args[0], cmd.args[1]));

            if (vm.state.getVar(cmd.args[0]) <= cmd.args[1])
                return;

            goToElse(c);
        }

        void ifVarInRange(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} in range {2} {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            int value = vm.state.getVar(cmd.args[0]);
            if(value >= cmd.args[1] && value <= cmd.args[2])
                return;

            goToElse(c);
        }

        void ifVarNotInRange(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} not in range {2} {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            int value = vm.state.getVar(cmd.args[0]);
            if(value < cmd.args[1] || value > cmd.args[2])
                return;

            goToElse(c);
        }

        void ifVar1SupEqVar2(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} >= var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            if (vm.state.getVar(cmd.args[0]) >= vm.state.getVar(cmd.args[1]))
                return;

            goToElse(c);
        }

        void ifVar1SupVar2(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} > var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            if (vm.state.getVar(cmd.args[0]) > vm.state.getVar(cmd.args[1]))
                return;

            goToElse(c);
        }

        void ifVar1InfEqVar2(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} <= var {2}", cmd.op, cmd.args[0], cmd.args[1]));

            if (vm.state.getVar(cmd.args[0]) <= vm.state.getVar(cmd.args[1]))
                return;

            goToElse(c);
        }

        void ifVarHasAllBitsSet(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} & val {2} == val {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[1]));

            if ((vm.state.getVar(cmd.args[0]) & cmd.args[1]) == cmd.args[1])
                return;

            goToElse(c);
        }

        void ifVarHasNoBitsSet(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} & val {2} == 0", cmd.op, cmd.args[0], cmd.args[1]));

            if ((vm.state.getVar(cmd.args[0]) & cmd.args[1]) == 0)
                return;

            goToElse(c);
        }

        void ifVarHasSomeBitsSet(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: If var {1} & val {2} == val {3}", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            if ((vm.state.getVar(cmd.args[0]) & cmd.args[1]) == cmd.args[2])
                return;

            goToElse(c);
        }

        /*

        void ifHeadingInRange(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: If heading in range %d . %d",
                    cmd.op, cmd.args[0], cmd.args[1]);

            float heading = vm.state.getLookAtHeading();

            if (cmd.args[1] > cmd.args[0]) {
                // If heading in range
                if (heading > cmd.args[0] && heading < cmd.args[1]) {
                    return;
                }
            } else {
                // If heading *not* in range
                if (heading > cmd.args[0] || heading < cmd.args[1]) {
                    return;
                }
            }

            goToElse(c);
        }

        void ifPitchInRange(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: If pitch in range %d . %d",
                    cmd.op, cmd.args[0], cmd.args[1]);

            float pitch = vm.state.getLookAtPitch();

            // If pitch in range
            if (pitch > cmd.args[0] && pitch < cmd.args[1])
                return;

            goToElse(c);
        }

        void ifHeadingPitchInRect(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: If heading in range %d . %d",
                    cmd.op, cmd.args[0], cmd.args[1]);

            float heading = vm.state.getLookAtHeading();
            float pitch = vm.state.getLookAtPitch();

            // If pitch in range
            if (pitch <= cmd.args[0] || pitch >= cmd.args[1]) {
                goToElse(c);
                return;
            }

            if (cmd.args[3] > cmd.args[2]) {
                // If heading in range
                if (heading > cmd.args[2] && heading < cmd.args[3])
                    return;
            } else {
                // If heading *not* in range
                if (heading > cmd.args[2] || heading < cmd.args[3])
                    return;
            }

            goToElse(c);
        }

        void ifMouseIsInRect(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: If mouse in rect l%d t%d w%d h%d",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]);

            Common::Rect r = Common::Rect(cmd.args[2], cmd.args[3]);
            r.translate(cmd.args[0], cmd.args[1]);

            Common::Point mouse = vm._cursor.getPosition(false);
            mouse = vm._scene.scalePoint(mouse);

            if (r.contains(mouse))
                return;

            goToElse(c);
        }

        void leverDrag(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Drag lever for var %d with script %d", cmd.op, cmd.args[4], cmd.args[6]));

            short minPosX = cmd.args[0];
            short minPosY = cmd.args[1];
            short maxPosX = cmd.args[2];
            short maxPosY = cmd.args[3];
            short var = cmd.args[4];
            short numPositions = cmd.args[5];
            short script = cmd.args[6];

            vm._cursor.changeCursor(2);

            bool mousePressed = true;
            while (true) {
                float ratioPosition = 0.0;
                // Compute the distance to the minimum lever point
                // and divide it by the lever movement amplitude
                if (vm.state.getViewType() == kCube) {
                    float pitch, heading;
                    vm._cursor.getDirection(pitch, heading);

                    float amplitude = sqrt(Math::square(maxPosX - minPosX) + Math::square(maxPosY - minPosY));
                    float distanceToMin = sqrt(Math::square(pitch - minPosX) + Math::square(heading - minPosY));
                    float distanceToMax = sqrt(Math::square(pitch - maxPosX) + Math::square(heading - maxPosY));

                    ratioPosition = distanceToMax < amplitude ? distanceToMin / amplitude : 0.0;
                } else {
                    Common::Point mouse = vm._cursor.getPosition(false);
                    mouse = vm._scene.scalePoint(mouse);
                    short amplitude;
                    short pixelPosition;

                    if (minPosX == maxPosX) {
                        // Vertical slider
                        amplitude = maxPosY - minPosY;
                        pixelPosition = mouse.y - minPosY;
                    } else {
                        // Horizontal slider
                        amplitude = maxPosX - minPosX;
                        pixelPosition = mouse.x - minPosX;
                    }

                    ratioPosition = pixelPosition / (float) amplitude;
                }

                short position = (short)(ratioPosition * (numPositions + 1));
                position = CLIP<short>(position, 1, numPositions);

                if (vm.state.getDragLeverLimited()) {
                    short minPosition = vm.state.getDragLeverLimitMin();
                    short maxPosition = vm.state.getDragLeverLimitMax();
                    position = CLIP(position, minPosition, maxPosition);
                }

                // Set new lever position
                vm.state.setVar(var, position);

                // Draw a frame
                vm.processInput(true);
                vm.drawFrame();

                mousePressed = vm.getEventManager().getButtonState() & Common::EventManager::LBUTTON;
                vm.state.setDragEnded(!mousePressed);

                if (vm.state.getDragLeverSpeed()) {
                    //log(string.Format("Interaction with var 58 is missing in opcode 127."));
                    return;
                }

                if (script) {
                    vm.state.setVar(var, position);
                    vm.runScriptsFromNode(abs(script));
                }

                if (!mousePressed)
                    break;
            }

            vm.state.setDragLeverLimited(0);
            vm.state.setDragLeverSpeed(0);
        }

        void leverDragPositions(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Drag lever for var %d with script %d", cmd.op, cmd.args[0], cmd.args[1]));

            short var = cmd.args[0];
            short script = cmd.args[1];
            ushort numPositions = (cmd.args.Count - 3) / 3;

            if (cmd.args[2 + numPositions * 3] != -1)
                error("leverDragPositions no end marker found");

            vm._cursor.changeCursor(2);

            bool mousePressed = true;
            while (true) {
                float pitch, heading;
                vm._cursor.getDirection(pitch, heading);

                float minDistance = 180.0;
                uint position = 0;

                // Find the lever position where the distance between the lever
                // and the mouse is minimal, by trying every possible position.
                for (uint i = 0; i < numPositions; i++) {
                    float posPitch = cmd.args[2 + i * 3 + 0] * 0.1;
                    float posHeading = cmd.args[2 + i * 3 + 1] * 0.1;

                    // Distance between the mouse and the lever
                    float distance = sqrt(Math::square(pitch - posPitch) + Math::square(heading - posHeading));

                    if (distance < minDistance) {
                        minDistance = distance;
                        position = cmd.args[2 + i * 3 + 2];
                    }
                }

                // Set new lever position
                vm.state.setVar(var, position);

                // Draw a frame
                vm.processInput(true);
                vm.drawFrame();

                mousePressed = vm.inputValidatePressed();
                vm.state.setDragEnded(!mousePressed);

                if (vm.state.getDragLeverSpeed()) {
                    //log(string.Format("Interaction with var 58 is missing in opcode 132."));
                    return;
                }

                if (script) {
                    vm.state.setVar(var, position);
                    vm.runScriptsFromNode(abs(script));
                }

                if (!mousePressed)
                    break;
            }

            vm.state.setDragLeverSpeed(0);
        }

        void leverDragXY(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Drag 2D lever and update X (var %d) and Y (var %d) coordinates, while running script %d", cmd.op, cmd.args[0], cmd.args[1], cmd.args[4]));

            ushort varX = cmd.args[0];
            ushort varY = cmd.args[1];
            ushort scale = cmd.args[2];
            ushort maxLeverPosition = cmd.args[3];
            ushort script = vm.state.valueOrVarValue(cmd.args[4]);

            Common::Point mouseInit = vm._cursor.getPosition(false);
            mouseInit = vm._scene.scalePoint(mouseInit);

            vm._cursor.changeCursor(2);

            bool mousePressed = true;
            do {
                Common::Point mouse = vm._cursor.getPosition(false);
                mouse = vm._scene.scalePoint(mouse);
                short distanceX = (mouseInit.x - mouse.x) / scale;
                short distanceY = (mouseInit.y - mouse.y) / scale;

                distanceX = CLIP<short>(distanceX, -maxLeverPosition, maxLeverPosition);
                distanceY = CLIP<short>(distanceY, -maxLeverPosition, maxLeverPosition);

                // Set lever position variables
                vm.state.setVar(varX, distanceX);
                vm.state.setVar(varY, distanceY);

                // Draw a frame
                vm.processInput(true);
                vm.drawFrame();

                mousePressed = vm.getEventManager().getButtonState() & Common::EventManager::LBUTTON;
                vm.state.setDragEnded(!mousePressed);

                // Run script
                if (script)
                    vm.runScriptsFromNode(script);
            } while (mousePressed);
        }

        void itemDrag(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Drag item %d", cmd.op, cmd.args[4]));
            vm.dragItem(cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3], cmd.args[4]);
        }

        void runScriptWhileDragging(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: While dragging lever, run script %d", cmd.op, cmd.args[7]));

            ushort script = vm.state.valueOrVarValue(cmd.args[7]);
            ushort maxDistance = cmd.args[6];
            ushort maxLeverPosition = cmd.args[5];
            short lastLeverPosition = vm.state.getVar(cmd.args[4]);
            short leverHeight = cmd.args[3];
            short leverWidth = cmd.args[2];

            vm._cursor.changeCursor(2);

            bool dragWithDirectionKeys = vm.state.hasVarDragWithDirectionKeys()
                    && vm.state.getDragWithDirectionKeys();

            bool dragging = true;
            do {
                dragging = vm.getEventManager().getButtonState() & Common::EventManager::LBUTTON;
                dragging |= vm.state.hasVarGamePadActionPressed() && vm.state.getGamePadActionPressed();
                vm.state.setDragEnded(!dragging);

                vm.processInput(true);
                vm.drawFrame();

                if (!dragWithDirectionKeys) {
                    // Distance between the mouse and the lever
                    Common::Point mouse = vm._cursor.getPosition(false);
                    mouse = vm._scene.scalePoint(mouse);
                    short distanceX = mouse.x - leverWidth / 2 - vm.state.getVar(cmd.args[0]);
                    short distanceY = mouse.y - leverHeight / 2 - vm.state.getVar(cmd.args[1]);
                    float distance = sqrt((float) distanceX * distanceX + distanceY * distanceY);

                    ushort bestPosition = lastLeverPosition;
                    if (distance > maxDistance) {
                        vm.state.setDragLeverPositionChanged(false);
                    } else {
                        // Find the lever position where the distance between the lever
                        // and the mouse is minimal, by trying every possible position.
                        float minDistance = 1000;
                        for (uint i = 0; i < maxLeverPosition; i++) {
                            vm.state.setDragPositionFound(false);

                            vm.state.setVar(cmd.args[4], i);
                            vm.runScriptsFromNode(script);

                            mouse = vm._cursor.getPosition(false);
                            mouse = vm._scene.scalePoint(mouse);
                            distanceX = mouse.x - leverWidth / 2 - vm.state.getVar(cmd.args[0]);
                            distanceY = mouse.y - leverHeight / 2 - vm.state.getVar(cmd.args[1]);
                            distance = sqrt((float) distanceX * distanceX + distanceY * distanceY);

                            if (distance < minDistance) {
                                minDistance = distance;
                                bestPosition = i;
                            }
                        }
                        vm.state.setDragLeverPositionChanged(bestPosition != lastLeverPosition);
                    }

                    // Set the lever position to the best position
                    vm.state.setDragPositionFound(true);
                    vm.state.setVar(cmd.args[4], bestPosition);
                } else {
                    ushort previousPosition = vm.state.getVar(cmd.args[4]);
                    ushort position = previousPosition;

                    if (vm.state.getGamePadLeftPressed()) {
                        position--;
                    } else if (vm.state.getGamePadRightPressed()) {
                        position++;
                    }

                    position = CLIP<short>(position, 0, maxLeverPosition);
                    vm.state.setVar(cmd.args[4], position);
                    vm.state.setDragLeverPositionChanged(position != previousPosition);
                }

                vm.runScriptsFromNode(script);
                vm.processInput(true);
                vm.drawFrame();
            } while (dragging);

            if (dragWithDirectionKeys) {
                vm.state.setDragWithDirectionKeys(false);
            }

            vm.state.setDragPositionFound(false);
        }

        void chooseNextNode(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Choose next node using condition %d", cmd.op, cmd.args[0]));

            if (vm.state.evaluate(cmd.args[0]))
                vm.state.setLocationNextNode(cmd.args[1]);
            else
                vm.state.setLocationNextNode(cmd.args[2]);
        }

        //*/

        void goToNodeTransition(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Go to node {1} with transition {2}", cmd.op, cmd.args[0], cmd.args[1]));

            vm.goToNode((ushort)cmd.args[0], (TransitionType)(cmd.args[1]));
        }

        void goToNodeTrans2(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Go to node {1}", cmd.op, cmd.args[0]));

            vm.goToNode((ushort)cmd.args[0], TransitionType.kTransitionNone);
        }

        void goToNodeTrans1(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Go to node {1}", cmd.op, cmd.args[0]));

            vm.goToNode((ushort)cmd.args[0], TransitionType.kTransitionFade);
        }

        void goToRoomNode(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Go to room {1}, node {2}", cmd.op, cmd.args[0], cmd.args[1]));

            vm.state.setVar("LocationNextRoom", cmd.args[0]);
            vm.state.setVar("LocationNextNode", cmd.args[1]);

            vm.goToNode(0, TransitionType.kTransitionFade);
        }

        /*

        void zipToNode(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Zip to node %d", cmd.op, cmd.args[0]));

            vm.goToNode(cmd.args[0], kTransitionZip);
        }

        void zipToRoomNode(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Zip to room %d, node %d", cmd.op, cmd.args[0], cmd.args[1]));

            vm.state.setLocationNextRoom(cmd.args[0]);
            vm.state.setLocationNextNode(cmd.args[1]);

            vm.goToNode(0, kTransitionZip);
        }

        void drawTransition(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Draw transition", cmd.op));

            vm.drawTransition(kTransitionFade);
        }

        void reloadNode(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Reload current node", cmd.op));

            vm.loadNode(0);
            vm.drawFrame();
        }

        void redrawFrame(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Reload current node", cmd.op));

            vm.drawFrame();
        }

        void moviePlay(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play movie %d", cmd.op, cmd.args[0]));

            vm.playSimpleMovie(vm.state.valueOrVarValue(cmd.args[0]));
        }

        void moviePlaySynchronized(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play movie %d, synchronized with framerate", cmd.op, cmd.args[0]));

            vm.state.setMovieSynchronized(1);
            vm.playSimpleMovie(vm.state.valueOrVarValue(cmd.args[0]));
        }

        void cameraLimitMovement(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Limit camera movement in a rect", cmd.op));

            vm.state.limitCubeCamera(cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]);
        }

        //*/

        void cameraFreeMovement(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Free camera movement from rect", cmd.op));

            vm.state.freeCubeCamera();
        }

        /*

        void cameraLookAt(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Camera look at %d %d", cmd.op, cmd.args[0], cmd.args[1]));

            float pitch = cmd.args[0];
            float heading = cmd.args[1];
            vm.state.lookAt(pitch, heading);
        }

        void cameraLookAtVar(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Camera look at value of var %d", cmd.op, cmd.args[0]));

            float pitch = vm.state.getVar(cmd.args[0]) / 1000.0;
            float heading = vm.state.getVar(cmd.args[0] + 1) / 1000.0;
            vm.state.lookAt(pitch, heading);
        }

        void cameraGetLookAt(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Save camera look at to var %d", cmd.op, cmd.args[0]));

            float pitch = vm.state.getLookAtPitch() * 1000.0;
            float heading = vm.state.getLookAtHeading() * 1000.0;

            vm.state.setVar(cmd.args[0],(int) pitch);
            vm.state.setVar(cmd.args[0] + 1, (int)heading);
        }

        void lookAtMovieStartImmediate(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Look at movie %d start", cmd.op, cmd.args[0]));

            ushort movieId = vm.state.valueOrVarValue(cmd.args[0]);

            float startPitch, startHeading;
            vm.getMovieLookAt(movieId, true, startPitch, startHeading);
            vm.state.lookAt(startPitch, startHeading);
        }

        //*/

        void cameraSetFOV(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Set camera fov {1}", cmd.op, cmd.args[0]));

            int fov = vm.state.valueOrVarValue(cmd.args[0]);

            vm.state.setLookAtFOV(fov);
        }

        /*

        void changeNode(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Go to node %d", cmd.op, cmd.args[0]));

            vm.loadNode(cmd.args[0]);
        }

        void changeNodeRoom(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Go to node %d room %d", cmd.op, cmd.args[0], cmd.args[1]));

            vm.loadNode(cmd.args[1], cmd.args[0]);
        }

        void changeNodeRoomAge(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Go to node %d room %d age %d", cmd.op, cmd.args[2], cmd.args[1], cmd.args[0]));

            vm.loadNode(cmd.args[2], cmd.args[1], cmd.args[0]);
        }

        void drawXTicks(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Draw %d ticks", cmd.op, cmd.args[0]));

            uint endTick = vm.state.getTickCount() + cmd.args[0];

            while (vm.state.getTickCount() < endTick) {
                vm.processInput(true);
                vm.drawFrame();
            }
        }

        void drawWhileCond(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: While condition %d, draw", cmd.op, cmd.args[0]));

            while (vm.state.evaluate(cmd.args[0]) && !vm.inputEscapePressed()) {
                vm.processInput(true);
                vm.drawFrame();
            }
        }

        void whileStart(Context c, Opcode cmd) {
            const Command &whileEndCommand = findCommandByProc(&Script::whileEnd);

            c.whileStart = c.op - 1;

            // Check the while condition
            if (!vm.state.evaluate(cmd.args[0])) {
                // Condition is false, go to the next opcode after the end of the while loop
                do {
                    c.op++;
                } while (c.op != c.script.end() && c.op.op != whileEndCommand.op);
            }

            vm.processInput(true);
            vm.drawFrame();
        }

        void whileEnd(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: End of while condition", cmd.op));

            // Go to while start
            c.op = c.whileStart;
        }

        void runScriptWhileCond(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: While condition %d, run script %d", cmd.op, cmd.args[0], cmd.args[1]));

            while (vm.state.evaluate(cmd.args[0])) {
                vm.runScriptsFromNode(cmd.args[1]);
                vm.processInput(true);
                vm.drawFrame();
            }

            vm.processInput(true);
            vm.drawFrame();
        }

        void runScriptWhileCondEachXFrames(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: While condition %d, run script %d each %d frames", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            uint step = cmd.args[2] % 100;

            uint firstStep = cmd.args[2];
            if (firstStep > 100)
                firstStep /= 100;

            uint nextScript = vm.state.getTickCount() + firstStep;

            while (vm.state.evaluate(cmd.args[0])) {

                if (vm.state.getTickCount() >= nextScript) {
                    nextScript = vm.state.getTickCount() + step;

                    vm.runScriptsFromNode(cmd.args[1]);
                }

                vm.processInput(true);
                vm.drawFrame();
            }

            vm.processInput(true);
            vm.drawFrame();
        }

        void moviePlayFullFrame(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play movie %d", cmd.op, cmd.args[0]));

            ushort movieId = vm.state.valueOrVarValue(cmd.args[0]);
            vm._cursor.setVisible(false);
            vm.playMovieFullFrame(movieId);
            vm._cursor.setVisible(true);
        }

        void moviePlayFullFrameTrans(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play movie %d with transition", cmd.op, cmd.args[0]));

            ushort movieId = vm.state.valueOrVarValue(cmd.args[0]);
            vm._cursor.setVisible(false);
            vm.playMovieFullFrame(movieId);
            vm._cursor.setVisible(true);

            vm.drawTransition(kTransitionFade);
        }

        void moviePlayChangeNode(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play movie %d, go to node %d", cmd.op, cmd.args[1], cmd.args[0]));

            ushort nodeId = vm.state.valueOrVarValue(cmd.args[0]);
            ushort movieId = vm.state.valueOrVarValue(cmd.args[1]);
            vm._cursor.setVisible(false);
            vm.playMovieGoToNode(movieId, nodeId);
            vm._cursor.setVisible(true);
        }

        //*/

        void moviePlayChangeNodeTrans(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: Play movie %d, go to node %d with transition", cmd.op, cmd.args[1], cmd.args[0]));

            ushort nodeId = (ushort)vm.state.valueOrVarValue(cmd.args[0]);
            ushort movieId = (ushort)vm.state.valueOrVarValue(cmd.args[1]);
            vm.cursor.setVisible(false);
            vm.playMovieGoToNode(movieId, nodeId);
            vm.cursor.setVisible(true);

            // vm.drawTransition(kTransitionFade); // FIXME
        }

        /*

        void lookAt(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Look at %d, %d", cmd.op, cmd.args[0], cmd.args[1]));

            vm.animateDirectionChange(cmd.args[0], cmd.args[1], 0);
        }

        void lookAtInXFrames(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Look at %d, %d in %d frames", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            vm.animateDirectionChange(cmd.args[0], cmd.args[1], cmd.args[2]);
        }

        void lookAtMovieStart(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Look at movie %d start", cmd.op, cmd.args[0]));

            ushort movieId = vm.state.valueOrVarValue(cmd.args[0]);

            float startPitch, startHeading;
            vm.getMovieLookAt(movieId, true, startPitch, startHeading);
            vm.animateDirectionChange(startPitch, startHeading, 0);
        }

        void lookAtMovieStartInXFrames(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Look at movie %d start in %d frames", cmd.op, cmd.args[0], cmd.args[1]));

            ushort movieId = vm.state.valueOrVarValue(cmd.args[0]);

            float startPitch, startHeading;
            vm.getMovieLookAt(movieId, true, startPitch, startHeading);
            vm.animateDirectionChange(startPitch, startHeading, cmd.args[1]);
        }

        void runScriptForVarDrawTicksHelper(ushort var, int startValue, int endValue, ushort script, int numTicks) {
            if (numTicks < 0) {
                numTicks = -numTicks;
                uint startTick = vm.state.getTickCount();
                uint currentTick = startTick;
                uint endTick = startTick + numTicks;
                uint numValues = abs(endValue - startValue);

                if (startTick < endTick) {
                    int currentValue = -9999;
                    while (1) {
                        int nextValue = numValues * (currentTick - startTick) / numTicks;
                        if (currentValue != nextValue) {
                            currentValue = nextValue;

                            short varValue;
                            if (endValue > startValue)
                                varValue = startValue + currentValue;
                            else
                                varValue = startValue - currentValue;

                            vm.state.setVar(var, varValue);

                            if (script) {
                                vm.runScriptsFromNode(script);
                            }
                        }

                        vm.processInput(true);
                        vm.drawFrame();
                        currentTick = vm.state.getTickCount();

                        if (currentTick > endTick)
                            break;
                    }
                }

                vm.state.setVar(var, endValue);
            } else {
                int currentValue = startValue;
                uint endTick = 0;

                bool positiveDirection = endValue > startValue;

                while (1) {
                    if ((positiveDirection && (currentValue > endValue))
                            || (!positiveDirection && (currentValue < endValue)))
                        break;

                    vm.state.setVar(var, currentValue);

                    if (script)
                        vm.runScriptsFromNode(script);

                    for (uint i = vm.state.getTickCount(); i < endTick; i = vm.state.getTickCount()) {
                        vm.processInput(true);
                        vm.drawFrame();
                    }

                    endTick = vm.state.getTickCount() + numTicks;

                    currentValue += positiveDirection ? 1 : -1;
                }
            }
        }

        void runScriptForVar(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: For var %d from %d to %d, run script %d",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]);

            runScriptForVarDrawTicksHelper(cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3], 0);
        }

        void runScriptForVarEachXFrames(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: For var %d from %d to %d, run script %d every %d frames",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3], cmd.args[4]);

            runScriptForVarDrawTicksHelper(cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3], cmd.args[4]);
        }

        void runScriptForVarStartVar(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: For var %d from var %d value to %d, run script %d",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]);

            runScriptForVarDrawTicksHelper(cmd.args[0], vm.state.getVar(cmd.args[1]), cmd.args[2], cmd.args[3], 0);
        }

        void runScriptForVarStartVarEachXFrames(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: For var %d from var %d value to %d, run script %d every %d frames",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3], cmd.args[4]);

            runScriptForVarDrawTicksHelper(cmd.args[0], vm.state.getVar(cmd.args[1]), cmd.args[2], cmd.args[3], cmd.args[4]);
        }

        void runScriptForVarEndVar(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: For var %d from %d to var %d value, run script %d",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]);

            runScriptForVarDrawTicksHelper(cmd.args[0], cmd.args[1], vm.state.getVar(cmd.args[2]), cmd.args[3], 0);
        }

        void runScriptForVarEndVarEachXFrames(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: For var %d from var %d value to var %d value, run script %d every %d frames",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3], cmd.args[4]);

            runScriptForVarDrawTicksHelper(cmd.args[0], cmd.args[1], vm.state.getVar(cmd.args[2]), cmd.args[3], cmd.args[4]);
        }

        void runScriptForVarStartEndVar(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: For var %d from var %d value to var %d value, run script %d",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]);

            runScriptForVarDrawTicksHelper(cmd.args[0], vm.state.getVar(cmd.args[1]), vm.state.getVar(cmd.args[2]),
                                        cmd.args[3], 0);
        }

        void runScriptForVarStartEndVarEachXFrames(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: For var %d from var %d value to var %d value, run script %d every %d frames",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3], cmd.args[4]);

            runScriptForVarDrawTicksHelper(cmd.args[0], vm.state.getVar(cmd.args[1]), vm.state.getVar(cmd.args[2]),
                                        cmd.args[3], cmd.args[4]);
        }

        void drawFramesForVar(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: For var %d from %d to %d, every %d frames",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]);

            runScriptForVarDrawTicksHelper(cmd.args[0], cmd.args[1], cmd.args[2], 0, -cmd.args[3]);
        }

        void drawFramesForVarEachTwoFrames(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: For var %d from %d to %d draw 2 frames",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]);

            uint numFrames = 2 * (-1 - abs(cmd.args[2] - cmd.args[1]));

            runScriptForVarDrawTicksHelper(cmd.args[0], cmd.args[1], cmd.args[2], 0, numFrames);
        }

        void drawFramesForVarStartEndVarEachTwoFrames(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: For var %d from var %d value to var %d value draw 2 frames",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]);

            uint numFrames = 2 * (-1 - abs(cmd.args[2] - cmd.args[1]));

            runScriptForVarDrawTicksHelper(cmd.args[0], vm.state.getVar(cmd.args[1]), vm.state.getVar(cmd.args[2]), 0,
                                        numFrames);
        }

        void runScript(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Run scripts from node %d", cmd.op, cmd.args[0]));

            ushort node = vm.state.valueOrVarValue(cmd.args[0]);

            vm.runScriptsFromNode(node, vm.state.getLocationRoom());
        }

        void runScriptWithVar(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Run scripts from node %d with var %d", cmd.op, cmd.args[0], cmd.args[1]));

            vm.state.setVar(26, cmd.args[1]);
            ushort node = vm.state.valueOrVarValue(cmd.args[0]);

            vm.runScriptsFromNode(node, vm.state.getLocationRoom());
        }

        //*/

        void runCommonScript(Context c, Opcode cmd) {
            log(string.Format("Opcode {0}: Run common script {1}", cmd.op, cmd.args[0]));

            vm.runScriptsFromNode((ushort)cmd.args[0], 101, 1);
        }

        /*

        void runCommonScriptWithVar(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Run common script %d with var %d", cmd.op, cmd.args[0], cmd.args[1]));

            vm.state.setVar(26, cmd.args[1]);

            vm.runScriptsFromNode(cmd.args[0], 101, 1);
        }

        void runPuzzle1(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Run puzzle helper %d", cmd.op, cmd.args[0]));

            _puzzles.run(cmd.args[0]);
        }

        void runPuzzle2(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Run puzzle helper %d", cmd.op, cmd.args[0]));

            _puzzles.run(cmd.args[0], cmd.args[1]);
        }

        void runPuzzle3(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Run puzzle helper %d", cmd.op, cmd.args[0]));

            _puzzles.run(cmd.args[0], cmd.args[1], cmd.args[2]);
        }

        void runPuzzle4(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Run puzzle helper %d", cmd.op, cmd.args[0]));

            _puzzles.run(cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]);
        }

        void ambientLoadNode(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Load ambient sounds from node %d %d %d", cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]));

            vm._ambient.loadNode(cmd.args[2], cmd.args[1], cmd.args[0]);
        }

        void ambientReloadCurrentNode(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Reload ambient sounds from current node with fade out delay : %d", cmd.op, cmd.args[0]));

            vm._ambient.loadNode(0, 0, 0);
            vm._ambient.applySounds(vm.state.valueOrVarValue(cmd.args[0]));
        }

        void ambientPlayCurrentNode(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play ambient sounds from current node %d %d", cmd.op, cmd.args[0], cmd.args[1]));

            vm._ambient.playCurrentNode(cmd.args[0], cmd.args[1]);
        }

        void ambientApply(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Apply loadad ambient sounds", cmd.op));

            vm._ambient.applySounds(1);
        }

        void ambientApplyWithFadeDelay(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Apply loadad ambient sounds with fade out delay : %d", cmd.op, cmd.args[0]));

            vm._ambient.applySounds(vm.state.valueOrVarValue(cmd.args[0]));
        }

        void soundPlayBadClick(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play bad click sound", cmd.op));

            vm._sound.playEffect(697, 5);
        }

        void soundPlayBlocking(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play skippable sound %d", cmd.op, cmd.args[0]));

            short soundId = cmd.args[0];
            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            int heading = vm.state.valueOrVarValue(cmd.args[2]);
            int att = vm.state.valueOrVarValue(cmd.args[3]);
            bool nonBlocking = vm.state.valueOrVarValue(cmd.args[4]);
            vm._sound.playEffect(soundId, volume, heading, att);

            if (nonBlocking || !vm._sound.isPlaying(soundId)) {
                return;
            }

            while (vm._sound.isPlaying(soundId) && !vm.inputEscapePressed()) {
                vm.processInput(true);
                vm.drawFrame();
            }
        }

        void soundPlay(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play sound %d", cmd.op, cmd.args[0]));

            vm._sound.playEffect(cmd.args[0], 100);
        }

        void soundPlayVolume(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play sound %d at volume %d", cmd.op, cmd.args[0], cmd.args[1]));

            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            vm._sound.playEffect(cmd.args[0], volume);
        }

        void soundPlayVolumeDirection(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: Play sound %d at volume %d in direction %d",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2]);

            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            int heading = vm.state.valueOrVarValue(cmd.args[2]);
            vm._sound.playEffect(cmd.args[0], volume, heading, 85);
        }

        void soundPlayVolumeDirectionAtt(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: Play sound %d at volume %d in direction %d with attenuation %d",
                    cmd.op, cmd.args[0], cmd.args[1], cmd.args[2], cmd.args[3]);

            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            int heading = vm.state.valueOrVarValue(cmd.args[2]);
            int att = vm.state.valueOrVarValue(cmd.args[3]);
            vm._sound.playEffect(cmd.args[0], volume, heading, att);
        }

        void soundStopEffect(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Stop sound effect %d", cmd.op, cmd.args[0]));

            int id = vm.state.valueOrVarValue(cmd.args[0]);

            vm._sound.stopEffect(id, 0);
        }

        void soundFadeOutEffect(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Stop sound effect %d", cmd.op, cmd.args[0]));

            int id = vm.state.valueOrVarValue(cmd.args[0]);
            int fadeDuration = vm.state.valueOrVarValue(cmd.args[1]);

            vm._sound.stopEffect(id, fadeDuration);
        }

        void soundPlayLooping(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play sound effect looping %d", cmd.op, cmd.args[0]));

            int id = vm.state.valueOrVarValue(cmd.args[0]);

            vm._sound.playEffectLooping(id, 100);
        }

        void soundPlayFadeInOut(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Play sound effect fade in fade out %d", cmd.op, cmd.args[0]));

            int id = vm.state.valueOrVarValue(cmd.args[0]);
            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            int fadeInDuration = vm.state.valueOrVarValue(cmd.args[2]);

            int playDuration;
            if (cmd.args[3] == -1) {
                playDuration = 108000;
            } else {
                playDuration = vm.state.valueOrVarValue(cmd.args[3]);
            }

            int fadeOutDuration = vm.state.valueOrVarValue(cmd.args[4]);

            vm._sound.playEffectFadeInOut(id, volume, 0, 0, fadeInDuration, playDuration, fadeOutDuration);
        }

        void soundChooseNext(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Setup next sound with control var %d", cmd.op, cmd.args[0]));

            short controlVar = cmd.args[0];
            short startSoundId = cmd.args[1];
            short soundCount = cmd.args[2];
            int soundMinDelay = vm.state.valueOrVarValue(cmd.args[3]);
            int soundMaxDelay = vm.state.valueOrVarValue(cmd.args[4]);

            vm._sound.setupNextSound(kNext, controlVar, startSoundId, soundCount, soundMinDelay, soundMaxDelay);
        }

        void soundRandomizeNext(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Setup next sound with control var %d", cmd.op, cmd.args[0]));

            short controlVar = cmd.args[0];
            short startSoundId = cmd.args[1];
            short soundCount = cmd.args[2];
            int soundMinDelay = vm.state.valueOrVarValue(cmd.args[3]);
            int soundMaxDelay = vm.state.valueOrVarValue(cmd.args[4]);

            vm._sound.setupNextSound(kRandom, controlVar, startSoundId, soundCount, soundMinDelay, soundMaxDelay);
        }

        void soundChooseNextAfterOther(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Setup next sound with control var %d", cmd.op, cmd.args[0]));

            short controlVar = cmd.args[0];
            short startSoundId = cmd.args[1];
            short soundCount = cmd.args[2];
            int soundMinDelay = vm.state.valueOrVarValue(cmd.args[3]);
            int soundMaxDelay = vm.state.valueOrVarValue(cmd.args[4]);

            int controlSoundId = vm.state.valueOrVarValue(cmd.args[5]);
            int controlSoundMaxPosition = vm.state.valueOrVarValue(cmd.args[6]);

            vm._sound.setupNextSound(kNextIfOtherStarting, controlVar, startSoundId, soundCount, soundMinDelay, soundMaxDelay, controlSoundId, controlSoundMaxPosition);
        }

        void soundRandomizeNextAfterOther(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Setup next sound with control var %d", cmd.op, cmd.args[0]));

            short controlVar = cmd.args[0];
            short startSoundId = cmd.args[1];
            short soundCount = cmd.args[2];
            int soundMinDelay = vm.state.valueOrVarValue(cmd.args[3]);
            int soundMaxDelay = vm.state.valueOrVarValue(cmd.args[4]);

            int controlSoundId = vm.state.valueOrVarValue(cmd.args[5]);
            int controlSoundMaxPosition = vm.state.valueOrVarValue(cmd.args[6]);

            vm._sound.setupNextSound(kRandomIfOtherStarting, controlVar, startSoundId, soundCount, soundMinDelay, soundMaxDelay, controlSoundId, controlSoundMaxPosition);
        }

        void ambientSetFadeOutDelay(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Set fade out delay : %d", cmd.op, cmd.args[0]));

            vm.state.setAmbiantPreviousFadeOutDelay(cmd.args[0]);
        }

        void ambientAddSound1(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Add ambient sound %d", cmd.op, cmd.args[0]));

            int id = vm.state.valueOrVarValue(cmd.args[0]);
            int volume = vm.state.valueOrVarValue(cmd.args[1]);

            vm._ambient.addSound(id, volume, 0, 0, 0, 0);
        }

        void ambientAddSound2(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Add ambient sound %d", cmd.op, cmd.args[0]));

            int id = vm.state.valueOrVarValue(cmd.args[0]);
            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            int fadeOutDelay = cmd.args[2];

            vm._ambient.addSound(id, volume, 0, 0, 0, fadeOutDelay);
        }

        void ambientAddSound3(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Add ambient sound %d", cmd.op, cmd.args[0]));

            int id = vm.state.valueOrVarValue(cmd.args[0]);
            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            int heading = cmd.args[2];

            vm._ambient.addSound(id, volume, heading, 85, 0, 0);
        }

        void ambientAddSound4(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Add ambient sound %d", cmd.op, cmd.args[0]));

            int id = vm.state.valueOrVarValue(cmd.args[0]);
            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            int heading = cmd.args[2];
            int angle = cmd.args[3];

            vm._ambient.addSound(id, volume, heading, angle, 0, 0);
        }

        void ambientAddSound5(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Add ambient sound %d", cmd.op, cmd.args[0]));

            int id = vm.state.valueOrVarValue(cmd.args[0]);
            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            int u1 = vm.state.valueOrVarValue(cmd.args[2]);

            vm._ambient.addSound(id, volume, 0, 0, u1, 0);
        }

        void ambientSetCue1(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Set ambient cue %d", cmd.op, cmd.args[0]));

            int id = cmd.args[0];
            int volume = vm.state.valueOrVarValue(cmd.args[1]);

            vm._ambient.setCueSheet(id, volume, 0, 0);
        }

        void ambientSetCue2(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Set ambient cue %d", cmd.op, cmd.args[0]));

            int id = cmd.args[0];
            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            int heading = cmd.args[2];

            vm._ambient.setCueSheet(id, volume, heading, 85);
        }

        void ambientSetCue3(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Set ambient cue %d", cmd.op, cmd.args[0]));

            int id = cmd.args[0];
            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            int heading = cmd.args[2];
            int angle = cmd.args[3];

            vm._ambient.setCueSheet(id, volume, heading, angle);
        }

        void ambientSetCue4(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Set ambient cue %d", cmd.op, cmd.args[0]));

            int id = cmd.args[0];
            int volume = vm.state.valueOrVarValue(cmd.args[1]);

            vm._ambient.setCueSheet(id, volume, 32766, 85);
        }

        void runAmbientScriptNode(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: Run ambient script for node %d",
                    cmd.op, cmd.args[0]);

            int node = vm.state.valueOrVarValue(cmd.args[0]);
            vm.runAmbientScripts(node);
        }

        void runAmbientScriptNodeRoomAge(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: Run sound script for node %d, room %d, age %d",
                    cmd.op, cmd.args[2], cmd.args[1], cmd.args[0]);

            int node = vm.state.valueOrVarValue(cmd.args[2]);
            vm._ambient._scriptRoom = vm.state.valueOrVarValue(cmd.args[1]);
            vm._ambient._scriptAge = vm.state.valueOrVarValue(cmd.args[0]);

            vm.runAmbientScripts(node);
            vm._ambient.scaleVolume(vm.state.valueOrVarValue(cmd.args[3]));
        }

        void runSoundScriptNode(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: Run sound script for node %d",
                    cmd.op, cmd.args[0]);

            int node = vm.state.valueOrVarValue(cmd.args[0]);
            vm.runBackgroundSoundScriptsFromNode(node);
        }

        void runSoundScriptNodeRoom(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: Run sound script for node %d, room %d",
                    cmd.op, cmd.args[1], cmd.args[0]);

            int node = vm.state.valueOrVarValue(cmd.args[1]);
            int room = vm.state.valueOrVarValue(cmd.args[0]);
            vm.runBackgroundSoundScriptsFromNode(node, room);
        }

        void runSoundScriptNodeRoomAge(Context c, Opcode cmd) {
            log(string.Format("Opcode %d: Run sound script for node %d, room %d, age %d",
                    cmd.op, cmd.args[2], cmd.args[1], cmd.args[0]);

            int node = vm.state.valueOrVarValue(cmd.args[2]);
            int room = vm.state.valueOrVarValue(cmd.args[1]);
            int age = vm.state.valueOrVarValue(cmd.args[0]);
            vm.runBackgroundSoundScriptsFromNode(node, room, age);
        }

        void soundStopMusic(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Stop music", cmd.op));

            int fadeOutDuration = vm.state.valueOrVarValue(cmd.args[0]);

            vm._sound.stopMusic(fadeOutDuration);
        }

        void movieSetStartupSound(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Set movie startup sound %d", cmd.op, cmd.args[0]));

            int soundId = vm.state.valueOrVarValue(cmd.args[0]);

            vm.state.setMovieStartSoundId(soundId);
            vm.state.setMovieStartSoundVolume(100);
            vm.state.setMovieStartSoundHeading(0);
            vm.state.setMovieStartSoundAttenuation(0);
        }

        void movieSetStartupSoundVolume(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Set movie startup sound %d", cmd.op, cmd.args[0]));

            int soundId = vm.state.valueOrVarValue(cmd.args[0]);
            int volume = vm.state.valueOrVarValue(cmd.args[1]);

            vm.state.setMovieStartSoundId(soundId);
            vm.state.setMovieStartSoundVolume(volume);
            vm.state.setMovieStartSoundHeading(0);
            vm.state.setMovieStartSoundAttenuation(0);
        }

        void movieSetStartupSoundVolumeH(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Set movie startup sound %d", cmd.op, cmd.args[0]));

            int soundId = vm.state.valueOrVarValue(cmd.args[0]);
            int volume = vm.state.valueOrVarValue(cmd.args[1]);
            int heading = vm.state.valueOrVarValue(cmd.args[2]);

            vm.state.setMovieStartSoundId(soundId);
            vm.state.setMovieStartSoundVolume(volume);
            vm.state.setMovieStartSoundHeading(heading);
            vm.state.setMovieStartSoundAttenuation(0);
        }

        void drawOneFrame(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Draw one frame", cmd.op));

            vm.processInput(true);
            vm.drawFrame();
        }

        void cursorHide(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Hide cursor", cmd.op));

            vm._cursor.setVisible(false);
        }

        void cursorShow(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Show cursor", cmd.op));

            vm._cursor.setVisible(true);
        }

        void cursorSet(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Set cursor %d", cmd.op, cmd.args[0]));

            vm._cursor.changeCursor(cmd.args[0]);
        }

        void cursorLock(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Lock cursor", cmd.op));

            vm.state.setCursorLocked(true);
        }

        void cursorUnlock(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Unlock cursor", cmd.op));

            vm.state.setCursorLocked(false);
        }

        void dialogOpen(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: Open dialog %d", cmd.op, cmd.args[0]));

            ushort dialog = vm.state.valueOrVarValue(cmd.args[0]);
            short result = vm.openDialog(dialog);
            vm.state.setDialogResult(result);
        }

        void newGame(Context c, Opcode cmd) {
            //log(string.Format("Opcode %d: New game", cmd.op));

            vm.state.newGame();
            vm._inventory.reset();
        }







        //*/

    }
}

