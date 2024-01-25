using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

#pragma warning disable 414, 219

namespace Myst3
{
    public enum Platform {
        kPlatformDOS,
        kPlatformAmiga,
        kPlatformAtariST,
        kPlatformMacintosh,
        kPlatformFMTowns,
        kPlatformWindows,
        kPlatformNES,
        kPlatformC64,
        kPlatformCoCo3,
        kPlatformLinux,
        kPlatformAcorn,
        kPlatformSegaCD,
        kPlatform3DO,
        kPlatformPCEngine,
        kPlatformApple2GS,
        kPlatformPC98,
        kPlatformWii,
        kPlatformPSX,
        //ResidualVM: playstation2, xbox
        kPlatformPS2,
        kPlatformXbox,
        kPlatformCDi,
        kPlatformIOS,
        kPlatformOS2,
        kPlatformBeOS,

        kPlatformUnknown = -1
    };

    public enum Language {
        ZH_CNA,
        ZH_TWN,
        HR_HRV,
        CZ_CZE,
        NL_NLD,
        EN_ANY,     // Generic English (when only one game version exist)
        EN_GRB,
        EN_USA,
        FR_FRA,
        DE_DEU,
        GR_GRE,
        HE_ISR,
        HU_HUN,
        IT_ITA,
        JA_JPN,
        KO_KOR,
        LV_LAT,
        NB_NOR,
        PL_POL,
        PT_BRA,
        RU_RUS,
        ES_ESP,
        SE_SWE,

        UNK_LANG = -1    // Use default language (i.e. none specified)
    };

    public enum TransitionType {
        kTransitionFade = 1,
        kTransitionNone,
        kTransitionZip,
        kTransitionLeftToRight,
        kTransitionRightToLeft
    };

    [Serializable]
    public class Myst3GameDescription : ADGameDescription {
        public GameLocalizationType localizationType;
    };

    public class Myst3 : MonoBehaviour, IMyst3Engine
    {
        public MeshRenderer environmentRenderer;
        public Transform sunshaftsCasterParent;

        public Myst3GameDescription gameDescription;

        [Serializable]
        public class hackAgeStartPoint {
            public string name;
            public int node=1;
            public int room=1;
            public int age=1;
        }
        public hackAgeStartPoint[] startPoints;
        int curStartPoint=0;
        [Tooltip("Location of M3. Leave empty to use persistentDataPath")]
        public string gameLocation;

        [Header("Image processing")]
        public bool imgEditorOnly = true;
        public bool imgDumpTextures = true;
        public bool imgUpscale = true;
        [Range(1, 4)]
        public float imgUpscaleFactor = 2;
        public bool imgNoiseReduction = true;
        [Range(0, 3)]
        public int imgNoiseReductionAggressivity = 0;
        public string imgWaifuExecutable;
        public string imgDumpFolder;

        [Header("Debug: location")]
        public ushort curNodeID;
        public uint curRoomID;
        public uint curAgeID;

        ConfigurationManager configManager;
        Script scriptEngine;
        List<ScriptedMovie> movies;
        List<SunSpot> sunspots;
        public GameState state { get; private set; }
        public Cursor cursor { get; private set; }
        Database db;
        Archive archiveNode;
        Node node;
        NodeData currentNodeData;
        List<Archive> archivesCommon;

        // Used by Amateria's magnetic rings
        ShakeEffect shakeEffect;
        // Used by Voltaic's spinning gears
        RotationEffect rotationEffect;

        // Used for setting textures and doing transition effects
        Material matNorth;
        Material matSouth;
        Material matEast;
        Material matWest;
        Material matZenith;
        Material matNadir;
        float transitionDuration = .3f;
        float transitionProgress = 0;
        bool transitionState = false;
        Texture2D[] nodeTexturesA;
        Texture2D[] nodeTexturesB;
        Material[] cubeMats;

        public bool debugTestContent = false;
        public float fovDebug;

        void Start()
        {
            StartCoroutine(WaitForPermissions());
        }

        IEnumerator WaitForPermissions()
        {
            // Android must be the shittiest platform ever.
            // (alright, there must be worse even amongst the other major platform, but still, that
            // permission system is FUBAR crap.)
            // AND THIS STILL DOES NOT WORK. Fuck you in the ass Google, your permission system is asinine and stupid !
            // You KNOW you're only screwing up independant devs, right ? This provides zero security ! All it does is give
            // an incentive for big companies like you to random control over the user's phone ! Well done, fuckfaces.
            // But you don't care as long as your paycheck falls at the end of each month, RIGHT ?
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);

