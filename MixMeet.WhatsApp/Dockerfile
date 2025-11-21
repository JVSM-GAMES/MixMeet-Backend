# MixMeet-Backend/MixMeet.WhatsApp/Dockerfile (CORRIGIDO)

# Use a imagem base do Node.js
FROM node:20-slim

# Define o diretório de trabalho
WORKDIR /usr/src/app

# --- PASSO NOVO E ESSENCIAL: INSTALAR GIT ---
# O node:20-slim usa Debian/apt. O Git é necessário para instalar dependências do NPM que usam repositórios Git.
RUN apt-get update && apt-get install -y git && rm -rf /var/lib/apt/lists/*
# ---------------------------------------------

# Copia os arquivos de dependência e instala-as
COPY package*.json ./
RUN npm install

# Copia o código da aplicação (index.js)
COPY . .

# Expõe a porta que o servidor Express usará (porta 3001 interna)
EXPOSE 3001

# Comando para iniciar a aplicação Node.js
CMD [ "npm", "start" ]