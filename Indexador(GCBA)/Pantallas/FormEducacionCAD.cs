using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using static System.Net.Mime.MediaTypeNames;
using Indexador_GCBA_.Datos;

using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics.Eventing.Reader;
using System.Collections;
using System.Security.Policy;
using System.Diagnostics.Contracts;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using Indexador_GCBA_.Entidades;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Drawing.Text;

namespace Indexador_GCBA_
{
    public partial class FormEducacionCAD : Form
    {
        Entidades.Indexador oIndexador = new Entidades.Indexador();
        Graphics Lienzo;
        string RutaNombreImagen="";
        int usuariocoincide = 0;
        int Xrecta;
        int Yrecta;
        public FormEducacionCAD(Entidades.Indexador pIndexador)
        {
            InitializeComponent();
            oIndexador = pIndexador;
            WindowState = FormWindowState.Maximized;
          
            DesHabilarBotones();

        }

        private void HabilitarBotones()
        {
            btnGrabar.Enabled = true;
            btnSepararIlegible.Enabled = true;

        }

        private void DesHabilarBotones()
        {
            btnGrabar.Enabled = false;
            btnSepararIlegible.Enabled = false;

        }

        private void btnSeleccionarLote_Click(object sender, EventArgs e)
        {

            string lote = "";
            string apellido = "";
            DirectoryInfo di1 = new DirectoryInfo("J:\\BDs\\RCproc\\FICHAS\\PROCESADAS");

                foreach (var items in di1.GetDirectories())
                {
                apellido = items.Name; 
              
                 if (apellido != "")
                {
                    DirectoryInfo diap = new DirectoryInfo("J:\\BDs\\RCproc\\FICHAS\\PROCESADAS\\" + apellido);

                    foreach (var items1 in diap.GetDirectories())
                    {
                        lote = items1.Name;

                        if (lote != "")
                        {
                         string  lotea =  loteasignado(lote, apellido);
                          
                            if (lotea !="")
                            {
                            DirectoryInfo dilo = new DirectoryInfo("J:\\BDs\\RCproc\\FICHAS\\PROCESADAS\\" + apellido+"\\"+lotea);
                            txtRutaLote.Text = "J:\\BDs\\RCproc\\FICHAS\\PROCESADAS\\" + apellido + "\\" + lotea;


                          
                            ObtenerDatos oconsulta = new ObtenerDatos();
                            string consulta = oconsulta.Consultaloteasignado(txtRutaLote.Text);
                            string pindexador = txtUsuario.Text;

      
                                
                            foreach (var items2 in dilo.GetFiles())
                            {
                                if (items2.Extension.Equals(".jpg"))
                                {
                                        if (usuariocoincide==1)
                                        {
                                            string cadena = items2.Name;
                                         
                                            int cadena1 = oconsulta.Consultaimagenverificada(txtRutaLote.Text + "\\" + items2);
                                         
                                            if (cadena1 == 0)
                                            {
                                                string[] nombre = cadena.Split(new char[] { '.' });
                                                string archivo = nombre[0];
                                                lboxIndice.Items.Add(archivo);
                                            }

                                        }
                                        else
                                        {
                                            string cadena = items2.Name;
                                          
                                            string[] nombre = cadena.Split(new char[] { '.' });
                                            string archivo = nombre[0];
                                            lboxIndice.Items.Add(archivo);



                                        }
                                    

                                }


                            }
                            if (usuariocoincide == 0)
                                {
                                ProduccionAccess oinsertar = new ProduccionAccess();
                                oinsertar.loteasignado(txtRutaLote.Text, pindexador.Replace("Usuario: ", ""));
                                }
                                btnSeleccionarLote.Enabled = false;
                                return;
                        }
                            else
                            {
                                btnSeleccionarLote.Enabled = false;
                                continue;
                            }
                    }

                }
               

            }
        }
            btnSeleccionarLote.Enabled = false;
        }

