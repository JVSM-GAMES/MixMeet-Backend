from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from datetime import timedelta
from fastapi.middleware.cors import CORSMiddleware
from auth_service import request_verification_code, verify_code_and_login, create_access_token, ACCESS_TOKEN_EXPIRE_MINUTES, is_whatsapp_ready, check_wa_existence

app = FastAPI()
origins = [
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "http://localhost:5000",
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"], 
    allow_credentials=True,
    allow_methods=["*"], 
    allow_headers=["*"], 
)

class PhoneRequest(BaseModel):
    phone_number: str

class VerificationRequest(BaseModel):
    phone_number: str
    code: str

class Token(BaseModel):
    access_token: str
    token_type: str = "bearer"
    expires_in: int = ACCESS_TOKEN_EXPIRE_MINUTES * 60

@app.post("/api/auth/request-code")
async def handle_request_code(request: PhoneRequest):
    if not request.phone_number:
        raise HTTPException(status_code=400, detail="Número de telefone é obrigatório.")

    try:
        # Tenta executar a lógica principal
        request_verification_code(request.phone_number)
    
    except RuntimeError as e:
        # CAPTURA a exceção de falha de serviço e retorna 503 com a mensagem amigável
        raise HTTPException(status_code=503, detail=str(e)) 

    return {"message": "Código de verificação solicitado com sucesso."}

@app.post("/api/auth/verify-code", response_model=Token)
async def handle_verify_code(request: VerificationRequest):
    
    if not verify_code_and_login(request.phone_number, request.code):
        raise HTTPException(
            status_code=401,
            detail="Código de verificação inválido ou expirado.",
            headers={"WWW-Authenticate": "Bearer"},
        )

    access_token_expires = timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)
    access_token = create_access_token(
        data={"phone_number": request.phone_number}, expires_delta=access_token_expires
    )
    
    return Token(access_token=access_token, expires_in=ACCESS_TOKEN_EXPIRE_MINUTES * 60)

# --- Endpoint de Validação de Existência do WhatsApp ---
@app.post("/api/auth/check-wa-existence")
async def check_wa_existence_endpoint(request: PhoneRequest):
    if not request.phone_number:
        raise HTTPException(status_code=400, detail="Número é obrigatório.")
    
    # Chama a função do serviço que interage com o Baileys/Node.js
    wa_exists = await check_wa_existence(request.phone_number)
    
    return {"exists": wa_exists}

@app.get("/api/auth/whatsapp/status")
async def get_whatsapp_status():
    """
    Endpoint para ser chamado pelo Front-end (Acesso Admin) para verificar 
    o status do serviço Baileys.
    """
    try:
        # Nota: Chamamos o auth_service.is_whatsapp_ready() que retorna True/False
        is_ready = is_whatsapp_ready()

        # O Front-end agora chama o Node.js diretamente para obter o QR. 
        # Este endpoint será simplificado para verificar apenas a disponibilidade.
        return {"ready": is_ready, "qr": None}
    
    except Exception:
        raise HTTPException(status_code=503, detail="Serviço WhatsApp indisponível.")


@app.get("/health")
async def health_check():
    return {"status": "Auth Service OK"}