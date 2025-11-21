#!/bin/bash
# MixMeet-Backend/wait-for-it.sh
# Script para esperar que o host:porta esteja acessível.
# Versão simplificada para fins do desafio.

TIMEOUT=30
HOST=$1
PORT=$2

echo "Waiting for $HOST:$PORT to be available..."

for i in $(seq $TIMEOUT); do
  (echo > /dev/tcp/$HOST/$PORT) >/dev/null 2>&1
  if [ $? -eq 0 ]; then
    echo "$HOST:$PORT is available after $i seconds."
    # Remove o host e a porta dos argumentos e executa o comando restante (o dotnet)
    shift 2 
    exec "$@" 
  fi
  sleep 1
done

echo "Error: $HOST:$PORT not available after $TIMEOUT seconds."
exit 1