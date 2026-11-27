"""Prepara los sprites de Peakingu a partir de las imágenes de Gemini.

Uso (desde la raíz del proyecto):
    python preparar_assets.py            # SOLO los assets nuevos: casa, pescados, humo/vapor y fondos (seguro)
    python preparar_assets.py base       # solo suelo, hielo, decoración y fondo de montañas
    python preparar_assets.py todo       # base + nuevos
Requiere:  pip install pillow numpy scipy

OJO: "base" y "todo" REGENERAN todo Assets/Sprites/Clean desde Raw. Las 9 piezas de suelo
(Clean/Tiles/suelo_*.png) que hay hoy en el repo NO coinciden con lo que genera este script
desde Raw/Tiles/suelo.jpg (probablemente las retocaste a mano), así que las sobrescribiría.
Haz una copia de Clean antes, o usa el modo por defecto.

Los sprites con transparencia se piden a Gemini sobre fondo verde plano #00FF00.
Los fondos completos (cima e interior) NO llevan fondo verde: son escenas enteras.

ARCHIVOS NUEVOS QUE ESPERA (en Assets/Sprites/Raw, .png o .jpg):
    Decor/casa                 casa exterior, un solo sprite sobre verde
    Decor/pescados             DOS sprites sobre verde: izquierda = bueno, derecha = quemado
       (alternativa: Decor/pescado_bueno y Decor/pescado_quemado como dos archivos)
    Decor/humo_vapor           2 filas x 3 frames sobre verde: fila 1 = humo negro, fila 2 = vapor blanco
    Background/cima            fondo panorámico de la cima
    Background/interior_casa   interior de la cabaña
Lo que no encuentre lo salta con un aviso, sin detener el resto.

SALIDAS NUEVAS (en Assets/Sprites/Clean):
    Decor/casa.png, Decor/pescado_bueno.png, Decor/pescado_quemado.png
    Anim/humo_negro_01..03.png, Anim/vapor_blanco_01..03.png
    Background/cima.png, Background/interior_casa.png
"""
from pathlib import Path
import sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

RAW = Path("Assets/Sprites/Raw")
OUT = Path("Assets/Sprites/Clean")
LADO_TILE = 128        # tamaño en px de cada tile (PPU 128 => 1 tile = 1 unidad)
# Escala de la decoración por tipo (1 = tamaño original de Gemini). Sube o baja según el tamaño de Pingu.
ESCALA_DECOR = {"pino": 0.9, "roca": 0.5, "monticulo": 0.5, "cartel": 0.55,
                "carambano": 0.55, "bandera": 0.6, "huellas": 0.5}

# ---- Tamaños de los assets nuevos (con PPU 128, 128 px = 1 unidad de Unity) ----
ANCHO_CASA = 640       # casa de unos 5 unidades de ancho
ANCHO_PESCADO = 192    # ancho del pescado más ancho; los dos salen del mismo tamaño y lienzo
LADO_FRAME = 192       # cada frame de humo/vapor queda en un cuadrado de LADO_FRAME px
ANIMACIONES = [("humo_negro", 3), ("vapor_blanco", 3)]   # una fila por animación, de arriba a abajo
ANCHO_MAX_FONDO = {"cima": 3584, "interior_casa": 1920}  # si Gemini entrega más ancho, se reduce


def abrir(carpeta, nombre):
    for ext in (".png", ".jpg", ".jpeg"):
        p = RAW / carpeta / (nombre + ext)
        if p.exists():
            return Image.open(p).convert("RGB")
    sys.exit(f"Falta {RAW / carpeta / nombre}.jpg (o .png)")


def a_rgba(img):
    """Quita el verde #00FF00, limpia el halo verdoso y devuelve (RGBA, mascara_verde)."""
    a = np.array(img).astype(np.int16)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    verde = (g > 150) & (g > r + 60) & (g > b + 60)
    tope = np.maximum(r, b)
    a[..., 1] = np.where(g > tope, tope, g)          # elimina el reflejo verde en bordes
    alfa = np.where(verde, 0, 255)
    rgba = np.dstack([a, alfa]).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA"), verde


