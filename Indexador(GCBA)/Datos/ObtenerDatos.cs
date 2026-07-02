using IndexadorCopidis.Entidades;
using System;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;

namespace IndexadorCopidis.Datos
{
    public class ObtenerDatos
    {
        // ==========================================================
        // HELPERS
        // ==========================================================

        private static object ValorTexto(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return DBNull.Value;

            return valor.Trim();
        }

        private static void LogError(string metodo, Exception ex)
        {
            Debug.WriteLine("[ObtenerDatos][ERROR] " + metodo);
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
                // No cortar si falla el log.
            }
        }

        private DataTable EjecutarConsultaDataTable(string sql, params OleDbParameter[] parametros)
        {
            DataTable dt = new DataTable();

            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                oCom.CommandText = sql;
                oCom.CommandType = CommandType.Text;

                if (parametros != null && parametros.Length > 0)
                    oCom.Parameters.AddRange(parametros);

                using (OleDbDataAdapter da = new OleDbDataAdapter(oCom))
                {
                    oCon.Open();
                    da.Fill(dt);
                }
            }

            return dt;
        }

        // ==========================================================
        // CUD - CONSULTAS PRINCIPALES
        // ==========================================================

        public DataTable ObtenerCudPorId(int idCud)
        {
            try
            {
                string sql = @"
SELECT *
FROM [TD_CUD]
WHERE [Id] = ?";

                OleDbParameter p1 = new OleDbParameter("?", OleDbType.Integer);
                p1.Value = idCud;

                return EjecutarConsultaDataTable(sql, p1);
            }
            catch (Exception ex)
            {
                LogError("ObtenerCudPorId", ex);
                throw;
            }
        }

        public DataTable ObtenerCudPorRutaSalida(string rutaSalida)
        {
            try
            {
                string sql = @"
SELECT TOP 1 *
FROM [TD_CUD]
WHERE [RutaSalida] = ?
ORDER BY [Id] DESC";

                OleDbParameter p1 = new OleDbParameter("?", OleDbType.VarWChar);
                p1.Value = ValorTexto(rutaSalida);

                return EjecutarConsultaDataTable(sql, p1);
            }
            catch (Exception ex)
            {
                LogError("ObtenerCudPorRutaSalida", ex);
                throw;
            }
        }

        public DataTable ObtenerCudPorNumeroCertificado(string numeroCertificado)
        {
            try
            {
                string sql = @"
SELECT TOP 1 *
FROM [TD_CUD]
WHERE [NumeroCertificado] = ?
ORDER BY [Id] DESC";

                OleDbParameter p1 = new OleDbParameter("?", OleDbType.VarWChar);
                p1.Value = ValorTexto(numeroCertificado);

                return EjecutarConsultaDataTable(sql, p1);
            }
            catch (Exception ex)
            {
                LogError("ObtenerCudPorNumeroCertificado", ex);
                throw;
            }
        }

        public int ConsultaCudVerificadoPorRutaEntrada(string rutaEntrada)
        {
            const int maxIntentos = 5;
            int esperaMs = 300;

            for (int intento = 1; intento <= maxIntentos; intento++)
            {
                try
                {
                    using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
                    using (OleDbCommand oCom = oCon.CreateCommand())
                    {
                        oCom.CommandText = @"
SELECT TOP 1 1
FROM [TD_CUD]
WHERE [RutaEntrada] = ?
  AND [Estado] IN ('INDEXADO', 'MODIFICADO', 'VERIFICADO')";

                        OleDbParameter pRuta = oCom.Parameters.Add("?", OleDbType.VarWChar);
                        pRuta.Value = ValorTexto(rutaEntrada);
                        pRuta.Size = 500;

                        oCom.CommandTimeout = 10;

                        oCon.Open();

                        object resultado = oCom.ExecuteScalar();

                        return resultado == null || resultado == DBNull.Value ? 0 : 1;
                    }
                }
                catch (OleDbException ex)
                {
                    bool baseEnUso =
                        ex.Message.IndexOf("archivo ya está en uso", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        ex.Message.IndexOf("file already in use", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        ex.Message.IndexOf("could not use", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!baseEnUso || intento == maxIntentos)
                    {
                        LogError("ConsultaCudVerificadoPorRutaEntrada", ex);
                        throw;
                    }

                    Debug.WriteLine(
                        "[BDCertificados][LOCK] Base en uso. Reintento " +
                        intento + "/" + maxIntentos +
                        " en " + esperaMs + " ms.");

                    System.Threading.Thread.Sleep(esperaMs);

                    esperaMs += 300;
                }
                catch (Exception ex)
                {
                    LogError("ConsultaCudVerificadoPorRutaEntrada", ex);
                    throw;
                }
            }

            return 0;
        }

        public int ConsultaCudVerificadoPorArchivoEntrada(string caja, string archivoEntrada)
        {
            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"
SELECT TOP 1 [Id]
FROM [TD_CUD]
WHERE [Caja] = ?
  AND [ArchivoEntrada] = ?
  AND ([Estado] = 'INDEXADO' OR [Estado] = 'MODIFICADO' OR [Estado] = 'VERIFICADO')";

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(caja);
                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(archivoEntrada);

                    oCon.Open();

                    object resultado = oCom.ExecuteScalar();

                    return resultado == null || resultado == DBNull.Value ? 0 : 1;
                }
                catch (Exception ex)
                {
                    LogError("ConsultaCudVerificadoPorArchivoEntrada", ex);
                    throw;
                }
            }
        }

        // ==========================================================
        // LOTES
        // Este método reemplaza el anterior, pero parametrizado.
        // ==========================================================

        public string Consultaloteasignado(string pruta)
        {
            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"
SELECT TOP 1 [DSUSUARIO]
FROM [TD_LOTE]
WHERE [DSRUTALOTE] = ?
ORDER BY [fecha] DESC";

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = ValorTexto(pruta);

                    oCon.Open();

                    object usuario = oCom.ExecuteScalar();

                    return usuario == null || usuario == DBNull.Value
                        ? string.Empty
                        : usuario.ToString();
                }
                catch (Exception ex)
                {
                    LogError("Consultaloteasignado", ex);
                    throw;
                }
            }
        }

        public PreprocesamientoCudDto ObtenerPreprocesamientoPorRutaSalida(string rutaSalida)
        {
            if (string.IsNullOrWhiteSpace(rutaSalida))
                return null;

            using (OleDbConnection oCon = ConexionAccess.ObtenerConexionPreprocesamiento())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"
SELECT TOP 1
    [Caja],
    [ArchivoEntrada],
    [RutaEntrada],
    [RutaSalida],
    [NumeroCertificado],
    [ApellidoNombre],
    [DNI],
    [CUIL],
    [FechaNacimiento],
    [FuncionesCorporales],
    [EstructurasCorporales],
    [ActividadParticipacion],
    [FactoresAmbientales],
    [OrientacionPrestacional],
    [FechaEmision],
    [FechaVencimiento],
    [Acompaniante],
    [ScoreGlobal],
    [EstadoOCR],
    [RequiereQA],
    [Observaciones]
FROM [Certificados_OCR]
WHERE [RutaSalida] = ?
ORDER BY [FechaProceso] DESC";

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = rutaSalida.Trim();

                    oCon.Open();

                    using (OleDbDataReader dr = oCom.ExecuteReader())
                    {
                        if (!dr.Read())
                            return null;

                        return new PreprocesamientoCudDto
                        {
                            Caja = ValorString(dr["Caja"]),
                            ArchivoEntrada = ValorString(dr["ArchivoEntrada"]),
                            RutaEntrada = ValorString(dr["RutaEntrada"]),
                            RutaSalida = ValorString(dr["RutaSalida"]),

                            NumeroCertificado = ValorString(dr["NumeroCertificado"]),
                            ApellidoNombre = ValorString(dr["ApellidoNombre"]),
                            DNI = ValorString(dr["DNI"]),
                            CUIL = ValorString(dr["CUIL"]),

                            FechaNacimiento = ValorFechaNullable(dr["FechaNacimiento"]),
                            FechaEmision = ValorFechaNullable(dr["FechaEmision"]),
                            FechaVencimiento = ValorFechaNullable(dr["FechaVencimiento"]),

                            FuncionesCorporales = ValorString(dr["FuncionesCorporales"]),
                            EstructurasCorporales = ValorString(dr["EstructurasCorporales"]),
                            ActividadParticipacion = ValorString(dr["ActividadParticipacion"]),
                            FactoresAmbientales = ValorString(dr["FactoresAmbientales"]),

                            OrientacionPrestacional = ValorString(dr["OrientacionPrestacional"]),
                            Acompaniante = ValorString(dr["Acompaniante"]),

                            ScoreGlobal = ValorDouble(dr["ScoreGlobal"]),
                            EstadoOCR = ValorString(dr["EstadoOCR"]),
                            RequiereQA = ValorBool(dr["RequiereQA"]),
                            Observaciones = ValorString(dr["Observaciones"])
                        };
                    }
                }
                catch (Exception ex)
                {
                    LogError("ObtenerPreprocesamientoPorRutaSalida", ex);
                    throw;
                }
            }
        }


        public int ContarCudIndexadosHoy(string indexador)
        {
            if (string.IsNullOrWhiteSpace(indexador))
                return 0;

            DateTime desde = DateTime.Today;
            DateTime hasta = desde.AddDays(1);

            using (OleDbConnection oCon = ConexionAccess.ObtenerConexion1())
            using (OleDbCommand oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"
SELECT COUNT(*)
FROM [TD_CUD]
WHERE Trim([Indexador]) = ?
  AND UCase(Trim([Estado])) = 'INDEXADO'
  AND [Fecha Indexado] >= ?
  AND [Fecha Indexado] < ?";

                    oCom.Parameters.Add("?", OleDbType.VarWChar).Value = indexador.Trim();
                    oCom.Parameters.Add("?", OleDbType.Date).Value = desde;
                    oCom.Parameters.Add("?", OleDbType.Date).Value = hasta;

                    oCon.Open();

                    object resultado = oCom.ExecuteScalar();

                    return resultado == null || resultado == DBNull.Value
                        ? 0
                        : Convert.ToInt32(resultado);
                }
                catch (Exception ex)
                {
                    LogError("ContarCudIndexadosHoy", ex);
                    throw;
                }
            }
        }


        private static string ValorString(object valor)
        {
            if (valor == null || valor == DBNull.Value)
                return string.Empty;

            return valor.ToString().Trim();
        }

        private static DateTime? ValorFechaNullable(object valor)
        {
            if (valor == null || valor == DBNull.Value)
                return null;

            if (DateTime.TryParse(valor.ToString(), out DateTime fecha))
                return fecha;

            return null;
        }

        private static double ValorDouble(object valor)
        {
            if (valor == null || valor == DBNull.Value)
                return 0;

            if (double.TryParse(valor.ToString(), out double numero))
                return numero;

            return 0;
        }

        private static bool ValorBool(object valor)
        {
            if (valor == null || valor == DBNull.Value)
                return false;

            if (valor is bool b)
                return b;

            if (bool.TryParse(valor.ToString(), out bool resultado))
                return resultado;

            return false;
        }




    }
}