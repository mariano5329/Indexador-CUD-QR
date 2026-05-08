using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Indexador_GCBA_.Datos;
using Indexador_GCBA_.Servicios;
using PdfiumViewer;

namespace Indexador_GCBA_
{
    public partial class FormActasDeNacimiento : Form
    {
        private Entidades.Indexador oIndexador = new Entidades.Indexador();

        private Graphics Lienzo;
        private string RutaNombreImagen = string.Empty;

        private int usuariocoincide = 0;
        private int inicio = 0;
        private int pixelgriss = 0;
        private int editar = 0;

        // ==========================================================
        // RUTAS CUD
        // Ajustalas a tus carpetas reales cuando definas ambiente final.
        // ==========================================================
        private readonly string rutaCudProcesadas = @"J:\BDs\CUDproc\PROCESADAS\";
        private readonly string rutaCudVerificada = @"J:\BDs\CUD\CERTIFICADOS_VERIFICADOS\";
        private readonly string rutaCudIlegibles = @"J:\BDs\CUD\ILEGIBLES\";

        // ==========================================================
        // VALIDACIÓN / UX CUD
        // ==========================================================
        private readonly ErrorProvider _erroresCud = new ErrorProvider();
        private readonly ToolTip _toolTipCud = new ToolTip();

        private bool _normalizandoCud = false;
        private bool _cargandoDatosCud = false;

        private string _ultimoDniAuto = string.Empty;
        private string _ultimaEmisionAuto = string.Empty;
        private string _ultimoVencimientoAuto = string.Empty;

        private readonly Color _colorOk = SystemColors.Window;
        private readonly Color _colorError = Color.MistyRose;
        private readonly Color _colorAdvertencia = Color.LemonChiffon;
        private readonly Color _colorAuto = Color.FromArgb(226, 246, 255);

        // ==========================================================
        // PICTUREBOX / RESALTADO
        // ==========================================================
        private int OriginalWidth = 911;
        private int OriginalHeight = 564;

        private int lastX;
        private int lastY;
        private int lastWidth;
        private int lastHeight;
        private bool _autocompletandoCuil = false;
        private bool _sincronizandoCamposCud = false;

        public FormActasDeNacimiento(Entidades.Indexador pIndexador)
        {
            InitializeComponent();

            oIndexador = pIndexador;
            WindowState = FormWindowState.Maximized;

            DesHabilarBotones();

            ListEmbeddedResources();
            ExtractPdfium();

            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.SizeChanged += PictureBox1_SizeChanged;

            ConfigurarFormularioCud();
        }

        // ==========================================================
        // CONFIGURACIÓN GENERAL CUD
        // ==========================================================

        private void ConfigurarFormularioCud()
        {
            _erroresCud.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            _toolTipCud.AutoPopDelay = 8000;
            _toolTipCud.InitialDelay = 300;
            _toolTipCud.ReshowDelay = 100;

            DesvincularEventosViejosActas();
            ConfigurarCombosCud();
            ConfigurarEventosCud();
            ConfigurarOrdenTabulacionCud();
            RestaurarColoresFormularioCud();
        }

        private void DesvincularEventosViejosActas()
        {
            // Evita que eventos viejos del diseñador o del formulario anterior
            // se ejecuten junto con la lógica nueva de CUD.

            TxtNumeCert.KeyDown -= TxtNumeCert_KeyDown;
            TxtNumeCert.KeyPress -= TxtNumeCert_KeyPress;
            TxtNumeCert.MouseClick -= TxtNumeCert_MouseClick;

            TxtApellidoNombre.KeyDown -= txtTomo_Acta_KeyDown;
            CmbSexo.KeyDown -= txtNumero_Acta_KeyDown;
            TxtNumDNI.KeyDown -= txtAnio_Acta_KeyDown;
            TxtNumCuil.KeyDown -= txtDia_inscripcion_KeyDown;
            TxtFechaNac.KeyDown -= txtMes_Inscripcion_KeyDown;
            TxtDiagnostico.KeyDown -= txtAño_Inscripcion_KeyDown;
            TxtFuncionesCorporales.KeyDown -= txtNombre_Nacido_KeyDown;
            TxtEstructurasCorporales.KeyDown -= txtCNI_Numero_KeyDown;
            TxtActividad_Particip.KeyDown -= txtSexo_Nacido_KeyDown;
            TxtFactoresAmbientales.KeyDown -= txtDia_Nacimiento_KeyDown;
            TxtOrientacion.KeyDown -= txtMes_Nacimiento_KeyDown;
            TxtFechaVencimiento.KeyDown -= txtAño_Nacimiento_KeyDown;
            CmbAcomapante.KeyDown -= txtNombre_Padre_KeyDown;
            TxtFechaEmision.KeyDown -= txtNombre_Madre_KeyDown;

            TxtOrientacion.TextChanged -= txtMes_Nacimiento_TextChanged;
            TxtFechaNac.TextChanged -= txtMes_Inscripcion_TextChanged;
            TxtActividad_Particip.TextChanged -= txtSexo_Nacido_TextChanged;
        }

        private void ConfigurarCombosCud()
        {
            CmbSexo.DropDownStyle = ComboBoxStyle.DropDownList;
            CmbSexo.Items.Clear();
            CmbSexo.Items.AddRange(new object[]
            {
                "",
                "FEMENINO",
                "MASCULINO",
                "X",
                "NO ESPECIFICA"
            });
            CmbSexo.SelectedIndex = 0;

            CmbAcomapante.DropDownStyle = ComboBoxStyle.DropDownList;
            CmbAcomapante.Items.Clear();
            CmbAcomapante.Items.AddRange(new object[]
            {
                "",
                "SI",
                "NO"
            });
            CmbAcomapante.SelectedIndex = 0;
        }

        private void ConfigurarEventosCud()
        {
            // Escritura preventiva.
            TxtNumeCert.KeyPress += KeyPressNumeroCertificadoCud;
            TxtNumDNI.KeyPress += KeyPressSoloNumerosCud;
            TxtNumCuil.KeyPress += KeyPressSoloNumerosCud;

            TxtFechaNac.KeyPress += KeyPressFechaCud;
            TxtFechaEmision.KeyPress += KeyPressFechaCud;
            TxtFechaVencimiento.KeyPress += KeyPressFechaCud;

            TxtFuncionesCorporales.KeyPress += KeyPressCodigoCifCud;
            TxtEstructurasCorporales.KeyPress += KeyPressCodigoCifCud;
            TxtActividad_Particip.KeyPress += KeyPressCodigoCifCud;
            TxtFactoresAmbientales.KeyPress += KeyPressCodigoCifCud;

            // Pegado / OCR / normalización en vivo.
            TxtNumeCert.TextChanged += TxtNumeCert_TextChangedCud;

         
            TxtNumCuil.TextChanged += TxtNumCuil_TextChangedCud;

            TxtFuncionesCorporales.TextChanged += CodigoCif_TextChangedCud;
            TxtEstructurasCorporales.TextChanged += CodigoCif_TextChangedCud;
            TxtActividad_Particip.TextChanged += CodigoCif_TextChangedCud;
            TxtFactoresAmbientales.TextChanged += CodigoCif_TextChangedCud;
         

            // Validación al salir.
            TxtNumeCert.Leave += TxtNumeCert_LeaveCud;
            TxtApellidoNombre.Leave += TxtApellidoNombre_LeaveCud;
            TxtNumDNI.Leave += TxtNumDNI_LeaveCud;
            TxtNumCuil.Leave += TxtNumCuil_LeaveCud;
            TxtNumCuil.TextChanged += TxtNumCuil_TextChangedCud;


            TxtFechaNac.Leave += Fecha_LeaveCud;
            TxtFechaEmision.Leave += Fecha_LeaveCud;
            TxtFechaVencimiento.Leave += Fecha_LeaveCud;

            TxtDiagnostico.Leave += TextoLibre_LeaveCud;
            TxtOrientacion.Leave += TxtOrientacion_LeaveCud;

            TxtFuncionesCorporales.Leave += CodigoCif_LeaveCud;
            TxtEstructurasCorporales.Leave += CodigoCif_LeaveCud;
            TxtActividad_Particip.Leave += CodigoCif_LeaveCud;
            TxtFactoresAmbientales.Leave += CodigoCif_LeaveCud;

            CmbSexo.SelectedIndexChanged += Combo_ChangedCud;
            CmbAcomapante.SelectedIndexChanged += Combo_ChangedCud;

            // Navegación productiva.
            foreach (Control c in ObtenerControlesCargaCud())
            {
                c.KeyDown += ControlCarga_KeyDownCud;
            }

            btnGrabar.KeyDown += ControlCarga_KeyDownCud;
        }

