using System.Collections.Generic;
using AYNI.Narrative;
using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Bajo la Laguna El Perol se nada y se baja. El socavón abre solo con la lámpara encendida.
    /// El oro y el Muqui siguen el texto del capítulo.
    /// </summary>
    public class MinaDelMuqui : MonoBehaviour
    {
        const float LipX = 16f;
        const float Paso = 5.2f;

        LagunaBattleManager battle;
        BattleCameraFollow ojo;
        VizcachaFighter hero;
        Vector3 regreso;
        float piso;
        float fondo;
        float finX;
        bool tiene;
        bool encendida;
        bool carga;
        bool cobro;
        bool devuelto;
        bool dijoOrilla;
        bool dijoNada;
        bool dijoSinLuz;
        bool dijoAire;
        bool dijoLibre;
        bool dijoQueda;
        bool dijoEncima;
        bool dijoLejos;
        bool junto;
        bool enGaleria;
        float impulso;
        float impulsoX;
        float muquiBaja;
        bool muquiVivo = true;

        GameObject lamparaOrilla;
        GameObject brillo;
        GameObject velo;
        GameObject ciego;
        GameObject rojo;
        GameObject muqui;
        readonly List<Transform> oros = new List<Transform>();
        readonly List<Vector3> casas = new List<Vector3>();

        public static bool Ocupa { get; private set; }
        public static bool Junto => actual != null && actual.junto;
        public static bool EnGaleria => actual != null && actual.enGaleria;
        public static bool Encendida => actual != null && actual.encendida;
        public static bool CargaOro => actual != null && actual.carga;

        static MinaDelMuqui actual;

        public static void PulsarLampara()
        {
            if (actual == null) return;
            if (!actual.tiene)
            {
                if (Ocupa)
                {
                    if (!actual.dijoLejos)
                    {
                        actual.dijoLejos = true;
                        actual.Decir("LA LÁMPARA", LeyDelMonte.MinaEnOrilla, "El cuy");
                    }
                    return;
                }

                if (actual.junto) actual.Tomar();
                return;
            }

            actual.Encender();
        }

        public static void PulsarBajar()
        {
            if (actual == null || EnGaleria) return;
            if (!Ocupa)
            {
                if (actual.junto) actual.Entrar();
                return;
            }

            bool fondoListo = actual.hero != null && actual.hero.transform.position.y < actual.fondo + 2.6f;
            if (fondoListo) actual.impulsoX = -4.2f;
            else actual.impulso = -3.6f;
        }

        public static void PulsarSubir()
        {
            if (actual == null || !Ocupa) return;
            if (actual.enGaleria) actual.impulsoX = 6.5f;
            else actual.impulso = 3.6f;
        }

        public static void PulsarSoltar()
        {
            if (actual == null || !actual.carga) return;
            actual.Soltar();
        }

        public static void Preparar(LagunaBattleManager host)
        {
            if (host == null || host.transform.Find("MinaDelMuqui") != null) return;
            var go = new GameObject("MinaDelMuqui");
            go.transform.SetParent(host.transform, false);
            go.AddComponent<MinaDelMuqui>().Armar(host);
        }

        void OnDestroy()
        {
            if (actual == this) actual = null;
            if (Ocupa) Ocupa = false;
        }

        void Armar(LagunaBattleManager host)
        {
            actual = this;
            battle = host;
            piso = host.FloorY;
            fondo = piso - 14f;
            finX = LipX - 20f;
            Pozo();
            Socavon();
            Lampara();
            Oscuridad();
            var cam = Camera.main;
            if (cam != null) ojo = cam.GetComponent<BattleCameraFollow>();
        }

        void Pozo()
        {
            float alto = piso - fondo;
            float medio = (piso + fondo) * 0.5f;
            Muro("MuroEste", transform, new Vector3(LipX + 1.85f, medio, 1.2f), new Vector3(0.7f, alto, 0.8f), "Calle/piedra", new Color(0.42f, 0.4f, 0.36f));
            Muro("MuroOeste", transform, new Vector3(LipX - 1.85f, piso - 5.25f, 1.2f), new Vector3(0.7f, 10.5f, 0.8f), "Calle/piedra", new Color(0.42f, 0.4f, 0.36f));
            Muro("Agua", transform, new Vector3(LipX, medio, 2.4f), new Vector3(2.5f, alto - 0.4f, 0.35f), "Calle/piedra", new Color(0.1f, 0.32f, 0.72f));
            Muro("Charco", transform, new Vector3(LipX, piso + 0.12f, 0.4f), new Vector3(3.4f, 0.1f, 0.55f), "Calle/piedra", new Color(0.16f, 0.42f, 0.78f));
        }

        void Socavon()
        {
            float largo = LipX - finX;
            float centro = (LipX + finX) * 0.5f;
            float techo = fondo + 3.5f;
            Muro("PisoMina", transform, new Vector3(centro, fondo - 0.28f, 1f), new Vector3(largo, 0.5f, 1.1f), "Calle/piedra", new Color(0.38f, 0.34f, 0.3f));
            Muro("TechoMina", transform, new Vector3(centro, techo, 1f), new Vector3(largo, 0.45f, 1.1f), "Calle/piedra", new Color(0.28f, 0.26f, 0.24f));
            Muro("FondoMina", transform, new Vector3(centro, fondo + 1.6f, 2.6f), new Vector3(largo, 3.2f, 0.3f), "Calle/piedra", new Color(0.22f, 0.2f, 0.18f));
            Muro("Tapon", transform, new Vector3(finX - 0.4f, fondo + 1.6f, 1f), new Vector3(0.7f, 3.3f, 1.1f), "Calle/piedra", new Color(0.3f, 0.28f, 0.25f));
            for (int i = 0; i < 4; i++)
            {
                float x = LipX - 3.5f - i * 4.2f;
                Muro("Poste" + i, transform, new Vector3(x, fondo + 1.55f, 0.7f), new Vector3(0.28f, 3.1f, 0.28f), "Calle/madera_oscura", new Color(0.7f, 0.55f, 0.32f));
                Muro("Viga" + i, transform, new Vector3(x, fondo + 3.15f, 0.7f), new Vector3(1.4f, 0.22f, 0.28f), "Calle/madera_oscura", new Color(0.7f, 0.55f, 0.32f));
            }

            for (int i = 0; i < 3; i++)
            {
                var pepa = Muro("Oro" + i, transform, new Vector3(finX + 3.2f + i * 0.7f, fondo + 0.35f, 0.55f), new Vector3(0.42f, 0.32f, 0.42f), "Calle/piedra", new Color(1f, 0.82f, 0.22f));
                oros.Add(pepa);
                casas.Add(pepa.position);
            }

            muqui = new GameObject("Muqui");
            muqui.transform.SetParent(transform, false);
            muqui.transform.position = new Vector3(finX + 1.3f, fondo, 0.6f);
            Muro("Cuerpo", muqui.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.55f, 1.15f, 0.35f), "Calle/muro_adobe", new Color(0.55f, 0.42f, 0.3f));
            Muro("Cabeza", muqui.transform, new Vector3(0f, 1.5f, 0f), new Vector3(0.42f, 0.42f, 0.32f), "Calle/muro_adobe", new Color(0.62f, 0.48f, 0.34f));
            Muro("Sombrero", muqui.transform, new Vector3(0f, 1.82f, 0f), new Vector3(0.7f, 0.16f, 0.5f), "Calle/madera_oscura", new Color(0.45f, 0.28f, 0.14f));
            Muro("Ojo", muqui.transform, new Vector3(0.1f, 1.52f, -0.16f), new Vector3(0.1f, 0.1f, 0.08f), "Calle/piedra", new Color(1f, 0.9f, 0.4f));
        }

        void Lampara()
        {
            lamparaOrilla = new GameObject("LamparaOrilla");
            lamparaOrilla.transform.SetParent(transform, false);
            lamparaOrilla.transform.position = new Vector3(LipX - 2.4f, piso, 0f);
            Muro("Pie", lamparaOrilla.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.18f, 0.7f, 0.18f), "Calle/madera_oscura", new Color(0.55f, 0.4f, 0.22f));
            Muro("Fanal", lamparaOrilla.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.34f, 0.34f, 0.34f), "Calle/piedra", new Color(1f, 0.86f, 0.35f));
            var aviso = new GameObject("Aviso");
            aviso.transform.SetParent(lamparaOrilla.transform, false);
            aviso.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            var mesh = aviso.AddComponent<TextMesh>();
            mesh.text = "LAGUNA";
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.characterSize = 0.08f;
            mesh.fontSize = 56;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.95f, 0.9f, 0.75f);

            brillo = new GameObject("Brillo");
            brillo.transform.SetParent(transform, false);
            var luz = brillo.AddComponent<SpriteRenderer>();
            luz.sprite = Disco(new Color(1f, 0.9f, 0.45f, 0.85f), false);
            luz.sortingOrder = 16;
            brillo.SetActive(false);
        }

        void Oscuridad()
        {
            velo = Hoja("Velo", Disco(Color.black, true), 8);
            ciego = Hoja("Ciego", Disco(Color.black, false), 9);
            ciego.transform.localScale = new Vector3(1.4f, 1f, 1f);
            rojo = Hoja("Rojo", Disco(new Color(0.7f, 0.05f, 0.04f, 0.55f), false), 5);
            rojo.SetActive(false);
        }

        GameObject Hoja(string nombre, Sprite sprite, int orden)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var ink = go.AddComponent<SpriteRenderer>();
            ink.sprite = sprite;
            ink.sortingOrder = orden;
            go.SetActive(false);
            return go;
        }

        static Sprite Disco(Color tinta, bool hueco)
        {
            const int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pix = new Color[n * n];
            var centro = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - centro) / centro;
                    float dy = (y - centro) / centro;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    var c = tinta;
                    if (hueco)
                    {
                        c.a = Mathf.InverseLerp(0.16f, 0.42f, r);
                        if (r > 0.98f) c.a = 1f;
                    }
                    else if (r > 0.92f)
                        c.a *= 1f - Mathf.InverseLerp(0.92f, 1f, r);
                    pix[y * n + x] = c;
                }
            }

            tex.SetPixels(pix);
            tex.Apply(false);
            return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n / 32f);
        }

        static Transform Muro(string nombre, Transform parent, Vector3 local, Vector3 tam, string tex, Color tinta)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            var cuerpo = go.GetComponent<Collider>();
            if (cuerpo != null) Destroy(cuerpo);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = tam;
            var renderer = go.GetComponent<Renderer>();
            var shader = Shader.Find("AYNI/Calle") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            var texture = Resources.Load<Texture2D>(tex);
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                mat.mainTexture = texture;
                mat.mainTextureScale = new Vector2(Mathf.Max(1f, tam.x), Mathf.Max(1f, tam.y));
            }

            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mesh = go.GetComponent<MeshFilter>().mesh;
            var cols = new Color[mesh.vertexCount];
            for (int i = 0; i < cols.Length; i++) cols[i] = tinta;
            mesh.colors = cols;
            return go.transform;
        }

        void Update()
        {
            if (battle == null) return;
            if (!battle.IsPlaying || battle.IsGameOver)
            {
                if (Ocupa) Salir();
                return;
            }

            if (CaminoRelato.Reading) return;
            if (hero == null) hero = battle.Player;
            if (hero == null) return;

            if (!Ocupa)
            {
                Orilla();
                return;
            }

            Bucear();
        }

        void LateUpdate()
        {
            if (!Ocupa || hero == null || ojo == null) return;
            bool galeria = hero.PositionX < LipX - 1.2f;
            ojo.Focus(hero.PositionX, hero.transform.position.y + 1.15f, galeria ? 4.6f : 5.2f);
            var centro = new Vector3(ojo.transform.position.x, ojo.transform.position.y, 0f);
            if (velo != null)
            {
                velo.SetActive(galeria && encendida);
                velo.transform.position = centro;
            }

            if (ciego != null)
            {
                ciego.SetActive(galeria && !encendida);
                ciego.transform.position = centro;
            }

            if (rojo != null)
            {
                rojo.SetActive(galeria && carga);
                rojo.transform.position = centro;
            }

            if (brillo != null && brillo.activeSelf)
                brillo.transform.position = hero.transform.position + new Vector3(0.45f, 0.8f, 0f);
        }

        void Orilla()
        {
            float dx = hero.PositionX - LipX;
            bool cerca = Mathf.Abs(dx) < 2.4f && Mathf.Abs(hero.transform.position.y - piso) < 1.4f;
            junto = cerca;
            if (cerca && !dijoOrilla)
            {
                dijoOrilla = true;
                Decir("LA LAGUNA", LeyDelMonte.MinaOrilla, "El cuy");
            }

            bool lampara = Input.GetKeyDown(KeyCode.L);
            if (lampara && !tiene && Mathf.Abs(hero.PositionX - (LipX - 2.4f)) < 1.8f)
            {
                Tomar();
                return;
            }

            if (lampara && tiene) Encender();
            if (Input.GetKeyDown(KeyCode.G) && carga) Soltar();

            bool baja = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
            if (baja && cerca) Entrar();
        }

        void Tomar()
        {
            tiene = true;
            encendida = true;
            if (lamparaOrilla != null) lamparaOrilla.SetActive(false);
            if (brillo != null) brillo.SetActive(true);
            Decir("LA LÁMPARA", LeyDelMonte.MinaToma, "El cuy");
        }

        void Encender()
        {
            encendida = !encendida;
            if (brillo != null) brillo.SetActive(encendida);
            if (encendida) dijoSinLuz = false;
            Decir("LA LÁMPARA", encendida ? LeyDelMonte.MinaLuz : LeyDelMonte.MinaApaga, "El cuy");
        }

        void Entrar()
        {
            regreso = hero.transform.position;
            junto = false;
            Ocupa = true;
            hero.transform.position = new Vector3(LipX, piso - 1.1f, 0f);
            hero.Face(false);
            if (!dijoNada)
            {
                dijoNada = true;
                Decir("EL AGUA", LeyDelMonte.MinaNada, "El cuy");
            }
        }

        void Bucear()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Salir();
                return;
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                if (tiene) Encender();
                else if (!dijoLejos)
                {
                    dijoLejos = true;
                    Decir("LA LÁMPARA", LeyDelMonte.MinaEnOrilla, "El cuy");
                }
            }

            if (Input.GetKeyDown(KeyCode.G) && carga) Soltar();

            float h = Input.GetAxisRaw("Horizontal");
            float v = 0f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;

            var p = hero.transform.position;
            bool galeria = p.x < LipX - 1.15f;
            if (galeria) impulso = 0f;
            float avanceX = TomarImpulso(ref impulsoX);
            float avanceY = galeria ? 0f : TomarImpulso(ref impulso);
            p.x += h * Paso * Time.deltaTime + avanceX;
            if (!galeria) p.y += v * 4.2f * Time.deltaTime + avanceY;
            h = Mathf.Abs(h) > 0.01f ? h : avanceX;

            if (p.y > piso - 0.35f && (v > 0f || avanceY > 0.01f) && !galeria)
            {
                Salir();
                return;
            }

            p.y = Mathf.Clamp(p.y, fondo + 0.15f, piso - 0.35f);
            bool boca = p.y < fondo + 2.3f;
            if (!galeria)
            {
                float min = boca && encendida ? finX + 0.8f : LipX - 0.95f;
                if (boca && !encendida && (h < -0.01f || avanceX < -0.01f) && !dijoSinLuz)
                {
                    dijoSinLuz = true;
                    Decir("EL SOCAVÓN", LeyDelMonte.MinaSinLuz, "El cuy");
                }

                p.x = Mathf.Clamp(p.x, min, LipX + 0.95f);
            }
            else
            {
                p.y = fondo + 0.15f;
                float min = encendida ? finX + 0.8f : LipX - 1.05f;
                if (!encendida && (h < -0.01f || avanceX < -0.01f)) min = p.x;
                p.x = Mathf.Clamp(p.x, min, LipX - 0.4f);
                if (!dijoAire)
                {
                    dijoAire = true;
                    Decir("LA MINA", LeyDelMonte.MinaAire, "La mina");
                }
            }

            hero.transform.position = p;
            enGaleria = p.x < LipX - 1.15f;
            if (Mathf.Abs(h) > 0.01f) hero.Face(h > 0f);
            if (galeria) hero.GetComponent<SpriteFrameAnimator>()?.PlayLocomotion(Mathf.Abs(h) > 0.01f, false);
            else hero.GetComponent<SpriteFrameAnimator>()?.PlaySwim();

            if (muquiBaja > 0f)
            {
                muquiBaja -= Time.deltaTime;
                if (muqui != null) muqui.transform.position += Vector3.down * Time.deltaTime * 0.8f;
                if (muquiBaja <= 0f && muqui != null) muqui.SetActive(false);
            }

            ProbarOro(p);
        }

        void ProbarOro(Vector3 pies)
        {
            RecordarOro();
            if (!muquiVivo || carga) return;
            for (int i = 0; i < oros.Count; i++)
            {
                var pepa = oros[i];
                if (pepa == null) continue;
                if (Vector3.Distance(pies, pepa.position) > 1.15f) continue;
                if (devuelto)
                {
                    if (!dijoQueda)
                    {
                        dijoQueda = true;
                        Decir("EL ORO", LeyDelMonte.MinaQueda, "El cuy");
                    }
                    return;
                }

                carga = true;
                PartidaDelCamino.GuardarOro();
                if (!cobro)
                {
                    cobro = true;
                    battle.NoteCodicia();
                }

                for (int k = 0; k < oros.Count; k++)
                {
                    if (oros[k] == null) continue;
                    oros[k].SetParent(hero.transform, true);
                    oros[k].localPosition = new Vector3(-0.35f + k * 0.28f, 0.9f, 0f);
                }

                if (!dijoLibre)
                {
                    dijoLibre = true;
                    Decir("EL ORO", LeyDelMonte.MinaLibre, "La vizcacha");
                }
                return;
            }
        }

        void Soltar()
        {
            carga = false;
            for (int i = 0; i < oros.Count; i++)
            {
                if (oros[i] == null) continue;
                oros[i].SetParent(transform, true);
                oros[i].position = casas[i];
                oros[i].localScale = new Vector3(0.42f, 0.32f, 0.42f);
            }

            if (cobro && !devuelto)
            {
                devuelto = true;
                battle.DevolverOro();
            }

            PartidaDelCamino.SoltarOro();
            muquiBaja = 1.6f;
            muquiVivo = false;
            Decir("EL MUQUI", LeyDelMonte.MinaSuelta, "El cuy");
        }

        void RecordarOro()
        {
            if (PartidaDelCamino.EleccionOro == PartidaDelCamino.Oro.Soltado)
            {
                devuelto = true;
                cobro = true;
                carga = false;
                muquiVivo = false;
                if (muqui != null) muqui.SetActive(false);
                return;
            }

            if (PartidaDelCamino.EleccionOro != PartidaDelCamino.Oro.Guardado || carga || hero == null) return;
            carga = true;
            cobro = true;
            for (int k = 0; k < oros.Count; k++)
            {
                if (oros[k] == null) continue;
                oros[k].SetParent(hero.transform, true);
                oros[k].localPosition = new Vector3(-0.35f + k * 0.28f, 0.9f, 0f);
            }
        }

        void Salir()
        {
            if (hero != null)
            {
                hero.transform.position = new Vector3(regreso.x, piso, 0f);
                hero.GetComponent<SpriteFrameAnimator>()?.PlayLocomotion(false, false);
            }

            Ocupa = false;
            enGaleria = false;
            impulso = 0f;
            impulsoX = 0f;
            ojo?.Release();
            if (velo != null) velo.SetActive(false);
            if (ciego != null) ciego.SetActive(false);
            if (rojo != null) rojo.SetActive(false);
            if (carga && !dijoEncima)
            {
                dijoEncima = true;
                Decir("EL ORO", LeyDelMonte.MinaEncima, "El cuy");
            }
        }

        static float TomarImpulso(ref float queda)
        {
            if (Mathf.Abs(queda) < 0.02f)
            {
                queda = 0f;
                return 0f;
            }

            float paso = Mathf.Sign(queda) * Mathf.Min(Mathf.Abs(queda), 9f * Time.deltaTime);
            queda -= paso;
            return paso;
        }

        void Decir(string kicker, string linea, string quien)
        {
            battle?.ShowRoadLine(kicker, linea, quien, 6.5f);
        }
    }
}
