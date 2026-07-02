using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using IndexadorCopidis.Entidades;

namespace IndexadorCopidis.Datos
{
    public class ProduccionAccess
    {
        // ==========================================================
        // HELPERS GENERALES ACCESS / OLEDB
        // ==========================================================

        private static object ValorTexto(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return DBNull.Value;

            return valor.Trim();
        }

        private static object ValorFechaDesdeTexto(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return DBNull.Value;

            DateTime fecha;

            if (DateTime.TryParseExact(
                valor.Trim(),
                "dd/MM/yyyy",
                CultureInfo.GetCultureInfo("es-AR"),
                DateTimeStyles.None,
                out fecha))
            {
                return fecha;
            }

            return DBNull.Value;
        }

        private static object ValorFechaHoraActual()
        {
            return DateTime.Now;
        }

        private static void LogError(string metodo, Exception ex)
        {
            Debug.WriteLine("[ProduccionAccess][ERROR] " + metodo);
            Debug.WriteLine(ex.ToString());

            try
            {
                File.AppendAllText(
                    "error_log.txt",
                    DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") +
                    " - Error en " + metodo + ": " +
                    ex.ToString() + Environment.NewLine);
            }
            catch
            {
                // No cortar el flujo si falla el log.
            }
        }

        // ==========================================================
        // CUD - DUPLICADOS
        // ==========================================================

