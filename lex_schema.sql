-- =====================================================================
--  LEX — Esquema de base de datos (versión funcional para el prototipo)
--  Marketplace que conecta estudiantes universitarios con clientes.
--
--  Basado en la maqueta original del equipo, con tres tipos de correcciones:
--   1. Typos corregidos (intitucion -> institucion).
--   2. FKs de 'trabajo' convertidas a NULLABLE (un trabajo nace de UN origen).
--   3. Campos funcionales agregados: estados, montos, escrow, fechas,
--      consentimiento de salud, historial de estados y verificacion.
--
--  Dialecto: pensado para portabilidad. Para SQLite quitar AUTO_INCREMENT
--  y usar INTEGER PRIMARY KEY AUTOINCREMENT; el ORM (EF Core) lo maneja solo.
-- =====================================================================


-- ---------------------------------------------------------------------
--  BLOQUE 1 — Identidad y roles
-- ---------------------------------------------------------------------

CREATE TABLE usuario (
  usuario_id        INT          NOT NULL AUTO_INCREMENT,
  email             VARCHAR(150) NOT NULL UNIQUE,
  password_hash     VARCHAR(255) NOT NULL,
  nombre_completo   VARCHAR(150) NOT NULL,
  telefono          VARCHAR(30),
  fecha_registro    DATETIME     NOT NULL,
  activo            BOOLEAN      NOT NULL DEFAULT TRUE,
  PRIMARY KEY (usuario_id)
);

CREATE TABLE rol (
  rol_id  INT         NOT NULL AUTO_INCREMENT,
  nombre  VARCHAR(50) NOT NULL UNIQUE,   -- Estudiante, Cliente, Agencia, Admin
  PRIMARY KEY (rol_id)
);

CREATE TABLE usuario_rol (
  rol_id      INT NOT NULL,
  usuario_id  INT NOT NULL,
  PRIMARY KEY (rol_id, usuario_id),
  FOREIGN KEY (rol_id)     REFERENCES rol(rol_id),
  FOREIGN KEY (usuario_id) REFERENCES usuario(usuario_id)
);


-- ---------------------------------------------------------------------
--  BLOQUE 2 — Validacion institucional (el foso defensivo de LEX)
-- ---------------------------------------------------------------------

CREATE TABLE tipo_institucion (
  tipo_institucion_id INT         NOT NULL AUTO_INCREMENT,
  nombre              VARCHAR(80) NOT NULL,   -- Universidad, Instituto terciario...
  PRIMARY KEY (tipo_institucion_id)
);

CREATE TABLE institucion (
  institucion_id      INT          NOT NULL AUTO_INCREMENT,
  nombre              VARCHAR(150) NOT NULL,
  tipo_institucion_id INT          NOT NULL,
  provincia           VARCHAR(80),
  ciudad              VARCHAR(80),
  PRIMARY KEY (institucion_id),
  FOREIGN KEY (tipo_institucion_id) REFERENCES tipo_institucion(tipo_institucion_id)
);

CREATE TABLE carrera (
  carrera_id        INT          NOT NULL AUTO_INCREMENT,
  nombre            VARCHAR(150) NOT NULL,
  institucion_id    INT          NOT NULL,
  area_conocimiento VARCHAR(80),   -- define si habilita servicios de Salud, etc.
  PRIMARY KEY (carrera_id),
  FOREIGN KEY (institucion_id) REFERENCES institucion(institucion_id)
);


-- ---------------------------------------------------------------------
--  BLOQUE 3 — Perfil del estudiante (la oferta)
-- ---------------------------------------------------------------------

CREATE TABLE perfil_estudiante (
  usuario_id            INT          NOT NULL,
  bio                   VARCHAR(500),
  anio_cursado          INT,
  calificacion_promedio DECIMAL(3,2) NOT NULL DEFAULT 0,
  cantidad_trabajos     INT          NOT NULL DEFAULT 0,
  disponible            BOOLEAN      NOT NULL DEFAULT TRUE,
  PRIMARY KEY (usuario_id),
  FOREIGN KEY (usuario_id) REFERENCES usuario(usuario_id)
);

