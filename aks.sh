#!/usr/bin/env bash

# Automatiza un despliegue pequeno de AKS + Fleet Manager + Nginx Proxy Manager
# + Portainer en eastus2. No se despliega un Docker Engine separado porque AKS
# usa containerd como runtime y Portainer se ejecuta en modo Kubernetes dentro
# del propio cluster. Forzar Docker-in-Docker o un daemon Docker privilegiado
# en AKS aumentaria el riesgo operativo y no aporta valor real en este caso.

set -Eeuo pipefail

usage() {
  cat <<'EOF'
Uso: ./aks.sh

Variables opcionales:
  TARGET_SUBSCRIPTION_ID   Fuerza la suscripcion de Azure a usar.
  LOCATION                 Region de despliegue. Por defecto: eastus2
  BILLING_ACCOUNT_ID       Billing account. Por defecto: 6a74355b-d0aa-4bfb-b194-3e8de6164ea6
  TENANT_ID                Tenant de Azure. Por defecto: 1427ba79-0e7a-4976-8aca-1e714957f671
  NAME_PREFIX              Prefijo comun para los recursos. Por defecto: aks-npm
  RESOURCE_GROUP           Nombre del resource group.
  CLUSTER_NAME             Nombre del cluster AKS.
  FLEET_NAME               Nombre del Fleet hub.
  NAMESPACE                Namespace de Kubernetes. Por defecto: edge-platform
  STORAGE_CLASS_NAME       StorageClass para PVCs. Por defecto: managed-csi

Autenticacion no interactiva soportada:
  AZURE_CLIENT_ID / ARM_CLIENT_ID
  AZURE_CLIENT_SECRET / ARM_CLIENT_SECRET
  AZURE_TENANT_ID / ARM_TENANT_ID
  AZURE_USE_MANAGED_IDENTITY=true
EOF
}

if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
  usage
  exit 0
fi

readonly LOCATION="${LOCATION:-eastus2}"
readonly BILLING_ACCOUNT_ID="${BILLING_ACCOUNT_ID:-6a74355b-d0aa-4bfb-b194-3e8de6164ea6}"
readonly TENANT_ID="${TENANT_ID:-1427ba79-0e7a-4976-8aca-1e714957f671}"
readonly NAME_PREFIX="${NAME_PREFIX:-aks-npm}"
readonly RESOURCE_GROUP="${RESOURCE_GROUP:-${NAME_PREFIX}-rg}"
readonly CLUSTER_NAME="${CLUSTER_NAME:-${NAME_PREFIX}-cluster}"
readonly FLEET_NAME="${FLEET_NAME:-${NAME_PREFIX}-fleet}"
readonly FLEET_MEMBER_NAME="${FLEET_MEMBER_NAME:-${CLUSTER_NAME}-member}"
readonly NAMESPACE="${NAMESPACE:-edge-platform}"
readonly SYSTEM_POOL_NAME="${SYSTEM_POOL_NAME:-sysnp}"
readonly USER_POOL_NAME="${USER_POOL_NAME:-appnp}"
readonly STORAGE_CLASS_NAME="${STORAGE_CLASS_NAME:-managed-csi}"
readonly ROLLOUT_TIMEOUT="${ROLLOUT_TIMEOUT:-20m}"
readonly LB_WAIT_TIMEOUT_SECONDS="${LB_WAIT_TIMEOUT_SECONDS:-1800}"
readonly POLL_INTERVAL_SECONDS="${POLL_INTERVAL_SECONDS:-15}"
readonly USER_POOL_MAX_CAP="${USER_POOL_MAX_CAP:-5}"

readonly MIN_NODE_VCPUS=2
readonly MIN_NODE_MEMORY_GB=4
readonly BASELINE_TOTAL_NODES=2
readonly PREFERRED_TOTAL_NODES=3
readonly TARGET_SYSTEM_NODE_VCPUS=2
readonly TARGET_USER_NODE_VCPUS=4
readonly PREFERRED_USER_NODES=$(( PREFERRED_TOTAL_NODES - 1 ))

readonly -a TAGS=(
  "managedBy=aks.sh"
  "workload=npm-portainer"
  "tenant=${TENANT_ID}"
  "billingAccount=${BILLING_ACCOUNT_ID}"
)

SUBSCRIPTION_ID=""
SYSTEM_POOL_FAMILY=""
SYSTEM_POOL_VM_SIZE=""
SYSTEM_POOL_VM_VCPUS=0
SYSTEM_POOL_VM_MEMORY_GB=0
USER_POOL_FAMILY=""
USER_POOL_VM_SIZE=""
USER_POOL_VM_VCPUS=0
USER_POOL_VM_MEMORY_GB=0
REGIONAL_CORES_AVAILABLE=0
SYSTEM_NODE_COUNT=1
USER_NODE_MIN_COUNT=1
USER_NODE_MAX_COUNT=1
NPM_ENDPOINT=""

