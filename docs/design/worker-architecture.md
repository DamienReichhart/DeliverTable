# Worker Architecture

## How an email gets sent

```mermaid
sequenceDiagram
    participant Client
    participant API as API Server
    participant DB as PostgreSQL
    participant RMQ as RabbitMQ
    participant Worker

    Client->>API: Create order
    API->>DB: Save order
    API->>DB: Save EmailJob (Pending)
    API->>RMQ: Publish { JobId: 42 }
    API->>Client: 200 OK

    RMQ->>Worker: Deliver message
    Worker->>DB: Load EmailJob 42
    Worker->>DB: Update status → Processing
    Worker->>Worker: Render Razor template
    Worker->>Worker: Send via SMTP
    Worker->>DB: Update status → Sent
    Worker->>RMQ: Ack message
```

## Job lifecycle

```mermaid
stateDiagram-v2
    [*] --> Pending: API creates job
    Pending --> Processing: Worker picks up
    Processing --> Sent: Email sent OK
    Processing --> RetryScheduled: SMTP error (retries left)
    RetryScheduled --> Pending: After delay (1m / 5m / 15m)
    Processing --> DeadLettered: Max retries exceeded
    Sent --> [*]
    DeadLettered --> [*]
```

## System overview

```mermaid
graph LR
    API[API Server] -->|publish job ID| RMQ[RabbitMQ]
    API -->|save job| DB[(PostgreSQL)]
    RMQ -->|deliver| Worker
    Worker -->|load/update job| DB
    Worker -->|send email| SMTP[Hostinger SMTP]
    Worker -.->|retry after delay| RMQ
```

## RabbitMQ topology

```mermaid
graph TD
    EX[delivertable.jobs<br/>exchange] -->|routing: email| Q[delivertable.jobs.email<br/>main queue]
    Q -->|on failure| DLX[delivertable.jobs.dlx<br/>retry exchange]
    DLX -->|retry 1| R1[retry.1<br/>TTL 1min]
    DLX -->|retry 2| R2[retry.2<br/>TTL 5min]
    DLX -->|retry 3| R3[retry.3<br/>TTL 15min]
    R1 -->|after TTL| EX
    R2 -->|after TTL| EX
    R3 -->|after TTL| EX
    Q -->|max retries exceeded| DEAD[delivertable.jobs.email.dead]
```

## Recovery mechanisms

```mermaid
graph TD
    subgraph Sweep Service — runs every 60s
        S1[Find Pending jobs > 2 min old] -->|re-publish to RabbitMQ| RMQ[RabbitMQ]
        S2[Find Processing jobs > 5 min old] -->|reset to Pending| DB[(PostgreSQL)]
    end
```