-- Vinculo estudiante <-> carrera, con el estado de verificacion incluido.
-- estado_verificacion: 0=Pendiente, 1=Verificado, 2=Rechazado
CREATE TABLE estudiante_carrera (
  estudiante_id        INT NOT NULL,
  carrera_id           INT NOT NULL,
  estado_verificacion  INT NOT NULL DEFAULT 0,
  fecha_verificacion   DATETIME,
  documento_comprobante VARCHAR(255),
  PRIMARY KEY (estudiante_id, carrera_id),
  FOREIGN KEY (estudiante_id) REFERENCES perfil_estudiante(usuario_id),
  FOREIGN KEY (carrera_id)    REFERENCES carrera(carrera_id)
);


-- ---------------------------------------------------------------------
--  BLOQUE 4 — Perfiles de demanda (cliente y agencia)
--  Decision del equipo: la agencia es un perfil propio que cuelga de
--  usuario (NO es subtipo de cliente).
-- ---------------------------------------------------------------------

-- tipo_cliente: 0=Particular, 1=Empresa
CREATE TABLE perfil_cliente (
  usuario_id  INT NOT NULL,
  tipo_cliente INT NOT NULL DEFAULT 0,
  PRIMARY KEY (usuario_id),
  FOREIGN KEY (usuario_id) REFERENCES usuario(usuario_id)
);

CREATE TABLE datos_particular (
  usuario_id INT         NOT NULL,
  dni        VARCHAR(20),
  PRIMARY KEY (usuario_id),
  FOREIGN KEY (usuario_id) REFERENCES perfil_cliente(usuario_id)
);

CREATE TABLE datos_empresa (
  usuario_id   INT          NOT NULL,
  razon_social VARCHAR(150),
  cuit         VARCHAR(20),
  rubro        VARCHAR(100),
  PRIMARY KEY (usuario_id),
  FOREIGN KEY (usuario_id) REFERENCES perfil_cliente(usuario_id)
);

-- Perfil propio aparte (cuelga de usuario, segun decision del equipo)
CREATE TABLE perfil_agencia (
  usuario_id    INT          NOT NULL,
  nombre_agencia VARCHAR(150),
  rubro         VARCHAR(100),
  sitio_web     VARCHAR(150),
  PRIMARY KEY (usuario_id),
  FOREIGN KEY (usuario_id) REFERENCES usuario(usuario_id)
);


-- ---------------------------------------------------------------------
--  BLOQUE 5 — Catalogo de servicios (Flujo 1: contratacion directa)
-- ---------------------------------------------------------------------

CREATE TABLE tipo_servicio (
  tipo_servicio_id    INT         NOT NULL AUTO_INCREMENT,
  nombre              VARCHAR(80) NOT NULL,   -- Digital, Clase, Salud, Otro
  requiere_supervision BOOLEAN    NOT NULL DEFAULT FALSE,  -- activa logica de Salud
  PRIMARY KEY (tipo_servicio_id)
);

CREATE TABLE servicio (
  id_servicio        INT          NOT NULL AUTO_INCREMENT,
  estudiante_id      INT          NOT NULL,
  tipo_servicio_id   INT          NOT NULL,
  titulo             VARCHAR(150) NOT NULL,
  descripcion        VARCHAR(1000),
  precio             DECIMAL(12,2) NOT NULL,
  tiempo_entrega_dias INT,
  activo             BOOLEAN      NOT NULL DEFAULT TRUE,
  fecha_publicacion  DATETIME     NOT NULL,
  PRIMARY KEY (id_servicio),
  FOREIGN KEY (estudiante_id)    REFERENCES perfil_estudiante(usuario_id),
  FOREIGN KEY (tipo_servicio_id) REFERENCES tipo_servicio(tipo_servicio_id)
);


