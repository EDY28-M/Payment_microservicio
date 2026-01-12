# Sistema de Recibos Digitales de Pago

## Descripción

Sistema completo de recibos digitales para pagos de matrícula mediante Stripe Checkout. Los recibos se generan automáticamente cuando Stripe confirma el pago mediante webhooks verificados.

## Características

- **Idempotencia**: Si el mismo evento de Stripe llega 2 veces, no se duplica el recibo
- **Seguridad**: El recibo solo se crea cuando Stripe confirma el pago mediante webhook verificado (firma)
- **Autenticación**: El endpoint que entrega el recibo verifica que pertenece al estudiante autenticado
- **Diseño Institucional**: Recibos con diseño administrativo, sin decoración excesiva

## Estructura de Base de Datos

### Tabla: PaymentReceipt

```sql
CREATE TABLE [dbo].[PaymentReceipt] (
    [id] INT PRIMARY KEY IDENTITY(1,1),
    [receipt_code] NVARCHAR(50) NOT NULL UNIQUE,
    [stripe_session_id] NVARCHAR(255) NOT NULL UNIQUE,
    [payment_intent_id] NVARCHAR(255) NULL,
    [student_id] INT NOT NULL,
    [student_code] NVARCHAR(50) NOT NULL,
    [student_name] NVARCHAR(200) NOT NULL,
    [university_name] NVARCHAR(200) NOT NULL,
    [faculty_name] NVARCHAR(200) NOT NULL,
    [concept] NVARCHAR(200) NOT NULL,
    [period] NVARCHAR(50) NOT NULL,
    [academic_year] INT NOT NULL,
    [amount] DECIMAL(10,2) NOT NULL,
    [currency] NVARCHAR(3) NOT NULL DEFAULT 'PEN',
    [status] NVARCHAR(50) NOT NULL DEFAULT 'PAID',
    [paid_at] DATETIME2 NOT NULL,
    [stripe_event_id] NVARCHAR(255) NOT NULL,
    [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [updated_at] DATETIME2 NULL
);
```

Ejecutar el script SQL: `SQL/create_payment_receipts_table.sql`

## Configuración

### Variables de Entorno

```env
# Stripe
Stripe__SecretKey=sk_test_...
Stripe__PublishableKey=pk_test_...
Stripe__WebhookSecret=whsec_...

# Configuración Institucional (opcional, se pueden usar valores por defecto)
AppSettings__UniversityName=Universidad Nacional de San Agustín
AppSettings__FacultyName=Facultad de Ingeniería de Producción y Servicios

# Frontend
AppSettings__FrontendUrl=https://gestion-academica-frontend.onrender.com
```

### appsettings.json

```json
{
  "AppSettings": {
    "UniversityName": "Universidad Nacional de San Agustín",
    "FacultyName": "Facultad de Ingeniería de Producción y Servicios",
    "FrontendUrl": "https://gestion-academica-frontend.onrender.com"
  },
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

## Flujo de Proceso

### 1. Crear Checkout Session

El frontend llama al endpoint para crear una sesión de checkout:

```http
POST /api/payments/checkout/matricula
Authorization: Bearer {token}
Content-Type: application/json

{
  "idPeriodo": 12,
  "tipoPago": "matricula"
}
```

**Nota**: Los datos del estudiante (studentCode, studentName, period, academicYear) se pueden incluir en metadata cuando se crea el checkout session desde el backend principal. Si no están disponibles, se usarán valores por defecto o se intentará obtenerlos desde la sesión de Stripe.

### 2. Webhook de Stripe

Cuando Stripe confirma el pago, envía un webhook a:

```http
POST /api/webhooks/stripe
Stripe-Signature: {signature}
Content-Type: application/json

