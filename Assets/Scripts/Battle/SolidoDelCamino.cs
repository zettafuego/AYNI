using UnityEngine;

namespace AYNI.Battle
{
    /// <summary>
    /// Muros, troncos, rocas y puestos paran al cuerpo.
    /// La cámara se acerca si alguno se mete entre el lente y el personaje.
    /// El piso, el río y el pasto no entran: el suelo ya se pisa por altura.
    /// La capa 4 (Water) es solo la copa del árbol, para no editar el TagManager.
    /// </summary>
    public static class SolidoDelCamino
    {
        const int CapaCopa = 4;
        const int MascaraCuerpo = 1;
        const int MascaraVista = 1 | (1 << CapaCopa);

        static readonly Collider[] cerca = new Collider[12];

        public static void Armar(Transform root)
        {
            if (root == null) return;
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
                Armar(filters[i]);
        }

        public static bool Libre(Vector3 pies, float radio, float alto)
        {
            float hip = Mathf.Clamp(alto * 0.42f, 0.65f, 1.6f);
            float pecho = Mathf.Clamp(alto * 0.72f, 1.0f, 2.6f);
            if (Ocupado(pies + Vector3.up * hip, radio)) return false;
            if (Ocupado(pies + Vector3.up * pecho, radio * 0.85f)) return false;
            return true;
        }

        /// <summary>Deja salir si el cuerpo ya nació dentro de un volumen.</summary>
        public static bool Puede(Vector3 desde, Vector3 hasta, float radio, float alto)
        {
            if (Libre(hasta, radio, alto)) return true;
            return !Libre(desde, radio, alto);
        }

        public static void Encuadrar(Camera cam, Vector3 pivot, Vector3 deseada, bool salto)
        {
            if (cam == null) return;
            const float radio = 0.3f;
            Vector3 clara = Vista(pivot, deseada, radio);
            Vector3 next;
            if (salto)
                next = clara;
            else
            {
                float quiere = (clara - pivot).magnitude;
                float tiene = (cam.transform.position - pivot).magnitude;
                if (quiere + 0.2f < tiene)
                    next = clara;
                else
                {
                    float t = 1f - Mathf.Exp(-7f * Time.deltaTime);
                    next = Vector3.Lerp(cam.transform.position, clara, t);
                    next = Vista(pivot, next, radio);
                }
            }

            next = Fuera(pivot, next, radio);
            Vector3 hacia = pivot - next;
            if (hacia.sqrMagnitude < 0.0004f) hacia = Vector3.forward;
            cam.transform.SetPositionAndRotation(next, Quaternion.LookRotation(hacia, Vector3.up));
        }

        static Vector3 Vista(Vector3 pivot, Vector3 deseada, float radio)
        {
            Vector3 delta = deseada - pivot;
            float dist = delta.magnitude;
            if (dist < 0.25f) return pivot + Vector3.up * 0.4f;
            Vector3 dir = delta / dist;
            if (Physics.SphereCast(pivot, radio, dir, out var hit, dist, MascaraVista, QueryTriggerInteraction.Ignore))
            {
                float queda = Mathf.Clamp(hit.distance - 0.22f, 0.9f, dist);
                return pivot + dir * queda;
            }

            return deseada;
        }

        static Vector3 Fuera(Vector3 pivot, Vector3 punto, float radio)
        {
            if (!Adentro(punto, radio)) return punto;
            Vector3 dir = punto - pivot;
            float dist = dir.magnitude;
            if (dist < 0.05f)
                dir = Vector3.back;
            else
                dir /= dist;

            Vector3 claro = pivot;
            for (int i = 1; i <= 8; i++)
            {
                Vector3 prueba = pivot + dir * (dist * i / 8f);
                if (Adentro(prueba, radio)) break;
                claro = prueba;
            }

            if (!Adentro(claro, radio * 0.65f)) return claro;
            if (Physics.Raycast(pivot, Vector3.up, out var techo, 28f, MascaraVista, QueryTriggerInteraction.Ignore))
                return techo.point + Vector3.up * 0.7f;
            return pivot + Vector3.up * 1.4f;
        }

        static bool Adentro(Vector3 punto, float radio)
        {
            return Physics.CheckSphere(punto, radio, MascaraVista, QueryTriggerInteraction.Ignore);
        }

        static bool Ocupado(Vector3 centro, float radio)
        {
            int n = Physics.OverlapSphereNonAlloc(centro, radio, cerca, MascaraCuerpo, QueryTriggerInteraction.Ignore);
            return n > 0;
        }

