-- =========================================================
-- Script para crear la tabla ServiceCatalog
-- Catálogo de servicios disponibles para pago
-- =========================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ServiceCatalog')
BEGIN
    CREATE TABLE ServiceCatalog (
        id INT PRIMARY KEY IDENTITY(1,1),
        code NVARCHAR(50) NOT NULL UNIQUE,
        nombre NVARCHAR(255) NOT NULL,
        descripcion NVARCHAR(500) NOT NULL,
        detalle NVARCHAR(500) NULL,
        monto DECIMAL(10,2) NOT NULL,
        categoria NVARCHAR(100) NOT NULL,
        tipo_pago NVARCHAR(50) NOT NULL,
        activo BIT NOT NULL DEFAULT 1,
        orden INT NOT NULL DEFAULT 0,
        fecha_creacion DATETIME NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE UNIQUE INDEX IX_ServiceCatalog_code ON ServiceCatalog(code);
    CREATE INDEX IX_ServiceCatalog_activo ON ServiceCatalog(activo);

    PRINT '✅ Tabla ServiceCatalog creada exitosamente';
END
ELSE
BEGIN
    PRINT '⚠️ Tabla ServiceCatalog ya existe';
END
GO

-- Insertar servicios iniciales
IF NOT EXISTS (SELECT 1 FROM ServiceCatalog WHERE code = 'matricula')
BEGIN
    INSERT INTO ServiceCatalog (code, nombre, descripcion, detalle, monto, categoria, tipo_pago, activo, orden)
    VALUES 
    ('matricula', 
     'RESERVA DE MATRÍCULA POR SEMESTRE (PREGRADO)', 
     'Registrar en el sistema académico sus cursos',
     'Costo por c/reserva de matrícula por semestre (pregrado)',
     5.00, 'Matrícula', 'matricula', 1, 1),

    ('seguimiento-curricular',
     'SEGUIMIENTO CURRICULAR PREGRADO',
     'Trámite de seguimiento curricular',
     'Seguimiento y verificación del avance curricular',
     15.00, 'Trámites', 'servicio', 1, 2),

    ('constancia-matricula',
     'CONSTANCIA DE MATRÍCULA',
     'Documento oficial de matrícula vigente',
     'Emisión de constancia de matrícula del período actual',
     10.00, 'Constancias', 'servicio', 1, 3),

    ('constancia-notas',
     'CONSTANCIA DE NOTAS',
     'Registro oficial de calificaciones',
     'Emisión de constancia con historial de notas',
     15.00, 'Constancias', 'servicio', 1, 4),

    ('carnet-duplicado',
     'CARNET UNIVERSITARIO - DUPLICADO',
     'Trámite de duplicado de carnet',
     'Solicitud de duplicado del carnet universitario',
     25.00, 'Trámites', 'servicio', 1, 5),

    ('examen-culminacion',
     'EXAMEN EXCEPCIONAL - CULMINACIÓN DE PLAN DE ESTUDIOS PREGRADO',
     'Tramitar',
     'Examen excepcional para culminación de plan de estudios',
     105.00, 'Exámenes', 'servicio', 1, 6),

    ('certificado-estudios',
     'CERTIFICADO DE ESTUDIOS',
     'Documento oficial de estudios realizados',
     'Emisión de certificado oficial de estudios completos',
     50.00, 'Constancias', 'servicio', 1, 7),

    ('constancia-egresado',
     'CONSTANCIA DE EGRESADO',
     'Documento que acredita condición de egresado',
     'Emisión de constancia de egresado de la universidad',
     20.00, 'Constancias', 'servicio', 1, 8);

    PRINT '✅ 8 servicios insertados en el catálogo';
END
ELSE
BEGIN
    PRINT '⚠️ Los servicios ya existen en el catálogo';
END
GO
