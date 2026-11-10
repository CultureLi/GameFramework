import os
import struct
import zlib

PNG_SIGNATURE = b'\x89PNG\r\n\x1a\n'
CHUNK_TYPE = b'ziP7'


def read_chunks(data):
    offset = 8
    while offset < len(data):
        length = struct.unpack(">I", data[offset:offset + 4])[0]
        chunk_type = data[offset + 4:offset + 8]
        chunk_data = data[offset + 8:offset + 8 + length]
        crc = data[offset + 8 + length:offset + 12 + length]

        yield length, chunk_type, chunk_data, crc
        offset += length + 12


def make_chunk(chunk_type, chunk_data):
    length = struct.pack(">I", len(chunk_data))
    crc = zlib.crc32(chunk_type + chunk_data) & 0xffffffff
    return length + chunk_type + chunk_data + struct.pack(">I", crc)


def embed_one(png_path, zip_path, output_path):
    with open(png_path, "rb") as f:
        png_data = f.read()

    with open(zip_path, "rb") as f:
        zip_data = f.read()

    if not png_data.startswith(PNG_SIGNATURE):
        print(f"Skip (not PNG): {png_path}")
        return

    output = bytearray()
    output += PNG_SIGNATURE

    for length, chunk_type, chunk_data, crc in read_chunks(png_data):

        if chunk_type == b'IEND':
            output += make_chunk(CHUNK_TYPE, zip_data)

        output += struct.pack(">I", length)
        output += chunk_type
        output += chunk_data
        output += crc

    with open(output_path, "wb") as f:
        f.write(output)

    print(f"Embed OK -> {output_path}")


def main():
    files = os.listdir(".")

    png_files = [f for f in files if f.endswith(".png") and not f.endswith("_embed.png")]

    for png in png_files:
        base = os.path.splitext(png)[0]
        zip_file = base + ".7z"

        if not os.path.exists(zip_file):
            print(f"Skip (no zip): {base}")
            continue

        output_file = base + "_embed.png"

        embed_one(png, zip_file, output_file)


if __name__ == "__main__":
    main()