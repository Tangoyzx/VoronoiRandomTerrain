using UnityEngine;
using System.Collections.Generic;
public class VCenter {
    public int index;

    public Vector2 point = Vector2Checker.getNull();
    public bool water;
    public bool ocean;
    public bool coast;
    public bool border;
    public string biome;
    public float elevation;
    public float moisture;

    public List<VCenter> neighbors;
    public List<VEdge> borders;
    public List<VCorner> corners;
}