        private void TxtNumCuil_TextChangedCud(object sender, EventArgs e)
        {
            if (_cargandoDatosCud || _normalizandoCud || _autocompletandoCuil || _sincronizandoCamposCud)
                return;

            string cuilActual = IndexadorCudService.NormalizarCuil(TxtNumCuil.Text);
            string dni = IndexadorCudService.NormalizarDni(TxtNumDNI.Text);

            // Solo autocompleta cuando el usuario escribió exactamente los 2 primeros dígitos.
            if (cuilActual.Length != 2)
                return;

            if (string.IsNullOrWhiteSpace(dni))
            {
                MarcarCampo(
                    TxtNumCuil,
                    ResultadoValidacion.Advertencia("Cargue primero el DNI para autocompletar el CUIL."));

                return;
            }

            if (dni.Length < 7 || dni.Length > 8)
            {
                MarcarCampo(
                    TxtNumCuil,
                    ResultadoValidacion.Advertencia("El DNI debe tener 7 u 8 dígitos para autocompletar el CUIL."));

                return;
            }

            string dni8 = dni.PadLeft(8, '0');
            string cuilBase = cuilActual + dni8;

            _autocompletandoCuil = true;

            TxtNumCuil.Text = cuilBase;
            TxtNumCuil.SelectionStart = TxtNumCuil.Text.Length;
            TxtNumCuil.SelectionLength = 0;

            _autocompletandoCuil = false;

            MarcarCampoAuto(
                TxtNumCuil,
                "CUIL autocompletado con el DNI. Ingrese el último dígito verificador.");
        }
        private void ActualizarNumeroCertificadoDesdeDni(string dni)
        {
            dni = IndexadorCudService.NormalizarDni(dni);

            if (dni.Length < 7 || dni.Length > 8)
                return;

            string numero = IndexadorCudService.NormalizarNumeroCertificado(TxtNumeCert.Text);

            if (string.IsNullOrWhiteSpace(numero))
                return;

            string[] partes = numero.Split('-');

            // Formato esperado:
            // ARG-02-00011132502-20231219-20321219-CBA-536
            if (partes.Length != 7)
                return;

            // Solo tocamos el segmento DNI, no reconstruimos todo.
            partes[2] = dni.PadLeft(11, '0');

            string nuevoNumero = string.Join("-", partes);

            TxtNumeCert.Text = nuevoNumero;
            _ultimoDniAuto = dni;

            MarcarCampoAuto(
                TxtNumeCert,
                "Número de certificado sincronizado con el DNI cargado.");
        }


        private void ConfigurarOrdenTabulacionCud()
        {
            TxtNumeCert.TabIndex = 0;
            TxtApellidoNombre.TabIndex = 1;
            CmbSexo.TabIndex = 2;
            TxtNumDNI.TabIndex = 3;
            TxtNumCuil.TabIndex = 4;
            TxtFechaNac.TabIndex = 5;
            TxtDiagnostico.TabIndex = 6;
            TxtFuncionesCorporales.TabIndex = 7;
            TxtEstructurasCorporales.TabIndex = 8;
            TxtActividad_Particip.TabIndex = 9;
            TxtFactoresAmbientales.TabIndex = 10;

            // ORDEN CORRECTO FINAL
            TxtOrientacion.TabIndex = 11;
            TxtFechaVencimiento.TabIndex = 12;
            CmbAcomapante.TabIndex = 13;
            TxtFechaEmision.TabIndex = 14;
            btnGrabar.TabIndex = 15;

            foreach (Control c in ObtenerControlesCargaCud())
            {
                c.TabStop = true;
            }

            btnGrabar.TabStop = true;
        }

        private List<Control> ObtenerControlesCargaCud()
        {
            return new List<Control>
    {
        TxtNumeCert,
        TxtApellidoNombre,
        CmbSexo,
        TxtNumDNI,
        TxtNumCuil,
        TxtFechaNac,
        TxtDiagnostico,
        TxtFuncionesCorporales,
        TxtEstructurasCorporales,
        TxtActividad_Particip,
        TxtFactoresAmbientales,

        // ORDEN CORRECTO FINAL
        TxtOrientacion,
        TxtFechaVencimiento,
        CmbAcomapante,
        TxtFechaEmision
    };
        }

        // ==========================================================
        // BOTONES
        // ==========================================================

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
                if (!Directory.Exists(rutaCudProcesadas))
                    return;

                DirectoryInfo di1 = new DirectoryInfo(rutaCudProcesadas);

