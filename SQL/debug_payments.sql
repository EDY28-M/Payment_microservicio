-- =========================================================
-- Script de DEBUG para verificar pagos
-- =========================================================
USE [db_ac27fb_sistemagestiontram];
GO

-- 1. Ver TODOS los pagos del estudiante 74 (el que acaba de pagar)
SELECT 
    id,
    idEstudiante,
    idPeriodo,
    stripe_session_id,
    amount,
    currency,
    [status],
    payment_type,
    procesado,
    fecha_creacion,
    fecha_pago_exitoso,
    metadata_json
FROM [dbo].[Payment]
WHERE idEstudiante = 74
ORDER BY fecha_creacion DESC;
GO

-- 2. Ver los últimos 10 pagos
SELECT TOP 10
    id,
    idEstudiante,
    idPeriodo,
    stripe_session_id,
    amount,
    [status],
    payment_type,
    procesado,
    fecha_creacion,
    fecha_pago_exitoso
FROM [dbo].[Payment]
ORDER BY fecha_creacion DESC;
GO

-- 3. Verificar pagos "Succeeded" pero NO procesados
SELECT 
    id,
    idEstudiante,
    idPeriodo,
    stripe_session_id,
    [status],
    payment_type,
    procesado,
    fecha_creacion
FROM [dbo].[Payment]
WHERE [status] = 'Succeeded' 
  AND procesado = 0;
GO

-- 4. Ver pagos de matrícula por estudiante y período
SELECT 
    idEstudiante,
    idPeriodo,
    COUNT(*) as TotalPagos,
    SUM(CASE WHEN [status] = 'Succeeded' THEN 1 ELSE 0 END) as PagosExitosos,
    SUM(CASE WHEN procesado = 1 THEN 1 ELSE 0 END) as PagosProcesados
FROM [dbo].[Payment]
WHERE payment_type = 'Enrollment'
GROUP BY idEstudiante, idPeriodo
ORDER BY idEstudiante, idPeriodo;
GO

-- 5. Si quieres MARCAR MANUALMENTE un pago como procesado (SOLO PARA DEBUG):
/*
UPDATE Payment
SET procesado = 1,
    fecha_actualizacion = GETUTCDATE()
WHERE id = [ID_DEL_PAGO]; -- Reemplaza [ID_DEL_PAGO] con el ID real
*/

-- 6. Ver estructura de la tabla Payment
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Payment'
ORDER BY ORDINAL_POSITION;
