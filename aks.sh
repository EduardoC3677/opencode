#!/usr/bin/env bash
# ==============================================================================
# DESPLIEGUE AUTOMÁTICO DE AKS + FLEET + NGINX PROXY MANAGER + PORTAINER + DOCKER
# REGIÓN: eastus2
# BILLING ACCOUNT: 6a74355b-d0aa-4bfb-b194-3e8de6164ea6
# TENANT: 1427ba79-0e7a-4976-8aca-1e714957f671
# ==============================================================================

set -Eeuo pipefail

SCRIPT_NAME="$(basename "$0")"

TENANT_ID="1427ba79-0e7a-4976-8aca-1e714957f671"
BILLING_ACCOUNT_ID="6a74355b-d0aa-4bfb-b194-3e8de6164ea6"
LOCATION="${LOCATION:-eastus2}"
RANDOM_SUFFIX="$(printf '%05d%05d' "$RANDOM" "$RANDOM")"
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-aks-platform-${RANDOM_SUFFIX}}"
CLUSTER_NAME="${CLUSTER_NAME:-aks-platform-${RANDOM_SUFFIX}}"
FLEET_NAME="${FLEET_NAME:-fleet-platform-${RANDOM_SUFFIX}}"
NAMESPACE="${NAMESPACE:-platform-system}"
SYSTEM_POOL_NAME="${SYSTEM_POOL_NAME:-systempool}"
USER_POOL_NAME="${USER_POOL_NAME:-userpool}"
DNS_LABEL="${DNS_LABEL:-npm-${RANDOM_SUFFIX}}"
NPM_SERVICE_NAME="npm-ingress"
PORTAINER_SERVICE_NAME="portainer"
DOCKER_SERVICE_NAME="docker-dind"

MIN_TOTAL_NODES=4
MIN_SYSTEM_NODES=2
MIN_USER_NODES=2
WORKLOAD_CPU_MILLICORES=2800
WORKLOAD_MEMORY_MIB=6144
HEADROOM_PERCENT=70
ACTIVE_SUBSCRIPTION=""
REGIONAL_CORES_AVAILABLE=0
OPTIMAL_FAMILY=""
FAMILY_AVAILABLE_CORES=0
SELECTED_VM_SIZE=""
SELECTED_VM_VCPUS=0
SELECTED_VM_MEMORY_GIB=0
SYSTEM_POOL_NODE_COUNT=0
USER_POOL_NODE_COUNT=0
USER_POOL_MAX_COUNT=0
MAX_NODE_COUNT_BY_QUOTA=0

trap 'printf "\n[ERROR] %s falló en la línea %s ejecutando: %s\n" "$SCRIPT_NAME" "$LINENO" "$BASH_COMMAND" >&2' ERR

log() {
  printf '[%s] %s\n' "$(date -u +"%Y-%m-%dT%H:%M:%SZ")" "$*"
}

fail() {
  log "ERROR: $*"
  exit 1
}

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || fail "Dependencia requerida no encontrada: $1"
}

ensure_kubectl() {
  if command -v kubectl >/dev/null 2>&1; then
    return 0
  fi

  log "kubectl no está instalado; instalándolo con Azure CLI..."
  mkdir -p "${HOME}/.local/bin"
  az aks install-cli --install-location "${HOME}/.local/bin/kubectl" >/dev/null
  export PATH="${HOME}/.local/bin:${PATH}"
  require_cmd kubectl
}

ensure_login() {
  log "Validando autenticación no interactiva en Azure CLI..."

  if az account show >/dev/null 2>&1; then
    local current_tenant
    current_tenant="$(az account show --query tenantId -o tsv)"
    if [[ "$current_tenant" == "$TENANT_ID" ]]; then
      log "Sesión de Azure CLI válida detectada en el tenant solicitado."
      return 0
    fi
    log "Existe una sesión activa, pero pertenece al tenant ${current_tenant}. Se intentará autenticación no interactiva en ${TENANT_ID}."
  fi

  if [[ -n "${AZURE_CLIENT_ID:-}" && -n "${AZURE_CLIENT_SECRET:-}" ]]; then
    az login \
      --service-principal \
      --username "$AZURE_CLIENT_ID" \
      --password "$AZURE_CLIENT_SECRET" \
      --tenant "${AZURE_TENANT_ID:-$TENANT_ID}" \
      >/dev/null
    log "Autenticación completada con service principal."
    return 0
  fi

  if az login --identity --tenant "$TENANT_ID" >/dev/null 2>&1; then
    log "Autenticación completada con Managed Identity."
    return 0
  fi

  fail "No hay una sesión válida en el tenant ${TENANT_ID} ni credenciales no interactivas configuradas (AZURE_CLIENT_ID/AZURE_CLIENT_SECRET o Managed Identity)."
}