log() {
  printf '[INFO] %s\n' "$*"
}

warn() {
  printf '[WARN] %s\n' "$*" >&2
}

fail() {
  printf '[ERROR] %s\n' "$*" >&2
  exit 1
}

on_error() {
  local line="$1"
  local exit_code="$2"
  fail "Fallo inesperado en la linea ${line} (codigo ${exit_code})."
}

trap 'on_error ${LINENO} $?' ERR

require_command() {
  local command_name="$1"
  command -v "$command_name" >/dev/null 2>&1 || fail "No se encontro el comando requerido: ${command_name}"
}

random_hex() {
  local length="$1"
  openssl rand -hex 32 | cut -c1-"${length}"
}

abs_int() {
  local value="$1"
  if (( value < 0 )); then
    printf '%s\n' "$(( -1 * value ))"
  else
    printf '%s\n' "$value"
  fi
}

floor_decimal_to_int() {
  local value="$1"
  printf '%s\n' "${value%%.*}"
}

min_int() {
  local left="$1"
  local right="$2"
  if (( left < right )); then
    printf '%s\n' "$left"
  else
    printf '%s\n' "$right"
  fi
}

score_vm_candidate() {
  local vcpus="$1"
  local memory_gb="$2"
  local target_vcpus="$3"
  local preferred_nodes="$4"
  local distance=0

  distance="$(abs_int $(( vcpus - target_vcpus )))"
  printf '%s\n' "$(( 1000 - (distance * 100) + (memory_gb * 3) + (preferred_nodes * 25) ))"
}

azure_cli_logged_in() {
  az account show --only-show-errors >/dev/null 2>&1
}

current_tenant() {
  az account show --query tenantId --output tsv --only-show-errors 2>/dev/null || true
}

ensure_azure_login() {
  local active_tenant=""
  local client_id="${AZURE_CLIENT_ID:-${ARM_CLIENT_ID:-}}"
  local client_secret="${AZURE_CLIENT_SECRET:-${ARM_CLIENT_SECRET:-}}"
  local explicit_tenant="${AZURE_TENANT_ID:-${ARM_TENANT_ID:-${TENANT_ID}}}"

  if azure_cli_logged_in; then
    active_tenant="$(current_tenant)"
    if [[ "$active_tenant" == "$TENANT_ID" ]]; then
      log "Azure CLI ya esta autenticado en el tenant solicitado: ${TENANT_ID}"
      return
    fi
    warn "Azure CLI tiene sesion activa en otro tenant (${active_tenant}); se intentara reautenticar."
  fi

  if [[ -n "$client_id" && -n "$client_secret" ]]; then
    log "Autenticando Azure CLI mediante service principal en modo no interactivo."
    az login \
      --service-principal \
      --username "$client_id" \
      --password "$client_secret" \
      --tenant "$explicit_tenant" \
      --allow-no-subscriptions \
      --only-show-errors \
      >/dev/null
    return
  fi

  if [[ "${AZURE_USE_MANAGED_IDENTITY:-false}" == "true" ]]; then
    log "Autenticando Azure CLI mediante managed identity."
    az login --identity --tenant "$TENANT_ID" --allow-no-subscriptions --only-show-errors >/dev/null
    return
  fi

  if [[ -t 0 ]]; then
    log "No hay credenciales no interactivas; se intentara az login sobre el tenant solicitado."
    az login --tenant "$TENANT_ID" --allow-no-subscriptions --only-show-errors >/dev/null
    return
  fi

  fail "No fue posible autenticar Azure CLI sin interaccion. Configure un service principal o una managed identity."
}

resolve_subscription_from_billing() {
  local subscription=""

  if [[ -n "${TARGET_SUBSCRIPTION_ID:-}" ]]; then
    printf '%s\n' "${TARGET_SUBSCRIPTION_ID}"
    return 0
  fi

  subscription="$(az billing subscription list --billing-account-name "$BILLING_ACCOUNT_ID" --query "[?subscriptionId!=null] | [0].subscriptionId" --output tsv --only-show-errors 2>/dev/null || true)"
  if [[ -n "$subscription" ]]; then
    printf '%s\n' "$subscription"
    return 0
  fi

  subscription="$(az billing subscription list --account-name "$BILLING_ACCOUNT_ID" --query "[?subscriptionId!=null] | [0].subscriptionId" --output tsv --only-show-errors 2>/dev/null || true)"
  if [[ -n "$subscription" ]]; then
    printf '%s\n' "$subscription"
    return 0
  fi

  subscription="$(az account list --all --query "[?tenantId=='${TENANT_ID}' && state=='Enabled'] | [0].id" --output tsv --only-show-errors)"
  if [[ -n "$subscription" ]]; then
    warn "No se pudo resolver una suscripcion directamente desde billing; se usara la primera suscripcion habilitada del tenant."
    printf '%s\n' "$subscription"
    return 0
  fi

  fail "No se encontro una suscripcion habilitada en el tenant ${TENANT_ID}."
}

