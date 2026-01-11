-- =========================================================
-- Script de migración para eliminar FOREIGN KEYS
-- y ajustar las tablas Payment y PaymentItem
-- =========================================================
-- Este script elimina las dependencias con Estudiante, Periodo y Curso
-- para hacer el microservicio de pagos independiente.
-- =========================================================

USE [db_ac27fb_sistemagestiontram];
GO

PRINT '🔧 Iniciando migración de tablas Payment y PaymentItem...';
GO

-- =========================================================
-- 1. ELIMINAR FOREIGN KEY CONSTRAINTS DE PAYMENT
-- =========================================================

-- Buscar y eliminar FK hacia Estudiante
DECLARE @FKName_Estudiante NVARCHAR(255);
SELECT @FKName_Estudiante = name 
FROM sys.foreign_keys 
WHERE parent_object_id = OBJECT_ID('Payment') 
  AND referenced_object_id = OBJECT_ID('Estudiante');

IF @FKName_Estudiante IS NOT NULL
BEGIN
    DECLARE @SQL1 NVARCHAR(500) = 'ALTER TABLE Payment DROP CONSTRAINT ' + @FKName_Estudiante;
    EXEC sp_executesql @SQL1;
    PRINT '✅ Eliminada FK Payment -> Estudiante: ' + @FKName_Estudiante;
END
ELSE
BEGIN
    PRINT '⚠️ No se encontró FK Payment -> Estudiante';
END
GO

-- Buscar y eliminar FK hacia Periodo
DECLARE @FKName_Periodo NVARCHAR(255);
SELECT @FKName_Periodo = name 
FROM sys.foreign_keys 
WHERE parent_object_id = OBJECT_ID('Payment') 
  AND referenced_object_id = OBJECT_ID('Periodo');

IF @FKName_Periodo IS NOT NULL
BEGIN
    DECLARE @SQL2 NVARCHAR(500) = 'ALTER TABLE Payment DROP CONSTRAINT ' + @FKName_Periodo;
    EXEC sp_executesql @SQL2;
    PRINT '✅ Eliminada FK Payment -> Periodo: ' + @FKName_Periodo;
END
ELSE
BEGIN
    PRINT '⚠️ No se encontró FK Payment -> Periodo';
END
GO

-- =========================================================
-- 2. ELIMINAR FOREIGN KEY CONSTRAINTS DE PAYMENTITEM
-- =========================================================

-- Buscar y eliminar FK hacia Curso
DECLARE @FKName_Curso NVARCHAR(255);
SELECT @FKName_Curso = name 
FROM sys.foreign_keys 
WHERE parent_object_id = OBJECT_ID('PaymentItem') 
  AND referenced_object_id = OBJECT_ID('Curso');

IF @FKName_Curso IS NOT NULL
BEGIN
    DECLARE @SQL3 NVARCHAR(500) = 'ALTER TABLE PaymentItem DROP CONSTRAINT ' + @FKName_Curso;
    EXEC sp_executesql @SQL3;
    PRINT '✅ Eliminada FK PaymentItem -> Curso: ' + @FKName_Curso;
END
ELSE
BEGIN
    PRINT '⚠️ No se encontró FK PaymentItem -> Curso';
END
GO

-- =========================================================
-- 3. RENOMBRAR COLUMNAS PARA COINCIDIR CON EL MODELO C#
-- =========================================================

-- Renombrar stripe_payment_intent_id a stripe_session_id si existe
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payment') AND name = 'stripe_payment_intent_id')
BEGIN
    EXEC sp_rename 'Payment.stripe_payment_intent_id', 'stripe_session_id', 'COLUMN';
    PRINT '✅ Renombrada columna: stripe_payment_intent_id -> stripe_session_id';
END
ELSE IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payment') AND name = 'stripe_session_id')
BEGIN
    ALTER TABLE Payment ADD stripe_session_id NVARCHAR(255) NULL;
    PRINT '✅ Agregada columna: stripe_session_id';
END
ELSE
BEGIN
    PRINT '⚠️ La columna stripe_session_id ya existe';
END
GO

-- Agregar payment_type si no existe
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payment') AND name = 'payment_type')
BEGIN
    ALTER TABLE Payment ADD payment_type VARCHAR(50) NOT NULL DEFAULT 'Enrollment';
    PRINT '✅ Agregada columna: payment_type';
END
ELSE
BEGIN
    PRINT '⚠️ La columna payment_type ya existe';
END
GO

-- Agregar nombre_curso a PaymentItem si no existe
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PaymentItem') AND name = 'nombre_curso')
BEGIN
    ALTER TABLE PaymentItem ADD nombre_curso NVARCHAR(255) NULL;
    PRINT '✅ Agregada columna: nombre_curso a PaymentItem';
END
ELSE
BEGIN
    PRINT '⚠️ La columna nombre_curso ya existe en PaymentItem';
END
GO

-- Actualizar stripe_session_id para que sea UNIQUE si no lo es
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE object_id = OBJECT_ID('Payment') 
      AND name = 'UQ_Payment_stripe_session_id'
)
BEGIN
    ALTER TABLE Payment ADD CONSTRAINT UQ_Payment_stripe_session_id UNIQUE (stripe_session_id);
    PRINT '✅ Agregado constraint UNIQUE a stripe_session_id';
END
ELSE
BEGIN
    PRINT '⚠️ El constraint UNIQUE en stripe_session_id ya existe';
END
GO

-- =========================================================
-- 4. ACTUALIZAR ÍNDICES
-- =========================================================

-- Índice en payment_type
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE object_id = OBJECT_ID('Payment') 
      AND name = 'IX_Payment_payment_type'
)
BEGIN
    CREATE INDEX IX_Payment_payment_type ON Payment(payment_type);
    PRINT '✅ Creado índice: IX_Payment_payment_type';
END
GO

-- Índice en procesado
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE object_id = OBJECT_ID('Payment') 
      AND name = 'IX_Payment_procesado'
)
BEGIN
    CREATE INDEX IX_Payment_procesado ON Payment(procesado);
    PRINT '✅ Creado índice: IX_Payment_procesado';
END
GO

PRINT '✅✅✅ Migración completada exitosamente!';
PRINT '';
PRINT '📊 Resumen:';
PRINT '   - Eliminadas FK hacia Estudiante, Periodo y Curso';
PRINT '   - Renombradas columnas para coincidir con modelo C#';
PRINT '   - Agregados índices para mejor performance';
PRINT '   - Microservicio de pagos ahora es INDEPENDIENTE';
GO
