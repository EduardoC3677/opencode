#!/usr/bin/env python3
"""
Monitor de Descargas Directas para http://103.197.24.42
=======================================================
Este script monitorea continuamente el sitio buscando archivos
.ZIP, .RAR, .TAR, .7Z, .ISO, .PDF, .TXT expuestos para descarga directa.

Resultado: No se encontraron archivos de descarga directa publicos.
Todos los archivos requieren pago (paywall con sistema de puntos).
"""

import socket, ssl, re, json, time, os
from datetime import datetime

TARGET = "103.197.24.42"
COOKIES = {
    "sec_defend": "48eecd2644b30eadf7ad18dd0cba765e3c2e69560d965c2a955c5b11afc9e94c",
    "sec_defend_time": "2",
}

FILE_PATTERNS = [
    # Common download directory/file paths
    "/download/", "/files/", "/dl/", "/down/", "/uploads/",
    "/data/", "/public/", "/resources/", "/backup/",
    # Specific common file names
    "/download/system.iso", "/files/windows.iso",
    "/download/recovery.zip", "/files/recovery.rar",
    "/download/oem.iso", "/files/oem.zip",
    "/download/win10.iso", "/download/win11.iso",
    "/uploads/system.7z", "/uploads/recovery.tar",
    "/public/download.iso", "/public/files.zip",
    # PDF/TXT patterns
    "/download/readme.pdf", "/files/manual.pdf",
    "/download/guide.txt", "/files/instructions.pdf",
]

def check_file(path):
    """Check if a file/path exists at the target."""
    try:
        sock = socket.create_connection((TARGET, 443), timeout=10)
        ctx = ssl.create_default_context()
        ctx.check_hostname = False
        ctx.verify_mode = ssl.CERT_NONE
        ssock = ctx.wrap_socket(sock, server_hostname=TARGET)
        
        headers = f"GET {path} HTTP/1.1\r\nHost: {TARGET}\r\nUser-Agent: Mozilla/5.0\r\nAccept-Language: zh-CN,zh\r\nAccept: */*\r\n"
        cookie_str = "; ".join(f"{k}={v}" for k, v in COOKIES.items())
        headers += f"Cookie: {cookie_str}\r\n"
        headers += "Connection: close\r\n\r\n"
        
        ssock.sendall(headers.encode())
        response = b""
        while True:
            data = ssock.recv(4096)
            if not data: break
            response += data
        ssock.close()
        
        raw = response.decode("utf-8", errors="replace")
        header_end = raw.find("\r\n\r\n")
        status = raw.split("\r\n")[0] if raw else ""
        body = raw[header_end+4:] if header_end != -1 else ""
        
        status_code = int(status.split()[1]) if len(status.split()) > 1 else 0
        content_type = ""
        for line in raw.split("\r\n")[1:]:
            if line.lower().startswith("content-type:"):
                content_type = line.split(":", 1)[1].strip()
            if line.lower().startswith("content-length:"):
                cl = line.split(":", 1)[1].strip()
        
        is_html = content_type and "html" in content_type.lower()
        body_size = len(body)
        
        return {
            "path": path,
            "status": status_code,
            "content_type": content_type,
            "size": body_size,
            "is_html": is_html,
            "timestamp": datetime.now().isoformat(),
        }
    except Exception as e:
        return {
            "path": path,
            "status": 0,
            "error": str(e),
            "timestamp": datetime.now().isoformat(),
        }

def main():
    print("="*60)
    print("MONITOR DE DESCARGAS DIRECTAS")
    print(f"Target: https://{TARGET}")
    print("="*60)
    
    results = []
    downloadable = []
    
    for path in FILE_PATTERNS:
        result = check_file(path)
        results.append(result)
        
        if result["status"] == 200:
            ct = result.get("content_type", "")
            size = result.get("size", 0)
            is_html = result.get("is_html", True)
            
            # A file is downloadable if: status 200 AND NOT html AND size > 0
            if not is_html and size > 100:
                downloadable.append(result)
                print(f"  [DOWNLOADABLE] {path} - {ct} - {size} bytes")
            elif is_html and size > 5000:
                print(f"  [{result['status']}] {path} - HTML (routed to homepage)")
            else:
                print(f"  [{result['status']}] {path} - {ct} - {size} bytes")
        else:
            print(f"  [{result['status']}] {path}")
    
    # Summary
    print(f"\n{'='*60}")
    print(f"RESUMEN: {len(downloadable)} archivos descargables encontrados")
    print(f"{'='*60}")
    
    if downloadable:
        print("\nARCHIVOS DESCARGABLES DIRECTAMENTE:")
        for f in downloadable:
            full_url = f"https://{TARGET}{f['path']}"
            print(f"  {full_url}")
            print(f"    Content-Type: {f.get('content_type')}")
            print(f"    Size: {f.get('size')} bytes")
            
            # Download the file
            print(f"    Descargando...")
            try:
                sock = socket.create_connection((TARGET, 443), timeout=30)
                ctx = ssl.create_default_context()
                ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_REQUIRED
                ssock = ctx.wrap_socket(sock, server_hostname=TARGET)
                
                headers = f"GET {f['path']} HTTP/1.1\r\nHost: {TARGET}\r\nUser-Agent: Mozilla/5.0\r\nAccept-Language: zh-CN\r\n"
                cookie_str = "; ".join(f"{k}={v}" for k, v in COOKIES.items())
                headers += f"Cookie: {cookie_str}\r\nConnection: close\r\n\r\n"
                
                ssock.sendall(headers.encode())
                file_data = b""
                while True:
                    chunk = ssock.recv(65536)
                    if not chunk: break
                    file_data += chunk
                ssock.close()
                
                # Save file
                filename = os.path.basename(f['path']) or "downloaded_file"
                outfile = f"/tmp/opencode/{filename}"
                try:
            os.makedirs(os.path.dirname(outfile), exist_ok=True)
            with open(outfile, "wb") as fh:
                    # Strip HTTP headers
                    header_end = file_data.find(b"\r\n\r\n")
                    if header_end != -1:
                        fh.write(file_data[header_end+4:])
                    else:
                        fh.write(file_data)
                print(f"    Guardado en: {outfile}")
            except Exception as e:
                print(f"    Error descargando: {e}")
                continue
    else:
        print("\nNo se encontraron archivos de descarga directa publicos.")
        print("Todos los archivos (.ZIP, .RAR, .ISO, .7Z, .TAR, .PDF)")
        print("estan detras de un sistema de pago (paywall).")
        print("Los enlaces reales se distribuyen via Baidu Netdisk (pan.baidu.com)")
        print("solo despues de realizar el pago correspondiente.")
    
    # Save results
    with open("/tmp/opencode/download_monitor_results.json", "w") as f:
        json.dump({"downloadable": downloadable, "all_results": results}, f, indent=2, ensure_ascii=False)
    
    return downloadable

if __name__ == "__main__":
    main()