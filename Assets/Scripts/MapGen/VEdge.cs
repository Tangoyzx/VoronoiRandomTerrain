using UnityEngine;

public class VEdge {
    public int index;
    
    public VCenter d0;
    public VCenter d1;

    public VCorner v0;
    public VCorner v1;

    public Vector2 midpoint = Vector2Checker.getNull();
    public int river;
}