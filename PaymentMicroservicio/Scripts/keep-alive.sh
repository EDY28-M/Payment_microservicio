#!/bin/bash
# Script para mantener activo el microservicio de pagos en Render
# Ejecuta cada 2 minutos para evitar que Render ponga el servicio en sleep

# Colores para output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# URL del servicio (ajustar según tu configuración)
PAYMENT_URL="${PAYMENT_URL:-https://microservicios-pago.onrender.com}"

# Timestamp para logs
TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')

echo "[$TIMESTAMP] Iniciando keep-alive check para Payment Service..."

# Hacer ping al health endpoint
FULL_URL="${PAYMENT_URL}/health"
echo -n "  Checking Payment Service... "

# Hacer la petición con timeout de 10 segundos
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$FULL_URL" 2>/dev/null)

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✓ OK${NC} (HTTP $HTTP_CODE)"
    exit 0
else
    echo -e "${RED}✗ FAILED${NC} (HTTP $HTTP_CODE)"
    exit 1
fi
