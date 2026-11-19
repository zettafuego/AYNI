using UnityEngine;

namespace AYNI.Battle
{
    public class BattleCameraFollow : MonoBehaviour
    {
        [SerializeField] float smoothX = 5f;
        [SerializeField] float smoothY = 3f;
        [SerializeField] float yOffset = 1.8f;
        [SerializeField] float zOffset = -10f;
        [SerializeField] float maxYOffset = 2.5f;

        Transform target;
        LagunaBattleManager battle;
        Camera cam;
        bool lookClose;
        float lookX;
        float lookY;
        float lookSize = 6f;

        public void Focus(float x, float y, float size)
        {
            lookClose = true;
            lookX = x;
            lookY = y;
            lookSize = size;
        }

        public void Release()
        {
            lookClose = false;
            lookSize = 6f;
        }

        void Start()
        {
            battle = FindFirstObjectByType<LagunaBattleManager>();
            var player = FindFirstObjectByType<VizcachaFighter>();
            if (player != null) target = player.transform;
            cam = GetComponent<Camera>();
            if (cam != null)
                cam.orthographicSize = 6f;
        }

        void LateUpdate()
        {
            if (target == null || battle == null || !battle.IsPlaying) return;

            if (cam != null)
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, lookClose ? lookSize : 6f, 5f * Time.deltaTime);

            float desiredX;
            float targetY;
            if (lookClose)
            {
                desiredX = lookX;
                targetY = lookY;
            }
            else
            {
                float halfView = cam != null ? cam.orthographicSize * cam.aspect : 12f;
                float minCam = battle.ArenaMinX + halfView;
                float maxCam = battle.ArenaMaxX - halfView;
                if (minCam > maxCam)
                    minCam = maxCam = (battle.ArenaMinX + battle.ArenaMaxX) * 0.5f;
                desiredX = Mathf.Clamp(target.position.x, minCam, maxCam);
                float jumpBoost = Mathf.Clamp((target.position.y - battle.FloorY) * 0.35f, 0f, maxYOffset);
                targetY = battle.FloorY + yOffset + jumpBoost;
            }

            var pos = transform.position;
            pos.x = Mathf.Lerp(pos.x, desiredX, smoothX * Time.deltaTime);
            pos.y = Mathf.Lerp(pos.y, targetY, smoothY * Time.deltaTime);
            pos.z = zOffset;
            transform.position = pos;
        }
    }
}