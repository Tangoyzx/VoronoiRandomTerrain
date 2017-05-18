using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using csDelaunay;

public class MapGen {
	private readonly float LAKE_THRESHOLD = 0.3f;
	private int size;
	private Texture2D perlinTex;
	private List<Vector2> points;
	public List<VEdge> edges = new List<VEdge>();
	public List<VCorner> corners = new List<VCorner>();
	public List<VCenter> centers = new List<VCenter>();
	private Dictionary<int, List<VCorner>> _cornerMap = new Dictionary<int, List<VCorner>>();
	public MapGen(int size, Texture2D perlinTex)
	{
		this.size = size;
		this.perlinTex = perlinTex;
	}
	public void Create(int n) {
		var points = GetRandomPoint(n);
		var voronoi = new Voronoi(points,new Rect(0, 0, this.size, this.size), 3);

		points.Clear();
		foreach (KeyValuePair<Vector2,Site> kv in voronoi.SitesIndexedByLocation) {
			points.Add(kv.Key);
		}

		BuildGraph(points, voronoi);
		// voronoi.Dispose();
		AssignCornerElevations();
		AssignOceanCoastAndLand();

		
		RedistributeElevations(GetLandCorners(corners));
		ClearNotLandElevations();
		AssignPolygonElevations();

		CalculateDownslopes();
		CalculateWatersheds();
		CreateRivers();
		RedistributeMoisture(GetLandCorners(corners));
		AssignPolygonMoisture();

		AssignBiomes();

		// drawDebugEdge(points, voronoi);
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
				if (dx * dx + dy * dy < 0.01f) {
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
			} else {
				q1.elevation = float.PositiveInfinity;
			}
		}

