using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 414,649

namespace Myst3
{
    public enum ViewType {
        kCube = 1,
        kFrame = 2,
        kMenu = 3
    };

    [Serializable]
    public class GameState
    {
        Platform platform;
        Database db;
        Dictionary<string, VarDescription> varDescriptions; // holds most of the variables for the whole game's progress
        StateData data;
        static uint kSaveVersion = 149;

        class VarDescription
        {
            public VarDescription()
            { variable = 0; name = ""; unknown = false; }

            public VarDescription(ushort v, string n, bool u)
            { variable = v; name = n; unknown = u; }

            public ushort variable;
            public string name;
            public bool unknown;
        }
        class StateData
        {
            public uint version;
            public uint gameRunning;
            public uint tickCount;
            public uint nextSecondsUpdate;
            public uint secondsPlayed;
            public uint dword_4C2C44;
            public uint dword_4C2C48;
            public uint dword_4C2C4C;
            public uint dword_4C2C50;
            public uint dword_4C2C54;
            public uint dword_4C2C58;
            public uint dword_4C2C5C;
            public uint dword_4C2C60;
            public uint currentNodeType;
            public float lookatPitch;
            public float lookatHeading;
            public float lookatFOV;
            public float pitchOffset;
            public float headingOffset;
            public uint limitCubeCamera;
            public float minPitch;
            public float maxPitch;
            public float minHeading;
            public float maxHeading;
            public uint  dword_4C2C90;
            public int[] vars;
            public uint inventoryCount;
            public uint[] inventoryList;
            public uint[] zipDestinations;

            public byte saveDay;
            public byte saveMonth;
            public ushort saveYear;

            public byte saveHour;
            public byte saveMinute;

            public string saveDescription;

            // public Common::SharedPtr<Graphics::Surface> thumbnail; // TODO: serializable equivalent of Texture2D

            public StateData()
            {
                version = GameState.kSaveVersion;
                gameRunning = 1;
                tickCount = 0;
                nextSecondsUpdate = 0;
                secondsPlayed = 0;
                dword_4C2C44 = 0;
                dword_4C2C48 = 0;
                dword_4C2C4C = 0;
                dword_4C2C50 = 0;
                dword_4C2C54 = 0;
                dword_4C2C58 = 0;
                dword_4C2C5C = 0;
                dword_4C2C60 = 0;
                currentNodeType = 0;
                lookatPitch = 0;
                lookatHeading = 0;
                lookatFOV = 0;
                pitchOffset = 0;
                headingOffset = 0;
                limitCubeCamera = 0;
                minPitch = 0;
                maxPitch = 0;
                minHeading = 0;
                maxHeading = 0;
                dword_4C2C90 = 0;

                vars = new int[2048];
                for (int i = 0; i < 2048; i++)
                    vars[i] = 0;

                vars[0] = 0;
                vars[1] = 1;

                inventoryCount = 0;

                inventoryList = new uint[7];
                for (int i = 0; i < 7; i++)
                    inventoryList[i] = 0;

                zipDestinations = new uint[64];
                for (int i = 0; i < 64; i++)
                    zipDestinations[i] = 0;

                saveDay = 0;
                saveMonth = 0;
                saveYear = 0;
                saveHour = 0;
                saveMinute = 0;
            }
            // void syncWithSaveGame(Common::Serializer &s)
            // {
            // }
            // void resizeThumbnail(Graphics::Surface *small)
            // {

            // }
        };

