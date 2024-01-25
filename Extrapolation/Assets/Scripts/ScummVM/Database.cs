using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Myst3
{
    public enum GameLocalizationType {
        kLocMonolingual,
        kLocMulti2,
        kLocMulti6
    }

    public enum MystLanguage {
        kEnglish = 0,
        kOther   = 1, // Dutch, Japanese or Polish
        kDutch   = 1,
        kFrench  = 2,
        kGerman  = 3,
        kItalian = 4,
        kSpanish = 5
    }

    public class NodeData {
        public short id;
        public short zipBitIndex;
        public List<CondScript> scripts = new List<CondScript>();
        public List<HotSpot> hotspots = new List<HotSpot>();
        public List<CondScript> soundScripts = new List<CondScript>();
        public List<CondScript> backgroundSoundScripts = new List<CondScript>();
    }

    public struct RoomData {
        public uint id;
        public string name;

        public RoomData(uint id, string name)
        {
            this.id = id; this.name = name;
        }
    }

    struct RoomKey {
        public ushort ageID;
        public ushort roomID;

        public RoomKey(ushort room, ushort age)
        {
            roomID = room;
            ageID = age;
        }
    }

    public struct AgeData {
        public uint id;
        public uint disk;
        public uint roomCount;
        public RoomData[] rooms;
        public uint labelId;

        public AgeData(uint id, uint disk, uint roomCount, RoomData[] rooms, uint labelId)
        {
            this.id = id;
            this.disk = disk;
            this.roomCount = roomCount;
            this.rooms = rooms;
            this.labelId = labelId;
        }
    }

    struct AmbientCue {
        public ushort id;
        public ushort minFrames;
        public ushort maxFrames;
        public List<ushort> tracks;
    }

    /**
    * Script types stored in 'myst3.dat'
    */
    enum ScriptType {
        kScriptTypeNode,
        kScriptTypeAmbientSound,
        kScriptTypeBackgroundSound,
        kScriptTypeNodeInit,
        kScriptTypeAmbientCue
    }

    /**
    * A script index entry in the 'myst3.dat' file
    */
    struct RoomScripts {
        public string room;
        public ScriptType type;
        public uint offset;
        public uint size;
    }

    /**
    * A collection of functions used to read script related data
    */
    static class ScriptData {
        public static List<CondScript> readCondScripts(BinaryReader br)
        {
            List<CondScript> scripts = new List<CondScript>();
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                CondScript script = readCondScript(br);
                if (script.condition == 0)
                    break;
                scripts.Add(script);
            }

            return scripts;
        }
        public static List<Opcode> readOpcodes(BinaryReader br)
        {
            List<Opcode> script = new List<Opcode>();

            Stream s = br.BaseStream;

            while (s.Position < s.Length) {
                Opcode opcode = new Opcode();
                opcode.args = new List<short>();
                ushort code = br.ReadUInt16();

                opcode.op = (byte)(code & 0xff);
                byte count = (byte)(code >> 8);
                if (count == 0 && opcode.op == 0)
                    break;

                for (int i = 0; i < count; i++) {
                    short value = br.ReadInt16();
                    opcode.args.Add(value);
                }

                script.Add(opcode);
            }

            return script;
        }
        public static List<HotSpot> readHotspots(BinaryReader br)
        {
            List<HotSpot> scripts = new List<HotSpot>();

            Stream s = br.BaseStream;

            while (s.Position < s.Length) {
                HotSpot hotspot = readHotspot(br);

                if (hotspot.condition == 0)
                    break;

                scripts.Add(hotspot);
            }

            return scripts;
        }
        public static List<PolarRect> readRects(BinaryReader br)
        {
            List<PolarRect> rects = new List<PolarRect>();

            Stream s = br.BaseStream;

            bool lastRect = false;
            do {
                PolarRect rect;
                rect.centerPitch = br.ReadInt16();
                rect.centerHeading = br.ReadInt16();
                rect.width = br.ReadInt16();
                rect.height = br.ReadInt16();

                if (rect.width < 0) {
                    rect.width = (short)-rect.width;
                } else {
                    lastRect = true;
                }

                rects.Add(rect);
            } while (!lastRect && s.Position < s.Length);

            return rects;
        }
        public static CondScript readCondScript(BinaryReader br)
        {
            CondScript script = new CondScript();
            script.condition = br.ReadUInt16();
            if(script.condition == 0)
                return script;

            // WORKAROUND: Original data bug in MATO 32765
            // The script data for node MATO 32765 is missing its first two bytes
            // of data, resulting in incorrect opcodes being read

            // Original disassembly:
            // init 0 > c[v565 != 0]
            //     op 115, ifVarInRange ( )
            //     op 45, inventoryAddBack ( )
            //     op 53, varSetValue ( vSunspotColor 4090 )
            //     op 53, varSetValue ( vSunspotRadius 40 )
            //     op 33, waterEffectSetWave ( 100 80 )
            //     op 32, waterEffectSetAttenuation ( 359 )
            //     op 31, waterEffectSetSpeed ( 15 )

            // Fixed disassembly
            // init 0 > c[v1 != 0]
            //     op 53, varSetValue ( vSunspotIntensity 45 )
            //     op 53, varSetValue ( vSunspotColor 4090 )
            //     op 53, varSetValue ( vSunspotRadius 40 )
            //     op 33, waterEffectSetWave ( 100 80 )
            //     op 32, waterEffectSetAttenuation ( 359 )
            //     op 31, waterEffectSetSpeed ( 15 )

            if (script.condition == 565) {
                script.condition = 1;
                br.BaseStream.Position -= 2;
            }
            // END WORKAROUND

            script.script = readOpcodes(br);

            return script;
        }
        public static HotSpot readHotspot(BinaryReader br)
        {
            HotSpot hotspot = new HotSpot();
            hotspot.condition = br.ReadInt16();

            if (hotspot.condition == 0)
                return hotspot;

            if (hotspot.condition != -1) {
                hotspot.rects = readRects(br);
                hotspot.cursor = br.ReadInt16();
            }

            hotspot.script = readOpcodes(br);

            return hotspot;
        }
    }








    public class Database
    {
        #pragma warning disable 414
        Platform platform;
        #pragma warning restore 414
        Language language;
        GameLocalizationType localizationType;
        BinaryReader datFile;

        uint kDatVersion = 2;
        List<RoomScripts> roomScriptsIndex;
        public List<Opcode> nodeInitScript { get; private set; }
        Dictionary<uint, string> soundNames;
        Dictionary<ushort, AmbientCue> ambientCues;
        Dictionary<uint, short> roomZipBitIndex;
        Dictionary<RoomKey, List<NodeData>> roomNodesCache;
        int roomScriptsStartOffset;

        static RoomData[] roomsXXXX = {
                new RoomData(101, "XXXX")
        };
        static RoomData[] roomsINTR = {
                new RoomData(201, "INTR")
        };
        static RoomData[] roomsTOHO = {
                // Tomahna (beginning)
                new RoomData(301, "TOHO")
        };
        static RoomData[] roomsTOHB = {
                // Tomahna (endgame)
                new RoomData(401, "TOHB")
        };
        static RoomData[] roomsLE = {
                // J'nanin
                new RoomData(501, "LEIS"),
                new RoomData(502, "LEOS"),
                new RoomData(503, "LEET"),
                new RoomData(504, "LELT"),
                new RoomData(505, "LEMT"),
                new RoomData(506, "LEOF")
        };
        static RoomData[] roomsLI = {
                // Edanna
                new RoomData(601, "LIDR"),
                new RoomData(602, "LISW"),
                new RoomData(603, "LIFO"),
                new RoomData(604, "LISP"),
                new RoomData(605, "LINE")
        };
        static RoomData[] roomsEN = {
                // Voltaic (ENergy)
                new RoomData(701, "ENSI"),
                new RoomData(703, "ENPP"),
                new RoomData(704, "ENEM"),
                new RoomData(705, "ENLC"),
                new RoomData(706, "ENDD"),
                new RoomData(707, "ENCH"),
                new RoomData(708, "ENLI")
        };
        static RoomData[] roomsNA = {
                // NArayan
                new RoomData(801, "NACH")
        };
        static RoomData[] roomsMENU = {
                // Menu (duh)
                new RoomData(901, "MENU"),
                new RoomData(902, "JRNL"),
                new RoomData(903, "DEMO"),
                new RoomData(904, "ATIX")
        };
        static RoomData[] roomsMA = {
                // AMAteria
                new RoomData(1001, "MACA"),
                new RoomData(1002, "MAIS"),
                new RoomData(1003, "MALL"),
                new RoomData(1004, "MASS"),
                new RoomData(1005, "MAWW"),
                new RoomData(1006, "MATO")
        };
        static RoomData[] roomsLOGO = {
                new RoomData(1101, "LOGO")
        };

        public static AgeData[] ages = {
            new AgeData( 1, 0, 1, roomsXXXX, 0 ),
            new AgeData( 2, 1, 1, roomsINTR, 0 ),
            new AgeData( 3, 2, 1, roomsTOHO, 0 ),
            new AgeData( 4, 4, 1, roomsTOHB, 0 ),
            new AgeData( 5, 2, 6, roomsLE, 1 ),
            new AgeData( 6, 4, 5, roomsLI, 2 ),
            new AgeData( 7, 3, 7, roomsEN, 3 ),
            new AgeData( 8, 3, 1, roomsNA, 4 ),
            new AgeData( 9, 0, 4, roomsMENU, 0 ),
            new AgeData( 10, 1, 6, roomsMA, 5 ),
            new AgeData( 11, 0, 1, roomsLOGO, 0 )
        };

        public Database(Platform platform, Language language, GameLocalizationType localizationType)
        {
            this.platform = platform;
            this.language = language;
            this.localizationType = localizationType;

            roomScriptsIndex = new List<RoomScripts>();
            soundNames = new Dictionary<uint, string>();
            ambientCues = new Dictionary<ushort, AmbientCue>();
            roomZipBitIndex = new Dictionary<uint, short>();
            roomNodesCache = new Dictionary<RoomKey, List<NodeData>>();

            FileStream m3dat = FileManager.getDatabase();
            if (m3dat == null)
                throw new Exception("Couldn't find myst3.dat !");
            datFile = new BinaryReader(m3dat);

            uint magic = datFile.ReadUInt32();
            if (magic != Common.MKTAG('M', 'Y', 'S', 'T'))
                throw new Exception("'myst3.dat' is invalid");

            uint version = datFile.ReadUInt32();
            if (version != kDatVersion)
                throw new Exception("Incorrect 'myst3.dat' version. Expected '" + kDatVersion + "', found '" + version + "'");

            bool isWindowMacVersion = platform == Platform.kPlatformWindows || platform == Platform.kPlatformMacintosh;
            bool isXboxVersion = platform == Platform.kPlatformXbox;

            readScriptIndex(datFile, isWindowMacVersion);                                                                   // Main scripts
            readScriptIndex(datFile, isWindowMacVersion && localizationType == GameLocalizationType.kLocMulti6);            // Menu scripts 6 languages version
            readScriptIndex(datFile, isWindowMacVersion && localizationType == GameLocalizationType.kLocMulti2);            // Menu scripts 2 languages CD version
            readScriptIndex(datFile, isWindowMacVersion && localizationType == GameLocalizationType.kLocMonolingual);       // Menu scripts english CD version
            readScriptIndex(datFile, isXboxVersion);                                                                        // Main scripts Xbox version
            readScriptIndex(datFile, isXboxVersion && localizationType != GameLocalizationType.kLocMonolingual);            // Menu scripts PAL Xbox version
            readScriptIndex(datFile, isXboxVersion && localizationType == GameLocalizationType.kLocMonolingual);            // Menu scripts NTSC Xbox version
            readSoundNames(datFile, isWindowMacVersion);                                                                    // Sound names
            readSoundNames(datFile, isXboxVersion);                                                                         // Sound names Xbox

            roomScriptsStartOffset = (int)datFile.BaseStream.Position;

            MemoryStream initScriptStream = getRoomScriptStream("INIT", ScriptType.kScriptTypeNodeInit);
            using (BinaryReader br = new BinaryReader(initScriptStream))
                nodeInitScript = ScriptData.readOpcodes(br);

            MemoryStream cuesStream = getRoomScriptStream("INIT", ScriptType.kScriptTypeAmbientCue);
            loadAmbientCues(cuesStream);
            cuesStream = null;

            preloadCommonRoomsAndInitZipBitIndex();

            if (isWindowMacVersion && localizationType == GameLocalizationType.kLocMulti2)
                patchLanguageMenu();
        }

        ~Database()
        {
            if (datFile != null)
            {
                ((IDisposable)datFile).Dispose();
                datFile.Close();
            }
        }

        /// Loads a room's nodes into the database cache
        public void cacheRoom(uint roomID, uint ageID)
        {
            if (roomNodesCache.ContainsKey(new RoomKey((ushort)roomID, (ushort)ageID)))
                return;

            // Remove old rooms from cache and add the new one
            roomNodesCache.Clear();

            RoomData currentRoomData = findRoomData(roomID, ageID);
            roomNodesCache[new RoomKey((ushort)roomID, (ushort)ageID)] = readRoomScripts(currentRoomData);
        }

        /// Tells if a room is a common room
        /// Common rooms are always in the cache
        public bool isCommonRoom(uint roomID, uint ageID)
        { return roomID == 101 || roomID == 901 || roomID == 902; }

        /// Returns the name of the currently loaded room
        public string getRoomName(uint roomID, uint ageID)
        {
            RoomData data = findRoomData(roomID, ageID);
            return data.name;
        }

        void readScriptIndex(BinaryReader stream, bool load)
        {
            uint count = stream.ReadUInt32();
            for (uint i = 0; i < count; i++)
            {
                RoomScripts roomScripts;

                char[] roomName = stream.ReadChars(5);

                roomScripts.room = new string(roomName, 0, 4);
                roomScripts.type = (ScriptType) stream.ReadUInt32();
                roomScripts.offset = stream.ReadUInt32();
                roomScripts.size = stream.ReadUInt32();

                if (load)
                    roomScriptsIndex.Add(roomScripts);
            }
        }

        void readSoundNames(BinaryReader stream, bool load)
        {
            uint count = stream.ReadUInt32();
            for (uint i = 0; i < count; i++)
            {
                uint id = stream.ReadUInt32();

                char[] soundName = stream.ReadChars(32);

                if (load)
                    soundNames[id] = new string(soundName, 0, 31);
            }
        }

        MemoryStream getRoomScriptStream(string room, ScriptType scriptType)
        {
            for (uint i = 0; i < roomScriptsIndex.Count; i++)
            {
                if (roomScriptsIndex[(int)i].room.ToLower() == room.ToLower() && roomScriptsIndex[(int)i].type == scriptType)
                {
                    uint startOffset = (uint)(roomScriptsStartOffset + roomScriptsIndex[(int)i].offset);
                    uint size = roomScriptsIndex[(int)i].size;

                    MemoryStream ms = new MemoryStream();
                    long pos = datFile.BaseStream.Position;
                    datFile.BaseStream.Position = startOffset;
                    byte[] bytes = datFile.ReadBytes((int)size);
                    if (bytes.Length != size)
                        throw new Exception("Invalid byte size !");
                    ms.Write(bytes, 0, (int)size);
                    datFile.BaseStream.Position = pos;
                    ms.Position = 0;
                    return ms;
                }
            }

            return null;
        }

        void loadAmbientCues(MemoryStream s)
        {
            using (BinaryReader br = new BinaryReader(s))
            {
                ambientCues.Clear();

                while (s.Position < s.Length)
                {
                    ushort id = br.ReadUInt16();

                    if (id == 0)
                        break;

                    AmbientCue cue;
                    cue.tracks = new List<ushort>();
                    cue.id = id;
                    cue.minFrames = br.ReadUInt16();
                    cue.maxFrames = br.ReadUInt16();

                    while (true)
                    {
                        ushort track = br.ReadUInt16();

                        if (track == 0)
                            break;

                        cue.tracks.Add(track);
                    }

                    ambientCues[id] = cue;
                }
            }
        }

        List<NodeData> readRoomScripts(RoomData room)
        {
            List<NodeData> nodes = new List<NodeData>();

            // Load the node scripts
            MemoryStream scriptsStream = getRoomScriptStream(room.name, ScriptType.kScriptTypeNode);
            if (scriptsStream != null)
                walkNodes(scriptsStream, nodes, nodeTransformAddHotspots);

            // Load the ambient sound scripts, if any
            MemoryStream ambientSoundsStream = getRoomScriptStream(room.name, ScriptType.kScriptTypeAmbientSound);
            if (ambientSoundsStream != null)
                walkNodes(ambientSoundsStream, nodes, nodeTransformAddSoundScripts);

            MemoryStream backgroundSoundsStream = getRoomScriptStream(room.name, ScriptType.kScriptTypeBackgroundSound);
            if (backgroundSoundsStream != null)
                walkNodes(backgroundSoundsStream, nodes, nodeTransformAddBackgroundSoundScripts);

            return nodes;
        }

        void nodeTransformAddHotspots(BinaryReader br, List<NodeData> nodes, int zipBitIndex)
        {
            List<CondScript> scripts = ScriptData.readCondScripts(br);
            List<HotSpot> hotspots = ScriptData.readHotspots(br);
            foreach (NodeData node in nodes)
            {
                node.zipBitIndex = (short)zipBitIndex;
                node.scripts.AddRange(scripts);
                node.hotspots.AddRange(hotspots);
            }
        }

        void nodeTransformAddSoundScripts(BinaryReader br, List<NodeData> nodes, int zipBitIndex)
        {
            List<CondScript> l = ScriptData.readCondScripts(br);
            foreach (NodeData node in nodes)
                node.soundScripts.AddRange(l);
        }

        void nodeTransformAddBackgroundSoundScripts(BinaryReader br, List<NodeData> nodes, int zipBitIndex)
        {
            List<CondScript> l = ScriptData.readCondScripts(br);
            foreach (NodeData node in nodes)
                node.backgroundSoundScripts.AddRange(l);
        }

        void walkNodes(MemoryStream file, List<NodeData> allNodes, Action<BinaryReader, List<NodeData>, int> transform)
        {
            int zipBitIndex = -1;
            using (BinaryReader br = new BinaryReader(file))
            {
                while (file.Position < file.Length)
                {
                    short id = br.ReadInt16();

                    // End of list
                    if (id == 0)
                        break;

                    if (id < -10)
                        throw new Exception("Unimplemented node list command");

                    if (id > 0)
                    {
                        // Normal node, find the node if existing
                        NodeData node = null;
                        for (int i = 0; i < allNodes.Count; i++)
                            if (allNodes[i].id == id) {
                                node = allNodes[i];
                                break;
                            }

                        // Node not found, create a new one
                        if (node == null)
                        {
                            node = new NodeData();
                            node.id = id;
                            allNodes.Add(node);
                        }

                        List<NodeData> nodeBuf = new List<NodeData>(); // transform action expects a list
                        nodeBuf.Add(node);
                        zipBitIndex++;
                        transform(br, nodeBuf, zipBitIndex);
                    } else {
                        // Several nodes sharing the same scripts
                        // Find the node ids the script applies to
                        List<short> scriptNodeIds = new List<short>();

                        if (id == -10)
                            do {
                                id = br.ReadInt16();
                                if (id < 0) {
                                    ushort end = br.ReadUInt16();
                                    for (int i = -id; i < end; i++)
                                        scriptNodeIds.Add((short)i);

                                } else if (id > 0) {
                                    scriptNodeIds.Add(id);
                                }
                            } while (id != 0);
                        else
                            for (int i = 0; i < -id; i++) {
                                scriptNodeIds.Add(br.ReadInt16());
                            }

                        List<NodeData> nodeBuf = new List<NodeData>(); // transform action expects a list
                        for (int i = 0; i < scriptNodeIds.Count; i++) {
                            NodeData node = null;

                            // Find the current node if existing
                            for (int j = 0; j < allNodes.Count; j++) {
                                if (allNodes[j].id == scriptNodeIds[i]) {
                                    node = allNodes[j];
                                    break;
                                }
                            }

                            // Node not found, skip it
                            if (node == null)
                                continue;

                            nodeBuf.Add(node);
                        }
                        // Add the script to each matching node
                        zipBitIndex++;
                        transform(br, nodeBuf, zipBitIndex);
                    }
                }
            }
        }

        void preloadCommonRoomsAndInitZipBitIndex()
        {
            short zipBit = 0;
            for (int i = 0; i < ages.Length; i++)
            {
                AgeData age = ages[i];
                for (int j = 0; j < age.roomCount; j++)
                {
                    roomZipBitIndex[age.rooms[j].id] = zipBit;

                    RoomData room = age.rooms[j];

                    // Debug.Log(age.id + " " + age.labelId + " " + room.name);
                    List<NodeData> nodes = readRoomScripts(room);
                    if (isCommonRoom(room.id, age.id))
                        roomNodesCache[new RoomKey((ushort)room.id, (ushort)age.id)] = nodes;

                    short maxZipBitForRoom = 0;
                    for (int k = 0; k < nodes.Count; k++) {
                        if (maxZipBitForRoom < nodes[k].zipBitIndex)
                            maxZipBitForRoom = nodes[k].zipBitIndex;
                    }

                    zipBit += (short)(maxZipBitForRoom + 1);
                }
            }
        }

        void patchLanguageMenu()
        {
            // The menu scripts in 'myst3.dat" for the non English CD versions come from the French version
            // The scripts for the other languages only differ by the value set for AudioLanguage variable
            // when the language selection is not English.
            // This function patches the language selection script to set the appropriate value based
            // on the detected game langage.

            // Script disassembly:
            //    hotspot 5 > c[v1 != 0] (true)
            //    rect > pitch: 373 heading: 114 width: 209 height: 28
            //    op 206, soundPlayVolume ( 795 5 )
            //    op 53, varSetValue ( vLanguageAudio 2 ) // <= The second argument of this opcode is patched
            //    op 194, runPuzzle1 ( 18 )
            //    op 194, runPuzzle1 ( 19 )

            NodeData languageMenu = getNodeData(530, 901, 9);
            languageMenu.hotspots[5].script[1].args[1] = (short)getGameLanguageCode();
        }

        public NodeData getNodeData(ushort nodeID, uint roomID, uint ageID)
        {
            List<NodeData> nodes = getRoomNodes(roomID, ageID);

            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].id == nodeID)
                    return nodes[i];

            return new NodeData();
        }

        public List<NodeData> getRoomNodes(uint roomID, uint ageID)
        {
            List<NodeData> nodes;

            if (roomNodesCache.ContainsKey(new RoomKey((ushort)roomID, (ushort)ageID)))
                nodes = roomNodesCache[new RoomKey((ushort)roomID, (ushort)ageID)];
            else {
                RoomData data = findRoomData(roomID, ageID);
                nodes = readRoomScripts(data);
            }

            return nodes;
        }

        RoomData findRoomData(uint roomID, uint ageID)
        {
            for (int i = 0; i < ages.Length; i++)
                if (ages[i].id == ageID)
                    for (int j = 0; j < ages[i].roomCount; j++)
                        if (ages[i].rooms[j].id == roomID)
                            return ages[i].rooms[j];

            throw new Exception("No room with ID " + roomID);
        }

        public MystLanguage getGameLanguageCode()
        {
            // The monolingual versions of the game always use 0 as the language code
            if (localizationType == GameLocalizationType.kLocMonolingual)
                return MystLanguage.kEnglish;

            switch (language)
            {
                case Language.FR_FRA:
                    return MystLanguage.kFrench;
                case Language.DE_DEU:
                    return MystLanguage.kGerman;
                case Language.IT_ITA:
                    return MystLanguage.kItalian;
                case Language.ES_ESP:
                    return MystLanguage.kSpanish;
                case Language.EN_ANY:
                    return MystLanguage.kEnglish;
                default:
                    return MystLanguage.kOther;
            }
        }

        public int getNodeZipBitIndex(ushort nodeID, uint roomID, uint ageID)
        {
            if (!roomZipBitIndex.ContainsKey(roomID))
                throw new Exception("Unable to find zip-bit index for room " + roomID);

            List<NodeData> nodes = getRoomNodes(roomID, ageID);

            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].id == nodeID)
                    return roomZipBitIndex[roomID] + nodes[i].zipBitIndex;

            throw new Exception($"Unable to find zip-bit index for node ({nodeID}, {roomID}). Available are: {string.Join(", ", nodes.Select(n => n.id).ToList())}");
        }

        public void TestContent()
        {
            string msg = "";
            for (int i = 0; i < ages.Length; i++)
            {
                msg += $"Age: {ages[i].id}\n";
                for (int j = 0; j < ages[i].roomCount; j++)
                {
                    RoomData room = ages[i].rooms[j];
                    msg += $"    Room: {room.id}\n";

                    List<NodeData> nodes = new List<NodeData>();
                    MemoryStream scriptsStream = getRoomScriptStream(room.name, ScriptType.kScriptTypeNode);
                    if (scriptsStream != null)
                    {
                        try
                        {
                            walkNodes(scriptsStream, nodes, (_, __, ___) => { });
                        } catch (Exception e)
                        {
                            Debug.LogWarning(e.Message);
                        }
                    }
                    foreach (NodeData node in nodes)
                        msg += $"        Node: {node.id}\n";
                }
            }
            Debug.Log(msg);
        }
    }
}

