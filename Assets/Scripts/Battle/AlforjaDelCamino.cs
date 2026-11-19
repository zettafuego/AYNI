using System.Collections.Generic;
using AYNI.Narrative;
using AYNI.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// Lo que te regalan va a la alforja. I o el botón la abre como menú.
    /// </summary>
    public class AlforjaDelCamino : MonoBehaviour
    {
        public struct Cosa
        {
            public string Id;
            public string Nombre;
            public string Nota;
            public int Cuenta;
        }

        static AlforjaDelCamino viva;
        static readonly List<Cosa> cosas = new List<Cosa>();

        GameObject panel;
        Transform rejilla;
        Text detalle;
        Text vacio;
        Text badge;
        Button[] huecos = new Button[0];
        int elegido = -1;

        public static bool Abierta { get; private set; }

        public static int Cuenta
        {
            get
            {
                int n = 0;
                for (int i = 0; i < cosas.Count; i++) n += cosas[i].Cuenta;
                return n;
            }
        }

        public static void Preparar(Transform hud)
        {
            if (hud == null) return;
            if (hud.Find("AlforjaMenu") != null) return;
            var host = new GameObject("AlforjaMenu");
            host.transform.SetParent(hud, false);
            var rect = host.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            host.AddComponent<AlforjaDelCamino>().Armar(hud);
        }

        public static void Guardar(string id, string nombre, string nota, int n = 1)
        {
            if (string.IsNullOrEmpty(id) || n <= 0) return;
            for (int i = 0; i < cosas.Count; i++)
            {
                if (cosas[i].Id != id) continue;
                var c = cosas[i];
                c.Cuenta += n;
                cosas[i] = c;
                viva?.Pintar();
                return;
            }

            cosas.Add(new Cosa { Id = id, Nombre = nombre, Nota = nota, Cuenta = n });
            viva?.Pintar();
        }

        public static void Quitar(string id, int n = 1)
        {
            for (int i = 0; i < cosas.Count; i++)
            {
                if (cosas[i].Id != id) continue;
                var c = cosas[i];
                c.Cuenta -= n;
                if (c.Cuenta <= 0) cosas.RemoveAt(i);
                else cosas[i] = c;
                viva?.Pintar();
                return;
            }
        }

        public static bool ConsumeEscape()
        {
            if (!Abierta) return false;
            Cerrar();
            return true;
        }

        public static void Alternar()
        {
            if (Abierta) Cerrar();
            else Abrir();
        }

        public static void Abrir()
        {
            if (viva == null || viva.panel == null) return;
            Abierta = true;
            viva.elegido = cosas.Count > 0 ? 0 : -1;
            viva.panel.SetActive(true);
            viva.Pintar();
            CamaraTercera.ShowCursor();
        }

        public static void Cerrar()
        {
            Abierta = false;
            if (viva != null && viva.panel != null) viva.panel.SetActive(false);
        }

        void Armar(Transform hud)
        {
            viva = this;
            var boton = Storybook.IconButton(hud, "Alforja", Storybook.Marca.Alforja,
                new Vector2(0.100f, 0.83f), new Vector2(0.174f, 0.96f), Alternar);
            Storybook.Pista(boton, "Alforja  ·  I");
            badge = Badge(boton.transform);

            var cuerpo = new GameObject("Cuerpo", typeof(RectTransform));
            cuerpo.transform.SetParent(transform, false);
            var cr = (RectTransform)cuerpo.transform;
            cr.anchorMin = Vector2.zero;
            cr.anchorMax = Vector2.one;
            cr.offsetMin = Vector2.zero;
            cr.offsetMax = Vector2.zero;

            var velo = Storybook.Plate(cuerpo.transform, "Velo", Vector2.zero, Vector2.one,
                new Color(0.04f, 0.03f, 0.02f, 0.72f));
            velo.raycastTarget = true;
            var cerrarVelo = velo.gameObject.AddComponent<Button>();
            cerrarVelo.navigation = new Navigation { mode = Navigation.Mode.None };
            cerrarVelo.onClick.AddListener(Cerrar);

            var papel = Storybook.Plate(cuerpo.transform, "Papel",
                new Vector2(0.22f, 0.16f), new Vector2(0.78f, 0.86f),
                new Color(0.96f, 0.91f, 0.78f, 0.98f));
            papel.raycastTarget = true;

            var titulo = Texto(papel.transform, "Titulo", new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.97f),
                LeyDelMonte.AlforjaTitulo, 34, TextAnchor.MiddleCenter);
            Storybook.Stamp(titulo);

            vacio = Texto(papel.transform, "Vacio", new Vector2(0.10f, 0.42f), new Vector2(0.90f, 0.78f),
                LeyDelMonte.AlforjaVacia, 22, TextAnchor.MiddleCenter);

            var caja = new GameObject("Rejilla", typeof(RectTransform));
            caja.transform.SetParent(papel.transform, false);
            var rr = (RectTransform)caja.transform;
            rr.anchorMin = new Vector2(0.08f, 0.34f);
            rr.anchorMax = new Vector2(0.92f, 0.84f);
            rr.offsetMin = Vector2.zero;
            rr.offsetMax = Vector2.zero;
            rejilla = caja.transform;

            detalle = Texto(papel.transform, "Detalle", new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.32f),
                LeyDelMonte.AlforjaPista, 20, TextAnchor.UpperLeft);

            Storybook.IconButton(papel.transform, "Cerrar", Storybook.Marca.Volver,
                new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.12f), Cerrar);
            Storybook.Pista(papel.transform.Find("Cerrar")?.GetComponent<Button>(), "Cerrar  ·  ESC");

            panel = cuerpo;
            panel.SetActive(false);
            Pintar();
        }

        void OnEnable() => viva = this;

        void OnDisable()
        {
            if (viva == this)
            {
                viva = null;
                Abierta = false;
            }
        }

        void Pintar()
        {
            if (badge != null) badge.text = Cuenta > 0 ? Cuenta.ToString() : "";
            if (rejilla == null) return;
            for (int i = rejilla.childCount - 1; i >= 0; i--)
                Destroy(rejilla.GetChild(i).gameObject);

            if (vacio != null) vacio.gameObject.SetActive(cosas.Count == 0);
            if (cosas.Count == 0)
            {
                if (detalle != null) detalle.text = LeyDelMonte.AlforjaPista;
                huecos = new Button[0];
                elegido = -1;
                return;
            }

            if (elegido < 0 || elegido >= cosas.Count) elegido = 0;
            huecos = new Button[cosas.Count];
            int cols = 3;
            for (int i = 0; i < cosas.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float x0 = col / 3f + 0.02f;
                float x1 = (col + 1) / 3f - 0.02f;
                float y1 = 1f - row * 0.46f - 0.04f;
                float y0 = y1 - 0.40f;
                int cual = i;
                var boton = Storybook.IconButton(rejilla, "Cosa" + i, IconoDe(cosas[i].Id),
                    new Vector2(x0, y0), new Vector2(x1, y1), () => Elegir(cual));
                var cuenta = Texto(boton.transform, "N", new Vector2(0.55f, 0.02f), new Vector2(0.96f, 0.34f),
                    "×" + cosas[i].Cuenta, 18, TextAnchor.MiddleRight);
                cuenta.color = new Color(0.16f, 0.10f, 0.05f);
                var nombre = Texto(boton.transform, "Nom", new Vector2(0.06f, 0.02f), new Vector2(0.62f, 0.34f),
                    cosas[i].Nombre, 14, TextAnchor.MiddleLeft);
                nombre.color = new Color(0.16f, 0.10f, 0.05f);
                huecos[i] = boton;
            }

            Marcar();
        }

        void Elegir(int i)
        {
            if (i < 0 || i >= cosas.Count) return;
            elegido = i;
            Marcar();
        }

        void Marcar()
        {
            for (int i = 0; i < huecos.Length; i++)
            {
                if (huecos[i] == null) continue;
                Storybook.LeafColors(huecos[i], i == elegido);
                var motion = huecos[i].GetComponent<MenuButtonMotion>();
                if (motion != null) motion.Chosen = i == elegido;
            }

            if (detalle == null) return;
            if (elegido < 0 || elegido >= cosas.Count)
            {
                detalle.text = LeyDelMonte.AlforjaPista;
                return;
            }

            var c = cosas[elegido];
            detalle.text = c.Nombre + "  ×" + c.Cuenta + "\n" + c.Nota;
        }

        static Storybook.Marca IconoDe(string id)
        {
            if (id == "flor" || id == "flor-cerro") return Storybook.Marca.Flor;
            if (id == "fruta") return Storybook.Marca.Aura;
            if (id == "pan") return Storybook.Marca.Atractivo;
            if (id == "ofrenda") return Storybook.Marca.Historia;
            if (id == "vela") return Storybook.Marca.Farol;
            return Storybook.Marca.Alforja;
        }

        static Text Texto(Transform parent, string name, Vector2 min, Vector2 max, string linea, int size, TextAnchor ancla)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<Text>();
            Storybook.DressText(text, linea, Storybook.DisplayFont, size, new Color(0.16f, 0.09f, 0.04f), FontStyle.Bold);
            text.alignment = ancla;
            text.raycastTarget = false;
            return text;
        }

        static Text Badge(Transform boton)
        {
            var go = new GameObject("Badge");
            go.transform.SetParent(boton, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.58f, 0.58f);
            rect.anchorMax = new Vector2(0.96f, 0.96f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<Text>();
            Storybook.DressText(text, "", Storybook.DisplayFont, 16, Color.white, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }
    }
}