resolve_subscription() {
  log "Resolviendo la suscripción habilitada asociada al billing account ${BILLING_ACCOUNT_ID}..."

  local billing_json
  local subscription_json
  local resolved_subscription

  billing_json="$(az billing subscription list --billing-account-name "$BILLING_ACCOUNT_ID" -o json 2>/dev/null || printf '[]')"
  subscription_json="$(az account list --all -o json)"

  resolved_subscription="$(BILLING_JSON="$billing_json" SUBSCRIPTION_JSON="$subscription_json" TENANT_ID="$TENANT_ID" python3 - <<'PY'
import json
import os

billing = json.loads(os.environ["BILLING_JSON"])
subscriptions = json.loads(os.environ["SUBSCRIPTION_JSON"])
tenant_id = os.environ["TENANT_ID"]

choices = []
for item in billing:
    subscription_id = item.get("subscriptionId") or item.get("id")
    state = (item.get("state") or item.get("status") or "").lower()
    if subscription_id and state in {"active", "enabled"}:
        choices.append(subscription_id)

if not choices:
    for item in subscriptions:
        subscription_id = item.get("id")
        state = (item.get("state") or "").lower()
        item_tenant = item.get("tenantId")
        if subscription_id and state == "enabled" and item_tenant == tenant_id:
            choices.append(subscription_id)

print(choices[0] if choices else "")
PY
)"

  [[ -n "$resolved_subscription" ]] || fail "No se encontró una suscripción habilitada para el tenant ${TENANT_ID}."

  az account set --subscription "$resolved_subscription"
  ACTIVE_SUBSCRIPTION="$resolved_subscription"
  log "Suscripción activa establecida: ${ACTIVE_SUBSCRIPTION}"
}

install_extensions_and_register_providers() {
  log "Instalando y actualizando extensiones requeridas..."
  az extension add --name fleet --upgrade --yes >/dev/null

  log "Registrando proveedores de recursos necesarios..."
  az provider register --namespace Microsoft.ContainerService --wait >/dev/null
  az provider register --namespace Microsoft.Network --wait >/dev/null
  az provider register --namespace Microsoft.Storage --wait >/dev/null
}

