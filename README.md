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
