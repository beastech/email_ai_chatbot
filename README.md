# Email-based AI Question Answering System

## Overview

This project implements an **email-driven AI question answering system** using a microservices architecture. It enables end-users to simply send an email with their question, and receive an AI-generated answer directly back in their inbox.

Key components are decoupled microservices communicating through standard protocols:

- **Mail Server** to receive user emails
- **Azure Function** as an email processor microservice that polls the mailbox and handles logic
- **Azure OpenAI API** as an AI microservice providing intelligent responses
- **SMTP Server** to send reply emails back to users

By leveraging microservices, the system achieves scalability, modularity, and the ability to update components independently.

---

## Architecture Diagram

```mermaid
sequenceDiagram
    participant User as End User (Sends Email)
    participant MailServer as Mail Server
    participant AzureFunc as Azure Function (Email Processor)
    participant AzureOpenAI as Azure OpenAI API
    participant SMTPServer as SMTP Server
    User->>MailServer: Sends email question to user_email@example.com
    MailServer->>AzureFunc: Azure Function polls mailbox via IMAP and fetches new email
    AzureFunc->>AzureOpenAI: Sends question for AI response
    AzureOpenAI-->>AzureFunc: Returns AI-generated answer
    AzureFunc->>SMTPServer: Sends reply email with answer via SMTP
    SMTPServer->>User: Delivers AI answer to user's inbox