        public int BuscarIdCudExistente(
            string numeroCertificado,
            string dni,
            string rutaEntrada,
            string caja,
            string archivoEntrada)
        {
            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"
SELECT TOP 1 [Id]
FROM [TD_CUD]
WHERE [NumeroCertificado] = ?
   OR ([DNI] = ? AND [NumeroCertificado] = ?)
   OR [RutaEntrada] = ?
   OR ([Caja] = ? AND [ArchivoEntrada] = ?)
ORDER BY [Id] DESC";

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(numeroCertificado);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(dni);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(numeroCertificado);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(rutaEntrada);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(caja);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(archivoEntrada);

                    oCon.Open();

                    object resultado = oCom.ExecuteScalar();

                    if (resultado == null || resultado == DBNull.Value)
                        return 0;

                    return Convert.ToInt32(resultado);
                }
                catch (Exception ex)
                {
                    LogError("BuscarIdCudExistente", ex);
                    throw;
                }
            }
        }

        public void RegistrarIndexacionDiaria(string nombreIndexador)
        {
            if (string.IsNullOrWhiteSpace(nombreIndexador))
                return;

            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCon.Open();

                    // Primero buscamos la fecha última y el contador actual.
                    oCom.CommandText = @"
SELECT TOP 1 [FECHAULTIMAINDEX], [INDEXACIONDIARIA]
FROM [TD_INDEXADORES]
WHERE [DSNOMBRE] = ?";

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = nombreIndexador.Trim();

                    DateTime? fechaUltima = null;
                    int cantidadActual = 0;

                    using (OleDbDataReader dr = oCom.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            if (dr["FECHAULTIMAINDEX"] != DBNull.Value)
                                fechaUltima = Convert.ToDateTime(dr["FECHAULTIMAINDEX"]);

                            if (dr["INDEXACIONDIARIA"] != DBNull.Value)
                                cantidadActual = Convert.ToInt32(dr["INDEXACIONDIARIA"]);
                        }
                        else
                        {
                            return;
                        }
                    }

                    int nuevaCantidad;

                    if (fechaUltima.HasValue && fechaUltima.Value.Date == DateTime.Today)
                        nuevaCantidad = cantidadActual + 1;
                    else
                        nuevaCantidad = 1;

                    using (OleDbCommand cmdUpdate = oCon.CreateCommand())
                    {
                        cmdUpdate.CommandText = @"
UPDATE [TD_INDEXADORES]
SET [FECHAULTIMAINDEX] = ?,
    [INDEXACIONDIARIA] = ?
WHERE [DSNOMBRE] = ?";

                        cmdUpdate.Parameters.Add("?", OleDbType.Date).Value = DateTime.Now;
                        cmdUpdate.Parameters.Add("?", OleDbType.Integer).Value = nuevaCantidad;
                        cmdUpdate.Parameters.Add("?", OleDbType.VarWChar).Value = nombreIndexador.Trim();

                        cmdUpdate.ExecuteNonQuery();
                    }

                    Debug.WriteLine("[INDEXADORES] Indexación diaria actualizada para: " + nombreIndexador);
                    Debug.WriteLine("[INDEXADORES] Nueva cantidad diaria: " + nuevaCantidad);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[INDEXADORES][ERROR] RegistrarIndexacionDiaria");
                    Debug.WriteLine(ex.ToString());

                    // No tiro excepción para no romper el guardado del CUD.
                    // Si el CUD se guardó bien, este contador es secundario.
                }
            }
        }


        public int ConsultaCudVerificadoPorRutaEntrada(string rutaEntrada)
        {
            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"
SELECT TOP 1 [Id]
FROM [TD_CUD]
WHERE [RutaEntrada] = ?
  AND ([Estado] = 'INDEXADO' OR [Estado] = 'MODIFICADO' OR [Estado] = 'VERIFICADO')";

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(rutaEntrada);

                    oCon.Open();

                    object resultado = oCom.ExecuteScalar();

                    return resultado == null || resultado == DBNull.Value ? 0 : 1;
                }
                catch (Exception ex)
                {
                    LogError("ConsultaCudVerificadoPorRutaEntrada", ex);
                    throw;
                }
            }
        }

        // ==========================================================
        // CUD - INSERT
        // ==========================================================

        public int CertificadoCud(
            string numeroCertificado,
            string apellidoNombre,
            string sexo,
            string dni,
            string cuil,
            string fechaNacimiento,
            string diagnostico,
            string funcionesCorporales,
            string estructurasCorporales,
            string actividadParticipacion,
            string factoresAmbientales,
            string orientacionPrestacional,
            string fechaEmision,
            string fechaVencimiento,
            string acompaniante,
            string rutaEntrada,
            string archivoEntrada,
            string rutaSalida,
            string caja,
            string indexador,
            string estado)
        {
            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"
INSERT INTO [TD_CUD]
(
    [Caja],
    [ArchivoEntrada],
    [RutaEntrada],
    [RutaSalida],
    [NumeroCertificado],
    [ApellidoNombre],
    [DNI],
    [CUIL],
    [Sexo],
    [FechaNacimiento],
    [Diagnostico],
    [FuncionesCorporales],
    [EstructurasCorporales],
    [ActividadParticipacion],
    [FactoresAmbientales],
    [OrientacionPrestacional],
    [FechaEmision],
    [FechaVencimiento],
    [Acompaniante],
    [Indexador],
    [Estado],
    [Fecha Indexado]
)
VALUES
(
    ?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?
)";

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(caja);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(archivoEntrada);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(rutaEntrada);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(rutaSalida);

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(numeroCertificado);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(apellidoNombre);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(dni);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(cuil);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(sexo);

                    oCom.Parameters.Add("?", OleDbType.DBDate).Value = ValorFechaDesdeTexto(fechaNacimiento);

                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(diagnostico);
                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(funcionesCorporales);
                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(estructurasCorporales);
                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(actividadParticipacion);
                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(factoresAmbientales);
                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(orientacionPrestacional);

                    oCom.Parameters.Add("?", OleDbType.DBDate).Value = ValorFechaDesdeTexto(fechaEmision);
                    oCom.Parameters.Add("?", OleDbType.DBDate).Value = ValorFechaDesdeTexto(fechaVencimiento);

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(acompaniante);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(indexador);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(estado);
                    oCom.Parameters.Add("?", OleDbType.Date).Value = ValorFechaHoraActual();
                    oCom.Parameters.Add("?", OleDbType.Date).Value = DBNull.Value;

                    Debug.WriteLine("[CUD INSERT] NumeroCertificado: " + numeroCertificado);
                    Debug.WriteLine("[CUD INSERT] DNI: " + dni);
                    Debug.WriteLine("[CUD INSERT] RutaEntrada: " + rutaEntrada);
                    Debug.WriteLine("[CUD INSERT] RutaSalida: " + rutaSalida);

                    oCon.Open();
                    oCom.ExecuteNonQuery();

                    using (OleDbCommand cmdId = oCon.CreateCommand())
                    {
                        cmdId.CommandText = "SELECT @@IDENTITY";
                        object id = cmdId.ExecuteScalar();

                        if (id == null || id == DBNull.Value)
                            return 0;

                        return Convert.ToInt32(id);
                    }
                }
                catch (Exception ex)
                {
                    LogError("CertificadoCud", ex);
                    throw;
                }
            }
        }

        // ==========================================================
        // CUD - UPDATE POR ID
        // ==========================================================

        public bool EditarCertificadoCudVerificado(
            int idCud,
            string numeroCertificado,
            string apellidoNombre,
            string sexo,
            string dni,
            string cuil,
            string fechaNacimiento,
            string diagnostico,
            string funcionesCorporales,
            string estructurasCorporales,
            string actividadParticipacion,
            string factoresAmbientales,
            string orientacionPrestacional,
            string fechaEmision,
            string fechaVencimiento,
            string acompaniante,
            string rutaSalida,
            string indexador,
            string estado)
        {
            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"
UPDATE [TD_CUD]
SET
    [RutaSalida] = ?,
    [NumeroCertificado] = ?,
    [ApellidoNombre] = ?,
    [DNI] = ?,
    [CUIL] = ?,
    [Sexo] = ?,
    [FechaNacimiento] = ?,
    [Diagnostico] = ?,
    [FuncionesCorporales] = ?,
    [EstructurasCorporales] = ?,
    [ActividadParticipacion] = ?,
    [FactoresAmbientales] = ?,
    [OrientacionPrestacional] = ?,
    [FechaEmision] = ?,
    [FechaVencimiento] = ?,
    [Acompaniante] = ?,
    [Indexador] = ?,
    [Estado] = ?,
    [Fecha Auditado] = ?
WHERE [Id] = ?";

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(rutaSalida);

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(numeroCertificado);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(apellidoNombre);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(dni);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(cuil);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(sexo);

                    oCom.Parameters.Add("?", OleDbType.DBDate).Value = ValorFechaDesdeTexto(fechaNacimiento);

                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(diagnostico);
                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(funcionesCorporales);
                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(estructurasCorporales);
                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(actividadParticipacion);
                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(factoresAmbientales);
                    oCom.Parameters.Add("?", OleDbType.LongVarWChar).Value = ValorTexto(orientacionPrestacional);

                    oCom.Parameters.Add("?", OleDbType.DBDate).Value = ValorFechaDesdeTexto(fechaEmision);
                    oCom.Parameters.Add("?", OleDbType.DBDate).Value = ValorFechaDesdeTexto(fechaVencimiento);

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(acompaniante);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(indexador);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(estado);
                    oCom.Parameters.Add("?", OleDbType.Date).Value = ValorFechaHoraActual();

                    oCom.Parameters.Add("?", OleDbType.Integer).Value = idCud;

                    Debug.WriteLine("[CUD UPDATE] Id: " + idCud);
                    Debug.WriteLine("[CUD UPDATE] NumeroCertificado: " + numeroCertificado);
                    Debug.WriteLine("[CUD UPDATE] DNI: " + dni);
                    Debug.WriteLine("[CUD UPDATE] RutaSalida: " + rutaSalida);

                    oCon.Open();

                    int filas = oCom.ExecuteNonQuery();

                    return filas > 0;
                }
                catch (Exception ex)
                {
                    LogError("EditarCertificadoCudVerificado", ex);
                    throw;
                }
            }
        }

        // ==========================================================
        // INDEXADORES
        // Se conserva porque tu proyecto ya lo usa.
        // ==========================================================

        public List<Indexador> CargarListaIndexadores()
        {
            List<Indexador> lista = new List<Indexador>();

            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"
SELECT [IDINDEXADOR], [DSNOMBRE]
FROM [TD_INDEXADORES]
ORDER BY [DSNOMBRE]";

                    oCon.Open();

                    using (OleDbDataReader dr = oCom.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Indexador item = new Indexador();

                            item.IdIndexador = dr["IDINDEXADOR"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(dr["IDINDEXADOR"]);

                            item.NombreIndexador = dr["DSNOMBRE"] == DBNull.Value
                                ? string.Empty
                                : dr["DSNOMBRE"].ToString();

                            lista.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("CargarListaIndexadores", ex);

                    MessageBox.Show(
                        "Error al cargar indexadores: " + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            return lista;
        }

        // ==========================================================
        // LOTES
        // Se conserva porque tu Form actual usa loteasignado y CerrarLoteAsignado.
        // ==========================================================

        public void loteasignado(string pruta, string pusuario)
        {
            if (string.IsNullOrWhiteSpace(pruta))
                throw new Exception("No se puede asignar un lote sin ruta.");

            if (string.IsNullOrWhiteSpace(pusuario))
                throw new Exception("No se puede asignar un lote sin usuario/indexador.");

            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"
INSERT INTO [TD_LOTE]
(
    [DSRUTALOTE],
    [DSUSUARIO],
    [fecha]
)
VALUES
(
    ?,?,?
)";

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = pruta.Trim();
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = pusuario.Trim();
                    oCom.Parameters.Add("?", OleDbType.Date).Value = DateTime.Now;

                    Debug.WriteLine("[LOTE] Asignando lote: " + pruta);
                    Debug.WriteLine("[LOTE] Usuario: " + pusuario);

                    oCon.Open();
                    oCom.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[LOTE][ERROR] loteasignado");
                    Debug.WriteLine(ex.ToString());
                    throw;
                }
            }
        }


        public void CerrarLoteAsignado(string pruta, string pusuario)
        {
            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"
UPDATE [TD_LOTE]
SET [DSUSUARIO] = ?
WHERE [DSRUTALOTE] = ?";

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(pusuario + " (CERRADO)");
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(pruta);

                    Debug.WriteLine("[LOTE] Cerrando lote: " + pruta);
                    Debug.WriteLine("[LOTE] Usuario cierre: " + pusuario);

                    oCon.Open();
                    oCom.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    LogError("CerrarLoteAsignado", ex);
                    throw;
                }
            }
        }
    }
}