        public GameState(Platform platform, Database database)
        {
            this.platform = platform;
            this.db = database;

            varDescriptions = new Dictionary<string, VarDescription>();

            varSetDefaultVal(14, "CursorTransparency", false);

            varSetDefaultVal(47, "ProjectorAngleX", false);
            varSetDefaultVal(48, "ProjectorAngleY", false);
            varSetDefaultVal(49, "ProjectorAngleZoom", false);
            varSetDefaultVal(50, "ProjectorAngleBlur", false);
            varSetDefaultVal(51, "DraggedWeight", false);

            varSetDefaultVal(57, "DragEnded", false);
            varSetDefaultVal(58, "DragLeverSpeed", false);
            varSetDefaultVal(59, "DragPositionFound", false);
            varSetDefaultVal(60, "DragLeverPositionChanged", false);

            varSetDefaultVal(61, "LocationAge", false);
            varSetDefaultVal(62, "LocationRoom", false);
            varSetDefaultVal(63, "LocationNode", false);
            varSetDefaultVal(64, "BookSavedAge", false);
            varSetDefaultVal(65, "BookSavedRoom", false);
            varSetDefaultVal(66, "BookSavedNode", false);
            varSetDefaultVal(67, "MenuSavedAge", false);
            varSetDefaultVal(68, "MenuSavedRoom", false);
            varSetDefaultVal(69, "MenuSavedNode", false);

            varSetDefaultVal(70, "SecondsCountdown", false);
            varSetDefaultVal(71, "TickCountdown", false);

            // Counters, unused by the game scripts
            varSetDefaultVal(76, "CounterUnk76", false);
            varSetDefaultVal(77, "CounterUnk77", false);
            varSetDefaultVal(78, "CounterUnk78", false);

            varSetDefaultVal(79, "SweepEnabled", false);
            varSetDefaultVal(80, "SweepValue", false);
            varSetDefaultVal(81, "SweepStep", false);
            varSetDefaultVal(82, "SweepMin", false);
            varSetDefaultVal(83, "SweepMax", false);

            varSetDefaultVal(84, "InputMousePressed", false);
            varSetDefaultVal(88, "InputEscapePressed", false);
            varSetDefaultVal(89, "InputTildePressed", false);
            varSetDefaultVal(90, "InputSpacePressed", false);

            varSetDefaultVal(92, "HotspotActiveRect", false);

            varSetDefaultVal(93, "WaterEffectRunning", false);
            varSetDefaultVal(94, "WaterEffectActive", false);
            varSetDefaultVal(95, "WaterEffectSpeed", false);
            varSetDefaultVal(96, "WaterEffectAttenuation", false);
            varSetDefaultVal(97, "WaterEffectFrequency", false);
            varSetDefaultVal(98, "WaterEffectAmpl", false);
            varSetDefaultVal(99, "WaterEffectMaxStep", false);
            varSetDefaultVal(100, "WaterEffectAmplOffset", false);

            varSetDefaultVal(101, "LavaEffectActive", false);
            varSetDefaultVal(102, "LavaEffectSpeed", false);
            varSetDefaultVal(103, "LavaEffectAmpl", false);
            varSetDefaultVal(104, "LavaEffectStepSize", false);

            varSetDefaultVal(105, "MagnetEffectActive", false);
            varSetDefaultVal(106, "MagnetEffectSpeed", false);
            varSetDefaultVal(107, "MagnetEffectUnk1", false);
            varSetDefaultVal(108, "MagnetEffectUnk2", false);
            varSetDefaultVal(109, "MagnetEffectSound", false);
            varSetDefaultVal(110, "MagnetEffectNode", false);
            varSetDefaultVal(111, "MagnetEffectUnk3", false);

            varSetDefaultVal(112, "ShakeEffectAmpl", false);
            varSetDefaultVal(113, "ShakeEffectTickPeriod", false);
            varSetDefaultVal(114, "RotationEffectSpeed", false);
            varSetDefaultVal(115, "SunspotIntensity", false);
            varSetDefaultVal(116, "SunspotColor", false);
            varSetDefaultVal(117, "SunspotRadius", false);

            varSetDefaultVal(119, "AmbiantFadeOutDelay", false);
            varSetDefaultVal(120, "AmbiantPreviousFadeOutDelay", false);
            varSetDefaultVal(121, "AmbientOverrideFadeOutDelay", false);
            varSetDefaultVal(122, "SoundScriptsSuspended", false);

            varSetDefaultVal(124, "SoundNextMultipleSounds", false);
            varSetDefaultVal(125, "SoundNextIsChoosen", false);
            varSetDefaultVal(126, "SoundNextId", false);
            varSetDefaultVal(127, "SoundNextIsLast", false);
            varSetDefaultVal(128, "SoundScriptsTimer", false);
            varSetDefaultVal(129, "SoundScriptsPaused", false);
            varSetDefaultVal(130, "SoundScriptFadeOutDelay", false);

            varSetDefaultVal(131, "CursorLocked", false);
            varSetDefaultVal(132, "CursorHidden", false);

            varSetDefaultVal(136, "CameraPitch", false);
            varSetDefaultVal(137, "CameraHeading", false);
            varSetDefaultVal(140, "CameraMinPitch", false);
            varSetDefaultVal(141, "CameraMaxPitch", false);

            varSetDefaultVal(142, "MovieStartFrame", false);
            varSetDefaultVal(143, "MovieEndFrame", false);
            varSetDefaultVal(144, "MovieVolume1", false);
            varSetDefaultVal(145, "MovieVolume2", false);
            varSetDefaultVal(146, "MovieOverrideSubtitles", false);

            varSetDefaultVal(149, "MovieConditionBit", false);
            varSetDefaultVal(150, "MoviePreloadToMemory", false);
            varSetDefaultVal(151, "MovieScriptDriven", false);
            varSetDefaultVal(152, "MovieNextFrameSetVar", false);
            varSetDefaultVal(153, "MovieNextFrameGetVar", false);
            varSetDefaultVal(154, "MovieStartFrameVar", false);
            varSetDefaultVal(155, "MovieEndFrameVar", false);
            varSetDefaultVal(156, "MovieForce2d", false);
            varSetDefaultVal(157, "MovieVolumeVar", false);
            varSetDefaultVal(158, "MovieSoundHeading", false);
            varSetDefaultVal(159, "MoviePanningStrenght", false);
            varSetDefaultVal(160, "MovieSynchronized", false);

            // We ignore this, and never skip frames
            varSetDefaultVal(161, "MovieNoFrameSkip", false);

            // Only play the audio track. This is used in TOHO 3 only.
            // Looks like it works fine without any specific implementation
            varSetDefaultVal(162, "MovieAudioOnly", false);

            varSetDefaultVal(163, "MovieOverrideCondition", false);
            varSetDefaultVal(164, "MovieUVar", false);
            varSetDefaultVal(165, "MovieVVar", false);
            varSetDefaultVal(166, "MovieOverridePosition", false);
            varSetDefaultVal(167, "MovieOverridePosU", false);
            varSetDefaultVal(168, "MovieOverridePosV", false);
            varSetDefaultVal(169, "MovieScale", false);
            varSetDefaultVal(170, "MovieAdditiveBlending", false);
            varSetDefaultVal(171, "MovieTransparency", false);
            varSetDefaultVal(172, "MovieTransparencyVar", false);
            varSetDefaultVal(173, "MoviePlayingVar", false);
            varSetDefaultVal(174, "MovieStartSoundId", false);
            varSetDefaultVal(175, "MovieStartSoundVolume", false);
            varSetDefaultVal(176, "MovieStartSoundHeading", false);
            varSetDefaultVal(177, "MovieStartSoundAttenuation", false);

            varSetDefaultVal(178, "MovieUseBackground", false);
            varSetDefaultVal(179, "CameraSkipAnimation", false);
            varSetDefaultVal(180, "MovieAmbiantScriptStartFrame", false);
            varSetDefaultVal(181, "MovieAmbiantScript", false);
            varSetDefaultVal(182, "MovieScriptStartFrame", false);
            varSetDefaultVal(183, "MovieScript", false);

            varSetDefaultVal(185, "CameraMoveSpeed", false);

            // We always allow missing SpotItem data
            varSetDefaultVal(186, "SpotItemAllowMissing", false);

            varSetDefaultVal(187, "TransitionSound", false);
            varSetDefaultVal(188, "TransitionSoundVolume", false);
            varSetDefaultVal(189, "LocationNextNode", false);
            varSetDefaultVal(190, "LocationNextRoom", false);
            varSetDefaultVal(191, "LocationNextAge", false);

            varSetDefaultVal(195, "BallPosition", false);
            varSetDefaultVal(196, "BallFrame", false);
            varSetDefaultVal(197, "BallLeverLeft", false);
            varSetDefaultVal(198, "BallLeverRight", false);

            varSetDefaultVal(228, "BallDoorOpen", false);

            varSetDefaultVal(243, "ProjectorX", false);
            varSetDefaultVal(244, "ProjectorY", false);
            varSetDefaultVal(245, "ProjectorZoom", false);
            varSetDefaultVal(246, "ProjectorBlur", false);
            varSetDefaultVal(247, "ProjectorAngleXOffset", false);
            varSetDefaultVal(248, "ProjectorAngleYOffset", false);
            varSetDefaultVal(249, "ProjectorAngleZoomOffset", false);
            varSetDefaultVal(250, "ProjectorAngleBlurOffset", false);

            varSetDefaultVal(277, "JournalAtrusState", false);
            varSetDefaultVal(279, "JournalSaavedroState", false);
            varSetDefaultVal(280, "JournalSaavedroClosed", false);
            varSetDefaultVal(281, "JournalSaavedroOpen", false);
            varSetDefaultVal(282, "JournalSaavedroLastPage", false);
            varSetDefaultVal(283, "JournalSaavedroChapter", false);
            varSetDefaultVal(284, "JournalSaavedroPageInChapter", false);

            varSetDefaultVal(329, "TeslaAllAligned", false);
            varSetDefaultVal(330, "TeslaTopAligned", false);
            varSetDefaultVal(331, "TeslaMiddleAligned", false);
            varSetDefaultVal(332, "TeslaBottomAligned", false);
            varSetDefaultVal(333, "TeslaMovieStart", false);

            // Amateria ambient sound / movie counters (XXXX 1001 and XXXX 1002)
            varSetDefaultVal(406, "AmateriaSecondsCounter", false);
            varSetDefaultVal(407, "AmateriaTicksCounter", false);

            varSetDefaultVal(444, "ResonanceRingsSolved", false);

            varSetDefaultVal(460, "PinballRemainingPegs", false);

            varSetDefaultVal(475, "OuterShieldUp", false);
            varSetDefaultVal(476, "InnerShieldUp", false);
            varSetDefaultVal(479, "SaavedroStatus", false);

            varSetDefaultVal(480, "BookStateTomahna", false);
            varSetDefaultVal(481, "BookStateReleeshahn", false);

            varSetDefaultVal(489, "SymbolCode2Solved", false);
            varSetDefaultVal(495, "SymbolCode1AllSolved", false);
            varSetDefaultVal(496, "SymbolCode1CurrentSolved", false);
            varSetDefaultVal(497, "SymbolCode1TopSolved", false);
            varSetDefaultVal(502, "SymbolCode1LeftSolved", false);
            varSetDefaultVal(507, "SymbolCode1RightSolved", false);

            varSetDefaultVal(540, "SoundVoltaicUnk540", false);
            varSetDefaultVal(587, "SoundEdannaUnk587", false);
            varSetDefaultVal(627, "SoundAmateriaUnk627", false);
            varSetDefaultVal(930, "SoundAmateriaUnk930", false);
            varSetDefaultVal(1031, "SoundEdannaUnk1031", false);
            varSetDefaultVal(1146, "SoundVoltaicUnk1146", false);

            varSetDefaultVal(1322, "ZipModeEnabled", false);
            varSetDefaultVal(1323, "SubtitlesEnabled", false);
            varSetDefaultVal(1324, "WaterEffects", false);
            varSetDefaultVal(1325, "TransitionSpeed", false);
            varSetDefaultVal(1326, "MouseSpeed", false);
            varSetDefaultVal(1327, "DialogResult", false);

            varSetDefaultVal(1395, "HotspotIgnoreClick", false);
            varSetDefaultVal(1396, "HotspotHovered", false);
            varSetDefaultVal(1397, "SpotSubtitle", false);

            // Override node from which effect masks are loaded
            // This is only used in LEIS x75, but is useless
            // since all the affected nodes have the same effect masks
            varSetDefaultVal(1398, "EffectsOverrideMaskNode", false);

            varSetDefaultVal(1399, "DragLeverLimited", false);
            varSetDefaultVal(1400, "DragLeverLimitMin", false);
            varSetDefaultVal(1401, "DragLeverLimitMax", false);

            // Mouse unk
            varSetDefaultVal(6, "Unk6", true);

            // Backup var for opcodes 245, 246 => find usage
            varSetDefaultVal(13, "Unk13", true);

            // ???
            varSetDefaultVal(147, "MovieUnk147", true);
            varSetDefaultVal(148, "MovieUnk148", true);

            if (platform != Platform.kPlatformXbox)
            {
                varSetDefaultVal(1337, "MenuEscapePressed", false);
                varSetDefaultVal(1338, "MenuNextAction", false);
                varSetDefaultVal(1339, "MenuLoadBack", false);
                varSetDefaultVal(1340, "MenuSaveBack", false);
                varSetDefaultVal(1341, "MenuSaveAction", false);
                varSetDefaultVal(1342, "MenuOptionsBack", false);

                varSetDefaultVal(1350, "MenuSaveLoadPageLeft", false);
                varSetDefaultVal(1351, "MenuSaveLoadPageRight", false);
                varSetDefaultVal(1352, "MenuSaveLoadSelectedItem", false);
                varSetDefaultVal(1353, "MenuSaveLoadCurrentPage", false);

                // Menu stuff does not look like it's too useful
                varSetDefaultVal(1361, "Unk1361", true);
                varSetDefaultVal(1362, "Unk1362", true);
                varSetDefaultVal(1363, "Unk1363", true);

                varSetDefaultVal(1374, "OverallVolume", false);
                varSetDefaultVal(1377, "MusicVolume", false);
                varSetDefaultVal(1380, "MusicFrequency", false);
                varSetDefaultVal(1393, "LanguageAudio", false);
                varSetDefaultVal(1394, "LanguageText", false);

                varSetDefaultVal(1406, "ShieldEffectActive", false);
            }
            else
            {
                shiftVariables(927, 1);
                shiftVariables(1031, 2);
                shiftVariables(1395, -22);

                varSetDefaultVal(1340, "MenuSavesAvailable", false);
                varSetDefaultVal(1341, "MenuNextAction", false);
                varSetDefaultVal(1342, "MenuLoadBack", false);
                varSetDefaultVal(1343, "MenuSaveBack", false);
                varSetDefaultVal(1344, "MenuSaveAction", false);
                varSetDefaultVal(1345, "MenuOptionsBack", false);
                varSetDefaultVal(1346, "MenuSelectedSave", false);

                varSetDefaultVal(1384, "MovieOptional", false);
                varSetDefaultVal(1386, "VibrationEnabled", false);

                varSetDefaultVal(1430, "GamePadActionPressed", false);
                varSetDefaultVal(1431, "GamePadDownPressed", false);
                varSetDefaultVal(1432, "GamePadUpPressed", false);
                varSetDefaultVal(1433, "GamePadLeftPressed", false);
                varSetDefaultVal(1434, "GamePadRightPressed", false);
                varSetDefaultVal(1435, "GamePadCancelPressed", false);

                varSetDefaultVal(1437, "DragWithDirectionKeys", false);
                varSetDefaultVal(1438, "MenuAttractCountDown", false);
                varSetDefaultVal(1439, "ShieldEffectActive", false);

                varSetDefaultVal(1445, "StateCanSave", false);
            }

            newGame();
        }

