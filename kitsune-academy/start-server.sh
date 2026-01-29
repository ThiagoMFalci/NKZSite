#!/bin/bash

# Script para rodar o servidor local da Kitsune Academy

cd "$(dirname "$0")"

echo "🎮 Iniciando Kitsune Academy Server..."
echo "📍 Caminho: $(pwd)"

# Verifica se Python está instalado
if command -v python3 &> /dev/null; then
    echo "🐍 Usando Python 3"
    python3 -m http.server 8000
elif command -v python &> /dev/null; then
    echo "🐍 Usando Python 2"
    python -m SimpleHTTPServer 8000
elif command -v node &> /dev/null; then
    echo "📦 Usando Node.js"
    npx http-server -p 8000
else
    echo "❌ Nenhum servidor encontrado. Instale Python ou Node.js"
    exit 1
fi

echo "✅ Servidor rodando em http://localhost:8000"
