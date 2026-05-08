using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Indexador_GCBA_.Datos;
using PdfiumViewer;
using System.Reflection;
using System.Linq;




namespace Indexador_GCBA_
{
    public partial class FormActasDeNacimiento : Form
    {

      

        Entidades.Indexador oIndexador = new Entidades.Indexador();
        Graphics Lienzo;
        string RutaNombreImagen = "";
        int usuariocoincide = 0;
        int inicio = 0;
        int mes_inscripcion = 0;
        int pixelgriss = 0;
        string rutaactasprecesadas= "J:\\BDs\\RCproc\\ACTAS\\PROCESADAS\\";
      //  string rutaactasprecesadaslocal = "C:\\Users\\20330200570\\Desktop\\Rcproc\\ACTAS\\PROCESADAS\\";
        string rutaverificada = "J:\\BDs\\RC\\ACTAS_VERIFICADAS";
        //  string rutaverificadalocal = "C:\\Users\\20330200570\\Desktop\\Rc\\ACTAS\\VERIFICADAS";
        int editar = 0;
     
        public FormActasDeNacimiento(Entidades.Indexador pIndexador)
        {
            InitializeComponent();
          
            oIndexador = pIndexador;
            WindowState = FormWindowState.Maximized;
            DesHabilarBotones();
            ListEmbeddedResources();
            ExtractPdfium();
            txtNumero_Acta.KeyPress += ValidarSoloNumeros;
            txtAnio_Acta.KeyPress += ValidarSoloNumeros;
            txtDia_inscripcion.KeyPress += ValidarSoloNumeros;
            txtAño_Inscripcion.KeyPress += ValidarSoloNumeros;
            txtDia_Nacimiento.KeyPress += ValidarSoloNumeros;
            txtAño_Nacimiento.KeyPress += ValidarSoloNumeros;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom; // Asegura que la imagen se ajusta correctamente
            pictureBox1.SizeChanged += PictureBox1_SizeChanged; // Detectar cambio de tamaño del PictureBox


        }

        private void ListEmbeddedResources()
        {
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            Console.WriteLine("Recursos incrustados en el ensamblado:");
            foreach (var resource in resources)
            {
                Console.WriteLine(resource);
            }
        }

        private void ExtractPdfium()
        {
            string resourceName = "Indexador_GCBA_.pdfium.dll"; // Nombre exacto del recurso
            string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdfium.dll");

            Console.WriteLine($"📂 Extrayendo pdfium.dll a: {outputPath}");

            if (!File.Exists(outputPath))
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Console.WriteLine($"❌ Error: No se encontró el recurso {resourceName}.");
                        return;
                    }