        static void Armar(MeshFilter filter)
        {
            if (filter == null) return;
            var go = filter.gameObject;
            if (go.GetComponent<Collider>() != null) return;
            if (go.GetComponent<TextMesh>() != null) return;
            if (go.GetComponent<SpriteRenderer>() != null) return;
            var rend = go.GetComponent<Renderer>();
            if (rend == null || !rend.enabled) return;

            string name = go.name;
            if (EsSuelo(name) || EsDecorado(name)) return;
            var mesh = filter.sharedMesh;
            if (mesh == null || mesh.vertexCount < 3) return;

            Vector3 lossy = go.transform.lossyScale;
            float sx = Mathf.Max(0.02f, Mathf.Abs(lossy.x));
            float sy = Mathf.Max(0.02f, Mathf.Abs(lossy.y));
            float sz = Mathf.Max(0.02f, Mathf.Abs(lossy.z));
            Vector3 world = new Vector3(mesh.bounds.size.x * sx, mesh.bounds.size.y * sy, mesh.bounds.size.z * sz);
            if (world.y < 0.55f) return;

            if (EsArbol(go.transform))
            {
                Tronco(go, mesh, world, sx, sz);
                return;
            }

            if (mesh.isReadable && mesh.vertexCount > 24)
            {
                var malla = go.AddComponent<MeshCollider>();
                malla.sharedMesh = mesh;
                malla.convex = false;
                return;
            }

            var caja = go.AddComponent<BoxCollider>();
            caja.center = mesh.bounds.center;
            caja.size = mesh.bounds.size;
        }

        static void Tronco(GameObject go, Mesh mesh, Vector3 world, float sx, float sz)
        {
            var cap = go.AddComponent<CapsuleCollider>();
            float ancho = Mathf.Max(sx, sz);
            float radio = Mathf.Clamp(Mathf.Min(world.x, world.z) * 0.14f, 0.35f, 1.35f);
            cap.direction = 1;
            cap.radius = radio / ancho;
            cap.height = Mathf.Max(mesh.bounds.size.y * 0.9f, cap.radius * 2.2f);
            cap.center = new Vector3(
                mesh.bounds.center.x,
                mesh.bounds.min.y + cap.height * 0.5f,
                mesh.bounds.center.z);

            var copa = new GameObject("Copa");
            copa.layer = CapaCopa;
            copa.transform.SetParent(go.transform, false);
            var caja = copa.AddComponent<BoxCollider>();
            float alto = mesh.bounds.size.y;
            caja.size = new Vector3(mesh.bounds.size.x * 0.68f, alto * 0.52f, mesh.bounds.size.z * 0.68f);
            caja.center = mesh.bounds.center + Vector3.up * (alto * 0.2f);
        }

        static bool EsArbol(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
            {
                string n = p.name;
                if (Tiene(n, "Arbol") || Tiene(n, "tree") || Tiene(n, "pine"))
                    return true;
            }

            return false;
        }

        static bool EsSuelo(string name)
        {
            return Tiene(name, "Terreno") || Tiene(name, "Suelo") || Tiene(name, "Calles")
                || Tiene(name, "Plazas") || Tiene(name, "Banda") || Tiene(name, "Sendero")
                || Tiene(name, "Cesped") || Tiene(name, "Agua") || Tiene(name, "Rio")
                || Tiene(name, "Atrio") || Tiene(name, "Escalin") || Tiene(name, "Nombres")
                || Tiene(name, "Lluvia") || Tiene(name, "Gota") || Tiene(name, "Nube")
                || Tiene(name, "Relampago") || Tiene(name, "Salpic") || Tiene(name, "Rampa")
                || Tiene(name, "Orilla") || Tiene(name, "Ribera") || Tiene(name, "Piso")
                || Tiene(name, "Borde") || Tiene(name, "Pista") || Tiene(name, "Cebra")
                || Tiene(name, "Vereda") || Tiene(name, "Grada");
        }

        static bool EsDecorado(string name)
        {
            return Tiene(name, "grass") || Tiene(name, "flower") || Tiene(name, "Farol")
                || Tiene(name, "mushroom") || Tiene(name, "Adorno")
                || Tiene(name, "Baranda") || Tiene(name, "SillaPlaza") || Tiene(name, "Balcon")
                || Tiene(name, "Fruto") || Tiene(name, "Baya") || Tiene(name, "Ademe");
        }

        static bool Tiene(string name, string token)
        {
            return name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
