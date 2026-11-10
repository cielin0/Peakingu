"""Prepara los sprites de Peakingu a partir de las imágenes de Gemini.
Uso (desde la raíz del proyecto):  python preparar_assets.py
Requiere:  pip install pillow numpy scipy
"""
from pathlib import Path
import sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

RAW = Path("Assets/Sprites/Raw")
OUT = Path("Assets/Sprites/Clean")
LADO_TILE = 128        # tamaño en px de cada tile (PPU 128 => 1 tile = 1 unidad)
ESCALA_DECOR = 0.35    # reducción de la decoración (1 = tamaño original)


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
            y0b = max(0, y1 - H) if fi == 0 else y0     # la fila superior es más baja
            x0b = max(0, x1 - W) if ci == 0 else x0
            t = rgba.crop((x0b + M, y0b + M, x0b + W - M, y0b + H - M))
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
        reducir(Image.fromarray(sub, "RGBA"), escala=ESCALA_DECOR).save(d / f"{nombre}.png")

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


if __name__ == "__main__":
    suelo(); hielo(); decoracion(); fondo()
    print("Listo. Vuelve a Unity para que importe Assets/Sprites/Clean.")