calculate_compute_plan() {
  log "Analizando cuotas disponibles y SKUs compatibles en ${LOCATION}..."

  local usage_file
  local sku_file
  local selection

  usage_file="$(mktemp)"
  sku_file="$(mktemp)"

  az vm list-usage --location "$LOCATION" -o json >"$usage_file"
  az vm list-skus --location "$LOCATION" --resource-type virtualMachines --all -o json >"$sku_file"

  selection="$(python3 - "$usage_file" "$sku_file" "$MIN_TOTAL_NODES" "$MIN_SYSTEM_NODES" "$MIN_USER_NODES" "$WORKLOAD_CPU_MILLICORES" "$WORKLOAD_MEMORY_MIB" "$HEADROOM_PERCENT" <<'PY'
import json
import math
import shlex
import sys

usage_path, sku_path, min_total_nodes, min_system_nodes, min_user_nodes, workload_cpu, workload_memory, headroom_percent = sys.argv[1:]
min_total_nodes = int(min_total_nodes)
min_system_nodes = int(min_system_nodes)
min_user_nodes = int(min_user_nodes)
workload_cpu = int(workload_cpu)
workload_memory = int(workload_memory)
headroom = int(headroom_percent) / 100.0

with open(usage_path, "r", encoding="utf-8") as fh:
    usage = json.load(fh)
with open(sku_path, "r", encoding="utf-8") as fh:
    skus = json.load(fh)

family_usage = {}
regional_available = 0

for item in usage:
    name = (item.get("name") or {}).get("value", "")
    localized = (item.get("name") or {}).get("localizedValue", "")
    try:
        limit = int(item.get("limit") or 0)
        current = int(item.get("currentValue") or 0)
    except (TypeError, ValueError):
        continue

    available = max(limit - current, 0)
    if name.lower() == "cores":
        regional_available = available
        continue

    lowered_name = name.lower()
    lowered_localized = localized.lower()
    if not lowered_name.startswith(("standardd", "standarde", "standardf")):
        continue
    if not lowered_name.endswith("family"):
        continue
    if any(token in lowered_name or token in lowered_localized for token in ("promo", "spot", "lowpriority", "gpu", "basic", "mfamily", "ncfamily", "ndfamily", "nvfamily", "hbfamily", "hcfamily")):
        continue

    family_usage[name] = {
        "available": available,
        "localized": localized,
    }

if regional_available <= 0:
    raise SystemExit("No hay cuota regional de vCPU disponible en la región seleccionada.")

candidates = []

for sku in skus:
    family = sku.get("family", "")
    if family not in family_usage:
        continue

    restrictions = sku.get("restrictions") or []
    if restrictions:
        continue

    name = sku.get("name", "")
    lowered_name = name.lower()
    if not lowered_name.startswith(("standard_d", "standard_e", "standard_f")):
        continue
    if any(token in lowered_name for token in ("promo", "spot", "lowpriority", "standard_a", "_a")):
        continue

    capabilities = {cap.get("name"): cap.get("value") for cap in sku.get("capabilities", [])}
    try:
        vcpus = int(float(capabilities.get("vCPUs", 0)))
        memory_gib = float(capabilities.get("MemoryGB", 0))
    except (TypeError, ValueError):
        continue

    if vcpus < 2 or memory_gib < 4:
        continue

    family_available = family_usage[family]["available"]
    max_nodes = min(family_available // vcpus, regional_available // vcpus)
    if max_nodes < 2:
        continue

    effective_cpu = max(int(vcpus * 1000 * headroom), 500)
    effective_memory = max(int(memory_gib * 1024 * headroom), 1024)
    user_nodes = max(
        min_user_nodes,
        math.ceil(workload_cpu / effective_cpu),
        math.ceil(workload_memory / effective_memory),
    )
    system_nodes = min_system_nodes if max_nodes >= min_total_nodes else 1
    total_required = system_nodes + user_nodes
    user_pool_ceiling = max(1, max_nodes - system_nodes)
    autoscaler_max = max(user_nodes, min(user_pool_ceiling, user_nodes + 3))

    candidates.append(
        {
            "family": family,
            "family_available": family_available,
            "name": name,
            "vcpus": vcpus,
            "memory_gib": memory_gib,
            "max_nodes": max_nodes,
            "system_nodes": system_nodes,
            "user_nodes": user_nodes,
            "total_required": total_required,
            "autoscaler_max": autoscaler_max,
        }
    )

if not candidates:
    raise SystemExit("No se encontraron SKUs compatibles con AKS y con cuota disponible en la región seleccionada.")

families_by_quota = sorted(
    family_usage.items(),
    key=lambda entry: entry[1]["available"],
    reverse=True,
)

selected = None
for family_name, _ in families_by_quota:
    family_candidates = [c for c in candidates if c["family"] == family_name]
    family_candidates.sort(key=lambda c: (c["vcpus"], c["memory_gib"]), reverse=True)
    for candidate in family_candidates:
        if candidate["max_nodes"] >= max(candidate["total_required"], min_total_nodes):
            selected = candidate
            break
    if selected:
        break

if selected is None:
    candidates.sort(
        key=lambda c: (
            c["family_available"],
            c["max_nodes"],
            c["vcpus"],
            c["memory_gib"],
        ),
        reverse=True,
    )
    selected = candidates[0]
    selected["system_nodes"] = min(selected["system_nodes"], max(1, selected["max_nodes"] - 1))
    selected["user_nodes"] = max(1, selected["max_nodes"] - selected["system_nodes"])
    selected["total_required"] = selected["system_nodes"] + selected["user_nodes"]
    selected["autoscaler_max"] = selected["user_nodes"]

values = {
    "REGIONAL_CORES_AVAILABLE": regional_available,
    "OPTIMAL_FAMILY": selected["family"],
    "FAMILY_AVAILABLE_CORES": selected["family_available"],
    "SELECTED_VM_SIZE": selected["name"],
    "SELECTED_VM_VCPUS": selected["vcpus"],
    "SELECTED_VM_MEMORY_GIB": f"{selected['memory_gib']:.1f}",
    "SYSTEM_POOL_NODE_COUNT": selected["system_nodes"],
    "USER_POOL_NODE_COUNT": selected["user_nodes"],
    "USER_POOL_MAX_COUNT": max(selected["user_nodes"], selected["autoscaler_max"]),
    "MAX_NODE_COUNT_BY_QUOTA": selected["max_nodes"],
}

for key, value in values.items():
    print(f"{key}={shlex.quote(str(value))}")
PY
)"

  rm -f "$usage_file" "$sku_file"

  eval "$selection"

  log "Familia con mayor cuota utilizable: ${OPTIMAL_FAMILY}"
  log "Quota regional libre: ${REGIONAL_CORES_AVAILABLE} vCPU"
  log "Quota disponible en la familia seleccionada: ${FAMILY_AVAILABLE_CORES} vCPU"
  log "VM seleccionada automáticamente: ${SELECTED_VM_SIZE} (${SELECTED_VM_VCPUS} vCPU / ${SELECTED_VM_MEMORY_GIB} GiB)"
  log "Pool del sistema: ${SYSTEM_POOL_NODE_COUNT} nodo(s)"
  log "Pool de usuario: ${USER_POOL_NODE_COUNT} nodo(s), autoscaler máximo: ${USER_POOL_MAX_COUNT}"
}