        void varSetDefaultVal(ushort variable, string name, bool value)
        {
            varDescriptions[name] = new VarDescription(variable, name, value);
        }

        void shiftVariables(ushort _base, int value)
        {
            foreach (VarDescription varValue in varDescriptions.Values)
            {
                if (varValue.variable >= _base)
                    varValue.variable += (ushort)value;
            }
        }

        void newGame()
        {
            data = new StateData();
        }

        VarDescription findDescription(ushort variable)
        {
            foreach (VarDescription varValue in varDescriptions.Values)
            {
                if (varValue.variable == variable)
                    return varValue;
            }

            return new VarDescription();
        }

        public void setVar(short variable, int value)
        { setVar((ushort) variable, value); }
        public void setVar(int variable, int value)
        { setVar((ushort) variable, value); }
        public void setVar(short variable, bool value)
        { setVar((ushort) variable, value ? 1 : 0); }
        public void setVar(ushort variable, int value)
        {
            checkRange(variable);

            // if (DebugMan.isDebugChannelEnabled(kDebugVariable))
            if (true)
            {
                VarDescription d = findDescription(variable);

                if (!string.IsNullOrEmpty(d.name) && d.unknown)
                    Debug.LogWarning("A script is writing to the unimplemented engine-mapped var " + variable + " (" + d.name + ")");
            }

            data.vars[variable] = value;
        }

