using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indexador_GCBA_.Datos
{
    class ConexionAccess
    {

        public static OleDbConnection ObtenerConexion1()
        {
            // ==========================================================
            // BASE ÚNICA INDEXADOR CUD - COPIDIS
            // ==========================================================
            string rutaBase = @"J:\COPIDIS\00_BD\BDCertificados.accdb";

            if (!File.Exists(rutaBase))
                throw new FileNotFoundException("No se encontró la base de datos Access.", rutaBase);

            string cadenaConexion =
                @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                @"Data Source=" + rutaBase + ";" +
                @"Persist Security Info=False;";

            OleDbConnection conexion1 = new OleDbConnection(cadenaConexion);

            return conexion1;
        }


    }
}