select_subscription() {
  SUBSCRIPTION_ID="$(resolve_subscription_from_billing)"
  [[ -n "$SUBSCRIPTION_ID" ]] || fail "No se pudo determinar la suscripcion de trabajo."
  log "Usando la suscripcion ${SUBSCRIPTION_ID}"
  az account set --subscription "$SUBSCRIPTION_ID" --only-show-errors
}

ensure_kubectl() {
  if command -v kubectl >/dev/null 2>&1; then
    return
  fi

  log "kubectl no esta instalado; se instalara desde Azure CLI en ~/.local/bin"
  mkdir -p "${HOME}/.local/bin"
  az aks install-cli \
    --install-location "${HOME}/.local/bin/kubectl" \
    --kubelogin-install-location "${HOME}/.local/bin/kubelogin" \
    --only-show-errors \
    >/dev/null
  export PATH="${HOME}/.local/bin:${PATH}"
  command -v kubectl >/dev/null 2>&1 || fail "kubectl no quedo disponible tras la instalacion automatica."
}

ensure_extensions() {
  log "Instalando/actualizando extensiones requeridas de Azure CLI"
  az config set extension.use_dynamic_install=yes_without_prompt --only-show-errors >/dev/null
  az extension add --name aks-preview --upgrade --yes --only-show-errors >/dev/null
  az extension add --name fleet --upgrade --yes --only-show-errors >/dev/null
}

register_provider() {
  local namespace="$1"
  local attempts=36
  local state=""

  log "Registrando proveedor ${namespace}"
  az provider register --namespace "$namespace" --only-show-errors >/dev/null

  for ((i = 1; i <= attempts; i++)); do
    state="$(az provider show --namespace "$namespace" --query registrationState --output tsv --only-show-errors 2>/dev/null || true)"
    if [[ "$state" == "Registered" ]]; then
      log "Proveedor ${namespace} registrado"
      return
    fi
    sleep 10
  done

  fail "El proveedor ${namespace} no alcanzo estado Registered a tiempo."
}

register_required_providers() {
  register_provider "Microsoft.ContainerService"
  register_provider "Microsoft.Compute"
  register_provider "Microsoft.Network"
  register_provider "Microsoft.ManagedIdentity"
}

resource_group_exists() {
  local name="$1"
  [[ "$(az group exists --name "$name" --output tsv --only-show-errors)" == "true" ]]
}

ensure_resource_group() {
  if resource_group_exists "$RESOURCE_GROUP"; then
    log "El resource group ${RESOURCE_GROUP} ya existe"
    return
  fi

  log "Creando resource group ${RESOURCE_GROUP}"
  az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --tags "${TAGS[@]}" \
    --only-show-errors \
    >/dev/null
}

cluster_exists() {
  az aks show --resource-group "$RESOURCE_GROUP" --name "$CLUSTER_NAME" --only-show-errors >/dev/null 2>&1
}

fleet_exists() {
  az fleet show --resource-group "$RESOURCE_GROUP" --name "$FLEET_NAME" --only-show-errors >/dev/null 2>&1
}

fleet_member_exists() {
  az fleet member show \
    --resource-group "$RESOURCE_GROUP" \
    --fleet-name "$FLEET_NAME" \
    --name "$FLEET_MEMBER_NAME" \
    --only-show-errors \
    >/dev/null 2>&1
}

user_pool_exists() {
  az aks nodepool show \
    --resource-group "$RESOURCE_GROUP" \
    --cluster-name "$CLUSTER_NAME" \
    --name "$USER_POOL_NAME" \
    --only-show-errors \
    >/dev/null 2>&1
}

