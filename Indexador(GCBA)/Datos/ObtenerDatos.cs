using System;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;

namespace Indexador_GCBA_.Datos
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

     
  
    }
}