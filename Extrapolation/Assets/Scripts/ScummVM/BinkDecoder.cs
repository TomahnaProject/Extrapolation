using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Myst3
{
    public class BinkDecoder {

        // public void setDefaultHighColorFormat(Graphics.PixelFormat format)
        // {
            // defaultHighColorFormat = format;
        // }

        public bool loadStream(Stream stream)
        {
            close();
            /*
            uint32 id = stream.ReadUInt32BE();
            if ((id != kBIKfID) && (id != kBIKgID) && (id != kBIKhID) && (id != kBIKiID))
                return false;

            uint32 fileSize         = stream->readUint32LE() + 8;
            uint32 frameCount       = stream->readUint32LE();
            uint32 largestFrameSize = stream->readUint32LE();

            if (largestFrameSize > fileSize) {
                warning("Largest frame size greater than file size");
                return false;
            }

            stream->skip(4);

            uint32 width  = stream->readUint32LE();
            uint32 height = stream->readUint32LE();

            uint32 frameRateNum = stream->readUint32LE();
            uint32 frameRateDen = stream->readUint32LE();
            if (frameRateNum == 0 || frameRateDen == 0) {
                warning("Invalid frame rate (%d/%d)", frameRateNum, frameRateDen);
                return false;
            }

            _bink = stream;

            uint32 videoFlags = _bink->readUint32LE();

            // BIKh and BIKi swap the chroma planes
            addTrack(new BinkVideoTrack(width, height, getDefaultHighColorFormat(), frameCount,
                    Common::Rational(frameRateNum, frameRateDen), (id == kBIKhID || id == kBIKiID), videoFlags & kVideoFlagAlpha, id));

            uint32 audioTrackCount = _bink->readUint32LE();

            if (audioTrackCount > 0) {
                _audioTracks.resize(audioTrackCount);

                _bink->skip(4 * audioTrackCount);

                // Reading audio track properties
                for (uint32 i = 0; i < audioTrackCount; i++) {
                    AudioInfo &track = _audioTracks[i];

                    track.sampleRate = _bink->readUint16LE();
                    track.flags      = _bink->readUint16LE();

                    initAudioTrack(track);
                }

                _bink->skip(4 * audioTrackCount);
            }

            // Reading video frame properties
            _frames.resize(frameCount);
            for (uint32 i = 0; i < frameCount; i++) {
                _frames[i].offset   = _bink->readUint32LE();
                _frames[i].keyFrame = _frames[i].offset & 1;

                _frames[i].offset &= ~1;

                if (i != 0)
                    _frames[i - 1].size = _frames[i].offset - _frames[i - 1].offset;

                _frames[i].bits = 0;
            }

            _frames[frameCount - 1].size = _bink->size() - _frames[frameCount - 1].offset;

            return true;
            //*/

            // TODO
            return false;
        }

        public bool setAudioTrack(int index)
        {
            // TODO
            return false;
        }

        public void start()
        {
            // TODO
        }

        public bool isVideoLoaded()
        {
            // TODO
            return false;
        }

        public uint getFrameCount()
        {
            // TODO
            return 0;
        }

        public int getCurFrame()
        {
            // TODO
            return 0;
        }

        public bool seekToFrame(uint frame)
        {
            // TODO
            return false;
        }

        public void setVolume(byte volume)
        {
            // TODO
        }

        public bool needsUpdate()
        {
            // TODO
            return false;
        }

        public bool endOfVideo()
        {
            // TODO
            return true;
        }

        public void close()
        {
            // TODO
        }
    }
}
