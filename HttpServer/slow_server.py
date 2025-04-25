import time
from http.server import HTTPServer, SimpleHTTPRequestHandler

class ThrottledHandler(SimpleHTTPRequestHandler):
    def copyfile(self, source, outputfile):
        """Override to throttle output"""
        bufsize = 8192  # 每次读 8KB
        while True:
            buf = source.read(bufsize)
            if not buf:
                break
            outputfile.write(buf)
            outputfile.flush()
            time.sleep(0.0005)  # 每 8KB 等 50ms => 限速大约 160 KB/s

if __name__ == '__main__':
    HOST = '10.23.50.187'  # 👉 改成你自己机器的局域网 IP
    PORT = 7888

    httpd = HTTPServer((HOST, PORT), ThrottledHandler)
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\n🛑 Server stopped.")
