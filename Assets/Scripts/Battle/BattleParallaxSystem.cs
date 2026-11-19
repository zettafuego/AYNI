using System.Collections.Generic;
using UnityEngine;

namespace AYNI.Battle
{
    public class BattleParallaxSystem : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] float floorY = -2.5f;
        [SerializeField] int tileCount = 9;

        [Header("Background — CIELO (parte de arriba)")]
        [Tooltip("Imagen del cielo/montañas lejanas")]
        [SerializeField] string skyImage = "Battle/fondos/Backgrond";
        [Tooltip("Altura del centro del cielo. Más alto = más arriba en pantalla")]
        [SerializeField] float skyCenterY = 4f;
        [Tooltip("Qué tan grande es el cielo. Más = cubre más pantalla")]
        [SerializeField] float skyHeight = 9f;
        [Tooltip("Velocidad al caminar. Más bajo = más lejos")]
        [SerializeField] float skyParallax = 0.08f;

        [Header("Middleground — CENTRO de pantalla")]
        [SerializeField] string mgImageA = "Battle/fondos/mideground_nuevo";
        [SerializeField] string mgImageB = "Battle/fondos/mideground1";
        [SerializeField] string mgImageC = "Battle/fondos/mideground2";
        [Tooltip("0 = centro de la pantalla. Sube/baja el bosque")]
        [SerializeField] float mgCenterY = 0f;
        [SerializeField] float mgHeight = 7.6f;
        [SerializeField] float mgParallax = 0.4f;

        [Header("Foreground — parte de abajo (arbustos)")]
        [SerializeField] string fgImage = "Battle/fondos/foreground22";
        [SerializeField] float fgCenterY = -1.2f;
        [SerializeField] float fgHeight = 4f;
        [SerializeField] float fgParallax = 0.9f;
        [SerializeField] float fgAlpha = 0.9f;

        [Header("Foreground suelo — muy abajo")]
        [SerializeField] string fgFloorImage = "Battle/fondos/foreground";
        [SerializeField] float fgFloorCenterY = -2.1f;
        [SerializeField] float fgFloorHeight = 2f;
        [SerializeField] float fgFloorParallax = 1.1f;
        [SerializeField] float fgFloorAlpha = 0.75f;

        readonly List<LayerGroup> groups = new();

        struct LayerGroup
        {
            public Transform root;
            public float factor;
            public float segmentWidth;
            public Transform[] segments;
        }

        public ParallaxLayer[] Layers => GetComponentsInChildren<ParallaxLayer>(true);

        public void Build(float floor, float halfWidth)
        {
            floorY = floor;
            // La escena guarda 9 losas. El cielo es la tira corta, y en el sendero el scroll se queda en x 28
            // mientras la cámara sigue hasta x 112. 16 losas cubren ese tramo.
            tileCount = 16;
            // La escena guarda mg 5, fg 3 y suelo 1.5. El bosque pintado sale más alto desde aquí.
            mgHeight = 7.6f;
            fgHeight = 4f;
            fgFloorHeight = 2f;
            ClearOldLayers();

            var root = new GameObject("ParallaxRoot");
            root.transform.SetParent(transform);

            // Cielo arriba
            BuildTiledLayer(root.transform, "Background", skyImage,
                skyCenterY, skyHeight, 0, skyParallax, 1f);

            // Bosque en el centro
            BuildTiledLayer(root.transform, "Middleground_A", mgImageA,
                mgCenterY, mgHeight, 1, mgParallax, 1f);
            BuildTiledLayer(root.transform, "Middleground_B", mgImageB,
                mgCenterY - 0.15f, mgHeight * 0.95f, 2, mgParallax + 0.1f, 0.95f);
            BuildTiledLayer(root.transform, "Middleground_C", mgImageC,
                mgCenterY + 0.1f, mgHeight * 0.9f, 3, mgParallax + 0.15f, 0.9f);

            // Arbustos y suelo abajo
            BuildTiledLayer(root.transform, "Foreground", fgImage,
                fgCenterY, fgHeight, 4, fgParallax, fgAlpha);
            BuildTiledLayer(root.transform, "ForegroundFloor", fgFloorImage,
                fgFloorCenterY, fgFloorHeight, 3, fgFloorParallax, fgFloorAlpha);

            SetCameraSkyColor();
        }

        void SetCameraSkyColor()
        {
            var cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = new Color(0.45f, 0.65f, 0.9f);
        }

        void ClearOldLayers()
        {
            foreach (var name in new[] { "Background", "Middleground", "Foreground", "ForegroundFloor", "ParallaxRoot" })
            {
                var old = GameObject.Find(name);
                if (old != null) Destroy(old);
            }
            groups.Clear();
        }

        void BuildTiledLayer(Transform parent, string layerName, string resourcePath,
            float centerY, float targetHeight, int sortOrder, float parallaxFactor, float alpha)
        {
            var sprite = BattleRuntimeAssets.LoadBackground(resourcePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[AYNI] No se encontró fondo: {resourcePath}");
                return;
            }

            float spriteW = sprite.bounds.size.x;
            float spriteH = sprite.bounds.size.y;
            float scale = targetHeight / spriteH;
            float segmentW = spriteW * scale;

            var groupRoot = new GameObject(layerName);
            groupRoot.transform.SetParent(parent);

            var segments = new Transform[tileCount];
            float startX = -((tileCount - 1) * segmentW) * 0.5f;

            for (int i = 0; i < tileCount; i++)
            {
                var seg = new GameObject($"{layerName}_{i}");
                seg.transform.SetParent(groupRoot.transform);
                seg.transform.position = new Vector3(startX + i * segmentW, centerY, sortOrder * 0.1f);
                seg.transform.localScale = new Vector3(scale, scale, 1f);

                var sr = seg.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = sortOrder;
                sr.color = new Color(1f, 1f, 1f, alpha);

                segments[i] = seg.transform;
            }

            var para = groupRoot.AddComponent<ParallaxLayer>();
            para.Configure(parallaxFactor, segmentW);

            groups.Add(new LayerGroup
            {
                root = groupRoot.transform,
                factor = parallaxFactor,
                segmentWidth = segmentW,
                segments = segments
            });
        }

        public void FollowPlayer(float playerX)
        {
            foreach (var layer in GetComponentsInChildren<ParallaxLayer>())
                layer.FollowPlayer(playerX);
        }
    }
}