# MixMeet-Backend/MixMeet.Auth/auth_service.py (VERSÃO FINAL E ESTÁVEL)

import random
from datetime import datetime, timedelta
from typing import Optional
from jose import jwt
import redis
import os
import requests 
from requests.exceptions import RequestException

# --- Configurações de Segurança e Tempo ---
SECRET_KEY = os.environ.get("SECRET_KEY", "chave_super_secreta_mixmeet_2025")
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 60
REDIS_EXPIRATION_SECONDS = 300 # 5 minutos para o código expirar
WHATSAPP_SERVICE_URL = os.environ.get("WHATSAPP_SERVICE_URL", "http://whatsapp-service:3001") 

# Conexão com Redis
try:
    redis_client = redis.Redis(host='redis', port=6379, db=0, decode_responses=True)
    redis_client.ping()
except Exception as e:
    print(f"Erro ao conectar ao Redis: {e}")


# --- FUNÇÕES DE STATUS E ENVIO ---

def is_whatsapp_ready() -> bool:
    """
    Verifica se o serviço Node.js/Baileys está logado e pronto para enviar.
    """
    try:
        response = requests.get(f"{WHATSAPP_SERVICE_URL}/api/whatsapp/status", timeout=2)
        response.raise_for_status()
        return response.json().get('ready', False)
    except RequestException:
        return False


def _send_code_via_whatsapp_service(phone_number: str, code: str):
    """
    Tenta chamar o microsserviço Node.js para enviar o código.
    Lança exceção interna (RequestException) em caso de falha de comunicação/rede.
    """
    try:
        response = requests.post(
            f"{WHATSAPP_SERVICE_URL}/api/whatsapp/send-code",
            json={"phoneNumber": phone_number, "code": code},
            timeout=30
        )
        response.raise_for_status() # Lança HTTPError se o Node.js retornar 4xx/5xx
        print(f"Código {code} enviado (via Baileys Service) para {phone_number}.")
        
    except RequestException as e:
        print(f"ERRO ao chamar WhatsApp Service (Baileys): {e}.")
        # Lançamos uma exceção genérica aqui que será capturada no main.py para retornar 503.
        raise RuntimeError("Falha na comunicação do serviço de envio de código.") 


# --- Funções de Serviço Principais ---

def request_verification_code(phone_number: str):
    """
    Implementa o Feature Toggle. Checa o Baileys, gera o código, salva e envia.
    """
    if not is_whatsapp_ready():
        # Lança erro com a mensagem amigável desejada
        raise RuntimeError("Whatsapp indisponivel no momento, contate um administrador.")

    code = str(random.randint(100000, 999999))
    redis_client.setex(phone_number, REDIS_EXPIRATION_SECONDS, code)
    
    # Chama o serviço, se falhar, a exceção será propagada para o main.py
    _send_code_via_whatsapp_service(phone_number, code)

# --- Valida se o número existe no WhatsApp (Baileys) ---
async def check_wa_existence(e164_number: str) -> bool:
    """
    Chama o endpoint do Node.js/Baileys para verificar se o número existe.
    """
    if not is_whatsapp_ready():
        # Se o serviço estiver inativo, não podemos validar. Retornamos True 
        # e tratamos a falha mais tarde no request-code (Feature Toggle).
        return True 

    try:
        # Chamada ao endpoint que o Node.js PRECISA expor para a validação.
        # NOTA: Este endpoint precisa ser adicionado ao index.js do WhatsApp!
        response = requests.post(
            f"{WHATSAPP_SERVICE_URL}/api/whatsapp/check-number", 
            json={"phoneNumber": e164_number},
            timeout=15
        )
        response.raise_for_status() 
        # O Node.js deve retornar {"exists": true/false}
        return response.json().get('exists', False)
        
    except RequestException as e:
        print(f"ERRO ao validar existência no WhatsApp: {e}")
        # Falha na comunicação: assumimos True para não bloquear o usuário injustamente
        return True

def verify_code_and_login(phone_number: str, submitted_code: str) -> bool:
    """
    Verifica o código contra o Redis.
    """
    stored_code = redis_client.get(phone_number)
    
    if stored_code and stored_code == submitted_code:
        redis_client.delete(phone_number)
        return True
    
    return False

def create_access_token(data: dict, expires_delta: Optional[timedelta] = None):
    """
    Cria um JWT.
    """
    to_encode = data.copy()
    if expires_delta:
        expire = datetime.utcnow() + expires_delta
    else:
        expire = datetime.utcnow() + timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)
    
    to_encode.update({"exp": expire, "sub": data.get("phone_number")})
    encoded_jwt = jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)
    return encoded_jwt