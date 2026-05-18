# 🤖 opencode-pro

**GitHub App bot powered by OpenCode AI** — Mention, assign, and slash-command your way to automated code reviews, fixes, and explanations.

[![Node.js](https://img.shields.io/badge/node-%3E%3D22-brightgreen)](https://nodejs.org)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.7-blue)](https://www.typescriptlang.org)
[![Probot](https://img.shields.io/badge/Probot-13.4-purple)](https://probot.github.io)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

---

## ✨ Características

| Feature | Descripción |
|---------|-------------|
| 🗣️ **@Mentions** | Invoca al bot mencionando `@opencode-pro` en cualquier comentario |
| 📋 **Asignación** | Asigna el bot a issues o PRs y se ejecuta automáticamente |
| 🔧 **Slash Commands** | `/review`, `/fix`, `/explain`, `/test` en Pull Requests |
| 🔍 **Auto-Review** | Revisa automáticamente nuevos PRs y actualizaciones |
| 🧠 **OpenCode AI** | Usa el motor de IA de OpenCode para todas las tareas |
| ⚡ **Sin /oc** | No necesitas prefijos — solo menciona al bot o usa comandos |

---

## 🚀 Inicio Rápido

### Para Usuarios

1. **Instala la app** en tu cuenta/organización desde [GitHub Marketplace](#)
2. **Agrega el workflow** a tu repo: copia `opencode-pro.yml` a `.github/workflows/`
3. **Configura el secret** `AZURE_OPENAI_API_KEY` en tu repo
4. **¡Listo!** Menciona `@opencode-pro` en cualquier issue o PR

### Comandos Disponibles

| Comando | Acción | Dónde usarlo |
|---------|--------|-------------|
| `@opencode-pro <mensaje>` | El bot responde a tu mensaje | Issues y PRs |
| `/review` | Revisa el código del PR | Solo en PRs |
| `/fix` | Analiza y corrige problemas | Solo en PRs |
| `/explain` | Explica qué hace el PR | Solo en PRs |
| `/test` | Escribe pruebas para los cambios | Solo en PRs |
| Asignar `@opencode-pro[bot]` | El bot procesa la tarea automáticamente | Issues y PRs |

---

## 🏗️ Para Desarrolladores

### Requisitos

- Node.js >= 22
- npm
- Una GitHub App registrada
- API key de Azure OpenAI (o proveedor compatible)

### Instalación Local

```bash
git clone https://github.com/anomalyco/opencode.git
cd opencode/opencode-pro

npm install
cp .env.example .env
# Edita .env con tus credenciales

npm run dev
```

### Estructura del Proyecto

```
opencode-pro/
├── src/
│   ├── index.ts              # Entry point del servidor
│   ├── app.ts                # Configuración de Probot
│   ├── config.ts             # Carga de variables de entorno
│   ├── types.ts              # Tipos TypeScript
│   ├── handlers/             # Manejadores de webhooks
│   │   ├── installation.ts   # Instalación/desinstalación
│   │   ├── issue-comment.ts  # @mentions y slash commands
│   │   ├── issues.ts         # Asignación de issues
│   │   └── pull-request.ts   # Auto-review y asignación de PRs
│   ├── opencode/             # Integración con OpenCode
│   │   ├── prompt.ts         # Composición de prompts
│   │   └── runner.ts         # Ejecutor de tareas OpenCode
│   └── github/               # Utilidades de GitHub API
│       └── client.ts
├── opencode-pro.yml          # Workflow de GitHub Actions
├── app.yml                   # Manifiesto de GitHub App
├── GUIDE.md                  # Guía de despliegue completa
├── .env.example              # Plantilla de variables de entorno
├── package.json
└── tsconfig.json
```

### Scripts

```bash
npm run build    # Compila TypeScript
npm run dev      # Desarrollo con hot-reload
npm start        # Producción
npm test         # Ejecuta tests
npm run lint     # Linting
```

---

## 📚 Documentación

- **[GUIDE.md](GUIDE.md)** — Guía completa de despliegue desde cero
- **[app.yml](app.yml)** — Manifiesto para crear la GitHub App
- **[opencode-pro.yml](opencode-pro.yml)** — Workflow para repositorios

---

## 🔗 Basado en

- [OpenCode](https://github.com/anomalyco/opencode) — El motor de IA original
- [Probot](https://probot.github.io) — Framework para GitHub Apps
- [Octokit](https://github.com/octokit) — Cliente de GitHub API

---

## 📄 Licencia

MIT — Ver [LICENSE](LICENSE) para más detalles.