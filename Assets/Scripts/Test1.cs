using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test1 : MonoBehaviour {
	public Texture2D perlinTex;
	// Use this for initialization
	void Start () {
		var mapgen = new MapGen(512, perlinTex);
		mapgen.Create(200);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
