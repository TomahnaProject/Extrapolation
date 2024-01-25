using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace Myst3
{
    public interface IMyst3Engine
    {
        Platform getPlatform();
        Language getGameLanguage();
        GameLocalizationType getGameLocalizationType();
        void loadNodeCubeFaces(ushort nodeID);
        GameState state { get; }
        void loadMovie(ushort id, ushort condition, bool resetCond, bool loop);
        void removeMovie(ushort id);
        void setMovieLooping(ushort id, bool loop);
        void addSunSpot(ushort pitch, ushort heading, ushort intensity, ushort color, ushort var, bool varControlledIntensity, ushort radius);
        void goToNode(ushort nodeID, TransitionType transitionType);
        void playMovieGoToNode(ushort movie, ushort node);
        void runScriptsFromNode(ushort nodeID, uint roomID, uint ageID);
        Cursor cursor { get; }
    }
}
