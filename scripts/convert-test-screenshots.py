"""Convert test screenshots in place; preserve saved-career artwork fixtures."""
import concurrent.futures
import importlib.util
import json
from pathlib import Path


REPO = Path(__file__).resolve().parents[1]
ROOT = (REPO / "TestResults").resolve()
spec = importlib.util.spec_from_file_location("screenshot_avif", REPO / "godot/Tools/screenshot_avif.py")
encoder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(encoder)


def convert(path):
    source = path.resolve()
    target = source.with_suffix(".avif")
    if not source.is_relative_to(ROOT) or not target.is_relative_to(ROOT):
        raise ValueError(f"Screenshot path leaves TestResults: {path}")
    if target.exists():
        raise FileExistsError(f"Keep both files for review: {target}")
    before = source.stat().st_size
    size = encoder.encode(source, target)
    after = target.stat().st_size
    # encode fully decodes and verifies the output before atomically publishing it.
    source.unlink()
    return {"path": str(target.relative_to(ROOT)), "width": size[0], "height": size[1], "png_bytes": before, "avif_bytes": after}


if __name__ == "__main__":
    candidates = sorted(ROOT.rglob("*.png"))
    screenshots = [p for p in candidates if "art" not in p.relative_to(ROOT).parts]
    results = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        for result in pool.map(convert, screenshots):
            results.append(result)
            if len(results) % 25 == 0:
                print(f"Verified and converted {len(results)}/{len(screenshots)} screenshots", flush=True)
    report = {"converted": results, "preserved_artwork_fixtures": len(candidates)-len(screenshots),
              "png_bytes": sum(r["png_bytes"] for r in results), "avif_bytes": sum(r["avif_bytes"] for r in results)}
    (ROOT / "screenshot-avif-conversion.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Converted {len(results)} screenshots: {report['png_bytes']:,} -> {report['avif_bytes']:,} bytes. Preserved {report['preserved_artwork_fixtures']} artwork fixtures.")
