#!/usr/bin/env python3
"""
Acer Website Analyzer - Extrae HTML, CSS, JS, busca subdominios,
repositorios de factory media, recovery, ZIPs y documenta todo.
"""
import json
import os
import re
import sys
import time
from urllib.parse import urljoin, urlparse
from pathlib import Path

from bs4 import BeautifulSoup
from curl_cffi import requests as curl_requests

OUT_DIR = Path(os.path.dirname(os.path.abspath(__file__)))
DATA_FILE = OUT_DIR / "acer_analysis_data.json"
HTML_DIR = OUT_DIR / "html_pages"
CSS_DIR = OUT_DIR / "css_files"
JS_DIR = OUT_DIR / "js_files"

session = curl_requests.Session()
session.headers.update({
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
                  "AppleWebKit/537.36 (KHTML, like Gecko) "
                  "Chrome/125.0.0.0 Safari/537.36",
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
    "Accept-Language": "en-US,en;q=0.5",
})

visited_urls = set()
analysis = {
    "acer_com": {},
    "subdomains": [],
    "recovery_links": [],
    "zip_links": [],
    "factory_media": [],
    "support_links": [],
    "external_repos": [],
    "all_links": [],
    "css_resources": [],
    "js_resources": [],
}


def fetch_page(url, label=""):
    if url in visited_urls:
        return None
    visited_urls.add(url)
    try:
        print(f"[FETCH] {label or url}")
        resp = session.get(url, timeout=30, impersonate="chrome124")
        resp.raise_for_status()
        return resp
    except Exception as e:
        print(f"[ERROR] {url}: {e}")
        return None


def save_resource(content, filepath):
    filepath.parent.mkdir(parents=True, exist_ok=True)
    with open(filepath, "w", encoding="utf-8", errors="replace") as f:
        f.write(content)
    return filepath


def extract_resources(soup, base_url):
    resources = {"css": [], "js": []}
    for tag in soup.find_all("link", rel="stylesheet"):
        href = tag.get("href")
        if href:
            full_url = urljoin(base_url, href)
            resources["css"].append(full_url)
    for tag in soup.find_all("script", src=True):
        src = tag.get("src")
        if src:
            full_url = urljoin(base_url, src)
            resources["js"].append(full_url)
    return resources


def download_resource(url, save_dir, label=""):
    try:
        resp = session.get(url, timeout=15, impersonate="chrome124")
        if resp.status_code == 200 and len(resp.text) > 100:
            parsed = urlparse(url)
            filename = re.sub(r'[^\w\-\.]', '_', parsed.path.strip("/") or "index")
            if not filename.endswith(".css") and not filename.endswith(".js"):
                ext = ".css" if "css" in url else ".js"
                filename += ext
            filepath = save_dir / filename
            save_resource(resp.text, filepath)
            print(f"  [SAVED] {label}: {filepath.name} ({len(resp.text)} bytes)")
            return str(filepath)
    except Exception as e:
        print(f"  [SKIP] {url}: {e}")
    return None


def find_keywords(text, keywords):
    results = []
    for kw in keywords:
        for m in re.finditer(re.escape(kw), text, re.IGNORECASE):
            start = max(0, m.start() - 100)
            end = min(len(text), m.end() + 200)
            context = text[start:end].strip()
            results.append({"keyword": kw, "context": context[:300]})
    return results


