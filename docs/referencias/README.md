# Referencias visuales — CALLES (Cajamarca)

Paquete de fotos reales de Cajamarca descargado de **Wikimedia Commons** para el
pase de arte del capítulo CALLES. Vive en `docs/`, **fuera de `Assets/`**, así que
no entra al build ni afecta a `Resources.Load("Calle/...")`.

- **70 archivos · 44,3 MB**
- 60 fotos (1920 px de ancho) + 9 muestras de material recortadas + 1 JSON de cortes
- Licencias y autores: [`CREDITOS.md`](CREDITOS.md) (mayoría CC BY-SA 4.0; una CC BY-SA 3.0 y una de dominio público)

## Estructura

```
docs/referencias/
├── CREDITOS.md              autores, licencias y enlaces a Commons
├── README.md                este archivo
└── cajamarca/
    ├── fachadas/            16  casas, portadas, puertas, casonas
    ├── calles/              23  jirones, esquinas, plazuelas, calle rural
    ├── monumentos/          17  catedral, San Francisco, Belén, Recoleta, rescate, plazas
    ├── materiales/           4  adobe, cantería, detalles
    └── muestras/             9  recortes de textura + _cortes.json
```

Contactos visuales: `docs/img/ref-fachadas.png`, `ref-calles.png`,
`ref-monumentos.png`, `ref-materiales.png`, `ref-muestras.png`.

## Cómo usarlo

0. **Ya está hecho:** las 9 muestras se convirtieron en 8 texturas tileables
   instaladas en el juego. Ver [`../TEXTURAS-CALLE-INTEGRACION.md`](../TEXTURAS-CALLE-INTEGRACION.md).
1. **Mirar antes de pintar.** Los contactos dan la vista general; los archivos
   individuales están a 1920 px para ampliar portadas, balcones y zócalos.
2. **Muestrear color** con el cuentagotas del editor de imágenes; las medias de
   las muestras están abajo.
3. **Recortar o derivar** solo si se asume la licencia: los derivados heredan
   CC BY-SA y hay que acreditarlos. Alternativa recomendada: usar la foto como
   guía y pintar la textura desde cero.
4. **No copiar marcas** (letreros, afiches, logos, matrículas). Ya hay dos fotos
   descartadas por ese motivo.

## Lectura de las fachadas (lo que cambia respecto al arte actual)

| Elemento | Lo que muestran las fotos | Problema en el juego |
| --- | --- | --- |
| Zócalo | Banda pintada de 1,0–1,2 m en gris-azul, verde seco o granate, con filo superior recto | Casi inexistente; todo el muro es beige |
| Planta alta | Cal blanco o crema; a veces ocre amarillo, rosa pálido o celeste | Un solo beige para las 293 casas |
| Balcones | Madera oscura (nogal), baranda de balaustres, base con canes | Metálicos/genéricos |
| Ventanas | Reja de hierro forjado, antepecho de piedra, a veces reja de madera | Sin reja ni antepecho |
| Portadas | Traquita gris con jambas, dintel y cornisa; arco solo en templos | Piedra clara y plana |
| Alero | Madera oscura con teja cerámica; 40–60 cm de vuelo | Teja sin alero |
| Techo | Teja ámbar-rojiza con vejez desigual (manchas, líquenes) | Color uniforme |
| Piso | Loseta/concreto en vías vehiculares; **piedra azul** en plazas y pasajes | Empedrado genérico repetido |
| Altura | 2 pisos + tímpano; 7,5–9,5 m al alero | Volúmenes más bajos y regulares |

Paleta medida (media de cada muestra):

| Muestra | Hex | Uso |
| --- | --- | --- |
| `cal-blanco` | `#BFBFB7` | muro de cal con manchas |
| `cal-ocre` | `#9E8856` | muro ocre amarillo |
| `revoco-desgastado` | `#9F9A88` | revoque con humedad y grietas |
| `canteria-belena` | `#878782` | sillería gris de Belén |
| `piedra-inca` | `#C6BFB3` | bloque inca del Cuarto del Rescate |
| `piedra-azul-labrada` | `#554F53` | piedra azul en plaza |
| `empedrado` | `#65615E` | empedrado de río |
| `teja` | `#A68C7C` | teja vista en techos |
| `madera-puerta` | `#4C3931` | madera de puerta/balcón |

## Cobertura por calle del juego

Con referencias directas: **Jr. 2 de Mayo**, **Jr. Amalia Puga**, **Jr. Belén**
(entorno San Francisco), **Jr. Eten**, **Jr. Ayacucho**, **Jr. Cinco Esquinas**,
**Jr. del Comercio**, **Jr. Silva Santisteban**, **Av. Atahualpa**,
**Av. Independencia**, **Jr. América**, **Plazuela Amalia Puga**.

Sin foto específica todavía (aparecen en `calles.json` pero no en Commons):
Apurímac, Tarapacá, La Mar, Pisagua, José Gálvez, Juan Villanueva,
Horacio Urteaga, Ucayali, San Pablo, Desamparados, Pasaje San Martín,
Bambamarca. Para estas, usar las fotos de tipología como referencia y no
inventar una arquitectura distinta.

## Siguientes pasos sugeridos

1. ~~Convertir las 9 muestras en texturas tileables~~ **hecho**: 4 reemplazos +
   4 texturas nuevas, ver [`../TEXTURAS-CALLE-INTEGRACION.md`](../TEXTURAS-CALLE-INTEGRACION.md).
   Pendiente: usar `empedrado_rio`, `cal_ocre` y `piedra_inca` en escena
   (hoy están en la biblioteca, sin colocar).
2. Derivar mapas de normales suaves (Sobel) para zócalo, cal y piedra.
3. Variar el tinte de `fachada_colonial` por tramo con los hex de arriba en vez
   de los 7 tints actuales.
4. Añadir un atlas de portadas (dintel + jambas + cornisa) reutilizable en los
   293 `Casa_XXX`.
5. Si se usan recortes dentro del juego, publicar el derivado bajo CC BY-SA y
   sumar el crédito compacto de `CREDITOS.md` a la pantalla de créditos.
