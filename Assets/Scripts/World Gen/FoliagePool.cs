using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using NUnit.Framework;

public class FoliagePool : MonoBehaviour {

	[System.Serializable]
	public class FoliageItem{
		public GameObject prefab;
		public int spawnChance;
		public int minHeight;
		public int maxHeight;
	}

	[System.Serializable]
	public class FoliageSubPool{
		public int poolSize;
		public int spawnChance;
		public string tag;
		public List<FoliageItem> items;
	}

	public GameObject defaultParent;
	public List<FoliageSubPool> subPools;
	public Dictionary<string, FoliageSubPool> subPoolDict;
	Dictionary<string, Queue<GameObject>> poolDict;

	void Start(){
		
		poolDict = new Dictionary<string, Queue<GameObject>> ();
		subPoolDict = new Dictionary<string, FoliageSubPool> ();

		foreach (FoliageSubPool subPool in subPools){
			subPoolDict.Add (subPool.tag, subPool);
		}

		foreach (KeyValuePair<string, FoliageSubPool> subPoolPair in subPoolDict){
			FoliageSubPool subPool = subPoolPair.Value;
				
			for (int j = 0; j < subPool.items.Count; j++){

				Queue<GameObject> objectPool = new Queue<GameObject> ();

				//90 is for leway, poolsize will end up being a bit larger than designated
				for(int i =  0; i < subPool.poolSize * (subPool.items[j].spawnChance / 90.0F); i++){
					GameObject currObj = Instantiate (subPool.items[j].prefab, new Vector3(999999, 999999, 99999), Quaternion.identity);
					currObj.transform.parent = defaultParent.transform;
					objectPool.Enqueue (currObj);
				}

				poolDict.Add (subPoolPair.Key + j.ToString (), objectPool);
			}
		}
	}

	public static string getKey(string tag, int type){
		return tag + type.ToString();
	}

	public GameObject SpawnFromPool (string tag, Vector3 position, Quaternion rotation, Vector3 scaleChange, GameObject parent){
		if (parent = null){
			parent = defaultParent;
		}

		if (!poolDict.ContainsKey(tag)){
			Debug.LogWarning ("Pool with tag " + tag + " doesn't exist.");
			return null;
		}

		GameObject spawnObject = poolDict [tag].Dequeue();

		spawnObject.SetActive (true);
		spawnObject.transform.position = position;
		spawnObject.transform.rotation = rotation;
		spawnObject.transform.localScale += scaleChange;
		//spawnObject.transform.parent = parent.transform;

		poolDict [tag].Enqueue (spawnObject);

		return spawnObject;
	}
}
