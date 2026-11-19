using AYNI.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// La misma fila en calles, bosque, valle y montaña.
    /// Planta e inclinación abajo a la izquierda. Cerca y lejos a la derecha, encima de Volver.
    /// Volar solo se crea en las calles.
    /// </summary>
    public class BotonesDeMovimiento
    {
        public const float OficiosDesde = 0.49f;

        public Button Planta;
        public Button Inclinacion;
        public Button Cerca;
        public Button Lejos;
        public Button Volar;

        public static BotonesDeMovimiento Armar(
            Transform parent,
            UnityAction planta,
            UnityAction inclinacion,
            UnityAction cerca,
            UnityAction lejos,
            UnityAction volar = null)
        {
            var fila = new BotonesDeMovimiento();
            if (parent == null) return fila;

            fila.Planta = Storybook.IconButton(parent, "Planta", Storybook.Marca.Planta,
                new Vector2(0.018f, 0.02f), new Vector2(0.092f, 0.15f), planta);
            fila.Inclinacion = Storybook.IconButton(parent, "Inclinacion", Storybook.Marca.Inclinacion,
                new Vector2(0.100f, 0.02f), new Vector2(0.174f, 0.15f), inclinacion);
            fila.Cerca = Storybook.IconButton(parent, "Cerca", Storybook.Marca.Cerca,
                new Vector2(0.908f, 0.18f), new Vector2(0.982f, 0.31f), cerca);
            fila.Lejos = Storybook.IconButton(parent, "Lejos", Storybook.Marca.Lejos,
                new Vector2(0.908f, 0.33f), new Vector2(0.982f, 0.46f), lejos);
            if (volar != null)
            {
                fila.Volar = Storybook.IconButton(parent, "Volar", Storybook.Marca.Volar,
                    new Vector2(0.182f, 0.02f), new Vector2(0.256f, 0.15f), volar);
            }

            return fila;
        }

        public void Marcar(bool planta, bool inclinacion, bool volar = false)
        {
            Pintar(Planta, planta);
            Pintar(Inclinacion, inclinacion);
            Pintar(Volar, volar);
        }

        public static void LeerMapa(
            Camera cam,
            Transform space,
            ref float size,
            float min,
            float max,
            ref bool suelto,
            ref bool arrastre,
            ref Vector2 mira,
            ref Vector3 cursor,
            Vector2 heroe)
        {
            float wheel = Input.mouseScrollDelta.y;
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
                wheel += 6.4f * Time.deltaTime;
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
                wheel -= 6.4f * Time.deltaTime;
            if (wheel > 0.01f) size = Mathf.Max(min, size * (1f - 0.18f * Mathf.Min(wheel, 3f)));
            if (wheel < -0.01f) size = Mathf.Min(max, size / (1f - 0.18f * Mathf.Min(-wheel, 3f)));

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(1) && !overUi)
            {
                if (!suelto) mira = heroe;
                suelto = true;
                arrastre = true;
                cursor = Input.mousePosition;
            }

            if (!Input.GetMouseButton(1)) arrastre = false;
            if (!arrastre || cam == null || space == null) return;

            Vector3 now = Input.mousePosition;
            Vector3 delta = now - cursor;
            cursor = now;
            Vector3 right = cam.transform.right;
            Vector3 up = cam.transform.up;
            right.y = 0f;
            up.y = 0f;
            if (right.sqrMagnitude < 0.001f || up.sqrMagnitude < 0.001f) return;
            right.Normalize();
            up.Normalize();
            float units = (size * 2f) / Mathf.Max(1f, Screen.height);
            Vector3 move = space.InverseTransformDirection((-delta.x * right - delta.y * up) * units);
            mira.x += move.x;
            mira.y += move.z;
        }

        static void Pintar(Button button, bool on)
        {
            if (button == null) return;
            Storybook.LeafColors(button, on);
            var motion = button.GetComponent<MenuButtonMotion>();
            if (motion != null) motion.Chosen = on;
        }
    }
}
