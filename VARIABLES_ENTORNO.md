# Variables de Entorno - Sistema de Recibos de Pago

## 🔧 Backend (PaymentMicroservicio)

### Variables Requeridas

#### Base de Datos
```env
ConnectionStrings__DefaultConnection=Server=tu-servidor;Database=tu-base-datos;User Id=tu-usuario;Password=tu-password;TrustServerCertificate=True;
```

#### Stripe (REQUERIDO)
```env
Stripe__SecretKey=sk_test_...                    # Clave secreta de Stripe (Test o Live)
Stripe__PublishableKey=pk_test_...                # Clave pública de Stripe (Test o Live)
Stripe__WebhookSecret=whsec_...                   # Secreto del webhook de Stripe (obtener desde Stripe Dashboard)
```

#### JWT Authentication (REQUERIDO)
```env
JwtSettings__SecretKey=tu-clave-secreta-jwt-muy-larga-y-segura
JwtSettings__Issuer=GestionAcademicaAPI
JwtSettings__Audience=GestionAcademicaClients
```

### Variables Opcionales (con valores por defecto)

#### URLs de la Aplicación
```env
AppSettings__FrontendUrl=https://gestion-academica-frontend.onrender.com
AppSettings__BackendPrincipalUrl=https://gestion-academica-api.onrender.com
```

#### Configuración Institucional (para recibos)
```env
AppSettings__UniversityName=Universidad Nacional de San Agustín
AppSettings__FacultyName=Facultad de Ingeniería de Producción y Servicios
```

### Ejemplo para Render.com

En Render.com, configura estas variables en el servicio:

```
ConnectionStrings__DefaultConnection=Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;
Stripe__SecretKey=sk_live_...
Stripe__PublishableKey=pk_live_...
Stripe__WebhookSecret=whsec_...
JwtSettings__SecretKey=tu-clave-secreta-jwt
AppSettings__FrontendUrl=https://gestion-academica-frontend.onrender.com
AppSettings__UniversityName=Universidad Nacional de San Agustín
AppSettings__FacultyName=Facultad de Ingeniería de Producción y Servicios
```

### Ejemplo para Docker

En un archivo `.env` o en docker-compose.yml:

```yaml
environment:
  - ConnectionStrings__DefaultConnection=Server=sql-server;Database=payments;User Id=sa;Password=Password123!;TrustServerCertificate=True;
  - Stripe__SecretKey=sk_test_...
  - Stripe__PublishableKey=pk_test_...
  - Stripe__WebhookSecret=whsec_...
  - JwtSettings__SecretKey=tu-clave-secreta-jwt
  - AppSettings__FrontendUrl=http://localhost:5173
  - AppSettings__UniversityName=Universidad Nacional de San Agustín
  - AppSettings__FacultyName=Facultad de Ingeniería de Producción y Servicios
```

---

## 🎨 Frontend (React/Vite)

### Variables Requeridas

#### API del Microservicio de Pagos
```env
VITE_PAYMENT_API_URL=https://microservicios-pago.onrender.com/api
```

#### Stripe (para componentes de pago, si se usan)
```env
VITE_STRIPE_PUBLISHABLE_KEY=pk_test_...
```

### Variables Opcionales (con valores por defecto)

#### API del Backend Principal
```env
VITE_API_URL=https://gestion-academica-api.onrender.com/api
VITE_BACKEND_URL=https://gestion-academica-api.onrender.com
```

### Ejemplo para Render.com

En Render.com, configura estas variables en el servicio de frontend:

```
VITE_PAYMENT_API_URL=https://microservicios-pago.onrender.com/api
VITE_API_URL=https://gestion-academica-api.onrender.com/api
VITE_STRIPE_PUBLISHABLE_KEY=pk_live_...
```

### Ejemplo para Desarrollo Local

Crea un archivo `.env.local` en la raíz del proyecto frontend:

```env
# Desarrollo Local
VITE_PAYMENT_API_URL=http://localhost:5000/api
VITE_API_URL=http://localhost:5251/api
VITE_BACKEND_URL=http://localhost:5251
VITE_STRIPE_PUBLISHABLE_KEY=pk_test_...
```