collect_compute_plan() {
  declare -A family_available=()

  local usage_lines=()
  local sku_lines=()
  local candidate_lines=()
  local limit=0
  local current=0
  local line=""
  local family=""
  local vm_size=""
  local available=0
  local vcpus=0
  local memory_raw=""
  local memory_gb=0
  local system_line=""
  local user_line=""
  local system_family=""
  local system_vm_size=""
  local system_vcpus=0
  local system_memory_gb=0
  local user_family=""
  local user_vm_size=""
  local user_vcpus=0
  local user_memory_gb=0
  local system_family_remaining=0
  local user_family_remaining=0
  local regional_remaining=0
  local raw_user_max_nodes=0
  local effective_user_max_nodes=0
  local preferred_user_nodes=0
  local system_score=0
  local user_score=0
  local pair_score=0
  local best_pair_score=-1
  local best_system_family=""
  local best_system_vm_size=""
  local best_system_vcpus=0
  local best_system_memory_gb=0
  local best_user_family=""
  local best_user_vm_size=""
  local best_user_vcpus=0
  local best_user_memory_gb=0
  local best_user_max_nodes=0

  log "Calculando cuota regional disponible en ${LOCATION}"
  limit="$(az vm list-usage --location "$LOCATION" --query "[?name.value=='cores'] | [0].limit" --output tsv --only-show-errors)"
  current="$(az vm list-usage --location "$LOCATION" --query "[?name.value=='cores'] | [0].currentValue" --output tsv --only-show-errors)"
  REGIONAL_CORES_AVAILABLE=$(( limit - current ))
  (( REGIONAL_CORES_AVAILABLE > 0 )) || fail "La cuota regional de cores en ${LOCATION} es insuficiente."
  log "vCPU regionales disponibles: ${REGIONAL_CORES_AVAILABLE}"

  mapfile -t usage_lines < <(az vm list-usage --location "$LOCATION" --query "[?contains(name.value, 'Family')].[name.value,limit,currentValue]" --output tsv --only-show-errors)
  ((${#usage_lines[@]} > 0)) || fail "No fue posible obtener cuotas por familia en ${LOCATION}."

  for line in "${usage_lines[@]}"; do
    IFS=$'\t' read -r family limit current <<<"$line"
    [[ -n "$family" ]] || continue
    [[ "$limit" =~ ^[0-9]+$ && "$current" =~ ^[0-9]+$ ]] || continue
    case "$family" in
      standardD*Family|standardE*Family) ;;
      *) continue ;;
    esac
    available=$(( limit - current ))
    if (( available > 0 )); then
      family_available["$family"]="$available"
    fi
  done

  mapfile -t sku_lines < <(az vm list-skus \
    --location "$LOCATION" \
    --resource-type virtualMachines \
    --query "[?restrictions==null || length(restrictions)==\`0\`].[family,name,capabilities[?name=='vCPUs'] | [0].value,capabilities[?name=='MemoryGB'] | [0].value]" \
    --output tsv \
    --only-show-errors)
  ((${#sku_lines[@]} > 0)) || fail "No fue posible obtener el catalogo de SKUs de VM en ${LOCATION}."

  for line in "${sku_lines[@]}"; do
    IFS=$'\t' read -r family vm_size vcpus memory_raw <<<"$line"
    [[ -n "$family" && -n "$vm_size" && -n "$vcpus" && -n "$memory_raw" ]] || continue
    [[ -n "${family_available[$family]:-}" ]] || continue

    memory_gb="$(floor_decimal_to_int "$memory_raw")"
    [[ "$vcpus" =~ ^[0-9]+$ && "$memory_gb" =~ ^[0-9]+$ ]] || continue
    (( vcpus >= MIN_NODE_VCPUS )) || continue
    (( memory_gb >= MIN_NODE_MEMORY_GB )) || continue
    (( family_available[$family] >= vcpus )) || continue
    (( REGIONAL_CORES_AVAILABLE >= vcpus + MIN_NODE_VCPUS )) || continue

    candidate_lines+=("${family}"$'\t'"${vm_size}"$'\t'"${vcpus}"$'\t'"${memory_gb}")
  done

  ((${#candidate_lines[@]} > 0)) || fail "No se encontro ningun SKU de VM elegible en ${LOCATION}."

  for system_line in "${candidate_lines[@]}"; do
    IFS=$'\t' read -r system_family system_vm_size system_vcpus system_memory_gb <<<"$system_line"
    system_family_remaining=$(( family_available[$system_family] - system_vcpus ))
    regional_remaining=$(( REGIONAL_CORES_AVAILABLE - system_vcpus ))
    (( system_family_remaining >= 0 )) || continue
    (( regional_remaining >= MIN_NODE_VCPUS )) || continue

    for user_line in "${candidate_lines[@]}"; do
      IFS=$'\t' read -r user_family user_vm_size user_vcpus user_memory_gb <<<"$user_line"

      if [[ "$system_family" == "$user_family" ]]; then
        user_family_remaining="$system_family_remaining"
      else
        user_family_remaining="${family_available[$user_family]}"
      fi

      (( user_family_remaining >= user_vcpus )) || continue
      raw_user_max_nodes="$(min_int "$(( user_family_remaining / user_vcpus ))" "$(( regional_remaining / user_vcpus ))")"
      (( raw_user_max_nodes >= (BASELINE_TOTAL_NODES - 1) )) || continue

      effective_user_max_nodes="$(min_int "$raw_user_max_nodes" "$USER_POOL_MAX_CAP")"
      preferred_user_nodes="$(min_int "$effective_user_max_nodes" "$PREFERRED_USER_NODES")"
      system_score="$(score_vm_candidate "$system_vcpus" "$system_memory_gb" "$TARGET_SYSTEM_NODE_VCPUS" "1")"
      user_score="$(score_vm_candidate "$user_vcpus" "$user_memory_gb" "$TARGET_USER_NODE_VCPUS" "$preferred_user_nodes")"
      pair_score=$(( (preferred_user_nodes * 1000000) + (effective_user_max_nodes * 10000) + (user_score * 10) + system_score - (system_vcpus * 100) ))

      if (( pair_score > best_pair_score )); then
        best_pair_score="$pair_score"
        best_system_family="$system_family"
        best_system_vm_size="$system_vm_size"
        best_system_vcpus="$system_vcpus"
        best_system_memory_gb="$system_memory_gb"
        best_user_family="$user_family"
        best_user_vm_size="$user_vm_size"
        best_user_vcpus="$user_vcpus"
        best_user_memory_gb="$user_memory_gb"
        best_user_max_nodes="$effective_user_max_nodes"
      fi
    done
  done

  [[ -n "$best_system_vm_size" && -n "$best_user_vm_size" ]] || fail "No se encontro una combinacion valida de familias/SKUs que permita al menos ${BASELINE_TOTAL_NODES} nodos en ${LOCATION}."

  SYSTEM_POOL_FAMILY="$best_system_family"
  SYSTEM_POOL_VM_SIZE="$best_system_vm_size"
  SYSTEM_POOL_VM_VCPUS="$best_system_vcpus"
  SYSTEM_POOL_VM_MEMORY_GB="$best_system_memory_gb"
  USER_POOL_FAMILY="$best_user_family"
  USER_POOL_VM_SIZE="$best_user_vm_size"
  USER_POOL_VM_VCPUS="$best_user_vcpus"
  USER_POOL_VM_MEMORY_GB="$best_user_memory_gb"

  SYSTEM_NODE_COUNT=1
  USER_NODE_MIN_COUNT=1
  if (( best_user_max_nodes >= PREFERRED_USER_NODES )); then
    USER_NODE_MIN_COUNT=$PREFERRED_USER_NODES
  fi

  USER_NODE_MAX_COUNT="$best_user_max_nodes"
  if (( USER_NODE_MAX_COUNT < USER_NODE_MIN_COUNT )); then
    USER_NODE_MAX_COUNT=$USER_NODE_MIN_COUNT
  fi

  log "Pool system seleccionado: ${SYSTEM_POOL_VM_SIZE} (${SYSTEM_POOL_FAMILY}, ${SYSTEM_POOL_VM_VCPUS} vCPU / ${SYSTEM_POOL_VM_MEMORY_GB} GiB)"
  log "Pool user seleccionado: ${USER_POOL_VM_SIZE} (${USER_POOL_FAMILY}, ${USER_POOL_VM_VCPUS} vCPU / ${USER_POOL_VM_MEMORY_GB} GiB)"
  log "Capacidad segura calculada: system=${SYSTEM_NODE_COUNT}, user-min=${USER_NODE_MIN_COUNT}, user-max=${USER_NODE_MAX_COUNT}"
}

ensure_fleet_hub() {
  if fleet_exists; then
    log "Fleet hub ${FLEET_NAME} ya existe"
    return
  fi

  log "Creando Azure Kubernetes Fleet Manager hub ${FLEET_NAME}"
  az fleet create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$FLEET_NAME" \
    --location "$LOCATION" \
    --enable-hub \
    --enable-managed-identity \
    --tags "${TAGS[@]}" \
    --only-show-errors \
    >/dev/null
}

ensure_aks_cluster() {
  if cluster_exists; then
    log "El cluster AKS ${CLUSTER_NAME} ya existe; no se recreara"
    return
  fi

  log "Creando cluster AKS ${CLUSTER_NAME}"
  az aks create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$CLUSTER_NAME" \
    --location "$LOCATION" \
    --nodepool-name "$SYSTEM_POOL_NAME" \
    --node-count "$SYSTEM_NODE_COUNT" \
    --node-vm-size "$SYSTEM_POOL_VM_SIZE" \
    --network-plugin azure \
    --network-plugin-mode overlay \
    --load-balancer-sku standard \
    --enable-managed-identity \
    --generate-ssh-keys \
    --tier standard \
    --tags "${TAGS[@]}" \
    --only-show-errors \
    >/dev/null
}

ensure_user_nodepool() {
  local existing_vm_size=""

  if user_pool_exists; then
    existing_vm_size="$(az aks nodepool show --resource-group "$RESOURCE_GROUP" --cluster-name "$CLUSTER_NAME" --name "$USER_POOL_NAME" --query vmSize --output tsv --only-show-errors)"
    if [[ "$existing_vm_size" != "$USER_POOL_VM_SIZE" ]]; then
      warn "El nodepool ${USER_POOL_NAME} ya existe con vmSize ${existing_vm_size}; no se migrara automaticamente."
    fi
    log "Actualizando autoscaler del nodepool ${USER_POOL_NAME}"
    az aks nodepool update \
      --resource-group "$RESOURCE_GROUP" \
      --cluster-name "$CLUSTER_NAME" \
      --name "$USER_POOL_NAME" \
      --enable-cluster-autoscaler \
      --min-count "$USER_NODE_MIN_COUNT" \
      --max-count "$USER_NODE_MAX_COUNT" \
      --only-show-errors \
      >/dev/null
    return
  fi

  log "Creando nodepool de aplicaciones ${USER_POOL_NAME}"
  az aks nodepool add \
    --resource-group "$RESOURCE_GROUP" \
    --cluster-name "$CLUSTER_NAME" \
    --name "$USER_POOL_NAME" \
    --mode User \
    --node-vm-size "$USER_POOL_VM_SIZE" \
    --node-count "$USER_NODE_MIN_COUNT" \
    --enable-cluster-autoscaler \
    --min-count "$USER_NODE_MIN_COUNT" \
    --max-count "$USER_NODE_MAX_COUNT" \
    --tags "${TAGS[@]}" \
    --only-show-errors \
    >/dev/null
}

ensure_fleet_member() {
  local cluster_resource_id=""

  if fleet_member_exists; then
    log "El cluster ya esta registrado como miembro en Fleet"
    return
  fi

  cluster_resource_id="$(az aks show --resource-group "$RESOURCE_GROUP" --name "$CLUSTER_NAME" --query id --output tsv --only-show-errors)"
  [[ -n "$cluster_resource_id" ]] || fail "No se pudo obtener el resource ID del cluster AKS."

  log "Registrando ${CLUSTER_NAME} como miembro del Fleet hub"
  az fleet member create \
    --resource-group "$RESOURCE_GROUP" \
    --fleet-name "$FLEET_NAME" \
    --name "$FLEET_MEMBER_NAME" \
    --member-cluster-id "$cluster_resource_id" \
    --only-show-errors \
    >/dev/null
}

connect_kubeconfig() {
  log "Obteniendo credenciales de kubectl para ${CLUSTER_NAME}"
  az aks get-credentials \
    --resource-group "$RESOURCE_GROUP" \
    --name "$CLUSTER_NAME" \
    --overwrite-existing \
    --only-show-errors \
    >/dev/null
}

apply_namespace() {
  kubectl apply -f - >/dev/null <<EOF
apiVersion: v1
kind: Namespace
metadata:
  name: ${NAMESPACE}
  labels:
    app.kubernetes.io/part-of: edge-platform
EOF
}

ensure_database_secret() {
  local root_password=""
  local app_password=""

  if kubectl -n "$NAMESPACE" get secret npm-db-secret >/dev/null 2>&1; then
    log "El secret npm-db-secret ya existe; se reutilizara para no invalidar el estado persistente"
    return
  fi

  root_password="$(random_hex 32)"
  app_password="$(random_hex 32)"

  kubectl -n "$NAMESPACE" create secret generic npm-db-secret \
    --from-literal=MARIADB_ROOT_PASSWORD="$root_password" \
    --from-literal=MARIADB_DATABASE=npm \
    --from-literal=MARIADB_USER=npm \
    --from-literal=MARIADB_PASSWORD="$app_password" \
    --dry-run=client \
    --output yaml | kubectl apply -f - >/dev/null
}

apply_workloads() {
  log "Aplicando manifiestos de Nginx Proxy Manager, MariaDB y Portainer"
  kubectl apply -f - <<EOF
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: npm-data
  namespace: ${NAMESPACE}
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: ${STORAGE_CLASS_NAME}
  resources:
    requests:
      storage: 8Gi
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: npm-letsencrypt
  namespace: ${NAMESPACE}
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: ${STORAGE_CLASS_NAME}
  resources:
    requests:
      storage: 8Gi
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: npm-mariadb
  namespace: ${NAMESPACE}
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: ${STORAGE_CLASS_NAME}
  resources:
    requests:
      storage: 8Gi
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: portainer-data
  namespace: ${NAMESPACE}
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: ${STORAGE_CLASS_NAME}
  resources:
    requests:
      storage: 5Gi
---
apiVersion: v1
kind: Service
metadata:
  name: npm-mariadb
  namespace: ${NAMESPACE}
spec:
  type: ClusterIP
  selector:
    app: npm-mariadb
  ports:
    - name: mysql
      port: 3306
      targetPort: 3306
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: npm-mariadb
  namespace: ${NAMESPACE}
spec:
  replicas: 1
  selector:
    matchLabels:
      app: npm-mariadb
  template:
    metadata:
      labels:
        app: npm-mariadb
    spec:
      nodeSelector:
        agentpool: ${USER_POOL_NAME}
      containers:
        - name: mariadb
          image: mariadb:11.4
          imagePullPolicy: IfNotPresent
          args:
            - --character-set-server=utf8mb4
            - --collation-server=utf8mb4_unicode_ci
          envFrom:
            - secretRef:
                name: npm-db-secret
          ports:
            - containerPort: 3306
              name: mysql
          resources:
            requests:
              cpu: 250m
              memory: 512Mi
            limits:
              cpu: 1000m
              memory: 1Gi
          readinessProbe:
            tcpSocket:
              port: 3306
            initialDelaySeconds: 15
            periodSeconds: 10
          livenessProbe:
            tcpSocket:
              port: 3306
            initialDelaySeconds: 30
            periodSeconds: 20
          volumeMounts:
            - name: data
              mountPath: /var/lib/mysql
      volumes:
        - name: data
          persistentVolumeClaim:
            claimName: npm-mariadb
---
apiVersion: v1
kind: Service
metadata:
  name: npm-lb
  namespace: ${NAMESPACE}
spec:
  type: LoadBalancer
  externalTrafficPolicy: Cluster
  selector:
    app: nginx-proxy-manager
  ports:
    - name: http
      port: 80
      targetPort: 80
    - name: admin
      port: 81
      targetPort: 81
    - name: https
      port: 443
      targetPort: 443
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: nginx-proxy-manager
  namespace: ${NAMESPACE}
spec:
  replicas: 1
  selector:
    matchLabels:
      app: nginx-proxy-manager
  template:
    metadata:
      labels:
        app: nginx-proxy-manager
    spec:
      nodeSelector:
        agentpool: ${USER_POOL_NAME}
      containers:
        - name: npm
          image: jc21/nginx-proxy-manager:latest
          imagePullPolicy: IfNotPresent
          env:
            - name: DB_MYSQL_HOST
              value: npm-mariadb
            - name: DB_MYSQL_PORT
              value: "3306"
            - name: DB_MYSQL_NAME
              valueFrom:
                secretKeyRef:
                  name: npm-db-secret
                  key: MARIADB_DATABASE
            - name: DB_MYSQL_USER
              valueFrom:
                secretKeyRef:
                  name: npm-db-secret
                  key: MARIADB_USER
            - name: DB_MYSQL_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: npm-db-secret
                  key: MARIADB_PASSWORD
          ports:
            - containerPort: 80
              name: http
            - containerPort: 81
              name: admin
            - containerPort: 443
              name: https
          resources:
            requests:
              cpu: 250m
              memory: 512Mi
            limits:
              cpu: 1000m
              memory: 1Gi
          readinessProbe:
            tcpSocket:
              port: 81
            initialDelaySeconds: 25
            periodSeconds: 10
          livenessProbe:
            tcpSocket:
              port: 81
            initialDelaySeconds: 40
            periodSeconds: 20
          volumeMounts:
            - name: data
              mountPath: /data
            - name: letsencrypt
              mountPath: /etc/letsencrypt
      volumes:
        - name: data
          persistentVolumeClaim:
            claimName: npm-data
        - name: letsencrypt
          persistentVolumeClaim:
            claimName: npm-letsencrypt
---
apiVersion: v1
kind: ServiceAccount
metadata:
  name: portainer-sa
  namespace: ${NAMESPACE}
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: portainer-${NAMESPACE}-cluster-admin
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: cluster-admin
subjects:
  - kind: ServiceAccount
    name: portainer-sa
    namespace: ${NAMESPACE}
---
apiVersion: v1
kind: Service
metadata:
  name: portainer
  namespace: ${NAMESPACE}
spec:
  type: ClusterIP
  selector:
    app: portainer
  ports:
    - name: http
      port: 9000
      targetPort: 9000
    - name: https
      port: 9443
      targetPort: 9443
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: portainer
  namespace: ${NAMESPACE}
spec:
  replicas: 1
  selector:
    matchLabels:
      app: portainer
  template:
    metadata:
      labels:
        app: portainer
    spec:
      serviceAccountName: portainer-sa
      nodeSelector:
        agentpool: ${USER_POOL_NAME}
      containers:
        - name: portainer
          image: portainer/portainer-ce:lts
          imagePullPolicy: IfNotPresent
          args:
            - --http-enabled
          ports:
            - containerPort: 9000
              name: http
            - containerPort: 9443
              name: https
          resources:
            requests:
              cpu: 200m
              memory: 256Mi
            limits:
              cpu: 750m
              memory: 768Mi
          readinessProbe:
            tcpSocket:
              port: 9000
            initialDelaySeconds: 20
            periodSeconds: 10
          livenessProbe:
            tcpSocket:
              port: 9000
            initialDelaySeconds: 30
            periodSeconds: 20
          volumeMounts:
            - name: data
              mountPath: /data
      volumes:
        - name: data
          persistentVolumeClaim:
            claimName: portainer-data
EOF
}

wait_for_rollouts() {
  log "Esperando disponibilidad de los despliegues de Kubernetes"
  kubectl -n "$NAMESPACE" rollout status deployment/npm-mariadb --timeout="$ROLLOUT_TIMEOUT"
  kubectl -n "$NAMESPACE" rollout status deployment/nginx-proxy-manager --timeout="$ROLLOUT_TIMEOUT"
  kubectl -n "$NAMESPACE" rollout status deployment/portainer --timeout="$ROLLOUT_TIMEOUT"
}

wait_for_public_endpoint() {
  local started_at=0
  local now=0
  local ip=""
  local hostname=""

  started_at="$(date +%s)"
  log "Esperando IP publica del LoadBalancer de Nginx Proxy Manager"
  while true; do
    ip="$(kubectl -n "$NAMESPACE" get svc npm-lb -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)"
    hostname="$(kubectl -n "$NAMESPACE" get svc npm-lb -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)"

    if [[ -n "$ip" ]]; then
      NPM_ENDPOINT="$ip"
      return 0
    fi

    if [[ -n "$hostname" ]]; then
      NPM_ENDPOINT="$hostname"
      return 0
    fi

    now="$(date +%s)"
    if (( now - started_at >= LB_WAIT_TIMEOUT_SECONDS )); then
      return 1
    fi
    sleep "$POLL_INTERVAL_SECONDS"
  done
}

print_summary() {
  printf '\n'
  printf '======================================================================\n'
  printf ' DESPLIEGUE AKS + FLEET + NGINX PROXY MANAGER + PORTAINER COMPLETADO\n'
  printf '======================================================================\n'
  printf ' Subscription:                  %s\n' "$SUBSCRIPTION_ID"
  printf ' Resource Group:                %s\n' "$RESOURCE_GROUP"
  printf ' AKS Cluster:                   %s\n' "$CLUSTER_NAME"
  printf ' Fleet Hub:                     %s\n' "$FLEET_NAME"
  printf ' Region:                        %s\n' "$LOCATION"
  printf ' Pool system:                   %s (%s)\n' "$SYSTEM_POOL_VM_SIZE" "$SYSTEM_POOL_FAMILY"
  printf ' Pool user:                     %s (%s)\n' "$USER_POOL_VM_SIZE" "$USER_POOL_FAMILY"
  printf ' Capacidad nodos user:          min=%s max=%s\n' "$USER_NODE_MIN_COUNT" "$USER_NODE_MAX_COUNT"
  printf ' Namespace:                     %s\n' "$NAMESPACE"
  if [[ -n "$NPM_ENDPOINT" ]]; then
    printf ' NPM public endpoint:           http://%s\n' "$NPM_ENDPOINT"
    printf ' NPM admin:                     http://%s:81\n' "$NPM_ENDPOINT"
  else
    printf ' NPM public endpoint:           pendiente de aprovisionamiento\n'
  fi
  printf ' NPM credenciales iniciales:    admin@example.com / changeme\n'
  printf ' Portainer interno:             http://portainer.%s.svc.cluster.local:9000\n' "$NAMESPACE"
  printf ' Nota tecnica Docker:           no se instala Docker Engine; AKS usa containerd y Portainer se ejecuta en modo Kubernetes.\n'
  printf '======================================================================\n'
}

main() {
  log "Iniciando despliegue autonomo en ${LOCATION}"
  log "Billing account objetivo: ${BILLING_ACCOUNT_ID}"
  log "Tenant objetivo: ${TENANT_ID}"
  log "Docker Engine separado no se desplegara: AKS usa containerd y Portainer se instalara en modo Kubernetes."

  require_command az
  require_command openssl

  ensure_azure_login
  select_subscription
  ensure_extensions
  ensure_kubectl
  register_required_providers
  collect_compute_plan
  ensure_resource_group
  ensure_fleet_hub
  ensure_aks_cluster
  ensure_user_nodepool
  ensure_fleet_member
  connect_kubeconfig
  apply_namespace
  ensure_database_secret
  apply_workloads
  wait_for_rollouts
  if ! wait_for_public_endpoint; then
    warn "El LoadBalancer sigue aprovisionandose; el endpoint publico aun no esta disponible."
  fi
  print_summary
}

main "$@"
