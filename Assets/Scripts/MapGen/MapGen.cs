using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using csDelaunay;

public class MapGen {
	private int size;
	private Texture2D perlinTex;
	private List<Vector2> points;
	private List<VEdge> edges = new List<VEdge>();
	private List<VCorner> corners = new List<VCorner>();
	private List<VCenter> centers = new List<VCenter>();
	private Dictionary<int, List<VCorner>> _cornerMap = new Dictionary<int, List<VCorner>>();
	public MapGen(int size, Texture2D perlinTex)
	{
		this.size = size;
		this.perlinTex = perlinTex;
	}
	public void Create(int n) {
		var points = GetRandomPoint(n);
		// List<Vector2> points = new List<Vector2>();
        // points.Add(new Vector2(100, 100));
        // points.Add(new Vector2(100, 300));
		var voronoi = new Voronoi(points,new Rect(0, 0, this.size, this.size), 3);

		points.Clear();
		foreach (KeyValuePair<Vector2,Site> kv in voronoi.SitesIndexedByLocation) {
			points.Add(kv.Key);
		}

		BuildGraph(points, voronoi);
		// voronoi.Dispose();
		AssignCornerElevations();


		drawDebugEdge(points, voronoi);
	}

	private void drawDebugEdge(List<Vector2> points, Voronoi voronoi) {
		Debug.DrawLine(new Vector3(), new Vector3(this.size, 0, 0), Color.blue, float.MaxValue);
		Debug.DrawLine(new Vector3(this.size, 0, 0), new Vector3(this.size, this.size, 0), Color.blue, float.MaxValue);
		Debug.DrawLine(new Vector3(this.size, this.size, 0), new Vector3(0, this.size, 0), Color.blue, float.MaxValue);
		Debug.DrawLine(new Vector3(0, this.size, 0), new Vector3(0, 0, 0), Color.blue, float.MaxValue);

		foreach(var q in corners)
		{
			var point = q.point;
			
			var color = (q.water)?Color.blue:(q.elevation < 100)?Color.red:Color.green;
			Debug.DrawLine(new Vector3(point.x - 2, point.y - 2, 0), new Vector3(point.x + 2, point.y - 2, 0), color,float.MaxValue);
			Debug.DrawLine(new Vector3(point.x + 2, point.y - 2, 0), new Vector3(point.x + 2, point.y + 2, 0), color,float.MaxValue);
			Debug.DrawLine(new Vector3(point.x + 2, point.y + 2, 0), new Vector3(point.x - 2, point.y + 2, 0), color,float.MaxValue);
			Debug.DrawLine(new Vector3(point.x - 2, point.y + 2, 0), new Vector3(point.x - 2, point.y - 2, 0), color,float.MaxValue);
		}

		foreach(var edge in edges)
		{
			if (edge.v0 != null && edge.v1 != null) {
				var color = (edge.v0.water || edge.v1.water)?Color.blue:Color.green;
				Debug.DrawLine(new Vector3(edge.v0.point.x, edge.v0.point.y, 0), new Vector3(edge.v1.point.x, edge.v1.point.y, 0), color, float.MaxValue);
				Debug.Log(edge.v0.point + "|" + edge.v1.point);
			}

			if (edge.d0 != null && edge.d1 != null) {
				// Debug.DrawLine(new Vector3(edge.d0.point.x, edge.d0.point.y, 0), new Vector3(edge.d1.point.x, edge.d1.point.y, 0), Color.red, float.MaxValue);
			}
		}
	}

	private List<Vector2> GetRandomPoint(int n) {
		var list = new List<Vector2>();
		for(int i = 0; i < n; i++)
		{
			list.Add(new Vector2(Random.Range(0, this.size), Random.Range(0, this.size)));
		}
		return list;
	}