---

## 📋 Checklist de Configuración

### Backend (PaymentMicroservicio)

- [ ] `ConnectionStrings__DefaultConnection` - Cadena de conexión a SQL Server
- [ ] `Stripe__SecretKey` - Clave secreta de Stripe (Test o Live)
- [ ] `Stripe__PublishableKey` - Clave pública de Stripe (Test o Live)
- [ ] `Stripe__WebhookSecret` - Secreto del webhook (obtener desde Stripe Dashboard)
- [ ] `JwtSettings__SecretKey` - Clave secreta para JWT (debe coincidir con el backend principal)
- [ ] `AppSettings__FrontendUrl` - URL del frontend para redirects de Stripe
- [ ] `AppSettings__UniversityName` - Nombre de la universidad (opcional)
- [ ] `AppSettings__FacultyName` - Nombre de la facultad (opcional)

### Frontend

- [ ] `VITE_PAYMENT_API_URL` - URL del microservicio de pagos
- [ ] `VITE_API_URL` - URL del backend principal (opcional, usa proxy en desarrollo)
- [ ] `VITE_STRIPE_PUBLISHABLE_KEY` - Clave pública de Stripe (si se usan componentes de Stripe)

---

## 🔐 Cómo Obtener las Claves de Stripe

1. **SecretKey y PublishableKey:**
   - Ve a [Stripe Dashboard](https://dashboard.stripe.com/)
   - Navega a **Developers > API keys**
   - Copia la **Secret key** (sk_test_... o sk_live_...)
   - Copia la **Publishable key** (pk_test_... o pk_live_...)

2. **WebhookSecret:**
   - Ve a [Stripe Dashboard](https://dashboard.stripe.com/)
   - Navega a **Developers > Webhooks**
   - Crea un nuevo endpoint webhook o edita uno existente
   - URL del endpoint: `https://tu-dominio.com/api/webhooks/stripe`
   - Eventos a escuchar:
     - `checkout.session.completed`
     - `checkout.session.expired`
     - `payment_intent.succeeded`
     - `payment_intent.payment_failed`
   - Copia el **Signing secret** (whsec_...)

---

## ⚠️ Notas Importantes

1. **JWT SecretKey**: Debe ser la misma clave que usa el backend principal (`API_REST_CURSOSACADEMICOS`) para que los tokens JWT sean válidos en ambos servicios.

2. **Stripe Keys**: 
   - Usa `sk_test_` y `pk_test_` para desarrollo/testing
   - Usa `sk_live_` y `pk_live_` para producción
   - **NUNCA** compartas las claves secretas públicamente

3. **WebhookSecret**: 
   - Cada endpoint webhook tiene su propio secreto
   - Si cambias el endpoint, necesitarás un nuevo secreto
   - El secreto es diferente para test y live mode

4. **Variables de Entorno en .NET**:
   - Usa doble guion bajo `__` para separar secciones (ej: `Stripe__SecretKey`)
   - O usa dos puntos `:` en algunos sistemas (ej: `Stripe:SecretKey`)

5. **Variables de Entorno en Vite**:
   - Todas las variables deben empezar con `VITE_` para ser accesibles en el código
   - Se acceden con `import.meta.env.VITE_NOMBRE_VARIABLE`

---

## 🧪 Verificación

### Backend
```bash
# Verificar que las variables estén cargadas
dotnet run --no-build
# Revisar logs al iniciar - no debe haber errores de configuración
```

### Frontend
```bash
# Verificar que las variables estén disponibles
npm run dev
# Abrir consola del navegador y verificar:
console.log(import.meta.env.VITE_PAYMENT_API_URL)
```

---

## 📚 Referencias

- [Stripe API Keys](https://stripe.com/docs/keys)
- [Stripe Webhooks](https://stripe.com/docs/webhooks)
- [.NET Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Vite Environment Variables](https://vitejs.dev/guide/env-and-mode.html)
