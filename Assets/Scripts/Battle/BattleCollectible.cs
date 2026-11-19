using UnityEngine;

namespace AYNI.Battle
{
    public enum CollectibleType { SpiritOrb, CocaLeaf, GoldenFlower }

    public class BattleCollectible : MonoBehaviour
    {
        [SerializeField] CollectibleType type;
        [SerializeField] Vector3 startPos;
        [SerializeField] float bobSpeed = 2f;
        [SerializeField] float bobAmount = 0.15f;
        bool collected;
        bool cuenta;

        public CollectibleType Type => type;

        SpriteRenderer sr;
        LagunaBattleManager battle;

        void Awake()
        {
            // La posición del Transform manda: así se respeta cualquier
            // movimiento hecho a mano en el editor.
            startPos = transform.position;

            battle = FindFirstObjectByType<LagunaBattleManager>();
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            // Objetos horneados: el sprite ya viene serializado.
            // Objetos creados por código: se reparan aquí.
            if (sr == null || sr.sprite == null) EnsureVisual();
        }

        public void Init(CollectibleType newType, Vector3 pos, bool suma = false)
        {
            type = newType;
            cuenta = suma;
            startPos = pos;
            transform.position = pos;
            battle = FindFirstObjectByType<LagunaBattleManager>();
            EnsureVisual();
        }

        void EnsureVisual()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = CreateOrbSprite();
            sr.sortingOrder = 8;
            transform.localScale = Vector3.one * 0.35f;
            sr.color = ColorFor(type);
        }

        static Color ColorFor(CollectibleType value) => value switch
        {
            CollectibleType.SpiritOrb => new Color(0.4f, 0.9f, 1f),
            CollectibleType.CocaLeaf => new Color(0.3f, 0.75f, 0.25f),
            CollectibleType.GoldenFlower => new Color(1f, 0.85f, 0.2f),
            _ => Color.white
        };

        /// <summary>
        /// Al editar la escena, mantiene el punto de origen y el color
        /// sincronizados con lo que ves en el editor.
        /// </summary>
        void OnValidate()
        {
            if (Application.isPlaying) return;

            startPos = transform.position;
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = ColorFor(type);
        }

        void Update()
        {
            if (collected) return;
            float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.position = new Vector3(startPos.x, y, startPos.z);
            transform.Rotate(0f, 0f, 40f * Time.deltaTime);
        }

        public bool TryCollect(Vector3 playerPos)
        {
            if (collected) return false;
            if (Vector3.Distance(playerPos, transform.position) > 1.2f) return false;

            collected = true;
            battle?.OnCollectibleGathered(Type, cuenta);
            Destroy(gameObject);
            return true;
        }

        static Sprite CreateOrbSprite()
        {
            const int s = 24;
            var tex = new Texture2D(s, s);
            var c = new Vector2(s / 2f, s / 2f);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, d < s * 0.4f ? Color.white : Color.clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }
    }
}