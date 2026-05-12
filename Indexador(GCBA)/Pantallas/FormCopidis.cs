using Indexador_GCBA_.Datos;
using Indexador_GCBA_.Servicios;
using PdfiumViewer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Indexador_GCBA_
{
    public partial class FormCopidis : Form
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
        private readonly string rutaCudProcesadas = @"J:\COPIDIS\01_PENDIENTES_INDEXAR\";
        private readonly string rutaCudVerificada = @"J:\COPIDIS\02_CERTIFICADOS_VERIFICADOS\";
        private readonly string rutaCudIlegibles = @"J:\COPIDIS\03_ILEGIBLES\";
        private int _idCudEditando = 0;

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

        // ==========================================================
        // PICTUREBOX / OCR POR CUADRANTE
        // ==========================================================

        private readonly OcrPorCuadranteService _ocrPorCuadranteService = new OcrPorCuadranteService();

        private bool _seleccionandoCuadranteOcr = false;
        private Point _puntoInicioCuadranteOcr = Point.Empty;
        private Rectangle _rectanguloSeleccionVistaOcr = Rectangle.Empty;

        private Control _controlDestinoOcrActivo = null;

        private readonly Color _colorCuadranteOcr = Color.FromArgb(70, 0, 120, 215);
        private readonly Pen _bordeCuadranteOcr = new Pen(Color.DodgerBlue, 2);
      
        private Dictionary<Control, RectangleF> _zonasSugeridasOcrCud =
    new Dictionary<Control, RectangleF>();

        private EstadoCuadranteOcr _estadoCuadranteOcr = EstadoCuadranteOcr.Ninguno;

        private bool _moviendoCuadranteSugeridoOcr = false;
        private bool _procesandoCuadranteOcr = false;

        private Point _offsetMovimientoCuadranteOcr = Point.Empty;

        private const int MIN_ANCHO_CUADRANTE_OCR = 8;
        private const int MIN_ALTO_CUADRANTE_OCR = 8;

        private enum EstadoCuadranteOcr
        {
            Ninguno,
            SugeridoAzul,
            DibujandoManual,
            MoviendoSugerido,
            ConfirmadoVerde,
            ErrorNaranja
        }

        public FormCopidis(Entidades.Indexador pIndexador)
        {
            InitializeComponent();

            oIndexador = pIndexador;

            if (oIndexador != null && !string.IsNullOrWhiteSpace(oIndexador.NombreIndexador))
            {
                txtUsuario.Text = "Usuario: " + oIndexador.NombreIndexador.Trim();
            }
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

            ConfigurarCombosCud();
            ConfigurarEventosCud();
            ConfigurarOrdenTabulacionCud();

            ConfigurarSeleccionCuadranteOcr();

            RestaurarColoresFormularioCud();
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
            string indexadorActual = ObtenerNombreIndexadorActual();

            if (string.IsNullOrWhiteSpace(indexadorActual))
            {
                btnSeleccionarLote.Enabled = true;

                MessageBox.Show(
                    "No se pudo determinar el usuario/indexador actual. Verifique que el usuario esté seleccionado o cargado antes de tomar un lote.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            Debug.WriteLine("[LOTE] Indexador actual detectado: " + indexadorActual);

            try
            {
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

                            string loteAsignado = loteasignado(lote, grupo, indexadorActual);

                            if (string.IsNullOrWhiteSpace(loteAsignado))
                                continue;

                            string rutaLote = Path.Combine(rutaCudProcesadas, grupo, loteAsignado);

                            string usuarioAsignado = new ObtenerDatos().Consultaloteasignado(rutaLote);

                            if (string.IsNullOrWhiteSpace(usuarioAsignado))
                            {
                                Debug.WriteLine("[LOTE] Asignando lote: " + rutaLote);
                                Debug.WriteLine("[LOTE] Usuario a guardar: " + indexadorActual);

                                new ProduccionAccess().loteasignado(rutaLote, indexadorActual);
                            }
                            else
                            {
                                Debug.WriteLine("[LOTE] Lote ya asignado a: " + usuarioAsignado);
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

                    MessageBox.Show(
                        "No se encontraron lotes CUD disponibles para indexar.",
                        "Indexador CUD",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                btnSeleccionarLote.Enabled = true;

                MessageBox.Show(
                    "Error al seleccionar lote CUD: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                MessageBox.Show(
                    "Debe seleccionar un certificado antes de marcarlo como ILEGIBLE.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            int numeroLineaActual = lboxIndice.SelectedIndex;
            int numeroLineaSiguiente = numeroLineaActual + 1;

            string lineaActual = lboxIndice.SelectedItem.ToString();
            string nombreOriginal = ObtenerNombreOriginalDesdeLineaIndice(lineaActual);

            if (string.IsNullOrWhiteSpace(nombreOriginal))
            {
                MessageBox.Show(
                    "No se pudo determinar el nombre original del certificado.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string rutaLote = txtRutaLote.Text;
            string rutaOrigenPdf = Path.Combine(rutaLote, nombreOriginal + ".pdf");

            if (!File.Exists(rutaOrigenPdf))
            {
                MessageBox.Show(
                    "No se encontró el PDF de origen:" + Environment.NewLine + rutaOrigenPdf,
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                string carpetaIlegibles = ObtenerCarpetaIlegiblesCaja();
                Directory.CreateDirectory(carpetaIlegibles);

                string destinoBase = Path.Combine(carpetaIlegibles, nombreOriginal + ".pdf");
                string destinoFinal = ObtenerRutaDisponible(destinoBase);

                Debug.WriteLine("[CUD ILEGIBLE] Origen: " + rutaOrigenPdf);
                Debug.WriteLine("[CUD ILEGIBLE] Destino: " + destinoFinal);

                File.Copy(rutaOrigenPdf, destinoFinal, false);
                EliminarArchivoOrigenConReintentos(rutaOrigenPdf);

                lboxIndice.Items[numeroLineaActual] = nombreOriginal + ";ILEGIBLE";

                PasarAlSiguiente(numeroLineaActual, numeroLineaSiguiente, nombreOriginal);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al procesar el certificado ilegible: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string pindexador = txtUsuario.Text.Replace("Usuario: ", "").Trim();

            if (lboxIndice.Items.Count > 0)
            {
                MessageBox.Show(
                    "El lote todavía tiene certificados pendientes.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            if (string.IsNullOrWhiteSpace(txtRutaLote.Text))
                return;

            try
            {
                new ProduccionAccess().CerrarLoteAsignado(txtRutaLote.Text, pindexador);

                txtRutaLote.Text = string.Empty;
                btnSeleccionarLote.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cerrar el lote: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "¿Desea pasar el lote completo a ILEGIBLE?",
                "Confirmación",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (result != DialogResult.OK)
                return;

            string origen = txtRutaLote.Text;

            if (string.IsNullOrWhiteSpace(origen) || !Directory.Exists(origen))
            {
                MessageBox.Show(
                    "La carpeta de origen no existe.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            try
            {
                Directory.CreateDirectory(rutaCudIlegibles);

                string carpetaNombre = new DirectoryInfo(origen).Name;
                string destinoBase = Path.Combine(rutaCudIlegibles, carpetaNombre);
                string destinoCompleto = destinoBase;

                if (Directory.Exists(destinoCompleto))
                {
                    int contador = 2;

                    while (Directory.Exists(destinoCompleto))
                    {
                        destinoCompleto = destinoBase + "_" + contador;
                        contador++;
                    }
                }

                Debug.WriteLine("[CUD LOTE ILEGIBLE] Origen: " + origen);
                Debug.WriteLine("[CUD LOTE ILEGIBLE] Destino: " + destinoCompleto);

                Directory.Move(origen, destinoCompleto);

                string pindexador = txtUsuario.Text.Replace("Usuario: ", "").Trim();

                try
                {
                    new ProduccionAccess().CerrarLoteAsignado(origen, pindexador);
                }
                catch (Exception exLote)
                {
                    Debug.WriteLine("[CUD LOTE ILEGIBLE][WARN] No se pudo cerrar lote:");
                    Debug.WriteLine(exLote.ToString());
                }

                VaciarFormulario();

                MessageBox.Show(
                    "El lote se movió correctamente a ILEGIBLE.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    "Error al mover el lote. Puede haber archivos en uso: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error inesperado: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
                MessageBox.Show(
                    "La carpeta del lote no existe.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                btnSeleccionarLote.Enabled = true;
                return;
            }

            try
            {
                List<string> pendientes = await Task.Run(() =>
                {
                    List<string> lista = new List<string>();

                    DirectoryInfo dilo = new DirectoryInfo(rutaLote);
                    ObtenerDatos oconsulta = new ObtenerDatos();

                    foreach (FileInfo file in dilo.GetFiles("*.pdf"))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file.Name);

                        if (usuariocoincide == 1)
                        {
                            int verificado = oconsulta.ConsultaCudVerificadoPorRutaEntrada(file.FullName);

                            if (verificado != 0)
                            {
                                Debug.WriteLine("[CUD LOTE] Se omite ya verificado: " + file.FullName);
                                continue;
                            }
                        }

                        lista.Add(fileName);
                    }

                    return lista;
                });

                lboxIndice.Items.Clear();

                foreach (string item in pendientes)
                {
                    lboxIndice.Items.Add(item);
                }

                if (lboxIndice.Items.Count > 0)
                {
                    lboxIndice.SelectedIndex = 0;
                    CargarIndiceFijoEImagen();
                }
                else
                {
                    btnSeleccionarLote.Enabled = true;

                    MessageBox.Show(
                        "El lote no contiene certificados pendientes.",
                        "Indexador CUD",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                btnSeleccionarLote.Enabled = true;

                MessageBox.Show(
                    "Error al cargar documentos del lote: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string loteasignado(string lote, string grupo, string pindexador)
        {
            usuariocoincide = 0;

            string rutaGrupo = Path.Combine(rutaCudProcesadas, grupo);
            string rutaLote = Path.Combine(rutaGrupo, lote);

            if (!Directory.Exists(rutaGrupo))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(pindexador))
            {
                Debug.WriteLine("[LOTE][ERROR] No se puede evaluar lote sin indexador.");
                return string.Empty;
            }

            ObtenerDatos oconsulta = new ObtenerDatos();
            string consulta = oconsulta.Consultaloteasignado(rutaLote);

            Debug.WriteLine("[LOTE] Evaluando lote: " + rutaLote);
            Debug.WriteLine("[LOTE] Usuario BD: " + consulta);
            Debug.WriteLine("[LOTE] Usuario actual: " + pindexador);

            if (!string.IsNullOrWhiteSpace(consulta))
            {
                if (consulta.Trim().Equals(pindexador.Trim(), StringComparison.OrdinalIgnoreCase))
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
            _idCudEditando = 0;

            CargarDatosFijosIndice();

            string nombreIndice = txtNombreImagen.Text;

            if (string.IsNullOrWhiteSpace(nombreIndice))
                return;

            if (nombreIndice.Contains(";"))
            {
                string nombreVerificado = ObtenerNombreVerificadoDesdeLineaIndice(nombreIndice);

                txtNombreImagen.Text = nombreVerificado;

                string rutaEnCarpetaLote = Path.Combine(
                    ObtenerCarpetaVerificadosCaja(),
                    nombreVerificado + ".pdf");

                string rutaEnCarpetaGeneral = Path.Combine(
                    rutaCudVerificada,
                    nombreVerificado + ".pdf");

                if (File.Exists(rutaEnCarpetaLote))
                    RutaNombreImagen = rutaEnCarpetaLote;
                else
                    RutaNombreImagen = rutaEnCarpetaGeneral;

                editar = 1;
            }
            else
            {
                txtNombreImagen.Text = nombreIndice;
                RutaNombreImagen = Path.Combine(txtRutaLote.Text, txtNombreImagen.Text + ".pdf");
            }

            inicio = 1;

            CargarFicha(RutaNombreImagen);

            if (editar == 1)
                CargarDatosCudEditado(RutaNombreImagen);

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
        }

        private void PasarAlSiguiente(int numeroLineaActual, int numeroLineaSiguiente, string nuevoNombre)
        {
            if (pictureBox1.Image != null)
            {
                Image img = pictureBox1.Image;
                pictureBox1.Image = null;
                img.Dispose();
            }

            LimpiarTextBox();
            inicio = 0;

            string rutaProcesada = txtRutaLote.Text;

            if (numeroLineaActual + 1 >= lboxIndice.Items.Count)
            {
                VaciarFormulario();
                DesHabilarBotones();

                if (pictureBox1.Image != null)
                {
                    Image img = pictureBox1.Image;
                    pictureBox1.Image = null;
                    img.Dispose();
                }

                MessageBox.Show(
                    "El lote está terminado.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

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

            editar = 0;
            _idCudEditando = 0;
            inicio = 0;

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

            if (txt == TxtFechaNac)
            {
                txt.Text = IndexadorCudService.NormalizarFechaCud(
                    txt.Text,
                    TipoFechaCud.Nacimiento);

                MarcarCampo(
                    txt,
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
                txt.Text = IndexadorCudService.NormalizarFechaCud(
                    txt.Text,
                    TipoFechaCud.Emision);

                MarcarCampo(
                    txt,
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
                txt.Text = IndexadorCudService.NormalizarFechaCud(
                    txt.Text,
                    TipoFechaCud.Vencimiento);

                MarcarCampo(
                    txt,
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

            // ==========================================================
            // CAMPOS OBLIGATORIOS
            // ==========================================================

            valido &= ValidarYMarcar(
                TxtNumeCert,
                ValidarNumeroCertificadoPantalla(),
                ref primerError);

            valido &= ValidarYMarcar(
                TxtApellidoNombre,
                IndexadorCudService.ValidarObligatorio(TxtApellidoNombre.Text, "Apellido y nombre"),
                ref primerError);

            valido &= ValidarYMarcar(
                CmbSexo,
                IndexadorCudService.ValidarObligatorio(CmbSexo.Text, "Sexo"),
                ref primerError);

            valido &= ValidarYMarcar(
                TxtNumDNI,
                IndexadorCudService.ValidarDni(TxtNumDNI.Text, true),
                ref primerError);

            // ==========================================================
            // CUIL OPCIONAL
            // Si está vacío, pasa.
            // Si tiene datos, se valida contra DNI.
            // ==========================================================

            valido &= ValidarYMarcar(
                TxtNumCuil,
                ValidarCuilOpcionalPantalla(),
                ref primerError);

            valido &= ValidarYMarcar(
                TxtFechaNac,
                IndexadorCudService.ValidarFecha(
                    TxtFechaNac.Text,
                    "Fecha de nacimiento",
                    true,
                    false,
                    new DateTime(1900, 1, 1),
                    DateTime.Today),
                ref primerError);

            valido &= ValidarYMarcar(
                TxtDiagnostico,
                IndexadorCudService.ValidarObligatorio(TxtDiagnostico.Text, "Diagnóstico"),
                ref primerError);

            // ==========================================================
            // CIF OPCIONALES
            // Si están vacíos, pasan.
            // Si tienen datos, se valida letra/formato.
            // ==========================================================

            valido &= ValidarYMarcar(
                TxtFuncionesCorporales,
                ValidarCodigoCifPorCampo(TxtFuncionesCorporales),
                ref primerError);

            valido &= ValidarYMarcar(
                TxtEstructurasCorporales,
                ValidarCodigoCifPorCampo(TxtEstructurasCorporales),
                ref primerError);

            valido &= ValidarYMarcar(
                TxtActividad_Particip,
                ValidarCodigoCifPorCampo(TxtActividad_Particip),
                ref primerError);

            valido &= ValidarYMarcar(
                TxtFactoresAmbientales,
                ValidarCodigoCifPorCampo(TxtFactoresAmbientales),
                ref primerError);

            valido &= ValidarYMarcar(
                TxtOrientacion,
                IndexadorCudService.ValidarObligatorio(TxtOrientacion.Text, "Orientación prestacional"),
                ref primerError);

            valido &= ValidarYMarcar(
                TxtFechaEmision,
                IndexadorCudService.ValidarFecha(
                    TxtFechaEmision.Text,
                    "Fecha de emisión",
                    true,
                    false,
                    new DateTime(1900, 1, 1),
                    DateTime.Today.AddDays(1)),
                ref primerError);

            valido &= ValidarYMarcar(
                TxtFechaVencimiento,
                IndexadorCudService.ValidarFecha(
                    TxtFechaVencimiento.Text,
                    "Fecha de vencimiento",
                    true,
                    true,
                    new DateTime(1900, 1, 1),
                    null),
                ref primerError);

            valido &= ValidarYMarcar(
                CmbAcomapante,
                IndexadorCudService.ValidarObligatorio(CmbAcomapante.Text, "Acompañante"),
                ref primerError);

            // ==========================================================
            // FECHAS CRUZADAS
            // ==========================================================

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

                MessageBox.Show(
                    "Hay campos obligatorios o inconsistentes. Revise los campos marcados.",
                    "Validación CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            return true;
        }
        private ResultadoValidacion ValidarCuilOpcionalPantalla()
        {
            string cuil = IndexadorCudService.NormalizarCuil(TxtNumCuil.Text);

            // CUIL no obligatorio.
            if (string.IsNullOrWhiteSpace(cuil))
                return ResultadoValidacion.Ok("CUIL no informado.");

            // Si el indexador cargó algo, debe ser válido.
            return IndexadorCudService.ValidarCuil(
                cuil,
                TxtNumDNI.Text,
                true);
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
            if (txt == null)
                return ResultadoValidacion.Ok();

            string texto = txt.Text.Trim();

            // Los campos CIF no son obligatorios.
            // Si están vacíos, pasan sin error.
            if (string.IsNullOrWhiteSpace(texto))
                return ResultadoValidacion.Ok("Campo CIF no informado.");

            // Si tienen datos, se valida normalmente.
            if (txt == TxtFuncionesCorporales)
            {
                return IndexadorCudService.ValidarCodigosCif(
                    texto,
                    'b',
                    "Funciones corporales",
                    true);
            }

            if (txt == TxtEstructurasCorporales)
            {
                return IndexadorCudService.ValidarCodigosCif(
                    texto,
                    's',
                    "Estructuras corporales",
                    true);
            }

            if (txt == TxtActividad_Particip)
            {
                return IndexadorCudService.ValidarCodigosCif(
                    texto,
                    'd',
                    "Actividad y participación",
                    true);
            }

            if (txt == TxtFactoresAmbientales)
            {
                return IndexadorCudService.ValidarCodigosCif(
                    texto,
                    'e',
                    "Factores ambientales",
                    true);
            }

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
                MessageBox.Show(
                    "Debe seleccionar un certificado antes de grabar.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            if (editar == 1)
            {
                MessageBox.Show(
                    "Este certificado ya está verificado. Para cambiar datos use Modificar.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            int numeroLineaActual = lboxIndice.SelectedIndex;
            int numeroLineaSiguiente = numeroLineaActual + 1;

            string lineaActual = lboxIndice.SelectedItem.ToString();
            string nombreOriginal = ObtenerNombreOriginalDesdeLineaIndice(lineaActual);

            if (string.IsNullOrWhiteSpace(nombreOriginal))
            {
                MessageBox.Show(
                    "No se pudo determinar el nombre original del certificado.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string nuevoNombreBase = CrearNombreArchivoCud(dto);
            string nombreFinalVerificado;

            bool grabado = RegistrarProduccionAccessCud(
                dto,
                nuevoNombreBase,
                nombreOriginal,
                out nombreFinalVerificado);

            if (!grabado)
                return;

            lboxIndice.Items[numeroLineaActual] = nombreOriginal + ";" + nombreFinalVerificado;

            RestaurarColoresFormularioCud();

            PasarAlSiguiente(numeroLineaActual, numeroLineaSiguiente, nombreFinalVerificado);
        }


        private string CrearNombreArchivoCud(CertificadoCudDto dto)
        {
            string dni = IndexadorCudService.SanitizarNombreArchivo(dto.DNI);

            if (!string.IsNullOrWhiteSpace(dni))
                return dni;

            string cert = IndexadorCudService.SanitizarNombreArchivo(dto.NumeroCertificado);

            if (!string.IsNullOrWhiteSpace(cert))
            {
                if (cert.Length > 120)
                    cert = cert.Substring(0, 120).Trim();

                return cert;
            }

            return "CUD_SIN_IDENTIFICAR";
        }


        private bool RegistrarProduccionAccessCud(
          CertificadoCudDto dto,
          string nuevoNombreBase,
          string nombreOriginalSinExtension,
          out string nombreFinalVerificado)
        {
            nombreFinalVerificado = string.Empty;

            string rutaLote = txtRutaLote.Text;
            string nombreLote = ObtenerNombreCarpetaLoteActual();

            string archivoEntrada = nombreOriginalSinExtension + ".pdf";
            string rutaOrigen = Path.Combine(rutaLote, archivoEntrada);

            string indexador = txtUsuario.Text.Replace("Usuario: ", "").Trim();

            if (string.IsNullOrWhiteSpace(nuevoNombreBase))
                nuevoNombreBase = "CUD_SIN_IDENTIFICAR";

            if (!File.Exists(rutaOrigen))
            {
                MessageBox.Show(
                    "No se encontró el PDF de origen:" + Environment.NewLine + rutaOrigen,
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            try
            {
                ProduccionAccess produccion = new ProduccionAccess();

                // ==========================================================
                // 1) VALIDAR DUPLICADO ANTES DE COPIAR PDF
                // ==========================================================
                int idExistente = produccion.BuscarIdCudExistente(
                    dto.NumeroCertificado,
                    dto.DNI,
                    rutaOrigen,
                    nombreLote,
                    archivoEntrada);

                if (idExistente > 0)
                {
                    MessageBox.Show(
                        "El certificado ya existe en la base de datos." + Environment.NewLine +
                        "Id existente: " + idExistente + Environment.NewLine +
                        "No se grabó un duplicado.",
                        "Duplicado CUD",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return false;
                }

                // ==========================================================
                // 2) PREPARAR DESTINO PDF VERIFICADO
                // ==========================================================
                string carpetaDestino = ObtenerCarpetaVerificadosCaja();
                Directory.CreateDirectory(carpetaDestino);

                string rutaDestinoBase = Path.Combine(carpetaDestino, nuevoNombreBase + ".pdf");
                string rutaDestinoFinal = ObtenerRutaDisponible(rutaDestinoBase);

                Debug.WriteLine("==================================================");
                Debug.WriteLine("[CUD GUARDAR] INICIO");
                Debug.WriteLine("[CUD GUARDAR] Lote/Caja: " + nombreLote);
                Debug.WriteLine("[CUD GUARDAR] ArchivoEntrada: " + archivoEntrada);
                Debug.WriteLine("[CUD GUARDAR] RutaEntrada: " + rutaOrigen);
                Debug.WriteLine("[CUD GUARDAR] RutaSalida: " + rutaDestinoFinal);
                Debug.WriteLine("[CUD GUARDAR] NumeroCertificado: " + dto.NumeroCertificado);
                Debug.WriteLine("[CUD GUARDAR] ApellidoNombre: " + dto.ApellidoNombre);
                Debug.WriteLine("[CUD GUARDAR] DNI: " + dto.DNI);
                Debug.WriteLine("[CUD GUARDAR] CUIL: " + dto.CUIL);
                Debug.WriteLine("[CUD GUARDAR] Indexador: " + indexador);

                bool pdfCopiado = false;

                try
                {
                    // ======================================================
                    // 3) COPIAR PDF PRIMERO
                    // Si falla la copia, NO se inserta en Access.
                    // ======================================================
                    File.Copy(rutaOrigen, rutaDestinoFinal, false);
                    pdfCopiado = true;

                    Debug.WriteLine("[CUD GUARDAR] PDF copiado correctamente.");

                    // ======================================================
                    // 4) INSERTAR EN ACCESS - TD_CUD
                    // ======================================================
                    int idNuevo = produccion.CertificadoCud(
                        dto.NumeroCertificado,
                        dto.ApellidoNombre,
                        dto.Sexo,
                        dto.DNI,
                        dto.CUIL,
                        dto.FechaNacimiento,
                        dto.Diagnostico,
                        dto.FuncionesCorporales,
                        dto.EstructurasCorporales,
                        dto.ActividadParticipacion,
                        dto.FactoresAmbientales,
                        dto.OrientacionPrestacional,
                        dto.FechaEmision,
                        dto.FechaVencimiento,
                        dto.Acompaniante,
                        rutaOrigen,
                        archivoEntrada,
                        rutaDestinoFinal,
                        nombreLote,
                        indexador,
                        "Indexado");

                    if (idNuevo <= 0)
                        throw new Exception("Access no devolvió un Id válido para el CUD insertado.");

                    Debug.WriteLine("[CUD GUARDAR] Insert OK. Id: " + idNuevo);

                    // ======================================================
                    // 5) ACTUALIZAR CONTADOR DIARIO DEL INDEXADOR
                    // Esto es secundario: si falla, NO debe romper el CUD.
                    // ======================================================
                    try
                    {
                        produccion.RegistrarIndexacionDiaria(indexador);
                        Debug.WriteLine("[CUD GUARDAR] Indexación diaria actualizada.");
                    }
                    catch (Exception exIndexador)
                    {
                        Debug.WriteLine("[CUD GUARDAR][WARN] No se pudo actualizar indexación diaria.");
                        Debug.WriteLine(exIndexador.ToString());
                    }

                    // ======================================================
                    // 6) ELIMINAR PDF ORIGINAL
                    // Recién ahora, cuando PDF + Access salieron bien.
                    // ======================================================
                    EliminarArchivoOrigenConReintentos(rutaOrigen);

                    nombreFinalVerificado = Path.GetFileNameWithoutExtension(rutaDestinoFinal);

                    Debug.WriteLine("[CUD GUARDAR] Nombre final verificado: " + nombreFinalVerificado);
                    Debug.WriteLine("[CUD GUARDAR] FIN OK");
                    Debug.WriteLine("==================================================");

                    return true;
                }
                catch
                {
                    // ======================================================
                    // ROLLBACK PARCIAL
                    // Si se copió el PDF pero falló Access, borramos la copia.
                    // Así no queda un PDF verificado sin registro en TD_CUD.
                    // ======================================================
                    if (pdfCopiado && File.Exists(rutaDestinoFinal))
                    {
                        try
                        {
                            File.Delete(rutaDestinoFinal);
                            Debug.WriteLine("[CUD GUARDAR] Rollback: PDF copiado eliminado.");
                        }
                        catch (Exception exDelete)
                        {
                            Debug.WriteLine("[CUD GUARDAR][WARN] No se pudo borrar el PDF copiado luego del error:");
                            Debug.WriteLine(exDelete.ToString());
                        }
                    }

                    throw;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[CUD GUARDAR][ERROR]");
                Debug.WriteLine(ex.ToString());

                MessageBox.Show(
                    "Error al guardar el CUD:" + Environment.NewLine + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }

        private void CargarDatosCudEditado(string rutaSalida)
        {
            if (string.IsNullOrWhiteSpace(rutaSalida))
                return;

            try
            {
                DataTable dt = new ObtenerDatos().ObtenerCudPorRutaSalida(rutaSalida);

                if (dt == null || dt.Rows.Count == 0)
                {
                    _idCudEditando = 0;

                    MessageBox.Show(
                        "No se encontraron datos en Access para el certificado verificado.",
                        "Indexador CUD",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return;
                }

                DataRow row = dt.Rows[0];

                _cargandoDatosCud = true;

                _idCudEditando = Convert.ToInt32(row["Id"]);

                TxtNumeCert.Text = ValorRow(row, "NumeroCertificado");
                TxtApellidoNombre.Text = ValorRow(row, "ApellidoNombre");
                TxtNumDNI.Text = ValorRow(row, "DNI");
                TxtNumCuil.Text = ValorRow(row, "CUIL");

                CmbSexo.Text = ValorRow(row, "Sexo");

                TxtFechaNac.Text = FechaRow(row, "FechaNacimiento");
                TxtDiagnostico.Text = ValorRow(row, "Diagnostico");
                TxtFuncionesCorporales.Text = ValorRow(row, "FuncionesCorporales");
                TxtEstructurasCorporales.Text = ValorRow(row, "EstructurasCorporales");
                TxtActividad_Particip.Text = ValorRow(row, "ActividadParticipacion");
                TxtFactoresAmbientales.Text = ValorRow(row, "FactoresAmbientales");
                TxtOrientacion.Text = ValorRow(row, "OrientacionPrestacional");
                TxtFechaEmision.Text = FechaRow(row, "FechaEmision");
                TxtFechaVencimiento.Text = FechaRow(row, "FechaVencimiento");

                CmbAcomapante.Text = ValorRow(row, "Acompaniante");

                _cargandoDatosCud = false;

                RestaurarColoresFormularioCud();

                Debug.WriteLine("[CUD EDITAR] Datos cargados. Id: " + _idCudEditando);
            }
            catch (Exception ex)
            {
                _cargandoDatosCud = false;

                Debug.WriteLine("[CUD EDITAR][ERROR] CargarDatosCudEditado");
                Debug.WriteLine(ex.ToString());

                MessageBox.Show(
                    "Error al cargar datos del CUD editado:" + Environment.NewLine + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            {
                TxtNumeCert.Text = IndexadorCudService.NormalizarNumeroCertificado(textoOcr);
            }
            else if (destino == TxtApellidoNombre)
            {
                TxtApellidoNombre.Text = IndexadorCudService.NormalizarNombreApellido(textoOcr);
            }
            else if (destino == TxtNumDNI)
            {
                TxtNumDNI.Text = IndexadorCudService.NormalizarDni(textoOcr);
            }
            else if (destino == TxtNumCuil)
            {
                TxtNumCuil.Text = IndexadorCudService.NormalizarCuil(textoOcr);
            }
            else if (destino == TxtFechaNac)
            {
                TxtFechaNac.Text = IndexadorCudService.NormalizarFechaCud(
                    textoOcr,
                    TipoFechaCud.Nacimiento);
            }
            else if (destino == TxtFechaEmision)
            {
                TxtFechaEmision.Text = IndexadorCudService.NormalizarFechaCud(
                    textoOcr,
                    TipoFechaCud.Emision);
            }
            else if (destino == TxtFechaVencimiento)
            {
                TxtFechaVencimiento.Text = IndexadorCudService.NormalizarFechaCud(
                    textoOcr,
                    TipoFechaCud.Vencimiento);
            }
            else if (destino == TxtFuncionesCorporales ||
                     destino == TxtEstructurasCorporales ||
                     destino == TxtActividad_Particip ||
                     destino == TxtFactoresAmbientales)
            {
                ((TextBox)destino).Text = IndexadorCudService.NormalizarCodigosCif(textoOcr);
            }
            else if (destino == TxtDiagnostico)
            {
                TxtDiagnostico.Text = IndexadorCudService.NormalizarTextoLibre(textoOcr);
            }
            else if (destino == TxtOrientacion)
            {
                TxtOrientacion.Text = IndexadorCudService.NormalizarOrientacion(textoOcr);
            }
            else if (destino == CmbSexo)
            {
                string sexo = IndexadorCudService.NormalizarSexo(textoOcr);

                if (CmbSexo.Items.Contains(sexo))
                    CmbSexo.Text = sexo;
            }
            else if (destino == CmbAcomapante)
            {
                string acompaniante = IndexadorCudService.NormalizarSiNo(textoOcr);

                if (CmbAcomapante.Items.Contains(acompaniante))
                    CmbAcomapante.Text = acompaniante;
            }

            _cargandoDatosCud = false;

            // ======================================================
            // SINCRONIZACIONES POST-OCR
            // ======================================================

            if (destino == TxtNumeCert)
            {
                DatosNumeroCertificadoCud datos =
                    IndexadorCudService.ParseNumeroCertificado(TxtNumeCert.Text);

                if (datos.EsValido)
                {
                    // Si ya implementaste la versión forzada, usá esta:
                    AplicarTriangulacionCertificadoForzada(datos);

                    // Si todavía NO implementaste la versión forzada,
                    // reemplazá la línea anterior por:
                    // AplicarTriangulacionCertificado(datos, true);
                }
            }
            else if (destino == TxtNumDNI)
            {
                string dni = IndexadorCudService.NormalizarDni(TxtNumDNI.Text);

                ResultadoValidacion rDni = IndexadorCudService.ValidarDni(dni, true);

                if (rDni.EsValido)
                {
                    _sincronizandoCamposCud = true;

                    // DNI controla CUIL.
                    ActualizarCuilDesdeDni(dni);

                    // DNI puede controlar el número de certificado.
                    ActualizarNumeroCertificadoDesdeDni(dni);

                    _sincronizandoCamposCud = false;
                }
            }
        }

        // ==========================================================
        // LIMPIEZA
        // ==========================================================

        private void LimpiarTextBox()
        {

            OcultarCuadranteOcr();
            pictureBox1.Image = null;

            _cargandoDatosCud = true;
            _idCudEditando = 0;
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
        private string ObtenerNombreOriginalDesdeLineaIndice(string linea)
        {
            if (string.IsNullOrWhiteSpace(linea))
                return string.Empty;

            int pos = linea.IndexOf(';');

            if (pos >= 0)
                return linea.Substring(0, pos).Trim();

            return linea.Trim();
        }

        private string ObtenerNombreVerificadoDesdeLineaIndice(string linea)
        {
            if (string.IsNullOrWhiteSpace(linea))
                return string.Empty;

            int pos = linea.IndexOf(';');

            if (pos >= 0 && pos + 1 < linea.Length)
                return linea.Substring(pos + 1).Trim();

            return linea.Trim();
        }

        private string ObtenerNombreCarpetaLoteActual()
        {
            if (string.IsNullOrWhiteSpace(txtRutaLote.Text))
                return "SIN_LOTE";

            string ruta = txtRutaLote.Text.Trim()
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            string nombre = Path.GetFileName(ruta);

            if (string.IsNullOrWhiteSpace(nombre))
                return "SIN_LOTE";

            return nombre;
        }

        private string ObtenerCarpetaVerificadosCaja()
        {
            string caja = ObtenerNombreCarpetaLoteActual();
            return Path.Combine(rutaCudVerificada, caja);
        }

        private string ObtenerCarpetaIlegiblesCaja()
        {
            string caja = ObtenerNombreCarpetaLoteActual();
            return Path.Combine(rutaCudIlegibles, caja);
        }

        private string ObtenerRutaDisponible(string rutaBase)
        {
            if (!File.Exists(rutaBase))
                return rutaBase;

            string carpeta = Path.GetDirectoryName(rutaBase);
            string nombre = Path.GetFileNameWithoutExtension(rutaBase);
            string extension = Path.GetExtension(rutaBase);

            int contador = 2;

            while (true)
            {
                string candidata = Path.Combine(carpeta, nombre + "_" + contador + extension);

                if (!File.Exists(candidata))
                    return candidata;

                contador++;
            }
        }

        private string ValorRow(DataRow row, string columna)
        {
            if (row == null)
                return string.Empty;

            if (!row.Table.Columns.Contains(columna))
                return string.Empty;

            if (row[columna] == DBNull.Value)
                return string.Empty;

            return row[columna].ToString().Trim();
        }

        private string FechaRow(DataRow row, string columna)
        {
            if (row == null)
                return string.Empty;

            if (!row.Table.Columns.Contains(columna))
                return string.Empty;

            if (row[columna] == DBNull.Value)
                return string.Empty;

            DateTime fecha;

            if (DateTime.TryParse(row[columna].ToString(), out fecha))
                return fecha.ToString("dd/MM/yyyy");

            return row[columna].ToString().Trim();
        }




        // ==========================================================
        // LOAD
        // ==========================================================

        private void FormActasDeNacimiento_Load(object sender, EventArgs e)
        {
            try
            {
                using (var oCon = ConexionAccess.ObtenerConexion1())
                {
                    oCon.Open();
                }
            }
            catch
            {
                MessageBox.Show(
                    "No se pudo establecer conexión con la base de datos. Verifique FortiClient o la conexión de red antes de comenzar con la indexación.",
                    "Error de conexión",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

          
        //   BeginInvoke(new Action(CargarPdfPruebaCopidis));
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

        private void ConfigurarSeleccionCuadranteOcr()
        {
            InicializarZonasSugeridasOcrCud();

            pictureBox1.MouseDown -= PictureBox1_MouseDownCuadranteOcr;
            pictureBox1.MouseMove -= PictureBox1_MouseMoveCuadranteOcr;
            pictureBox1.MouseUp -= PictureBox1_MouseUpCuadranteOcr;
            pictureBox1.Paint -= PictureBox1_PaintCuadranteOcr;

            pictureBox1.MouseDown += PictureBox1_MouseDownCuadranteOcr;
            pictureBox1.MouseMove += PictureBox1_MouseMoveCuadranteOcr;
            pictureBox1.MouseUp += PictureBox1_MouseUpCuadranteOcr;
            pictureBox1.Paint += PictureBox1_PaintCuadranteOcr;

            foreach (Control c in ObtenerControlesCargaCud())
            {
                c.GotFocus -= ControlCarga_GotFocusOcr;
                c.Enter -= ControlCarga_GotFocusOcr;

                c.Enter += ControlCarga_GotFocusOcr;
            }

            btnGrabar.Enter -= BtnGrabar_EnterOcr;
            btnGrabar.Enter += BtnGrabar_EnterOcr;
        }

        private Bitmap ClonarImagenActualPictureBoxParaOcr()
        {
            if (pictureBox1.Image == null)
                return null;

            try
            {
                // Se clona en el hilo UI, antes del Task.Run.
                // Así el OCR trabaja sobre una imagen independiente y no sobre pictureBox1.Image.
                return new Bitmap(pictureBox1.Image);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[OCR CUADRANTE] Error clonando imagen actual:");
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        private void InicializarZonasSugeridasOcrCud()
        {
            _zonasSugeridasOcrCud.Clear();

            // Coordenadas normalizadas:
            // X, Y, Ancho, Alto.
            // 0.00 = inicio del área visible de la imagen.
            // 1.00 = final del área visible de la imagen.
            //
            // Estos valores son iniciales. Luego los ajustamos mirando el CUD real.

            _zonasSugeridasOcrCud[TxtNumeCert] =
                new RectangleF(0.49f, 0.052f, 0.46f, 0.038f);

            _zonasSugeridasOcrCud[TxtApellidoNombre] =
                new RectangleF(0.27f, 0.11f, 0.30f, 0.025f);

            _zonasSugeridasOcrCud[CmbSexo] =
                new RectangleF(0.25f, 0.13f, 0.20f, 0.025f);

            _zonasSugeridasOcrCud[TxtNumDNI] =
                new RectangleF(0.25f, 0.13f, 0.20f, 0.025f);

            _zonasSugeridasOcrCud[TxtNumCuil] =
                new RectangleF(0.52f, 0.13f, 0.20f, 0.025f);

            _zonasSugeridasOcrCud[TxtFechaNac] =
                   new RectangleF(0.82f, 0.12f, 0.13f, 0.038f);

            _zonasSugeridasOcrCud[TxtDiagnostico] =
                  new RectangleF(0.22f, 0.16f, 0.46f, 0.045f);

            _zonasSugeridasOcrCud[TxtFuncionesCorporales] =
              new RectangleF(0.28f, 0.24f, 0.40f, 0.045f);

            _zonasSugeridasOcrCud[TxtEstructurasCorporales] =
                    new RectangleF(0.28f, 0.28f, 0.40f, 0.045f);

            _zonasSugeridasOcrCud[TxtActividad_Particip] =
                new RectangleF(0.08f, 0.32f, 0.84f, 0.042f);

            _zonasSugeridasOcrCud[TxtFactoresAmbientales] =
                 new RectangleF(0.28f, 0.36f, 0.40f, 0.045f);

            _zonasSugeridasOcrCud[TxtOrientacion] =
                new RectangleF(0.095f, 0.42f, 0.84f, 0.050f);

            _zonasSugeridasOcrCud[TxtFechaVencimiento] =
              new RectangleF(0.25f, 0.465f, 0.20f, 0.025f);

            _zonasSugeridasOcrCud[CmbAcomapante] =
           new RectangleF(0.16f, 0.495f, 0.19f, 0.035f);

            _zonasSugeridasOcrCud[TxtFechaEmision] =
                new RectangleF(0.695f, 0.55f, 0.25f, 0.040f);
        }

        private void ControlCarga_GotFocusOcr(object sender, EventArgs e)
        {
            if (_procesandoCuadranteOcr)
                return;

            Control control = sender as Control;

            if (control == null)
                return;

            if (!(control is TextBox) && !(control is ComboBox))
                return;

            _controlDestinoOcrActivo = control;

            MostrarCuadranteSugeridoParaControl(control);
        }
        private void BtnGrabar_EnterOcr(object sender, EventArgs e)
        {
            OcultarCuadranteOcr();
        }
        private void MostrarCuadranteSugeridoParaControl(Control control)
        {
            if (pictureBox1.Image == null)
                return;

            if (control == null)
                return;

            if (_zonasSugeridasOcrCud == null || _zonasSugeridasOcrCud.Count == 0)
                InicializarZonasSugeridasOcrCud();

            RectangleF zonaNormalizada;

            if (!_zonasSugeridasOcrCud.TryGetValue(control, out zonaNormalizada))
            {
                OcultarCuadranteOcr();
                return;
            }

            Rectangle rect = ConvertirZonaNormalizadaARectanguloVista(zonaNormalizada);

            if (!CuadranteOcrTieneTamanioValido(rect))
            {
                OcultarCuadranteOcr();
                return;
            }

            _rectanguloSeleccionVistaOcr = rect;
            _estadoCuadranteOcr = EstadoCuadranteOcr.SugeridoAzul;

            _seleccionandoCuadranteOcr = false;
            _moviendoCuadranteSugeridoOcr = false;

            Debug.WriteLine("[OCR CUADRANTE] Cuadrante sugerido para: " + control.Name);
            Debug.WriteLine("[OCR CUADRANTE] Rectángulo sugerido vista: " + _rectanguloSeleccionVistaOcr);

            pictureBox1.Invalidate();
        }
        private Rectangle ConvertirZonaNormalizadaARectanguloVista(RectangleF zona)
        {
            Rectangle areaImagen = ObtenerRectanguloImagenVisibleEnPictureBox();

            if (areaImagen == Rectangle.Empty)
                return Rectangle.Empty;

            int x = areaImagen.Left + (int)(zona.X * areaImagen.Width);
            int y = areaImagen.Top + (int)(zona.Y * areaImagen.Height);
            int width = (int)(zona.Width * areaImagen.Width);
            int height = (int)(zona.Height * areaImagen.Height);

            Rectangle rect = new Rectangle(x, y, width, height);

            return LimitarRectanguloAlAreaImagenVisible(rect);
        }
        private Rectangle ObtenerRectanguloImagenVisibleEnPictureBox()
        {
            if (pictureBox1.Image == null)
                return Rectangle.Empty;

            Rectangle area = pictureBox1.ClientRectangle;

            int imageWidth = pictureBox1.Image.Width;
            int imageHeight = pictureBox1.Image.Height;

            if (imageWidth <= 0 || imageHeight <= 0 || area.Width <= 0 || area.Height <= 0)
                return Rectangle.Empty;

            if (pictureBox1.SizeMode != PictureBoxSizeMode.Zoom)
                return area;

            float ratioImagen = (float)imageWidth / imageHeight;
            float ratioControl = (float)area.Width / area.Height;

            int anchoMostrado;
            int altoMostrado;
            int x;
            int y;

            if (ratioControl > ratioImagen)
            {
                altoMostrado = area.Height;
                anchoMostrado = (int)(altoMostrado * ratioImagen);
                x = area.X + (area.Width - anchoMostrado) / 2;
                y = area.Y;
            }
            else
            {
                anchoMostrado = area.Width;
                altoMostrado = (int)(anchoMostrado / ratioImagen);
                x = area.X;
                y = area.Y + (area.Height - altoMostrado) / 2;
            }

            return new Rectangle(x, y, anchoMostrado, altoMostrado);
        }


        private void PictureBox1_MouseDownCuadranteOcr(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null)
                return;

            if (_procesandoCuadranteOcr)
                return;

            if (e.Button == MouseButtons.Right)
            {
                if (ClickDentroDelCuadranteOcr(e.Location))
                {
                    Debug.WriteLine("[OCR CUADRANTE] Botón derecho: se oculta cuadrante.");
                    OcultarCuadranteOcr();
                }

                return;
            }

            if (e.Button != MouseButtons.Left)
                return;

            if (_controlDestinoOcrActivo == null)
            {
                InformarEstadoOcr("Primero seleccione el campo donde quiere pegar el texto OCR.");
                return;
            }

            Rectangle areaImagen = ObtenerRectanguloImagenVisibleEnPictureBox();

            if (areaImagen == Rectangle.Empty)
                return;

            if (!areaImagen.Contains(e.Location))
                return;

            if (ClickDentroDelCuadranteOcr(e.Location))
            {
                _moviendoCuadranteSugeridoOcr = true;
                _seleccionandoCuadranteOcr = false;

                _estadoCuadranteOcr = EstadoCuadranteOcr.MoviendoSugerido;

                _offsetMovimientoCuadranteOcr = new Point(
                    e.X - _rectanguloSeleccionVistaOcr.X,
                    e.Y - _rectanguloSeleccionVistaOcr.Y);

                pictureBox1.Cursor = Cursors.SizeAll;

                Debug.WriteLine("[OCR CUADRANTE] Inicia movimiento de cuadrante existente.");
                return;
            }

            _seleccionandoCuadranteOcr = true;
            _moviendoCuadranteSugeridoOcr = false;

            _estadoCuadranteOcr = EstadoCuadranteOcr.DibujandoManual;

            _puntoInicioCuadranteOcr = e.Location;
            _rectanguloSeleccionVistaOcr = new Rectangle(e.Location, Size.Empty);

            pictureBox1.Cursor = Cursors.Cross;

            Debug.WriteLine("[OCR CUADRANTE] Inicia dibujo manual de cuadrante.");
            pictureBox1.Invalidate();
        }

        private void PictureBox1_MouseMoveCuadranteOcr(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null)
                return;

            if (_procesandoCuadranteOcr)
                return;

            if (_moviendoCuadranteSugeridoOcr)
            {
                int nuevoX = e.X - _offsetMovimientoCuadranteOcr.X;
                int nuevoY = e.Y - _offsetMovimientoCuadranteOcr.Y;

                Rectangle nuevoRect = new Rectangle(
                    nuevoX,
                    nuevoY,
                    _rectanguloSeleccionVistaOcr.Width,
                    _rectanguloSeleccionVistaOcr.Height);

                _rectanguloSeleccionVistaOcr = LimitarRectanguloAlAreaImagenVisible(nuevoRect);

                pictureBox1.Invalidate();
                return;
            }

            if (_seleccionandoCuadranteOcr)
            {
                Rectangle rect = CrearRectanguloNormalizado(
                    _puntoInicioCuadranteOcr,
                    e.Location);

                Rectangle areaImagen = ObtenerRectanguloImagenVisibleEnPictureBox();

                _rectanguloSeleccionVistaOcr = Rectangle.Intersect(rect, areaImagen);

                pictureBox1.Invalidate();
                return;
            }

            if (ClickDentroDelCuadranteOcr(e.Location))
                pictureBox1.Cursor = Cursors.SizeAll;
            else
                pictureBox1.Cursor = Cursors.Cross;
        }

        private async void PictureBox1_MouseUpCuadranteOcr(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null)
                return;

            if (_procesandoCuadranteOcr)
                return;

            if (e.Button != MouseButtons.Left)
                return;

            bool ejecutarOcr = false;

            if (_moviendoCuadranteSugeridoOcr)
            {
                _moviendoCuadranteSugeridoOcr = false;
                ejecutarOcr = true;

                Debug.WriteLine("[OCR CUADRANTE] Finaliza movimiento. Ejecutar OCR.");
            }

            if (_seleccionandoCuadranteOcr)
            {
                _seleccionandoCuadranteOcr = false;

                _rectanguloSeleccionVistaOcr = CrearRectanguloNormalizado(
                    _puntoInicioCuadranteOcr,
                    e.Location);

                _rectanguloSeleccionVistaOcr =
                    LimitarRectanguloAlAreaImagenVisible(_rectanguloSeleccionVistaOcr);

                ejecutarOcr = true;

                Debug.WriteLine("[OCR CUADRANTE] Finaliza dibujo manual. Ejecutar OCR.");
            }

            pictureBox1.Cursor = Cursors.Cross;

            if (!ejecutarOcr)
                return;

            if (!CuadranteOcrTieneTamanioValido(_rectanguloSeleccionVistaOcr))
            {
                Debug.WriteLine("[OCR CUADRANTE] Rectángulo demasiado chico. Se descarta.");
                OcultarCuadranteOcr();
                return;
            }

            _estadoCuadranteOcr = EstadoCuadranteOcr.ConfirmadoVerde;
            pictureBox1.Invalidate();

            await ProcesarCuadranteSeleccionadoOcr();
        }

        private void PictureBox1_PaintCuadranteOcr(object sender, PaintEventArgs e)
        {
            if (_estadoCuadranteOcr == EstadoCuadranteOcr.Ninguno)
                return;

            if (_rectanguloSeleccionVistaOcr == Rectangle.Empty)
                return;

            if (!CuadranteOcrTieneTamanioValido(_rectanguloSeleccionVistaOcr))
                return;

            Color color = ObtenerColorCuadranteOcr();
            Color relleno = Color.FromArgb(45, color);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (Brush brush = new SolidBrush(relleno))
            {
                e.Graphics.FillRectangle(brush, _rectanguloSeleccionVistaOcr);
            }

            using (Pen pen = new Pen(color, 2))
            {
                if (_estadoCuadranteOcr == EstadoCuadranteOcr.SugeridoAzul)
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                e.Graphics.DrawRectangle(pen, _rectanguloSeleccionVistaOcr);
            }
        }

        private Color ObtenerColorCuadranteOcr()
        {
            switch (_estadoCuadranteOcr)
            {
                case EstadoCuadranteOcr.SugeridoAzul:
                    return Color.DodgerBlue;

                case EstadoCuadranteOcr.DibujandoManual:
                    return Color.DodgerBlue;

                case EstadoCuadranteOcr.MoviendoSugerido:
                    return Color.DeepSkyBlue;

                case EstadoCuadranteOcr.ConfirmadoVerde:
                    return Color.LimeGreen;

                case EstadoCuadranteOcr.ErrorNaranja:
                    return Color.Orange;

                default:
                    return Color.Transparent;
            }
        }

        private Rectangle CrearRectanguloNormalizado(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int width = Math.Abs(p1.X - p2.X);
            int height = Math.Abs(p1.Y - p2.Y);

            return new Rectangle(x, y, width, height);
        }

       private async Task ProcesarCuadranteSeleccionadoOcr()
{
    if (_procesandoCuadranteOcr)
        return;

    Control destino = _controlDestinoOcrActivo;

    if (destino == null)
    {
        InformarEstadoOcr("No hay un campo activo para pegar el OCR.");
        _estadoCuadranteOcr = EstadoCuadranteOcr.ErrorNaranja;
        pictureBox1.Invalidate();
        return;
    }

    if (!CuadranteOcrTieneTamanioValido(_rectanguloSeleccionVistaOcr))
    {
        InformarEstadoOcr("El cuadrante OCR es demasiado chico.");
        _estadoCuadranteOcr = EstadoCuadranteOcr.ErrorNaranja;
        pictureBox1.Invalidate();
        return;
    }

    bool finalizoEnExitoYAvanzo = false;

    try
    {
        _procesandoCuadranteOcr = true;
        Cursor = Cursors.WaitCursor;

        Debug.WriteLine("[OCR CUADRANTE] Procesando campo: " + destino.Name);
        Debug.WriteLine("[OCR CUADRANTE] Rectángulo vista: " + _rectanguloSeleccionVistaOcr);

                Rectangle rectanguloVistaOcr = _rectanguloSeleccionVistaOcr;
                Size tamanioPictureBox = pictureBox1.ClientSize;
                PictureBoxSizeMode sizeMode = pictureBox1.SizeMode;

                Bitmap imagenParaOcr = ClonarImagenActualPictureBoxParaOcr();

                if (imagenParaOcr == null)
                {
                    InformarEstadoOcr("No se pudo preparar la imagen para OCR.");
                    _estadoCuadranteOcr = EstadoCuadranteOcr.ErrorNaranja;
                    pictureBox1.Invalidate();

                    destino.Focus();
                    return;
                }

                ResultadoOcrCuadrante resultado;

                using (imagenParaOcr)
                {
                    resultado = await Task.Run(() =>
                    {
                        return _ocrPorCuadranteService.LeerTextoDesdeSeleccionDetalle(
                            imagenParaOcr,
                            tamanioPictureBox,
                            sizeMode,
                            rectanguloVistaOcr,
                            guardarDebug: true);
                    });
                }

                InformarEstadoOcr(resultado.MensajeTecnico);

        Debug.WriteLine("[OCR CUADRANTE] Mensaje técnico:");
        Debug.WriteLine(resultado.MensajeTecnico);
        Debug.WriteLine("[OCR CUADRANTE] Rectángulo imagen:");
        Debug.WriteLine(resultado.RectanguloImagen.ToString());
        Debug.WriteLine("[OCR CUADRANTE] Recorte:");
        Debug.WriteLine(resultado.AnchoRecorte + "x" + resultado.AltoRecorte);

        if (!resultado.HayTexto)
        {
            Debug.WriteLine("[OCR CUADRANTE] Sin texto detectado.");

            _estadoCuadranteOcr = EstadoCuadranteOcr.ErrorNaranja;
            pictureBox1.Invalidate();

            destino.Focus();
            return;
        }

        ResultadoAplicacionOcr resultadoAplicacion = PrepararTextoOcrParaDestino(
            destino,
            resultado.Texto,
            resultado.ConfianzaPromedio);

        Debug.WriteLine("[OCR CUADRANTE] Texto crudo:");
        Debug.WriteLine(resultado.Texto);
        Debug.WriteLine("[OCR CUADRANTE] Texto preparado:");
        Debug.WriteLine(resultadoAplicacion.TextoPreparado);
        Debug.WriteLine("[OCR CUADRANTE] Campo destino: " + destino.Name);
        Debug.WriteLine("[OCR CUADRANTE] Confianza OCR: " + resultado.ConfianzaPromedio);
        Debug.WriteLine("[OCR CUADRANTE] Aceptado: " + resultadoAplicacion.Aceptado);
        Debug.WriteLine("[OCR CUADRANTE] Mensaje: " + resultadoAplicacion.Mensaje);

        if (!resultadoAplicacion.Aceptado)
        {
            InformarEstadoOcr(resultadoAplicacion.Mensaje);
            MarcarCampo(destino, ResultadoValidacion.Advertencia(resultadoAplicacion.Mensaje));

            _estadoCuadranteOcr = EstadoCuadranteOcr.ErrorNaranja;
            pictureBox1.Invalidate();

            destino.Focus();
            return;
        }

        AplicarTextoCapturadoOcr(destino, resultadoAplicacion.TextoPreparado);
        ValidarCampoLuegoDeOcr(destino);

        // Marca visualmente el cuadrante como confirmado.
        _estadoCuadranteOcr = EstadoCuadranteOcr.ConfirmadoVerde;
        pictureBox1.Invalidate();

        Debug.WriteLine("[OCR CUADRANTE] OCR aceptado. Se libera bloqueo y se avanza al siguiente campo.");

        // ======================================================
        // CLAVE:
        // Liberamos ANTES de mover foco.
        // Si no, el Enter del siguiente campo no dibuja el azul.
        // ======================================================
        _procesandoCuadranteOcr = false;
        Cursor = Cursors.Default;

        finalizoEnExitoYAvanzo = true;

        MoverFocoCud(destino, true);
    }
    catch (Exception ex)
    {
        InformarEstadoOcr("Error OCR cuadrante: " + ex.Message);
        Debug.WriteLine("[OCR CUADRANTE][ERROR] " + ex);

        _estadoCuadranteOcr = EstadoCuadranteOcr.ErrorNaranja;
        pictureBox1.Invalidate();

        if (destino != null)
            destino.Focus();
    }
    finally
    {
        if (!finalizoEnExitoYAvanzo)
        {
            _procesandoCuadranteOcr = false;
            Cursor = Cursors.Default;
        }
    }
}


        private void InformarEstadoOcr(string mensaje)
        {
            if (string.IsNullOrWhiteSpace(mensaje))
                return;

            Debug.WriteLine("[OCR CUADRANTE] " + mensaje);

            // No es intrusivo: queda como ayuda al pasar el mouse sobre el PDF.
            _toolTipCud.SetToolTip(pictureBox1, mensaje);
        }


        private void ValidarCampoLuegoDeOcr(Control destino)
        {
            if (destino == TxtNumeCert)
            {
                TxtNumeCert_LeaveCud(TxtNumeCert, EventArgs.Empty);
            }
            else if (destino == TxtApellidoNombre)
            {
                TxtApellidoNombre_LeaveCud(TxtApellidoNombre, EventArgs.Empty);
            }
            else if (destino == TxtNumDNI)
            {
                TxtNumDNI_LeaveCud(TxtNumDNI, EventArgs.Empty);
            }
            else if (destino == TxtNumCuil)
            {
                TxtNumCuil_LeaveCud(TxtNumCuil, EventArgs.Empty);
            }
            else if (destino == TxtFechaNac || destino == TxtFechaEmision || destino == TxtFechaVencimiento)
            {
                Fecha_LeaveCud(destino, EventArgs.Empty);
            }
            else if (destino == TxtDiagnostico)
            {
                TextoLibre_LeaveCud(TxtDiagnostico, EventArgs.Empty);
            }
            else if (destino == TxtOrientacion)
            {
                TxtOrientacion_LeaveCud(TxtOrientacion, EventArgs.Empty);
            }
            else if (destino == TxtFuncionesCorporales ||
                     destino == TxtEstructurasCorporales ||
                     destino == TxtActividad_Particip ||
                     destino == TxtFactoresAmbientales)
            {
                CodigoCif_LeaveCud(destino, EventArgs.Empty);
            }
            else if (destino == CmbSexo || destino == CmbAcomapante)
            {
                Combo_ChangedCud(destino, EventArgs.Empty);
            }
        }
        private ResultadoAplicacionOcr PrepararTextoOcrParaDestino(Control destino, string textoOcr, float confianza)
        {
            string crudo = textoOcr ?? string.Empty;

            if (destino == TxtNumeCert)
                return PrepararOcrNumeroCertificado(crudo, confianza);

            if (destino == TxtNumDNI)
                return PrepararOcrDni(crudo, confianza);

            if (destino == TxtNumCuil)
                return PrepararOcrCuil(crudo, confianza);

            if (destino == TxtFechaNac || destino == TxtFechaEmision || destino == TxtFechaVencimiento)
                return PrepararOcrFecha(destino, crudo, confianza);

            if (destino == TxtFuncionesCorporales)
                return PrepararOcrCif(crudo, 'b', "Funciones corporales", confianza);

            if (destino == TxtEstructurasCorporales)
                return PrepararOcrCif(crudo, 's', "Estructuras corporales", confianza);

            if (destino == TxtActividad_Particip)
                return PrepararOcrCif(crudo, 'd', "Actividad y participación", confianza);

            if (destino == TxtFactoresAmbientales)
                return PrepararOcrCif(crudo, 'e', "Factores ambientales", confianza);

            if (destino == TxtApellidoNombre)
                return PrepararOcrTextoLibre(crudo, "Apellido y nombre", confianza, minimoCaracteres: 4);

            if (destino == TxtDiagnostico)
                return PrepararOcrDiagnostico(crudo, confianza);

            if (destino == TxtOrientacion)
                return PrepararOcrTextoLibre(crudo, "Orientación prestacional", confianza, minimoCaracteres: 5);

            if (destino == CmbSexo)
                return PrepararOcrSexo(crudo);

            if (destino == CmbAcomapante)
                return PrepararOcrSiNo(crudo);

            return ResultadoAplicacionOcr.Rechazar("Campo destino no soportado para OCR.");
        }



        private ResultadoAplicacionOcr PrepararOcrNumeroCertificado(string textoOcr, float confianza)
        {
            string t = textoOcr.ToUpperInvariant();

            t = t.Replace("—", "-")
                 .Replace("–", "-")
                 .Replace("_", "-")
                 .Replace(" ", "");

            Match m = Regex.Match(
                t,
                @"ARG-\d{2}-\d{11}-\d{8}-\d{8}-[A-Z]{3}-\d{3}",
                RegexOptions.IgnoreCase);

            if (!m.Success)
                return ResultadoAplicacionOcr.Rechazar("El OCR no detectó un número de certificado válido.", t);

            string cert = IndexadorCudService.NormalizarNumeroCertificado(m.Value);
            DatosNumeroCertificadoCud datos = IndexadorCudService.ParseNumeroCertificado(cert);

            if (!datos.EsValido)
                return ResultadoAplicacionOcr.Rechazar(datos.Mensaje, cert);

            return ResultadoAplicacionOcr.Ok(cert, "Número de certificado OCR válido.");
        }
        private ResultadoAplicacionOcr PrepararOcrDni(string textoOcr, float confianza)
        {
            string digitos = ExtraerDigitos(textoOcr);

            if (digitos.Length > 8)
            {
                // Si vino un CUIL por error, intenta tomar DNI interno.
                if (digitos.Length == 11)
                    digitos = digitos.Substring(2, 8);
                else
                    digitos = digitos.Substring(digitos.Length - 8);
            }

            digitos = digitos.TrimStart('0');

            ResultadoValidacion validacion = IndexadorCudService.ValidarDni(digitos, true);

            if (!validacion.EsValido)
                return ResultadoAplicacionOcr.Rechazar("El OCR no produjo un DNI válido.", digitos);

            return ResultadoAplicacionOcr.Ok(digitos, "DNI OCR válido.");
        }
        private ResultadoAplicacionOcr PrepararOcrCuil(string textoOcr, float confianza)
        {
            string digitos = ExtraerDigitos(textoOcr);

            if (digitos.Length > 11)
                digitos = digitos.Substring(0, 11);

            if (digitos.Length == 2)
                return ResultadoAplicacionOcr.Ok(digitos, "Prefijo CUIL detectado. Se completará con DNI.");

            ResultadoValidacion validacion = IndexadorCudService.ValidarCuil(digitos, TxtNumDNI.Text, true);

            if (!validacion.EsValido)
                return ResultadoAplicacionOcr.Rechazar(validacion.Mensaje, digitos);

            return ResultadoAplicacionOcr.Ok(digitos, "CUIL OCR válido.");
        }
        private ResultadoAplicacionOcr PrepararOcrFecha(Control destino, string textoOcr, float confianza)
        {
            string fecha = ExtraerFechaProbable(destino, textoOcr);

            if (string.IsNullOrWhiteSpace(fecha))
                return ResultadoAplicacionOcr.Rechazar("El OCR no produjo una fecha reconocible.", textoOcr);

            TipoFechaCud tipoFecha;

            if (destino == TxtFechaNac)
                tipoFecha = TipoFechaCud.Nacimiento;
            else if (destino == TxtFechaEmision)
                tipoFecha = TipoFechaCud.Emision;
            else
                tipoFecha = TipoFechaCud.Vencimiento;

            fecha = IndexadorCudService.NormalizarFechaCud(fecha, tipoFecha);

            ResultadoValidacion validacion;

            if (destino == TxtFechaNac)
            {
                validacion = IndexadorCudService.ValidarFecha(
                    fecha,
                    "Fecha de nacimiento",
                    true,
                    false,
                    new DateTime(1900, 1, 1),
                    DateTime.Today);
            }
            else if (destino == TxtFechaEmision)
            {
                validacion = IndexadorCudService.ValidarFecha(
                    fecha,
                    "Fecha de emisión",
                    true,
                    false,
                    new DateTime(1900, 1, 1),
                    DateTime.Today.AddDays(1));
            }
            else
            {
                validacion = IndexadorCudService.ValidarFecha(
                    fecha,
                    "Fecha de vencimiento",
                    true,
                    true,
                    new DateTime(1900, 1, 1),
                    null);
            }

            if (!validacion.EsValido)
                return ResultadoAplicacionOcr.Rechazar(validacion.Mensaje, fecha);

            return ResultadoAplicacionOcr.Ok(fecha, "Fecha OCR válida.");
        }


        private ResultadoAplicacionOcr PrepararOcrCif(string textoOcr, char letraEsperada, string nombreCampo, float confianza)
        {
            string preparado = IndexadorCudService.PrepararTextoCifDesdeOcr(
                textoOcr,
                letraEsperada);

            Debug.WriteLine("[OCR CIF] Campo: " + nombreCampo);
            Debug.WriteLine("[OCR CIF] Letra esperada: " + letraEsperada);
            Debug.WriteLine("[OCR CIF] Texto OCR crudo:");
            Debug.WriteLine(textoOcr);
            Debug.WriteLine("[OCR CIF] Texto preparado:");
            Debug.WriteLine(preparado);

            ResultadoValidacion validacion = IndexadorCudService.ValidarCodigosCif(
                preparado,
                letraEsperada,
                nombreCampo,
                true);

            if (!validacion.EsValido)
            {
                Debug.WriteLine("[OCR CIF] Rechazado: " + validacion.Mensaje);

                return ResultadoAplicacionOcr.Rechazar(
                    "El OCR no produjo códigos CIF válidos para " + nombreCampo + ".",
                    preparado);
            }

            return ResultadoAplicacionOcr.Ok(preparado, "CIF OCR válido.");
        }


        private ResultadoAplicacionOcr PrepararOcrDiagnostico(string textoOcr, float confianza)
        {
            string t = LimpiarDiagnosticoDesdeOcr(textoOcr);

            Debug.WriteLine("[OCR DIAGNOSTICO] Texto OCR crudo:");
            Debug.WriteLine(textoOcr);
            Debug.WriteLine("[OCR DIAGNOSTICO] Texto limpio:");
            Debug.WriteLine(t);

            if (string.IsNullOrWhiteSpace(t))
                return ResultadoAplicacionOcr.Rechazar("El OCR no detectó texto útil para Diagnóstico.");

            if (t.Length < 3)
                return ResultadoAplicacionOcr.Rechazar("El texto OCR para Diagnóstico es demasiado corto.", t);

            // Mismo criterio bajo que venías usando para texto libre.
            if (confianza < 0.25f)
            {
                return ResultadoAplicacionOcr.Rechazar(
                    "Confianza OCR muy baja para Diagnóstico. Revise el recorte o amplíe la selección.",
                    t);
            }

            return ResultadoAplicacionOcr.Ok(t, "Diagnóstico OCR aceptado.");
        }
        private string LimpiarDiagnosticoDesdeOcr(string textoOcr)
        {
            if (string.IsNullOrWhiteSpace(textoOcr))
                return string.Empty;

            string texto = textoOcr.Replace("\r\n", "\n")
                                   .Replace("\r", "\n");

            string[] lineas = texto.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> lineasUtiles = new List<string>();

            foreach (string lineaOriginal in lineas)
            {
                string linea = lineaOriginal.Trim();

                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                linea = linea.Replace("—", "-")
                             .Replace("–", "-")
                             .Replace("_", " ")
                             .Replace("¬", " ")
                             .Replace("°", " ");

                linea = Regex.Replace(linea, @"[-]{2,}", " ");
                linea = Regex.Replace(linea, @"\s+", " ").Trim();

                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                if (EsLineaRuidoDiagnosticoOcr(linea))
                {
                    Debug.WriteLine("[OCR DIAGNOSTICO] Línea descartada por ruido: " + linea);
                    continue;
                }

                lineasUtiles.Add(linea);
            }

            string resultado = string.Join(" ", lineasUtiles);

            resultado = resultado.Replace("'", "");
            resultado = Regex.Replace(resultado, @"\s+", " ").Trim();

            // Limpieza final de separadores colgados al principio o al final.
            resultado = Regex.Replace(resultado, @"^[\-\.:;, ]+", "").Trim();
            resultado = Regex.Replace(resultado, @"[\-\.:;, ]+$", "").Trim();

            return resultado;
        }

        private bool EsLineaRuidoDiagnosticoOcr(string linea)
        {
            if (string.IsNullOrWhiteSpace(linea))
                return true;

            string l = linea.Trim();

            // Líneas hechas solo de separadores.
            if (Regex.IsMatch(l, @"^[\-\._=~:,;|\\/ ]+$"))
                return true;

            string soloLetrasNumeros = Regex.Replace(l, @"[^a-zA-ZáéíóúÁÉÍÓÚñÑ0-9]", "");

            if (string.IsNullOrWhiteSpace(soloLetrasNumeros))
                return true;

            // Caso del log: "rs".
            // También descarta residuos tipo "fs", "rn", "ll", etc.
            // No descarta siglas con vocales, por ejemplo TEA.
            if (soloLetrasNumeros.Length <= 3 &&
                !Regex.IsMatch(soloLetrasNumeros, @"[aeiouáéíóúAEIOUÁÉÍÓÚ]") &&
                !Regex.IsMatch(soloLetrasNumeros, @"\d"))
            {
                return true;
            }

            // Líneas muy cortas con solo una letra o número suelto.
            if (soloLetrasNumeros.Length == 1)
                return true;

            return false;
        }



        private ResultadoAplicacionOcr PrepararOcrTextoLibre(
    string textoOcr,
    string nombreCampo,
    float confianza,
    int minimoCaracteres)
        {
            string t = IndexadorCudService.NormalizarTextoLibre(textoOcr);

            t = LimpiarRuidoTextoLibreOcr(t);

            if (string.IsNullOrWhiteSpace(t))
                return ResultadoAplicacionOcr.Rechazar("El OCR no detectó texto útil para " + nombreCampo + ".");

            if (t.Length < minimoCaracteres)
                return ResultadoAplicacionOcr.Rechazar("El texto OCR para " + nombreCampo + " es demasiado corto.", t);

            // Umbral bajo porque Tesseract puede dar confianza baja en texto correcto.
            if (confianza < 0.25f)
            {
                return ResultadoAplicacionOcr.Rechazar(
                    "Confianza OCR muy baja para " + nombreCampo + ". Revise el recorte o amplíe la selección.",
                    t);
            }

            if (nombreCampo == "Apellido y nombre")
                t = IndexadorCudService.NormalizarNombreApellido(t);

            if (nombreCampo == "Orientación prestacional")
                t = IndexadorCudService.NormalizarOrientacion(t);

            return ResultadoAplicacionOcr.Ok(t, nombreCampo + " OCR aceptado.");
        }
        private string LimpiarRuidoTextoLibreOcr(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim();

            t = t.Replace("—", " ")
                 .Replace("_", " ")
                 .Replace("¬", " ")
                 .Replace("°", " ");

            t = Regex.Replace(t, @"[-_]{3,}", " ");
            t = Regex.Replace(t, @"\s+", " ");

            return t.Trim();
        }
        private ResultadoAplicacionOcr PrepararOcrSexo(string textoOcr)
        {
            string sexo = ExtraerSexoDesdeOcr(textoOcr);

            Debug.WriteLine("[OCR SEXO] Texto OCR crudo:");
            Debug.WriteLine(textoOcr);
            Debug.WriteLine("[OCR SEXO] Sexo detectado: " + sexo);

            if (sexo == "FEMENINO" || sexo == "MASCULINO" || sexo == "X" || sexo == "NO ESPECIFICA")
                return ResultadoAplicacionOcr.Ok(sexo, "Sexo OCR válido.");

            return ResultadoAplicacionOcr.Rechazar(
                "El OCR no detectó un valor válido para Sexo.",
                textoOcr);
        }

        private string ExtraerSexoDesdeOcr(string textoOcr)
        {
            if (string.IsNullOrWhiteSpace(textoOcr))
                return string.Empty;

            string t = textoOcr.ToUpperInvariant();

            t = t.Replace("Á", "A")
                 .Replace("É", "E")
                 .Replace("Í", "I")
                 .Replace("Ó", "O")
                 .Replace("Ú", "U")
                 .Replace("—", " ")
                 .Replace("–", " ")
                 .Replace("-", " ")
                 .Replace("_", " ")
                 .Replace("|", " ")
                 .Replace("/", " ")
                 .Replace("\\", " ")
                 .Replace(":", " ")
                 .Replace(";", " ")
                 .Replace(",", " ")
                 .Replace(".", " ")
                 .Replace("(", " ")
                 .Replace(")", " ")
                 .Replace("[", " ")
                 .Replace("]", " ")
                 .Replace("\"", " ")
                 .Replace("“", " ")
                 .Replace("”", " ")
                 .Replace("'", " ");

            t = Regex.Replace(t, @"\s+", " ").Trim();

            // Primero detectamos palabras completas si vinieran así.
            if (Regex.IsMatch(t, @"\bFEMENINO\b"))
                return "FEMENINO";

            if (Regex.IsMatch(t, @"\bMASCULINO\b"))
                return "MASCULINO";

            if (Regex.IsMatch(t, @"\bNO\s+ESPECIFICA\b"))
                return "NO ESPECIFICA";

            // Después detectamos letras aisladas.
            // Caso real:
            // "— ON... - M- 56577647" -> MASCULINO
            if (Regex.IsMatch(t, @"\bM\b"))
                return "MASCULINO";

            if (Regex.IsMatch(t, @"\bF\b"))
                return "FEMENINO";

            if (Regex.IsMatch(t, @"\bX\b"))
                return "X";

            return string.Empty;
        }

        private ResultadoAplicacionOcr PrepararOcrSiNo(string textoOcr)
        {
            string valor = ExtraerSiNoDesdeOcr(textoOcr);

            Debug.WriteLine("[OCR SI/NO] Texto OCR crudo:");
            Debug.WriteLine(textoOcr);
            Debug.WriteLine("[OCR SI/NO] Valor detectado: " + valor);

            if (valor == "SI" || valor == "NO")
                return ResultadoAplicacionOcr.Ok(valor, "Acompañante OCR válido.");

            return ResultadoAplicacionOcr.Rechazar(
                "El OCR no detectó SI/NO para acompañante.",
                textoOcr);
        }
        private string ExtraerSiNoDesdeOcr(string textoOcr)
        {
            if (string.IsNullOrWhiteSpace(textoOcr))
                return string.Empty;

            string t = textoOcr.ToUpperInvariant();

            t = t.Replace("Í", "I")
                 .Replace("Ì", "I")
                 .Replace("Ï", "I")
                 .Replace("1", "I")
                 .Replace("|", " ")
                 .Replace(":", " ")
                 .Replace(";", " ")
                 .Replace(",", " ")
                 .Replace(".", " ")
                 .Replace("_", " ")
                 .Replace("-", " ")
                 .Replace("—", " ")
                 .Replace("–", " ")
                 .Replace("\"", " ")
                 .Replace("“", " ")
                 .Replace("”", " ")
                 .Replace("'", " ");

            t = Regex.Replace(t, @"\s+", " ").Trim();

            // Busca SI/SÍ o NO como palabra aislada.
            // Esto evita tomar "SI" dentro de palabras más largas.
            Match m = Regex.Match(t, @"\b(SI|NO)\b", RegexOptions.IgnoreCase);

            if (m.Success)
                return m.Value.ToUpperInvariant();

            return string.Empty;
        }

        private string ExtraerDigitos(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            return new string(texto.Where(char.IsDigit).ToArray());
        }

        private string ExtraerFechaProbable(Control destino, string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim();

            Debug.WriteLine("[OCR FECHA] Texto recibido en extractor:");
            Debug.WriteLine(t);

            t = t.Replace("O", "0")
                 .Replace("o", "0")
                 .Replace("I", "1")
                 .Replace("l", "1")
                 .Replace("|", "1")
                 .Replace(":", "/")
                 .Replace("-", "/")
                 .Replace(".", "/");

            t = Regex.Replace(t, @"\s+", " ").Trim();

            Debug.WriteLine("[OCR FECHA] Texto normalizado para búsqueda:");
            Debug.WriteLine(t);

            // ==========================================================
            // 1) Fecha completa directa:
            // 10/05/2028
            // 10-05-2028 ya llega como 10/05/2028
            // ==========================================================
            Match mSeparada = Regex.Match(t, @"\b\d{1,2}/\d{1,2}/\d{4}\b");

            if (mSeparada.Success)
            {
                Debug.WriteLine("[OCR FECHA] Detectó fecha completa: " + mSeparada.Value);
                return mSeparada.Value;
            }

            // ==========================================================
            // 2) Fecha compacta:
            // 20280510 o 10052028
            // ==========================================================
            Match mCompacta = Regex.Match(t, @"\b\d{8}\b");

            if (mCompacta.Success)
            {
                Debug.WriteLine("[OCR FECHA] Detectó fecha compacta: " + mCompacta.Value);
                return mCompacta.Value;
            }

            // ==========================================================
            // 3) Fecha completa con espacios raros:
            // Ejemplo: 1/ 05/2028
            // Esto conviene evaluarlo ANTES que mes/año,
            // porque si no podría tomar solo 05/2028.
            // ==========================================================
            Match mParcialConDia = Regex.Match(t, @"\b(\d{1,2})/\s*(\d{1,2})/\s*(\d{4})\b");

            if (mParcialConDia.Success)
            {
                string fecha = mParcialConDia.Groups[1].Value.PadLeft(2, '0') + "/" +
                               mParcialConDia.Groups[2].Value.PadLeft(2, '0') + "/" +
                               mParcialConDia.Groups[3].Value;

                Debug.WriteLine("[OCR FECHA] Detectó fecha con espacios raros: " + fecha);
                return fecha;
            }

            // ==========================================================
            // 4) Caso parcial con mes/año:
            // ACTUALIZACIÓN: 05-2028
            //
            // Después de normalizar:
            // ACTUALIZACIÓN/ 05/2028
            //
            // Regla:
            // - Si hay número de certificado válido y mes/año coinciden,
            //   usa el día del certificado.
            //
            // - Si NO hay certificado cargado o válido,
            //   completa con día 01.
            //
            // - Si hay certificado válido pero mes/año NO coinciden,
            //   rechaza para no inventar una fecha contradictoria.
            // ==========================================================
            Match mMesAnio = Regex.Match(t, @"\b(0?[1-9]|1[0-2])/\s*(\d{4})\b");

            if (mMesAnio.Success)
            {
                int mesOcr = Convert.ToInt32(mMesAnio.Groups[1].Value);
                int anioOcr = Convert.ToInt32(mMesAnio.Groups[2].Value);

                Debug.WriteLine("[OCR FECHA] Detectó mes/año OCR: " + mesOcr.ToString("00") + "/" + anioOcr);

                // Para fecha de nacimiento no completamos día automáticamente.
                // Solo tiene sentido para emisión o vencimiento.
                if (destino != TxtFechaEmision && destino != TxtFechaVencimiento)
                {
                    Debug.WriteLine("[OCR FECHA] Mes/año detectado, pero el campo no permite completar día automáticamente.");
                    return string.Empty;
                }

                string fechaDesdeCertificado = ObtenerFechaDesdeCertificadoParaCampo(destino);

                Debug.WriteLine("[OCR FECHA] Fecha desde certificado para este campo: " + fechaDesdeCertificado);

                if (!string.IsNullOrWhiteSpace(fechaDesdeCertificado))
                {
                    DateTime fechaCert;

                    if (DateTime.TryParseExact(
                        fechaDesdeCertificado,
                        "dd/MM/yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out fechaCert))
                    {
                        Debug.WriteLine("[OCR FECHA] Mes/año certificado: " + fechaCert.ToString("MM/yyyy"));

                        if (fechaCert.Month == mesOcr && fechaCert.Year == anioOcr)
                        {
                            Debug.WriteLine("[OCR FECHA] Mes/año coincide. Se usa fecha completa del certificado: " + fechaDesdeCertificado);
                            return fechaDesdeCertificado;
                        }

                        Debug.WriteLine("[OCR FECHA] Mes/año OCR no coincide con certificado. No se inventa día.");
                        Debug.WriteLine("[OCR FECHA] OCR: " + mesOcr.ToString("00") + "/" + anioOcr);
                        Debug.WriteLine("[OCR FECHA] Certificado: " + fechaCert.ToString("MM/yyyy"));

                        return string.Empty;
                    }

                    Debug.WriteLine("[OCR FECHA] No se pudo interpretar la fecha del certificado.");
                }

                // ======================================================
                // Si no hay certificado válido, usamos día 01.
                // Ejemplo:
                // ACTUALIZACIÓN: 05-2028 -> 01/05/2028
                // ======================================================
                string fechaConDiaPrimero =
                    "01/" + mesOcr.ToString("00") + "/" + anioOcr;

                Debug.WriteLine("[OCR FECHA] No hay certificado válido. Se completa con primer día del mes: " + fechaConDiaPrimero);

                return fechaConDiaPrimero;
            }

            Debug.WriteLine("[OCR FECHA] No se detectó ningún patrón de fecha.");
            return string.Empty;
        }

        private string ObtenerFechaDesdeCertificadoParaCampo(Control destino)
        {
            DatosNumeroCertificadoCud datos =
                IndexadorCudService.ParseNumeroCertificado(TxtNumeCert.Text);

            if (datos == null || !datos.EsValido)
                return string.Empty;

            if (destino == TxtFechaEmision)
                return datos.FechaEmision;

            if (destino == TxtFechaVencimiento)
                return datos.FechaVencimiento;

            return string.Empty;
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



        private void txtNombre_Madre_TabStopChanged(object sender, EventArgs e)
        {
            btnGrabar.Focus();
        }

        private class ResultadoAplicacionOcr
        {
            public bool Aceptado { get; set; }
            public string TextoPreparado { get; set; }
            public string Mensaje { get; set; }

            public static ResultadoAplicacionOcr Ok(string texto, string mensaje = "")
            {
                return new ResultadoAplicacionOcr
                {
                    Aceptado = true,
                    TextoPreparado = texto,
                    Mensaje = mensaje
                };
            }

            public static ResultadoAplicacionOcr Rechazar(string mensaje, string texto = "")
            {
                return new ResultadoAplicacionOcr
                {
                    Aceptado = false,
                    TextoPreparado = texto,
                    Mensaje = mensaje
                };
            }
        }
        private string ObtenerNombreIndexadorActual()
        {
            string usuario = string.Empty;

            if (txtUsuario != null && !string.IsNullOrWhiteSpace(txtUsuario.Text))
            {
                usuario = txtUsuario.Text
                    .Replace("Usuario:", "")
                    .Replace("Usuario", "")
                    .Trim();
            }

            // Fallback al objeto que recibís en el constructor.
            if (string.IsNullOrWhiteSpace(usuario) && oIndexador != null)
            {
                usuario = oIndexador.NombreIndexador == null
                    ? string.Empty
                    : oIndexador.NombreIndexador.Trim();
            }

            return usuario;
        }

        private bool ClickDentroDelCuadranteOcr(Point punto)
        {
            if (_rectanguloSeleccionVistaOcr == Rectangle.Empty)
                return false;

            if (_estadoCuadranteOcr == EstadoCuadranteOcr.Ninguno)
                return false;

            Rectangle zonaClick = _rectanguloSeleccionVistaOcr;
            zonaClick.Inflate(6, 6);

            return zonaClick.Contains(punto);
        }
        private bool CuadranteOcrTieneTamanioValido(Rectangle rect)
        {
            return rect.Width >= MIN_ANCHO_CUADRANTE_OCR &&
                   rect.Height >= MIN_ALTO_CUADRANTE_OCR;
        }
        private Rectangle LimitarRectanguloAlAreaImagenVisible(Rectangle rect)
        {
            Rectangle areaImagen = ObtenerRectanguloImagenVisibleEnPictureBox();

            if (areaImagen == Rectangle.Empty)
                return Rectangle.Empty;

            if (rect.Width <= 0 || rect.Height <= 0)
                return Rectangle.Empty;

            Rectangle interseccion = Rectangle.Intersect(rect, areaImagen);

            if (interseccion.Width <= 0 || interseccion.Height <= 0)
                return Rectangle.Empty;

            int ancho = Math.Min(rect.Width, areaImagen.Width);
            int alto = Math.Min(rect.Height, areaImagen.Height);

            int x = rect.X;
            int y = rect.Y;

            if (x < areaImagen.Left)
                x = areaImagen.Left;

            if (y < areaImagen.Top)
                y = areaImagen.Top;

            if (x + ancho > areaImagen.Right)
                x = areaImagen.Right - ancho;

            if (y + alto > areaImagen.Bottom)
                y = areaImagen.Bottom - alto;

            return new Rectangle(x, y, ancho, alto);
        }
        private void OcultarCuadranteOcr()
        {
            _estadoCuadranteOcr = EstadoCuadranteOcr.Ninguno;

            _seleccionandoCuadranteOcr = false;
            _moviendoCuadranteSugeridoOcr = false;

            _rectanguloSeleccionVistaOcr = Rectangle.Empty;
            _puntoInicioCuadranteOcr = Point.Empty;
            _offsetMovimientoCuadranteOcr = Point.Empty;

            if (pictureBox1 != null)
                pictureBox1.Invalidate();
        }



        private void LimpiarCampoCud(Control control)
        {
            if (control == null)
                return;

            _cargandoDatosCud = true;

            TextBox txt = control as TextBox;
            if (txt != null)
            {
                txt.Clear();
            }

            ComboBox cmb = control as ComboBox;
            if (cmb != null)
            {
                if (cmb.Items.Count > 0)
                    cmb.SelectedIndex = 0;
                else
                    cmb.Text = string.Empty;
            }

            _cargandoDatosCud = false;

            // Limpia marca visual/error sin disparar validación obligatoria.
            control.BackColor = _colorOk;
            _erroresCud.SetError(control, string.Empty);
            _toolTipCud.SetToolTip(control, string.Empty);

            control.Focus();

            TextBox txtFoco = control as TextBox;
            if (txtFoco != null)
            {
                txtFoco.SelectionStart = 0;
                txtFoco.SelectionLength = 0;
            }

            ComboBox cmbFoco = control as ComboBox;
            if (cmbFoco != null)
            {
                cmbFoco.SelectionStart = 0;
                cmbFoco.SelectionLength = 0;
            }

            // Como vuelve el foco al campo, dejamos activo el destino OCR.
            _controlDestinoOcrActivo = control;

            // Si ya implementaste el cuadrante sugerido por foco,
            // esto lo vuelve a mostrar para ese campo.
            MostrarCuadranteSugeridoParaControl(control);

            Debug.WriteLine("[LIMPIAR CAMPO CUD] Campo limpiado: " + control.Name);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtDiagnostico);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtFuncionesCorporales);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtEstructurasCorporales);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtActividad_Particip);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtFactoresAmbientales);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtApellidoNombre);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(CmbSexo);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtNumDNI);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtNumCuil);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtFechaNac);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtOrientacion);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtFechaVencimiento);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(CmbAcomapante);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            LimpiarCampoCud(TxtFechaEmision);
        }


    }
}