using UnityEngine;
using System.Collections.Generic;

public class VCorner {
    public int index;

    public Vector2 point = Vector2Checker.getNull();
    public bool ocean;
    public bool water;
    public bool coast;
    public bool border;
    public float elevation;
    public float moisture;

    public List<VCenter> touches;
    public List<VEdge> protrudes;
    public List<VCorner> adjacent;

    public int river;
    public VCorner downslope;
    public VCorner watershed;
    public int watershed_size;
}