                    using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream);
                    }

                    Console.WriteLine($"✅ pdfium.dll extraída en {outputPath}");
                }
            }
            else
            {
                Console.WriteLine($"🔹 pdfium.dll ya existe en {outputPath}, no se necesita extraer.");
            }
        }





        private void PictureBox1_SizeChanged(object sender, EventArgs e)
        {
            if (lastWidth > 0 && lastHeight > 0) // Verifica si hay un rectángulo almacenado
            {
                ResaltarCampo(lastX, lastY, lastWidth, lastHeight);
            }
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

        private async void btnSeleccionarLote_Click(object sender, EventArgs e)
        {
            btnSeleccionarLote.Enabled = false;
            string loteSeleccionado = null;
            string usuarioAsignado = null;

            await Task.Run(() =>
            {
                DirectoryInfo di1 = new DirectoryInfo(rutaactasprecesadas);

                foreach (var items in di1.GetDirectories())
                {
                    string apellido = items.Name;
                    if (string.IsNullOrEmpty(apellido)) continue;

                    DirectoryInfo diap = new DirectoryInfo(Path.Combine(rutaactasprecesadas, apellido));

                    foreach (var items1 in diap.GetDirectories())
                    {
                        string lote = items1.Name;
                        if (string.IsNullOrEmpty(lote)) continue;

                        string lotea = loteasignado(lote, apellido);
                        if (string.IsNullOrEmpty(lotea)) continue;

                        string rutaLote = Path.Combine(rutaactasprecesadas, apellido, lotea);
                        usuarioAsignado = new ObtenerDatos().Consultaloteasignado(rutaLote);

                        if (string.IsNullOrEmpty(usuarioAsignado))
                        {
                            usuarioAsignado = txtUsuario.Text;
                            new ProduccionAccess().loteasignado(rutaLote, usuarioAsignado.Replace("Usuario: ", ""));
                        }

                        loteSeleccionado = rutaLote;
                        break;
                    }
                    if (loteSeleccionado != null) break;
                }
            });

            if (!string.IsNullOrEmpty(loteSeleccionado))
            {
                txtRutaLote.Text = loteSeleccionado;
                CargarDocumentosEnSegundoPlano(loteSeleccionado);
            }
        }

        private async void CargarDocumentosEnSegundoPlano(string rutaLote)
        {
            DirectoryInfo dilo = new DirectoryInfo(rutaLote);
            ObtenerDatos oconsulta = new ObtenerDatos();
            bool primerElemento = true; // Variable para seleccionar solo el primer elemento

            foreach (var file in dilo.GetFiles("*.pdf"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file.Name);

                if (usuariocoincide == 1)
                {
                    int verificado = await Task.Run(() => oconsulta.Consultaactaverificada(file.FullName));
                    if (verificado == 0)
                    {
                        Invoke(new Action(() =>
                        {
                            lboxIndice.Items.Add(fileName);
                            if (primerElemento) // Seleccionar el primer elemento agregado
                            {
                                lboxIndice.SelectedIndex = 0;
                                CargarIndiceFijoEImagen(); // Cargar la imagen asociada
                                primerElemento = false;
                            }
                        }));
                    }
                }
                else
                {
                    Invoke(new Action(() =>
                    {
                        lboxIndice.Items.Add(fileName);
                        if (primerElemento) // Seleccionar el primer elemento agregado
                        {
                            lboxIndice.SelectedIndex = 0;
                            CargarIndiceFijoEImagen(); // Cargar la imagen asociada
                            primerElemento = false;
                        }
                    }));
                }
            }
        }




        private string loteasignado(string lote, string apellido)
        {
            usuariocoincide = 0;

            string lotet = lote;
            DirectoryInfo dilo = new DirectoryInfo(rutaactasprecesadas + apellido);
            string ruta = rutaactasprecesadas + apellido + "\\" + lote;
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
            if (txtNumero_Acta.Text.Length < 4)
            {
                txtNumero_Acta.Text = txtNumero_Acta.Text.PadLeft(4, '0');

            }
            // Lista de TextBox obligatorios
            TextBox[] camposObligatorios = {
        txtAño_Inscripcion, txtMes_Inscripcion, txtDia_inscripcion,
        txtAnio_Acta, txtNumero_Acta, txtTomo_Acta, txtCircunscripcion_Acta,
        txtNombre_Nacido, txtCNI_Numero, txtSexo_Nacido, txtDia_Nacimiento,
        txtMes_Nacimiento, txtAño_Nacimiento, txtNombre_Padre, txtNombre_Madre
    };

            // Validar que todos los campos estén completos
            foreach (TextBox campo in camposObligatorios)
            {
                if (string.IsNullOrWhiteSpace(campo.Text))
                {
                    campo.BackColor = System.Drawing.Color.LightCoral;
                    campo.Focus();
                    MessageBox.Show("Falta completar campos obligatorios");
                    return; // Sale del método si falta un campo
                }
                else
                {
                    campo.BackColor = SystemColors.Window; // Restablece el color si está correcto
                }
            }

            // Conversión de Mes a Número (optimizado con Dictionary)
            // Diccionario para convertir el mes de texto a número
            Dictionary<string, int> meses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    { "ENERO", 1 }, { "FEBRERO", 2 }, { "MARZO", 3 }, { "ABRIL", 4 },
    { "MAYO", 5 }, { "JUNIO", 6 }, { "JULIO", 7 }, { "AGOSTO", 8 },
    { "SEPTIEMBRE", 9 }, { "OCTUBRE", 10 }, { "NOVIEMBRE", 11 }, { "DICIEMBRE", 12 }
};

            // Intentar convertir el mes de inscripción
            if (!meses.TryGetValue(txtMes_Inscripcion.Text.Trim().ToUpper(), out int mesInscripcion))
            {
                MessageBox.Show("Mes de inscripción inválido. Debe ser un mes válido (Ejemplo: ENERO, FEBRERO, etc.)",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtMes_Inscripcion.BackColor = Color.LightCoral;
                txtMes_Inscripcion.Focus();
                return; // Detiene el proceso si el mes es inválido
            }

            // Si todos los campos están completos, proceder con la grabación
            Grabar(mesInscripcion);
        }


        private void Grabar(int mesInscripcion)
        {
            if (txtNumero_Acta.Text.Length < 4)
            {
                txtNumero_Acta.Text = txtNumero_Acta.Text.PadLeft(4, '0');

            }
            // Verificar si hay un elemento seleccionado en lboxIndice
            if (lboxIndice.SelectedIndex == -1)
            {
                MessageBox.Show("Debe seleccionar un índice antes de grabar.");
                return;
            }

            int NumeroLineaActual = lboxIndice.SelectedIndex;
            int NumeroLineaSiguiente = NumeroLineaActual + 1;

            string LineaOriginal = txtNombreImagen.Text + ";";
            string LineaOriginal2 = txtNombreImagen.Text;

            // Construcción del nuevo nombre de imagen
            string NuevoNombreImagen = $"{txtAnio_Acta.Text}-{txtCircunscripcion_Acta.Text}-{txtTomo_Acta.Text}-{txtNumero_Acta.Text}";

            // Insertar la nueva línea y eliminar la anterior
            lboxIndice.Items.Insert(NumeroLineaActual, LineaOriginal + NuevoNombreImagen);

            if (NumeroLineaSiguiente < lboxIndice.Items.Count)
            {
                lboxIndice.Items.RemoveAt(NumeroLineaSiguiente);
            }

            // Registrar en la base de datos
            RegistrarProduccionAccess(NuevoNombreImagen, LineaOriginal2, mesInscripcion);

            // Lista de TextBox a resetear colores
            TextBox[] campos = {
        txtAño_Inscripcion, txtMes_Inscripcion, txtDia_inscripcion, txtAnio_Acta,
        txtNumero_Acta, txtTomo_Acta, txtCircunscripcion_Acta, txtNombre_Nacido,
        txtCNI_Numero, txtSexo_Nacido, txtDia_Nacimiento, txtMes_Nacimiento,
        txtAño_Nacimiento, txtNombre_Padre, txtNombre_Madre
    };

            // Restaurar color de los campos
            foreach (TextBox campo in campos)
            {
                campo.BackColor = Color.White;
            }

            // Pasar al siguiente índice
            PasarAlSiguiente(NumeroLineaActual, NumeroLineaSiguiente, NuevoNombreImagen);
        }



        private void btnSepararIlegible_Click(object sender, EventArgs e)
        {
            // Verificar si hay un elemento seleccionado en lboxIndice
            if (lboxIndice.SelectedIndex == -1)
            {
                MessageBox.Show("Debe seleccionar un índice antes de marcar como ILEGIBLE.");
                return;
            }

            int NumeroLineaActual = lboxIndice.SelectedIndex;
            int NumeroLineaSiguiente = NumeroLineaActual + 1;

            string LineaOriginal = txtNombreImagen.Text + ";";
            string NuevoNombreImagen = "ILEGIBLE";

            // Modificar la lista de índices
            lboxIndice.Items.Insert(NumeroLineaActual, LineaOriginal + NuevoNombreImagen);

            if (NumeroLineaSiguiente < lboxIndice.Items.Count)
            {
                lboxIndice.Items.RemoveAt(NumeroLineaSiguiente);
            }

            string rutaProcesada = txtRutaLote.Text;
            string rutaBD = Path.Combine(rutaProcesada, txtNombreImagen.Text + ".pdf");
            string destinoIlegible = Path.Combine(@"J:\BDs\RC\ILEGIBLE\ACTAS\", txtNombreImagen.Text + ".pdf");

            try
            {
                // Copiar archivo si no existe en destino
                if (!File.Exists(destinoIlegible))
                {
                    File.Copy(rutaBD, destinoIlegible);
                }

                // Intentar eliminar el archivo con reintentos si está en uso
                int intentos = 0, maxIntentos = 3;
                TimeSpan espera = TimeSpan.FromSeconds(1);

                while (intentos < maxIntentos)
                {
                    try
                    {
                        if (File.Exists(rutaBD))
                        {
                            File.SetAttributes(rutaBD, FileAttributes.Normal);
                            File.Delete(rutaBD);
                        }
                        break; // Salir del bucle si la eliminación fue exitosa
                    }
                    catch (IOException)
                    {
                        System.Threading.Thread.Sleep(espera);
                        intentos++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Pasar al siguiente índice
            PasarAlSiguiente(NumeroLineaActual, NumeroLineaSiguiente, txtNombreImagen.Text);
        }



        private void VaciarFormulario()
        {
            txtRutaLote.Clear();

            txtNombreImagen.Clear();


            LimpiarTextBox();
            lboxIndice.Items.Clear();

            btnSeleccionarLote.Enabled = true;
            btnSeleccionarLote.Select();
            btnSeleccionarLote.Focus();

        }
        private void PasarAlSiguiente(int pNumeroLineaActual, int pNumeroLineaSiguiente, string pnuevonombre)
        {
            // Resetear interfaz
            pictureBox1.Image = null;
           // pictureBox1.Visible = false;
            LimpiarTextBox();
            inicio = 0;
            txtCircunscripcion_Acta.Focus();

            ObtenerDatos oconsulta = new ObtenerDatos();
            string rutaprocesada = txtRutaLote.Text;
            string rutabd = Path.Combine(rutaprocesada, txtNombreImagen.Text + ".pdf");

            // Extraer nombre del lote
            string NombreLote = Path.GetFileName(rutaprocesada);

            // Si el lote ha terminado
            if (pNumeroLineaActual + 1 >= lboxIndice.Items.Count)
            {
                VaciarFormulario();
                DesHabilarBotones();
                pictureBox1.Image = null;
                MessageBox.Show("El lote está terminado!!!", "Mensaje", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Intentar eliminar la carpeta del lote si está vacía
                try
                {
                    DirectoryInfo di = new DirectoryInfo(rutaprocesada);
                    if (di.Exists && di.GetFiles().Length == 0)
                    {
                        di.Delete(); // Borra la carpeta del lote si está vacía
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al eliminar la carpeta del lote: " + ex.Message);
                }

                // Intentar eliminar la carpeta contenedora (apellido) si está vacía
                try
                {
                    string[] carpetas = rutaprocesada.Split('\\');
                    if (carpetas.Length > 4)
                    {
                        string carpetaContenedora = Path.Combine(rutaactasprecesadas, carpetas[4]);
                        DirectoryInfo dicont = new DirectoryInfo(carpetaContenedora);

                        if (dicont.Exists && dicont.GetDirectories().Length == 0)
                        {
                            dicont.Delete(); // Borra la carpeta contenedora si está vacía
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al eliminar la carpeta contenedora: " + ex.Message);
                }
            }
            else
            {
                // Pasar al siguiente índice en lboxIndice
                if (pNumeroLineaSiguiente < lboxIndice.Items.Count)
                {
                    lboxIndice.SetSelected(pNumeroLineaSiguiente, true);
                    CargarIndiceFijoEImagen();
                }
            }
        }


        private void lboxIndice_MouseClick(object sender, MouseEventArgs e)
        {
            // Si no hay un elemento seleccionado, salir del método
            if (lboxIndice.SelectedIndex == -1)
                return;

            LimpiarTextBox();
            CargarIndiceFijoEImagen();
            

            if (editar == 1)
            {
                ObtenerDatos odatos = new ObtenerDatos();
                DataTable dataTable = odatos.editaracta(txtNombreImagen.Text + ".pdf");

                if (dataTable.Rows.Count > 0)
                {
                    // Mapeo de columnas del DataTable a los TextBox correspondientes
                    Dictionary<TextBox, int> mapeoCampos = new Dictionary<TextBox, int>
            {
                { txtCircunscripcion_Acta, 2 }, { txtTomo_Acta, 3 }, { txtNumero_Acta, 4 }, { txtAnio_Acta, 5 },
                { txtDia_inscripcion, 6 }, { txtMes_Inscripcion, 7 }, { txtAño_Inscripcion, 8 }, { txtNombre_Nacido, 9 },
                { txtCNI_Numero, 10 }, { txtSexo_Nacido, 11 }, { txtDia_Nacimiento, 12 }, { txtMes_Nacimiento, 13 },
                { txtAño_Nacimiento, 14 }, { txtNombre_Padre, 15 }, { txtNombre_Madre, 16 }
            };

                    // Asignación de valores de manera más eficiente
                    foreach (var campo in mapeoCampos)
                    {
                        campo.Key.Text = dataTable.Rows[0][campo.Value]?.ToString() ?? string.Empty;
                    }
                }
            }
        }



        private void btnSalir_Click(object sender, EventArgs e)
        {
            //Datos.ObtenerDatos odatos = new Datos.ObtenerDatos();
            //dataGridView1.DataSource = odatos.seleccionartodosql();
            //this.Close();
            //btnSeleccionarLote.Enabled = true;
        }

        private void CargarFicha(string pruta)
        {
            // pdfViewer1.Load()
            //   webBrowser1.Navigate(pruta);
            CargarPDFEnPictureBox(pruta);
            HabilitarBotones();

        }






        private void CargarIndiceFijoEImagen()
        {
            editar = 0;
            CargarDatosFijosIndice();

            string nombreactaeditar = txtNombreImagen.Text;

            if (nombreactaeditar.Contains(";"))
            {
                // Extraer la parte posterior al punto y coma
                txtNombreImagen.Text = nombreactaeditar.Substring(nombreactaeditar.IndexOf(';') + 1);
                RutaNombreImagen = Path.Combine(rutaverificada, txtNombreImagen.Text + ".pdf");
                editar = 1;
            }
            else
            {
                RutaNombreImagen = Path.Combine(txtRutaLote.Text, txtNombreImagen.Text + ".pdf");

                if (nombreactaeditar.Contains("-"))
                {
                    int guion = nombreactaeditar.IndexOf('-');

                    if (guion != -1)
                    {
                        string numeroActa = nombreactaeditar.Substring(guion + 1);
                        txtNumero_Acta.Text = numeroActa.StartsWith("0") ? numeroActa.Substring(1) : numeroActa;
                    }
                }
            }
            inicio = 1;
            CargarFicha(RutaNombreImagen);
        }




        private void CargarDatosFijosIndice()
        {

            txtNombreImagen.Text = lboxIndice.SelectedItem.ToString();

        }



        private void LimpiarTextBox()
        {
            pictureBox1.Image = null;
           // pictureBox1.Visible = false;
            txtMes_Inscripcion.Clear();
            txtCircunscripcion_Acta.Clear();
            txtNumero_Acta.Clear();
            txtAño_Inscripcion.Clear();
            txtAnio_Acta.Clear();
            txtTomo_Acta.Clear();
            txtDia_inscripcion.Clear();
            txtNombre_Nacido.Clear();
            txtSexo_Nacido.Clear();
            txtDia_Nacimiento.Clear();
            txtMes_Nacimiento.Clear();
            txtAño_Nacimiento.Clear();
            txtNombre_Padre.Clear();
            txtNombre_Madre.Clear();
            //  txtCircunscripcion_Acta.Focus();
        }




      

        private void RegistrarProduccionAccess(string pnuevonombre, string nombreantesdeeditar, int mesinscripcion)
        {
            Datos.ProduccionAccess oProduccionAccess = new Datos.ProduccionAccess();
            int copiaExitosa = 0;
            int grabacionExitosa = 0;

            string rutaLote = txtRutaLote.Text;
            string nombreLote = Path.GetFileName(rutaLote);
            string rutaOrigen = Path.Combine(rutaLote, txtNombreImagen.Text + ".pdf");
            string rutaDestino = Path.Combine(rutaverificada, $"{txtAnio_Acta.Text}-{txtCircunscripcion_Acta.Text}-{txtTomo_Acta.Text}-{txtNumero_Acta.Text}.pdf");

            try
            {
                // Copiar archivo si no existe en destino
                if (File.Exists(rutaOrigen))
                {
                    if (!File.Exists(rutaDestino))
                    {
                        File.Copy(rutaOrigen, rutaDestino);
                        copiaExitosa = 1;
                    }
                    else if (editar == 0)
                    {
                        string rutaDestinoDuplicado = rutaDestino.Replace(".pdf", "(2).pdf");
                        File.Copy(rutaOrigen, rutaDestinoDuplicado);
                        copiaExitosa = 1;
                    }
                }

                // Parámetros de la base de datos
                int diaInscripcion = Convert.ToInt32(txtDia_inscripcion.Text);
                int anioActa = Convert.ToInt32(txtAnio_Acta.Text);
                int numeroActa = Convert.ToInt32(txtNumero_Acta.Text);
                int anioInscripcion = Convert.ToInt32(txtAño_Inscripcion.Text);
               
                int diaNacimiento = Convert.ToInt32(txtDia_Nacimiento.Text);
                int anioNacimiento = Convert.ToInt32(txtAño_Nacimiento.Text);
                string rutaImagen = rutaOrigen;
                string pindexador = txtUsuario.Text.Replace("Usuario: ", "");

                // Eliminar apóstrofes en nombres para evitar errores en la base de datos
                string nombreMadre = txtNombre_Madre.Text.Replace("'", "");
                string nombrePadre = txtNombre_Padre.Text.Replace("'", "");
                string nombreNacido = txtNombre_Nacido.Text.Replace("'", "");

                if (editar == 1)
                {
                    oProduccionAccess.EditarActasVerificadasRC(txtCircunscripcion_Acta.Text, txtTomo_Acta.Text, diaInscripcion,
                        txtMes_Inscripcion.Text, anioActa, numeroActa, anioInscripcion, nombreNacido, txtCNI_Numero.Text,
                        txtSexo_Nacido.Text, diaNacimiento, txtMes_Nacimiento.Text, anioNacimiento, nombrePadre, nombreMadre,
                        nombreantesdeeditar + ".pdf");
                }
                else if (copiaExitosa == 1)
                {
                    oProduccionAccess.ActasNacimiento(
     txtCircunscripcion_Acta.Text, txtTomo_Acta.Text, numeroActa, anioActa,
     diaInscripcion, mesinscripcion, anioInscripcion, nombreNacido, txtCNI_Numero.Text, txtSexo_Nacido.Text,
     diaNacimiento, txtMes_Nacimiento.Text, anioNacimiento, nombrePadre, nombreMadre, rutaImagen,
     txtNombreImagen.Text + ".pdf", nombreLote, pindexador);


                    oProduccionAccess.ActasVerificadasRC(txtCircunscripcion_Acta.Text, txtTomo_Acta.Text, diaInscripcion,
                        txtMes_Inscripcion.Text, anioActa, numeroActa, anioInscripcion, nombreNacido, txtCNI_Numero.Text,
                        txtSexo_Nacido.Text, diaNacimiento, txtMes_Nacimiento.Text, anioNacimiento, nombrePadre,
                        nombreMadre, pnuevonombre + ".pdf");

                    grabacionExitosa = 1;
                }

                // Si la grabación fue exitosa o es una edición, eliminar el archivo original
                if (grabacionExitosa == 1 || editar == 1)
                {
                    if (File.Exists(rutaOrigen))
                    {
                        File.SetAttributes(rutaOrigen, FileAttributes.Normal);
                        int intentos = 0, maxIntentos = 10;
                        TimeSpan espera = TimeSpan.FromSeconds(1);

                        while (intentos < maxIntentos)
                        {
                            try
                            {
                                File.Delete(rutaOrigen);
                                break; // Eliminación exitosa, salir del bucle
                            }
                            catch (IOException)
                            {
                                System.Threading.Thread.Sleep(espera);
                                intentos++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en RegistrarProduccionAccess: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        // Método único para validar que solo se ingresen números
        private void ValidarSoloNumeros(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // Bloquea la entrada de caracteres no numéricos
            }
        }

        // Asignar el mismo evento a varios TextBox en el Load del formulario




        private int OriginalWidth = 911;  // Ancho original del PictureBox (ajustar según tu diseño)
        private int OriginalHeight = 564; // Alto original del PictureBox (ajustar según tu diseño)

        private async void ResaltarCampo(int x, int y, int width, int height)
        {
            limpiarrectangulo();
            await Task.Delay(100);

            // Guardar la última posición resaltada
            lastX = x;
            lastY = y;
            lastWidth = width;
            lastHeight = height;

            // Calcular la escala actual del PictureBox
            float scaleX = (float)pictureBox1.Width / OriginalWidth;
            float scaleY = (float)pictureBox1.Height / OriginalHeight;

            // Aplicar la escala a las coordenadas
            int scaledX = (int)(x * scaleX);
            int scaledY = (int)(y * scaleY);
            int scaledWidth = (int)(width * scaleX);
            int scaledHeight = (int)(height * scaleY);

            using (Graphics Lienzo = pictureBox1.CreateGraphics())
            {
                using (Brush brush = new SolidBrush(Color.FromArgb(60, 255, 255, 0)))
                {
                    Lienzo.FillRectangle(brush, scaledX, scaledY + pixelgriss, scaledWidth, scaledHeight);
                }
            }
        }
      
        private int lastX, lastY, lastWidth, lastHeight; // Última posición resaltada

        // Eventos Enter optimizados
        private void txtCircunscripcion_Acta_Enter(object sender, EventArgs e) => ResaltarCampo(226, 38, 150, 60);
        private void txtTomo_Acta_Enter(object sender, EventArgs e) => ResaltarCampo(383, 38, 150, 60);
        private void txtNumero_Acta_Enter(object sender, EventArgs e) => ResaltarCampo(600, 38, 150, 60);
        private void txtAnio_Acta_Enter(object sender, EventArgs e) => ResaltarCampo(720, 38, 150, 60);
        private void txtDia_inscripcion_Enter(object sender, EventArgs e) => ResaltarCampo(580, 60, 150, 60);
        private void txtMes_Inscripcion_Enter(object sender, EventArgs e) => ResaltarCampo(680, 60, 150, 60);
        private void txtAño_Inscripcion_Enter(object sender, EventArgs e) => ResaltarCampo(220, 80, 150, 60);
        private void txtNombre_Nacido_Enter(object sender, EventArgs e) => ResaltarCampo(210, 130, 600, 60);
        private void txtSexo_Nacido_Enter(object sender, EventArgs e) => ResaltarCampo(220, 150, 200, 60);
        private void txtDia_Nacimiento_Enter(object sender, EventArgs e) => ResaltarCampo(420, 150, 150, 60);
        private void txtMes_Nacimiento_Enter(object sender, EventArgs e) => ResaltarCampo(540, 150, 160, 60);
        private void txtAño_Nacimiento_Enter(object sender, EventArgs e) => ResaltarCampo(700, 150, 150, 60);
        private void txtNombre_Padre_Enter(object sender, EventArgs e) => ResaltarCampo(220, 210, 600, 60);
        private void txtNombre_Madre_Enter(object sender, EventArgs e) => ResaltarCampo(220, 250, 600, 60);

      
        // Método para limpiar el rectángulo sin afectar otros elementos
        private void limpiarrectangulo()
        {
            if (Lienzo != null)
            {
                pictureBox1.Refresh(); // Restablece la imagen sin necesidad de ocultar y mostrar
               

            }
            pictureBox1.Invalidate(); // Limpia el dibujo anterior
        }


        // Diccionario para la conversión de meses
        private readonly Dictionary<string, string> meses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "E", "ENERO" }, { "F", "FEBRERO" }, { "MAR", "MARZO" }, { "AB", "ABRIL" },
    { "MAY", "MAYO" }, { "JUN", "JUNIO" }, { "JUL", "JULIO" }, { "AG", "AGOSTO" },
    { "S", "SEPTIEMBRE" }, { "O", "OCTUBRE" }, { "N", "NOVIEMBRE" }, { "D", "DICIEMBRE" }
};

        // Diccionario para la conversión de sexo
        private readonly Dictionary<string, string> sexoDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "M", "MASCULINO" }, { "F", "FEMENINO" }
};

        // Método para autocompletar meses
        private void AutoCompletarMes(TextBox textBox)
        {
            string input = textBox.Text.ToUpper();
            if (string.IsNullOrWhiteSpace(input)) return;

            foreach (var mes in meses.Keys)
            {
                if (input.StartsWith(mes))
                {
                    textBox.Text = meses[mes];
                    return;
                }
            }
        }

        // Método para autocompletar sexo
        private void AutoCompletarSexo(TextBox textBox)
        {
            string input = textBox.Text.ToUpper();
            if (sexoDict.ContainsKey(input))
            {
                textBox.Text = sexoDict[input];
                textBox.SelectionStart = textBox.Text.Length;
            }
        }

        // Método para manejar Tab automático en KeyDown
        private void ManejarTabulacion(KeyEventArgs e, Control anterior, Control siguiente)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Down)
            {
                if (siguiente is Button boton)
                {
                    boton.PerformClick(); // Ejecuta el evento Click del botón automáticamente
                }
                else if (siguiente != null)
                {
                    siguiente.Focus(); // Mueve el foco al siguiente control
                }
            }
            else if (e.KeyCode == Keys.Up && anterior != null)
            {
                anterior.Focus();
            }
        }


        // Eventos de TextChanged optimizados
        private void txtMes_Nacimiento_TextChanged(object sender, EventArgs e) => AutoCompletarMes(txtMes_Nacimiento);
        private void txtMes_Inscripcion_TextChanged(object sender, EventArgs e) => AutoCompletarMes(txtMes_Inscripcion);
        private void txtSexo_Nacido_TextChanged(object sender, EventArgs e) => AutoCompletarSexo(txtSexo_Nacido);

        // Eventos de KeyDown optimizados
        private void txtCircunscripcion_Acta_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, null, txtTomo_Acta);
        private void txtTomo_Acta_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtCircunscripcion_Acta, txtNumero_Acta);
        private void txtNumero_Acta_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtTomo_Acta, txtAnio_Acta);
        private void txtAnio_Acta_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtNumero_Acta, txtDia_inscripcion);
        private void txtDia_inscripcion_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtAnio_Acta, txtMes_Inscripcion);
        private void txtMes_Inscripcion_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtDia_inscripcion, txtAño_Inscripcion);
        private void txtAño_Inscripcion_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtMes_Inscripcion, txtNombre_Nacido);
        private void txtNombre_Nacido_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtAño_Inscripcion, txtCNI_Numero);
        private void txtCNI_Numero_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtNombre_Nacido, txtSexo_Nacido);
        private void txtSexo_Nacido_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtCNI_Numero, txtDia_Nacimiento);
        private void txtDia_Nacimiento_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtSexo_Nacido, txtMes_Nacimiento);
        private void txtMes_Nacimiento_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtDia_Nacimiento, txtAño_Nacimiento);
        private void txtAño_Nacimiento_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtMes_Nacimiento, txtNombre_Padre);
        private void txtNombre_Padre_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtAño_Nacimiento, txtNombre_Madre);
        private void txtNombre_Madre_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtNombre_Padre, btnGrabar);
        private void btnGrabar_KeyDown(object sender, KeyEventArgs e) => ManejarTabulacion(e, txtNombre_Madre, null);


        private void txtCircunscripcion_Acta_KeyPress(object sender, KeyPressEventArgs e)
        {

        }
        private void txtNombre_Madre_TabStopChanged(object sender, EventArgs e)
        {
            btnGrabar.Focus();
        }

        private void txtCircunscripcion_Acta_MouseClick(object sender, MouseEventArgs e)
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



        private void CargarPDFEnPictureBox(string filePath)
        {
            try
            {
                using (var pdfDocument = PdfDocument.Load(filePath))
                {
                    if (pdfDocument.PageCount > 0)
                    {
                        Image pdfImage = pdfDocument.Render(0, pictureBox1.Width, pictureBox1.Height, 96, 96, true);
                        pictureBox1.Image = pdfImage;
                    }
                    else
                    {
                        MessageBox.Show("El PDF no contiene páginas.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al abrir el PDF: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtAnio_Acta_Leave(object sender, EventArgs e)
        {
            txtAño_Inscripcion.Text = txtAnio_Acta.Text;
            txtAño_Nacimiento.Text = txtAnio_Acta.Text;
        }

        //static FileInfo ObtenerPrimerArchivo(DirectoryInfo directorio)
        //{
        //    string primerArchivo = Directory.EnumerateFiles(directorio.FullName).FirstOrDefault();
        //    return primerArchivo != null ? new FileInfo(primerArchivo) : null;
        //}



        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("¿Desea pasar el Lote a Ilegible?", "Confirmación", MessageBoxButtons.OKCancel);

            if (result != DialogResult.OK)
                return;

            string origen = txtRutaLote.Text;
            string destinoBase = @"J:\BDs\RC\ILEGIBLE\ACTAS\";

            if (!Directory.Exists(origen))
            {
                MessageBox.Show("La carpeta de origen no existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string carpetaNombre = new DirectoryInfo(origen).Name;
                string destinoCompleto = Path.Combine(destinoBase, carpetaNombre);

                // Si es posible, mover el directorio en lugar de copiar y eliminar
                if (!Directory.Exists(destinoCompleto))
                {
                    Directory.Move(origen, destinoCompleto);
                }
                else
                {
                    // Si la carpeta ya existe, copiar archivos sin sobrescribir
                    CopyDirectory(origen, destinoCompleto);
                    Directory.Delete(origen, true);
                }

                // Verificar si el lote está vacío y cerrar lote si es necesario
                if (lboxIndice.Items.Count == 0 && !string.IsNullOrEmpty(txtRutaLote.Text))
                {
                    string pindexador = txtUsuario.Text;
                    ProduccionAccess oinsertar = new ProduccionAccess();
                    oinsertar.CerrarLoteAsignado(txtRutaLote.Text, pindexador.Replace("Usuario: ", ""));
                    txtRutaLote.Text = "";
                    btnSeleccionarLote.Enabled = true;
                }

                VaciarFormulario();
                MessageBox.Show("El lote se ha movido correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Error al mover el lote (archivo en uso): {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        static void CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (string file in Directory.EnumerateFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                if (!File.Exists(destFile))  // Evitar sobrescribir archivos ya existentes
                {
                    File.Copy(file, destFile);
                }
            }

            foreach (string subDir in Directory.EnumerateDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }


        private void FormActasDeNacimiento_Load(object sender, EventArgs e)
        {
            DataTable dt;
            //ObtenerDatos odatos = new ObtenerDatos();
            //dt = odatos.ConsultaActa1();
            try
            {
                using (var oCon = ConexionAccess.ObtenerConexion2())
                {
                    oCon.Open();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo establecer conexión con la base de datos. Por favor, asegúrese de estar logueado en FortiClient para comenzar con la indexación.", "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtNumero_Acta_Leave(object sender, EventArgs e)
        {
            if (txtNumero_Acta.Text.Length < 4)
            {
                txtNumero_Acta.Text = txtNumero_Acta.Text.PadLeft(4, '0');

            }
        }

        private void AjustarPictureBox()
        {
            // Ajustar el PictureBox proporcionalmente al tamaño del formulario
            float scaleX = (float)this.ClientSize.Width / OriginalWidth;
            float scaleY = (float)this.ClientSize.Height / OriginalHeight;

            pictureBox1.Width = (int)(OriginalWidth * scaleX);
            pictureBox1.Height = (int)(OriginalHeight * scaleY);
        }
        private void groupBox11_Enter(object sender, EventArgs e)
        {

        }

        private void FormActasDeNacimiento_Resize(object sender, EventArgs e)
        {
            AjustarPictureBox();

        }
    }
        }
    


    
    
    