{
  "type": "checkout.session.completed",
  "data": {
    "object": {
      "id": "cs_test_...",
      "payment_status": "paid",
      "amount_total": 500,
      "currency": "pen",
      "metadata": {
        "idEstudiante": "16",
        "idPeriodo": "12",
        "tipo": "matricula"
      }
    }
  }
}
```

El webhook:
1. Verifica la firma del webhook con `StripeSettings:WebhookSecret`
2. Procesa el pago (marca como exitoso)
3. Crea el recibo digital (idempotente - verifica que no exista por `stripe_event_id` o `stripe_session_id`)

### 3. Obtener Recibo

El frontend obtiene el recibo después del pago:

```http
GET /api/receipts/by-session/{sessionId}
Authorization: Bearer {token}
```

**Respuesta:**

```json
{
  "id": 1,
  "receiptCode": "REC-2029-000001",
  "stripeSessionId": "cs_test_...",
  "paymentIntentId": "pi_test_...",
  "studentId": 16,
  "studentCode": "EST202601110082",
  "studentName": "Mel Milena a",
  "universityName": "Universidad Nacional de San Agustín",
  "facultyName": "Facultad de Ingeniería de Producción y Servicios",
  "concept": "Matrícula Académica",
  "period": "2029-II",
  "academicYear": 2029,
  "amount": 5.00,
  "currency": "PEN",
  "status": "PAID",
  "paidAt": "2029-01-11T18:04:00Z",
  "stripeEventId": "evt_test_...",
  "createdAt": "2029-01-11T18:04:01Z"
}
```

## Idempotencia

El sistema implementa idempotencia de dos maneras:

1. **Por `stripe_event_id`**: Si ya existe un recibo con el mismo `stripe_event_id`, no se crea otro
2. **Por `stripe_session_id`**: Si ya existe un recibo con el mismo `stripe_session_id`, no se crea otro

Esto asegura que si el mismo evento de Stripe llega múltiples veces (retry, reenvío), no se dupliquen los recibos.

## Seguridad

- **Webhook Verification**: Todos los webhooks se verifican con la firma de Stripe antes de procesarse
- **Autenticación**: El endpoint de obtener recibo requiere autenticación JWT
- **Autorización**: Se verifica que el recibo pertenezca al estudiante autenticado (por `studentId`)

## Frontend

### Página de Recibo

Ruta: `/estudiante/pago-exitoso?session_id={session_id}`

La página:
1. Obtiene el `session_id` de la query string
2. Llama a `GET /api/receipts/by-session/{sessionId}`
3. Si el recibo no existe aún (404), reintenta cada 2 segundos por hasta 10 segundos (5 intentos)
4. Muestra el recibo con diseño institucional
5. Permite imprimir el recibo usando `window.print()`

### Diseño

- **Paleta**: Zinc/Grises (neutro, institucional)
- **Tipografía**: Sobria, legible
- **Sin decoración**: No hay iconos decorativos ni emojis
- **Botón**: Verde sobrio (#2E7D32) para "Imprimir / Guardar"
- **Impresión**: CSS print-friendly para imprimir el recibo

## Ejemplos de Uso

### Crear Checkout Session (desde Backend Principal)

Cuando el backend principal crea una sesión de checkout, puede incluir metadata adicional:

```csharp
var metadata = new Dictionary<string, string>
{
    { "idEstudiante", estudiante.Id.ToString() },
    { "idPeriodo", periodo.Id.ToString() },
    { "tipo", "matricula" },
    { "studentCode", estudiante.Codigo },
    { "studentName", $"{estudiante.Nombres} {estudiante.Apellidos}" },
    { "period", periodo.Nombre },
    { "academicYear", periodo.Anio.ToString() },
    { "universityName", "Universidad Nacional de San Agustín" },
    { "facultyName", "Facultad de Ingeniería de Producción y Servicios" },
    { "concept", "Matrícula Académica" }
};

// Llamar al microservicio con metadata
```

### Obtener Recibo (Frontend)

```typescript
import paymentApi from '@/lib/paymentApi';

const sessionId = searchParams.get('session_id');
const response = await paymentApi.get(`/receipts/by-session/${sessionId}`);
const receipt = response.data;

// Mostrar recibo
```

## Troubleshooting

### El recibo no se crea después del pago

1. Verificar que el webhook esté configurado en Stripe Dashboard
2. Verificar que `StripeSettings:WebhookSecret` esté correcto
3. Verificar logs del webhook en el backend
4. Verificar que el evento `checkout.session.completed` esté siendo manejado

### Error 404 al obtener recibo

1. El webhook puede no haber procesado aún - el frontend reintenta automáticamente
2. Verificar que el `session_id` sea correcto
3. Verificar que el estudiante esté autenticado
4. Verificar que el recibo pertenezca al estudiante autenticado

### Recibo duplicado

- El sistema es idempotente, pero si ocurre un error de base de datos entre la verificación y la inserción, podría duplicarse
- Verificar constraints UNIQUE en la base de datos (`stripe_event_id`, `stripe_session_id`)

## Notas Importantes

- **NO confíes en el frontend**: El recibo solo se crea cuando Stripe confirma el pago mediante webhook
- **Datos snapshot**: Los datos del estudiante se guardan como snapshot en el recibo (no hay foreign keys)
- **Microservicio independiente**: El microservicio no tiene dependencias del backend principal para los recibos
- **Metadata de Stripe**: Se recomienda incluir metadata adicional al crear el checkout session para mejores datos en el recibo
