using UnityEngine;

namespace AYNI.Battle
{
    public static class LeafEffect
    {
        public static void Spawn(Vector3 position, bool driftRight)
        {
            var go = new GameObject("Hoja");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateLeafSprite();
            sr.color = new Color(0.3f, 0.8f, 0.3f);
            sr.sortingOrder = 50;
            go.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.5f), 0f);
            go.transform.localScale = Vector3.one * 0.15f;
            go.AddComponent<LeafParticle>().Init(driftRight ? 1f : -1f);
        }

        static Sprite CreateLeafSprite()
        {
            const int w = 16, h = 16;
            var tex = new Texture2D(w, h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, (x + y > 4 && x < w - 2 && y < h - 2) ? Color.white : Color.clear);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }
    }

    public class LeafParticle : MonoBehaviour
    {
        float dir;
        float lifetime;
        Vector3 velocity;
        SpriteRenderer sr;

        public void Init(float direction)
        {
            dir = direction;
            lifetime = 1.5f;
            velocity = new Vector3(dir * Random.Range(2f, 4f), Random.Range(1.5f, 3f), 0f);
            sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            lifetime -= Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            velocity.y -= Time.deltaTime * 2f;
            transform.Rotate(0f, 0f, 200f * Time.deltaTime);
            if (sr != null)
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, lifetime / 1.5f);
            if (lifetime <= 0f)
                Destroy(gameObject);
        }
    }
}