-- ---------------------------------------------------------------------
--  BLOQUE 6 — Demanda abierta (Flujo 2: solicitud -> postulacion)
-- ---------------------------------------------------------------------

-- estado: 0=Abierta, 1=Cerrada, 2=Cancelada
CREATE TABLE solicitud (
  id_solicitud         INT          NOT NULL AUTO_INCREMENT,
  cliente_id           INT          NOT NULL,
  tipo_servicio_id     INT,
  titulo               VARCHAR(150) NOT NULL,
  descripcion          VARCHAR(1000),
  presupuesto_estimado DECIMAL(12,2),
  estado               INT          NOT NULL DEFAULT 0,
  fecha_creacion       DATETIME     NOT NULL,
  fecha_cierre         DATETIME,
  PRIMARY KEY (id_solicitud),
  FOREIGN KEY (cliente_id)       REFERENCES perfil_cliente(usuario_id),
  FOREIGN KEY (tipo_servicio_id) REFERENCES tipo_servicio(tipo_servicio_id)
);

-- estado: 0=Enviada, 1=Aceptada, 2=Rechazada
CREATE TABLE postulacion (
  id_postulacion    INT           NOT NULL AUTO_INCREMENT,
  id_solicitud      INT           NOT NULL,
  estudiante_id     INT           NOT NULL,
  mensaje           VARCHAR(1000),
  monto_propuesto   DECIMAL(12,2),
  estado            INT           NOT NULL DEFAULT 0,
  fecha_postulacion DATETIME      NOT NULL,
  PRIMARY KEY (id_postulacion),
  FOREIGN KEY (id_solicitud)  REFERENCES solicitud(id_solicitud),
  FOREIGN KEY (estudiante_id) REFERENCES perfil_estudiante(usuario_id)
);


-- ---------------------------------------------------------------------
--  BLOQUE 7 — Salud (Flujo 3, como extension del motor)
-- ---------------------------------------------------------------------

CREATE TABLE pacientes (
  paciente_id     INT          NOT NULL AUTO_INCREMENT,
  cliente_id      INT          NOT NULL,
  nombre_completo VARCHAR(150) NOT NULL,
  edad            INT,
  notas           VARCHAR(500),
  PRIMARY KEY (paciente_id),
  FOREIGN KEY (cliente_id) REFERENCES perfil_cliente(usuario_id)
);


-- ---------------------------------------------------------------------
--  BLOQUE 8 — Motor transaccional (convergencia de los tres flujos)
--  CORRECCION CLAVE: id_servicio, id_postulacion y paciente_id ahora son
--  NULLABLE. Un trabajo nace de un servicio directo O de una postulacion
--  aceptada; paciente_id solo se completa en trabajos del area Salud.
-- ---------------------------------------------------------------------

-- origen: 0=Directo (desde servicio), 1=Postulacion (desde solicitud)
-- estado: 0=Pendiente, 1=Aceptado, 2=EnCurso, 3=Completado, 4=Cancelado
CREATE TABLE trabajo (
  id_trabajo      INT           NOT NULL AUTO_INCREMENT,
  estudiante_id   INT           NOT NULL,
  cliente_id      INT           NOT NULL,
  tipo_servicio_id INT,
  origen          INT           NOT NULL DEFAULT 0,
  id_servicio     INT           NULL,
  id_postulacion  INT           NULL,
  paciente_id     INT           NULL,
  estado          INT           NOT NULL DEFAULT 0,
  monto           DECIMAL(12,2) NOT NULL,
  fecha_creacion  DATETIME      NOT NULL,
  fecha_inicio    DATETIME,
  fecha_fin       DATETIME,
  PRIMARY KEY (id_trabajo),
  FOREIGN KEY (estudiante_id)    REFERENCES perfil_estudiante(usuario_id),
  FOREIGN KEY (cliente_id)       REFERENCES perfil_cliente(usuario_id),
  FOREIGN KEY (tipo_servicio_id) REFERENCES tipo_servicio(tipo_servicio_id),
  FOREIGN KEY (id_servicio)      REFERENCES servicio(id_servicio),
  FOREIGN KEY (id_postulacion)   REFERENCES postulacion(id_postulacion),
  FOREIGN KEY (paciente_id)      REFERENCES pacientes(paciente_id)
);

