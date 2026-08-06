"""Split the generated 4x3 item atlas into normalized square Unity sprites."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Tools/ItemArt/ItemAtlas.png"
OUTPUT = ROOT / "Assets/Art/ItemIcons/Generated"
NAMES = (
    "Dice",
    "Bow",
    "Sword",
    "Crossbow",
    "Cannon",
    "FireBook",
    "FrostStone",
    "LightningOrb",
    "Anvil",
    "Hourglass",
    "Shield",
)
OUTPUT_SIZE = 256
PADDING = 14


def main() -> None:
    atlas = Image.open(SOURCE).convert("RGBA")
    x_edges = [round(atlas.width * column / 4) for column in range(5)]
    y_edges = [round(atlas.height * row / 3) for row in range(4)]

    for index, name in enumerate(NAMES):
        row, column = divmod(index, 4)
        cell = atlas.crop(
            (x_edges[column], y_edges[row], x_edges[column + 1], y_edges[row + 1])
        )
        alpha_bounds = cell.getchannel("A").getbbox()
        if alpha_bounds is None:
            raise RuntimeError(f"Cell {index} ({name}) has no visible pixels")

        item = cell.crop(alpha_bounds)
        max_extent = OUTPUT_SIZE - 2 * PADDING
        scale = min(max_extent / item.width, max_extent / item.height)
        new_size = (
            max(1, round(item.width * scale)),
            max(1, round(item.height * scale)),
        )
        item = item.resize(new_size, Image.Resampling.LANCZOS)

        sprite = Image.new("RGBA", (OUTPUT_SIZE, OUTPUT_SIZE), (0, 0, 0, 0))
        position = (
            (OUTPUT_SIZE - item.width) // 2,
            (OUTPUT_SIZE - item.height) // 2,
        )
        sprite.alpha_composite(item, position)
        sprite.save(OUTPUT / f"{name}.png", optimize=True)

        corner_alpha = [
            sprite.getpixel((0, 0))[3],
            sprite.getpixel((OUTPUT_SIZE - 1, 0))[3],
            sprite.getpixel((0, OUTPUT_SIZE - 1))[3],
            sprite.getpixel((OUTPUT_SIZE - 1, OUTPUT_SIZE - 1))[3],
        ]
        coverage = sum(1 for value in sprite.getchannel("A").get_flattened_data() if value > 8)
        print(f"{name}: {new_size[0]}x{new_size[1]}, coverage={coverage}, corners={corner_alpha}")


if __name__ == "__main__":
    main()
