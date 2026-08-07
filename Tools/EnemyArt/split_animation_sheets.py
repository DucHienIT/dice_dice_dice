"""Split 2x2 transparent enemy sprite sheets into Unity-ready frame PNGs."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def split_sheet(sheet_path: Path, output_root: Path, frame_size: int) -> None:
    enemy_name = sheet_path.stem.removesuffix("-alpha")
    sheet = Image.open(sheet_path).convert("RGBA")
    cell_width = sheet.width // 2
    cell_height = sheet.height // 2
    output_dir = output_root / enemy_name / "Locomotion"
    output_dir.mkdir(parents=True, exist_ok=True)

    frame = 0
    for row in range(2):
        for column in range(2):
            left = column * cell_width
            top = row * cell_height
            right = sheet.width if column == 1 else left + cell_width
            bottom = sheet.height if row == 1 else top + cell_height
            image = sheet.crop((left, top, right, bottom))
            image = image.resize((frame_size, frame_size), Image.Resampling.LANCZOS)
            image.save(output_dir / f"{frame:02d}.png", optimize=True)
            frame += 1


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input-dir", type=Path, required=True)
    parser.add_argument("--output-root", type=Path, required=True)
    parser.add_argument("--frame-size", type=int, default=256)
    args = parser.parse_args()

    sheets = sorted(args.input_dir.glob("*-alpha.png"))
    if not sheets:
        raise SystemExit("No *-alpha.png sprite sheets found")
    for sheet in sheets:
        split_sheet(sheet, args.output_root, args.frame_size)
        print(f"Created 4 frames for {sheet.stem.removesuffix('-alpha')}")


if __name__ == "__main__":
    main()