        private string  loteasignado (string lote, string apellido)
        {
            usuariocoincide = 0;
       
            string lotet = lote;
            DirectoryInfo dilo = new DirectoryInfo("J:\\BDs\\RCproc\\FICHAS\\PROCESADAS\\" + apellido);
            string ruta = "J:\\BDs\\RCproc\\FICHAS\\PROCESADAS\\" + apellido + "\\" + lote;
            foreach (var items1 in dilo.GetDirectories())

            {

                ObtenerDatos oconsulta = new ObtenerDatos();
              

                string consulta = oconsulta.Consultaloteasignado(ruta);
                string pindexador = txtUsuario.Text;
              
                if (consulta != "")
                {
                   
                    if (consulta == pindexador.Replace("Usuario: ", ""))
                    {
                        usuariocoincide = 1;
                        lotet = lote;
                        return lotet;


                    }
                    else
                    {

                    }
                }
                else
                {
                  
                    return lotet;
                    
                }
               
            }
      
            return "";
        }

        private void btnGrabar_Click(object sender, EventArgs e)
        {

            limpiarygrabar();
           
        }

        private void limpiarygrabar ()
        {

            if (txtficha.Text == "")
            {
                txtficha.BackColor = Color.LightCoral;
                txtficha.Focus();
            }

            if (txtacta.Text == "")
            {
                txtacta.BackColor = Color.LightCoral;
                txtacta.Focus();
            }
            if (txttomo.Text == "")
            {
                txttomo.BackColor = Color.LightCoral;
                txttomo.Focus();
            }
            if (txtseccion.Text == "")
            {
                txtseccion.BackColor = Color.LightCoral;
                txtseccion.Focus();
            }
            if (txtaño.Text == "")
            {
                txtaño.BackColor = Color.LightCoral;
                txtaño.Focus();
            }
            if (txttipo.Text == "")
            {
                txttipo.BackColor = Color.LightCoral;
                txttipo.Focus();
            }
            if (txtapellido.Text == "")
            {
                txtapellido.BackColor = Color.LightCoral;
                txtapellido.Focus();
            }

            if (txtapellido.Text != "" && txttipo.Text != "" && txtaño.Text != "" && txtseccion.Text != "" && txttomo.Text != "" && txtacta.Text != "" && txtficha.Text != "")
            {
                Grabar();
            }

        }

        private void Grabar()
        {
          

            int NumeroLineaActual = lboxIndice.SelectedIndex;
            int NumeroLineaSiguiente = lboxIndice.SelectedIndex + 1;



            string LineaOriginal =  txtNombreImagen.Text + ";";


            string NuevoNombreImagen = (txtaño.Text + "-" + txtseccion.Text + "-" + txttomo.Text + "-" + txtacta.Text);

            lboxIndice.Items.Insert(NumeroLineaActual, LineaOriginal + NuevoNombreImagen);
            lboxIndice.Items.RemoveAt(NumeroLineaSiguiente);

            RegistrarProduccionAccess();
         
            txtficha.BackColor = Color.White;
            txtacta.BackColor = Color.White;
            txttomo.BackColor = Color.White;
            txtseccion.BackColor = Color.White;
            txtaño.BackColor = Color.White;
            txttipo.BackColor = Color.White;
            txtapellido.BackColor = Color.White;

            PasarAlSiguiente(NumeroLineaActual, NumeroLineaSiguiente);
        }


        private void btnSepararIlegible_Click(object sender, EventArgs e)
        {

            int NumeroLineaActual = lboxIndice.SelectedIndex;
            int NumeroLineaSiguiente = lboxIndice.SelectedIndex + 1;



            string LineaOriginal = txtNombreImagen.Text + ";";


            
            string NuevoNombreImagen = "ILEGIBLE";

            lboxIndice.Items.Insert(NumeroLineaActual, LineaOriginal + NuevoNombreImagen);
            lboxIndice.Items.RemoveAt(NumeroLineaSiguiente);

           

         
            
           
            string rutaprocesada = txtRutaLote.Text;
            string Rutabd = rutaprocesada + "\\" + txtNombreImagen.Text + ".jpg";
            string rutaverificada = "J:\\BDs\\RC\\FICHAS_VERIFICADAS";


            File.Copy(Rutabd,"\\srv01digi-prd.seclyt.gcba.gob.ar\\Indexacion\\BDs\\RC\\ILEGIBLE\\FICHAS\\" + txtNombreImagen.Text + ".jpg");
            File.Delete(Rutabd); //borra LA IMAGEN QUE SE ACABA DE GUARDAR

            PasarAlSiguiente(NumeroLineaActual, NumeroLineaSiguiente);

        }