            while (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead) || !Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
            {
                Debug.Log("Waiting for permissions...");
                yield return new WaitForSeconds(1);
            }
            #else
                yield return null; // coroutines are crap.
            #endif

            run();
        }

        void run()
        {
            int matIndex = 0;
            matWest = environmentRenderer.materials[matIndex++];
            matNorth = environmentRenderer.materials[matIndex++];
            matEast = environmentRenderer.materials[matIndex++];
            matSouth = environmentRenderer.materials[matIndex++];
            matNadir = environmentRenderer.materials[matIndex++];
            matZenith = environmentRenderer.materials[matIndex++];

            configManager = new ConfigurationManager();

            // TODO - replace with user-defined path
            // FileManager.Initialize(Application.streamingAssetsPath);
            #if UNITY_ANDROID && !UNITY_EDITOR
                // FileManager.Initialize("/storage/emulated/0/m3"); // Android should die in a fire.
                string androidPath = Application.persistentDataPath;
                Debug.Log($"Shitty Android path: {androidPath}");
                FileManager.Initialize(androidPath); // figure it out. Android says "fuck" to clean folder architecture in favor of fake security...
            #else
            if (!string.IsNullOrEmpty(gameLocation))
                FileManager.Initialize(gameLocation, Path.Combine(Application.streamingAssetsPath, "myst3.dat"));
            else
                FileManager.Initialize(Path.Combine(Application.persistentDataPath, "m3exile"), Path.Combine(Application.streamingAssetsPath, "myst3.dat"));
            #endif

            if (!FileManager.hasFile("OVER101.m3o"))
            {
                Debug.LogError("Couldn't find file OVER101.m3o ! Ensure the game has the latest official patch !");
                enabled = false;
                return;
            }

            node = null;
            currentNodeData = null;
            archivesCommon = new List<Archive>();
            movies = new List<ScriptedMovie>();
            sunspots = new List<SunSpot>();
            cubeMats = new Material[] {
                matWest,
                matNadir,
                matEast,
                matNorth,
                matSouth,
                matZenith,
            };
            nodeTexturesA = new Texture2D[6];
            for (int i=0; i<6; i++)
                cubeMats[i].mainTexture = nodeTexturesA[i] = new Texture2D(4, 4);
            nodeTexturesB = new Texture2D[6];
            for (int i=0; i<6; i++)
                cubeMats[i].SetTexture("_Tex2", nodeTexturesB[i] = new Texture2D(4, 4));

            scriptEngine = new Script(this);
            db = new Database(getPlatform(), getGameLanguage(), getGameLocalizationType());
            state = new GameState(getPlatform(), db);
            cursor = new Cursor();
            archiveNode = new Archive();

            settingsInitDefaults();
            // syncSoundSettings();

            openArchives();

            // Game init script, loads the menu
            // loadNode(1, 101, 1);

            // FIXME HAXX
            // loadNode(25, 701, 7); // load node from Voltaic
            loadNode(1, 1001, 10); // load node from Amateria
            // loadNode(5, 801, 8); // load node from Narayan
            // loadNode(1, 601, 6); // load node from Edanna
            // loadNode(1, 501, 5); // load node from J'nanin
        }

        void OnDestroy()
        {
            foreach (Texture2D tex in nodeTexturesA)
                Destroy(tex);
            foreach (Texture2D tex in nodeTexturesB)
                Destroy(tex);

            unloadNode();

            archiveNode.close();
            // gfx.freeFont();
        }

        void settingsInitDefaults()
        {
            int defaultLanguage = (int)db.getGameLanguageCode();

            int defaultTextLanguage;
            if (getGameLocalizationType() == GameLocalizationType.kLocMulti6)
                defaultTextLanguage = defaultLanguage;
            else
                defaultTextLanguage = (getGameLanguage() != Language.EN_ANY) ? 1 : 0;

            configManager.registerDefault("overall_volume", 256);
            configManager.registerDefault("music_volume", 256 / 2);
            configManager.registerDefault("music_frequency", 75);
            configManager.registerDefault("audio_language", defaultLanguage);
            configManager.registerDefault("text_language", defaultTextLanguage);
            configManager.registerDefault("water_effects", true);
            configManager.registerDefault("transition_speed", 50);
            configManager.registerDefault("mouse_speed", 50);
            configManager.registerDefault("zip_mode", false);
            configManager.registerDefault("subtitles", false);
            configManager.registerDefault("vibrations", true); // Xbox specific
        }

