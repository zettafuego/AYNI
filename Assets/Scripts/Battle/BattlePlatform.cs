using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Plataforma caminable del mapa.
    /// El tamaño se lee de la escala del Transform (X = ancho, Y = alto),
    /// así que se puede mover y escalar con las herramientas normales de Unity.
    /// </summary>
    public class BattlePlatform : MonoBehaviour
    {
        public float Width => Mathf.Abs(transform.localScale.x);
        public float Height => Mathf.Abs(transform.localScale.y);
        public float TopY => transform.position.y + Height * 0.5f;
        public float LeftX => transform.position.x - Width * 0.5f;
        public float RightX => transform.position.x + Width * 0.5f;

        public void Init(Vector3 pos, Vector2 size, Color color, int sortOrder = 6)
        {
            transform.position = pos;

            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlatformSprite();
            sr.color = color;
            sr.sortingOrder = sortOrder;
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        public bool IsAbove(float x, float y, float feetY)
        {
            if (x < LeftX || x > RightX) return false;
            return feetY >= TopY - 0.15f && feetY <= TopY + 0.35f && y <= TopY + 0.1f;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
            Gizmos.DrawWireCube(transform.position, new Vector3(Width, Height, 0.25f));
        }

        static Sprite sharedSprite;

        static Sprite CreatePlatformSprite()
        {
            if (sharedSprite != null) return sharedSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.name = "PlataformaTex";
            sharedSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return sharedSprite;
        }
    }
}