def analyze_main_page():
    print("\n=== ANALIZANDO www.acer.com ===")
    resp = fetch_page("https://www.acer.com", "acer.com homepage")
    if not resp:
        return

    html = resp.text
    analysis["acer_com"]["html_size"] = len(html)
    analysis["acer_com"]["status_code"] = resp.status_code
    analysis["acer_com"]["headers"] = dict(resp.headers)

    soup = BeautifulSoup(html, "html.parser")
    resources = extract_resources(soup, "https://www.acer.com")
    analysis["css_resources"] = resources["css"]
    analysis["js_resources"] = resources["js"]

    # save HTML
    html_path = HTML_DIR / "acer_com_homepage.html"
    save_resource(html, html_path)
    analysis["acer_com"]["saved_html"] = str(html_path)

    # save CSS/JS
    for css_url in resources["css"][:20]:
        path = download_resource(css_url, CSS_DIR, "CSS")
        if path:
            analysis["acer_com"].setdefault("downloaded_css", []).append(path)

    for js_url in resources["js"][:20]:
        path = download_resource(js_url, JS_DIR, "JS")
        if path:
            analysis["acer_com"].setdefault("downloaded_js", []).append(path)

    # extract all links
    links = []
    for a in soup.find_all("a", href=True):
        href = a["href"]
        full = urljoin("https://www.acer.com", href)
        text = a.get_text(strip=True)[:100]
        links.append({"url": full, "text": text})
    analysis["all_links"] = links

    # keyword search
    recovery_kw = ["recovery", "factory", "reset", "restore", "download", "zip",
                   "driver", "support", "media creation", "acer recovery",
                   "acer care center", "acer eRecovery", "management"]
    found = find_keywords(html, recovery_kw)
    analysis["recovery_links"] = found

    # find ZIP links
    zip_links = []
    for a in soup.find_all("a", href=True):
        href = a["href"].lower()
        if href.endswith(".zip") or "zip" in href:
            zip_links.append({"url": urljoin("https://www.acer.com", a["href"]),
                              "text": a.get_text(strip=True)[:100]})
    analysis["zip_links"] = zip_links

    # find iframes
    iframes = []
    for iframe in soup.find_all("iframe", src=True):
        iframes.append(iframe["src"])
    analysis["acer_com"]["iframes"] = iframes

    print(f"  HTML: {len(html)} bytes")
    print(f"  Links encontrados: {len(links)}")
    print(f"  Recursos CSS: {len(resources['css'])}")
    print(f"  Recursos JS: {len(resources['js'])}")
    print(f"  Keywords recovery: {len(found)}")
    print(f"  ZIP links: {len(zip_links)}")


def analyze_support_page():
    print("\n=== ANALIZANDO PAGINA DE SOPORTE ===")
    url = "https://www.acer.com/us-en/support"
    resp = fetch_page(url, "acer support page")
    if not resp:
        url = "https://www.acer.com/ac/en/US/content/support"
        resp = fetch_page(url, "acer support alt")
    if not resp:
        return

    html = resp.text
    soup = BeautifulSoup(html, "html.parser")
    support_html_path = HTML_DIR / "acer_support.html"
    save_resource(html, support_html_path)

    # search for download/recovery links
    links = []
    for a in soup.find_all("a", href=True):
        href = a["href"]
        text = a.get_text(strip=True)
        full_url = urljoin(url, href)
        links.append({"url": full_url, "text": text[:100]})
        low = href.lower() + text.lower()
        if any(kw in low for kw in ["recovery", "factory", "download", "driver",
                                     "zip", "media", "restore", "backup"]):
            analysis["support_links"].append({
                "url": full_url,
                "text": text[:150],
                "page": "support"
            })
            print(f"  [SUPPORT LINK] {text[:60]} -> {full_url}")

    # keyword search
    found = find_keywords(html, ["recovery management", "acer recovery",
                                 "factory reset", "download center",
                                 "driver and manual", "recovery media",
                                 "zip download"])
    analysis["recovery_links"].extend(found)
    analysis["acer_com"]["support_html_size"] = len(html)


