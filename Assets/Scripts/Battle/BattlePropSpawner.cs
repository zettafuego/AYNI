using System.Collections.Generic;
using UnityEngine;

namespace AYNI.Battle
{
    public class BattlePropSpawner : MonoBehaviour
    {
        [SerializeField] int platformCount = 14;
        [SerializeField] int collectibleCount = 18;

        readonly List<BattlePlatform> platforms = new();
        Transform propsRoot;

        public IReadOnlyList<BattlePlatform> Platforms => platforms;

        /// <summary>
        /// Usa las plataformas que ya están colocadas en la escena (mundo horneado/editado a mano)
        /// en lugar de generarlas con Random.
        /// </summary>
        public void RegisterBakedPlatforms()
        {
            platforms.Clear();
            var all = FindObjectsByType<BattlePlatform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var platform in all)
                if (platform != null) platforms.Add(platform);

            propsRoot = GameObject.Find("BattleProps")?.transform;
        }

        public void SpawnAll(float floorY, float minX, float maxX)
        {
            Clear();
            propsRoot = new GameObject("BattleProps").transform;

            SpawnPlatforms(floorY, minX, maxX);
            SpawnCollectibles(floorY, minX, maxX);
        }

        void Clear()
        {
            platforms.Clear();
            if (propsRoot != null) Destroy(propsRoot.gameObject);
        }

        void SpawnPlatforms(float floorY, float minX, float maxX)
        {
            float[] heights = { 1.2f, 2f, 2.8f, 3.6f, 4.2f };

            for (int i = 0; i < platformCount; i++)
            {
                float x = 0f;
                float w = 0f;
                bool clear = false;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    x = Random.Range(minX + 2f, maxX - 2f);
                    w = Random.Range(1.8f, 3.5f);
                    if (!RioDeEntrada.Blocks(x, w))
                    {
                        clear = true;
                        break;
                    }
                }
                if (!clear) continue;

                float h = heights[Random.Range(0, heights.Length)];

                var go = new GameObject($"Platform_{i}");
                go.transform.SetParent(propsRoot);
                var plat = go.AddComponent<BattlePlatform>();

                Color color = Random.value > 0.5f
                    ? new Color(0.35f, 0.55f, 0.3f)
                    : new Color(0.45f, 0.38f, 0.28f);

                plat.Init(new Vector3(x, floorY + h, 0f), new Vector2(w, 0.45f), color);
                platforms.Add(plat);
                MonteDressing.Dress(plat);
            }

            // Plataforma central alta para saltos largos
            var center = CreatePlatform("Platform_Center", new Vector3(0f, floorY + 3.2f, 0f),
                new Vector2(4f, 0.5f), new Color(0.25f, 0.6f, 0.45f));
            platforms.Add(center);
        }

        BattlePlatform CreatePlatform(string name, Vector3 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(propsRoot);
            var p = go.AddComponent<BattlePlatform>();
            p.Init(pos, size, color);
            MonteDressing.Dress(p);
            return p;
        }

        void SpawnCollectibles(float floorY, float minX, float maxX)
        {
            var types = new[] { CollectibleType.SpiritOrb, CollectibleType.CocaLeaf, CollectibleType.GoldenFlower };

            for (int i = 0; i < collectibleCount; i++)
            {
                float x = Random.Range(minX + 1f, maxX - 1f);
                float y = floorY + Random.Range(0.8f, 5f);
                var type = types[i % types.Length];

                var go = new GameObject($"Collectible_{i}");
                go.transform.SetParent(propsRoot);
                go.AddComponent<BattleCollectible>().Init(type, new Vector3(x, y, 0f));
            }
        }
    }
}