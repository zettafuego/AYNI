using UnityEngine;
using UnityEngine.EventSystems;

namespace AYNI.Battle
{
    /// <summary>
    /// Third person behind the hero, like Roblox. Hold the right mouse button
    /// and the cursor locks so the view orbits the body. The wheel zooms.
    /// W walks away from the lens. Q and E turn that same circle.
    /// The map has its own yaw and can spin a full turn while the lens stays overhead.
    /// </summary>
    public class CamaraTercera
    {
        public static float VistaYaw;
        public static bool UseYaw;

        /// <summary>
        /// Plan yaw in degrees. It is not clamped, so the map can turn forever.
        /// At 0 the screen-up is world +Z and the screen-right is world +X.
        /// </summary>
        public static float PlanYaw;

        public float Yaw;
        public float Pitch = 22f;
        public float Distance = 12f;
        public float MinDistance = 6f;
        public float MaxDistance = 28f;
        public float PivotHeight = 2.2f;

        bool dragging;
        bool middle;
        static bool planSpin;
        static bool pedirNorte;

        public static void Release()
        {
            UseYaw = false;
            ShowCursor();
        }

        public static void Norte()
        {
            PlanYaw = 0f;
            pedirNorte = true;
        }

        public static bool TakeNorth()
        {
            if (!pedirNorte) return false;
            pedirNorte = false;
            return true;
        }

        /// <summary>
        /// Q turns the map left and E turns it right. The middle button drags
        /// the same circle and leaves the cursor free. The right button stays a pan.
        /// </summary>
        public static void ReadPlanTurn()
        {
            if (Input.GetKey(KeyCode.Q)) PlanYaw -= 90f * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) PlanYaw += 90f * Time.deltaTime;

            if (Input.GetMouseButtonDown(2))
                planSpin = !OverUi();
            if (!Input.GetMouseButton(2))
                planSpin = false;
            if (planSpin)
                PlanYaw += Input.GetAxis("Mouse X") * 4.8f;
        }

        public void ReadMouse()
        {
            if (Input.GetKey(KeyCode.Q)) Yaw -= 90f * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) Yaw += 90f * Time.deltaTime;

            if (Input.GetMouseButtonDown(1))
                dragging = !OverUi();
            if (!Input.GetMouseButton(1))
                dragging = false;

            if (Input.GetMouseButtonDown(2))
                middle = !OverUi();
            if (!Input.GetMouseButton(2))
                middle = false;

            if (dragging)
            {
                // Como en Roblox: el cursor se queda fijo y la vista da la vuelta
                // al personaje mientras se mantiene el clic derecho.
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Yaw += Input.GetAxis("Mouse X") * 4.8f;
                Pitch -= Input.GetAxis("Mouse Y") * 3.4f;
                Pitch = Mathf.Clamp(Pitch, 6f, 78f);
            }
            else if (middle)
            {
                ShowCursor();
                Yaw += Input.GetAxis("Mouse X") * 4.8f;
                Pitch -= Input.GetAxis("Mouse Y") * 3.4f;
                Pitch = Mathf.Clamp(Pitch, 6f, 78f);
            }
            else
                ShowCursor();

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f && !OverUi())
                Distance = Mathf.Clamp(Distance - wheel * 1.6f, MinDistance, MaxDistance);

            Publish();
        }

        public static void ShowCursor()
        {
            if (Cursor.lockState == CursorLockMode.None && Cursor.visible) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Pose(Vector3 feet, out Vector3 position, out Quaternion rotation)
        {
            Publish();
            Vector3 pivot = feet + Vector3.up * PivotHeight;
            Quaternion spin = Quaternion.Euler(Pitch, Yaw, 0f);
            position = pivot + spin * new Vector3(0f, 0f, -Distance);
            Vector3 to = pivot - position;
            rotation = to.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(to, Vector3.up)
                : spin;
        }

        public Vector3 Wish()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 move = Quaternion.Euler(0f, Yaw, 0f) * new Vector3(h, 0f, v);
            move.y = 0f;
            if (move.sqrMagnitude > 1f) move.Normalize();
            return move;
        }

        /// <summary>
        /// Walk with W A S D. The arrow keys are kept for the camera.
        /// </summary>
        public Vector3 Wasd()
        {
            float h = 0f;
            float v = 0f;
            if (Input.GetKey(KeyCode.A)) h -= 1f;
            if (Input.GetKey(KeyCode.D)) h += 1f;
            if (Input.GetKey(KeyCode.S)) v -= 1f;
            if (Input.GetKey(KeyCode.W)) v += 1f;
            Vector3 move = Quaternion.Euler(0f, Yaw, 0f) * new Vector3(h, 0f, v);
            move.y = 0f;
            if (move.sqrMagnitude > 1f) move.Normalize();
            return move;
        }

        public void ReadArrows()
        {
            float giro = 110f * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftArrow)) Yaw -= giro;
            if (Input.GetKey(KeyCode.RightArrow)) Yaw += giro;
            if (Input.GetKey(KeyCode.UpArrow)) Pitch -= giro * 0.7f;
            if (Input.GetKey(KeyCode.DownArrow)) Pitch += giro * 0.7f;
            Pitch = Mathf.Clamp(Pitch, -24f, 75f);
            Publish();
        }

        /// <summary>
        /// Walk the way the plan is drawn. The camera uses Euler(90, PlanYaw, 0),
        /// so this yaw is the same turn: 0 keeps W on +Z and D on +X,
        /// and 90 puts W on +X and D on -Z.
        /// </summary>
        public Vector3 PlanLocal(Transform space)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 world = Quaternion.Euler(0f, PlanYaw, 0f) * new Vector3(h, 0f, v);
            world.y = 0f;
            if (world.sqrMagnitude > 1f) world.Normalize();
            return ToLocal(space, world);
        }

        public Vector3 FlatForward()
        {
            Vector3 forward = Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        public Vector3 ToLocal(Transform space, Vector3 worldDir)
        {
            if (space == null) return worldDir;
            Vector3 local = space.InverseTransformDirection(worldDir);
            local.y = 0f;
            if (local.sqrMagnitude > 1f) local.Normalize();
            return local;
        }

        public static Vector3 WorldDir(Transform space, Vector3 localDir)
        {
            if (space == null) return localDir;
            Vector3 world = space.TransformDirection(localDir);
            world.y = 0f;
            return world.sqrMagnitude > 0.0001f ? world.normalized : localDir;
        }

        void Publish()
        {
            VistaYaw = Yaw;
            UseYaw = true;
        }

        static bool OverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