create_resource_group_and_fleet() {
  log "Creando resource group ${RESOURCE_GROUP} en ${LOCATION}..."
  az group create --name "$RESOURCE_GROUP" --location "$LOCATION" >/dev/null

  log "Creando Azure Kubernetes Fleet Manager ${FLEET_NAME}..."
  az fleet create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$FLEET_NAME" \
    --location "$LOCATION" \
    --enable-hub \
    --enable-managed-identity \
    >/dev/null
}

create_aks_cluster() {
  log "Desplegando clúster AKS ${CLUSTER_NAME} con Azure CNI Overlay..."
  az aks create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$CLUSTER_NAME" \
    --location "$LOCATION" \
    --nodepool-name "$SYSTEM_POOL_NAME" \
    --node-count "$SYSTEM_POOL_NODE_COUNT" \
    --node-vm-size "$SELECTED_VM_SIZE" \
    --vm-set-type VirtualMachineScaleSets \
    --load-balancer-sku standard \
    --network-plugin azure \
    --network-plugin-mode overlay \
    --enable-managed-identity \
    --enable-oidc-issuer \
    --enable-workload-identity \
    --generate-ssh-keys \
    --tier standard \
    --yes \
    >/dev/null

  log "Agregando pool de usuario ${USER_POOL_NAME}..."
  az aks nodepool add \
    --resource-group "$RESOURCE_GROUP" \
    --cluster-name "$CLUSTER_NAME" \
    --name "$USER_POOL_NAME" \
    --mode User \
    --node-count "$USER_POOL_NODE_COUNT" \
    --node-vm-size "$SELECTED_VM_SIZE" \
    --enable-cluster-autoscaler \
    --min-count "$USER_POOL_NODE_COUNT" \
    --max-count "$USER_POOL_MAX_COUNT" \
    --labels workload=apps stack=platform \
    >/dev/null
}

attach_cluster_to_fleet() {
  log "Adjuntando el clúster AKS a Fleet Manager..."
  local cluster_resource_id
  cluster_resource_id="$(az aks show --resource-group "$RESOURCE_GROUP" --name "$CLUSTER_NAME" --query id -o tsv)"

  az fleet member create \
    --resource-group "$RESOURCE_GROUP" \
    --fleet-name "$FLEET_NAME" \
    --name "${CLUSTER_NAME}-member" \
    --member-cluster-id "$cluster_resource_id" \
    >/dev/null
}

connect_kubectl() {
  log "Obteniendo credenciales del clúster para kubectl..."
  az aks get-credentials \
    --resource-group "$RESOURCE_GROUP" \
    --name "$CLUSTER_NAME" \
    --overwrite-existing \
    >/dev/null

  kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f - >/dev/null
}

