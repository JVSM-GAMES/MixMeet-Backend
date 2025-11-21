# Use a imagem base do Python
FROM python:3.11-slim

# Define o diretório de trabalho dentro do contêiner
WORKDIR /app

# Copia e instala as dependências
# O arquivo requirements.txt deve ter requests, fastapi, uvicorn, python-jose, redis
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copia o código da aplicação (main.py, auth_service.py etc.)
COPY . .

# Expõe a porta que o FastAPI usará (porta 8081 interna)
EXPOSE 8081

# Comando para iniciar o servidor Uvicorn (servidor ASGI para FastAPI)
# --host 0.0.0.0 garante que o serviço seja acessível de fora do contêiner
CMD ["uvicorn", "MixMeet.Auth.main:app", "--host", "0.0.0.0", "--port", "8081"]