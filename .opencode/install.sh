#!/usr/bin/env bash
set -euo pipefail

BOLD="\033[1m"
GREEN="\033[32m"
YELLOW="\033[33m"
CYAN="\033[36m"
RESET="\033[0m"

log()  { echo -e "${GREEN}[+]${RESET} ${BOLD}$*${RESET}"; }
warn() { echo -e "${YELLOW}[!]${RESET} $*"; }
info() { echo -e "${CYAN}[i]${RESET} $*"; }

OS="$(uname -s)"
PLATFORM="$(uname -m)"

# ---------------------------------------------------------------------------
# 1. Detect package manager & install system deps
# ---------------------------------------------------------------------------
install_system_deps() {
  log "Installing system dependencies..."

  case "$(uname -s)" in
    Linux)
      if command -v apt-get &>/dev/null; then
        sudo apt-get update -qq
        sudo apt-get install -y curl git python3 2>/dev/null || true
      elif command -v dnf &>/dev/null; then
        sudo dnf install -y curl git python3
      elif command -v pacman &>/dev/null; then
        sudo pacman -S --noconfirm curl git python
      fi
      ;;
    Darwin)
      if ! command -v brew &>/dev/null; then
        warn "Homebrew not found. Install it from https://brew.sh"
        exit 1
      fi
      brew install curl git python3 gh
      ;;
  esac
}

# ---------------------------------------------------------------------------
# 2. Install Node.js via nvm
# ---------------------------------------------------------------------------
install_node() {
  if command -v node &>/dev/null; then
    info "Node.js already installed: $(node --version)"
    return
  fi

  log "Installing Node.js via nvm..."
  export NVM_DIR="$HOME/.nvm"
  if [ ! -s "$NVM_DIR/nvm.sh" ]; then
    curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.1/install.sh | bash
  fi
  [ -s "$NVM_DIR/nvm.sh" ] && . "$NVM_DIR/nvm.sh"
  nvm install --lts
  nvm alias default lts/*
}

# ---------------------------------------------------------------------------
# 3. Install OpenCode
# ---------------------------------------------------------------------------
install_opencode() {
  if command -v opencode &>/dev/null; then
    info "OpenCode already installed: $(opencode --version 2>/dev/null || echo 'ok')"
    return
  fi

  log "Installing OpenCode..."
  curl -fsSL https://opencode.ai/install | bash
}

# ---------------------------------------------------------------------------
# 4. Install LSP servers
# ---------------------------------------------------------------------------
install_lsp_servers() {
  log "Installing LSP servers..."

  export NVM_DIR="$HOME/.nvm"
  [ -s "$NVM_DIR/nvm.sh" ] && . "$NVM_DIR/nvm.sh"

  # TypeScript/JavaScript
  npm install -g typescript-language-server typescript

  # Bash + Python
  npm install -g bash-language-server pyright

  # Java (Eclipse JDTLS + JDK)
  if ! command -v java &>/dev/null || java -version 2>&1 | grep -q "21"; then
    log "Installing JDK 21..."
    mkdir -p ~/.local/java
    curl -sL --max-time 300 \
      "https://api.adoptium.net/v3/binary/latest/21/ga/linux/x64/jdk/hotspot/normal/eclipse?project=jdk" \
      -o /tmp/jdk21.tar.gz
    tar xzf /tmp/jdk21.tar.gz -C ~/.local/java
    rm /tmp/jdk21.tar.gz
  fi

  # Find JDK path
  JDK_DIR=$(ls -d ~/.local/java/jdk-* 2>/dev/null | sort -V | tail -1)

  if [ ! -d ~/.local/jdtls ]; then
    log "Installing Eclipse JDTLS..."
    mkdir -p ~/.local/jdtls
    curl -sL --max-time 300 \
      "https://download.eclipse.org/jdtls/snapshots/jdt-language-server-latest.tar.gz" \
      -o /tmp/jdtls.tar.gz
    tar xzf /tmp/jdtls.tar.gz -C ~/.local/jdtls
    rm /tmp/jdtls.tar.gz

    # FIX: Ensure the target directory for the wrapper exists
    mkdir -p ~/.local/bin

    # Create jdtls wrapper
    cat > ~/.local/bin/jdtls-wrapper.sh << 'JDTLS_WRAPPER'
#!/bin/bash
export JAVA_HOME="$(ls -d $HOME/.local/java/jdk-* 2>/dev/null | sort -V | tail -1)"
export PATH="$JAVA_HOME/bin:$HOME/.nvm/versions/node/*/bin:$PATH"
exec "$HOME/.local/jdtls/bin/jdtls" "$@"
JDTLS_WRAPPER
    chmod +x ~/.local/bin/jdtls-wrapper.sh
  fi
}

