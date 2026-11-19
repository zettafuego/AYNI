using UnityEngine;

namespace AYNI.Battle
{
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] float parallaxFactor = 0.5f;
        [SerializeField] float segmentWidth = 10f;

        Vector3 startPos;
        Transform[] segments;

        void Awake()
        {
            startPos = transform.position;
            segments = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                segments[i] = transform.GetChild(i);
        }

        public void Configure(float factor, float width)
        {
            parallaxFactor = factor;
            segmentWidth = width;
            startPos = transform.position;
            segments = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                segments[i] = transform.GetChild(i);
        }

        public void FollowPlayer(float playerX)
        {
            float offset = playerX * parallaxFactor;
            transform.position = new Vector3(startPos.x - offset, startPos.y, startPos.z);
        }
    }
}