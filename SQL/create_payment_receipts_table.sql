-- Tabla de Recibos de Pago (PaymentReceipt)
-- Esta tabla almacena recibos digitales generados cuando Stripe confirma el pago mediante webhook

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

-- Índices para búsquedas rápidas
CREATE UNIQUE INDEX [UQ_PaymentReceipt_receipt_code] ON [dbo].[PaymentReceipt]([receipt_code]);
CREATE UNIQUE INDEX [UQ_PaymentReceipt_stripe_session_id] ON [dbo].[PaymentReceipt]([stripe_session_id]);
CREATE UNIQUE INDEX [UQ_PaymentReceipt_stripe_event_id] ON [dbo].[PaymentReceipt]([stripe_event_id]);
CREATE INDEX [IX_PaymentReceipt_student_id] ON [dbo].[PaymentReceipt]([student_id]);
CREATE INDEX [IX_PaymentReceipt_student_code] ON [dbo].[PaymentReceipt]([student_code]);
CREATE INDEX [IX_PaymentReceipt_status] ON [dbo].[PaymentReceipt]([status]);
CREATE INDEX [IX_PaymentReceipt_created_at] ON [dbo].[PaymentReceipt]([created_at]);

-- Nota: No hay foreign keys hacia Estudiante/Periodo porque este es un microservicio independiente
-- Los datos del estudiante se almacenan como snapshot en el recibo

GO
