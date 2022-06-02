using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

public static class Noise {

	[BurstCompile]
	/// <param name="normalizeLocal">true is local, false is global</param>
	public static void GenerateNoiseMap(NativeArray<float> noiseMap, int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, float2 offset, bool normalizeLocal) {

		Unity.Mathematics.Random prng = new Unity.Mathematics.Random((uint)seed);
		NativeArray<float2> octaveOffsets = new NativeArray<float2>(octaves, Allocator.Temp);

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < octaves; i++) {
			float offsetX = prng.NextFloat (-100000, 100000) + offset.x;
			float offsetY = prng.NextFloat (-100000, 100000) - offset.y;
			octaveOffsets [i] = new float2 (offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= persistance;
		}

		if (scale <= 0) {
			scale = 0.0001f;
		}

		float maxLocalNoiseHeight = float.MinValue;
		float minLocalNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

		for (int y = 0; y < mapWidth; y++) {
			for(int x = 0; x < mapHeight; x++) {

				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < octaves; i++) {
					float sampleX = (x - halfWidth + octaveOffsets [i].x) / scale * frequency;
					float sampleY = (y - halfHeight + octaveOffsets [i].y) / scale * frequency;

					float perlinValue = noise.cnoise (new float2(sampleX, sampleY)) + 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistance;
					frequency *= lacunarity;
				}

				if (noiseHeight > maxLocalNoiseHeight) {
					maxLocalNoiseHeight = noiseHeight;
				} else if (noiseHeight < minLocalNoiseHeight) {
					minLocalNoiseHeight = noiseHeight;
				}
				noiseMap [ArrayFlatten.IndexToFlat2D(x, y, mapWidth)] = noiseHeight;
			}
		}

		for (int y = 0; y < mapWidth; y++) {
			for (int x = 0; x < mapHeight; x++) {
				if (normalizeLocal) {
					noiseMap [ArrayFlatten.IndexToFlat2D(x, y, mapWidth)] = math.unlerp (minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap [ArrayFlatten.IndexToFlat2D(x, y, mapWidth)]);
				} else {
					float normalizedHeight = (noiseMap [ArrayFlatten.IndexToFlat2D(x, y, mapWidth)] + 1) / (2f * maxPossibleHeight / 2f);
					noiseMap [ArrayFlatten.IndexToFlat2D(x, y, mapWidth)] = math.clamp (normalizedHeight, 0, int.MaxValue);
				}
			}
		}

		octaveOffsets.Dispose();
	}
}