def reducir(img, lado=None, escala=None):
    if lado:
        return img.resize((lado, lado), Image.BOX)
    w, h = img.size
    return img.resize((max(1, round(w * escala)), max(1, round(h * escala))), Image.BOX)


# ---------- 1) SUELO 3x3 ----------
def tramos(es_hueco):
    res, ini = [], None
    for i, h in enumerate(es_hueco):
        if not h and ini is None:
            ini = i
        if h and ini is not None:
            res.append((ini, i)); ini = None
    if ini is not None:
        res.append((ini, len(es_hueco)))
    return res


def suelo():
    nombres = [["esq_sup_izq", "borde_sup", "esq_sup_der"],
               ["borde_izq", "relleno", "borde_der"],
               ["esq_inf_izq", "borde_inf", "esq_inf_der"]]
    rgba, verde = a_rgba(abrir("Tiles", "suelo"))
    cols = tramos(verde.mean(axis=0) > 0.97)
    filas = tramos(verde.mean(axis=1) > 0.97)
    if len(cols) != 3 or len(filas) != 3:
        sys.exit("SUELO: no detecté una cuadrícula 3x3 limpia.")
    H = max(y1 - y0 for y0, y1 in filas)
    W = max(x1 - x0 for x0, x1 in cols)
    M = 6  # recorte hacia adentro para quitar restos verdes
    d = OUT / "Tiles"; d.mkdir(parents=True, exist_ok=True)
    for fi, (y0, y1) in enumerate(filas):
        for ci, (x0, x1) in enumerate(cols):
            x0b = max(0, x1 - W) if ci == 0 else x0
            if fi == 0:
                # La fila superior es más baja que las demás. En vez de dejar un hueco
                # transparente ARRIBA (los objetos quedarían flotando sobre la nieve),
                # se completa hacia ABAJO con el reflejo de su propia roca.
                t = rgba.crop((x0b + M, y0 + M, x0b + W - M, y1 - M))
                falta = (H - 2 * M) - t.height
                lienzo = Image.new("RGBA", (t.width, t.height + falta), (0, 0, 0, 0))
                lienzo.paste(t, (0, 0))
                reflejo = t.crop((0, t.height - falta, t.width, t.height)).transpose(Image.FLIP_TOP_BOTTOM)
                lienzo.paste(reflejo, (0, t.height))
                t = lienzo
            else:
                t = rgba.crop((x0b + M, y0 + M, x0b + W - M, y0 + H - M))
            reducir(t, lado=LADO_TILE).save(d / f"suelo_{nombres[fi][ci]}.png")
    print("Suelo: 9 tiles OK")


# ---------- 2) HIELO (12 bloques) ----------
def hielo():
    rgba, verde = a_rgba(abrir("Tiles", "hielo"))
    h, w = verde.shape
    sol = ndi.binary_opening(~verde, iterations=2)
    lab, n = ndi.label(sol)
    piezas = []
    for i, sl in enumerate(ndi.find_objects(lab), 1):
        ys, xs = sl
        cy, cx = (ys.start + ys.stop) / 2, (xs.start + xs.stop) / 2
        if cx > 0.48 * w and cy > 0.5 * h and (ys.stop - ys.start) > 0.08 * h:
            piezas.append((i, xs, ys))
    if len(piezas) != 12:
        sys.exit(f"HIELO: esperaba 12 bloques y encontré {len(piezas)}.")
    piezas.sort(key=lambda p: (round(((p[2].start + p[2].stop) / 2) / (0.17 * h)), p[1].start))
    d = OUT / "Tiles"
    for k, (i, xs, ys) in enumerate(piezas, 1):
        t = rgba.crop((xs.start + 6, ys.start + 6, xs.stop - 6, ys.stop - 6))
        reducir(t, lado=LADO_TILE).save(d / f"hielo_{k:02d}.png")
    print("Hielo: 12 bloques OK")


