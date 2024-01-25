using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 414

namespace Myst3
{
    public class Movie
    {
        protected Myst3 vm;

        protected ushort id;
        // Subtitles subtitles;

        protected Vector3 pTopLeft;
        protected Vector3 pBottomLeft;
        protected Vector3 pBottomRight;
        protected Vector3 pTopRight;

        protected bool force2d;
        protected bool forceOpaque;
        protected int posU;
        protected int posV;

        protected BinkDecoder bink;
        protected Texture2D texture;

        protected int startFrame;
        protected int endFrame;

        protected int volume;

        protected bool additiveBlending;
        protected int transparency;

        public Movie(Myst3 vm, ushort id)
        {
            this.vm = vm;
            this.id = id;
            posU = 0;
            posV = 0;
            startFrame = 0;
            endFrame = 0;
            // texture = 0;
            force2d = false;
            forceOpaque = false;
            // subtitles = 0;
            volume = 0;
            additiveBlending = false;
            transparency = 100;

            bink = new BinkDecoder();

            DirectorySubEntry binkDesc = vm.getFileDescription("", id, 0, DirectorySubEntry.ResourceType.kMultitrackMovie);

            if (binkDesc == null)
                binkDesc = vm.getFileDescription("", id, 0, DirectorySubEntry.ResourceType.kDialogMovie);

            if (binkDesc == null)
                binkDesc = vm.getFileDescription("", id, 0, DirectorySubEntry.ResourceType.kStillMovie);

            if (binkDesc == null)
                binkDesc = vm.getFileDescription("", id, 0, DirectorySubEntry.ResourceType.kMovie);

            // Check whether the video is optional
            bool optional = false;
            if (vm.state.hasVar("MovieOptional"))
            {
                optional = vm.state.getVar("MovieOptional") != 0;
                vm.state.setVar("MovieOptional", 0);
            }

            if (binkDesc == null)
            {
                if (!optional)
                    throw new Exception("Movie " + id + " does not exist");
                else
                    return;
            }

            // HAXX
            // MemoryStream binkStream = binkDesc.getData();
            // using (FileStream file = new FileStream("bink.bin", FileMode.Create, System.IO.FileAccess.Write)) {
                // byte[] bytes = new byte[binkStream.Length];
                // binkStream.Read(bytes, 0, (int)binkStream.Length);
                // file.Write(bytes, 0, bytes.Length);
                // binkStream.Close();
                // Debug.Log("Wrote bink");
            // }

            /*
            // loadPosition(binkDesc.getVideoData()); // FIXME

            MemoryStream binkStream = binkDesc.getData();
            bink.setDefaultHighColorFormat(Graphics::PixelFormat(4, 8, 8, 8, 8, 0, 8, 16, 24));
            bink.loadStream(binkStream);

            if (binkDesc.getType() == DirectorySubEntry.ResourceType.kMultitrackMovie
                    || binkDesc.getType() == DirectorySubEntry.kDialogMovie) {
                uint language = vm.configManager.getInt("audio_language");
                bink.setAudioTrack(language);
            }

            bink.start();

            // if (vm.configManager.getBool("subtitles"))
                // subtitles = Subtitles.create(vm, id);

            // Clear the subtitles override anyway, so that it does not end up
            // being used by another movie at some point.
            vm.state.setVar("MovieOverrideSubtitles", 0);
            //*/
        }

        // virtual ~Movie();

        // public virtual void draw() override;
        // public virtual void drawOverlay() override;

        // /** Increase or decrease the movie's pause level by one */
        // public void pause(bool pause);

        public ushort getId() { return id; }
        // public bool isVideoLoaded() { return bink.isVideoLoaded(); }
        // public void setPosU(int v) { posU = v; }
        // public void setPosV(int v) { posV = v; }
        // public void setForce2d(bool b);
        // public void setForceOpaque(bool b) { forceOpaque = b; }
        // public void setStartFrame(int v);
        // public void setEndFrame(int v);
        // public void setVolume(int v) { volume = v; }


        // int adjustFrameForRate(int frame, bool dataToBink);
        // void loadPosition(VideoData videoData);
        // void drawNextFrameToTexture();

        // void draw2d();
        // void draw3d();
    }