-- Trazabilidad de cambios de estado de cada trabajo.
CREATE TABLE trabajo_historial (
  id_historial    INT      NOT NULL AUTO_INCREMENT,
  id_trabajo      INT      NOT NULL,
  estado_anterior INT,
  estado_nuevo    INT      NOT NULL,
  fecha           DATETIME NOT NULL,
  usuario_id      INT,    -- quien provoco el cambio
  PRIMARY KEY (id_historial),
  FOREIGN KEY (id_trabajo) REFERENCES trabajo(id_trabajo),
  FOREIGN KEY (usuario_id) REFERENCES usuario(usuario_id)
);


-- ---------------------------------------------------------------------
--  BLOQUE 9 — Dinero (escrow + take rate)
--  La comision LEX y el monto del estudiante quedan registrados.
--  estado: 0=Retenido, 1=Liberado, 2=Reembolsado
-- ---------------------------------------------------------------------

CREATE TABLE pago (
  id_pago             INT           NOT NULL AUTO_INCREMENT,
  id_trabajo          INT           NOT NULL,
  monto_total         DECIMAL(12,2) NOT NULL,
  porcentaje_comision DECIMAL(5,2)  NOT NULL,   -- ej. 10.00 = 10%
  comision_lex        DECIMAL(12,2) NOT NULL,
  monto_estudiante    DECIMAL(12,2) NOT NULL,
  estado              INT           NOT NULL DEFAULT 0,
  metodo_pago         VARCHAR(50),
  fecha_retencion     DATETIME,
  fecha_liberacion    DATETIME,
  PRIMARY KEY (id_pago),
  FOREIGN KEY (id_trabajo) REFERENCES trabajo(id_trabajo)
);


-- ---------------------------------------------------------------------
--  BLOQUE 10 — Salud: consentimiento informado del turno clinico
--  Cuelga del trabajo; existe solo cuando el tipo de servicio requiere
--  supervision. Es el registro legal del area de salud.
-- ---------------------------------------------------------------------

CREATE TABLE consentimiento (
  id_consentimiento     INT           NOT NULL AUTO_INCREMENT,
  id_trabajo            INT           NOT NULL,
  paciente_id           INT,
  texto_consentimiento  VARCHAR(2000),
  aceptado              BOOLEAN       NOT NULL DEFAULT FALSE,
  fecha_aceptacion      DATETIME,
  supervisor_responsable VARCHAR(150),  -- profesional matriculado a cargo
  PRIMARY KEY (id_consentimiento),
  FOREIGN KEY (id_trabajo)  REFERENCES trabajo(id_trabajo),
  FOREIGN KEY (paciente_id) REFERENCES pacientes(paciente_id)
);


-- ---------------------------------------------------------------------
--  BLOQUE 11 — Reputacion (resenas bidireccionales)
--  Un trabajo puede generar dos resenas: cliente->estudiante y
--  estudiante->cliente. Una resena por autor por trabajo.
-- ---------------------------------------------------------------------

CREATE TABLE resena (
  id_resena           INT           NOT NULL AUTO_INCREMENT,
  id_trabajo          INT           NOT NULL,
  autor_usuario_id    INT           NOT NULL,
  receptor_usuario_id INT           NOT NULL,
  puntaje             INT           NOT NULL,   -- 1 a 5
  comentario          VARCHAR(1000),
  fecha               DATETIME      NOT NULL,
  PRIMARY KEY (id_resena),
  FOREIGN KEY (id_trabajo)          REFERENCES trabajo(id_trabajo),
  FOREIGN KEY (autor_usuario_id)    REFERENCES usuario(usuario_id),
  FOREIGN KEY (receptor_usuario_id) REFERENCES usuario(usuario_id),
  UNIQUE (id_trabajo, autor_usuario_id)
);