        private void PasarAlSiguiente(int pNumeroLineaActual, int pNumeroLineaSiguiente)
        {
            int ficha1 = int.Parse(txtficha.Text);
            LimpiarTextBox();
            ObtenerDatos oconsulta = new ObtenerDatos();
            string rutaprocesada = txtRutaLote.Text;
            int Lote = rutaprocesada.LastIndexOf('\\');
            string NombreLoteCrudo = rutaprocesada.Substring(Lote);
            string Rutabd = rutaprocesada + "\\" + txtNombreImagen.Text + ".jpg";
            string NombreLote = NombreLoteCrudo.Replace("\\", "");
            string rutaverificada = "J:\\BDs\\RC\\FICHAS_VERIFICADAS";
           

            int ficha = oconsulta.Consultaficha(Rutabd);
          

            if (pNumeroLineaActual + 1 == lboxIndice.Items.Count)
            {

                VaciarFormulario();
                DesHabilarBotones();
                pictureBox1.Image = null;
                MessageBox.Show("El lote esta terminado!!!", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DirectoryInfo di = new DirectoryInfo(rutaprocesada);
                
                //File.Copy(Rutabd, rutaverificada + "\\"+(txtaño.Text + "-" + txtseccion.Text + "-" + txttomo.Text + "-" + txtacta.Text)+".jpg");
               


                if (di.GetFiles().Length > 0)
                {

                }
                else
                {
                    di.Delete(); // Borro la carpeta LOTE
                }
              
                string[] carpetacontenedoradelotes = rutaprocesada.Split(new char[] { '\\' });
                string carpetacontenedora = carpetacontenedoradelotes[4];
                DirectoryInfo dicont = new DirectoryInfo(" J:\\BDs\\RCproc\\FICHAS\\PROCESADAS\\" + carpetacontenedora);
           
                if (dicont.GetDirectories().Length > 0 ) 

                { }    
                else
                {
                    dicont.Delete(); // Borro la carpeta contenedora (apellido)
                }
            }


            else
            {
          

                lboxIndice.SetSelected(pNumeroLineaSiguiente, Convert.ToBoolean(1));
                CargarIndiceFijoEImagen();
             
            

            }

         
        }

        private void lboxIndice_MouseClick(object sender, MouseEventArgs e)
        {

            LimpiarTextBox();
            CargarIndiceFijoEImagen();
         
          
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            this.Close();
            btnSeleccionarLote.Enabled = true;
        }

        private void CargarFicha (string pruta)
        {
            System.IO.FileStream fs = new FileStream(pruta, FileMode.Open);

            pictureBox1.Image = System.Drawing.Image.FromStream(fs);
            HabilitarBotones();
            fs.Close();
        }

        private void CargarIndiceFijoEImagen()
        {
         


            CargarDatosFijosIndice();

             RutaNombreImagen = txtRutaLote.Text + "\\" + txtNombreImagen.Text + ".jpg";

            CargarFicha(RutaNombreImagen);

            ObtenerDatos oconsulta = new ObtenerDatos();
            DataTable dt;
            dt = oconsulta.ConsultaOCR(RutaNombreImagen);

            if (dt.Rows.Count > 0)
            { 
            txtapellido.Text = dt.Rows[0][1].ToString();
            txttipo.Text = dt.Rows[0][2].ToString();
            txttomo.Text = dt.Rows[0][3].ToString();
            txtacta.Text = dt.Rows[0][4].ToString();
            txtseccion.Text = dt.Rows[0][5].ToString();
            txtaño.Text= dt.Rows[0][6].ToString();
            txtficha.Text = dt.Rows[0][9].ToString();
             Xrecta = int.Parse (dt.Rows[0][16].ToString());
             Yrecta = int.Parse(dt.Rows[0][15].ToString());
            }
            string unificado = "";

            for (int i = 0; i < txttipo.Text.Length; i++)
            {
                if (txttipo.Text[i].ToString() == " " || txttipo.Text[i].ToString() == ":")
                {         
                }
                else
                {
                 unificado = unificado + txttipo.Text[i].ToString();
                   
                }
              
            }
            txttipo.Text = unificado;
            unificado = "";
            for (int i = 0; i < txtaño.Text.Length; i++)
            {
                if (txtaño.Text[i].ToString() == " " || txtaño.Text[i].ToString() == ":")
                {
                }
                else
                {
                    unificado = unificado + txtaño.Text[i].ToString();

                }
      
            }
            txtaño.Text = unificado;
            unificado = "";
            for (int i = 0; i < txttomo.Text.Length; i++)
            {
                if (txttomo.Text[i].ToString() == " " || txttomo.Text[i].ToString() == ":" )
                {
                }
                else
                {
                    unificado = unificado + txttomo.Text[i].ToString();

                }

            }
            txttomo.Text = unificado;
            unificado = "";
            for (int i = 0; i < txtseccion.Text.Length; i++)
            {
                if (txtseccion.Text[i].ToString() == " "|| txtseccion.Text[i].ToString() == ":")
                {
                }
                else
                {
                    unificado = unificado + txtseccion.Text[i].ToString();

                }
            }
            txtseccion.Text = unificado;
            unificado = "";
            for (int i = 0; i < txtacta.Text.Length; i++)
            {
                if (txtacta.Text[i].ToString() == " " || txtacta.Text[i].ToString() == ":")
                {
                }
                else
                {
                    unificado = unificado + txtacta.Text[i].ToString();

                }
            }
            txtacta.Text = unificado;
            unificado = "";
            for (int i = 0; i < txtficha.Text.Length; i++)
            {
                if (txtficha.Text[i].ToString() == " ")
                {
                }
                else
                {
                    unificado = unificado + txtficha.Text[i].ToString();

                }
            }
            txtficha.Text = unificado;
            unificado = "";



            if (txtapellido.Lines.Length >= 2)
            {
                string[] lineas = txtapellido.Lines;

                List<string> lineasNoVacias = new List<string>();
                foreach (string linea in lineas)
                {
                    if (!string.IsNullOrWhiteSpace(linea))
                    {
                        lineasNoVacias.Add(linea);
                    }
                }
                txtapellido.Lines = lineasNoVacias.ToArray();
            }
            if (txttipo.Lines.Length >= 2)
            {
                string[] lineas = txttipo.Lines;

                List<string> lineasNoVacias = new List<string>();
                foreach (string linea in lineas)
                {
                    if (!string.IsNullOrWhiteSpace(linea))
                    {
                        lineasNoVacias.Add(linea);
                    }
                }
                txttipo.Lines = lineasNoVacias.ToArray();
            }
            if (txtaño.Lines.Length >= 2)
            {
                string[] lineas = txtaño.Lines;

                List<string> lineasNoVacias = new List<string>();
                foreach (string linea in lineas)
                {
                    if (!string.IsNullOrWhiteSpace(linea))
                    {
                        lineasNoVacias.Add(linea);
                    }
                }
                txtaño.Lines = lineasNoVacias.ToArray();
            }
            if (txttomo.Lines.Length >= 2)
            {
                string[] lineas = txttomo.Lines;
                List<string> lineasNoVacias = new List<string>();
                string lineaActual = "";

                foreach (string linea in lineas)
                {
                    if (!string.IsNullOrWhiteSpace(linea))
                    {
                        lineaActual += linea.Trim() + " "; // Agregar la línea actual con un espacio en blanco al final
                    }
                }

                // Eliminar el último espacio en blanco
                lineaActual = lineaActual.TrimEnd();

                // Asignar la línea resultante al control txttomo
                txttomo.Text = lineaActual;
            }



            if (txtseccion.Lines.Length >= 2)
            {
                string[] lineas = txtseccion.Lines;

                List<string> lineasNoVacias = new List<string>();
                foreach (string linea in lineas)
                {
                    if (!string.IsNullOrWhiteSpace(linea))
                    {
                        lineasNoVacias.Add(linea);
                    }
                }
                txtseccion.Lines = lineasNoVacias.ToArray();
            }
            if (txtacta.Lines.Length >= 2)
            {
                string[] lineas = txtacta.Lines;

                List<string> lineasNoVacias = new List<string>();
                foreach (string linea in lineas)
                {
                    if (!string.IsNullOrWhiteSpace(linea))
                    {
                        lineasNoVacias.Add(linea);
                    }
                }
                txtacta.Lines = lineasNoVacias.ToArray();
            }
            if (txtficha.Lines.Length >= 2)
            {
                string[] lineas = txtficha.Lines;

                List<string> lineasNoVacias = new List<string>();
                foreach (string linea in lineas)
                {
                    if (!string.IsNullOrWhiteSpace(linea))
                    {
                        lineasNoVacias.Add(linea);
                    }
                }
                txtficha.Lines = lineasNoVacias.ToArray();
            }







            txtapellido.Focus();
        }



        private void CargarDatosFijosIndice()
        {
           
            txtNombreImagen.Text = lboxIndice.SelectedItem.ToString();

        }


      
        private void LimpiarTextBox()
        {
            txtacta.Clear();
            txtapellido.Clear();
            txtaño.Clear();
            txtficha.Clear();
            txtseccion.Clear();
            txttipo.Clear();
            txttomo.Clear();
        }

        private void VaciarFormulario()
        {
            txtRutaLote.Clear();
           
            txtNombreImagen.Clear();
          

            LimpiarTextBox();
            lboxIndice.Items.Clear();


            btnSeleccionarLote.Select();
            btnSeleccionarLote.Focus();
           
        }



       private void RegistrarProduccionAccess()
        {
        string Ruta = txtRutaLote.Text;
        int Lote = Ruta.LastIndexOf('\\');
        string NombreLoteCrudo = Ruta.Substring(Lote);
        string Rutabd = txtRutaLote.Text+ "\\"+txtNombreImagen.Text+ ".jpg";
        string NombreLote = NombreLoteCrudo.Replace("\\", "");
        int Imagen = Convert.ToInt32(txtNombreImagen.Text);
        string NombreImagen = Convert.ToString(Imagen);
        int IdProyecto = 21;
        string  pindexador = txtUsuario.Text;
        string rutaverificada = "J:\\BDs\\RC\\FICHAS_VERIFICADAS";

            Datos.ProduccionAccess oProduccionAccess = new Datos.ProduccionAccess();
            ObtenerDatos oconsulta = new ObtenerDatos();


            if (oconsulta.ConsultaOCR(Rutabd).Rows.Count == 0)
            {
              
                oProduccionAccess.VerificadasInsert(txtapellido.Text, txttipo.Text, txttomo.Text, txtacta.Text, txtseccion.Text, txtaño.Text, "0", "", txtficha.Text, Rutabd, (txtNombreImagen.Text + ".jpg"), NombreLote, pindexador.Replace("Usuario: ", ""));
            }
           else
            {
             
                oProduccionAccess.Verificadas(txtapellido.Text, txttipo.Text, txttomo.Text, txtacta.Text, txtseccion.Text, txtaño.Text, "0", "", txtficha.Text, Rutabd, (txtNombreImagen.Text + ".jpg"), NombreLote, pindexador.Replace("Usuario: ", ""));
            }
    

            oProduccionAccess.FichasVerificadasRC(txtapellido.Text, txttipo.Text, txttomo.Text, txtacta.Text, txtseccion.Text, txtaño.Text, "0", "", txtficha.Text, (txtaño.Text + "-" + txtseccion.Text + "-" + txttomo.Text + "-" + txtacta.Text));
          
            try
            {
                string archivoOrigen = Rutabd;
                string archivoDestino = Path.Combine(rutaverificada, $"{txtaño.Text}-{txtseccion.Text}-{txttomo.Text}-{txtacta.Text}.jpg");

                File.Copy(archivoOrigen, archivoDestino);

                // La copia fue exitosa, ahora puedes eliminar el archivo original
                File.Delete(archivoOrigen);
            }
            catch (IOException ex)
            {
                // Manejar la excepción si la copia o eliminación falla
                Console.WriteLine("Error al copiar o eliminar el archivo: " + ex.Message);
            }

            //File.Copy(Rutabd, rutaverificada + "\\" + (txtaño.Text + "-" + txtseccion.Text + "-" + txttomo.Text + "-" + txtacta.Text) + ".jpg");
            //File.Delete(Rutabd); //borra LA IMAGEN QUE SE ACABA DE GUARDAR

        }

      

        private async void txtapellido_Enter(object sender, EventArgs e)
        {
            if (lboxIndice.SelectedIndex >= 0)
            {
                
          
            limpiarrectangulo();

            Lienzo = pictureBox1.CreateGraphics();
            
            Pen pincel = new Pen(Color.Yellow, 4);
           
            Brush brush = new SolidBrush(Color.FromArgb(60, 255, 255, 0));
            await Task.Delay(100);
            
                if (Yrecta != 0 && Xrecta != 0)
                {
                    Lienzo.DrawRectangle(pincel, Xrecta -290, Yrecta - 596, 800, 100);
                    Lienzo.FillRectangle(brush, Xrecta - 290, Yrecta - 596, 800, 100);
                  
                }
                else
                {
                    Lienzo.DrawRectangle(pincel, 102, 6, 800, 100);
                    Lienzo.FillRectangle(brush, 102, 6, 800, 100);
                }

            }
        }

      private  void limpiarrectangulo()
        {
            if (RutaNombreImagen != "")
            {

                CargarFicha(RutaNombreImagen);
              
            }
        }

        private async void txtaño_Enter(object sender, EventArgs e)
        {
            limpiarrectangulo();
            if (lboxIndice.SelectedIndex >= 0)
            {
                Lienzo = pictureBox1.CreateGraphics();
            Pen pincel = new Pen(Color.Yellow, 4);
            Brush brush = new SolidBrush(Color.FromArgb(60, 255, 255, 0));
            await Task.Delay(100);
                if (Yrecta != 0 && Xrecta != 0)
                {
                    Lienzo.DrawRectangle(pincel, Xrecta - 320, Yrecta - 380, 250, 60);
                    Lienzo.FillRectangle(brush, Xrecta - 320, Yrecta - 380, 250, 60);

                }
                else
                {
                    Lienzo.DrawRectangle(pincel, 70, 222, 250, 60);
                    Lienzo.FillRectangle(brush, 70, 222, 250, 60);
                }
               
            }
        }

        private async void txtacta_Enter(object sender, EventArgs e)
        {
            limpiarrectangulo();
            if (lboxIndice.SelectedIndex >= 0)
            {
                Lienzo = pictureBox1.CreateGraphics();
            Pen pincel = new Pen(Color.Yellow, 4);
            Brush brush = new SolidBrush(Color.FromArgb(60, 255, 255, 0));
            await Task.Delay(100);
                if (Yrecta != 0 && Xrecta != 0)
                {
                    Lienzo.DrawRectangle(pincel, Xrecta+410, Yrecta - 380, 220, 60);
                    Lienzo.FillRectangle(brush, Xrecta+410 , Yrecta - 380, 220, 60);

                }
                else
                {
                    Lienzo.DrawRectangle(pincel, 802, 222, 250, 80);
                    Lienzo.FillRectangle(brush, 802, 222, 250, 80);
                }
              
            }
        }

        private async void txttomo_Enter(object sender, EventArgs e)
        {
            limpiarrectangulo();
                if (lboxIndice.SelectedIndex >= 0)
                {
                    Lienzo = pictureBox1.CreateGraphics();
            Pen pincel = new Pen(Color.Yellow, 4);
            Brush brush = new SolidBrush(Color.FromArgb(60, 255, 255, 0));
            await Task.Delay(100);

                if (Yrecta != 0 && Xrecta != 0)
                {
                    Lienzo.DrawRectangle(pincel, Xrecta +180, Yrecta - 380, 250, 60);
                    Lienzo.FillRectangle(brush, Xrecta+180, Yrecta - 380, 250, 60);

                }
                else
                {
                    Lienzo.DrawRectangle(pincel, 572, 222, 250, 60);
                    Lienzo.FillRectangle(brush, 572, 222, 250, 60);
                }

               
                }

            }

        private async void txtficha_Enter(object sender, EventArgs e)
        {
            limpiarrectangulo();
                    if (lboxIndice.SelectedIndex >= 0)
                    {
                        Lienzo = pictureBox1.CreateGraphics();
            Pen pincel = new Pen(Color.Yellow, 4);
            Brush brush = new SolidBrush(Color.FromArgb(60, 255, 255, 0));
            await Task.Delay(100);
                if (Yrecta != 0 && Xrecta != 0)
                {
                    Lienzo.DrawRectangle(pincel, Xrecta , Yrecta-130  , 280, 130);
                    Lienzo.FillRectangle(brush, Xrecta , Yrecta -130 , 280, 130);

                }
                else
                {
                    Lienzo.DrawRectangle(pincel, 392, 472, 280, 130);
                    Lienzo.FillRectangle(brush, 392, 472, 280, 130);
                }
              
                    }
                }

        private async void txttipo_Enter(object sender, EventArgs e)
        {
            limpiarrectangulo();
          if (lboxIndice.SelectedIndex >= 0)
                        {
           Lienzo = pictureBox1.CreateGraphics();

            Pen pincel = new Pen(Color.Yellow, 4);

            Brush brush = new SolidBrush(Color.FromArgb(60, 255, 255, 0));
            await Task.Delay(130);
                if (Yrecta != 0 && Xrecta != 0)
                {
                    Lienzo.DrawRectangle(pincel, Xrecta-100 , Yrecta - 520, 400, 80);
                    Lienzo.FillRectangle(brush, Xrecta -100, Yrecta - 520, 400, 80);

                }
                else
                {
                    Lienzo.DrawRectangle(pincel, 360, 110, 400, 80);
                    Lienzo.FillRectangle(brush, 360, 110, 400, 80);
                }

              
                        }
                    
         }

     

        private async void txtseccion_Enter(object sender, EventArgs e)
        {
            limpiarrectangulo();
                        if (lboxIndice.SelectedIndex >= 0)
                        {
                            Lienzo = pictureBox1.CreateGraphics();
            Pen pincel = new Pen(Color.Yellow, 4);
            Brush brush = new SolidBrush(Color.FromArgb(60, 255, 255, 0));
            await Task.Delay(100);
                if (Yrecta != 0 && Xrecta != 0)
                {
                    Lienzo.DrawRectangle(pincel, Xrecta - 100, Yrecta - 380, 250, 60);
                    Lienzo.FillRectangle(brush, Xrecta - 100, Yrecta - 380, 250, 60);

                }
                else
                {
                    Lienzo.DrawRectangle(pincel, 290, 285, 250, 60);
                    Lienzo.FillRectangle(brush, 290, 285, 250, 60);
                }
              
                        }
                    }

    

        private void txtaño_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txtacta_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txtficha_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }



