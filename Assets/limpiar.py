from pathlib import Path
import numpy as np
from PIL import Image

RAW = Path("Assets/Sprites/Raw")
CLEAN = Path("Assets/Sprites/Clean")
TOLERANCIA = 90                      # sube si queda verde, baja si se come partes del dibujo
SIN_RECORTE = ("tiles", "background")  # carpetas donde NO se recorta


def tiene_fondo_verde(a):
    esquinas = [a[0, 0], a[0, -1], a[-1, 0], a[-1, -1]]
    verdes = sum(1 for c in esquinas if c[1] > 180 and c[0] < 100 and c[2] < 100)
    return verdes >= 3


def procesar(origen: Path, destino: Path):
    img = Image.open(origen).convert("RGBA")
    a = np.array(img).astype(np.int16)

    if tiene_fondo_verde(a):
        r, g, b = a[..., 0], a[..., 1], a[..., 2]
        distancia = np.sqrt(r**2 + (g - 255) ** 2 + b**2)
        dominante = (g > r + 40) & (g > b + 40) & (g > 150)
        fondo = (distancia < TOLERANCIA) | dominante
        a[..., 3] = np.where(fondo, 0, 255)

        # quita el reflejo verde en los bordes de lo que queda
        tope = np.maximum(r, b)
        derrame = (~fondo) & (g > tope)
        a[..., 1] = np.where(derrame, tope, g)

    resultado = Image.fromarray(a.astype(np.uint8), "RGBA")

    ruta = str(origen).lower()
    if not any(c in ruta for c in SIN_RECORTE):
        caja = resultado.getbbox()
        if caja:
            resultado = resultado.crop(caja)

    destino.parent.mkdir(parents=True, exist_ok=True)
    resultado.save(destino)


def main():
    archivos = list(RAW.rglob("*.png"))
    if not archivos:
        print("No encontré PNG en", RAW)
        return
    for f in archivos:
        salida = CLEAN / f.relative_to(RAW)
        procesar(f, salida)
        print("OK:", salida)
    print("Listo,", len(archivos), "archivos.")


if __name__ == "__main__":
    main()