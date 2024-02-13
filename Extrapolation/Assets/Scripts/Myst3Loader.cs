using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Myst3;
using UnityEngine;

/// <summary>
/// Mock Myst 3 engine (based on converted Residual/ScummVM code). Handles loading cube nodes.
/// </summary>
public class Myst3Loader : IMyst3Engine
{
    public IEnumerable<M3Cube> Cubes => _cubes;

    readonly Myst3GameDescription _gameDescription;
    readonly Database _db;
    readonly ConfigurationManager _configManager;
    readonly GameState _state;
    readonly List<Archive> _archivesCommon = new();
    readonly List<M3Cube> _cubes = new();
    readonly Archive _archiveNode = new();
    readonly Myst3.Cursor _cursor = new();
    readonly Script _scriptEngine;

    public class M3Cube
    {
        public MemoryStream[] faces;
        public uint ageID;
        public uint roomID;
        public ushort nodeID;
        public string roomName;
    }
    
    public Myst3Loader(Myst3GameDescription gameDescription, string path)
    {
        _gameDescription = gameDescription;
        _configManager = new();

        FileManager.Initialize(path, Application.streamingAssetsPath);

        if (!FileManager.hasFile("OVER101.m3o"))
            throw new System.IO.FileNotFoundException("Couldn't find file OVER101.m3o ! Ensure the game has the latest official patch !");

        _scriptEngine = new Script(this);
        _db = new Database(getPlatform(), getGameLanguage(), getGameLocalizationType());
        _state = new(getPlatform(), _db);
        SettingsInitDefaults();
        OpenArchives();
    }

    void SettingsInitDefaults()
    {
        int defaultLanguage = (int)_db.getGameLanguageCode();

        int defaultTextLanguage;
        if (getGameLocalizationType() == GameLocalizationType.kLocMulti6)
            defaultTextLanguage = defaultLanguage;
        else
            defaultTextLanguage = (getGameLanguage() != Language.EN_ANY) ? 1 : 0;

        _configManager.registerDefault("overall_volume", 256);
        _configManager.registerDefault("music_volume", 256 / 2);
        _configManager.registerDefault("music_frequency", 75);
        _configManager.registerDefault("audio_language", defaultLanguage);
        _configManager.registerDefault("text_language", defaultTextLanguage);
        _configManager.registerDefault("water_effects", true);
        _configManager.registerDefault("transition_speed", 50);
        _configManager.registerDefault("mouse_speed", 50);
        _configManager.registerDefault("zip_mode", false);
        _configManager.registerDefault("subtitles", false);
        _configManager.registerDefault("vibrations", true); // Xbox specific
    }

    void OpenArchives()
    {
            // The language of the menus is always the same as the executable
            // The English CD version can only display English text
            // The non English CD versions can display their localized language and English
            // The DVD version can display 6 different languages

            string menuLanguage;
            string textLanguage;

            switch (getGameLanguage()) {
                case Language.NL_NLD:
                    menuLanguage = "DUTCH";
                    break;
                case Language.FR_FRA:
                    menuLanguage = "FRENCH";
                    break;
                case Language.DE_DEU:
                    menuLanguage = "GERMAN";
                    break;
                case Language.IT_ITA:
                    menuLanguage = "ITALIAN";
                    break;
                case Language.ES_ESP:
                    menuLanguage = "SPANISH";
                    break;
                case Language.JA_JPN:
                    menuLanguage = "JAPANESE";
                    break;
                case Language.PL_POL:
                    menuLanguage = "POLISH";
                    break;
                case Language.EN_ANY:
                case Language.RU_RUS:
                default:
                    menuLanguage = "ENGLISH";
                    break;
            }

            if (getGameLocalizationType() == GameLocalizationType.kLocMulti6) {
                switch ((MystLanguage)_configManager.getInt("text_language")) {
                    case MystLanguage.kDutch:
                        textLanguage = "DUTCH";
                        break;
                    case MystLanguage.kFrench:
                        textLanguage = "FRENCH";
                        break;
                    case MystLanguage.kGerman:
                        textLanguage = "GERMAN";
                        break;
                    case MystLanguage.kItalian:
                        textLanguage = "ITALIAN";
                        break;
                    case MystLanguage.kSpanish:
                        textLanguage = "SPANISH";
                        break;
                    case MystLanguage.kEnglish:
                    default:
                        textLanguage = "ENGLISH";
                        break;
                }
            } else {
                if (getGameLocalizationType() == GameLocalizationType.kLocMonolingual || _configManager.getInt("text_language") != 0) {
                    textLanguage = menuLanguage;
                } else {
                    textLanguage = "ENGLISH";
                }
            }

            if (getGameLocalizationType() != GameLocalizationType.kLocMonolingual && getPlatform() != Platform.kPlatformXbox && textLanguage == "ENGLISH") {
                textLanguage = "ENGLISHjp";
            }

            if (getPlatform() == Platform.kPlatformXbox) {
                menuLanguage += "X";
                textLanguage += "X";
            }

            // Load all the override files in the search path
            string[] overrides = FileManager.listMatchingMembers("*.m3o");
            foreach (string archiveName in overrides)
                AddArchive(archiveName, false);

            AddArchive(textLanguage + ".m3t", true);

            if (getGameLocalizationType() != GameLocalizationType.kLocMonolingual || getPlatform() == Platform.kPlatformXbox) {
                AddArchive(menuLanguage + ".m3u", true);
            }

            AddArchive("RSRC.m3r", true);
    }

