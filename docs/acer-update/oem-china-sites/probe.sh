#!/usr/bin/env bash
# Reproducir los tests del análisis sin login de los 3 sitios chinos OEM Acer.
# Uso: ./probe.sh
set -u
UA='Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'

echo "============================================================"
echo "[1] zhyxz.cn - WordPress + ceomax-pro + ceoshop"
echo "============================================================"
echo "[1.1] Página HTML (descripción + DOM del modal):"
curl -sL -A "$UA" "https://www.zhyxz.cn/1936.html" -o /tmp/z.html
wc -c /tmp/z.html

echo
echo "[1.2] Endpoint download directo SIN LOGIN -> debe responder 'no login':"
curl -s -X POST -A "$UA" \
  -H "Referer: https://www.zhyxz.cn/1936.html" \
  -H "X-Requested-With: XMLHttpRequest" \
  "https://www.zhyxz.cn/wp-admin/admin-ajax.php?action=ceo_shop_pay_product_download&product_id=1936"
echo

echo
echo "[1.3] REST API filtra el link de descarga (verificable):"
curl -s -A "$UA" "https://www.zhyxz.cn/wp-json/wp/v2/posts/1936" \
  | python3 -c "import sys,json; d=json.load(sys.stdin); print('CONTENT_LEN:',len(d['content']['rendered'])); print('HAS_PAN:','pan.baidu' in d['content']['rendered'] or 'aliyun' in d['content']['rendered'] or 'quark' in d['content']['rendered'])"

echo
echo "============================================================"
echo "[2] dhzxt.cn - WordPress + ceomax + erphpdown"
echo "============================================================"
echo "[2.1] HTML del post (45 yuan, login required):"
curl -sL -A "$UA" "https://www.dhzxt.cn/12336.html" -o /tmp/d.html
grep -oE 'erphpdown-price">[0-9]+' /tmp/d.html | head -1

echo
echo "[2.2] epd_wppay -- inicia orden de pago SIN LOGIN (devuelve QR alipay+wx):"
curl -s -X POST -A "$UA" \
  -H "Referer: https://www.dhzxt.cn/12336.html" \
  -d "action=epd_wppay&post_id=12336" \
  "https://www.dhzxt.cn/wp-admin/admin-ajax.php" \
  | python3 -m json.tool 2>/dev/null || true

echo
echo "[2.3] Otros endpoints epd_* sin login -> deben devolver 0/400 (no autorizado):"
for action in epd_index epd_see epd_check_pan epd_checkin epd_promo; do
  printf "  %-20s -> " "$action"
  curl -s -X POST -A "$UA" -d "action=$action&post_id=12336" \
    "https://www.dhzxt.cn/wp-admin/admin-ajax.php" -w "  [HTTP %{http_code}]\n"
done

echo
echo "[2.4] REST API filtra shortcode erphpdown:"
curl -s -A "$UA" "https://www.dhzxt.cn/wp-json/wp/v2/posts/12336" \
  | python3 -c "import sys,json; d=json.load(sys.stdin); c=d['content']['rendered']; print('CONTENT_LEN:',len(c)); print('HAS_PAN:','pan.baidu' in c or 'quark' in c or '123pan' in c or 'lanzou' in c)"

echo
echo "============================================================"
echo "[3] oemxitong.com - EmpireCMS 7"
echo "============================================================"
echo "[3.1] Endpoint descarga directa SIN LOGIN -> debe redirigir a login:"
curl -s -L -A "$UA" -H "Referer: https://www.oemxitong.com/more/Acer/1207.html" \
  "https://www.oemxitong.com/e/DownSys/DownSoft/?classid=33&id=1207&pathid=0"
echo
echo "[3.2] Variantes EmpireCMS:"
for path in \
  "/e/DownSys/DownSoft/?classid=33&id=1207&pathid=1" \
  "/e/DownSys/index.php?classid=33&id=1207" \
  "/e/action/ShowInfo.php?classid=33&id=1207" \
  ; do
  printf "  %-60s -> " "$path"
  curl -s -A "$UA" -o /dev/null -w "[HTTP %{http_code}, %{size_download} B]\n" "https://www.oemxitong.com$path"
done

echo
echo "============================================================"
echo "CONCLUSION"
echo "============================================================"
echo "Ningun endpoint devuelve la URL de descarga sin sesion autenticada."
echo "Unica via sin login: pagar 45 yuan via xunhupay (epd_wppay en dhzxt.cn)."
echo "Vias oficiales Acer documentadas en docs/acer-update/README.md."
