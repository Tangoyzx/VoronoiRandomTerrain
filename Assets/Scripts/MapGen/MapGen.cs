using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using csDelaunay;

public class MapGen {
	public static int SIZE = 600;
	private List<Vector2> points;
	private List<VEdge> edges;
	private List<VCorner> corners;
	private List<VCenter> centers;

	private Dictionary<int, List<VCorner>> _cornerMap;
	public void Create(List<Vector2> points, Voronoi voronoi) {

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
			edge.d0 = centerLookup[dedge.p0];
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

	private void ImproveCorners() {
		var newCorners = new List<Vector2>(corners.Count);
		foreach (var q in corners)
		{
			if (q.border) {
				newCorners[q.index] = q.point;
			} else {
				var point = new Vector2();
				foreach(var r in q.touches)
				{
					point.x += r.point.x;
					point.y += r.point.y;
				}
				point.x /= q.touches.Count;
				point.y /= q.touches.Count;
			}
		}

		for(int i = 0; i < corners.Count; i++)
		{
			corners[i].point = newCorners[i];
		}

		foreach(var edge in edges)
		{
			if ((edge.v0 != null) && (edge.v1 != null)) {
				edge.midpoint = Vector2.Lerp(edge.v0.point, edge.v1.point, 0.5f);
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
		q.border = (point.x == 0 || point.x == SIZE || point.y == 0 || point.y == SIZE);
		q.touches = new List<VCenter>();
		q.protrudes = new List<VEdge>();
		q.adjacent = new List<VCorner>();
		_cornerMap[bucket].Add(q);
		return q;
	}
}