        private void txtapellido_KeyDown(object sender, KeyEventArgs e)
        {
            
                
                if (e.KeyCode.Equals(Keys.Enter))
            {
                System.Windows.Forms.SendKeys.Send("{TAB}");
            }
            if (e.KeyCode.Equals(Keys.Down))
            {
                System.Windows.Forms.SendKeys.Send("{TAB}");
            }


            if (e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                char caracter = (char)(e.KeyCode + 32); 
                limpiarygrabar();
            }
        }


    

        private void txttipo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter))
                System.Windows.Forms.SendKeys.Send("{TAB}");
           
            if (e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                char caracter = (char)(e.KeyCode + 32);
                limpiarygrabar();
            }
            if (e.KeyCode.Equals(Keys.Down))
            {
                System.Windows.Forms.SendKeys.Send("{TAB}");
            }
            if (e.KeyCode.Equals(Keys.Up))
            {
                txtapellido.Focus();
            }

        }

        private void txtaño_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter))
                System.Windows.Forms.SendKeys.Send("{TAB}");
          
            if (e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                char caracter = (char)(e.KeyCode + 32);
                limpiarygrabar();
            }
            if (e.KeyCode.Equals(Keys.Down))
            {
                System.Windows.Forms.SendKeys.Send("{TAB}");
            }
            if (e.KeyCode.Equals(Keys.Up))
            {
                txttipo.Focus();
            }

        }



        private void txtseccion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter))
                System.Windows.Forms.SendKeys.Send("{TAB}");
         
            if (e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                char caracter = (char)(e.KeyCode + 32);
                limpiarygrabar();
            }
            if (e.KeyCode.Equals(Keys.Down))
            {
                System.Windows.Forms.SendKeys.Send("{TAB}");
            }
            if (e.KeyCode.Equals(Keys.Up))
            {
                txtaño.Focus();
            }

        }

        private void txttomo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter))
                System.Windows.Forms.SendKeys.Send("{TAB}");
           
            if (e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                char caracter = (char)(e.KeyCode + 32);
                limpiarygrabar();
            }
            if (e.KeyCode.Equals(Keys.Down))
            {
                System.Windows.Forms.SendKeys.Send("{TAB}");
            }
            if (e.KeyCode.Equals(Keys.Up))
            {
                txtseccion.Focus();
            }

        }

        private void txtacta_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter))
                System.Windows.Forms.SendKeys.Send("{TAB}");
           
            if (e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                char caracter = (char)(e.KeyCode + 32);
                limpiarygrabar();
            }
            if (e.KeyCode.Equals(Keys.Down))
            {
                System.Windows.Forms.SendKeys.Send("{TAB}");
            }
            if (e.KeyCode.Equals(Keys.Up))
            {
                txttomo.Focus();
            }

        }

        private void txtficha_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter))
                System.Windows.Forms.SendKeys.Send("{TAB}");
         
            if (e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                char caracter = (char)(e.KeyCode + 32);
                limpiarygrabar();
            }
            if (e.KeyCode.Equals(Keys.Down))
            {
                System.Windows.Forms.SendKeys.Send("{TAB}");
            }
            if (e.KeyCode.Equals(Keys.Up))
            {
                txtacta.Focus();
            }

        }

        private void btnGrabar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter))
                System.Windows.Forms.SendKeys.Send("{TAB}");
          
            if (e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                char caracter = (char)(e.KeyCode + 32);
                limpiarygrabar();
            }
        }

        private void lboxIndice_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter))
                System.Windows.Forms.SendKeys.Send("{TAB}");
           
            if (e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                char caracter = (char)(e.KeyCode + 32);
                limpiarygrabar();
            }
        }

        private void txtapellido_KeyPress(object sender, KeyPressEventArgs e)
        {
           
        }

        private void txtapellido_TextChanged(object sender, EventArgs e)
        {
           
            

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string pindexador = txtUsuario.Text;

            if (lboxIndice.Items.Count == 0 && txtRutaLote.Text != "")
            {
                ProduccionAccess oinsertar = new ProduccionAccess();
                oinsertar.CerrarLoteAsignado(txtRutaLote.Text, pindexador.Replace("Usuario: ", ""));
                txtRutaLote.Text = "";
                btnSeleccionarLote.Enabled = true;
            }
        }
    }
    }

    
    
    

