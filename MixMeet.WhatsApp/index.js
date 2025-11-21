// MixMeet-Backend/MixMeet.WhatsApp/index.js (ATUALIZAÇÃO PARCIAL - Substitua o arquivo ou os endpoints)

import express from 'express';
import Pino from 'pino';
import * as baileys from '@whiskeysockets/baileys';
import qrcode from 'qrcode';
import { Boom } from '@hapi/boom';
import cors from 'cors';
import fs from 'fs'; 

const { DisconnectReason, default: makeWASocket } = baileys;
const useMultiFileAuthState = baileys.useMultiFileAuthState; 

const logger = Pino({ level: process.env.LOG_LEVEL || 'silent' });
const app = express();
app.use(cors({ origin: '*', credentials: true }));
app.use(express.json());

const PORT = process.env.PORT || 3001; 
const settingsFile = './settings.json';
let latestQr = null;
let waClient = null; 

const withTimeout = (promise, ms = 10000) => {
    let timeoutId;
    const timeoutPromise = new Promise((_, reject) => {
        timeoutId = setTimeout(() => {
            reject(new Error(`Operação Baileys excedeu o tempo limite de ${ms}ms`));
        }, ms);
    });
    return Promise.race([promise, timeoutPromise]).finally(() => clearTimeout(timeoutId));
};

async function startWA() {
    const { state, saveCreds } = await useMultiFileAuthState('./auth_info');
    const { version } = await baileys.fetchLatestBaileysVersion();
    
    const sock = makeWASocket({ 
        version, 
        auth: state, 
        printQRInTerminal: false, 
        logger,
        browser: ['MixMeet', 'Chrome', '4.0.0'],
        connectTimeoutMs: 60000,
        defaultQueryTimeoutMs: 60000,
        keepAliveIntervalMs: 10000,
        emitOwnEvents: true,
        retryRequestDelayMs: 250
    });
    
    waClient = sock; 

    sock.ev.on('connection.update', async (update) => {
        const { connection, lastDisconnect, qr } = update;
        
        if (qr) {
            latestQr = await qrcode.toDataURL(qr); 
        } else {
            latestQr = null; 
        }
        
        if (connection === 'close') {
            const code = new Boom(lastDisconnect?.error)?.output?.statusCode;
            const shouldReconnect = code !== DisconnectReason.loggedOut;
            if (shouldReconnect) {
                logger.info('Conexão fechada. Reconectando em 2s...');
                setTimeout(startWA, 2000);
            } else {
                logger.error('Sessão encerrada. QR Code necessário.');
            }
        } else if (connection === 'open') {
            latestQr = null; 
            logger.info('Conectado ao WhatsApp ✅');
        }
    });

    sock.ev.on('creds.update', saveCreds);
}

app.get('/api/whatsapp/status', (_, res) => {
    const isReady = waClient?.user ? true : false; 
    res.json({ ready: isReady, qr: isReady ? null : latestQr });
});

// --- CORREÇÃO AQUI: Sanitização do Número ---
app.post('/api/whatsapp/send-code', async (req, res) => {
    const { phoneNumber, code } = req.body;
    
    // REMOVE O '+' E OUTROS CARACTERES NÃO NUMÉRICOS
    const cleanNumber = phoneNumber.replace(/\D/g, ''); 
    const jid = `${cleanNumber}@s.whatsapp.net`;

    logger.info(`Tentando enviar para JID: ${jid}`); // Log para debug

    if (!waClient || !waClient.user) {
        return res.status(503).json({ message: "Serviço WhatsApp não está logado." });
    }

    try {
        await withTimeout(
            waClient.sendMessage(jid, {
                text: `Seu código MixMeet é: *${code}*. Válido por 5 minutos.`
            }),
            15000 
        );
        
        logger.info(`Mensagem enviada com sucesso para ${jid}`);
        res.status(200).json({ success: true });
    } catch (e) {
        logger.error({ error: e.message, stack: e.stack }, 'Falha ao enviar código WA');
        res.status(500).json({ message: e.message || 'Erro interno no envio.' });
    }
});

app.post('/api/whatsapp/check-number', async (req, res) => {
    const { phoneNumber } = req.body;
    // REMOVE O '+'
    const cleanNumber = phoneNumber.replace(/\D/g, '');
    const jid = `${cleanNumber}@s.whatsapp.net`;

    if (!waClient || !waClient.user) {
        return res.status(503).json({ message: "Serviço WhatsApp não está logado." });
    }

    try {
        const [result] = await withTimeout(
            waClient.onWhatsApp(jid),
            10000
        );
        
        const exists = result?.exists || false;
        res.status(200).json({ exists });
    } catch (e) {
        logger.error({ error: e.message }, 'Falha na verificação de número');
        res.status(200).json({ exists: true }); 
    }
});

app.listen(PORT, () => logger.info({ PORT }, 'HTTP server online'));

startWA().catch((err) => logger.error({ err }, 'Erro fatal na inicialização'));