    bool AddArchive(string file, bool mandatory)
    {
        Archive archive = new();
        bool opened = archive.open(file, null);

        if (opened)
            _archivesCommon.Add(archive);
        else if (mandatory)
            throw new System.IO.FileLoadException("Unable to open archive " + file);

        return opened;
    }

    public Platform getPlatform()
    { return _gameDescription.platform; }

    public Language getGameLanguage()
    { return _gameDescription.language; }

    public GameLocalizationType getGameLocalizationType()
    { return _gameDescription.localizationType; }
    
    public void loadNodeCubeFaces(ushort nodeID)
    {
        state.setViewType(ViewType.kCube);
        MemoryStream[] faces = Enumerable
            .Range(0, 6)
            .Select(i => GetFileDescription("", nodeID, (ushort)(i + 1), DirectorySubEntry.ResourceType.kCubeFace).getData())
            .ToArray();
        ushort node = (ushort)state.getVar("LocationNode");
        uint room = (uint)state.getVar("LocationRoom");
        uint age = (uint)state.getVar("LocationAge");
        NodeData currentNodeData = _db.getNodeData(node, room, age);
        if (_cubes.Any(c => c.ageID == age && c.roomID == room && c.nodeID == node))
            return;
        M3Cube cube = new()
        {
            faces = faces,
            ageID = age,
            roomID = room,
            nodeID = node,
            roomName = _db.getRoomName(room, age)
        };
        _cubes.Add(cube);
        return;
    }
    public void loadMovie(ushort id, ushort condition, bool resetCond, bool loop) { Debug.LogWarning("Not implemented: loadMovie"); }
    public void removeMovie(ushort id) { Debug.LogWarning("Not implemented: removeMovie"); }
    public void setMovieLooping(ushort id, bool loop) { Debug.LogWarning("Not implemented: setMovieLooping"); }
    public void addSunSpot(ushort pitch, ushort heading, ushort intensity, ushort color, ushort var, bool varControlledIntensity, ushort radius) { Debug.LogWarning("Not implemented: addSunSpot"); }
    public void goToNode(ushort nodeID, TransitionType transitionType) { Debug.LogWarning("Not implemented: goToNode"); }
    public void playMovieGoToNode(ushort movie, ushort node) { Debug.LogWarning("Not implemented: playMovieGoToNode"); }
    
    public void runScriptsFromNode(ushort nodeID, uint roomID, uint ageID)
    {
        if (roomID == 0)
            roomID = (uint)state.getVar("LocationRoom");

        if (ageID == 0)
            ageID = (uint)state.getVar("LocationAge");

        NodeData nodeData = _db.getNodeData(nodeID, roomID, ageID);

        for (int j = 0; j < nodeData.scripts.Count; j++)
            if (state.evaluate((short)nodeData.scripts[j].condition))
                if (!_scriptEngine.run(nodeData.scripts[j].script))
                    break;
    }

    public GameState state => _state;
    public Myst3.Cursor cursor => _cursor;