        public int getVar(short variable)
        { return getVar((ushort) variable); }
        public int getVar(int variable)
        { return getVar((ushort) variable); }
        public int getVar(ushort variable)
        {
            checkRange(variable);

            return data.vars[variable];
        }

        public bool hasVar(string name)
        { return varDescriptions.ContainsKey(name); }

        public int getVar(string varName)
        {
            if (!varDescriptions.ContainsKey(varName))
                throw new Exception("The engine is trying to access an undescribed var (" + varName + ")");

            VarDescription d = varDescriptions[varName];

            return data.vars[d.variable];
        }

        public void setVar(string varName, bool value)
        { setVar(varName, value ? 1 : 0); }
        public void setVar(string varName, int value)
        {
            if (!varDescriptions.ContainsKey(varName))
                throw new Exception("The engine is trying to access an undescribed var (" + varName + ")");

            VarDescription d = varDescriptions[varName];

            data.vars[d.variable] = value;
        }

        void checkRange(ushort variable)
        {
            if (variable < 1 || variable > 2047)
                throw new Exception("Variable out of range " + variable);
        }

        public bool evaluate(short condition)
        {
            ushort unsignedCond = (ushort)(condition < 0 ? -condition : condition);
            ushort var = (ushort)(unsignedCond & 2047);
            int varValue = getVar(var);
            int targetValue = (unsignedCond >> 11) - 1;

            if (targetValue >= 0)
            {
                if (condition >= 0)
                    return varValue == targetValue;
                else
                    return varValue != targetValue;
            }
            else
                {
                if (condition >= 0)
                    return varValue != 0;
                else
                    return varValue == 0;
            }
        }

