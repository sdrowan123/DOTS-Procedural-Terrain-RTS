﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData {

	public bool normalizeLocal;
	public float noiseScale;
	public int octaves;
	[Range(0,1)]
	public float persistance;
	public float lacunarity;
	public int seed;
	public Vector2 offset;

	protected override void OnValidate(){
		if (noiseScale < 1){
			noiseScale = 1;
		}
		if (lacunarity < 1){
			lacunarity = 1;
		}
		if (octaves < 1){
			octaves = 1;
		}

		base.OnValidate ();
	}
}