def analyze_subdomains():
    print("\n=== BUSCANDO SUBDOMINIOS DE ACER ===")
    known_subdomains = [
        "www.acer.com",
        "community.acer.com",
        "store.acer.com",
        "support.acer.com",
        "account.acer.com",
        "eshop.acer.com",
        "blog.acer.com",
        "investor.acer.com",
        "news.acer.com",
        "careers.acer.com",
        "laptop.acer.com",
        "predator.acer.com",
        "aspire.acer.com",
        "landing.acer.com",
        "config.acer.com",
        "driver.acer.com",
        "download.acer.com",
        "ftp.acer.com",
        "ftp2.acer.com",
        "global.acer.com",
        "id.acer.com",
        "login.acer.com",
        "my.acer.com",
        "partner.acer.com",
        "parts.acer.com",
        "registration.acer.com",
        "service.acer.com",
        "static.acer.com",
        "status.acer.com",
        "api.acer.com",
        "cdn.acer.com",
        "images.acer.com",
        "assets.acer.com",
    ]

    results = []
    for sub in known_subdomains:
        url = f"https://{sub}"
        try:
            resp = session.get(url, timeout=10, impersonate="chrome124",
                               allow_redirects=True)
            results.append({
                "subdomain": sub,
                "url": url,
                "status": resp.status_code,
                "final_url": str(resp.url),
                "title": "",
                "accessible": True,
                "redirect": str(resp.url) != url
            })
            print(f"  [{resp.status_code}] {sub}")
            # try to get title
            if "text/html" in resp.headers.get("content-type", ""):
                s = BeautifulSoup(resp.text, "html.parser")
                title_tag = s.find("title")
                if title_tag:
                    results[-1]["title"] = title_tag.get_text(strip=True)[:100]
                    print(f"    Title: {results[-1]['title']}")
        except Exception as e:
            results.append({
                "subdomain": sub,
                "url": url,
                "status": 0,
                "accessible": False,
                "error": str(e)[:100]
            })
            print(f"  [ERR] {sub}: {str(e)[:60]}")

    analysis["subdomains"] = results

    # Try DNS resolution for more subdomains
    print("\n=== RESOLVIENDO DNS PARA SUBDOMINIOS COMUNES ===")
    common_prefixes = [
        "ftp", "download", "support", "driver", "recovery", "backup",
        "files", "repo", "repository", "mirror", "cdn", "static",
        "assets", "media", "images", "api", "dev", "test", "stage",
        "beta", "portal", "help", "service", "parts", "spare",
        "manual", "manuals", "doc", "docs", "wiki", "kb",
        "config", "shop", "store", "eshop", "partner",
        "login", "account", "my", "id", "auth", "sso",
        "mail", "email", "webmail", "owa",
        "vpn", "remote", "access",
        "status", "monitor", "health",
        "jobs", "careers", "hr",
        "news", "blog", "community", "forum",
        "investor", "ir", "corporate",
        "landing", "event", "promo",
        "app", "mobile",
        "s3", "bucket", "storage",
        "archive", "old", "legacy",
    ]
    dns_results = []
    for prefix in common_prefixes:
        sub = f"{prefix}.acer.com"
        try:
            result = os.system(f"dig +short {sub} A 2>/dev/null | head -1")
            # We'll use a simpler approach
            import socket
            try:
                ip = socket.getaddrinfo(sub, 443)[0][4][0]
                dns_results.append({"subdomain": sub, "ip": ip, "resolves": True})
                print(f"  [DNS OK] {sub} -> {ip}")
            except socket.gaierror:
                pass
        except:
            pass
    analysis["subdomains_dns"] = dns_results