        void openArchives()
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
                switch ((MystLanguage)configManager.getInt("text_language")) {
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
                if (getGameLocalizationType() == GameLocalizationType.kLocMonolingual || configManager.getInt("text_language") != 0) {
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
                addArchive(archiveName, false);

            addArchive(textLanguage + ".m3t", true);

            if (getGameLocalizationType() != GameLocalizationType.kLocMonolingual || getPlatform() == Platform.kPlatformXbox) {
                addArchive(menuLanguage + ".m3u", true);
            }

            addArchive("RSRC.m3r", true);
        }

        bool addArchive(string file, bool mandatory)
        {
            Archive archive = new Archive();
            bool opened = archive.open(file, null);

            if (opened)
                archivesCommon.Add(archive);
            else if (mandatory)
                throw new Exception("Unable to open archive " + file);

            return opened;
        }

        void loadNode(ushort nodeID, uint roomID, uint ageID)
        {
            unloadNode();

            scriptEngine.run(db.nodeInitScript);
            if (nodeID != 0)
                state.setVar("LocationNode", state.valueOrVarValue(nodeID));

            if (roomID != 0)
                state.setVar("LocationRoom", state.valueOrVarValue(roomID));
            else
                roomID = (uint)state.getVar("LocationRoom");

            if (ageID != 0)
                state.setVar("LocationAge", state.valueOrVarValue(ageID));
            else
                ageID = (uint)state.getVar("LocationAge");

            curNodeID = nodeID;
            curRoomID = roomID;
            curAgeID = ageID;

            db.cacheRoom(roomID, ageID);

            string newRoomName = db.getRoomName(roomID, ageID);
            if (archiveNode.roomName != newRoomName && !db.isCommonRoom(roomID, ageID))
            {
                string nodeFile = string.Format("{0}nodes.m3a", newRoomName);

                archiveNode.close();
                if (!archiveNode.open(nodeFile, newRoomName))
                    throw new Exception("Unable to open archive " + nodeFile);
            }

            runNodeInitScripts();

            // These effects can only be created after running the node init scripts
            shakeEffect = new ShakeEffect(this);
            rotationEffect = new RotationEffect(this);

            // WORKAROUND: In Narayan, the scripts in node NACH 9 test on var 39
            // without first reinitializing it leading to Saavedro not always giving
            // Releeshan to the player when he is trapped between both shields.
            if (nodeID == 9 && roomID == 801)
                state.setVar(39, 0);

            print($"LoadNode: {nodeID}, {roomID}, {ageID}");
        }

        void unloadNode()
        {
            if (node == null)
                return;

            // Delete all movies
            removeMovie(0);

            // Remove all sunspots
            sunspots.Clear();
            // sunshafts.enabled = false;

            // Clean up the effects
            node = null;

            shakeEffect = null;
            rotationEffect = null;
            state.setVar("ShakeEffectAmpl", 0);
        }

        void runNodeInitScripts()
        {
            NodeData nodeData = db.getNodeData(
                    (ushort)state.getVar("LocationNode"),
                    (uint)state.getVar("LocationRoom"),
                    (uint)state.getVar("LocationAge"));

            NodeData nodeDataInit = db.getNodeData(
                    (ushort)32765,
                    (uint)state.getVar("LocationRoom"),
                    (uint)state.getVar("LocationAge"));

            if (nodeDataInit != null)
                runScriptsFromNode(32765, 0, 0);

            if (nodeData == null)
                throw new Exception("Node " + state.getVar("LocationNode") + " unknown in the database");

            for (int j = 0; j < nodeData.scripts.Count; j++)
                if (state.evaluate((short)nodeData.scripts[j].condition))
                    scriptEngine.run(nodeData.scripts[j].script);

            // Mark the node as a reachable zip destination
            state.markNodeAsVisited(
                    (ushort)state.getVar("LocationNode"),
                    (ushort)state.getVar("LocationRoom"),
                    (uint)state.getVar("LocationAge"));
        }

        void extractAllNodes()
        {
            foreach (AgeData age in Database.ages)
            {
                foreach (RoomData room in age.rooms)
                {
                    int i = 0;
                    while (i < 3000)
                    {
                        //try
                        //{
                            loadNode((ushort)i++, room.id, age.id);
                        //}
                        //catch (Exception)
                        //{
                        //    //Debug.LogWarning(e.Message);
                        //    //continue;
                        //}
                    }
                }
            }
        }

        public Platform getPlatform()
        { return gameDescription.platform; }

        public Language getGameLanguage()
        { return gameDescription.language; }

        public GameLocalizationType getGameLocalizationType()
        { return gameDescription.localizationType; }

        public void runScriptsFromNode(ushort nodeID, uint roomID, uint ageID)
        {
            if (roomID == 0)
                roomID = (uint)state.getVar("LocationRoom");

            if (ageID == 0)
                ageID = (uint)state.getVar("LocationAge");

            NodeData nodeData = db.getNodeData(nodeID, roomID, ageID);

            for (int j = 0; j < nodeData.scripts.Count; j++)
                if (state.evaluate((short)nodeData.scripts[j].condition))
                    if (!scriptEngine.run(nodeData.scripts[j].script))
                        break;
        }

        public void removeMovie(ushort id)
        {
            if (id == 0)
                movies.Clear();
            else
            {
                for (int i = 0; i < movies.Count; i++)
                    if (movies[i].getId() == id)
                    {
                        movies.RemoveAt(i);
                        break;
                    }
            }
        }

        public void loadMovie(ushort id, ushort condition, bool resetCond, bool loop)
        {
            // ScriptedMovie movie;

            // if (!state.getVar("MovieUseBackground"))
                // movie = new ScriptedMovie(this, id);
            // else {
                // movie = new ProjectorMovie(this, id, _projectorBackground);
                // _projectorBackground = 0;
                // _state->setMovieUseBackground(0);
            // }

            // movie.setCondition(condition);
            // movie.setDisableWhenComplete(resetCond);
            // movie.setLoop(loop);

            /*


            if (_state->getMovieScriptDriven()) {
                movie->setScriptDriven(_state->getMovieScriptDriven());
                _state->setMovieScriptDriven(0);
            }

            if (_state->getMovieStartFrameVar()) {
                movie->setStartFrameVar(_state->getMovieStartFrameVar());
                _state->setMovieStartFrameVar(0);
            }

            if (_state->getMovieEndFrameVar()) {
                movie->setEndFrameVar(_state->getMovieEndFrameVar());
                _state->setMovieEndFrameVar(0);
            }

            if (_state->getMovieStartFrame()) {
                movie->setStartFrame(_state->getMovieStartFrame());
                _state->setMovieStartFrame(0);
            }

            if (_state->getMovieEndFrame()) {
                movie->setEndFrame(_state->getMovieEndFrame());
                _state->setMovieEndFrame(0);
            }

            if (_state->getMovieNextFrameGetVar()) {
                movie->setNextFrameReadVar(_state->getMovieNextFrameGetVar());
                _state->setMovieNextFrameGetVar(0);
            }

            if (_state->getMovieNextFrameSetVar()) {
                movie->setNextFrameWriteVar(_state->getMovieNextFrameSetVar());
                _state->setMovieNextFrameSetVar(0);
            }

            if (_state->getMoviePlayingVar()) {
                movie->setPlayingVar(_state->getMoviePlayingVar());
                _state->setMoviePlayingVar(0);
            }

            if (_state->getMovieOverridePosition()) {
                movie->setPosU(_state->getMovieOverridePosU());
                movie->setPosV(_state->getMovieOverridePosV());
                _state->setMovieOverridePosition(0);
            }

            if (_state->getMovieUVar()) {
                movie->setPosUVar(_state->getMovieUVar());
                _state->setMovieUVar(0);
            }

            if (_state->getMovieVVar()) {
                movie->setPosVVar(_state->getMovieVVar());
                _state->setMovieVVar(0);
            }

            if (_state->getMovieOverrideCondition()) {
                movie->setCondition(_state->getMovieOverrideCondition());
                _state->setMovieOverrideCondition(0);
            }

            if (_state->getMovieConditionBit()) {
                movie->setConditionBit(_state->getMovieConditionBit());
                _state->setMovieConditionBit(0);
            }

            if (_state->getMovieForce2d()) {
                movie->setForce2d(_state->getMovieForce2d());
                _state->setMovieForce2d(0);
            }

            if (_state->getMovieVolume1()) {
                movie->setVolume(_state->getMovieVolume1());
                _state->setMovieVolume1(0);
            } else {
                movie->setVolume(_state->getMovieVolume2());
            }

            if (_state->getMovieVolumeVar()) {
                movie->setVolumeVar(_state->getMovieVolumeVar());
                _state->setMovieVolumeVar(0);
            }

            if (_state->getMovieSoundHeading()) {
                movie->setSoundHeading(_state->getMovieSoundHeading());
                _state->setMovieSoundHeading(0);
            }

            if (_state->getMoviePanningStrenght()) {
                movie->setSoundAttenuation(_state->getMoviePanningStrenght());
                _state->setMoviePanningStrenght(0);
            }

            if (_state->getMovieAdditiveBlending()) {
                movie->setAdditiveBlending(true);
                _state->setMovieAdditiveBlending(0);
            }

            if (_state->getMovieTransparency()) {
                movie->setTransparency(_state->getMovieTransparency());
                _state->setMovieTransparency(0);
            } else {
                movie->setTransparency(100);
            }

            if (_state->getMovieTransparencyVar()) {
                movie->setTransparencyVar(_state->getMovieTransparencyVar());
                _state->setMovieTransparencyVar(0);
            }

            */

            // movies.Add(movie);
        }

        public void setMovieLooping(ushort id, bool loop)
        {
            for (int i = 0; i < movies.Count; i++)
            {
                if (movies[i].getId() == id)
                {
                    // Enable or disable looping
                    movies[i].setLoop(loop);
                    movies[i].setDisableWhenComplete(!loop);
                    break;
                }
            }
        }

        public void loadNodeCubeFaces(ushort nodeID)
        {
            state.setViewType(ViewType.kCube);

            // _cursor->lockPosition(true);
            // updateCursor();

            node = new NodeCube(this, nodeID);
            // int matIndex = 0;

            transitionState = !transitionState;

            currentNodeData = db.getNodeData((ushort)state.getVar("LocationNode"), (uint)state.getVar("LocationRoom"), (uint)state.getVar("LocationAge"));
        }

        public Texture2D getNextNodeCubeTex(int index)
        {
            if (transitionState)
                return nodeTexturesA[index];
            return nodeTexturesB[index];
        }

        public bool decodeJpeg(DirectorySubEntry jpegDesc, Texture2D tex, int faceIndex)
        {
            MemoryStream jpegStream = jpegDesc.getData();
            byte[] imageBytes = jpegStream.ToArray();

            bool doDump = imgDumpTextures && (!imgEditorOnly || Application.isEditor);
            if (doDump)
            {
                string fileName = $"{state.getVar("LocationAge")}_" +
                    $"{state.getVar("LocationRoom")}_" +
                    $"{state.getVar("LocationNode")}_{faceIndex}.jpg";
                string fullFileName = Path.Combine(imgDumpFolder, fileName);

                using (FileStream file = new FileStream(fullFileName, FileMode.Create, FileAccess.Write))
                    file.Write(imageBytes, 0, imageBytes.Length);

                if (imgNoiseReduction || imgUpscale)
                {
                    string command = "";
                    if (imgNoiseReduction)
                        command = $"--noise-level {imgNoiseReductionAggressivity} ";
                    if (imgUpscale)
                        command = $"--scale-ratio {imgUpscaleFactor} ";
                    if (imgNoiseReduction && imgUpscale)
                        command += "-m noise-scale";
                    else if (imgNoiseReduction)
                        command += "-m noise";
                    else if (imgUpscale)
                        command += "-m scale";

                    command += $" -c 0"; // gonna make huge pngs, but fast...

                    command += $" -i \"{fullFileName}\"";
                    command += $" -o \"{fullFileName}.png\"";

                    print(imgWaifuExecutable + ' ' + command);
                    var proc = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo(imgWaifuExecutable, command)
                        {
                            WorkingDirectory = Path.GetDirectoryName(imgWaifuExecutable),
                            CreateNoWindow = true
                        }
                    };
                    proc.Start();
                    proc.WaitForExit();
                    imageBytes = File.ReadAllBytes(fullFileName + ".png");
                }
            }

            if (tex.LoadImage(imageBytes))
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                return true;
            }
            return false;
        }

        public DirectorySubEntry getFileDescription(string room, uint index, ushort face, DirectorySubEntry.ResourceType type)
        {
            string archiveRoom = room;
            if (archiveRoom == "")
                archiveRoom = db.getRoomName((uint)state.getVar("LocationRoom"), (uint)state.getVar("LocationAge"));

            DirectorySubEntry desc = null;

            // Search common archives
            int i = 0;
            while (desc == null && i < archivesCommon.Count)
            {
                desc = archivesCommon[i].getDescription(archiveRoom, index, face, type);
                i++;
            }

            // Search currently loaded node archive
            if (desc == null && archiveNode != null)
                desc = archiveNode.getDescription(archiveRoom, index, face, type);

            return desc;
        }

        public void goToNode(ushort nodeID, TransitionType transitionType)
        {
            ushort node = (ushort)state.getVar("LocationNextNode");
            if (node == 0)
                node = nodeID;

            ushort room = (ushort)state.getVar("LocationNextRoom");
            ushort age = (ushort)state.getVar("LocationNextAge");
            // print(new Vector3(node, room, age));

            // TODO
            /*
            setupTransition();

            ViewType sourceViewType = _state->getViewType();
            if (sourceViewType == kCube) {
                // The lookat direction in the next node should be
                // the direction of the mouse cursor
                float pitch, heading;
                _cursor->getDirection(pitch, heading);
                _state->lookAt(pitch, heading);
            }

            //*/

            loadNode(node, room, age);

            state.setVar("LocationNextNode", 0);
            state.setVar("LocationNextRoom", 0);
            state.setVar("LocationNextAge", 0);

            // if (_state->getAmbiantPreviousFadeOutDelay() > 0) {
                // _ambient->playCurrentNode(100, _state->getAmbiantPreviousFadeOutDelay());
            // }

            // drawTransition(transitionType);
            //*/
        }


        void Update()
        {
            fovDebug = state.getLookAtFOV();

            if (transitionState)
                transitionProgress = Mathf.Min(transitionProgress + Time.deltaTime / transitionDuration, 1);
            else
                transitionProgress = Mathf.Max(transitionProgress - Time.deltaTime / transitionDuration, 0);

            matWest.SetFloat("_Transition", transitionProgress);
            matNadir.SetFloat("_Transition", transitionProgress);
            matEast.SetFloat("_Transition", transitionProgress);
            matNorth.SetFloat("_Transition", transitionProgress);
            matSouth.SetFloat("_Transition", transitionProgress);
            matZenith.SetFloat("_Transition", transitionProgress);


            // // TMP HACK: check cursor location
            // // this should be enough to trigger half the script attached to pointed hotspots -
            // // thus allowing us to move from node to node... Kinda.
            // if (controller.ControllerInputDevice.Buttons.HasFlag(GvrControllerButton.TouchPadButton) && (transitionProgress == 0 || transitionProgress == 1))
            // {
            //     if (state.getViewType() == ViewType.kCube)
            //     {
            //         foreach (HotSpot hs in currentNodeData.hotspots)
            //         {
            //             float x = controllerPointer.PointerTransform.rotation.eulerAngles.x;
            //             float y = controllerPointer.PointerTransform.rotation.eulerAngles.y;
            //             int hitRect = hs.isPointInRectsCube(x, y);
            //             if (hitRect >= 0 && hs.isEnabled(state, 0)) {
            //                 // trigger the hotspot's script.
            //                 scriptEngine.run(hs.script);
            //             }
            //         }
            //     }
            // }

            // if (controller.ControllerInputDevice.ButtonsUp.HasFlag(GvrControllerButton.App) && (transitionProgress == 0 || transitionProgress == 1))
            // {
            //     curStartPoint++;
            //     curStartPoint %= startPoints.Length;
            //     hackAgeStartPoint point = startPoints[curStartPoint];
            //     loadNode((ushort)point.node, (uint)point.room, (uint)point.age);
            // }
            
            if (Input.GetKeyDown(KeyCode.N) && (transitionProgress == 0 || transitionProgress == 1))
            {
                curStartPoint++;
                curStartPoint %= startPoints.Length;
                hackAgeStartPoint point = startPoints[curStartPoint];
                loadNode((ushort)point.node, (uint)point.room, (uint)point.age);
            }

            if (Input.GetKeyDown(KeyCode.D))
                extractAllNodes();

            if (debugTestContent)
            {
                debugTestContent = false;
                TestDatabase();
            }
        }




        // TODO TMP
        // (probably nothing to do as most will be done either by Unity or by other monobehaviors)
        /*
        void Update()
        {

            if (state->getViewType() == kCube) {
                float pitch = _state->getLookAtPitch();
                float heading = _state->getLookAtHeading();
                float fov = _state->getLookAtFOV();

                // Apply the rotation effect
                if (_rotationEffect) {
                    _rotationEffect->update();

                    heading += _rotationEffect->getHeadingOffset();
                    _state->lookAt(pitch, heading);
                }

                // Apply the shake effect
                if (_shakeEffect) {
                    _shakeEffect->update();
                    pitch += _shakeEffect->getPitchOffset();
                    heading += _shakeEffect->getHeadingOffset();
                }

                _gfx->setupCameraPerspective(pitch, heading, fov);
            }

            if (_node) {
                _node->update();
                _gfx->renderDrawable(_node, _scene);
            }

            for (int i = _movies.size() - 1; i >= 0 ; i--) {
                _movies[i]->update();
                _gfx->renderDrawable(_movies[i], _scene);
            }

            if (_state->getViewType() == kMenu) {
                _gfx->renderDrawable(_menu, _scene);
            }

            for (uint i = 0; i < _drawables.size(); i++) {
                _gfx->renderDrawable(_drawables[i], _scene);
            }

            if (_state->getViewType() != kMenu) {
                float pitch = _state->getLookAtPitch();
                float heading = _state->getLookAtHeading();
                SunSpot flare = computeSunspotsIntensity(pitch, heading);
                if (flare.intensity >= 0)
                    _scene->drawSunspotFlare(flare);
            }

            if (isInventoryVisible()) {
                _gfx->renderWindow(_inventory);
            }

            // Draw overlay 2D movies
            for (int i = _movies.size() - 1; i >= 0 ; i--) {
                _gfx->renderDrawableOverlay(_movies[i], _scene);
            }

            for (uint i = 0; i < _drawables.size(); i++) {
                _gfx->renderDrawableOverlay(_drawables[i], _scene);
            }

            // Draw spot subtitles
            if (_node) {
                _gfx->renderDrawableOverlay(_node, _scene);
            }

            bool cursorVisible = _cursor->isVisible();

            if (getPlatform() == Common::kPlatformXbox) {
                // The cursor is not drawn in the Xbox version menus and journals
                cursorVisible &= !(_state->getLocationRoom() == 901 || _state->getLocationRoom() == 902);
            }

            if (cursorVisible)
                _gfx->renderDrawable(_cursor, _scene);

            _gfx->flipBuffer();

            if (!noSwap) {
                _frameLimiter->delayBeforeSwap();
                _system->updateScreen();
                _state->updateFrameCounters();
                _frameLimiter->startFrame();
            }

        }
        // */

        public void addSunSpot(ushort pitch, ushort heading, ushort intensity,
                ushort color, ushort var, bool varControlledIntensity, ushort radius)
        {
            SunSpot s = new SunSpot();

            s.pitch = pitch;
            s.heading = heading;
            s.intensity = intensity * 2.55f;
            s.color = (uint)((color & 0xF) | 16
                    * ((color & 0xF) | 16
                    * (((color >> 4) & 0xF) | 16
                    * (((color >> 4) & 0xF) | 16
                    * (((color >> 8) & 0xF) | 16
                    * (((color >> 8) & 0xF)))))));
            s.var = var;
            s.variableIntensity = varControlledIntensity;
            s.radius = radius;

            sunspots.Add(s);

            // sunshafts.enabled = true;
            SunSpot mostImportantSpot = s;
            foreach (SunSpot spot in sunspots)
                if (spot.intensity > mostImportantSpot.intensity)
                    mostImportantSpot = spot;

            sunshaftsCasterParent.rotation = Quaternion.Euler(-mostImportantSpot.pitch, mostImportantSpot.heading, 0);
            // sunshafts.sunShaftIntensity = mostImportantSpot.intensity;
            // sunshafts.sunColor = Common.int2col(mostImportantSpot.color);
            // print(mostImportantSpot.intensity);
            // print(mostImportantSpot.color);
            // print(mostImportantSpot.var);
            // print(mostImportantSpot.variableIntensity);
            // print(mostImportantSpot.radius);

            // print("sun color: " + mostImportantSpot.color);

            // since we don't have the correct sun color yet, simply force it to some nice looking value, depending on the Age
            // if (state.getVar("LocationAge") == 5)
            //     // J'nanin
            //     sunshafts.sunColor = new Color(1, .9176f, .698f);
            // else if (state.getVar("LocationAge") == 6)
            //     // Edanna
            //     sunshafts.sunColor = new Color(1, .945f, .8f);
            // else if (state.getVar("LocationAge") == 7)
            //     // Voltaic
            //     sunshafts.sunColor = new Color(1, .878f, .741f);
            // else if (state.getVar("LocationAge") == 8)
            //     // Narayan
            //     sunshafts.sunColor = new Color(.972f, .216f, .043f);
            // else if (state.getVar("LocationAge") == 10)
            //     // Amateria
            //     sunshafts.sunColor = new Color(1, .435f, 0);
        }

        public void playMovieGoToNode(ushort movie, ushort node)
        {
            ushort room = (ushort)state.getVar("LocationNextRoom");
            ushort age = (ushort)state.getVar("LocationNextAge");

            if (state.getVar("LocationNextNode") != 0)
                node = (ushort)state.getVar("LocationNextNode");

            // FIXME
            // if (state.getViewType() == ViewType.kCube && !state.getVar("CameraSkipAnimation"))
            // {
                // float startPitch, startHeading;
                // getMovieLookAt(movie, true, startPitch, startHeading);
                // animateDirectionChange(startPitch, startHeading, 0);
            // }
            state.setVar("CameraSkipAnimation", 0);

            loadNode(node, room, age);

            playSimpleMovie(movie, true);

            state.setVar("LocationNextNode", 0);
            state.setVar("LocationNextRoom", 0);
            state.setVar("LocationNextAge", 0);

            // FIXME
            // if (state.getViewType() == ViewType.kCube)
            // {
                // float endPitch, endHeading;
                // getMovieLookAt(movie, false, endPitch, endHeading);
                // state.lookAt(endPitch, endHeading);
            // }
        }

        void playSimpleMovie(ushort id)
        {
            playSimpleMovie(id, false);
        }
        void playSimpleMovie(ushort id, bool fullframe)
        {
            SimpleMovie movie = new SimpleMovie(this, id);

            // TODO
            /*
            if (state.getVar("MovieSynchronized")) {
                movie.setSynchronized(state->getMovieSynchronized());
                state->setMovieSynchronized(0);
            }

            if (_state->getMovieStartFrame()) {
                movie.setStartFrame(_state->getMovieStartFrame());
                _state->setMovieStartFrame(0);
            }

            if (_state->getMovieEndFrame()) {
                movie.setEndFrame(_state->getMovieEndFrame());
                _state->setMovieEndFrame(0);
            }

            if (_state->getMovieVolume1()) {
                movie.setVolume(_state->getMovieVolume1());
                _state->setMovieVolume1(0);
            } else {
                movie.setVolume(_state->getMovieVolume2());
            }

            if (fullframe) {
                movie.setForce2d(_state->getViewType() == kCube);
                movie.setForceOpaque(true);
                movie.setPosU(0);
                movie.setPosV(0);
            }

            movie.playStartupSound();

            _drawables.push_back(&movie);

            bool skip = false;

            while (!skip && !shouldQuit() && movie.update()) {
                // Process events
                Common::Event event;
                while (getEventManager()->pollEvent(event))
                    if (event.type == Common::EVENT_MOUSEMOVE) {
                        if (_state->getViewType() == kCube)
                            _scene->updateCamera(event.relMouse);

                        _cursor->updatePosition(event.mouse);

                    } else if (event.type == Common::EVENT_KEYDOWN) {
                        if (event.kbd.keycode == Common::KEYCODE_SPACE
                                || event.kbd.keycode == Common::KEYCODE_ESCAPE)
                            skip = true;
                    }

                drawFrame();
            }

            _drawables.pop_back();

            // Reset the movie script so that the next movie will not try to run them
            // when the user has skipped this one before the script is triggered.
            _state->setMovieScriptStartFrame(0);
            _state->setMovieScript(0);
            _state->setMovieAmbiantScriptStartFrame(0);
            _state->setMovieAmbiantScript(0);
            //*/
        }

        void TestDatabase()
        {
            db.TestContent();
        }
    }
}
