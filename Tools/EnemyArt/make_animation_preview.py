"""Build a contact-sheet GIF that previews every enemy locomotion loop."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--frames-root", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()

    enemy_dirs = sorted(path for path in args.frames_root.iterdir() if path.is_dir())
    cell_size = 256
    label_height = 24
    columns = 3
    rows = (len(enemy_dirs) + columns - 1) // columns
    previews = []

    for frame_index in range(4):
        canvas = Image.new("RGBA", (columns * cell_size, rows * (cell_size + label_height)), (24, 25, 32, 255))
        draw = ImageDraw.Draw(canvas)
        for index, enemy_dir in enumerate(enemy_dirs):
            x = index % columns * cell_size
            y = index // columns * (cell_size + label_height)
            frame = Image.open(enemy_dir / "Locomotion" / f"{frame_index:02d}.png").convert("RGBA")
            canvas.alpha_composite(frame, (x, y + label_height))
            draw.text((x + 8, y + 5), enemy_dir.name, fill=(245, 245, 245, 255))
        previews.append(canvas.convert("P", palette=Image.Palette.ADAPTIVE))

    args.out.parent.mkdir(parents=True, exist_ok=True)
    previews[0].save(args.out, save_all=True, append_images=previews[1:], duration=100, loop=0, disposal=2)
    print(args.out)


if __name__ == "__main__":
    main()
