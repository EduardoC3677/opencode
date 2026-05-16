# Análisis profundo: sitios chinos de OEM "factory image" para Acer

Sesión opencode #4 — 2026-05-14

Páginas analizadas:

1. `https://www.zhyxz.cn/1936.html` — Acer Aspire AN515-58
2. `https://www.dhzxt.cn/12336.html` — Acer AN515-58 Win11
3. `https://www.oemxitong.com/more/Acer/1207.html` — Acer AN515-58-90U2 Win11 22H2 (mencionado en sesión #3)

Tarea recibida: "analiza las páginas, su HTML/JS/CSS/PHP/etc, y busca la forma de descargar el sistema/uso sin login ni otros requisitos".

**Veredicto ejecutivo**: las 3 páginas son **paywall puro server-side**. El enlace de descarga real (a pan.baidu, quark, 123pan, aliyundrive, etc.) está almacenado en la base de datos y **nunca se envía al cliente** hasta que el back-end PHP valida (a) cookie de sesión de un usuario logueado y (b) saldo / pago consumido. **No existe un endpoint sin login que devuelva la URL**. Sí hay (i) endpoints AJAX que se pueden invocar sin sesión pero solo devuelven QR de pago externo (xunhupay), (ii) bypasses estructurales con coste (registro + ¥45 a xunhupay), y (iii) caché de motor que **no** ha indexado los enlaces (filtran shortcode/template).

A continuación, el detalle técnico por sitio.

---

## 1. zhyxz.cn — WordPress + tema *ceomax-pro* + "ceoshop" propio

### Stack identificado

```
WordPress 6.x
Tema: ceomax-pro (de pukmemao / mobantu — comercial)
Plugin pago: integrado en el tema, no es erphpdown
Pasarela: ceoshop (modal propio)
```

Activos visitados:
- HTML: `https://www.zhyxz.cn/1936.html` (`docs/acer-update/oem-china-sites/zhyxz_1936.html`, 80 KB)
- JS principal de compra: `https://www.zhyxz.cn/wp-content/themes/ceomax-pro/ceoshop/assets/js/product.js` (`zhyxz_product.js`)
- Otros: `member.js`, `ceoshop.js`, `ajax.js`

### Flujo de pago (analizado en `product.js`)

Botón visible:
```html
<a href="javascript:void(0)" data-product-id="1936" data-flush="1"
   class="makeFunc z1 btn-ceo-purchase-product" data-style="slide-down">
   立即下载
</a>
```

Hooks JS:
- `purchaseProductClick()` (líneas 1–417) → al click hace GET a:
  ```
  POST /wp-admin/admin-ajax.php
    action=ceo_shop_pay_product
    product_id=1936
    download=1
  ```
- El servidor renderiza un modal con: SKU, precio, cupones, "VIP discount".
- Al "comprar" hace:
  ```
  POST /wp-admin/admin-ajax.php
    action=ceo_shop_pay_product
    product_id=1936
    nonce=<...>
    sku=<...>
    coupon=<...>
  ```
- Tras éxito, llama:
  ```
  POST /wp-admin/admin-ajax.php
    action=ceo_shop_pay_product_download
    product_id=1936
    sku=<...>
  ```
  Que **es el que renderiza el enlace de descarga real**.

### Test sin login

```bash
curl -s -X POST \
  -H "Referer: https://www.zhyxz.cn/1936.html" \
  -H "X-Requested-With: XMLHttpRequest" \
  "https://www.zhyxz.cn/wp-admin/admin-ajax.php?action=ceo_shop_pay_product_download&product_id=1936"
```

Respuesta:
```json
{"success":false,"data":"未登录，无权下载！"}
```

(Sin login, sin permiso de descarga.)

### Vectores probados — todos cerrados

| Vector | Resultado |
|---|---|
| `wp-json/wp/v2/posts/1936` (REST API) | 200, pero el shortcode/template responsable del link se renderiza con condicional PHP `if(is_user_logged_in() && user_can_download($post))`, así que el `content.rendered` **no contiene el enlace**. Solo se ve descripción + imágenes. |
| `wp-json/wp/v2/posts/1936?context=edit` | 401 — `rest_forbidden_context`. |
| Feed RSS (`/feed/`, `/1936.html/feed/`) | Filtra el shortcode también. |
| `?preview=true` | Idem, hay que estar autenticado como autor. |
| Cache Wayback Machine | sin snapshots. |
| Bing/DDG cache | Sin indexación del cuerpo (sitio reciente / robots.txt). |

### Lo que sí sigue funcionando sin login

- `GET /wp-admin/admin-ajax.php?action=ceo_shop_pay_product&product_id=1936&download=1` devuelve el HTML del modal de pago (precio: 45 点券 = 45 puntos).
- En el modal se ve que hay 3 botones:
  - `#submit-ceo-product` — comprar con saldo (requiere login + saldo).
  - `#submit-ceo-product2` — comprar VIP (requiere login + suscripción).
  - `#submit-ceo-product3` — **descargar** (solo aparece si `data-download == 1|2`, es decir, **ya comprado**).

### Precio efectivo

Precio: **45 puntos** (≈ ¥45 si compras puntos al 1:1).
Recargas: ver `/charge` o `/vip` del sitio. Hay descuentos VIP (5–8 折).

---

## 2. dhzxt.cn — WordPress + tema *ceomax* + plugin **erphpdown**

### Stack

```
WordPress 6.x
Tema: ceomax (versión antigua del mismo tpl ceomax-pro)
Plugin pago: erphpdown (de mobantu.com) — comercial, GPL violator
Pasarela: xunhupay (虎皮椒) appid=201906145747/8 — multi-canal Alipay+WeChat
```

Activos:
- HTML: `dhzxt_12336.html` (86 KB)
- JS de descarga: `erphpdown.js` de `/wp-content/plugins/erphpdown/static/`

### Flujo erphpdown

El HTML inyecta:
```html
<fieldset class="erphpdown erphpdown-default" id="erphpdown">
  <legend>资源下载</legend>
  此资源下载价格为<span class="erphpdown-price">45</span>点券，请先
  <a href="/user/login/" class="erphp-login-must">登录</a>
  <div class="erphpdown-tips">付款成功后，会自动弹出下载链接。<br>
</fieldset>

<script>window._ERPHPDOWN = {"uri":"...", "payment": "5", "wppay": "scan", "author": "mobantu"}</script>
```

Endpoints AJAX descubiertos en `erphpdown.js`:

| `action` | Función | Login? |
|---|---|---|
| `epd_wppay` | Inicia orden de pago, devuelve QR | **NO requiere login** |
| `epd_wppay_pay` | Polling de confirmación de pago | NO |
| `epd_index` | Comprar acceso (con puntos) | SÍ |
| `epd_see` | "Pase de visualización gratis" (cuota diaria) | SÍ |
| `epd_checkin` | Check-in diario (regala puntos) | SÍ |
| `epd_check_pan` | Verifica que el link de pan no está caído | SÍ |
| `epd_promo` | Aplica código promocional | NO (revela `{status:0,type:0,money:""}` ) |

### Test sin login

```bash
curl -s -X POST -H "Referer: https://www.dhzxt.cn/12336.html" \
  -d "action=epd_wppay&post_id=12336" \
  "https://www.dhzxt.cn/wp-admin/admin-ajax.php"
```

Respuesta REAL (sí responde sin login):
```json
{
  "status": 200,
  "price": "45.00",
  "code":  "https://api.xunhupay.com/payments/alipay/qrcode?id=20297941615&...&hash=...",
  "code2": "https://api.xunhupay.com/payments/wechat/qrcode_v3?id=20297941616&...&hash=...",
  "aliurl":"https://api.xunhupay.com/payments/alipay/index?id=20297941615&...&hash=...",
  "wxurl": "https://api.xunhupay.com/payments/wechat/index?id=20297941616&...&hash=...",
  "num":   "MD260514120209909461927",
  "minute":0
}
```

Es decir: cualquiera puede generar una orden de ¥45 a alipay/wechat sin estar registrado. Pero **el link de descarga sigue NO devuelto**. Hay que esperar a que `epd_wppay_pay` con el `order_num` devuelva `status:1` (lo cual solo ocurre cuando xunhupay confirma el pago al webhook PHP del sitio).

### Probado: endpoints que no autorizan sin login

```
epd_index       → HTTP 400 (rechazo)
epd_check_pan   → HTTP 400
epd_checkin     → HTTP 400
epd_buy_post    → HTTP 400 (no existe)
epd_user_buy    → HTTP 400 (no existe)
epd_orders      → HTTP 400 (no existe)
epd_pan         → HTTP 400 (no existe)
epd_recharge    → HTTP 400 (no existe)
epd_invite      → HTTP 400 (no existe)
```

### Bypasses NO funcionales (verificados)

- `wp-json/wp/v2/posts/12336` → 200 pero `content.rendered` filtra el shortcode `[erphpdown ...]` (su callback devuelve string vacío para no-logged).
- `wp-json/wp/v2/posts/12336?context=edit` → 401.
- `/?p=12336&preview=true` → idem cuerpo público.
- `/12336.html/feed/` → feed de comentarios, sin link.
- `/?s=12336` (búsqueda) → muestra link `pan.baidu.com/s/1zpM2VrLbF9xSw1SIqGFXMg?pwd=f1ic` pero **es el APP cliente del propio sitio**, no el sistema de fábrica (verificado: el link de baidu lleva al instalador de la app de dhzxt, no a la ISO de Acer).
- archive.org / webcache.googleusercontent.com / bing.cn → sin snapshots útiles.

### Lo único confirmado público en dhzxt

El enlace baidu visible en el footer de la página (`pan.baidu.com/s/1zpM2VrLbF9xSw1SIqGFXMg?pwd=f1ic`) corresponde a **"官方APP下载"** (descarga del cliente oficial del sitio, no del sistema de fábrica). Probado con `curl`: devuelve la página estándar de Baidu Pan pidiendo el código `f1ic`, contenido = app del cliente del sitio.

---

## 3. oemxitong.com — EmpireCMS 7.x

### Stack

```
EmpireCMS 7.x (帝国CMS) — PHP, GBK encoding
Skin: ecms011
Pasarela: puntos internos (`点数`) → enlace de pan
```

Activos:
- HTML: `oemxitong_1207.utf8.html` (convertido GBK→UTF-8)

### Flujo

Dos botones:
```html
<a class="button" target="_blank"
   href="https://www.oemxitong.com/e/DownSys/DownSoft/?classid=33&id=1207&pathid=0">
   普通下载
</a>
<a class="button" target="_blank"
   href="https://www.oemxitong.com/e/DownSys/DownSoft/?classid=33&id=1207&pathid=1">
   VIP下载 [2折]
</a>
```

Test sin login:
```bash
curl -s -L -H "Referer: ..." \
  "https://www.oemxitong.com/e/DownSys/DownSoft/?classid=33&id=1207&pathid=0"
```

Respuesta:
```html
<script>
  alert('您还没登录!');
  self.location.href='https://www.oemxitong.com/e/member/login/login.php?prt=1&from=...';
</script>
```

### Bypasses probados — todos fallan

| URL probada | Resultado |
|---|---|
| `/e/DownSys/DownSoft/?classid=33&id=1207&pathid=0&prt=1` | mismo alert |
| `/e/DownSys/DownSoft/?...&pathid=1` | mismo alert |
| `/e/DownSys/index.php?classid=33&id=1207` | 404 |
| `/e/DownSys/AdoDownSoft/index.php?...` | 404 |
| `/e/action/ShowInfo.php?classid=33&id=1207` | 200 — pero solo devuelve la misma página de descripción, sin URL del enlace. El campo `downpath`/`onclick` está oculto. |
| `/e/extend/` | 200 16 bytes (vacío). |

### Registro auto-bypass

Existe `/e/member/register/index.php?groupid=1&tobind=0&enews=ChRegister` → muestra el formulario. **Tiene captcha** (imagen en `/e/ShowKey/?v=reg`). Aún registrado, el grupo 1 (普通会员) no tiene puntos → no puede descargar.

Para obtener puntos hay que pagar via `/e/payapi/`.

### Vector EmpireCMS clásico

EmpireCMS tiene CVE históricos (CVE-2018-18086 SSRF, CVE-2022-3122 RCE en plugin `/e/admin/`). Ninguno aplica sin acceso de admin / no se puede usar éticamente.

---

## Resumen comparativo

| Sitio | CMS / Plugin | Endpoint AJAX sin login | Precio | ¿Link visible? |
|---|---|---|---|---|
| zhyxz.cn | WP + ceomax-pro | `ceo_shop_pay_product` (modal HTML) | 45 点券 | **No** (filtrado en template) |
| dhzxt.cn | WP + erphpdown | `epd_wppay` (QR xunhupay) | ¥45 | **No** (filtrado en shortcode `[erphpdown]`) |
| oemxitong.com | EmpireCMS 7 | ninguno (`alert+redirect`) | 点数 puntos | **No** (server side) |

## Conclusión: ¿se puede descargar sin login / sin requisitos?

**NO** por ninguna ruta directa. Los 3 sitios están diseñados específicamente para ocultar el link del recurso y solo entregarlo tras:

1. **Registro de cuenta** + (a) consumo de puntos (que se compran con dinero), o (b) suscripción VIP, o (c) check-in diario que genera puntos pero requiere muchos días para acumular 45.
2. **O pago directo de ¥45** (vía xunhupay → Alipay/WeChat) sin login (solo dhzxt.cn permite eso vía `epd_wppay`). Pero hay que esperar a que el webhook PHP confirme el pago, lo cual solo ocurre al pagar realmente.

### Lo único que sí se puede obtener sin login

| Cosa | Cómo |
|---|---|
| Descripción + screenshots del producto | `wp-json/wp/v2/posts/<id>` |
| Tamaño, formato del ISO, lista de drivers, modelo | Cuerpo HTML público |
| QR de pago Alipay/WeChat con order_num único | `POST admin-ajax.php?action=epd_wppay` en dhzxt |
| Link del APP cliente del sitio (dhzxt) | Visible en el HTML público, lleva a pan.baidu (`?pwd=f1ic`) — pero **no es el sistema de fábrica** |
| Link del cliente oficial Acer | `https://aluwsv2.acer.com/...` ya documentado en `docs/acer-update/README.md` |

### Vías legales / recomendadas para obtener un sistema de fábrica Acer

1. **Acer Live Updater (ALU) oficial** ya documentado en `docs/acer-update/README.md` — descarga drivers oficiales, no la imagen pero sí los paquetes individuales que la conforman.
2. **Acer Care Center → Recovery Management → Create Factory Default Backup** — herramienta legítima incluida en cualquier portátil Acer OEM (`AlaunchX.exe`). Crea la imagen de recovery en USB.
3. **`PreloadBackup.zip` en partición Acer OEM** (decompilado en este repo) — extraíble con `Acer.CareCenter.LiveUpdate.dll` cuando se ejecuta en el equipo de destino.
4. **Acer Customer Service** — al presentar S/N (`NXK6TAL019416025803400`) y comprobante, Acer envía un USB de recovery.

---

## Headers usados para los tests (curl)

```bash
-A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
-H "Referer: https://www.<dominio>/<ruta>.html"
-H "X-Requested-With: XMLHttpRequest"
-H "Accept: */*"
-c cookies.txt -b cookies.txt -L
```

Para reproducir las pruebas: ver `docs/acer-update/oem-china-sites/probe.sh` (generado abajo).

---

## Notas legales

Los 3 sitios distribuyen **imágenes OEM Win11 modificadas + drivers Acer firmados** sin licencia ni autorización de Acer ni Microsoft. Comprar y usar esas ISOs:

- Vulnera la EULA de Microsoft (las claves OEM están bloqueadas al SLIC de la mainboard del modelo concreto; las imágenes incluyen claves leak).
- Vulnera la EULA de Acer (los drivers son propiedad de Acer + redistributables limitados).
- Probable infringe la **Anti-Unfair Competition Law (PRC)** + ley china de propiedad intelectual sobre Acer.

Por eso, además de la imposibilidad técnica de bypassear el paywall sin pagar, **la vía recomendada sigue siendo Acer oficial** (Live Updater + Recovery Management). El RUN_LOG.md de la sesión #1 ya documenta todos los endpoints oficiales.
