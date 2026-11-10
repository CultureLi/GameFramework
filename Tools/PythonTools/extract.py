import os
import struct

PNG_SIGNATURE = b'\x89PNG\r\n\x1a\n'
CHUNK_TYPE = b'ziP7'


def read_chunks(data):
    offset = 8
    while offset < len(data):
        length = struct.unpack(">I", data[offset:offset + 4])[0]
        chunk_type = data[offset + 4:offset + 8]
        chunk_data = data[offset + 8:offset + 8 + length]

        yield length, chunk_type, chunk_data
        offset += length + 12


def extract_one(png_path, output_path):
    with open(png_path, "rb") as f:
        data = f.read()

    if not data.startswith(PNG_SIGNATURE):
        print(f"Skip (not PNG): {png_path}")
        return

    for length, chunk_type, chunk_data in read_chunks(data):

        if chunk_type == CHUNK_TYPE:

            with open(output_path, "wb") as f:
                f.write(chunk_data)

            print(f"Extract OK -> {output_path}")
            return

    print(f"No chunk found: {png_path}")


def main():
    files = os.listdir(".")

    png_files = [f for f in files if f.endswith("_embed.png")]

    for png in png_files:
        base = png.replace("_embed.png", "")
        output = base + "_extract.7z"

        extract_one(png, output)


if __name__ == "__main__":
    main()