        public void markNodeAsVisited(ushort node, ushort room, uint age)
        {
            int zipBitIndex = db.getNodeZipBitIndex(node, room, age);

            int arrayIndex = zipBitIndex / 32;
            if (!(arrayIndex < 64))
                throw new Exception("assert(arrayIndex < 64)");

            data.zipDestinations[arrayIndex] = (ushort)((int)data.zipDestinations[arrayIndex] | (1 << (zipBitIndex % 32)));
        }

        public int valueOrVarValue(uint value)
        { return valueOrVarValue((short) value); }
        public int valueOrVarValue(ushort value)
        { return valueOrVarValue((short) value); }
        public int valueOrVarValue(short value)
        {
            if (value < 0)
                return getVar((ushort)-value);

            return value;
        }

        public ViewType getViewType()                   { return (ViewType)data.currentNodeType; }
        public void setViewType(ViewType t)             { data.currentNodeType = (uint)t; }

        public float getLookAtFOV()                     { return data.lookatFOV; }
        public void setLookAtFOV(float fov)             { data.lookatFOV = fov; }
        public float getLookAtPitch()                   { return data.lookatPitch; }
        public float getLookAtHeading()                 { return data.lookatHeading; }
        public void lookAt(float pitch, float heading)  { data.lookatPitch = pitch; data.lookatHeading = heading; }

        public void limitCubeCamera(float minPitch, float maxPitch, float minHeading, float maxHeading)
        {
            data.limitCubeCamera = 1;
            data.minPitch = minPitch;
            data.maxPitch = maxPitch;
            data.minHeading = minHeading;
            data.maxHeading = maxHeading;
        }
        public void freeCubeCamera()                    { data.limitCubeCamera = 0; }
        public bool isCameraLimited()                   { return data.limitCubeCamera != 0; }
        public float getMinPitch()                      { return data.minPitch; }
        public float getMaxPitch()                      { return data.maxPitch; }
        public float getMinHeading()                    { return data.minHeading; }
        public float getMaxHeading()                    { return data.maxHeading; }

        public bool isZipDestinationAvailable(ushort node, ushort room, uint age)
        {
            int zipBitIndex = db.getNodeZipBitIndex(node, room, age);

            int arrayIndex = zipBitIndex / 32;
            if (!(arrayIndex < 64))
                throw new Exception("assert(arrayIndex < 64)");

            return (data.zipDestinations[arrayIndex] & (1 << (zipBitIndex % 32))) != 0;
        }
    }
}
