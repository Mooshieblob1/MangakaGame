"""Shared AVIF encoder for development screenshots (requires Pillow with AVIF)."""
import os
from io import BytesIO
from pathlib import Path
import sys
import tempfile

from PIL import Image, features


def encode(source, destination):
    if not features.check("avif"):
        raise RuntimeError("Screenshot capture requires Pillow with AVIF support.")
    destination = Path(destination)
    destination.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with Image.open(source) as original:
            original.load()
            image = original.convert("RGBA" if "A" in original.getbands() else "RGB")
            with tempfile.NamedTemporaryFile(dir=destination.parent, suffix=".avif", delete=False) as stream:
                temporary = Path(stream.name)
            # Full chroma resolution keeps small coloured UI text sharp.
            image.save(temporary, format="AVIF", quality=85, subsampling="4:4:4", speed=6, max_threads=2)
            with Image.open(temporary) as verified:
                verified.load()
                if verified.format != "AVIF" or verified.size != image.size:
                    raise RuntimeError("Encoded screenshot failed validation.")
            os.replace(temporary, destination)
            return image.size
    finally:
        if temporary is not None and temporary.exists():
            temporary.unlink()


if __name__ == "__main__":
    # Fully buffer the pipe: image decoders expect seekable input and complete reads.
    encode(BytesIO(sys.stdin.buffer.read()), sys.argv[1])
