using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data.SqlClient;

namespace Indexador_GCBA_.Datos
{
    class ConexionAccess
    {
        //public static OleDbConnection ObtenerConexion()
        //{
        //    //TEST
        //    //OleDbConnection Conexion = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\Pablo Ceresola\Desktop\TEST_DIGITALIZACION.mdb;Persist Security Info=False");

        //    //OleDbConnection Conexion = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\20330200570\Desktop\TEST_DIGITALIZACION.mdb;Persist Security Info=False");

        //    // PRODUCCION
        //   // OleDbConnection Conexion = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source= \\srv01digi-prd.seclyt.gcba.gob.ar\Indexacion\PRD INDEX\DIGITALIZACION.mdb;Persist Security Info=False");

        //    return Conexion;
        //}
        public static OleDbConnection ObtenerConexion1()
        {
            //TEST
            // OleDbConnection Conexion1 = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\Pablo Ceresola\Desktop\Fichas\BDINPUT.accdb;Persist Security Info=False");

            //OleDbConnection Conexion1 = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\20330200570\Desktop\BDINPUT.accdb;Persist Security Info=False");
            // PRODUCCION


            OleDbConnection Conexion1 = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=J:\Ejecutables\Indexador RCsql\BDINPUT.accdb;Persist Security Info=False");
            return Conexion1;
        }

        // Futuro SQL
        public static SqlConnection ObtenerConexion2()
        {
            //TEST
            //OleDbConnection Conexion2 = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\Users\20330200570\Desktop\RC.accdb;Persist Security Info=False");

            //// PRODUCCION
            //OleDbConnection Conexion2 = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source= \\10.67.224.181\RC\RC.accdb;Persist Security Info=False");



         //   SqlConnection Conexion2 = new SqlConnection(@"Data Source=10.80.8.80;Initial Catalog=abbyy_export;User ID=ConsultaIndexacion;Password=Consulta..I"); //BASE DE DATOS GONZA
            
            SqlConnection Conexion2 = new SqlConnection(@"Data Source=10.22.0.173;Initial Catalog=abbyy_prod;User ID=udbmartinez;Password=Tobias2024-");

            return Conexion2;
        }

    }
}