apply_platform_manifests() {
  log "Aplicando manifiestos de NGINX Proxy Manager, Portainer y Docker..."

  cat <<EOF | kubectl apply -f - >/dev/null
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: npm-data
  namespace: ${NAMESPACE}
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: managed-csi
  resources:
    requests:
      storage: 20Gi
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: npm-letsencrypt
  namespace: ${NAMESPACE}
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: managed-csi
  resources:
    requests:
      storage: 10Gi
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: portainer-data
  namespace: ${NAMESPACE}
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: managed-csi
  resources:
    requests:
      storage: 10Gi
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: docker-data
  namespace: ${NAMESPACE}
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: managed-csi
  resources:
    requests:
      storage: 30Gi
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
  name: portainer-cluster-admin-${RANDOM_SUFFIX}
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: cluster-admin
subjects:
  - kind: ServiceAccount
    name: portainer-sa
    namespace: ${NAMESPACE}
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: docker-dind
  namespace: ${NAMESPACE}
  labels:
    app: docker-dind
spec:
  replicas: 1
  selector:
    matchLabels:
      app: docker-dind
  template:
    metadata:
      labels:
        app: docker-dind
    spec:
      nodeSelector:
        agentpool: ${USER_POOL_NAME}
      containers:
        - name: docker-dind
          image: docker:27-dind
          securityContext:
            privileged: true
          env:
            - name: DOCKER_TLS_CERTDIR
              value: ""
          ports:
            - containerPort: 2375
              name: docker
          resources:
            requests:
              cpu: "1000m"
              memory: "2Gi"
            limits:
              cpu: "2000m"
              memory: "4Gi"
          readinessProbe:
            tcpSocket:
              port: 2375
            initialDelaySeconds: 20
            periodSeconds: 10
          livenessProbe:
            tcpSocket:
              port: 2375
            initialDelaySeconds: 45
            periodSeconds: 20
          volumeMounts:
            - name: docker-data
              mountPath: /var/lib/docker
      volumes:
        - name: docker-data
          persistentVolumeClaim:
            claimName: docker-data
---
apiVersion: v1
kind: Service
metadata:
  name: ${DOCKER_SERVICE_NAME}
  namespace: ${NAMESPACE}
spec:
  type: ClusterIP
  selector:
    app: docker-dind
  ports:
    - name: docker
      port: 2375
      targetPort: docker
      protocol: TCP
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: nginx-proxy-manager
  namespace: ${NAMESPACE}
  labels:
    app: nginx-proxy-manager
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
        - name: nginx-proxy-manager
          image: jc21/nginx-proxy-manager:latest
          ports:
            - containerPort: 80
              name: http
            - containerPort: 81
              name: admin
            - containerPort: 443
              name: https
          resources:
            requests:
              cpu: "500m"
              memory: "768Mi"
            limits:
              cpu: "1500m"
              memory: "2Gi"
          readinessProbe:
            tcpSocket:
              port: 81
            initialDelaySeconds: 20
            periodSeconds: 10
          livenessProbe:
            tcpSocket:
              port: 81
            initialDelaySeconds: 45
            periodSeconds: 20
          volumeMounts:
            - name: npm-data
              mountPath: /data
            - name: npm-letsencrypt
              mountPath: /etc/letsencrypt
      volumes:
        - name: npm-data
          persistentVolumeClaim:
            claimName: npm-data
        - name: npm-letsencrypt
          persistentVolumeClaim:
            claimName: npm-letsencrypt
---
apiVersion: v1
kind: Service
metadata:
  name: ${NPM_SERVICE_NAME}
  namespace: ${NAMESPACE}
  annotations:
    service.beta.kubernetes.io/azure-dns-label-name: ${DNS_LABEL}
spec:
  type: LoadBalancer
  externalTrafficPolicy: Cluster
  selector:
    app: nginx-proxy-manager
  ports:
    - name: http
      port: 80
      targetPort: http
      protocol: TCP
    - name: https
      port: 443
      targetPort: https
      protocol: TCP
    - name: admin
      port: 81
      targetPort: admin
      protocol: TCP
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: portainer
  namespace: ${NAMESPACE}
  labels:
    app: portainer
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
          env:
            - name: DOCKER_HOST
              value: tcp://${DOCKER_SERVICE_NAME}.${NAMESPACE}.svc.cluster.local:2375
          ports:
            - containerPort: 9000
              name: http
            - containerPort: 9443
              name: https
          resources:
            requests:
              cpu: "300m"
              memory: "512Mi"
            limits:
              cpu: "1000m"
              memory: "1Gi"
          readinessProbe:
            httpGet:
              path: /api/status
              port: 9000
            initialDelaySeconds: 20
            periodSeconds: 10
          livenessProbe:
            httpGet:
              path: /api/status
              port: 9000
            initialDelaySeconds: 45
            periodSeconds: 20
          volumeMounts:
            - name: portainer-data
              mountPath: /data
      volumes:
        - name: portainer-data
          persistentVolumeClaim:
            claimName: portainer-data
---
apiVersion: v1
kind: Service
metadata:
  name: ${PORTAINER_SERVICE_NAME}
  namespace: ${NAMESPACE}
spec:
  type: ClusterIP
  selector:
    app: portainer
  ports:
    - name: http
      port: 9000
      targetPort: http
      protocol: TCP
    - name: https
      port: 9443
      targetPort: https
      protocol: TCP
EOF

  log "Esperando a que los despliegues estén listos..."
  kubectl rollout status deployment/docker-dind -n "$NAMESPACE" --timeout=20m >/dev/null
  kubectl rollout status deployment/nginx-proxy-manager -n "$NAMESPACE" --timeout=20m >/dev/null
  kubectl rollout status deployment/portainer -n "$NAMESPACE" --timeout=20m >/dev/null
}

