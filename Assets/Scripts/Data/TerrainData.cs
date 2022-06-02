using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData {

	public readonly static float uniformScale = 5f;
	public bool useFalloff;
	public float heightRound;
	public float maxSteepness;
	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;
	public float[] meshHeightCurveRounded;

	public float distanceBetweenFoliage;
	public int maxFoliageMovesPerFrame;

	public float minHeight{
		get{
			return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate (0);
		}
	}

	public float maxHeight{
		get{
			return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate (1);
		}
	}

	protected override void OnValidate(){
		if(heightRound < 0.03){
			heightRound = 0.03f;
		}

		//MeshHeightCurveRounded will be between 0 and 2, multiply input by 500
		meshHeightCurveRounded = new float[1000];
		for(int i = 0; i < 1000; i++) {
			meshHeightCurveRounded[i] = meshHeightCurve.Evaluate(i / 700f);
        }

		base.OnValidate ();
	}
}