                foreach (DirectoryInfo carpeta in di1.GetDirectories())
                {
                    string grupo = carpeta.Name;
                    if (string.IsNullOrWhiteSpace(grupo))
                        continue;

                    DirectoryInfo diGrupo = new DirectoryInfo(Path.Combine(rutaCudProcesadas, grupo));

                    foreach (DirectoryInfo loteDir in diGrupo.GetDirectories())
                    {
                        string lote = loteDir.Name;
                        if (string.IsNullOrWhiteSpace(lote))
                            continue;

                        string loteAsignado = loteasignado(lote, grupo);
                        if (string.IsNullOrWhiteSpace(loteAsignado))
                            continue;

                        string rutaLote = Path.Combine(rutaCudProcesadas, grupo, loteAsignado);

                        usuarioAsignado = new ObtenerDatos().Consultaloteasignado(rutaLote);

                        if (string.IsNullOrWhiteSpace(usuarioAsignado))
                        {
                            usuarioAsignado = txtUsuario.Text;
                            new ProduccionAccess().loteasignado(rutaLote, usuarioAsignado.Replace("Usuario: ", ""));
                        }

                        loteSeleccionado = rutaLote;
                        break;
                    }

                    if (!string.IsNullOrWhiteSpace(loteSeleccionado))
                        break;
                }
            });

            if (!string.IsNullOrWhiteSpace(loteSeleccionado))
            {
                txtRutaLote.Text = loteSeleccionado;
                CargarDocumentosEnSegundoPlano(loteSeleccionado);
            }
            else
            {
                btnSeleccionarLote.Enabled = true;
                MessageBox.Show("No se encontraron lotes CUD disponibles para indexar.", "Indexador CUD",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnGrabar_Click(object sender, EventArgs e)
        {
            if (!ValidarFormularioCudCompleto())
                return;

            CertificadoCudDto dto = ObtenerDtoCudDesdePantalla();
            GrabarCud(dto);
        }

        private void btnSepararIlegible_Click(object sender, EventArgs e)
        {
            if (lboxIndice.SelectedIndex == -1)
            {
                MessageBox.Show("Debe seleccionar un certificado antes de marcarlo como ILEGIBLE.",
                    "Indexador CUD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int numeroLineaActual = lboxIndice.SelectedIndex;
            int numeroLineaSiguiente = numeroLineaActual + 1;

            string lineaOriginal = txtNombreImagen.Text + ";";
            string nuevoNombreImagen = "ILEGIBLE";

            lboxIndice.Items.Insert(numeroLineaActual, lineaOriginal + nuevoNombreImagen);

            if (numeroLineaSiguiente < lboxIndice.Items.Count)
                lboxIndice.Items.RemoveAt(numeroLineaSiguiente);

            string rutaProcesada = txtRutaLote.Text;
            string rutaOrigenPdf = Path.Combine(rutaProcesada, txtNombreImagen.Text + ".pdf");

            Directory.CreateDirectory(rutaCudIlegibles);
            string destinoIlegible = Path.Combine(rutaCudIlegibles, txtNombreImagen.Text + ".pdf");

            try
            {
                if (File.Exists(rutaOrigenPdf) && !File.Exists(destinoIlegible))
                    File.Copy(rutaOrigenPdf, destinoIlegible);

                EliminarArchivoOrigenConReintentos(rutaOrigenPdf);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar el certificado ilegible: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            PasarAlSiguiente(numeroLineaActual, numeroLineaSiguiente, txtNombreImagen.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string pindexador = txtUsuario.Text;

            if (lboxIndice.Items.Count == 0 && !string.IsNullOrWhiteSpace(txtRutaLote.Text))
            {
                ProduccionAccess oinsertar = new ProduccionAccess();
                oinsertar.CerrarLoteAsignado(txtRutaLote.Text, pindexador.Replace("Usuario: ", ""));

                txtRutaLote.Text = string.Empty;
                btnSeleccionarLote.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("¿Desea pasar el lote completo a ILEGIBLE?",
                "Confirmación", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

            if (result != DialogResult.OK)
                return;

            string origen = txtRutaLote.Text;

            if (!Directory.Exists(origen))
            {
                MessageBox.Show("La carpeta de origen no existe.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(rutaCudIlegibles);

                string carpetaNombre = new DirectoryInfo(origen).Name;
                string destinoCompleto = Path.Combine(rutaCudIlegibles, carpetaNombre);

                if (!Directory.Exists(destinoCompleto))
                {
                    Directory.Move(origen, destinoCompleto);
                }
                else
                {
                    CopyDirectory(origen, destinoCompleto);
                    Directory.Delete(origen, true);
                }

                if (lboxIndice.Items.Count == 0 && !string.IsNullOrWhiteSpace(txtRutaLote.Text))
                {
                    string pindexador = txtUsuario.Text;
                    ProduccionAccess oinsertar = new ProduccionAccess();
                    oinsertar.CerrarLoteAsignado(txtRutaLote.Text, pindexador.Replace("Usuario: ", ""));

                    txtRutaLote.Text = string.Empty;
                    btnSeleccionarLote.Enabled = true;
                }

                VaciarFormulario();
                MessageBox.Show("El lote se movió correctamente a ILEGIBLE.", "Indexador CUD",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Error al mover el lote. Puede haber archivos en uso: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inesperado: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            Close();
        }

        // ==========================================================
        // LOTES / DOCUMENTOS
        // ==========================================================

        private async void CargarDocumentosEnSegundoPlano(string rutaLote)
        {
            if (!Directory.Exists(rutaLote))
            {
                MessageBox.Show("La carpeta del lote no existe.", "Indexador CUD",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DirectoryInfo dilo = new DirectoryInfo(rutaLote);
            ObtenerDatos oconsulta = new ObtenerDatos();
            bool primerElemento = true;

            foreach (FileInfo file in dilo.GetFiles("*.pdf"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file.Name);

                if (usuariocoincide == 1)
                {
                    int verificado = await Task.Run(() => oconsulta.Consultaactaverificada(file.FullName));

                    if (verificado != 0)
                        continue;
                }

                Invoke(new Action(() =>
                {
                    lboxIndice.Items.Add(fileName);

                    if (primerElemento)
                    {
                        lboxIndice.SelectedIndex = 0;
                        CargarIndiceFijoEImagen();
                        primerElemento = false;
                    }
                }));
            }

            Invoke(new Action(() =>
            {
                if (lboxIndice.Items.Count == 0)
                {
                    btnSeleccionarLote.Enabled = true;
                    MessageBox.Show("El lote no contiene certificados pendientes.", "Indexador CUD",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }));
        }

        private string loteasignado(string lote, string grupo)
        {
            usuariocoincide = 0;

            string rutaGrupo = Path.Combine(rutaCudProcesadas, grupo);
            string rutaLote = Path.Combine(rutaGrupo, lote);

            if (!Directory.Exists(rutaGrupo))
                return string.Empty;

            ObtenerDatos oconsulta = new ObtenerDatos();
            string consulta = oconsulta.Consultaloteasignado(rutaLote);
            string pindexador = txtUsuario.Text.Replace("Usuario: ", "");

            if (!string.IsNullOrWhiteSpace(consulta))
            {
                if (consulta == pindexador)
                {
                    usuariocoincide = 1;
                    return lote;
                }

                return string.Empty;
            }

            return lote;
        }

        private void CargarIndiceFijoEImagen()
        {
            editar = 0;
            CargarDatosFijosIndice();

            string nombreIndice = txtNombreImagen.Text;

            if (nombreIndice.Contains(";"))
            {
                txtNombreImagen.Text = nombreIndice.Substring(nombreIndice.IndexOf(';') + 1);
                RutaNombreImagen = Path.Combine(rutaCudVerificada, txtNombreImagen.Text + ".pdf");
                editar = 1;
            }
            else
            {
                RutaNombreImagen = Path.Combine(txtRutaLote.Text, txtNombreImagen.Text + ".pdf");
            }

            inicio = 1;
            CargarFicha(RutaNombreImagen);
            TxtNumeCert.Focus();
        }

        private void CargarDatosFijosIndice()
        {
            if (lboxIndice.SelectedItem != null)
                txtNombreImagen.Text = lboxIndice.SelectedItem.ToString();
        }

        private void CargarFicha(string ruta)
        {
            CargarPDFEnPictureBox(ruta);
            HabilitarBotones();
        }

        private void lboxIndice_MouseClick(object sender, MouseEventArgs e)
        {
            if (lboxIndice.SelectedIndex == -1)
                return;

            LimpiarTextBox();
            CargarIndiceFijoEImagen();

            // Edición CUD:
            // Cuando tengas el método real en ObtenerDatos, cargamos desde BD acá.
            // Ejemplo futuro:
            // CargarDatosCudEditado(txtNombreImagen.Text + ".pdf");
        }

        private void PasarAlSiguiente(int numeroLineaActual, int numeroLineaSiguiente, string nuevoNombre)
        {
            pictureBox1.Image = null;
            LimpiarTextBox();
            inicio = 0;

            string rutaProcesada = txtRutaLote.Text;

            if (numeroLineaActual + 1 >= lboxIndice.Items.Count)
            {
                VaciarFormulario();
                DesHabilarBotones();
                pictureBox1.Image = null;

                MessageBox.Show("El lote está terminado.", "Indexador CUD",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                IntentarEliminarCarpetaVacia(rutaProcesada);
                return;
            }

            if (numeroLineaSiguiente < lboxIndice.Items.Count)
            {
                lboxIndice.SetSelected(numeroLineaSiguiente, true);
                CargarIndiceFijoEImagen();
            }
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

        // ==========================================================
        // VALIDACIONES DE ESCRITURA
        // ==========================================================

        private void KeyPressSoloNumerosCud(object sender, KeyPressEventArgs e)
        {
            if (!IndexadorCudService.CaracterPermitidoNumerico(e.KeyChar))
                e.Handled = true;
        }

        private void KeyPressNumeroCertificadoCud(object sender, KeyPressEventArgs e)
        {
            if (!IndexadorCudService.CaracterPermitidoNumeroCertificado(e.KeyChar))
                e.Handled = true;
        }

        private void KeyPressFechaCud(object sender, KeyPressEventArgs e)
        {
            if (!IndexadorCudService.CaracterPermitidoFecha(e.KeyChar))
                e.Handled = true;
        }

        private void KeyPressCodigoCifCud(object sender, KeyPressEventArgs e)
        {
            if (!IndexadorCudService.CaracterPermitidoCif(e.KeyChar))
                e.Handled = true;
        }

        private void TxtNumeCert_TextChangedCud(object sender, EventArgs e)
        {
            if (_cargandoDatosCud || _normalizandoCud || _sincronizandoCamposCud)
                return;

            DatosNumeroCertificadoCud datos = IndexadorCudService.ParseNumeroCertificado(TxtNumeCert.Text);

            if (datos.EsValido)
            {
                MarcarCampo(TxtNumeCert, ResultadoValidacion.Ok("Número de certificado válido. Al salir del campo se sincronizarán DNI, emisión y vencimiento."));
            }
            else
            {
                _erroresCud.SetError(TxtNumeCert, string.Empty);
            }
        }

        private void CodigoCif_TextChangedCud(object sender, EventArgs e)
        {
            if (_cargandoDatosCud || _normalizandoCud)
                return;

            TextBox txt = sender as TextBox;
            if (txt == null)
                return;

            string original = txt.Text;
            string limpio = FiltrarCaracteresCif(original);

            if (original == limpio)
                return;

            int pos = txt.SelectionStart;

            _normalizandoCud = true;
            txt.Text = limpio;
            txt.SelectionStart = Math.Min(pos, txt.Text.Length);
            _normalizandoCud = false;
        }

        private string FiltrarCaracteresCif(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return string.Empty;

            List<char> chars = new List<char>();

            foreach (char c in texto)
            {
                if (IndexadorCudService.CaracterPermitidoCif(c) ||
                    c == ',' || c == ';' || c == '|' ||
                    c == '\r' || c == '\n' || c == '\t')
                {
                    chars.Add(c);
                }
            }

            return new string(chars.ToArray());
        }

        // ==========================================================
        // VALIDACIONES AL SALIR
        // ==========================================================

        private void TxtNumeCert_LeaveCud(object sender, EventArgs e)
        {
            _normalizandoCud = true;
            TxtNumeCert.Text = IndexadorCudService.NormalizarNumeroCertificado(TxtNumeCert.Text);
            _normalizandoCud = false;

            DatosNumeroCertificadoCud datos = IndexadorCudService.ParseNumeroCertificado(TxtNumeCert.Text);

            if (!datos.EsValido)
            {
                MarcarCampo(TxtNumeCert, ResultadoValidacion.Error(datos.Mensaje));
                return;
            }

            // El certificado es el dato maestro:
            // actualiza SIEMPRE DNI, Fecha de emisión y Fecha de vencimiento.
            AplicarTriangulacionCertificadoForzada(datos);

            MarcarCampo(TxtNumeCert, ResultadoValidacion.Ok("Número de certificado válido. Datos dependientes sincronizados."));
            ValidarFechasCruzadasEnPantalla();
        }

        private void AplicarTriangulacionCertificadoForzada(DatosNumeroCertificadoCud datos)
        {
            if (datos == null || !datos.EsValido)
                return;

            _sincronizandoCamposCud = true;

            TxtNumDNI.Text = datos.Dni;
            TxtFechaEmision.Text = datos.FechaEmision;
            TxtFechaVencimiento.Text = datos.FechaVencimiento;

            _ultimoDniAuto = datos.Dni;
            _ultimaEmisionAuto = datos.FechaEmision;
            _ultimoVencimientoAuto = datos.FechaVencimiento;

            // Como el DNI cambió por certificado, también actualizamos el CUIL si ya tenía prefijo o valor.
            ActualizarCuilDesdeDni(datos.Dni);

            _sincronizandoCamposCud = false;

            MarcarCampoAuto(TxtNumDNI, "DNI sincronizado desde número de certificado.");
            MarcarCampoAuto(TxtFechaEmision, "Fecha de emisión sincronizada desde número de certificado.");
            MarcarCampoAuto(TxtFechaVencimiento, "Fecha de vencimiento sincronizada desde número de certificado.");

            if (!string.IsNullOrWhiteSpace(TxtNumCuil.Text))
            {
                MarcarCampo(
                    TxtNumCuil,
                    IndexadorCudService.ValidarCuil(TxtNumCuil.Text, TxtNumDNI.Text, true));
            }
        }
        private void TxtNumDNI_TextChangedCud(object sender, EventArgs e)
        {
            if (_cargandoDatosCud || _normalizandoCud || _sincronizandoCamposCud)
                return;

            string dni = IndexadorCudService.NormalizarDni(TxtNumDNI.Text);

            // Si el DNI todavía no tiene longitud razonable, no tocamos nada.
            if (dni.Length < 7 || dni.Length > 8)
                return;

            _sincronizandoCamposCud = true;

            // DNI controla CUIL.
            ActualizarCuilDesdeDni(dni);

            _sincronizandoCamposCud = false;
        }



        private void TxtApellidoNombre_LeaveCud(object sender, EventArgs e)
        {
            TxtApellidoNombre.Text = IndexadorCudService.NormalizarNombreApellido(TxtApellidoNombre.Text);
            MarcarCampo(TxtApellidoNombre,
                IndexadorCudService.ValidarObligatorio(TxtApellidoNombre.Text, "Apellido y nombre"));
        }

        private void TxtNumDNI_LeaveCud(object sender, EventArgs e)
        {
            TxtNumDNI.Text = IndexadorCudService.NormalizarDni(TxtNumDNI.Text);

            ResultadoValidacion resultadoDni = IndexadorCudService.ValidarDni(TxtNumDNI.Text, true);
            MarcarCampo(TxtNumDNI, resultadoDni);

            if (!resultadoDni.EsValido)
                return;

            _sincronizandoCamposCud = true;

            // DNI controla CUIL.
            ActualizarCuilDesdeDni(TxtNumDNI.Text);

            // DNI puede actualizar el segmento DNI del número de certificado.
            ActualizarNumeroCertificadoDesdeDni(TxtNumDNI.Text);

            _sincronizandoCamposCud = false;

            if (!string.IsNullOrWhiteSpace(TxtNumCuil.Text))
            {
                MarcarCampo(
                    TxtNumCuil,
                    IndexadorCudService.ValidarCuil(TxtNumCuil.Text, TxtNumDNI.Text, true));
            }
        }

        private void ActualizarCuilDesdeDni(string dni)
        {
            dni = IndexadorCudService.NormalizarDni(dni);

            if (dni.Length < 7 || dni.Length > 8)
                return;

            string cuilActual = IndexadorCudService.NormalizarCuil(TxtNumCuil.Text);

            // Si no hay CUIL ni prefijo, no inventamos 20/27.
            // El indexador debe cargar al menos los dos primeros dígitos.
            if (string.IsNullOrWhiteSpace(cuilActual))
                return;

            if (cuilActual.Length < 2)
                return;

            string prefijo = cuilActual.Substring(0, 2);
            string dni8 = dni.PadLeft(8, '0');

            string digitoFinal = string.Empty;

            // Si ya había CUIL completo, conservamos el último dígito.
            if (cuilActual.Length >= 11)
                digitoFinal = cuilActual.Substring(10, 1);

            string nuevoCuil = prefijo + dni8 + digitoFinal;

            TxtNumCuil.Text = nuevoCuil;
            TxtNumCuil.SelectionStart = TxtNumCuil.Text.Length;
            TxtNumCuil.SelectionLength = 0;

            MarcarCampoAuto(
                TxtNumCuil,
                digitoFinal == string.Empty
                    ? "CUIL sincronizado con el DNI. Ingrese el último dígito verificador."
                    : "CUIL sincronizado con el DNI. Se conservó el último dígito.");
        }



        private void TxtNumCuil_LeaveCud(object sender, EventArgs e)
        {
            TxtNumCuil.Text = IndexadorCudService.NormalizarCuil(TxtNumCuil.Text);

            MarcarCampo(TxtNumCuil,
                IndexadorCudService.ValidarCuil(TxtNumCuil.Text, TxtNumDNI.Text, true));
        }

        private void Fecha_LeaveCud(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt == null)
                return;

            txt.Text = IndexadorCudService.NormalizarFecha(txt.Text);

            if (txt == TxtFechaNac)
            {
                MarcarCampo(txt,
                    IndexadorCudService.ValidarFecha(
                        txt.Text,
                        "Fecha de nacimiento",
                        true,
                        false,
                        new DateTime(1900, 1, 1),
                        DateTime.Today));
            }
            else if (txt == TxtFechaEmision)
            {
                MarcarCampo(txt,
                    IndexadorCudService.ValidarFecha(
                        txt.Text,
                        "Fecha de emisión",
                        true,
                        false,
                        new DateTime(1900, 1, 1),
                        DateTime.Today.AddDays(1)));
            }
            else if (txt == TxtFechaVencimiento)
            {
                MarcarCampo(txt,
                    IndexadorCudService.ValidarFecha(
                        txt.Text,
                        "Fecha de vencimiento",
                        true,
                        true,
                        new DateTime(1900, 1, 1),
                        null));
            }

            ValidarFechasCruzadasEnPantalla();
        }

        private void TextoLibre_LeaveCud(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt == null)
                return;

            txt.Text = IndexadorCudService.NormalizarTextoLibre(txt.Text);

            string nombreCampo = txt == TxtDiagnostico ? "Diagnóstico" : "Campo";
            MarcarCampo(txt, IndexadorCudService.ValidarObligatorio(txt.Text, nombreCampo));
        }

        private void TxtOrientacion_LeaveCud(object sender, EventArgs e)
        {
            TxtOrientacion.Text = IndexadorCudService.NormalizarOrientacion(TxtOrientacion.Text);

            MarcarCampo(TxtOrientacion,
                IndexadorCudService.ValidarObligatorio(TxtOrientacion.Text, "Orientación prestacional"));
        }

        private void CodigoCif_LeaveCud(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt == null)
                return;

            txt.Text = IndexadorCudService.NormalizarCodigosCif(txt.Text);
            MarcarCampo(txt, ValidarCodigoCifPorCampo(txt));
        }

        private void Combo_ChangedCud(object sender, EventArgs e)
        {
            if (_cargandoDatosCud)
                return;

            ComboBox cmb = sender as ComboBox;
            if (cmb == null)
                return;

            string nombreCampo = cmb == CmbSexo ? "Sexo" : "Acompañante";
            MarcarCampo(cmb, IndexadorCudService.ValidarObligatorio(cmb.Text, nombreCampo));
        }

        // ==========================================================
        // TRIANGULACIÓN DESDE NÚMERO DE CERTIFICADO
        // ==========================================================

        private void AplicarTriangulacionCertificado(DatosNumeroCertificadoCud datos, bool mostrarAdvertencias)
        {
            if (datos == null || !datos.EsValido)
                return;

            AutocompletarDniDesdeCertificado(datos.Dni, mostrarAdvertencias);

            AutocompletarFechaDesdeCertificado(
                TxtFechaEmision,
                datos.FechaEmision,
                ref _ultimaEmisionAuto,
                "emisión",
                mostrarAdvertencias);

            AutocompletarFechaDesdeCertificado(
                TxtFechaVencimiento,
                datos.FechaVencimiento,
                ref _ultimoVencimientoAuto,
                "vencimiento",
                mostrarAdvertencias);
        }

        private void AutocompletarDniDesdeCertificado(string dniCertificado, bool mostrarAdvertencias)
        {
            string actual = IndexadorCudService.NormalizarDni(TxtNumDNI.Text);

            if (string.IsNullOrWhiteSpace(actual) || actual == _ultimoDniAuto)
            {
                TxtNumDNI.Text = dniCertificado;
                _ultimoDniAuto = dniCertificado;
                MarcarCampoAuto(TxtNumDNI, "Autocompletado desde número de certificado.");
                return;
            }

            if (actual != dniCertificado && mostrarAdvertencias)
            {
                MarcarCampo(TxtNumDNI,
                    ResultadoValidacion.Advertencia("El DNI no coincide con el DNI incluido en el número de certificado."));
            }
        }

        private void AutocompletarFechaDesdeCertificado(
            TextBox txt,
            string fechaCertificado,
            ref string ultimoAuto,
            string nombre,
            bool mostrarAdvertencias)
        {
            string actual = IndexadorCudService.NormalizarFecha(txt.Text);

            if (string.IsNullOrWhiteSpace(actual) || actual == ultimoAuto)
            {
                txt.Text = fechaCertificado;
                ultimoAuto = fechaCertificado;
                MarcarCampoAuto(txt, "Fecha de " + nombre + " autocompletada desde número de certificado.");
                return;
            }

            if (actual != fechaCertificado && mostrarAdvertencias)
            {
                MarcarCampo(txt,
                    ResultadoValidacion.Advertencia("La fecha de " + nombre + " no coincide con la incluida en el número de certificado."));
            }
        }

        // ==========================================================
        // VALIDACIÓN FINAL
        // ==========================================================

        private bool ValidarFormularioCudCompleto()
        {
            NormalizarFormularioCudCompleto();

            Control primerError = null;
            bool valido = true;

            valido &= ValidarYMarcar(TxtNumeCert, ValidarNumeroCertificadoPantalla(), ref primerError);
            valido &= ValidarYMarcar(TxtApellidoNombre, IndexadorCudService.ValidarObligatorio(TxtApellidoNombre.Text, "Apellido y nombre"), ref primerError);
            valido &= ValidarYMarcar(CmbSexo, IndexadorCudService.ValidarObligatorio(CmbSexo.Text, "Sexo"), ref primerError);
            valido &= ValidarYMarcar(TxtNumDNI, IndexadorCudService.ValidarDni(TxtNumDNI.Text, true), ref primerError);
            valido &= ValidarYMarcar(TxtNumCuil, IndexadorCudService.ValidarCuil(TxtNumCuil.Text, TxtNumDNI.Text, true), ref primerError);

            valido &= ValidarYMarcar(
                TxtFechaNac,
                IndexadorCudService.ValidarFecha(TxtFechaNac.Text, "Fecha de nacimiento", true, false, new DateTime(1900, 1, 1), DateTime.Today),
                ref primerError);

            valido &= ValidarYMarcar(TxtDiagnostico, IndexadorCudService.ValidarObligatorio(TxtDiagnostico.Text, "Diagnóstico"), ref primerError);
            valido &= ValidarYMarcar(TxtFuncionesCorporales, ValidarCodigoCifPorCampo(TxtFuncionesCorporales), ref primerError);
            valido &= ValidarYMarcar(TxtEstructurasCorporales, ValidarCodigoCifPorCampo(TxtEstructurasCorporales), ref primerError);
            valido &= ValidarYMarcar(TxtActividad_Particip, ValidarCodigoCifPorCampo(TxtActividad_Particip), ref primerError);
            valido &= ValidarYMarcar(TxtFactoresAmbientales, ValidarCodigoCifPorCampo(TxtFactoresAmbientales), ref primerError);
            valido &= ValidarYMarcar(TxtOrientacion, IndexadorCudService.ValidarObligatorio(TxtOrientacion.Text, "Orientación prestacional"), ref primerError);

            valido &= ValidarYMarcar(
                TxtFechaEmision,
                IndexadorCudService.ValidarFecha(TxtFechaEmision.Text, "Fecha de emisión", true, false, new DateTime(1900, 1, 1), DateTime.Today.AddDays(1)),
                ref primerError);

            valido &= ValidarYMarcar(
                TxtFechaVencimiento,
                IndexadorCudService.ValidarFecha(TxtFechaVencimiento.Text, "Fecha de vencimiento", true, true, new DateTime(1900, 1, 1), null),
                ref primerError);

            valido &= ValidarYMarcar(CmbAcomapante, IndexadorCudService.ValidarObligatorio(CmbAcomapante.Text, "Acompañante"), ref primerError);

            ResultadoValidacion fechas = IndexadorCudService.ValidarFechasCruzadas(
                TxtFechaNac.Text,
                TxtFechaEmision.Text,
                TxtFechaVencimiento.Text);

            if (!fechas.EsValido)
            {
                valido = false;
                MarcarCampo(TxtFechaEmision, fechas);
                MarcarCampo(TxtFechaVencimiento, fechas);

                if (primerError == null)
                    primerError = TxtFechaEmision;
            }

            if (!valido)
            {
                if (primerError != null)
                    primerError.Focus();

                MessageBox.Show("Hay campos obligatorios o inconsistentes. Revise los campos marcados.",
                    "Validación CUD", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return false;
            }

            return true;
        }

        private void NormalizarFormularioCudCompleto()
        {
            _normalizandoCud = true;

            TxtNumeCert.Text = IndexadorCudService.NormalizarNumeroCertificado(TxtNumeCert.Text);
            TxtApellidoNombre.Text = IndexadorCudService.NormalizarNombreApellido(TxtApellidoNombre.Text);
            TxtNumDNI.Text = IndexadorCudService.NormalizarDni(TxtNumDNI.Text);
            TxtNumCuil.Text = IndexadorCudService.NormalizarCuil(TxtNumCuil.Text);

            TxtFechaNac.Text = IndexadorCudService.NormalizarFecha(TxtFechaNac.Text);
            TxtFechaEmision.Text = IndexadorCudService.NormalizarFecha(TxtFechaEmision.Text);
            TxtFechaVencimiento.Text = IndexadorCudService.NormalizarFecha(TxtFechaVencimiento.Text);

            TxtDiagnostico.Text = IndexadorCudService.NormalizarTextoLibre(TxtDiagnostico.Text);
            TxtFuncionesCorporales.Text = IndexadorCudService.NormalizarCodigosCif(TxtFuncionesCorporales.Text);
            TxtEstructurasCorporales.Text = IndexadorCudService.NormalizarCodigosCif(TxtEstructurasCorporales.Text);
            TxtActividad_Particip.Text = IndexadorCudService.NormalizarCodigosCif(TxtActividad_Particip.Text);
            TxtFactoresAmbientales.Text = IndexadorCudService.NormalizarCodigosCif(TxtFactoresAmbientales.Text);
            TxtOrientacion.Text = IndexadorCudService.NormalizarOrientacion(TxtOrientacion.Text);

            CmbSexo.Text = IndexadorCudService.NormalizarSexo(CmbSexo.Text);
            CmbAcomapante.Text = IndexadorCudService.NormalizarSiNo(CmbAcomapante.Text);

            _normalizandoCud = false;
        }

        private ResultadoValidacion ValidarNumeroCertificadoPantalla()
        {
            DatosNumeroCertificadoCud datos = IndexadorCudService.ParseNumeroCertificado(TxtNumeCert.Text);
            return datos.EsValido
                ? ResultadoValidacion.Ok("Número de certificado válido.")
                : ResultadoValidacion.Error(datos.Mensaje);
        }

        private ResultadoValidacion ValidarCodigoCifPorCampo(TextBox txt)
        {
            if (txt == TxtFuncionesCorporales)
                return IndexadorCudService.ValidarCodigosCif(txt.Text, 'b', "Funciones corporales", true);

            if (txt == TxtEstructurasCorporales)
                return IndexadorCudService.ValidarCodigosCif(txt.Text, 's', "Estructuras corporales", true);

            if (txt == TxtActividad_Particip)
                return IndexadorCudService.ValidarCodigosCif(txt.Text, 'd', "Actividad y participación", true);

            if (txt == TxtFactoresAmbientales)
                return IndexadorCudService.ValidarCodigosCif(txt.Text, 'e', "Factores ambientales", true);

            return ResultadoValidacion.Ok();
        }

        private void ValidarFechasCruzadasEnPantalla()
        {
            ResultadoValidacion r = IndexadorCudService.ValidarFechasCruzadas(
                TxtFechaNac.Text,
                TxtFechaEmision.Text,
                TxtFechaVencimiento.Text);

            if (!r.EsValido)
            {
                MarcarCampo(TxtFechaEmision, r);
                MarcarCampo(TxtFechaVencimiento, r);
            }
        }

        private bool ValidarYMarcar(Control control, ResultadoValidacion resultado, ref Control primerError)
        {
            MarcarCampo(control, resultado);

            if (resultado.Severidad == SeveridadValidacion.Error)
            {
                if (primerError == null)
                    primerError = control;

                return false;
            }

            return true;
        }

        private void MarcarCampo(Control control, ResultadoValidacion resultado)
        {
            if (control == null || resultado == null)
                return;

            if (resultado.Severidad == SeveridadValidacion.Error)
            {
                control.BackColor = _colorError;
                _erroresCud.SetError(control, resultado.Mensaje);
                _toolTipCud.SetToolTip(control, resultado.Mensaje);
            }
            else if (resultado.Severidad == SeveridadValidacion.Advertencia)
            {
                control.BackColor = _colorAdvertencia;
                _erroresCud.SetError(control, resultado.Mensaje);
                _toolTipCud.SetToolTip(control, resultado.Mensaje);
            }
            else
            {
                control.BackColor = _colorOk;
                _erroresCud.SetError(control, string.Empty);
                _toolTipCud.SetToolTip(control, string.Empty);
            }
        }

        private void MarcarCampoAuto(Control control, string mensaje)
        {
            if (control == null)
                return;

            control.BackColor = _colorAuto;
            _erroresCud.SetError(control, string.Empty);
            _toolTipCud.SetToolTip(control, mensaje);
        }

        private void RestaurarColoresFormularioCud()
        {
            foreach (Control c in ObtenerControlesCargaCud())
            {
                c.BackColor = _colorOk;
                _erroresCud.SetError(c, string.Empty);
                _toolTipCud.SetToolTip(c, string.Empty);
            }
        }

        // ==========================================================
        // DTO / GRABACIÓN
        // ==========================================================

        private CertificadoCudDto ObtenerDtoCudDesdePantalla()
        {
            return new CertificadoCudDto
            {
                NumeroCertificado = TxtNumeCert.Text.Trim(),
                ApellidoNombre = TxtApellidoNombre.Text.Trim(),
                Sexo = CmbSexo.Text.Trim(),
                DNI = TxtNumDNI.Text.Trim(),
                CUIL = TxtNumCuil.Text.Trim(),
                FechaNacimiento = TxtFechaNac.Text.Trim(),
                Diagnostico = TxtDiagnostico.Text.Trim(),
                FuncionesCorporales = TxtFuncionesCorporales.Text.Trim(),
                EstructurasCorporales = TxtEstructurasCorporales.Text.Trim(),
                ActividadParticipacion = TxtActividad_Particip.Text.Trim(),
                FactoresAmbientales = TxtFactoresAmbientales.Text.Trim(),
                OrientacionPrestacional = TxtOrientacion.Text.Trim(),
                FechaEmision = TxtFechaEmision.Text.Trim(),
                FechaVencimiento = TxtFechaVencimiento.Text.Trim(),
                Acompaniante = CmbAcomapante.Text.Trim()
            };
        }

        private void GrabarCud(CertificadoCudDto dto)
        {
            if (lboxIndice.SelectedIndex == -1)
            {
                MessageBox.Show("Debe seleccionar un certificado antes de grabar.",
                    "Indexador CUD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int numeroLineaActual = lboxIndice.SelectedIndex;
            int numeroLineaSiguiente = numeroLineaActual + 1;

            string lineaOriginal = txtNombreImagen.Text + ";";
            string nombreAntesDeEditar = txtNombreImagen.Text;
            string nuevoNombreImagen = CrearNombreArchivoCud(dto);

            lboxIndice.Items.Insert(numeroLineaActual, lineaOriginal + nuevoNombreImagen);

            if (numeroLineaSiguiente < lboxIndice.Items.Count)
                lboxIndice.Items.RemoveAt(numeroLineaSiguiente);

            RegistrarProduccionAccessCud(dto, nuevoNombreImagen, nombreAntesDeEditar);

            RestaurarColoresFormularioCud();
            PasarAlSiguiente(numeroLineaActual, numeroLineaSiguiente, nuevoNombreImagen);
        }

        private string CrearNombreArchivoCud(CertificadoCudDto dto)
        {
            string dni = IndexadorCudService.SanitizarNombreArchivo(dto.DNI);
            string cert = IndexadorCudService.SanitizarNombreArchivo(dto.NumeroCertificado);
            string nombre = IndexadorCudService.SanitizarNombreArchivo(dto.ApellidoNombre);

            string archivo = dni + "-" + cert + "-" + nombre;

            if (archivo.Length > 180)
                archivo = archivo.Substring(0, 180).Trim();

            return archivo;
        }

        private void RegistrarProduccionAccessCud(CertificadoCudDto dto, string nuevoNombreImagen, string nombreAntesDeEditar)
        {
            int copiaExitosa = 0;
            int grabacionExitosa = 0;

            string rutaLote = txtRutaLote.Text;
            string nombreLote = Path.GetFileName(rutaLote);
            string rutaOrigen = Path.Combine(rutaLote, txtNombreImagen.Text + ".pdf");
            string rutaDestino = Path.Combine(rutaCudVerificada, nuevoNombreImagen + ".pdf");
            string indexador = txtUsuario.Text.Replace("Usuario: ", "");

            try
            {
                Directory.CreateDirectory(rutaCudVerificada);

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

                if (editar == 1)
                {
                    // ======================================================
                    // ACÁ VA TU MÉTODO REAL DE EDICIÓN CUD.
                    // No lo dejo inventado para no romper ProduccionAccess.
                    //
                    // Ejemplo futuro:
                    // new ProduccionAccess().EditarCertificadoCudVerificado(
                    //     dto.NumeroCertificado,
                    //     dto.ApellidoNombre,
                    //     dto.Sexo,
                    //     dto.DNI,
                    //     dto.CUIL,
                    //     dto.FechaNacimiento,
                    //     dto.Diagnostico,
                    //     dto.FuncionesCorporales,
                    //     dto.EstructurasCorporales,
                    //     dto.ActividadParticipacion,
                    //     dto.FactoresAmbientales,
                    //     dto.OrientacionPrestacional,
                    //     dto.FechaEmision,
                    //     dto.FechaVencimiento,
                    //     dto.Acompaniante,
                    //     nombreAntesDeEditar + ".pdf");
                    // ======================================================

                    grabacionExitosa = 1;
                }
                else if (copiaExitosa == 1)
                {
                    // ======================================================
                    // ACÁ VA TU MÉTODO REAL DE INSERCIÓN CUD.
                    //
                    // Ejemplo futuro:
                    // new ProduccionAccess().CertificadoCud(
                    //     dto.NumeroCertificado,
                    //     dto.ApellidoNombre,
                    //     dto.Sexo,
                    //     dto.DNI,
                    //     dto.CUIL,
                    //     dto.FechaNacimiento,
                    //     dto.Diagnostico,
                    //     dto.FuncionesCorporales,
                    //     dto.EstructurasCorporales,
                    //     dto.ActividadParticipacion,
                    //     dto.FactoresAmbientales,
                    //     dto.OrientacionPrestacional,
                    //     dto.FechaEmision,
                    //     dto.FechaVencimiento,
                    //     dto.Acompaniante,
                    //     rutaOrigen,
                    //     txtNombreImagen.Text + ".pdf",
                    //     nombreLote,
                    //     indexador);
                    //
                    // new ProduccionAccess().CertificadoCudVerificado(
                    //     dto.NumeroCertificado,
                    //     dto.ApellidoNombre,
                    //     dto.Sexo,
                    //     dto.DNI,
                    //     dto.CUIL,
                    //     dto.FechaNacimiento,
                    //     dto.Diagnostico,
                    //     dto.FuncionesCorporales,
                    //     dto.EstructurasCorporales,
                    //     dto.ActividadParticipacion,
                    //     dto.FactoresAmbientales,
                    //     dto.OrientacionPrestacional,
                    //     dto.FechaEmision,
                    //     dto.FechaVencimiento,
                    //     dto.Acompaniante,
                    //     nuevoNombreImagen + ".pdf");
                    // ======================================================

                    grabacionExitosa = 1;
                }

                if (grabacionExitosa == 1 || editar == 1)
                    EliminarArchivoOrigenConReintentos(rutaOrigen);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en RegistrarProduccionAccessCud: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================================
        // OCR MANUAL FUTURO DESDE CUADRANTE
        // ==========================================================

        private void AplicarTextoCapturadoOcr(Control destino, string textoOcr)
        {
            if (destino == null)
                return;

            _cargandoDatosCud = true;

            if (destino == TxtNumeCert)
                TxtNumeCert.Text = IndexadorCudService.NormalizarNumeroCertificado(textoOcr);
            else if (destino == TxtApellidoNombre)
                TxtApellidoNombre.Text = IndexadorCudService.NormalizarNombreApellido(textoOcr);
            else if (destino == TxtNumDNI)
                TxtNumDNI.Text = IndexadorCudService.NormalizarDni(textoOcr);
            else if (destino == TxtNumCuil)
                TxtNumCuil.Text = IndexadorCudService.NormalizarCuil(textoOcr);
            else if (destino == TxtFechaNac || destino == TxtFechaEmision || destino == TxtFechaVencimiento)
                ((TextBox)destino).Text = IndexadorCudService.NormalizarFecha(textoOcr);
            else if (destino == TxtFuncionesCorporales ||
                     destino == TxtEstructurasCorporales ||
                     destino == TxtActividad_Particip ||
                     destino == TxtFactoresAmbientales)
                ((TextBox)destino).Text = IndexadorCudService.NormalizarCodigosCif(textoOcr);
            else if (destino == TxtDiagnostico)
                TxtDiagnostico.Text = IndexadorCudService.NormalizarTextoLibre(textoOcr);
            else if (destino == TxtOrientacion)
                TxtOrientacion.Text = IndexadorCudService.NormalizarOrientacion(textoOcr);
            else if (destino == CmbSexo)
                CmbSexo.Text = IndexadorCudService.NormalizarSexo(textoOcr);
            else if (destino == CmbAcomapante)
                CmbAcomapante.Text = IndexadorCudService.NormalizarSiNo(textoOcr);

            _cargandoDatosCud = false;

            if (destino == TxtNumeCert)
            {
                DatosNumeroCertificadoCud datos = IndexadorCudService.ParseNumeroCertificado(TxtNumeCert.Text);
                if (datos.EsValido)
                    AplicarTriangulacionCertificado(datos, true);
            }
        }

        // ==========================================================
        // LIMPIEZA
        // ==========================================================

        private void LimpiarTextBox()
        {
            pictureBox1.Image = null;

            _cargandoDatosCud = true;

            TxtNumeCert.Clear();
            TxtApellidoNombre.Clear();
            TxtNumDNI.Clear();
            TxtNumCuil.Clear();
            TxtFechaNac.Clear();
            TxtDiagnostico.Clear();
            TxtFuncionesCorporales.Clear();
            TxtEstructurasCorporales.Clear();
            TxtActividad_Particip.Clear();
            TxtFactoresAmbientales.Clear();
            TxtOrientacion.Clear();
            TxtFechaEmision.Clear();
            TxtFechaVencimiento.Clear();

            if (CmbSexo.Items.Count > 0)
                CmbSexo.SelectedIndex = 0;

            if (CmbAcomapante.Items.Count > 0)
                CmbAcomapante.SelectedIndex = 0;

            _ultimoDniAuto = string.Empty;
            _ultimaEmisionAuto = string.Empty;
            _ultimoVencimientoAuto = string.Empty;

            _cargandoDatosCud = false;

            RestaurarColoresFormularioCud();
        }

        // ==========================================================
        // NAVEGACIÓN POR TECLADO
        // ==========================================================

        private void ControlCarga_KeyDownCud(object sender, KeyEventArgs e)
        {
            Control controlActual = sender as Control;
            if (controlActual == null)
                return;

            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;

                if (controlActual == btnGrabar)
                {
                    btnGrabar.PerformClick();
                    return;
                }

                MoverFocoCud(controlActual, true);
            }
            else if (e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                MoverFocoCud(controlActual, false);
            }
        }

        private void MoverFocoCud(Control actual, bool haciaAdelante)
        {
            List<Control> controles = ObtenerControlesCargaCud();

            // Al final siempre va Grabar y Continuar.
            controles.Add(btnGrabar);

            int indiceActual = controles.IndexOf(actual);

            if (indiceActual < 0)
                return;

            int nuevoIndice = haciaAdelante ? indiceActual + 1 : indiceActual - 1;

            if (nuevoIndice < 0)
                nuevoIndice = 0;

            if (nuevoIndice >= controles.Count)
                nuevoIndice = controles.Count - 1;

            Control destino = controles[nuevoIndice];

            if (destino != null && destino.CanFocus)
            {
                destino.Focus();

                TextBox txt = destino as TextBox;
                if (txt != null)
                {
                    txt.SelectionStart = txt.Text.Length;
                    txt.SelectionLength = 0;
                }

                ComboBox cmb = destino as ComboBox;
                if (cmb != null)
                {
                    cmb.SelectionStart = cmb.Text.Length;
                    cmb.SelectionLength = 0;
                }
            }
        }
      
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Tab)
            {
                Control activo = ActiveControl;

                if (activo != null && EsControlDeCargaCud(activo))
                {
                    MoverFocoCud(activo, true);
                    return true;
                }
            }

            if (keyData == (Keys.Shift | Keys.Tab))
            {
                Control activo = ActiveControl;

                if (activo != null && EsControlDeCargaCud(activo))
                {
                    MoverFocoCud(activo, false);
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
        private bool EsControlDeCargaCud(Control control)
        {
            if (control == null)
                return false;

            List<Control> controles = ObtenerControlesCargaCud();
            controles.Add(btnGrabar);

            return controles.Contains(control);
        }

        // ==========================================================
        // PDF / PICTUREBOX
        // ==========================================================

        private void CargarPDFEnPictureBox(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("No se encontró el PDF: " + filePath,
                        "Indexador CUD", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (pictureBox1.Image != null)
                {
                    Image imgAnterior = pictureBox1.Image;
                    pictureBox1.Image = null;
                    imgAnterior.Dispose();
                }

                using (PdfDocument pdfDocument = PdfDocument.Load(filePath))
                {
                    if (pdfDocument.PageCount > 0)
                    {
                        Image pdfImage = pdfDocument.Render(
                            0,
                            pictureBox1.Width,
                            pictureBox1.Height,
                            96,
                            96,
                            true);

                        pictureBox1.Image = pdfImage;
                    }
                    else
                    {
                        MessageBox.Show("El PDF no contiene páginas.",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al abrir el PDF: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PictureBox1_SizeChanged(object sender, EventArgs e)
        {
            if (lastWidth > 0 && lastHeight > 0)
                ResaltarCampo(lastX, lastY, lastWidth, lastHeight);
        }

        private async void ResaltarCampo(int x, int y, int width, int height)
        {
            limpiarrectangulo();
            await Task.Delay(100);

            lastX = x;
            lastY = y;
            lastWidth = width;
            lastHeight = height;

            float scaleX = (float)pictureBox1.Width / OriginalWidth;
            float scaleY = (float)pictureBox1.Height / OriginalHeight;

            int scaledX = (int)(x * scaleX);
            int scaledY = (int)(y * scaleY);
            int scaledWidth = (int)(width * scaleX);
            int scaledHeight = (int)(height * scaleY);

            using (Graphics lienzo = pictureBox1.CreateGraphics())
            using (Brush brush = new SolidBrush(Color.FromArgb(60, 255, 255, 0)))
            {
                lienzo.FillRectangle(brush, scaledX, scaledY + pixelgriss, scaledWidth, scaledHeight);
            }
        }

        private void limpiarrectangulo()
        {
            if (Lienzo != null)
                pictureBox1.Refresh();

            pictureBox1.Invalidate();
        }

        private void AjustarPictureBox()
        {
            if (OriginalWidth <= 0 || OriginalHeight <= 0)
                return;

            float scaleX = (float)ClientSize.Width / OriginalWidth;
            float scaleY = (float)ClientSize.Height / OriginalHeight;

            pictureBox1.Width = (int)(OriginalWidth * scaleX);
            pictureBox1.Height = (int)(OriginalHeight * scaleY);
        }

        private void FormActasDeNacimiento_Resize(object sender, EventArgs e)
        {
            AjustarPictureBox();
        }

        // ==========================================================
        // RECURSOS PDFIUM
        // ==========================================================

        private void ListEmbeddedResources()
        {
            string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            Console.WriteLine("Recursos incrustados en el ensamblado:");
            foreach (string resource in resources)
                Console.WriteLine(resource);
        }

        private void ExtractPdfium()
        {
            string resourceName = "Indexador_GCBA_.pdfium.dll";
            string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdfium.dll");

            Console.WriteLine("Extrayendo pdfium.dll a: " + outputPath);

            if (File.Exists(outputPath))
            {
                Console.WriteLine("pdfium.dll ya existe en " + outputPath);
                return;
            }

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Console.WriteLine("No se encontró el recurso " + resourceName);
                    return;
                }

                using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }
            }

            Console.WriteLine("pdfium.dll extraída correctamente.");
        }

        // ==========================================================
        // FILESYSTEM
        // ==========================================================

        private void EliminarArchivoOrigenConReintentos(string rutaOrigen)
        {
            if (!File.Exists(rutaOrigen))
                return;

            File.SetAttributes(rutaOrigen, FileAttributes.Normal);

            int intentos = 0;
            int maxIntentos = 10;

            while (intentos < maxIntentos)
            {
                try
                {
                    File.Delete(rutaOrigen);
                    return;
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(500);
                    intentos++;
                }
            }
        }

        private void IntentarEliminarCarpetaVacia(string ruta)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ruta))
                    return;

                DirectoryInfo di = new DirectoryInfo(ruta);

                if (di.Exists && di.GetFiles().Length == 0 && di.GetDirectories().Length == 0)
                    di.Delete();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al eliminar carpeta vacía: " + ex.Message);
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            foreach (string file in Directory.EnumerateFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));

                if (!File.Exists(destFile))
                    File.Copy(file, destFile);
            }

            foreach (string subDir in Directory.EnumerateDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

        // ==========================================================
        // LOAD
        // ==========================================================

        private void FormActasDeNacimiento_Load(object sender, EventArgs e)
        {
            //try
            //{
            //    using (var oCon = ConexionAccess.ObtenerConexion2())
            //    {
            //        oCon.Open();
            //    }
            //}
            //catch
            //{
            //    MessageBox.Show(
            //        "No se pudo establecer conexión con la base de datos. Verifique FortiClient o la conexión de red antes de comenzar con la indexación.",
            //        "Error de conexión",
            //        MessageBoxButtons.OK,
            //        MessageBoxIcon.Error);
            //}

            // Carga automática de PDF de prueba para empezar a trabajar OCR manual/cuadrante.
            BeginInvoke(new Action(CargarPdfPruebaCopidis));
        }

        private void CargarPdfPruebaCopidis()
        {
            string rutaPdfPrueba = @"C:\Users\Administrador\Desktop\Prueba Copidis\2.pdf";

            if (!File.Exists(rutaPdfPrueba))
            {
                MessageBox.Show(
                    "No se encontró el PDF de prueba:" + Environment.NewLine + rutaPdfPrueba,
                    "PDF de prueba",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            RutaNombreImagen = rutaPdfPrueba;
            txtNombreImagen.Text = Path.GetFileNameWithoutExtension(rutaPdfPrueba);
            txtRutaLote.Text = Path.GetDirectoryName(rutaPdfPrueba);

            CargarPDFEnPictureBox(rutaPdfPrueba);

            HabilitarBotones();

            TxtNumeCert.Focus();
        }



        // ==========================================================

        // MÉTODOS VIEJOS DE ACTAS: STUBS PARA QUE NO ROMPA EL DESIGNER
        // No tienen lógica vieja. Si el diseñador los sigue referenciando,
        // no generan comportamiento incorrecto.
        // ==========================================================

        private void ValidarSoloNumeros(object sender, KeyPressEventArgs e)
        {
            KeyPressSoloNumerosCud(sender, e);
        }

        private void TxtNumeCert_KeyPress(object sender, KeyPressEventArgs e)
        {
            KeyPressNumeroCertificadoCud(sender, e);
        }

        private void TxtNumeCert_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtTomo_Acta_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtNumero_Acta_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtAnio_Acta_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtDia_inscripcion_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtMes_Inscripcion_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtAño_Inscripcion_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtNombre_Nacido_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtCNI_Numero_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtSexo_Nacido_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtDia_Nacimiento_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtMes_Nacimiento_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtAño_Nacimiento_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtNombre_Padre_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtNombre_Madre_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void btnGrabar_KeyDown(object sender, KeyEventArgs e)
        {
            ControlCarga_KeyDownCud(sender, e);
        }

        private void txtMes_Nacimiento_TextChanged(object sender, EventArgs e)
        {
            // Sin lógica. Stub para compatibilidad con designer.
        }

        private void txtMes_Inscripcion_TextChanged(object sender, EventArgs e)
        {
            // Sin lógica. Stub para compatibilidad con designer.
        }

        private void txtSexo_Nacido_TextChanged(object sender, EventArgs e)
        {
            // Sin lógica. Stub para compatibilidad con designer.
        }

        private void txtNombre_Madre_TabStopChanged(object sender, EventArgs e)
        {
            btnGrabar.Focus();
        }

        private void TxtNumeCert_MouseClick(object sender, MouseEventArgs e)
        {
            // Sin lógica.
        }

        private void groupBox11_Enter(object sender, EventArgs e)
        {
            // Sin lógica.
        }
    }
}