wait_for_public_endpoint() {
  log "Esperando la IP pública del Load Balancer de Azure..."

  local public_endpoint=""
  local hostname=""

  for _ in $(seq 1 40); do
    public_endpoint="$(kubectl get service "$NPM_SERVICE_NAME" -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || true)"
    hostname="$(kubectl get service "$NPM_SERVICE_NAME" -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || true)"

    if [[ -n "$public_endpoint" || -n "$hostname" ]]; then
      printf '%s\n' "${public_endpoint:-$hostname}"
      return 0
    fi

    sleep 15
  done

  return 1
}

print_summary() {
  local public_endpoint="${1:-pendiente}"

  printf '\n======================================================================\n'
  printf ' DESPLIEGUE FINALIZADO\n'
  printf '======================================================================\n'
  printf ' Tenant: %s\n' "$TENANT_ID"
  printf ' Billing Account: %s\n' "$BILLING_ACCOUNT_ID"
  printf ' Suscripción: %s\n' "$ACTIVE_SUBSCRIPTION"
  printf ' Resource Group: %s\n' "$RESOURCE_GROUP"
  printf ' Clúster AKS: %s\n' "$CLUSTER_NAME"
  printf ' Fleet: %s\n' "$FLEET_NAME"
  printf ' Región: %s\n' "$LOCATION"
  printf ' VM seleccionada: %s (%s vCPU / %s GiB)\n' "$SELECTED_VM_SIZE" "$SELECTED_VM_VCPUS" "$SELECTED_VM_MEMORY_GIB"
  printf ' Familia con mayor cuota: %s (%s vCPU disponibles)\n' "$OPTIMAL_FAMILY" "$FAMILY_AVAILABLE_CORES"
  printf ' Nodos pool sistema: %s\n' "$SYSTEM_POOL_NODE_COUNT"
  printf ' Nodos pool usuario: %s (autoscaler máximo: %s)\n' "$USER_POOL_NODE_COUNT" "$USER_POOL_MAX_COUNT"
  printf ' Endpoint público NPM: %s\n' "$public_endpoint"
  printf ' NPM Admin: http://%s:81\n' "$public_endpoint"
  printf ' Portainer interno: http://%s.%s.svc.cluster.local:9000\n' "$PORTAINER_SERVICE_NAME" "$NAMESPACE"
  printf ' Docker interno: tcp://%s.%s.svc.cluster.local:2375\n' "$DOCKER_SERVICE_NAME" "$NAMESPACE"
  printf ' Credenciales iniciales de NPM: admin@example.com / changeme\n'
  printf '======================================================================\n\n'
}

main() {
  printf '======================================================================\n'
  printf ' INICIANDO DESPLIEGUE AUTOMÁTICO DE AKS, FLEET Y PLATAFORMA\n'
  printf '======================================================================\n'

  require_cmd az
  require_cmd python3
  ensure_kubectl
  ensure_login
  resolve_subscription
  install_extensions_and_register_providers
  calculate_compute_plan
  create_resource_group_and_fleet
  create_aks_cluster
  attach_cluster_to_fleet
  connect_kubectl
  apply_platform_manifests

  local public_endpoint
  if public_endpoint="$(wait_for_public_endpoint)"; then
    print_summary "$public_endpoint"
  else
    print_summary "pendiente-de-asignación"
  fi
}

main "$@"