# ---------- 3) DECORACIÓN (sprites sueltos con nombre) ----------
NOMBRES_DECOR = {
    1: "pino_01", 2: "pino_02", 3: "pino_03", 10: "pino_04", 11: "pino_05",
    12: "pino_06", 17: "pino_07", 19: "pino_08",
    4: "roca_01", 5: "roca_02", 6: "roca_03", 7: "roca_04", 8: "roca_05", 9: "roca_06",
    13: "cartel", 14: "monticulo_01", 15: "monticulo_02", 16: "monticulo_03",
    26: "monticulo_04", 18: "carambano_01", 20: "carambano_02", 22: "carambano_03",
    23: "carambano_04", 24: "bandera_apagada", 25: "bandera_encendida",
}
HUELLAS = [27, 28, 29, 30, 31]          # se unen en un solo sprite
IGNORAR = [21]                          # mota suelta


def decoracion():
    rgba, verde = a_rgba(abrir("Decor", "decor"))
    sol = ndi.binary_opening(~verde, iterations=1)
    lab, n = ndi.label(ndi.binary_dilation(sol, iterations=5))
    if n != 31:
        sys.exit(f"DECOR: esperaba 31 piezas y encontré {n}.")
    comp = []
    for i, sl in enumerate(ndi.find_objects(lab), 1):
        ys, xs = sl
        comp.append((ys.start // 450, xs.start, i, sl))
    comp.sort(key=lambda c: (c[0], c[1]))          # numeración 1..31 (arriba->abajo, izq->der)
    arr = np.array(rgba)
    d = OUT / "Decor"; d.mkdir(parents=True, exist_ok=True)

    def guardar(mascara, caja, nombre):
        ys, xs = caja
        sub = arr[ys, xs].copy()
        sub[..., 3] = np.where(mascara[ys, xs] & (sub[..., 3] > 0), 255, 0)
        esc = ESCALA_DECOR[nombre.split("_")[0]]
        reducir(Image.fromarray(sub, "RGBA"), escala=esc).save(d / f"{nombre}.png")

    for num, (_, _, i, sl) in enumerate(comp, 1):
        if num in NOMBRES_DECOR:
            guardar(lab == i, sl, NOMBRES_DECOR[num])
    # huellas: unir las 5 manchas
    m = np.zeros(lab.shape, bool); y0 = x0 = 10**9; y1 = x1 = 0
    for num in HUELLAS:
        _, _, i, sl = comp[num - 1]
        m |= (lab == i)
        y0, x0 = min(y0, sl[0].start), min(x0, sl[1].start)
        y1, x1 = max(y1, sl[0].stop), max(x1, sl[1].stop)
    guardar(m, (slice(y0, y1), slice(x0, x1)), "huellas")
    print("Decoración: 26 sprites OK")


# ---------- 4) FONDO ----------
def fondo():
    rgba, verde = a_rgba(abrir("Background", "montanas"))
    d = OUT / "Background"; d.mkdir(parents=True, exist_ok=True)
    rgba.save(d / "montanas_lejanas.png")      # tamaño original, sin recorte
    print("Fondo: montanas_lejanas.png OK")


# =====================================================================
#  NUEVOS: casa, pescados, humo/vapor y fondos de cima e interior
# =====================================================================
def buscar(carpeta, nombre):
    """Devuelve la ruta del archivo en Raw (.png/.jpg/.jpeg) o None si no existe."""
    for ext in (".png", ".jpg", ".jpeg"):
        p = RAW / carpeta / (nombre + ext)
        if p.exists():
            return p
    return None


def limpiar_alfa(rgba, verde, fraccion=0.005):
    """Deja alfa solo donde hay contenido sólido y quita motas sueltas
    (piezas menores que `fraccion` de la pieza más grande)."""
    sol = ndi.binary_opening(~verde, iterations=1)
    lab, n = ndi.label(sol)
    if n > 1:
        areas = ndi.sum(sol, lab, range(1, n + 1))
        grandes = [i + 1 for i, a in enumerate(areas) if a >= fraccion * max(areas)]
        sol = np.isin(lab, grandes)
    arr = np.array(rgba)
    arr[..., 3] = np.where(sol & (arr[..., 3] > 0), 255, 0)
    return Image.fromarray(arr, "RGBA")


def recorte_contenido(rgba, margen=2):
    """Recorta la imagen al rectángulo que contiene píxeles no transparentes."""
    alfa = np.array(rgba)[..., 3]
    ys, xs = np.where(alfa > 0)
    if len(ys) == 0:
        return None
    x0, x1 = max(0, int(xs.min()) - margen), min(rgba.width, int(xs.max()) + 1 + margen)
    y0, y1 = max(0, int(ys.min()) - margen), min(rgba.height, int(ys.max()) + 1 + margen)
    return rgba.crop((x0, y0, x1, y1))


def normalizar(recortes, ancho=None, lado=None):
    """Escala todos los recortes con el MISMO factor y los pega en un lienzo común,
    centrados en horizontal y apoyados abajo, para que se puedan intercambiar sin que salten.
    ancho: el recorte más ancho queda con ese ancho (lienzo = el mayor resultante).
    lado:  cada recorte cabe en un cuadrado lado x lado."""
    if lado:
        factor = (lado - 4) / max(max(r.width, r.height) for r in recortes)
    else:
        factor = ancho / max(r.width for r in recortes)
    esc = [reducir(r, escala=factor) for r in recortes]
    if lado:
        W = H = lado
        pie = 2
    else:
        W, H = max(e.width for e in esc), max(e.height for e in esc)
        pie = 0
    salida = []
    for e in esc:
        lienzo = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        lienzo.paste(e, ((W - e.width) // 2, H - e.height - pie))
        salida.append(lienzo)
    return salida


def recorte_de_archivo(carpeta, nombre):
    """Abre un sprite suelto sobre verde y devuelve su recorte limpio (o None con aviso)."""
    p = buscar(carpeta, nombre)
    if p is None:
        print(f"  (salto {nombre}: no existe {RAW / carpeta / nombre}.png/.jpg)")
        return None
    rgba, verde = a_rgba(Image.open(p).convert("RGB"))
    rec = recorte_contenido(limpiar_alfa(rgba, verde))
    if rec is None:
        print(f"  (salto {nombre}: no encontré contenido; ¿el fondo es verde plano #00FF00?)")
    return rec


def guardar_lista(carpeta, nombres, imagenes):
    d = OUT / carpeta; d.mkdir(parents=True, exist_ok=True)
    for nombre, img in zip(nombres, imagenes):
        img.save(d / f"{nombre}.png")
        print(f"  {carpeta}/{nombre}.png OK ({img.width}x{img.height})")


def sprite_casa():
    rec = recorte_de_archivo("Decor", "casa")
    if rec is not None:
        guardar_lista("Decor", ["casa"], normalizar([rec], ancho=ANCHO_CASA))


def pescados():
    nombres = ["pescado_bueno", "pescado_quemado"]
    if buscar("Decor", "pescados"):
        # Una sola imagen con los dos pescados: izquierda = bueno, derecha = quemado
        rgba, verde = a_rgba(Image.open(buscar("Decor", "pescados")).convert("RGB"))
        rgba = limpiar_alfa(rgba, verde)
        solido = np.array(rgba)[..., 3] > 0
        lab, n = ndi.label(ndi.binary_dilation(solido, iterations=5))
        if n != 2:
            print(f"  (salto pescados: esperaba 2 sprites y encontré {n}; revisa que estén separados)")
            return
        arr = np.array(rgba)
        piezas = sorted(((sl[1].start, i, sl) for i, sl in enumerate(ndi.find_objects(lab), 1)))
        recs = []
        for _, i, sl in piezas:
            sub = arr[sl].copy()
            sub[..., 3] = np.where((lab[sl] == i) & (sub[..., 3] > 0), 255, 0)
            recs.append(recorte_contenido(Image.fromarray(sub, "RGBA")))
    else:
        # Alternativa: dos archivos separados
        recs = [recorte_de_archivo("Decor", n) for n in nombres]
        if any(r is None for r in recs):
            return
    guardar_lista("Decor", nombres, normalizar(recs, ancho=ANCHO_PESCADO))


def _unir_tramos(tr, hueco_max):
    """Une tramos consecutivos separados por huecos pequeños."""
    res = []
    for ini, fin in tr:
        if res and ini - res[-1][1] <= hueco_max:
            res[-1] = (res[-1][0], fin)
        else:
            res.append((ini, fin))
    return res


def _partes_iguales(total, n):
    return [(round(i * total / n), round((i + 1) * total / n)) for i in range(n)]


def animaciones():
    p = buscar("Decor", "humo_vapor")
    if p is None:
        print(f"  (salto humo_vapor: no existe {RAW / 'Decor' / 'humo_vapor'}.png/.jpg)")
        return
    rgba, verde = a_rgba(Image.open(p).convert("RGB"))
    rgba = limpiar_alfa(rgba, verde, fraccion=0.002)
    solido = np.array(rgba)[..., 3] > 0
    h, w = solido.shape
    filas = _unir_tramos(tramos(~solido.any(axis=1)), max(4, int(h * 0.02)))
    if len(filas) != len(ANIMACIONES):
        print(f"  (humo_vapor: detecté {len(filas)} filas en vez de {len(ANIMACIONES)}; divido la imagen en partes iguales)")
        filas = _partes_iguales(h, len(ANIMACIONES))
    for (nombre, n_frames), (y0, y1) in zip(ANIMACIONES, filas):
        franja = solido[y0:y1]
        cols = _unir_tramos(tramos(~franja.any(axis=0)), max(4, int(w * 0.01)))
        if len(cols) != n_frames:
            print(f"  ({nombre}: detecté {len(cols)} frames en vez de {n_frames}; divido la fila en partes iguales)")
            cols = _partes_iguales(w, n_frames)
        recs = []
        for x0, x1 in cols:
            rec = recorte_contenido(rgba.crop((x0, y0, x1, y1)))
            if rec is None:
                print(f"  (salto {nombre}: hay un frame vacío)")
                recs = None
                break
            recs.append(rec)
        if recs:
            guardar_lista("Anim", [f"{nombre}_{k:02d}" for k in range(1, n_frames + 1)],
                          normalizar(recs, lado=LADO_FRAME))


def fondos_nuevos():
    """Fondos completos (sin fondo verde): solo se reducen si Gemini entregó más ancho del necesario."""
    for nombre, ancho_max in ANCHO_MAX_FONDO.items():
        p = buscar("Background", nombre)
        if p is None:
            print(f"  (salto {nombre}: no existe {RAW / 'Background' / nombre}.png/.jpg)")
            continue
        img = Image.open(p).convert("RGBA")
        if img.width > ancho_max:
            img = reducir(img, escala=ancho_max / img.width)
        guardar_lista("Background", [nombre], [img])


def nuevos():
    print("Assets nuevos:")
    sprite_casa()
    pescados()
    animaciones()
    fondos_nuevos()


# ---------- EJECUCIÓN ----------
def paso(funcion):
    """Ejecuta un paso; si falla con sys.exit, avisa y deja seguir con los demás."""
    try:
        funcion()
        return True
    except SystemExit as e:
        print(f"AVISO ({funcion.__name__}): {e}")
        return False


if __name__ == "__main__":
    modo = sys.argv[1] if len(sys.argv) > 1 else "nuevos"
    if modo not in ("todo", "base", "nuevos"):
        sys.exit("Uso: python preparar_assets.py [nuevos | base | todo]")
    fallos = []
    if modo in ("todo", "base"):
        for f in (suelo, hielo, decoracion, fondo):
            if not paso(f):
                fallos.append(f.__name__)
    if modo in ("todo", "nuevos"):
        if not paso(nuevos):
            fallos.append("nuevos")
    if fallos:
        print("Pasos con problemas: " + ", ".join(fallos))
    print("Listo. Vuelve a Unity para que importe Assets/Sprites/Clean.")
    if modo in ("todo", "nuevos"):
        print("Recuerda: los PNG nuevos entran con la importación por defecto de Unity. "
              "Ponles filtro Point y 128 PPU (los fondos: 100 PPU y Bilinear).")