	private void BuildGraph(List<Vector2> points, Voronoi voronoi)
	{
		var libedges = voronoi.Edges;
		var centerLookup = new Dictionary<Vector2, VCenter>();

		foreach(var point in points)
		{
			var p = new VCenter();
			p.index = centers.Count;
			p.point = point;
			p.neighbors = new List<VCenter>();
			p.borders = new List<VEdge>();
			p.corners = new List<VCorner>();
			centers.Add(p);
			centerLookup[point] = p;
		}

		foreach(var c in centers)
		{
			voronoi.Region(c.point);
		}

		foreach(var libedge in libedges)
		{
			var dedge = libedge.delaunayLine();
			var vedge = libedge.voronoiEdge();

			var edge = new VEdge();
			edge.index = edges.Count;
			edge.river = 0;
			edges.Add(edge);
			if (Vector2Checker.isNull(vedge.p0) || Vector2Checker.isNull(vedge.p1))
				edge.midpoint = Vector2Checker.getNull();
			else
				edge.midpoint = Vector2.Lerp(vedge.p0, vedge.p1, 0.5f);

			edge.v0 = MakeCorner(vedge.p0);
			edge.v1 = MakeCorner(vedge.p1);
			if (centerLookup.ContainsKey(dedge.p0))
				edge.d0 = centerLookup[dedge.p0];
			if (centerLookup.ContainsKey(dedge.p1))
				edge.d1 = centerLookup[dedge.p1];

			if (edge.d0 != null) edge.d0.borders.Add(edge);
			if (edge.d1 != null) edge.d1.borders.Add(edge);
			if (edge.v0 != null) edge.v0.protrudes.Add(edge);
			if (edge.v1 != null) edge.v1.protrudes.Add(edge);

			if (edge.d0 != null && edge.d1 != null) {
				AddToCenterList(edge.d0.neighbors, edge.d1);
				AddToCenterList(edge.d1.neighbors, edge.d0);
			}

			if (edge.v0 != null && edge.v1 != null) {
				AddToCornerList(edge.v0.adjacent, edge.v1);
				AddToCornerList(edge.v1.adjacent, edge.v0);
			}

			if (edge.d0 != null) {
				AddToCornerList(edge.d0.corners, edge.v0);
				AddToCornerList(edge.d0.corners, edge.v1);
			}
			if (edge.d1 != null) {
				AddToCornerList(edge.d1.corners, edge.v0);
				AddToCornerList(edge.d1.corners, edge.v1);
			}

			if (edge.v0 != null) {
				AddToCenterList(edge.v0.touches, edge.d0);
				AddToCenterList(edge.v0.touches, edge.d1);
			}
			if (edge.v1 != null) {
				AddToCenterList(edge.v1.touches, edge.d0);
				AddToCenterList(edge.v1.touches, edge.d1);
			}
		}
	}

	private void AddToCornerList(List<VCorner> v, VCorner x) {
		if (x != null && v.IndexOf(x) < 0) v.Add(x);
	}

	private void AddToCenterList(List<VCenter> v, VCenter x)
	{
		if (x!= null && v.IndexOf(x) < 0) v.Add(x);
	}

	private VCorner MakeCorner(Vector2 point) {
		if (Vector2Checker.isNull(point)) return null;
		int bucket;
		for(bucket = (int)(point.x) - 1; bucket <= (int)(point.x) + 1; bucket++)
		{
			if (!_cornerMap.ContainsKey(bucket)) continue;
			foreach(var qq in _cornerMap[bucket])
			{
				var dx = point.x - qq.point.x;
				var dy = point.y - qq.point.y;
				if (dx * dx + dy * dy < float.Epsilon) {
					return qq;
				}
			}
		}
		bucket = (int)(point.x);
		if (!_cornerMap.ContainsKey(bucket)) _cornerMap[bucket] = new List<VCorner>();
		var q = new VCorner();
		q.index = corners.Count;
		corners.Add(q);
		q.point = point;
		q.border = (point.x == 0 || point.x == size || point.y == 0 || point.y == size);
		q.touches = new List<VCenter>();
		q.protrudes = new List<VEdge>();
		q.adjacent = new List<VCorner>();
		_cornerMap[bucket].Add(q);
		return q;
	}

	private void AssignCornerElevations() {
		var queue = new Queue<VCorner>();
		foreach(var q0 in corners) {
			q0.water = !IsInLand(q0.point);
		}

		foreach(var q1 in corners)
		{
			if (q1.border) {
				q1.elevation = 0.0f;
				queue.Enqueue(q1);
			}
			else {
				q1.elevation = float.PositiveInfinity;
			}

			while (queue.Count > 0) {
				var q = queue.Dequeue();

				foreach(var s in q.adjacent) {
					var newElevation = 0.01f + q.elevation;
					if (!q.water && !s.water) {
						newElevation += 1;
					}

					if (newElevation < s.elevation) {
						s.elevation = newElevation;
						queue.Enqueue(s);
					}
				}
			}
		}
	}

	private bool IsInLand(Vector2 point)
	{
		var q = new Vector2(point.x / size * 2.0f - 1.0f, point.y / size * 2.0f - 1.0f);
		var c = perlinTex.GetPixel((int)(q.x+1) * perlinTex.width / 2, (int)(q.y+1) * perlinTex.height / 2);
		return c.r > (0.3 + 0.3 * q.sqrMagnitude);
	}
}