    public class ScriptedMovie: Movie
    {
    // protected:
        bool enabled;
        bool loop;
        bool disableWhenComplete;
        bool scriptDriven;
        bool isLastFrame;

        short condition;
        ushort conditionBit;

        ushort startFrameVar;
        ushort endFrameVar;
        ushort posUVar;
        ushort posVVar;
        ushort volumeVar;

        uint soundHeading;
        uint soundAttenuation;

        ushort nextFrameReadVar;
        ushort nextFrameWriteVar;

        ushort playingVar;

        ushort transparencyVar;


        public ScriptedMovie(Myst3 vm, ushort id): base(vm, id)
        {
        }

        // virtual ~ScriptedMovie();

        // void draw() override;
        // void drawOverlay() override;
        // virtual void update();

        // void setEndFrameVar(ushort v) { _endFrameVar = v; }
        // void setNextFrameReadVar(ushort v) { _nextFrameReadVar = v; }
        // void setNextFrameWriteVar(ushort v) { _nextFrameWriteVar = v; }
        // void setPlayingVar(ushort v) { _playingVar = v; }
        // void setPosUVar(ushort v) { _posUVar = v; }
        // void setPosVVar(ushort v) { _posVVar = v; }
        // void setVolumeVar(ushort v) { _volumeVar = v; }
        // void setStartFrameVar(ushort v) { _startFrameVar = v; }
        // void setCondition(short condition) { _condition = condition; }
        // void setConditionBit(short cb) { _conditionBit = cb; }
        public void setDisableWhenComplete(bool upd) { disableWhenComplete = upd; }
        public void setLoop(bool loop) { this.loop = loop; }
        // void setScriptDriven(bool b) { _scriptDriven = b; }
        // void setSoundHeading(ushort v) { _soundHeading = v; }
        // void setSoundAttenuation(ushort v) { _soundAttenuation = v; }
        // void setAdditiveBlending(bool b) { _additiveBlending = b; }
        // void setTransparency(int v) { _transparency = v; }
        // void setTransparencyVar(ushort v) { _transparencyVar = v; }

        // protected void updateVolume();
    }

    public class SimpleMovie: Movie
    {
        public SimpleMovie(Myst3 vm, ushort id): base(vm, id)
        {
            startFrame = 1;
            // endFrame = bink.getFrameCount(); // FIXME
            // startEngineFrame = (uint)vm.state.getFrameCount();
        }

        public bool update()
        {
            /*
            if (bink.getCurFrame() < (startFrame - 1))
                bink.seekToFrame(startFrame - 1);

            // bink.setVolume(volume * Audio::Mixer::kMaxChannelVolume / 100); // FIXME

            ushort scriptStartFrame = vm.state.getVar("MovieScriptStartFrame");
            if (scriptStartFrame && bink.getCurFrame() > scriptStartFrame)
            {
                ushort script = vm.state.getVar("MovieScript");

                // The control variables are reset before running the script because
                // some scripts set up another movie triggered script
                vm.state.setVar("MovieScriptStartFrame", 0);
                vm.state.setVar("MovieScript", 0);

                vm.runScriptsFromNode(script);
            }

            ushort ambiantStartFrame = vm.state.getVar("MovieAmbiantScriptStartFrame");
            // FIXME
            // if (ambiantStartFrame && bink.getCurFrame() > ambiantStartFrame) {
                // vm->runAmbientScripts(_vm->_state->getMovieAmbiantScript());
                // vm->_state->setMovieAmbiantScriptStartFrame(0);
                // vm->_state->setMovieAmbiantScript(0);
            // }

            if (!synchronized)
            {
                // Play the movie according to the bink file framerate
                if (bink.needsUpdate())
                    drawNextFrameToTexture();
            }
            else
            {
                // Draw a movie frame each two engine frames
                int targetFrame = (vm.state.getVar("FrameCount") - startEngineFrame) >> 2;
                if (bink.getCurFrame() < targetFrame)
                    drawNextFrameToTexture();
            }

            return !bink.endOfVideo() && bink.getCurFrame() < endFrame;
            //*/ return true;
        }

        public void playStartupSound()
        {
            // TODO
        }

        public void setSynchronized(bool b) { synchronized = b; }

        bool synchronized = false;
        uint startEngineFrame;
    }
}
