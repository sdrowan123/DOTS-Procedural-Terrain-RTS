using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using NUnit.Framework;
using System.Security.Cryptography;
using System.Xml.Schema;
using System;
using System.IO;

public class FoliageGenerator : MonoBehaviour {
	public static FoliageData GenerateFoliageData(float[,] heightmap, float distBetweenFoliage, float meshHeightMultiplier, float uniformScale, int seed, AnimationCurve _heightCurve, Vector2 objectPosition, Dictionary<string, FoliagePool.FoliageSubPool> pool, GameObject parent, int maxMovesPerFrame){
		float meshSize = heightmap.GetLength (0);
		float topLeftX = objectPosition.x - (meshSize - 1) * uniformScale / 2f;
		float topLeftZ = objectPosition.y + (meshSize - 1) * uniformScale / 2f;

		List<Vector3> foliagePositions = new List<Vector3> ();
		List<Quaternion> foliageRotations = new List<Quaternion> ();
		List<Vector3> foliageScaleModifiers = new List<Vector3> ();
		List<string> tags = new List<string> ();

		//THIS MUST BE CHANGED TO ALLOW MORE THAN JUST TREES!!!
		FoliagePool.FoliageSubPool currSubPool = pool["tree"];

		//Generates Probability Array MAKE THIS MORE EFFICIENT???
		List<string> probabilityTags = new List<string>();
		List<FoliagePool.FoliageItem> probabilityItems = new List<FoliagePool.FoliageItem> ();

		for (int j = 0; j < currSubPool.items.Count; j ++){
			for(int i = 0; i < currSubPool.items[j].spawnChance; i++){
				probabilityTags.Add (FoliagePool.getKey("tree", j));
				probabilityItems.Add (currSubPool.items[j]);
			}
		}
		System.Random rng = new System.Random (seed);
		int subPoolCounter = 0;

		for (float y = 0; y < meshSize-1; y += distBetweenFoliage) {
			for (float x = 0; x < meshSize-1; x += distBetweenFoliage) {

				if(subPoolCounter < currSubPool.poolSize){

					int random1 = rng.Next (0, 100);
					int random2 = rng.Next (0, 100);
					FoliagePool.FoliageItem currItem = probabilityItems [random1];
					string currTag = probabilityTags [random1];
					float roundTemp = 0.5f;
					float height = Mathf.Round ((_heightCurve.Evaluate (heightmap [(int)x, (int)y]) * meshHeightMultiplier) / roundTemp) * roundTemp;

					if(height * uniformScale < currItem.maxHeight && height * uniformScale >= currItem.minHeight){
						if (random2 <= currSubPool.spawnChance){
							foliagePositions.Add (new Vector3 (topLeftX + x * uniformScale + random1/50, height * uniformScale - 1, topLeftZ - y * uniformScale + random1/50));

							int randomRotation = rng.Next (0, 360);
							Quaternion rotation = Quaternion.Euler (0, randomRotation, 0);
							foliageRotations.Add (rotation);

							//int randomScaleYInt = rng.Next (2, 5);
							//float randomScaleY = randomScaleYInt / 4;
							//int randomScaleXZInt = rng.Next (8, 11);
							//float randomScaleXZ = randomScaleXZInt / 10;
							Vector3 scaleModifier = new Vector3 (0, 0, 0);
							foliageScaleModifiers.Add (scaleModifier);

							tags.Add (currTag);

							subPoolCounter += 1;
						}
					}
				}
			}
		}
		FoliageData foliageData = new FoliageData (foliagePositions.ToArray (), foliageRotations.ToArray (), foliageScaleModifiers.ToArray(), tags.ToArray (), parent, maxMovesPerFrame);
		return foliageData;
	}
}

public class FoliageData : MonoBehaviour{
	int maxMovesPerFrame;
	public Vector3[] foliagePositions;
	public Quaternion[] foliageRotations;
	public Vector3[] foliageScaleModifiers;
	public string[] tags;
	GameObject parent;

	public FoliageData(Vector3[] foliagePositions, Quaternion[] foliageRotations, Vector3[] foliageScaleModifiers, string[] tags, GameObject parent, int maxMovesPerFrame){
		this.maxMovesPerFrame = maxMovesPerFrame;
		this.foliagePositions = foliagePositions;
		this.foliageRotations = foliageRotations;
		this.foliageScaleModifiers = foliageScaleModifiers;
		this.tags = tags;
		this.parent = parent;
	}

	public void SpawnFoliage(FoliagePool foliagePool){
		for (int i = 0; i < tags.Length; i++) {
			foliagePool.SpawnFromPool (tags[i], foliagePositions[i], foliageRotations[i], foliageScaleModifiers[i], parent);
		}
	}
}
