using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;
using Indexador_GCBA_.Negocio;
using Indexador_GCBA_.Entidades;
using static System.Collections.Specialized.BitVector32;
using System.IO;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Indexador_GCBA_.Datos
{
    public class ProduccionAccess
    {

        public void Verificadas(string pnombre, string ptipo, string ptomo, string pacta, string pseccion, string paño, string pnumficha, string pnacimiento, string pficha, string pruta, string pimagen, string plote, string pIndexador)
        {
            using (var oCon = ConexionAccess.ObtenerConexion1())
            using (var oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"UPDATE TD_FICHASTOPOGRAFICAS 
                                 SET Nombre = ?, TipoFicha = ?, Tomo = ?, Acta = ?, Seccion = ?, Anio = ?, NumFicha = ?, Nacimiento = ?, nFicha = ?, estado = 'VERIFICADA', 
                                     imagen = ?, lote = ?, verificador = ?, fecha = ? 
                                 WHERE ruta = ?";

                    oCom.Parameters.AddWithValue("@Nombre", pnombre);
                    oCom.Parameters.AddWithValue("@TipoFicha", ptipo);
                    oCom.Parameters.AddWithValue("@Tomo", ptomo);
                    oCom.Parameters.AddWithValue("@Acta", pacta);
                    oCom.Parameters.AddWithValue("@Seccion", pseccion);
                    oCom.Parameters.AddWithValue("@Anio", paño);
                    oCom.Parameters.AddWithValue("@NumFicha", pnumficha);
                    oCom.Parameters.AddWithValue("@Nacimiento", pnacimiento);
                    oCom.Parameters.AddWithValue("@nFicha", pficha);
                    oCom.Parameters.AddWithValue("@imagen", pimagen);
                    oCom.Parameters.AddWithValue("@lote", plote);
                    oCom.Parameters.AddWithValue("@verificador", pIndexador);
                    oCom.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                    oCom.Parameters.AddWithValue("@ruta", pruta);

                    oCon.Open();
                    oCom.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error en Verificadas: " + ex.Message);
                }
            }
        }


        public void VerificadasInsert(string pnombre, string ptipo, string ptomo, string pacta, string pseccion, string paño, string pnumficha, string pnacimiento, string pficha, string pruta, string pimagen, string plote, string pIndexador)
        {
            using (var oCon = ConexionAccess.ObtenerConexion1())
            using (var oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"INSERT INTO TD_FICHASTOPOGRAFICAS 
                                 (Nombre, TipoFicha, Tomo, Acta, Seccion, Anio, NumFicha, Nacimiento, nFicha, estado, imagen, lote, verificador, ruta, fecha) 
                                 VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, 'VERIFICADA', ?, ?, ?, ?, ?)";

                    oCom.Parameters.AddWithValue("@Nombre", pnombre);
                    oCom.Parameters.AddWithValue("@TipoFicha", ptipo);
                    oCom.Parameters.AddWithValue("@Tomo", ptomo);
                    oCom.Parameters.AddWithValue("@Acta", pacta);
                    oCom.Parameters.AddWithValue("@Seccion", pseccion);
                    oCom.Parameters.AddWithValue("@Anio", paño);
                    oCom.Parameters.AddWithValue("@NumFicha", pnumficha);
                    oCom.Parameters.AddWithValue("@Nacimiento", pnacimiento);
                    oCom.Parameters.AddWithValue("@nFicha", pficha);
                    oCom.Parameters.AddWithValue("@imagen", pimagen);
                    oCom.Parameters.AddWithValue("@lote", plote);
                    oCom.Parameters.AddWithValue("@verificador", pIndexador);
                    oCom.Parameters.AddWithValue("@ruta", pruta);
                    oCom.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                    oCon.Open();
                    oCom.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error en VerificadasInsert: " + ex.Message);
                }
            }
        }

        public void FichasVerificadasRC(
     string pnombre, string ptipo, string ptomo, string pacta, string pseccion,
     string paño, string pnumficha, string pnacimiento, string pficha, string pimagen)
        {
            using (var oCon = ConexionAccess.ObtenerConexion2())
            using (var oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"INSERT INTO FICHA_TOPOGRAFICA 
                                 (Nombre, Nacimiento, Tomo, Acta, Seccion, Anio, nFicha, page_images) 
                                 VALUES (@Nombre, @Nacimiento, @Tomo, @Acta, @Seccion, @Anio, @nFicha, @page_images )";

                    oCom.Parameters.AddWithValue("@Nombre", pnombre);
                    oCom.Parameters.AddWithValue("@Nacimiento", pnacimiento);
                    oCom.Parameters.AddWithValue("@Tomo", ptomo);
                    oCom.Parameters.AddWithValue("@Acta", pacta);
                    oCom.Parameters.AddWithValue("@Seccion", pseccion);
                    oCom.Parameters.AddWithValue("@Anio", paño);
                    oCom.Parameters.AddWithValue("@nFicha", pficha);
                    oCom.Parameters.AddWithValue("@page_images", pimagen);
              

                    oCon.Open();
                    oCom.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error en FichasVerificadasRC: " + ex.Message);
                }
            }
        }

        public void ActasVerificadasRC(
    string Circunscripcion_Acta, string Tomo_Acta, int Dia_inscripcion, string Mes_Inscripcion, int Anio_Acta,
    int Numero_Acta, int Año_Inscripcion, string Nombre_Nacido, string CNI_Numero, string Sexo_Nacido,
    int Dia_Nacimiento, string Mes_Nacimiento, int Año_Nacimiento, string Nombre_Padre,
    string Nombre_Madre, string pimagen)
        {
            using (var oCon = ConexionAccess.ObtenerConexion2())
            using (var oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"INSERT INTO Acta_de_Nacimiento 
                                 (Cicrunscripcion_Acta, Tomo_Acta, Dia_inscripcion, Mes_Inscripcion, Anio_Acta, Numero_Acta, 
                                  Anio_Inscripcion, Nombre_Nacido, CNI_Numero, Sexo_Nacido, Dia_Nacimiento, Mes_Nacimiento, 
                                  Anio_Nacimiento, Nombre_Padre, Nombre_Madre, PAGE_IMAGES) 
                                 VALUES (@Circunscripcion_Acta, @Tomo_Acta, @Dia_inscripcion, @Mes_Inscripcion, @Anio_Acta, 
                                         @Numero_Acta, @Año_Inscripcion, @Nombre_Nacido, @CNI_Numero, @Sexo_Nacido, 
                                         @Dia_Nacimiento, @Mes_Nacimiento, @Año_Nacimiento, @Nombre_Padre, @Nombre_Madre,
                                         @pimagen)";

                    oCom.Parameters.AddWithValue("@Circunscripcion_Acta", Circunscripcion_Acta);
                    oCom.Parameters.AddWithValue("@Tomo_Acta", Tomo_Acta);
                    oCom.Parameters.AddWithValue("@Dia_inscripcion", Dia_inscripcion);
                    oCom.Parameters.AddWithValue("@Mes_Inscripcion", Mes_Inscripcion);
                    oCom.Parameters.AddWithValue("@Anio_Acta", Anio_Acta);
                    oCom.Parameters.AddWithValue("@Numero_Acta", Numero_Acta);
                    oCom.Parameters.AddWithValue("@Año_Inscripcion", Año_Inscripcion);
                    oCom.Parameters.AddWithValue("@Nombre_Nacido", Nombre_Nacido);
                    oCom.Parameters.AddWithValue("@CNI_Numero", CNI_Numero);
                    oCom.Parameters.AddWithValue("@Sexo_Nacido", Sexo_Nacido);
                    oCom.Parameters.AddWithValue("@Dia_Nacimiento", Dia_Nacimiento);
                    oCom.Parameters.AddWithValue("@Mes_Nacimiento", Mes_Nacimiento);
                    oCom.Parameters.AddWithValue("@Año_Nacimiento", Año_Nacimiento);
                    oCom.Parameters.AddWithValue("@Nombre_Padre", Nombre_Padre);
                    oCom.Parameters.AddWithValue("@Nombre_Madre", Nombre_Madre);
                    oCom.Parameters.AddWithValue("@pimagen", pimagen);

                    oCon.Open();
                    oCom.ExecuteNonQuery();
                }
                catch (OleDbException ex)
                {
                    MessageBox.Show("Error de base de datos: " + ex.Message + "\nPor favor, asegúrese de estar conectado a FortiCLIENT.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.AppendAllText("error_log.txt", DateTime.Now + " - Error de base de datos en ActasVerificadasRC: " + ex.ToString() + Environment.NewLine);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show("Error de operación inválida: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.AppendAllText("error_log.txt", DateTime.Now + " - Error de operación inválida en ActasVerificadasRC: " + ex.ToString() + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error desconocido: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.AppendAllText("error_log.txt", DateTime.Now + " - Error desconocido en ActasVerificadasRC: " + ex.ToString() + Environment.NewLine);
                }
            }
        }

        public void EditarActasVerificadasRC(
            string Circunscripcion_Acta, string Tomo_Acta, int Dia_inscripcion, string Mes_Inscripcion, int Anio_Acta,
            int Numero_Acta, int Año_Inscripcion, string Nombre_Nacido, string CNI_Numero, string Sexo_Nacido,
            int Dia_Nacimiento, string Mes_Nacimiento, int Año_Nacimiento, string Nombre_Padre,
            string Nombre_Madre, string pimagen)
        {
            using (var oCon = ConexionAccess.ObtenerConexion2())
            using (var oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"UPDATE Acta_de_Nacimiento 
                                 SET Cicrunscripcion_Acta = ?, Tomo_Acta = ?, Numero_Acta = ?, Anio_Acta = ?, 
                                     Dia_inscripcion = ?, Mes_Inscripcion = ?, Anio_Inscripcion = ?, 
                                     Nombre_Nacido = ?, CNI_Numero = ?, Sexo_Nacido = ?, 
                                     Dia_Nacimiento = ?, Mes_Nacimiento = ?, Anio_Nacimiento = ?, 
                                     Nombre_Padre = ?, Nombre_Madre = ?
                                 WHERE PAGE_IMAGES = ?";

                    oCom.Parameters.AddWithValue("@Circunscripcion_Acta", Circunscripcion_Acta);
                    oCom.Parameters.AddWithValue("@Tomo_Acta", Tomo_Acta);
                    oCom.Parameters.AddWithValue("@Numero_Acta", Numero_Acta);
                    oCom.Parameters.AddWithValue("@Anio_Acta", Anio_Acta);
                    oCom.Parameters.AddWithValue("@Dia_inscripcion", Dia_inscripcion);
                    oCom.Parameters.AddWithValue("@Mes_Inscripcion", Mes_Inscripcion);
                    oCom.Parameters.AddWithValue("@Anio_Inscripcion", Año_Inscripcion);
                    oCom.Parameters.AddWithValue("@Nombre_Nacido", Nombre_Nacido);
                    oCom.Parameters.AddWithValue("@CNI_Numero", CNI_Numero);
                    oCom.Parameters.AddWithValue("@Sexo_Nacido", Sexo_Nacido);
                    oCom.Parameters.AddWithValue("@Dia_Nacimiento", Dia_Nacimiento);
                    oCom.Parameters.AddWithValue("@Mes_Nacimiento", Mes_Nacimiento);
                    oCom.Parameters.AddWithValue("@Anio_Nacimiento", Año_Nacimiento);
                    oCom.Parameters.AddWithValue("@Nombre_Padre", Nombre_Padre);
                    oCom.Parameters.AddWithValue("@Nombre_Madre", Nombre_Madre);
                    oCom.Parameters.AddWithValue("@pimagen", pimagen);

                    oCon.Open();
                    oCom.ExecuteNonQuery();
                }
                catch (OleDbException ex)
                {
                    MessageBox.Show("Error de base de datos: " + ex.Message + "\nPor favor, asegúrese de estar conectado a FortiCLIENT.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.AppendAllText("error_log.txt", DateTime.Now + " - Error de base de datos en EditarActasVerificadasRC: " + ex.ToString() + Environment.NewLine);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show("Error de operación inválida: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.AppendAllText("error_log.txt", DateTime.Now + " - Error de operación inválida en EditarActasVerificadasRC: " + ex.ToString() + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error desconocido: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.AppendAllText("error_log.txt", DateTime.Now + " - Error desconocido en EditarActasVerificadasRC: " + ex.ToString() + Environment.NewLine);
                }
            }
        }




        public void ActasNacimiento(
            string Circunscripcion_Acta, string Tomo_Acta, int Numero_Acta, int Anio_Acta, int Dia_inscripcion, int Mes_Inscripcion, int Anio_Inscripcion,
            string Nombre_Nacido, string CNI_Numero, string Sexo_Nacido, int Dia_Nacimiento, string Mes_Nacimiento, int Anio_Nacimiento,
            string Nombre_Padre, string Nombre_Madre, string ruta, string pimagen, string plote, string pIndexador)
        {
            using (var oCon = ConexionAccess.ObtenerConexion1())
            using (var oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = @"INSERT INTO TD_ACTAS 
                         (Circunscripcion_Acta, Tomo_Acta, Numero_Acta, Anio_Acta, Dia_inscripcion, Mes_Inscripcion, Año_Inscripcion,
                          Nombre_Nacido, CNI_Numero, Sexo_Nacido, Dia_Nacimiento, Mes_Nacimiento, Año_Nacimiento,
                          Nombre_Padre, Nombre_Madre, ruta, estado, imagen, lote, verificador, fecha) 
                         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'VERIFICADA', ?, ?, ?, ?)";

                    oCom.Parameters.AddWithValue("?", Circunscripcion_Acta); // Texto
                    oCom.Parameters.AddWithValue("?", Tomo_Acta); // Texto
                    oCom.Parameters.AddWithValue("?", Numero_Acta); // Número
                    oCom.Parameters.AddWithValue("?", Anio_Acta); // Número
                    oCom.Parameters.AddWithValue("?", Dia_inscripcion); // Número
                    oCom.Parameters.AddWithValue("?", Mes_Inscripcion); // Número
                    oCom.Parameters.AddWithValue("?", Anio_Inscripcion); // Número
                    oCom.Parameters.AddWithValue("?", Nombre_Nacido); // Texto
                    oCom.Parameters.AddWithValue("?", CNI_Numero); // Texto
                    oCom.Parameters.AddWithValue("?", Sexo_Nacido); // Texto
                    oCom.Parameters.AddWithValue("?", Dia_Nacimiento); // Número
                    oCom.Parameters.AddWithValue("?", Mes_Nacimiento); // Texto
                    oCom.Parameters.AddWithValue("?", Anio_Nacimiento); // Número
                    oCom.Parameters.AddWithValue("?", Nombre_Padre); // Texto
                    oCom.Parameters.AddWithValue("?", Nombre_Madre); // Texto
                    oCom.Parameters.AddWithValue("?", ruta); // Texto
                    oCom.Parameters.AddWithValue("?", pimagen); // Texto
                    oCom.Parameters.AddWithValue("?", plote); // Texto
                    oCom.Parameters.AddWithValue("?", pIndexador); // Texto
                    oCom.Parameters.AddWithValue("?", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                    oCon.Open();
                    oCom.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error en ActasNacimiento: " + ex.Message);
                }
            }
        }




        public List<Entidades.Indexador> CargarListaIndexadores()
        {
            var oListaIndexadores = new List<Entidades.Indexador>();

            using (var oCon = ConexionAccess.ObtenerConexion1())
            using (var oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = "SELECT IDINDEXADOR, DSNOMBRE FROM TD_INDEXADORES";
                    Console.WriteLine("Abriendo conexión...");
                    oCon.Open();
                    Console.WriteLine("Conexión abierta.");

                    using (var oLeer = oCom.ExecuteReader())
                    {
                        while (oLeer.Read())
                        {
                            oListaIndexadores.Add(new Entidades.Indexador
                            {
                                IdIndexador = oLeer.GetInt32(0),
                                NombreIndexador = oLeer.GetString(1)
                            });
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    MessageBox.Show("Error de base de datos: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.AppendAllText("error_log.txt", DateTime.Now + " - Error de base de datos en CargarListaIndexadores: " + ex.ToString() + Environment.NewLine);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show("Error de operación inválida: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.AppendAllText("error_log.txt", DateTime.Now + " - Error de operación inválida en CargarListaIndexadores: " + ex.ToString() + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error desconocido: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.AppendAllText("error_log.txt", DateTime.Now + " - Error desconocido en CargarListaIndexadores: " + ex.ToString() + Environment.NewLine);
                }
            }

            if (oListaIndexadores.Count == 0)
            {
                MessageBox.Show("No se encontraron indexadores.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return oListaIndexadores;
        }



        public void loteasignado(string pruta, string pusuario)
        {
            OleDbConnection oCon = ConexionAccess.ObtenerConexion1();
            OleDbCommand oCom = oCon.CreateCommand();

            try
            {
                oCom.CommandText = $"INSERT INTO TD_LOTE (DSRUTALOTE, DSUSUARIO, fecha) VALUES(@ruta, @usuario, @fecha)";
                oCom.Parameters.AddWithValue("@ruta", pruta);
                oCom.Parameters.AddWithValue("@usuario", pusuario);
                oCom.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                oCon.Open();
                oCom.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                oCon.Close();
            }
        }

        public void CerrarLoteAsignado(string pruta, string pusuario)
        {
            using (var oCon = ConexionAccess.ObtenerConexion1())
            using (var oCom = oCon.CreateCommand())
            {
                try
                {
                    oCom.CommandText = "UPDATE TD_LOTE SET DSUSUARIO = ? WHERE DSRUTALOTE = ?";
                    oCom.Parameters.AddWithValue("@usuario", pusuario + " (CERRADO)");
                    oCom.Parameters.AddWithValue("@ruta", pruta);

                    oCon.Open();
                    oCom.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error en CerrarLoteAsignado: " + ex.Message);
                }
            }
        }






    }
}