# ---------------------------------------------------------------------------
# 5. Install formatters
# ---------------------------------------------------------------------------
install_formatters() {
  log "Installing formatters..."

  export NVM_DIR="$HOME/.nvm"
  [ -s "$NVM_DIR/nvm.sh" ] && . "$NVM_DIR/nvm.sh"

  # Prettier (JS/TS/JSON/MD/HTML/CSS/YAML)
  npm install -g prettier

  # Ruff (Python)
  if ! command -v ruff &>/dev/null; then
    curl -sL --max-time 60 \
      "https://github.com/astral-sh/ruff/releases/latest/download/ruff-x86_64-unknown-linux-gnu.tar.gz" \
      -o /tmp/ruff.tar.gz
    tar xzf /tmp/ruff.tar.gz -C /tmp
    mkdir -p ~/.local/bin
    mv /tmp/ruff-x86_64-unknown-linux-gnu/ruff ~/.local/bin/ruff
    chmod +x ~/.local/bin/ruff
    rm -rf /tmp/ruff.tar.gz /tmp/ruff-x86_64-unknown-linux-gnu
  fi
}

# ---------------------------------------------------------------------------
# 6. Install GitHub CLI + MCP server
# ---------------------------------------------------------------------------
install_github_tools() {
  log "Installing GitHub tools..."

  export NVM_DIR="$HOME/.nvm"
  [ -s "$NVM_DIR/nvm.sh" ] && . "$NVM_DIR/nvm.sh"

  # GitHub CLI
  if ! command -v gh &>/dev/null; then
    log "Installing GitHub CLI..."
    GH_VERSION=$(curl -s https://api.github.com/repos/cli/cli/releases/latest | grep -oP '"tag_name":\s*"\K[^"]+')
    curl -sL --max-time 60 \
      "https://github.com/cli/cli/releases/download/${GH_VERSION}/gh_${GH_VERSION#v}_linux_amd64.tar.gz" \
      -o /tmp/gh.tar.gz
    tar xzf /tmp/gh.tar.gz -C /tmp
    mkdir -p ~/.local/bin
    cp /tmp/gh_${GH_VERSION#v}_linux_amd64/bin/gh ~/.local/bin/gh
    chmod +x ~/.local/bin/gh
    rm -rf /tmp/gh.tar.gz /tmp/gh_${GH_VERSION#v}_linux_amd64
  fi

  # GitHub MCP server
  npm install -g @modelcontextprotocol/server-github

  # Create wrapper
  cat > ~/.local/bin/github-mcp-wrapper.sh << 'GH_WRAPPER'
#!/bin/bash
export PATH="$HOME/.local/bin:$HOME/.nvm/versions/node/*/bin:$PATH"
GITHUB_TOKEN=$(gh auth token 2>/dev/null)
if [ -z "$GITHUB_TOKEN" ]; then
  GITHUB_TOKEN="${GITHUB_PERSONAL_ACCESS_TOKEN:-}"
fi
if [ -z "$GITHUB_TOKEN" ]; then
  echo "GitHub token not found. Run 'gh auth login' or set GITHUB_PERSONAL_ACCESS_TOKEN." >&2
  exit 1
fi
export GITHUB_PERSONAL_ACCESS_TOKEN="$GITHUB_TOKEN"
export GITHUB_TOKEN="$GITHUB_TOKEN"
exec mcp-server-github
GH_WRAPPER
  chmod +x ~/.local/bin/github-mcp-wrapper.sh
}

# ---------------------------------------------------------------------------
# 7. Install Spec Kit (SDD)
# ---------------------------------------------------------------------------
install_spec_kit() {
  log "Installing Spec Kit (Spec-Driven Development)..."

  if ! command -v uv &>/dev/null; then
    curl -LsSf https://astral.sh/uv/install.sh | sh
  fi

  export PATH="$HOME/.local/bin:$PATH"

  if ! command -v specify &>/dev/null; then
    SPEC_VERSION=$(curl -s https://api.github.com/repos/github/spec-kit/releases/latest | grep -oP '"tag_name":\s*"\K[^"]+')
    uv tool install specify-cli --from "git+https://github.com/github/spec-kit.git@${SPEC_VERSION}"
  fi
}

# ---------------------------------------------------------------------------
# 8. Link configuration files
# ---------------------------------------------------------------------------
link_config() {
  log "Linking configuration files..."

  SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

  mkdir -p ~/.config/opencode/agents ~/.config/opencode/skills ~/.config/opencode/commands

  # Copy config if not exists
  [ -f ~/.config/opencode/opencode.json ] || cp "$SCRIPT_DIR/../.opencode/opencode.json" ~/.config/opencode/opencode.json

  # Copy agents, skills, and commands
  cp -r "$SCRIPT_DIR/../.opencode/agents/." ~/.config/opencode/agents/

  if [ -d "$SCRIPT_DIR/../.opencode/skills" ]; then
    cp -r "$SCRIPT_DIR/../.opencode/skills/." ~/.config/opencode/skills/
  fi

  if [ -d "$SCRIPT_DIR/../.opencode/commands" ]; then
    cp -r "$SCRIPT_DIR/../.opencode/commands/." ~/.config/opencode/commands/
  fi
}

# ---------------------------------------------------------------------------
# 9. Ensure PATH
# ---------------------------------------------------------------------------
ensure_path() {
  local PROFILE=""
  for f in ~/.bashrc ~/.zshrc ~/.profile; do
    [ -f "$f" ] && PROFILE="$f" && break
  done

  if [ -z "$PROFILE" ]; then
    PROFILE=~/.bashrc
  fi

  local ADDITIONS=(
    'export NVM_DIR="$HOME/.nvm"'
    '[ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"'
    'export PATH="$HOME/.local/bin:$PATH"'
    'export JAVA_HOME="$(ls -d $HOME/.local/java/jdk-* 2>/dev/null | sort -V | tail -1)"'
    'export PATH="$JAVA_HOME/bin:$PATH"'
  )

  for line in "${ADDITIONS[@]}"; do
    if ! grep -qF "$line" "$PROFILE" 2>/dev/null; then
      echo "$line" >> "$PROFILE"
    fi
  done

  info "PATH updated in $PROFILE. Run 'source $PROFILE' or restart your shell."
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
echo ""
echo -e "${BOLD}${CYAN}╔══════════════════════════════════════════════╗${RESET}"
echo -e "${BOLD}${CYAN}║   OpenCode Full Setup — Sakata Edition      ║${RESET}"
echo -e "${BOLD}${CYAN}╚══════════════════════════════════════════════╝${RESET}"
echo ""

install_system_deps
install_node
install_opencode
install_lsp_servers
install_formatters
install_github_tools
install_spec_kit
link_config
ensure_path

echo ""
echo -e "${GREEN}${BOLD}✔ All done!${RESET}"
echo ""
echo -e "Next steps:"
echo -e "  1. ${BOLD}source ~/.bashrc${RESET} (or restart your shell)"
echo -e "  2. ${BOLD}gh auth login${RESET} (authenticate GitHub CLI)"
echo -e "  3. ${BOLD}opencode${RESET} (start coding)"
echo ""
echo -e "In each project, run ${BOLD}specify init --integration opencode${RESET}"
echo -e "to enable Spec-Driven Development."
echo ""
