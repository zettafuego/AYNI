using UnityEngine;

namespace AYNI.Battle
{
    public enum ProjectileType { Aura, Healing }

    public class BattleProjectile : MonoBehaviour
    {
        public ProjectileType Type { get; private set; }
        public float Speed { get; private set; }

        SpriteRenderer spriteRenderer;
        bool active = true;

        public void Init(ProjectileType type, float speed, Color color, float size = 0.4f)
        {
            Type = type;
            Speed = speed;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            spriteRenderer.sprite = CreateCircleSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 30;
            transform.localScale = Vector3.one * size;
        }

        void Update()
        {
            if (!active) return;
            transform.position += Vector3.right * Speed * Time.deltaTime;

            if (transform.position.x < -48f || transform.position.x > 130f)
                Destroy(gameObject);
        }

        public void Deactivate()
        {
            active = false;
            Destroy(gameObject);
        }

        static Sprite CreateCircleSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size);
            var center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}