def search_acer_cloud_repos():
    print("\n=== BUSCANDO REPOSITORIOS EN LA NUBE ===")
    # Known Acer cloud/repo URLs
    known_repos = [
        # Acer download centers
        "https://www.acer.com/ac/en/US/content/drivers",
        "https://www.acer.com/ac/en/US/content/support-product/",
        "https://global-download.acer.com/GDFiles/",
        "https://global-download.acer.com/",
        "https://download.acer.com/",
        "https://ftp.acer.com/",
        "https://support.acer.com/",
        # Acer recovery URLs (known patterns)
        "https://www.acer.com/ac/en/US/content/recovery",
        "https://www.acer.com/ac/en/US/content/erecovery",
        "https://www.acer.com/ac/en/US/content/windows-recovery",
        # Various known endpoints
        "https://www.acer.com/ac/en/US/content/drivers/",
        "https://www.acer.com/ac/en/US/content/software/",
        "https://www.acer.com/ac/en/US/content/utilities/",
    ]

    for url in known_repos:
        try:
            resp = session.get(url, timeout=15, impersonate="chrome124",
                               allow_redirects=True)
            if resp.status_code < 400:
                entry = {
                    "url": url,
                    "status": resp.status_code,
                    "final_url": str(resp.url),
                    "size": len(resp.text),
                    "title": "",
                }
                if "text/html" in resp.headers.get("content-type", ""):
                    s = BeautifulSoup(resp.text, "html.parser")
                    t = s.find("title")
                    if t:
                        entry["title"] = t.get_text(strip=True)[:100]
                analysis["external_repos"].append(entry)
                print(f"  [OK {resp.status_code}] {url}")
            else:
                print(f"  [{resp.status_code}] {url}")
        except Exception as e:
            print(f"  [ERR] {url}: {str(e)[:60]}")


def search_recovery_keywords():
    print("\n=== BUSQUEDA PROFUNDA DE RECOVERY/FACTORY MEDIA ===")
    search_urls = [
        "https://www.acer.com/ac/en/US/content/support",
        "https://www.acer.com/ac/en/US/content/drivers",
        "https://www.acer.com/ac/en/US/content/recovery-management",
        "https://www.acer.com/ac/en/US/content/acer-care-center",
        "https://community.acer.com/",
    ]

    all_keywords_found = []
    for url in search_urls:
        resp = fetch_page(url, f"search on {url}")
        if not resp:
            continue
        html = resp.text
        soup = BeautifulSoup(html, "html.parser")

        # Search page text
        page_text = soup.get_text(separator=" ", strip=True)

        patterns = [
            r'recovery\s*(media|management|drive|partition|disc|disk|usb|dvd)',
            r'factory\s*(reset|restore|default|settings|image)',
            r'acer\s*recovery\s*management',
            r'create\s*recovery',
            r'backup\s*recovery',
            r'download\s*(driver|manual|utility|software)',
            r'\.zip',
            r'recovery\.zip',
            r'factory\.img',
            r'media\s*creation',
            r'restore\s*(point|image|partition)',
        ]

        page_lower = page_text.lower()
        for pattern in patterns:
            for m in re.finditer(pattern, page_lower):
                start = max(0, m.start() - 60)
                end = min(len(page_lower), m.end() + 100)
                all_keywords_found.append({
                    "url": url,
                    "pattern": pattern,
                    "match": m.group(),
                    "context": page_lower[start:end].strip()[:200]
                })

        # look for links with keywords
        for a in soup.find_all("a", href=True):
            href = a["href"]
            text = a.get_text(strip=True)
            combined = (href + " " + text).lower()
            if any(kw in combined for kw in ["recovery", "factory", "download",
                                               "driver", ".zip", "backup",
                                               "restore", "media"]):
                full_url = urljoin(url, href)
                analysis["support_links"].append({
                    "url": full_url,
                    "text": text[:200],
                    "page": url
                })

    analysis["deep_search_keywords"] = all_keywords_found
    print(f"  Keywords encontrados en busqueda profunda: {len(all_keywords_found)}")


def check_acer_ftp():
    print("\n=== VERIFICANDO FTP DE ACER ===")
    ftp_urls = [
        "https://ftp.acer.com/",
        "https://ftp2.acer.com/",
        "ftp://ftp.acer.com/",
        "https://global-download.acer.com/",
    ]
    for url in ftp_urls:
        try:
            resp = session.get(url, timeout=10, impersonate="chrome124",
                               allow_redirects=True)
            print(f"  [{resp.status_code}] {url} ({(len(resp.text))} bytes)")
            if resp.status_code < 400:
                analysis["external_repos"].append({
                    "url": url,
                    "status": resp.status_code,
                    "size": len(resp.text),
                    "type": "ftp_or_repo"
                })
                # save listing
                fname = re.sub(r'[^\w]', '_', url.strip("/")) + ".html"
                save_resource(resp.text, HTML_DIR / fname)
        except Exception as e:
            print(f"  [ERR] {url}: {str(e)[:60]}")