		while (queue.Count > 0) {
			var q = queue.Dequeue();

			foreach(var s in q.adjacent) {
				var newElevation = 0.01f + q.elevation;
				if (!q.water && !s.water) {
					newElevation += 1.0f;

					newElevation += Random.Range(0.0f, 2.0f);
				}

				if (newElevation < s.elevation) {
					s.elevation = newElevation;
					queue.Enqueue(s);
				}
			}
		}
	}

	private void AssignOceanCoastAndLand() {
		var queue = new Queue<VCenter>();
		foreach(var p in centers)
		{
			var numWater = 0;
			foreach(var q in p.corners) {
				if (q.border) {
					p.border = true;
					p.ocean = true;
					q.water = true;
					queue.Enqueue(p);
				}
				if (q.water) {
					numWater += 1;
				}
			}
			p.water = (p.ocean || numWater >= p.corners.Count * LAKE_THRESHOLD);
		}
		while (queue.Count > 0) {
			var p = queue.Dequeue();
			foreach(var r in p.neighbors) {
				if (r.water && !r.ocean) {
					r.ocean = true;
					queue.Enqueue(r);
				}
			}
		}

		foreach(var p in centers) {
			var numOcean = 0;
			var numLand = 0;
			foreach(var r in p.neighbors) {
				numOcean += (r.ocean)?1:0;
				numLand += (!r.water)?1:0;
			}
			p.coast = (numOcean > 0) && (numLand > 0);
		}

		foreach(var q in corners) {
			var numOcean = 0;
			var numLand = 0;
			foreach(var p in q.touches) {
				numOcean += (p.ocean)?1:0;
				numLand += (!p.water)?1:0;
			}
			q.ocean = (numOcean == q.touches.Count);
			q.coast = (numOcean > 0) && (numLand > 0);
			q.water = q.border || ((numLand != q.touches.Count) && !q.coast);
		}
	}

	private void RedistributeElevations(List<VCorner> locations)
	{
		var SCALE_FACTOR = 1.1f;
		
		locations.Sort(sortOnElevation);
		for(var i = 0; i < locations.Count; i++)
		{
			var y = (float)i / (locations.Count - 1.0f);
			var x = Mathf.Sqrt(SCALE_FACTOR) - Mathf.Sqrt(SCALE_FACTOR * (1 - y));
			if (x > 1.0f) x = 1.0f;
			locations[i].elevation = x;
		}
	}

	private void ClearNotLandElevations()
	{
		foreach(var q in corners) {
			if (q.ocean || q.coast) {
				q.elevation = 0.0f;
			}
		}
	}

	private void AssignPolygonElevations() {
		foreach(var p in centers) {
			var sumElevation = 0.0f;
			foreach(var q in p.corners) {
				sumElevation += q.elevation;
			}
			p.elevation = sumElevation / p.corners.Count;
		}
	}

	private void CalculateDownslopes() {
		foreach(var q in corners) {
			var r = q;
			foreach(var s in q.adjacent) {
				if (s.elevation <= r.elevation) {
					r = s;
				}
			}
			q.downslope = r;
		}
	}

	private void CalculateWatersheds() {
		foreach(var q in corners) {
			q.watershed = q;
			if (!q.ocean && !q.coast) {
				q.watershed = q.downslope;
			}
		}

		for(var i = 0; i < 100; i++)
		{
			var changed = false;
			foreach(var q in corners) {
				if (!q.ocean && !q.coast && !q.watershed.coast) {
					var r = q.downslope.watershed;
					if (!r.ocean) {
						q.watershed = r;
						changed = true;
					}
				}
			}
			if (!changed) break;
		}

		foreach(var q in corners) {
			var r = q.watershed;
			r.watershed_size = 1 + r.watershed_size;
		}
	}

	private void CreateRivers() {
		for(var i = 0; i < size * 0.2f; i++)
		{
			var q = corners[Random.Range(0, corners.Count - 1)];
			if (q.ocean || q.elevation < 0.3f || q.elevation > 0.9f) continue;
			while (!q.coast) {
				if (q == q.downslope) break;
				var edge = LookUpEdgeFromCorner(q, q.downslope);
				edge.river = edge.river + 1;
				q.river = q.river + 1;
				q.downslope.river = q.downslope.river + 1;
				q = q.downslope;
			}
		}
	}

	private void AssignCornerMoisture() {
		var queue = new Queue<VCorner>();
		foreach(var q in corners) {
			if ((q.water || q.river > 0) && !q.ocean) {
				q.moisture = (q.river > 0)?Mathf.Min(3.0f, (0.2f * q.river)):1.0f;
				queue.Enqueue(q);
			} else {
				q.moisture = 0.0f;
			}
		}

		while (queue.Count > 0) {
			var q = queue.Dequeue();
			foreach(var r in q.adjacent) {
				var newMoisture = q.moisture * 0.9f;
				if (newMoisture > r.moisture) {
					r.moisture = newMoisture;
					queue.Enqueue(r);
				}
			}
		}

		foreach(var q in corners) {
			if (q.ocean || q.coast) {
				q.moisture = 1.0f;
			}
		}
	}

	private void RedistributeMoisture(List<VCorner> locations) {
		locations.Sort(sortOnMoisture);
		for(var i = 0; i < locations.Count; i++)
		{
			locations[i].moisture = (float)i / (locations.Count - 1);
		}
	}

	private void AssignPolygonMoisture() {
		foreach(var p in centers) {
			var sumMoisture = 0.0f;
			foreach(var q in p.corners) {
				if (q.moisture > 1.0f) q.moisture = 1.0f;
				sumMoisture += q.moisture;
			}
			p.moisture = sumMoisture / p.corners.Count;
		}
	}

	private void AssignBiomes() {
		foreach(var p in centers) {
			p.biome = GetBiome(p);
		}
	}

	private int sortOnElevation(VCorner ca, VCorner cb)
	{
		return (int)(ca.elevation - cb.elevation);
	}

	private int sortOnMoisture(VCorner ca, VCorner cb) {
		return (int)(ca.moisture - cb.moisture);
	}

	private bool IsInLand(Vector2 point)
	{
		var q = new Vector2(point.x / size * 2.0f - 1.0f, point.y / size * 2.0f - 1.0f);
		var c = perlinTex.GetPixel((int)((q.x+1.0f) * perlinTex.width * 0.5f), (int)((q.y+1.0f) * perlinTex.height * 0.5f));
		return (q.sqrMagnitude < 0.7f && c.r > 0.41f);
	}

	private List<VCorner> GetLandCorners(List<VCorner> corners) {
		var locations = new List<VCorner>();
		foreach(var q in corners) {
			if (!q.ocean && !q.coast) {
				locations.Add(q);
			}
		}
		return locations;
	}

	private VEdge LookUpEdgeFromCorner(VCorner q, VCorner s) {
		foreach(var edge in q.protrudes) {
			if (edge.v0 == s || edge.v1 == s) return edge;
		}
		return null;
	}

	private string GetBiome(VCenter p) {
		if (p.ocean) {
        return "OCEAN";
      } else if (p.water) {
        if (p.elevation < 0.1) return "MARSH";
        if (p.elevation > 0.8) return "ICE";
        return "LAKE";
      } else if (p.coast) {
        return "BEACH";
      } else if (p.elevation > 0.8) {
        if (p.moisture > 0.50) return "SNOW";
        else if (p.moisture > 0.33) return "TUNDRA";
        else if (p.moisture > 0.16) return "BARE";
        else return "SCORCHED";
      } else if (p.elevation > 0.6) {
        if (p.moisture > 0.66) return "TAIGA";
        else if (p.moisture > 0.33) return "SHRUBLAND";
        else return "TEMPERATE_DESERT";
      } else if (p.elevation > 0.3) {
        if (p.moisture > 0.83) return "TEMPERATE_RAIN_FOREST";
        else if (p.moisture > 0.50) return "TEMPERATE_DECIDUOUS_FOREST";
        else if (p.moisture > 0.16) return "GRASSLAND";
        else return "TEMPERATE_DESERT";
      } else {
        if (p.moisture > 0.66) return "TROPICAL_RAIN_FOREST";
        else if (p.moisture > 0.33) return "TROPICAL_SEASONAL_FOREST";
        else if (p.moisture > 0.16) return "GRASSLAND";
        else return "SUBTROPICAL_DESERT";
      }
	}
}
