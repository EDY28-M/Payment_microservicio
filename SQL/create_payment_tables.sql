-- =========================================================
-- Script para crear las tablas de Payment y PaymentItem
-- Microservicio de Pagos - INDEPENDIENTE
-- =========================================================
-- IMPORTANTE: Este microservicio NO debe tener FOREIGN KEYS
-- hacia las tablas del sistema principal (Estudiante, Periodo, Curso)
-- para mantener la independencia de microservicios.
-- =========================================================

-- Verificar y crear tabla Payment
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payment')
BEGIN
    CREATE TABLE Payment (
        id INT PRIMARY KEY IDENTITY(1,1),
        idEstudiante INT NOT NULL, -- Sin FK: valor de referencia únicamente
        idPeriodo INT NOT NULL,    -- Sin FK: valor de referencia únicamente
        stripe_session_id NVARCHAR(255) NOT NULL UNIQUE,
        stripe_customer_id NVARCHAR(255) NULL,
        amount DECIMAL(10,2) NOT NULL,
        currency VARCHAR(3) NOT NULL DEFAULT 'PEN',
        status VARCHAR(50) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Succeeded', 'Failed', 'Canceled'
        payment_type VARCHAR(50) NOT NULL, -- 'Enrollment', 'Course'
        metadata_json NVARCHAR(MAX) NULL, -- JSON con detalles adicionales
        fecha_creacion DATETIME NOT NULL DEFAULT GETUTCDATE(),
        fecha_actualizacion DATETIME NULL,
        fecha_pago_exitoso DATETIME NULL,
        error_message NVARCHAR(1000) NULL,
        procesado BIT NOT NULL DEFAULT 0 -- Flag para evitar procesamiento duplicado
    );

    CREATE INDEX IX_Payment_idEstudiante ON Payment(idEstudiante);
    CREATE INDEX IX_Payment_idPeriodo ON Payment(idPeriodo);
    CREATE INDEX IX_Payment_stripe_session_id ON Payment(stripe_session_id);
    CREATE INDEX IX_Payment_status ON Payment(status);
    CREATE INDEX IX_Payment_payment_type ON Payment(payment_type);
    CREATE INDEX IX_Payment_procesado ON Payment(procesado);

    PRINT '✅ Tabla Payment creada exitosamente (sin FOREIGN KEYS)';
END
ELSE
BEGIN
    PRINT '⚠️ Tabla Payment ya existe';
END
GO

-- Verificar y crear tabla PaymentItem
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentItem')
BEGIN
    CREATE TABLE PaymentItem (
        id INT PRIMARY KEY IDENTITY(1,1),
        idPayment INT NOT NULL,
        idCurso INT NOT NULL,      -- Sin FK: valor de referencia únicamente
        nombre_curso NVARCHAR(255) NOT NULL,
        cantidad INT NOT NULL DEFAULT 1,
        precio_unitario DECIMAL(10,2) NOT NULL,
        subtotal DECIMAL(10,2) NOT NULL,
        FOREIGN KEY (idPayment) REFERENCES Payment(id) ON DELETE CASCADE
        -- NO hay FK hacia Curso: microservicio independiente
    );

    CREATE INDEX IX_PaymentItem_idPayment ON PaymentItem(idPayment);
    CREATE INDEX IX_PaymentItem_idCurso ON PaymentItem(idCurso);

    PRINT '✅ Tabla PaymentItem creada exitosamente';
END
ELSE
BEGIN
    PRINT '⚠️ Tabla PaymentItem ya existe';
END
GO

-- Script para verificar pagos de un estudiante (para debug)
/*
SELECT 
    p.id,
    p.idEstudiante,
    p.idPeriodo,
    p.stripe_payment_intent_id,
    p.amount,
    p.currency,
    p.status,
    p.metadata_json,
    p.fecha_creacion,
    p.fecha_pago_exitoso,
    p.procesado
FROM Payment p
WHERE p.idEstudiante = @IdEstudiante
  AND p.idPeriodo = @IdPeriodo
ORDER BY p.fecha_creacion DESC;
*/

PRINT 'Script de creación de tablas de Payment completado';
