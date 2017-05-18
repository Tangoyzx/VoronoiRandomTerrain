using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test1 : MonoBehaviour {
	public Texture2D perlinTex;
    public Material terrainMat;
	// Use this for initialization
	void Start () {
		var mapgen = new MapGen(512, perlinTex);
		mapgen.Create(600);
        var r = GetComponent<Renderer>();

        DrawTexture(mapgen);
        MakeMesh(mapgen);
	}

    private void MakeMesh(MapGen mapGen) {
        var go = new GameObject("tt");
        go.transform.position = Vector3.zero;
        var mr = go.AddComponent<MeshRenderer>();
        var mf = go.AddComponent<MeshFilter>();

        var verticeList = new List<Vector3>();
        var triangles = new List<int>();
        var colorList = new List<Color>();
        var cornerVerticeIndexMap = new Dictionary<int, int>();

        foreach(var center in mapGen.centers) {
            var v0 = new Vector3(center.point.x, center.elevation * 50.0f, center.point.y);
            var v0Index = verticeList.Count;
            verticeList.Add(v0);
            colorList.Add(getColor(center.water, center.coast));
            foreach(var edge in center.borders) {
                if (edge.v0 == null || edge.v1 == null) {
                    continue;
                }
                var v1 = edge.v0;
                var v2 = edge.v1;
                
                var v1Index = GetCornerIndex(verticeList, cornerVerticeIndexMap, colorList, v1);
                var v2Index = GetCornerIndex(verticeList, cornerVerticeIndexMap, colorList, v2);

                var v01 = verticeList[v1Index] - v0;
                var v02 = verticeList[v2Index] - v0;
                if (Vector3.Cross(v01, v02).y < 0) {
                    var t = v1Index;
                    v1Index = v2Index;
                    v2Index = t;
                }

                triangles.Add(v0Index);
                triangles.Add(v1Index);
                triangles.Add(v2Index);
            }
        }

        mf.mesh.vertices = verticeList.ToArray();
        mf.mesh.triangles = triangles.ToArray();
        mf.mesh.colors = colorList.ToArray();
        mf.mesh.RecalculateNormals();
        mr.materials = new Material[]{terrainMat};
    }

    private Color getColor(bool isWater, bool isCoast) {
        var color = (isWater)?(Color.blue):(isCoast?Color.green:Color.red);
        return color;
    }

    private int GetCornerIndex(List<Vector3> vertices, Dictionary<int, int> map, List<Color> colorList, VCorner corner) {
        if (map.ContainsKey(corner.index)) return map[corner.index];
        var newIndex = vertices.Count;
        map.Add(corner.index, newIndex);
        vertices.Add(new Vector3(corner.point.x, corner.elevation * 50.0f, corner.point.y));
        colorList.Add(getColor(corner.water, corner.coast));
        return newIndex;
    }

	
	private void DrawTexture(MapGen mapGen) {
        var colorOcean = Color.blue;
		var colorCoast = new Color(186.0f / 255.0f, 139.0f / 255.0f, 69.0f / 255.0f);
		var colorLand = Color.green;
		var tex = new Texture2D(512, 512);

        // foreach(var center in mapGen.centers) {
        //     var color = (center.ocean)?(colorOcean):(center.coast?colorCoast:colorLand);
        //     DrawRect(center.point, 2, tex, color);
        // }

        // foreach(var edge in mapGen.edges) {
        //     if (edge.v0 == null) {
        //         if (edge.v1 == null) continue;
        //         else DrawRect(edge.v1.point, 5, tex, Color.green);
        //     } else if (edge.v1 == null) {
        //         DrawRect(edge.v0.point, 5, tex, Color.green);
        //     } else {
        //         DrawLine(edge.v0.point, edge.v1.point, tex, Color.red);
        //     }
        // }

        foreach(var corner in mapGen.corners) {
            var color = (corner.water)?Color.blue:Color.yellow;
            // var color = (corner.river > 0)?Color.blue:Color.green;
            DrawRect(corner.point, 2, tex, color);
        }

        tex.Apply();

        gameObject.GetComponent<Renderer>().material.mainTexture = tex;
	}

	private void DrawRect(Vector2 center, float size, Texture2D tx, Color color) {
		float halfSize = size * 0.5f;
		var lt = new Vector2(center.x - halfSize, center.y + halfSize);
		var rt = new Vector2(center.x + halfSize, center.y + halfSize);
		var rb = new Vector2(center.x + halfSize, center.y - halfSize);
		var lb = new Vector2(center.x - halfSize, center.y - halfSize);
		DrawLine(lt, rt, tx, color);
		DrawLine(rt, rb, tx, color);
		DrawLine(rb, lb, tx, color);
		DrawLine(lb, lt, tx, color);
	}

	private void DrawLine(Vector2 p0, Vector2 p1, Texture2D tx, Color c, int offset = 0) {
        int x0 = (int)p0.x;
        int y0 = (int)p0.y;
        int x1 = (int)p1.x;
        int y1 = (int)p1.y;
       
        int dx = Mathf.Abs(x1-x0);
        int dy = Mathf.Abs(y1-y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx-dy;
       
        while (true) {
            tx.SetPixel(x0+offset,y0+offset,c);
           
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2*err;
            if (e2 > -dy) {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx) {
                err += dx;
                y0 += sy;
            }
        }
    }
}
