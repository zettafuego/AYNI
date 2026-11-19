using AYNI.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AYNI.Battle
{
    /// <summary>
    /// Iconos a la derecha. Solo aparecen cuando la acción se puede hacer.
    /// Un toque baja o sube un tramo. La tecla sigue igual.
    /// </summary>
    public class OficiosDelCamino : MonoBehaviour
    {
        enum Paso
        {
            Patada,
            Bajar,
            Subir,
            Lampara,
            Soltar,
            Entrar,
            Golpe,
            Salir,
        }

        static readonly Paso[] Orden =
        {
            Paso.Patada,
            Paso.Bajar,
            Paso.Subir,
            Paso.Lampara,
            Paso.Soltar,
            Paso.Entrar,
            Paso.Golpe,
            Paso.Salir,
        };

        LagunaBattleManager battle;
        Button[] botones;
        Sprite[] marcas;

        public static void Preparar(LagunaBattleManager host)
        {
            if (host == null || host.GetComponent<OficiosDelCamino>() != null) return;
            host.gameObject.AddComponent<OficiosDelCamino>().Armar(host);
        }

        void Armar(LagunaBattleManager host)
        {
            battle = host;
            var canvas = host.startScreen != null ? host.startScreen.transform.parent : null;
            if (canvas == null) return;

            var placa = Storybook.Plate(canvas, "Oficios", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
            botones = new Button[Orden.Length];
            marcas = new Sprite[Orden.Length];
            for (int i = 0; i < Orden.Length; i++)
            {
                Paso paso = Orden[i];
                marcas[i] = Dibujo(paso);
                botones[i] = Storybook.IconButton(
                    placa.transform,
                    paso.ToString(),
                    marcas[i],
                    new Vector2(0.905f, 0.20f),
                    new Vector2(0.985f, 0.315f),
                    () => AlPulsar(paso));
                botones[i].gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (botones == null)
            {
                if (battle != null) Armar(battle);
                if (botones == null) return;
            }
            if (CaminoRelato.Reading)
            {
                Ocultar();
                return;
            }

            bool calle = CajamarcaCalles.IsOpen;
            bool bosque = BosqueCapitulo.IsOpen || CajamarcaRealCapitulo.IsOpen;
            bool monte = MontanaDelPerol.IsOpen;
            bool pelea = battle != null && battle.IsPlaying && !battle.IsGameOver && !calle && !bosque && !monte;
            bool caminata = calle || bosque || monte;
            bool agua = MinaDelMuqui.Ocupa || MontanaDelPerol.Ocupa;
            bool galeria = MinaDelMuqui.EnGaleria || MontanaDelPerol.EnGaleria;
            int fila = 0;
            for (int i = 0; i < Orden.Length; i++)
            {
                bool on = Visible(Orden[i], pelea, calle, monte, agua, galeria);
                var boton = botones[i];
                if (boton == null) continue;
                if (boton.gameObject.activeSelf != on) boton.gameObject.SetActive(on);
                if (!on) continue;
                float y0 = (caminata ? BotonesDeMovimiento.OficiosDesde : 0.18f) + fila * 0.105f;
                var rect = boton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.905f, y0);
                rect.anchorMax = new Vector2(0.985f, y0 + 0.095f);
                fila++;
                bool oro = Orden[i] == Paso.Lampara && (monte ? MontanaDelPerol.Encendida : MinaDelMuqui.Encendida);
                Storybook.LeafColors(boton, oro);
                var motion = boton.GetComponent<MenuButtonMotion>();
                if (motion != null) motion.Chosen = oro;
            }
        }

        void Ocultar()
        {
            for (int i = 0; i < botones.Length; i++)
                if (botones[i] != null && botones[i].gameObject.activeSelf)
                    botones[i].gameObject.SetActive(false);
        }

        bool Visible(Paso paso, bool pelea, bool calle, bool monte, bool agua, bool galeria)
        {
            bool junto = MinaDelMuqui.Junto || MontanaDelPerol.Junto;
            bool carga = MinaDelMuqui.CargaOro || MontanaDelPerol.CargaOro;
            bool sitio = pelea || monte;
            switch (paso)
            {
                case Paso.Lampara:
                    return sitio && (junto || agua || carga);
                case Paso.Bajar:
                    return sitio && !galeria && (junto || agua);
                case Paso.Subir:
                    return sitio && agua;
                case Paso.Soltar:
                    return carga && (sitio || agua);
                case Paso.Entrar:
                    return calle && TiendasDelCamino.JuntoAPuerta && !TiendasDelCamino.Adentro;
                case Paso.Golpe:
                    return calle && TiendasDelCamino.Adentro && TiendasDelCamino.HayQuePelear;
                case Paso.Salir:
                    return calle && TiendasDelCamino.Adentro;
                case Paso.Patada:
                    return (pelea && !agua && RivalCerca()) || (monte && MontanaDelPerol.JuntoAlToro);
                default:
                    return false;
            }
        }

        bool RivalCerca()
        {
            var hero = battle != null ? battle.Player : null;
            if (hero == null) return false;
            var foe = ToroEnemyAI.Cercano(hero.PositionX);
            return foe != null && Mathf.Abs(foe.PositionX - hero.PositionX) < 8f;
        }

        void AlPulsar(Paso paso)
        {
            if (MontanaDelPerol.IsOpen)
            {
                switch (paso)
                {
                    case Paso.Patada: MontanaDelPerol.EmpezarPelea(); return;
                    case Paso.Bajar: MontanaDelPerol.PulsarBajar(); return;
                    case Paso.Subir: MontanaDelPerol.PulsarSubir(); return;
                    case Paso.Lampara: MontanaDelPerol.PulsarLampara(); return;
                    case Paso.Soltar: MontanaDelPerol.PulsarSoltar(); return;
                }
            }

            switch (paso)
            {
                case Paso.Patada:
                    battle?.Player?.CastGolpe(0);
                    break;
                case Paso.Bajar:
                    MinaDelMuqui.PulsarBajar();
                    break;
                case Paso.Subir:
                    MinaDelMuqui.PulsarSubir();
                    break;
                case Paso.Lampara:
                    MinaDelMuqui.PulsarLampara();
                    break;
                case Paso.Soltar:
                    MinaDelMuqui.PulsarSoltar();
                    break;
                case Paso.Entrar:
                    TiendasDelCamino.PulsarEntrar();
                    break;
                case Paso.Golpe:
                    TiendasDelCamino.PulsarGolpe();
                    break;
                case Paso.Salir:
                    TiendasDelCamino.PulsarSalir();
                    break;
            }
        }

        static Sprite Dibujo(Paso paso)
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            switch (paso)
            {
                case Paso.Lampara: PintarLampara(px, n); break;
                case Paso.Bajar: PintarFlecha(px, n, false); break;
                case Paso.Subir: PintarFlecha(px, n, true); break;
                case Paso.Soltar: PintarSoltar(px, n); break;
                case Paso.Entrar: PintarPuerta(px, n); break;
                case Paso.Golpe: PintarGolpe(px, n); break;
                case Paso.Salir: PintarSalir(px, n); break;
                default: PintarPatada(px, n); break;
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static readonly Color32 Tinta = new Color32(255, 255, 255, 255);

        static void Punto(Color32[] px, int n, int x, int y)
        {
            if ((uint)x >= (uint)n || (uint)y >= (uint)n) return;
            px[y * n + x] = Tinta;
        }

        static void Disco(Color32[] px, int n, int x, int y, int radio)
        {
            int r2 = radio * radio;
            for (int dy = -radio; dy <= radio; dy++)
            for (int dx = -radio; dx <= radio; dx++)
                if (dx * dx + dy * dy <= r2) Punto(px, n, x + dx, y + dy);
        }

        static void Trazo(Color32[] px, int n, int x0, int y0, int x1, int y1, int radio)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps < 1) steps = 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Disco(px, n,
                    Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)),
                    Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)),
                    radio);
            }
        }

        static void PintarLampara(Color32[] px, int n)
        {
            Trazo(px, n, 24, 50, 40, 50, 2);
            Trazo(px, n, 32, 50, 32, 44, 2);
            Trazo(px, n, 22, 28, 22, 42, 2);
            Trazo(px, n, 42, 28, 42, 42, 2);
            Trazo(px, n, 22, 28, 42, 28, 2);
            Trazo(px, n, 22, 42, 42, 42, 2);
            Disco(px, n, 32, 34, 4);
            Trazo(px, n, 26, 18, 38, 18, 2);
            Trazo(px, n, 29, 18, 29, 28, 2);
            Trazo(px, n, 35, 18, 35, 28, 2);
        }

        static void PintarFlecha(Color32[] px, int n, bool arriba)
        {
            int y0 = arriba ? 14 : 50;
            int y1 = arriba ? 50 : 14;
            int ala = arriba ? 34 : 30;
            Trazo(px, n, 32, y0, 32, y1, 3);
            Trazo(px, n, 32, y1, 16, ala, 3);
            Trazo(px, n, 32, y1, 48, ala, 3);
        }

        static void PintarSoltar(Color32[] px, int n)
        {
            Disco(px, n, 32, 46, 7);
            Trazo(px, n, 32, 34, 32, 16, 3);
            Trazo(px, n, 32, 16, 20, 28, 3);
            Trazo(px, n, 32, 16, 44, 28, 3);
        }

        static void PintarPuerta(Color32[] px, int n)
        {
            Trazo(px, n, 18, 12, 18, 42, 2);
            Trazo(px, n, 46, 12, 46, 42, 2);
            Trazo(px, n, 18, 12, 46, 12, 2);
            Trazo(px, n, 18, 42, 32, 54, 2);
            Trazo(px, n, 46, 42, 32, 54, 2);
            Disco(px, n, 38, 28, 3);
        }

        static void PintarGolpe(Color32[] px, int n)
        {
            Trazo(px, n, 14, 16, 34, 36, 3);
            Trazo(px, n, 18, 40, 36, 22, 3);
            Trazo(px, n, 30, 48, 50, 28, 3);
        }

        static void PintarSalir(Color32[] px, int n)
        {
            Trazo(px, n, 16, 32, 48, 32, 3);
            Trazo(px, n, 34, 32, 48, 46, 3);
            Trazo(px, n, 34, 32, 48, 18, 3);
        }

        static void PintarPatada(Color32[] px, int n)
        {
            Trazo(px, n, 10, 34, 36, 34, 3);
            Trazo(px, n, 36, 34, 52, 46, 3);
            Trazo(px, n, 36, 34, 52, 22, 3);
            Trazo(px, n, 52, 22, 52, 46, 3);
        }
    }
}
