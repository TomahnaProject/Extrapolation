using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerialProject
{
    public List<SerialCube> cubes = new();
    public List<SerialPointOfInterest> pointsOfInterest = new();
    public List<SerialPoiOnNode> poiOnNodes = new();
}
