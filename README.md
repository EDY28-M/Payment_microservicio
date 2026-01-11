# Payment Microservicio - Stripe Checkout

Microservicio de pagos integrado con Stripe Checkout para el sistema de gestión académica.

## 🚀 Características

- **Stripe Checkout**: Ventana de pago segura de Stripe (como OpenAI)
- **Pago de Matrícula**: Monto fijo de S/ 5.00
- **Pago de Cursos**: Múltiples cursos en una sola transacción
- **Webhooks**: Procesamiento automático de eventos de Stripe
- **JWT Authentication**: Compartido con el backend principal

## 📁 Estructura (Clean Architecture)

```
PaymentMicroservicio/
├── Domain/
│   ├── Entities/         # Payment, PaymentItem
│   └── Enums/           # PaymentStatus, PaymentType
├── Application/
│   ├── DTOs/            # Request/Response DTOs
│   └── Interfaces/      # IPaymentService, IStripeService
├── Infrastructure/
│   ├── Data/            # PaymentDbContext
│   ├── Repositories/    # PaymentRepository
│   └── Services/        # StripeService, PaymentService
└── Controllers/         # PaymentsController, WebhooksController
```

## 🔧 Configuración

### Variables de Entorno (Render)

| Variable | Descripción |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Connection string de SQL Server |
| `Stripe__SecretKey` | `sk_test_...` o `sk_live_...` |
| `Stripe__PublishableKey` | `pk_test_...` o `pk_live_...` |
| `Stripe__WebhookSecret` | `whsec_...` |
| `JwtSettings__SecretKey` | Misma key que el backend principal |
| `JwtSettings__Issuer` | `GestionAcademicaAPI` |
| `JwtSettings__Audience` | `GestionAcademicaClients` |

### Stripe Dashboard

1. Ve a [Stripe Dashboard](https://dashboard.stripe.com)
2. Copia las API Keys (test o live)
3. Configura el Webhook:
   - URL: `https://TU-SERVICIO.onrender.com/api/webhooks/stripe`
   - Eventos:
     - `checkout.session.completed`
     - `checkout.session.expired`
     - `payment_intent.succeeded`
     - `payment_intent.payment_failed`

## 📡 Endpoints

### Pagos

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/payments/checkout/matricula` | Crear checkout para matrícula |
| `POST` | `/api/payments/checkout/cursos` | Crear checkout para cursos |
| `GET` | `/api/payments/status/{id}` | Estado de un pago |
| `GET` | `/api/payments/verificar-matricula-pagada/{idEstudiante}/{idPeriodo}` | Verificar pago de matrícula |
| `GET` | `/api/payments/historial` | Historial de pagos |

### Webhooks

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/webhooks/stripe` | Recibir eventos de Stripe |

### Health

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `GET` | `/health` | Health check básico |
| `GET` | `/api/health/ready` | Health check con DB |

## 🔄 Flujo de Pago

```
1. Frontend llama POST /api/payments/checkout/matricula
2. Backend crea sesión de Stripe Checkout
3. Retorna { checkoutUrl: "https://checkout.stripe.com/..." }
4. Frontend redirige al usuario a checkoutUrl
5. Usuario paga en ventana de Stripe
6. Stripe redirige a successUrl
7. Stripe envía webhook a /api/webhooks/stripe
8. Backend procesa el pago y actualiza la base de datos
```

## 🗃️ Base de Datos

Usa las mismas tablas `Payment` y `PaymentItem` del sistema principal:

```sql
-- Ver create_payment_tables.sql en el proyecto principal
```

## 🏃 Desarrollo Local

```bash
# Restaurar dependencias
dotnet restore

# Ejecutar
dotnet run

# El servicio estará en: https://localhost:5001
# Swagger: https://localhost:5001/swagger
```

## 🚀 Deploy en Render

1. Conecta tu repositorio a Render
2. Crea un nuevo Web Service
3. Selecciona Docker
4. Configura las variables de entorno
5. Deploy!

## 📝 Licencia

MIT
