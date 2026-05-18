# 🚀 Guía de Despliegue de opencode-pro

> Despliega tu propio bot de IA para GitHub en ~30 minutos

## 📋 Índice

1. [Requisitos Previos](#1-requisitos-previos)
2. [Crear la GitHub App](#2-crear-la-github-app)
3. [Configurar el Servidor](#3-configurar-el-servidor)
4. [Desplegar el Bot](#4-desplegar-el-bot)
5. [Configurar los Repositorios](#5-configurar-los-repositorios)
6. [Probar el Bot](#6-probar-el-bot)
7. [Solución de Problemas](#7-solución-de-problemas)
8. [Mantenimiento](#8-mantenimiento)

---

## 1. Requisitos Previos

### Cuentas y Servicios

- ✅ **Cuenta de GitHub** — [github.com](https://github.com)
- ✅ **Azure OpenAI** (o cualquier proveedor compatible con OpenCode) — API key con acceso a modelos de lenguaje
- ✅ **Servidor o plataforma de hosting** — Opciones:
  - [Railway](https://railway.app) (recomendado para empezar, $5/mes)
  - [Fly.io](https://fly.io) (gratis para proyectos pequeños)
  - [Render](https://render.com) (gratis para pruebas)
  - [DigitalOcean](https://digitalocean.com) (desde $6/mes)
  - Tu propio VPS (AWS EC2, Azure VM, etc.)

### Software Local

- ✅ **Node.js 22+** — [Descargar](https://nodejs.org)
- ✅ **Git** — [Descargar](https://git-scm.com)
- ✅ **npm** (viene con Node.js)

### Conocimientos

- Familiaridad básica con terminal/command line
- Conceptos básicos de Git y GitHub

---

## 2. Crear la GitHub App

### 2.1 Registrar la App

1. Ve a [github.com/settings/apps/new](https://github.com/settings/apps/new)
2. Completa el formulario:

| Campo | Valor |
|-------|-------|
| **GitHub App name** | `opencode-pro` (o el nombre que prefieras) |
| **Homepage URL** | URL de tu proyecto o repositorio |
| **Webhook URL** | `https://TU_DOMINIO.com/api/webhook` (temporalmente puedes poner cualquier URL) |
| **Webhook secret** | Genera uno: `openssl rand -hex 32` |

3. En **Permissions**, configura:

| Permiso | Nivel |
|---------|-------|
| **Issues** | Read & Write |
| **Pull requests** | Read & Write |
| **Contents** | Read-only |
| **Metadata** | Read-only |

4. En **Subscribe to events**, marca:
   - ✅ Issues
   - ✅ Issue comment
   - ✅ Pull request
   - ✅ Pull request review
   - ✅ Pull request review comment
   - ✅ Installation
   - ✅ Installation repositories

5. En **Where can this app be installed?**, selecciona **Any account**

6. Haz clic en **Create GitHub App**

### 2.2 Obtener las Credenciales

Después de crear la app, estarás en la página de configuración:

1. **App ID** — Anota el número (ej: `123456`)
2. **Client ID** — Anótalo (ej: `Iv1.abc123...`)
3. **Private Key** — Haz clic en **Generate a private key**, descarga el archivo `.pem`
4. **Client Secret** — Haz clic en **Generate a new client secret**

Guarda estos valores — los necesitarás para el archivo `.env`.

### 2.3 Instalar la App

1. Ve a **Install App** en la barra lateral
2. Selecciona la cuenta donde quieres instalar el bot
3. Elige si instalar en todos los repositorios o solo en algunos
4. Haz clic en **Install**

---

## 3. Configurar el Servidor

### 3.1 Clonar el Repositorio

```bash
git clone https://github.com/TU_USUARIO/opencode-pro.git
cd opencode-pro
```

### 3.2 Instalar Dependencias

```bash
npm install
```

### 3.3 Configurar Variables de Entorno

Copia el archivo de ejemplo y edítalo:

```bash
cp .env.example .env
```

Edita `.env` con tus credenciales:

```env
# === REQUIRED ===
APP_ID=123456
PRIVATE_KEY="-----BEGIN RSA PRIVATE KEY-----
TU_CLAVE_PRIVADA_COMPLETA_AQUI
-----END RSA PRIVATE KEY-----"
WEBHOOK_SECRET=tu-webhook-secret-generado

# === OPENCODE CONFIGURATION ===
OPENCODE_CONFIG_DIR=.opencode
AZURE_OPENAI_API_KEY=tu-azure-api-key
OPENCODE_MODEL=deepseek/deepseek-v4-pro

# === OPTIONAL ===
PORT=3000
LOG_LEVEL=info
TASK_TIMEOUT=900
```

> ⚠️ **IMPORTANTE**: La `PRIVATE_KEY` debe incluir los saltos de línea `\n`. Si copias del archivo `.pem`, reemplaza los saltos de línea reales con `\n`.

### 3.4 Configurar OpenCode

El bot necesita la configuración de OpenCode para funcionar. Copia la carpeta `.opencode` desde el repositorio base:

```bash
# Si tienes el repo de opencode
cp -r ../opencode/.opencode .opencode
```

O crea tu propio archivo `.opencode/opencode.jsonc` con la configuración de tu proveedor de IA.

### 3.5 Compilar el TypeScript

```bash
npm run build
```

### 3.6 Probar Localmente

```bash
npm run dev
```

Deberías ver:
```
🚀 Starting opencode-pro GitHub App...
   App ID: 123456
   Port: 3000
   Model: deepseek/deepseek-v4-pro
✅ opencode-pro is running on port 3000
```

---

## 4. Desplegar el Bot

### Opción A: Railway (Recomendado)

1. Ve a [railway.app](https://railway.app) y crea una cuenta
2. Conecta tu repositorio de GitHub
3. Crea un nuevo proyecto → **Deploy from GitHub repo**
4. Selecciona el repositorio `opencode-pro`
5. Railway detectará automáticamente el `package.json`
6. Agrega las variables de entorno en **Variables**:
   - `APP_ID`
   - `PRIVATE_KEY`
   - `WEBHOOK_SECRET`
   - `AZURE_OPENAI_API_KEY`
   - `OPENCODE_CONFIG_DIR`
7. Railway te dará un dominio público (ej: `opencode-pro.up.railway.app`)
8. **IMPORTANTE**: Actualiza la **Webhook URL** en tu GitHub App a:
   ```
   https://opencode-pro.up.railway.app/api/webhook
   ```

### Opción B: Fly.io

```bash
# Instalar flyctl
curl -L https://fly.io/install.sh | sh

# Iniciar sesión
fly auth login

# Lanzar la app
fly launch

# Configurar secretos
fly secrets set APP_ID=123456
fly secrets set PRIVATE_KEY="$(cat tu-clave.pem)"
fly secrets set WEBHOOK_SECRET=tu-secreto
fly secrets set AZURE_OPENAI_API_KEY=tu-api-key
fly secrets set OPENCODE_CONFIG_DIR=.opencode

# Desplegar
fly deploy
```

### Opción C: Render

1. Ve a [render.com](https://render.com)
2. Crea un nuevo **Web Service**
3. Conecta tu repositorio
4. Configura:
   - **Build Command**: `npm install && npm run build`
   - **Start Command**: `npm start`
5. Agrega las variables de entorno
6. Render te dará un dominio `.onrender.com`
7. Actualiza la Webhook URL en tu GitHub App

### Opción D: VPS (DigitalOcean, AWS EC2, etc.)

```bash
# En tu VPS
ssh tu-usuario@tu-servidor

# Instalar Node.js 22
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
sudo apt-get install -y nodejs

# Clonar el repo
git clone https://github.com/TU_USUARIO/opencode-pro.git
cd opencode-pro

# Instalar y construir
npm install
npm run build

# Configurar .env
nano .env  # Pega tus variables de entorno

# Instalar PM2 para mantener el proceso vivo
npm install -g pm2
pm2 start dist/index.js --name opencode-pro
pm2 save
pm2 startup

# Configurar Nginx como proxy reverso (opcional pero recomendado)
sudo apt-get install -y nginx

sudo nano /etc/nginx/sites-available/opencode-pro
```

Configuración de Nginx:
```nginx
server {
    listen 80;
    server_name tu-dominio.com;

    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/opencode-pro /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx

# Configurar SSL con Certbot (gratis)
sudo apt-get install -y certbot python3-certbot-nginx
sudo certbot --nginx -d tu-dominio.com
```

---

## 5. Configurar los Repositorios

### 5.1 Agregar el Workflow

En cada repositorio donde quieras usar opencode-pro, crea el archivo `.github/workflows/opencode-pro.yml`:

```bash
# En tu repositorio
mkdir -p .github/workflows
cp opencode-pro/opencode-pro.yml .github/workflows/
```

### 5.2 Configurar Secrets del Repositorio

En cada repositorio, ve a **Settings → Secrets and variables → Actions** y agrega:

| Secret | Valor |
|--------|-------|
| `AZURE_OPENAI_API_KEY` | Tu API key de Azure OpenAI |

### 5.3 Configurar OpenCode

Asegúrate de que el repositorio tenga la carpeta `.opencode/` con la configuración:

```bash
# Copiar desde el repo base
cp -r opencode-pro/.opencode .opencode
```

### 5.4 Instalar la GitHub App en el Repositorio

1. Ve a la página de tu GitHub App
2. **Install App** → Selecciona el repositorio
3. Confirma la instalación

---

## 6. Probar el Bot

### 6.1 Verificar que el Bot Responde

Crea un issue de prueba y menciona al bot:

```
@opencode-pro Hola, ¿puedes explicar qué hace este repositorio?
```

El bot debería:
1. Reaccionar con 👀 al comentario
2. Procesar la tarea con OpenCode
3. Responder con un comentario

### 6.2 Probar Slash Commands en PRs

En un Pull Request, comenta:

```
/review
```

```
/fix
```

```
/explain
```

```
/test
```

### 6.3 Probar Asignación

1. Crea un issue
2. Asígnalo a `@opencode-pro[bot]`
3. El bot debería procesarlo automáticamente

### 6.4 Probar Auto-Review

1. Crea un Pull Request
2. El bot debería revisarlo automáticamente

---

## 7. Solución de Problemas

### El bot no responde

1. **Verifica los logs del servidor**:
   ```bash
   # Railway: Ve a la pestaña "Deployments" → "View Logs"
   # Fly.io: fly logs
   # PM2: pm2 logs opencode-pro
   ```

2. **Verifica la Webhook URL**:
   - Asegúrate de que la URL en la GitHub App apunte a tu servidor
   - La URL debe ser accesible públicamente
   - Debe terminar en `/api/webhook`

3. **Verifica las credenciales**:
   - `APP_ID` debe ser un número
   - `PRIVATE_KEY` debe incluir `-----BEGIN RSA PRIVATE KEY-----`
   - `WEBHOOK_SECRET` debe coincidir con el configurado en la GitHub App

### Error "Config file not found"

El workflow no encuentra `.opencode/opencode.jsonc`. Asegúrate de:
1. La carpeta `.opencode/` existe en la raíz del repositorio
2. Contiene `opencode.jsonc`
3. No está en `.gitignore`

### Error de API Key

1. Verifica que `AZURE_OPENAI_API_KEY` esté configurado como secret del repositorio
2. Verifica que la API key sea válida y tenga créditos
3. Prueba la API key directamente con curl

### El bot tarda mucho en responder

- Las tareas de IA pueden tomar varios minutos
- El timeout por defecto es 15 minutos
- Ajusta `TASK_TIMEOUT` en `.env` si es necesario

---

## 8. Mantenimiento

### Actualizar el Bot

```bash
# Pull de los últimos cambios
git pull origin main

# Reinstalar dependencias
npm install

# Recompilar
npm run build

# Reiniciar
# Railway/Fly.io: se reinicia automáticamente al hacer push
# PM2: pm2 restart opencode-pro
```

### Monitoreo

- **Railway**: Dashboard con métricas y logs
- **Fly.io**: `fly logs` y `fly status`
- **PM2**: `pm2 monit` para monitoreo en tiempo real

### Respaldo

Haz respaldo de:
- Archivo `.env` (credenciales)
- Carpeta `.opencode/` (configuración de IA)
- Private key de la GitHub App

---

## 📦 Resumen del Paquete

```
opencode-pro/
├── src/
│   ├── index.ts              # Entry point
│   ├── app.ts                # Probot app setup
│   ├── config.ts             # Environment config
│   ├── types.ts              # TypeScript types
│   ├── handlers/
│   │   ├── installation.ts   # App install/uninstall
│   │   ├── issue-comment.ts  # @mentions & slash commands
│   │   ├── issues.ts         # Issue assignment
│   │   └── pull-request.ts   # PR auto-review & assignment
│   ├── opencode/
│   │   ├── prompt.ts         # Prompt composition
│   │   └── runner.ts         # OpenCode process runner
│   └── github/
│       └── client.ts         # GitHub API utilities
├── opencode-pro.yml          # GitHub Actions workflow
├── app.yml                   # GitHub App manifest
├── .env.example              # Environment template
├── GUIDE.md                  # This guide
├── package.json
├── tsconfig.json
└── .gitignore
```

---

## 🔗 Enlaces Útiles

- [Documentación de GitHub Apps](https://docs.github.com/en/apps)
- [Documentación de Probot](https://probot.github.io/docs/)
- [Documentación de OpenCode](https://github.com/anomalyco/opencode)
- [Railway Docs](https://docs.railway.app)
- [Fly.io Docs](https://fly.io/docs)

---

¿Preguntas? Abre un issue en el repositorio de opencode-pro.