    /// <summary>
    /// Load all the cube nodes (and textures) from the given Age.
    /// </summary>
    /// <param name="ageID">ID of the Age to load.</param>
    /// <param name="roomID">ID of the room to load, or zero to load all.</param>
    public void LoadAge(uint ageID, uint roomID)
    {
        AgeData age = Database.ages.First(a => a.id == ageID);
        foreach (RoomData room in age.rooms)
        {
            uint curRoomID = room.id;
            if (roomID != 0 && roomID != curRoomID)
                continue;
            List<NodeData> nodes = _db.getRoomNodes(curRoomID, ageID);
            foreach (NodeData nodeData in nodes)
            {
                short nodeID = nodeData.id;
                _scriptEngine.run(_db.nodeInitScript);
                if (nodeID != 0)
                    state.setVar("LocationNode", state.valueOrVarValue(nodeID));

                if (curRoomID != 0)
                    state.setVar("LocationRoom", state.valueOrVarValue(curRoomID));
                else
                    curRoomID = (uint)state.getVar("LocationRoom");

                if (ageID != 0)
                    state.setVar("LocationAge", state.valueOrVarValue(ageID));
                else
                    ageID = (uint)state.getVar("LocationAge");

                _db.cacheRoom(curRoomID, ageID);

                string newRoomName = _db.getRoomName(curRoomID, ageID);
                if (_archiveNode.roomName != newRoomName && !_db.isCommonRoom(curRoomID, ageID))
                {
                    string nodeFile = string.Format("{0}nodes.m3a", newRoomName);

                    _archiveNode.close();
                    if (!_archiveNode.open(nodeFile, newRoomName))
                        throw new Exception("Unable to open archive " + nodeFile);
                }

                runNodeInitScripts();

                // These effects can only be created after running the node init scripts
                // shakeEffect = new ShakeEffect(this);
                // rotationEffect = new RotationEffect(this);

                // WORKAROUND: In Narayan, the scripts in node NACH 9 test on var 39
                // without first reinitializing it leading to Saavedro not always giving
                // Releeshan to the player when he is trapped between both shields.
                if (nodeID == 9 && curRoomID == 801)
                    state.setVar(39, 0);
            }
        }
        
        Debug.Log($"Loaded Age with id {ageID}, {_cubes.Count} cubes from {age.rooms.Length} rooms.");
    }

    void runNodeInitScripts()
    {
        NodeData nodeData = _db.getNodeData(
                (ushort)state.getVar("LocationNode"),
                (uint)state.getVar("LocationRoom"),
                (uint)state.getVar("LocationAge"));

        NodeData nodeDataInit = _db.getNodeData(
                (ushort)32765,
                (uint)state.getVar("LocationRoom"),
                (uint)state.getVar("LocationAge"));

        if (nodeDataInit != null)
            runScriptsFromNode(32765, 0, 0);

        if (nodeData == null)
            throw new Exception("Node " + state.getVar("LocationNode") + " unknown in the database");

        for (int j = 0; j < nodeData.scripts.Count; j++)
            if (state.evaluate((short)nodeData.scripts[j].condition))
                _scriptEngine.run(nodeData.scripts[j].script);

        // Mark the node as a reachable zip destination
        state.markNodeAsVisited(
                (ushort)state.getVar("LocationNode"),
                (ushort)state.getVar("LocationRoom"),
                (uint)state.getVar("LocationAge"));
    }
    
    DirectorySubEntry GetFileDescription(string room, uint index, ushort face, DirectorySubEntry.ResourceType type)
    {
        string archiveRoom = room;
        if (archiveRoom == "")
            archiveRoom = _db.getRoomName((uint)state.getVar("LocationRoom"), (uint)state.getVar("LocationAge"));

        DirectorySubEntry desc = null;

        // Search common archives
        int i = 0;
        while (desc == null && i < _archivesCommon.Count)
        {
            desc = _archivesCommon[i].getDescription(archiveRoom, index, face, type);
            i++;
        }

        // Search currently loaded node archive
        if (desc == null && _archiveNode != null)
            desc = _archiveNode.getDescription(archiveRoom, index, face, type);

        return desc;
    }

    public void Shutdown()
    {
        foreach (Archive archive in _archivesCommon)
            archive.close();
        _archiveNode.close();
    }
}
