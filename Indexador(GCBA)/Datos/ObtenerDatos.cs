using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO; // Manejo de Archivos
using System.Windows.Forms; // Manejo de Formularios de Windows
using Indexador_GCBA_.Negocio;
using System.Data.OleDb;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Indexador_GCBA_.Datos
{
    class ObtenerDatos
    {

       
     
        public DataTable ConsultaOCR(string pruta)
        {
            OleDbConnection conexion1 = ConexionAccess.ObtenerConexion1();
            OleDbCommand comando = conexion1.CreateCommand();
            DataTable dt = new DataTable();

            String query = "SELECT * From  TD_FICHASTOPOGRAFICAS where ruta = '" + pruta + "'";

            try
            {
                conexion1.Open();
                OleDbDataAdapter oda = new OleDbDataAdapter(query, conexion1);
                oda.Fill(dt);
                return (dt);
            }
            catch
            {
                throw;
            }
            finally
            {
                if (conexion1 != null)
                {
                    conexion1.Close();
                }
            }

        }
      
        public DataTable editaracta (string pimagen)
        {
            System.Data.SqlClient.SqlConnection conexion2 = ConexionAccess.ObtenerConexion2();
            SqlCommand comando = conexion2.CreateCommand();
            DataTable dt = new DataTable();

            String query = "SELECT * From  Acta_de_Nacimiento where PAGE_IMAGES = '" + pimagen + "'";
          
            try
            {
                conexion2.Open();
                SqlDataAdapter oda = new SqlDataAdapter(query, conexion2);
                oda.Fill(dt);
                return (dt);
            }
            catch
            {
                throw;
            }
            finally
            {
                if (conexion2 != null)
                {
                    conexion2.Close();
                }
            }

        }
        //public DataTable seleccionartodosql()
        //{
        //    System.Data.SqlClient.SqlConnection conexion2 = ConexionAccess.ObtenerConexion2();
        //    SqlCommand comando = conexion2.CreateCommand();
        //    DataTable dt = new DataTable();

        //    String query = "SELECT * From  Acta_de_Nacimiento ";

        //    try
        //    {
        //        conexion2.Open();
        //        SqlDataAdapter oda = new SqlDataAdapter(query, conexion2);
        //        oda.Fill(dt);
        //        return (dt);
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //    finally
        //    {
        //        if (conexion2 != null)
        //        {
        //            conexion2.Close();
        //        }
        //    }

        //}



        public string Consultaloteasignado(string pruta)
        {
            OleDbConnection conexion1 = ConexionAccess.ObtenerConexion1();
            OleDbCommand comando = conexion1.CreateCommand();

            try
            {
                comando.CommandText = $"SELECT DSUSUARIO FROM TD_LOTE WHERE dsrutalote = '{pruta}' ";

              

                conexion1.Open();
                comando.CommandType = CommandType.Text;

                object usuario = comando.ExecuteScalar();

                return usuario?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                Debug.Print("Error en Consultaloteasignado: " + ex.Message);
                Console.WriteLine("Error en Consultaloteasignado: " + ex.Message);
                throw;
            }
            finally
            {
                conexion1?.Close();
            }
        }

        public int Consultaactaverificada(string pruta)
        {
            OleDbConnection conexion1 = ConexionAccess.ObtenerConexion1();
            OleDbCommand comando = conexion1.CreateCommand();

            try
            {
                comando.CommandText = $"SELECT imagen FROM TD_ACTAS WHERE ruta = '{pruta}' AND ESTADO = 'VERIFICADA'";

             

                conexion1.Open();
                comando.CommandType = CommandType.Text;

                object resultado = comando.ExecuteScalar();

                return resultado != null ? 1 : 0;
            }
            catch (Exception ex)
            {
                Debug.Print("Error en Consultaactaverificada: " + ex.Message);
                Console.WriteLine("Error en Consultaactaverificada: " + ex.Message);
                throw;
            }
            finally
            {
                conexion1?.Close();
            }
        }
    
    public int Consultaficha(string pruta)
        {
            OleDbConnection conexion1 = ConexionAccess.ObtenerConexion1();
            OleDbCommand comando = conexion1.CreateCommand();


            try
            {
                comando.CommandText = string.Format("SELECT nFicha From  TD_FICHASTOPOGRAFICAS where ruta = '" + pruta + "'");

                conexion1.Open();
                comando.CommandType = CommandType.Text;



                object nficha = comando.ExecuteScalar();

                if (DBNull.Value.Equals(nficha))
                {
                    return 0;
                }
                else
                {
                    return Convert.ToInt32(nficha);
                }


            }
            catch
            {
                throw;
            }
            finally
            {
                if (conexion1 != null)
                {
                    conexion1.Close();
                }
            }

        }
        public int Consultaimagenverificada(string pruta)
        {
            OleDbConnection conexion1 = ConexionAccess.ObtenerConexion1();
            OleDbCommand comando = conexion1.CreateCommand();


            try
            {
                comando.CommandText = string.Format("SELECT imagen From  TD_FICHASTOPOGRAFICAS where ruta = '" + pruta + "' and ESTADO = 'VERIFICADA'");

                conexion1.Open();
                comando.CommandType = CommandType.Text;



                object usuario = comando.ExecuteScalar();

                if (usuario == null)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (conexion1 != null)
                {
                    conexion1.Close();
                }
            }

        }

    }
}
