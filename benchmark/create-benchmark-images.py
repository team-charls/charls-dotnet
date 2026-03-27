import numpy as np
from PIL import Image

def save_pgm_16bit(array: np.ndarray, path: str):
    height, width = array.shape
    header = f"P5\n{width} {height}\n65535\n".encode()
    with open(path, 'wb') as f:
        f.write(header)
        f.write(array.astype('>u2').tobytes())

size = 2048
y, x = np.mgrid[0:size, 0:size]

# 16-bit
gradient_16 = ((x + y) / (2 * size) * 65535).astype(np.uint16)
noise_16 = np.random.normal(0, 400, (size, size)).astype(np.int32)
img_16 = np.clip(gradient_16.astype(np.int32) + noise_16, 0, 65535).astype(np.uint16)

save_pgm_16bit(img_16, 'benchmark_16bit.pgm')
print("Saved benchmark_16bit.pgm")

# 8-bit
img_8 = (img_16 / 257).astype(np.uint8)

Image.fromarray(img_8, mode='L').save('benchmark_8bit.pgm')
print("Saved benchmark_8bit.pgm")