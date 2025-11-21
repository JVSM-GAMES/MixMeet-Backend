# 🚀 MixMeet - Backend (Poliglota & Microsserviços)

Sistema robusto de agendamento de salas de reunião com autenticação via WhatsApp, desenvolvido com uma arquitetura de microsserviços poliglota para demonstrar interoperabilidade, escalabilidade e uso da ferramenta certa para cada tarefa.

## 🏗️ Arquitetura do Sistema

O backend é orquestrado via **Docker Compose** e composto por três serviços distintos:

1.  **`reservas-api` (C# .NET 8):**
    * **Responsabilidade:** Core do negócio (CRUD de Reservas, Gestão de Usuários).
    * **Destaques:** Entity Framework Core, Validação de Conflito de Horários, Persistência em PostgreSQL.
2.  **`auth-api` (Python FastAPI):**
    * **Responsabilidade:** Gateway de Autenticação e Lógica de Negócio de Envio.
    * **Destaques:** FastAPI (alta performance), Gestão de Tokens JWT, Orquestração do serviço de mensageria.
3.  **`whatsapp-service` (Node.js):**
    * **Responsabilidade:** Interface com a rede do WhatsApp via protocolo WebSocket.
    * **Destaques:** Biblioteca `@whiskeysockets/baileys`, Gestão de Sessão, API interna para envio de OTP.

### 🗄️ Infraestrutura de Dados
* **PostgreSQL:** Banco de dados relacional para persistência de usuários e reservas.
* **Redis:** Cache de alta performance para armazenamento temporário de códigos OTP (One-Time Password).

## ⚙️ Pré-requisitos

* Docker & Docker Compose instalados.
* Portas 8080, 8081 e 3001 livres no host.

## 🚀 Como Rodar a Aplicação

A aplicação foi desenhada para ser iniciada com um único comando, graças à orquestração de contêineres.

1.  **Clone o repositório:**
    ```bash
    git clone https://github.com/JVSM-GAMES/MixMeet-Backend
    cd MixMeet-Backend
    ```

2.  **Inicie o ambiente (Build & Run):**
    ```bash
    sudo docker-compose up --build -d
    ```

3.  **Verifique os serviços:**
    ```bash
    sudo docker-compose ps
    ```
    *Todos os serviços (`reservas-api`, `auth-api`, `whatsapp-service`, `db`, `redis`) devem estar com status `Up`.*

## 🔌 Endpoints e Documentação

* **API de Reservas (C#):** `http://localhost:8080/swagger` (Documentação Swagger disponível).
* **API de Autenticação (Python):** `http://localhost:8081/docs` (Swagger UI do FastAPI).
* **Serviço WhatsApp (Node.js):** `http://localhost:3001` (API Interna).

## 📱 Fluxo de Autenticação (WhatsApp)

O sistema utiliza um fluxo de autenticação inovador sem senhas:

1.  O Admin acessa a configuração do sistema com a senha "admin4mixmeet" e escaneia um **QR Code** com o whatsapp.
2.  O usuário solicita login via número de telefone.
3.  O serviço Python gera um código, salva no Redis e comanda o Node.js.
4.  O serviço Node.js envia o código via WhatsApp real para o usuário.
5.  O usuário valida o código e recebe um JWT para acessar a API C#.

## 🛠️ Tecnologias Utilizadas

* **.NET 8 (C#)**
* **Python 3.11 (FastAPI)**
* **Node.js 20 (Express + Baileys)**
* **PostgreSQL 15**
* **Redis 7**
* **Docker & Docker Compose**