def search_acer_gdfiles():
    print("\n=== EXPLORANDO GLOBAL-DOWNLOAD.ACER.COM ===")
    # GDFiles is Acer's official driver/recovery repository structure
    gd_urls = [
        "https://global-download.acer.com/GDFiles/",
        "https://global-download.acer.com/GDFiles/Document/",
        "https://global-download.acer.com/GDFiles/Driver/",
        "https://global-download.acer.com/GDFiles/Application/",
        "https://global-download.acer.com/GDFiles/Bios/",
        "https://global-download.acer.com/GDFiles/Utility/",
        "https://global-download.acer.com/GDFiles/Recovery/",
        "https://global-download.acer.com/GDFiles/Manual/",
    ]
    for url in gd_urls:
        resp = fetch_page(url, f"GDFiles: {url}")
        if not resp:
            continue
        soup = BeautifulSoup(resp.text, "html.parser")
        # Check for directory listing or links
        links = [a.get("href") for a in soup.find_all("a", href=True) if a.get("href")]
        if links:
            print(f"  {len(links)} enlaces encontrados en {url}")
            # Check for ZIP/DOC/IMG files
            file_links = [l for l in links if any(l.lower().endswith(ext)
                          for ext in ['.zip', '.exe', '.iso', '.img', '.pdf'])]
            if file_links:
                print(f"  Archivos descargables: {len(file_links)}")
                for fl in file_links[:20]:
                    full_fl = urljoin(url, fl)
                    analysis["zip_links"].append({
                        "url": full_fl,
                        "text": fl,
                        "source": url
                    })
                    print(f"    {full_fl}")
        fname = re.sub(r'[^\w]', '_', url.strip("/")) + ".html"
        save_resource(resp.text, HTML_DIR / fname)
        analysis["external_repos"].append({
            "url": url,
            "status": resp.status_code,
            "size": len(resp.text),
            "links_count": len(links) if links else 0,
            "type": "GDFiles"
        })


def main():
    print("=" * 70)
    print("  ACER WEBSITE ANALYSIS - HTML/CSS/JS/SUBDOMAINS/RECOVERY")
    print("=" * 70)

    for d in [HTML_DIR, CSS_DIR, JS_DIR]:
        d.mkdir(parents=True, exist_ok=True)

    analyze_main_page()
    analyze_support_page()
    analyze_subdomains()
    search_acer_cloud_repos()
    search_recovery_keywords()
    check_acer_ftp()
    search_acer_gdfiles()

    # Save all analysis data
    with open(DATA_FILE, "w", encoding="utf-8") as f:
        json.dump(analysis, f, indent=2, ensure_ascii=False, default=str)

    # Summary
    print("\n" + "=" * 70)
    print("  RESUMEN DEL ANALISIS")
    print("=" * 70)
    print(f"  HTML guardado: {HTML_DIR}")
    print(f"  CSS guardados: {CSS_DIR} ({len(analysis.get('css_resources', []))} encontrados)")
    print(f"  JS guardados: {JS_DIR} ({len(analysis.get('js_resources', []))} encontrados)")
    print(f"  Subdominios analizados: {len(analysis.get('subdomains', []))}")
    print(f"  Links recovery encontrados: {len(analysis.get('recovery_links', []))}")
    print(f"  Links ZIP encontrados: {len(analysis.get('zip_links', []))}")
    print(f"  Links de soporte: {len(analysis.get('support_links', []))}")
    print(f"  Repositorios externos: {len(analysis.get('external_repos', []))}")
    print(f"  Keywords en busqueda profunda: {len(analysis.get('deep_search_keywords', []))}")
    print(f"  Datos completos guardados en: {DATA_FILE}")
    print("=" * 70)
    return analysis


if __name__ == "__main__":
    main()