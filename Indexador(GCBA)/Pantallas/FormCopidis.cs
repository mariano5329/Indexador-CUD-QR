using IndexadorCopidis.Datos;
using IndexadorCopidis.Entidades;
using IndexadorCopidis.Servicios;
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

namespace IndexadorCopidis
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
        private readonly string rutaCudProcesadas = @"J:\COPIDIS\02_PENDIENTES_INDEXAR\";
        private readonly string rutaCudVerificada = @"J:\COPIDIS\03_CERTIFICADOS_VERIFICADOS\";
        private readonly string rutaCudIlegibles = @"J:\COPIDIS\04_ILEGIBLES\";
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

        // ==========================================================
        // CERTIFICADO / VISTA RECORTADA
        // ==========================================================
        // Se muestra y se procesa OCR solo sobre la parte superior útil del CUD.
        // 0.62 mantiene margen para la fecha de emisión, que en tus zonas sugeridas
        // llega aproximadamente hasta Y=0.59 del certificado completo.
 
        private const int DPI_RENDER_VISUAL_CERTIFICADO = 150;
        private const int MAX_LADO_RENDER_VISUAL_CERTIFICADO = 2600;
     
        private const int FACTOR_RENDER_OCR_CUADRANTE = 4;
        private const int DPI_RENDER_OCR_CUADRANTE = 300;
        private const int MAX_LADO_RENDER_OCR_CUADRANTE = 4200;
        private bool _configurandoBloquesCertificado = false;
        private PerfilResolucion _perfilActual = PerfilResolucion.Escala145;
        private const float PORCENTAJE_MARGEN_SUPERIOR_CERTIFICADO = 0.03f;
        private const float PORCENTAJE_ALTO_UTIL_CERTIFICADO = 0.62f;

        // Recorte lateral del certificado.
        // 0.04f = recorta 4% a la izquierda y 4% a la derecha.
        private const float PORCENTAJE_MARGEN_IZQUIERDO_CERTIFICADO = 0.07f;
        private const float PORCENTAJE_MARGEN_DERECHO_CERTIFICADO = 0.04f;

        // Zoom visual real sobre el pictureBox.
        private const float ZOOM_VISUAL_CERTIFICADO = 1.0f;

        // Escalado horizontal limitado para que los TextBox no se ensanchen tanto.
        private const float FACTOR_ESCALA_HORIZONTAL_CONTROLES = 0.35f;



        private bool _lupaActiva = false;
        private Point _posicionLupa = Point.Empty;
        private Rectangle _ultimoRectanguloLupa = Rectangle.Empty;

        private const int LUPA_ANCHO = 800;
        private const int LUPA_ALTO = 100;
        private const float LUPA_FACTOR_ZOOM = 2.0f;


      
        private bool _lupaFija = false;
        private bool _lupaModoFijar = false;

  




        private Bitmap _imagenOcrHdCache = null;
        private string _rutaImagenOcrHdCache = string.Empty;


        private int _cantidadCudIndexadosHoy = 0;
    
        private string _indexadorContadorActual = string.Empty;
        private Bitmap RecortarAreaUtilCertificado(Bitmap imagenCompleta)
        {
            if (imagenCompleta == null)
                return null;

            int margenIzquierdo = (int)(imagenCompleta.Width * PORCENTAJE_MARGEN_IZQUIERDO_CERTIFICADO);
            int margenDerecho = (int)(imagenCompleta.Width * PORCENTAJE_MARGEN_DERECHO_CERTIFICADO);

            int margenSuperior = (int)(imagenCompleta.Height * PORCENTAJE_MARGEN_SUPERIOR_CERTIFICADO);

            int altoHasta = (int)(imagenCompleta.Height * PORCENTAJE_ALTO_UTIL_CERTIFICADO);

            int anchoUtil = imagenCompleta.Width - margenIzquierdo - margenDerecho;
            int altoUtil = altoHasta - margenSuperior;

            if (anchoUtil <= 0 || altoUtil <= 0)
                return new Bitmap(imagenCompleta);

            Rectangle zonaUtil = new Rectangle(
                margenIzquierdo,
                margenSuperior,
                anchoUtil,
                altoUtil
            );

            Bitmap recorte = new Bitmap(zonaUtil.Width, zonaUtil.Height);

            using (Graphics g = Graphics.FromImage(recorte))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                g.DrawImage(
                    imagenCompleta,
                    new Rectangle(0, 0, recorte.Width, recorte.Height),
                    zonaUtil,
                    GraphicsUnit.Pixel);
            }

            return recorte;
        }
        private void AplicarZoomVisualCertificado()
        {
            if (pictureBox1.Image == null)
                return;

            ScrollableControl contenedor = pictureBox1.Parent as ScrollableControl;

            if (contenedor != null)
            {
                contenedor.AutoScroll = true;
            }

            pictureBox1.Dock = DockStyle.None;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            int anchoBase = contenedor != null && contenedor.ClientSize.Width > 0
                ? contenedor.ClientSize.Width
                : pictureBox1.Width;

            int altoBase = contenedor != null && contenedor.ClientSize.Height > 0
                ? contenedor.ClientSize.Height
                : pictureBox1.Height;

            pictureBox1.Width = (int)(anchoBase * ZOOM_VISUAL_CERTIFICADO);
            pictureBox1.Height = (int)(altoBase * ZOOM_VISUAL_CERTIFICADO);

            pictureBox1.Left = 0;
            pictureBox1.Top = 0;
        }
        private enum PerfilResolucion
        {
            Escala100 = 0,
            Escala115 = 1,
            Escala130 = 2,
            Escala145 = 3,
            Escala160 = 4
        }

        private readonly Dictionary<Control, Rectangle> _boundsOriginales = new Dictionary<Control, Rectangle>();
        private readonly Dictionary<Control, float> _fuentesOriginales = new Dictionary<Control, float>();

        private Size _clientSizeOriginal;
        private bool _layoutCapturado = false;
        private bool _aplicandoResolucion = false;




        private enum EstadoCuadranteOcr
        {
            Ninguno,
            SugeridoAzul,
            DibujandoManual,
            MoviendoSugerido,
            ConfirmadoVerde,
            ErrorNaranja
        }

        private readonly Dictionary<Control, RectangleF> _zonasAjustadasOcrCertificadoActual =
    new Dictionary<Control, RectangleF>();



        private Bitmap RenderizarPdfActualAltaDefinicionParaOcr()
        {
            if (string.IsNullOrWhiteSpace(RutaNombreImagen))
            {
                Debug.WriteLine("[OCR HD] RutaNombreImagen vacía. No se puede renderizar PDF HD.");
                return null;
            }

            if (!File.Exists(RutaNombreImagen))
            {
                Debug.WriteLine("[OCR HD] No existe el PDF actual: " + RutaNombreImagen);
                return null;
            }

            if (pictureBox1.Image == null)
            {
                Debug.WriteLine("[OCR HD] pictureBox1.Image es null. No hay referencia visual.");
                return null;
            }

            try
            {
                int anchoBase = pictureBox1.Image.Width;
                int altoBase = pictureBox1.Image.Height;

                if (anchoBase <= 0 || altoBase <= 0)
                    return null;

                int anchoRender = anchoBase * FACTOR_RENDER_OCR_CUADRANTE;
                int altoRender = altoBase * FACTOR_RENDER_OCR_CUADRANTE;

                int ladoMayor = Math.Max(anchoRender, altoRender);

                if (ladoMayor > MAX_LADO_RENDER_OCR_CUADRANTE)
                {
                    float escala = (float)MAX_LADO_RENDER_OCR_CUADRANTE / ladoMayor;

                    anchoRender = Math.Max(1, (int)(anchoRender * escala));
                    altoRender = Math.Max(1, (int)(altoRender * escala));
                }

                using (PdfDocument pdfDocument = PdfDocument.Load(RutaNombreImagen))
                {
                    if (pdfDocument.PageCount <= 0)
                        return null;

                    using (Image render = pdfDocument.Render(
                        0,
                        anchoRender,
                        altoRender,
                        DPI_RENDER_OCR_CUADRANTE,
                        DPI_RENDER_OCR_CUADRANTE,
                        true))
                    {
                        using (Bitmap completo = new Bitmap(render))
                        {
                            return RecortarAreaUtilCertificado(completo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[OCR HD][ERROR] No se pudo renderizar PDF en alta definición.");
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }

        private float ObtenerPorcentajeAltoUtilCertificado()
        {
            if (PORCENTAJE_ALTO_UTIL_CERTIFICADO < 0.30f)
                return 0.30f;

            if (PORCENTAJE_ALTO_UTIL_CERTIFICADO > 1.00f)
                return 1.00f;

            return PORCENTAJE_ALTO_UTIL_CERTIFICADO;
        }

        //private Bitmap CrearBitmapParteAltaCertificado(Image imagenCompleta)
        //{
        //    if (imagenCompleta == null)
        //        return null;

        //    float altoUtil = ObtenerPorcentajeAltoUtilCertificado();

        //    int ancho = imagenCompleta.Width;
        //    int altoRecorte = Math.Max(1, (int)Math.Round(imagenCompleta.Height * altoUtil));

        //    Rectangle zonaSuperior = new Rectangle(0, 0, ancho, altoRecorte);
        //    Bitmap recorte = new Bitmap(zonaSuperior.Width, zonaSuperior.Height);

        //    using (Graphics g = Graphics.FromImage(recorte))
        //    {
        //        g.Clear(Color.White);
        //        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        //        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        //        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        //        g.DrawImage(
        //            imagenCompleta,
        //            new Rectangle(0, 0, recorte.Width, recorte.Height),
        //            zonaSuperior,
        //            GraphicsUnit.Pixel);
        //    }

        //    return recorte;
        //}

        private RectangleF CrearZonaOcrDesdeCertificadoCompleto(float x, float y, float width, float height)
        {
            float xInicio = PORCENTAJE_MARGEN_IZQUIERDO_CERTIFICADO;
            float xFin = 1.0f - PORCENTAJE_MARGEN_DERECHO_CERTIFICADO;
            float anchoVisible = xFin - xInicio;

            float yInicio = PORCENTAJE_MARGEN_SUPERIOR_CERTIFICADO;
            float yFin = ObtenerPorcentajeAltoUtilCertificado();
            float altoVisible = yFin - yInicio;

            if (anchoVisible <= 0f || altoVisible <= 0f)
                return RectangleF.Empty;

            RectangleF zonaOriginal = new RectangleF(x, y, width, height);
            RectangleF areaVisibleOriginal = new RectangleF(
                xInicio,
                yInicio,
                anchoVisible,
                altoVisible
            );

            RectangleF interseccion = RectangleF.Intersect(zonaOriginal, areaVisibleOriginal);

            if (interseccion.Width <= 0f || interseccion.Height <= 0f)
                return RectangleF.Empty;

            float nuevoX = (interseccion.X - xInicio) / anchoVisible;
            float nuevoY = (interseccion.Y - yInicio) / altoVisible;
            float nuevoWidth = interseccion.Width / anchoVisible;
            float nuevoHeight = interseccion.Height / altoVisible;

            return new RectangleF(nuevoX, nuevoY, nuevoWidth, nuevoHeight);
        }

        private Size CalcularTamanioRenderCompletoParaVista(PdfDocument pdfDocument, Size areaDestino)
        {
            float xInicio = PORCENTAJE_MARGEN_IZQUIERDO_CERTIFICADO;
            float xFin = 1.0f - PORCENTAJE_MARGEN_DERECHO_CERTIFICADO;
            float anchoVisible = xFin - xInicio;

            float yInicio = PORCENTAJE_MARGEN_SUPERIOR_CERTIFICADO;
            float yFin = ObtenerPorcentajeAltoUtilCertificado();
            float altoVisible = yFin - yInicio;

            if (anchoVisible <= 0f)
                anchoVisible = 1.0f;

            if (altoVisible <= 0f)
                altoVisible = ObtenerPorcentajeAltoUtilCertificado();

            int anchoDestino = Math.Max(1, areaDestino.Width);
            int altoDestino = Math.Max(1, areaDestino.Height);

            SizeF pageSize = pdfDocument.PageSizes[0];

            float ratioCompleto = pageSize.Height <= 0
                ? 0.707f
                : pageSize.Width / pageSize.Height;

            // Ratio real del área recortada:
            // ancho original * anchoVisible / alto original * altoVisible
            float ratioAreaUtil = ratioCompleto * (anchoVisible / altoVisible);
            float ratioDestino = (float)anchoDestino / altoDestino;

            int anchoAreaUtil;
            int altoAreaUtil;

            if (ratioDestino > ratioAreaUtil)
            {
                altoAreaUtil = altoDestino;
                anchoAreaUtil = Math.Max(1, (int)Math.Round(altoAreaUtil * ratioAreaUtil));
            }
            else
            {
                anchoAreaUtil = anchoDestino;
                altoAreaUtil = Math.Max(1, (int)Math.Round(anchoAreaUtil / ratioAreaUtil));
            }

            // Renderizamos por encima del tamaño visual para mantener nitidez.
            anchoAreaUtil *= 2;
            altoAreaUtil *= 2;

            int anchoCompleto = Math.Max(1, (int)Math.Round(anchoAreaUtil / anchoVisible));
            int altoCompleto = Math.Max(1, (int)Math.Round(altoAreaUtil / altoVisible));

            AjustarTamanioMaximo(
                ref anchoCompleto,
                ref altoCompleto,
                MAX_LADO_RENDER_VISUAL_CERTIFICADO);

            return new Size(anchoCompleto, altoCompleto);
        }

        private void AjustarTamanioMaximo(ref int ancho, ref int alto, int maxLado)
        {
            if (ancho <= 0 || alto <= 0 || maxLado <= 0)
                return;

            int ladoMayor = Math.Max(ancho, alto);

            if (ladoMayor <= maxLado)
                return;

            float escala = (float)maxLado / ladoMayor;

            ancho = Math.Max(1, (int)Math.Round(ancho * escala));
            alto = Math.Max(1, (int)Math.Round(alto * escala));
        }
        private float ObtenerEscalaPerfil(PerfilResolucion perfil)
        {
            switch (perfil)
            {
                case PerfilResolucion.Escala100:
                    return 1.00f;

                case PerfilResolucion.Escala115:
                    return 1.15f;

                case PerfilResolucion.Escala130:
                    return 1.30f;

                case PerfilResolucion.Escala145:
                    return 1.45f;

                case PerfilResolucion.Escala160:
                    return 1.60f;

                default:
                    return 1.45f;
            }
        }



        public FormCopidis(Entidades.Indexador pIndexador)
        {
            InitializeComponent();

    


            CapturarLayoutOriginal();



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

            HabilitarDobleBuffer(pictureBox1);
            this.KeyPreview = true;

            pictureBox1.MouseMove += PictureBox1_MouseMoveLupa;
            pictureBox1.MouseLeave += PictureBox1_MouseLeaveLupa;
            pictureBox1.Paint += PictureBox1_PaintLupa;

            this.KeyDown += FormCopidis_KeyDownLupa;
            this.KeyUp += FormCopidis_KeyUpLupa;
            ConfigurarFormularioCud();
        }
        private void FormCopidis_KeyDownLupa(object sender, KeyEventArgs e)
        {
            if (pictureBox1 == null || pictureBox1.Image == null)
                return;

            bool esControl = e.KeyCode == Keys.ControlKey;
            bool esShift = e.KeyCode == Keys.ShiftKey;

            if (!esControl && !esShift)
                return;

            Point posicionMousePantalla = Control.MousePosition;
            Point posicionMousePictureBox = pictureBox1.PointToClient(posicionMousePantalla);

            bool mouseSobrePictureBox = pictureBox1.ClientRectangle.Contains(posicionMousePictureBox);

            // Si la lupa está fija, CTRL o SHIFT la apagan.
            // Lo hacemos solo si el mouse está sobre el certificado para no molestar al tipear.
            if (_lupaFija && mouseSobrePictureBox)
            {
                DesactivarLupa();
                e.Handled = true;
                return;
            }

            if (!mouseSobrePictureBox)
                return;

            _lupaActiva = true;
            _lupaFija = false;
            _lupaModoFijar = esShift;
            _posicionLupa = posicionMousePictureBox;

            pictureBox1.Invalidate();
        }

        private void FormCopidis_KeyUpLupa(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
            {
                // CTRL es lupa momentánea: al soltar desaparece.
                if (_lupaActiva && !_lupaFija && !_lupaModoFijar)
                {
                    DesactivarLupa();
                }

                return;
            }

            if (e.KeyCode == Keys.ShiftKey)
            {
                // SHIFT fija la lupa al soltar.
                if (_lupaActiva && !_lupaFija && _lupaModoFijar)
                {
                    _lupaFija = true;
                    _lupaModoFijar = false;
                    pictureBox1.Invalidate();
                }

                return;
            }
        }

        private void PictureBox1_MouseMoveLupa(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null)
                return;

            // Si está fija, no seguimos al mouse.
            if (_lupaFija)
                return;

            bool controlPresionado = (ModifierKeys & Keys.Control) == Keys.Control;
            bool shiftPresionado = (ModifierKeys & Keys.Shift) == Keys.Shift;

            if (!controlPresionado && !shiftPresionado)
            {
                if (_lupaActiva)
                    DesactivarLupa();

                return;
            }

            _lupaActiva = true;
            _lupaModoFijar = shiftPresionado;
            _posicionLupa = e.Location;

            // Repintar completo evita basura visual al mover la lupa.
            pictureBox1.Invalidate();
        }

        private void PictureBox1_MouseLeaveLupa(object sender, EventArgs e)
        {
            // Si está fija, no desaparece al salir del pictureBox.
            if (_lupaFija)
                return;

            DesactivarLupa();
        }

        private void DesactivarLupa()
        {
            if (!_lupaActiva && !_lupaFija)
                return;

            _lupaActiva = false;
            _lupaFija = false;
            _lupaModoFijar = false;
            _ultimoRectanguloLupa = Rectangle.Empty;

            if (pictureBox1 != null)
                pictureBox1.Invalidate();
        }

        private void InvalidarLupaCompleta()
        {
            if (pictureBox1 == null)
                return;

            Rectangle rect = ObtenerRectanguloLupa(_posicionLupa);
            rect.Inflate(8, 8);

            pictureBox1.Invalidate(rect);

            if (_ultimoRectanguloLupa != Rectangle.Empty)
            {
                Rectangle anterior = _ultimoRectanguloLupa;
                anterior.Inflate(8, 8);
                pictureBox1.Invalidate(anterior);
            }
        }

        private Rectangle ObtenerRectanguloLupa(Point centro)
        {
            return new Rectangle(
                centro.X,
                centro.Y - (LUPA_ALTO / 2),
                LUPA_ANCHO,
                LUPA_ALTO);
        }

        private void PictureBox1_PaintLupa(object sender, PaintEventArgs e)
        {
            if (!_lupaActiva)
                return;

            if (pictureBox1.Image == null)
                return;

            if (!pictureBox1.ClientRectangle.Contains(_posicionLupa))
                return;

            Rectangle rectImagenMostrada = ObtenerRectanguloImagenMostradaParaLupa(pictureBox1);

            if (rectImagenMostrada == Rectangle.Empty)
                return;

            if (!rectImagenMostrada.Contains(_posicionLupa))
                return;

            Image imagen = pictureBox1.Image;

            float escalaX = (float)imagen.Width / rectImagenMostrada.Width;
            float escalaY = (float)imagen.Height / rectImagenMostrada.Height;

            int imagenX = (int)Math.Round((_posicionLupa.X - rectImagenMostrada.X) * escalaX);
            int imagenY = (int)Math.Round((_posicionLupa.Y - rectImagenMostrada.Y) * escalaY);

            int anchoFuente = (int)Math.Round((LUPA_ANCHO / LUPA_FACTOR_ZOOM) * escalaX);
            int altoFuente = (int)Math.Round((LUPA_ALTO / LUPA_FACTOR_ZOOM) * escalaY);

            if (anchoFuente <= 0 || altoFuente <= 0)
                return;

            Rectangle rectFuente = new Rectangle(
                imagenX,
                imagenY - altoFuente / 2,
                anchoFuente,
                altoFuente);

            AjustarRectanguloFuenteDentroImagen(ref rectFuente, imagen.Size);

            if (rectFuente.Width <= 0 || rectFuente.Height <= 0)
                return;

            Rectangle rectDestino = ObtenerRectanguloLupa(_posicionLupa);

            // Evita que la lupa se dibuje fuera del PictureBox.
            AjustarRectanguloDestinoDentroPictureBox(ref rectDestino, pictureBox1.ClientRectangle);

            if (rectDestino.Width <= 0 || rectDestino.Height <= 0)
                return;

            _ultimoRectanguloLupa = rectDestino;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            // Fondo
            using (Brush fondo = new SolidBrush(Color.White))
            {
                e.Graphics.FillRectangle(fondo, rectDestino);
            }

            // Imagen ampliada
            e.Graphics.DrawImage(
                imagen,
                rectDestino,
                rectFuente,
                GraphicsUnit.Pixel);

            // Borde de la lupa.
            // Rojo = lupa momentánea.
            // Azul = lupa fija.
            Rectangle borde = rectDestino;
            borde.Width -= 1;
            borde.Height -= 1;

            Color colorBorde = _lupaFija ? Color.DodgerBlue : Color.Red;

            using (Pen sombraNegra = new Pen(Color.Black, 2))
            using (Pen bordeColor = new Pen(colorBorde, 4))
            using (Pen bordeBlancoInterno = new Pen(Color.White, 1))
            {
                e.Graphics.DrawRectangle(sombraNegra, borde);

                Rectangle bordeColorRect = borde;
                bordeColorRect.Inflate(-2, -2);
                e.Graphics.DrawRectangle(bordeColor, bordeColorRect);

                Rectangle bordeInterno = borde;
                bordeInterno.Inflate(-5, -5);
                e.Graphics.DrawRectangle(bordeBlancoInterno, bordeInterno);
            }
        }

        private void AjustarRectanguloDestinoDentroPictureBox(ref Rectangle rectDestino, Rectangle limites)
        {
            if (rectDestino.Left < limites.Left)
                rectDestino.X = limites.Left;

            if (rectDestino.Top < limites.Top)
                rectDestino.Y = limites.Top;

            if (rectDestino.Right > limites.Right)
                rectDestino.X = limites.Right - rectDestino.Width;

            if (rectDestino.Bottom > limites.Bottom)
                rectDestino.Y = limites.Bottom - rectDestino.Height;

            if (rectDestino.X < limites.Left)
                rectDestino.X = limites.Left;

            if (rectDestino.Y < limites.Top)
                rectDestino.Y = limites.Top;
        }


        private Rectangle ObtenerRectanguloImagenMostradaParaLupa(PictureBox pictureBox)
        {
            if (pictureBox == null || pictureBox.Image == null)
                return Rectangle.Empty;

            Rectangle area = pictureBox.ClientRectangle;

            if (area.Width <= 0 || area.Height <= 0)
                return Rectangle.Empty;

            if (pictureBox.SizeMode == PictureBoxSizeMode.StretchImage ||
                pictureBox.SizeMode == PictureBoxSizeMode.Normal)
            {
                return area;
            }

            if (pictureBox.SizeMode != PictureBoxSizeMode.Zoom)
                return area;

            int imageWidth = pictureBox.Image.Width;
            int imageHeight = pictureBox.Image.Height;

            if (imageWidth <= 0 || imageHeight <= 0)
                return Rectangle.Empty;

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

        private void HabilitarDobleBuffer(Control control)
        {
            if (control == null)
                return;

            typeof(Control)
                .GetProperty(
                    "DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(control, true, null);
        }

        private void AjustarRectanguloFuenteDentroImagen(ref Rectangle rectFuente, Size tamanioImagen)
        {
            if (rectFuente.X < 0)
                rectFuente.X = 0;

            if (rectFuente.Y < 0)
                rectFuente.Y = 0;

            if (rectFuente.Right > tamanioImagen.Width)
                rectFuente.X = tamanioImagen.Width - rectFuente.Width;

            if (rectFuente.Bottom > tamanioImagen.Height)
                rectFuente.Y = tamanioImagen.Height - rectFuente.Height;

            if (rectFuente.X < 0)
            {
                rectFuente.Width += rectFuente.X;
                rectFuente.X = 0;
            }

            if (rectFuente.Y < 0)
            {
                rectFuente.Height += rectFuente.Y;
                rectFuente.Y = 0;
            }

            if (rectFuente.Right > tamanioImagen.Width)
                rectFuente.Width = tamanioImagen.Width - rectFuente.X;

            if (rectFuente.Bottom > tamanioImagen.Height)
                rectFuente.Height = tamanioImagen.Height - rectFuente.Y;
        }


        private void FormCopidis_Shown(object sender, EventArgs e)
        {
            ForzarMaximizado();
            AplicarPerfilResolucion(_perfilActual);
            ForzarMaximizado();
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
            ConfigurarBloquesNumeroCertificado();
            ConfigurarEventosCud();
            ConfigurarOrdenTabulacionCud();

            ConfigurarSeleccionCuadranteOcr();

            RestaurarColoresFormularioCud();
        }


        private void ConfigurarPreviewZoomOcr()
        {
            // Versión 2: se elimina el panel inferior de zoom OCR.
            // El certificado se muestra directamente recortado/ampliado en pictureBox1.
        }
        private void PictureBoxZoomOcr_Paint(object sender, PaintEventArgs e)
        {
            // Versión 2: método conservado vacío por compatibilidad interna.
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

            if (dni.Length < 6 || dni.Length > 8)
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

            // Si todavía no hay número certificado en bloques, no inventamos todo.
            // Solo actualizamos el bloque DNI si el certificado ya tiene estructura mínima.
            if (string.IsNullOrWhiteSpace(TxtCertBloque2.Text) ||
                string.IsNullOrWhiteSpace(TxtCertBloque4.Text) ||
                string.IsNullOrWhiteSpace(TxtCertBloque5.Text))
            {
                return;
            }

            _sincronizandoCamposCud = true;

            try
            {
                TxtCertBloque3.Text = dni.PadLeft(11, '0');
                TxtNumeCert.Text = ReconstruirNumeroCertificadoDesdeBloques();
                _ultimoDniAuto = dni;
            }
            finally
            {
                _sincronizandoCamposCud = false;
            }

            MarcarCampoAuto(
                TxtCertBloque3,
                "Bloque DNI del certificado sincronizado con el DNI cargado.");

            MarcarCampoAuto(
                TxtNumeCert,
                "Número de certificado sincronizado con el DNI cargado.");
        }


        private void ConfigurarOrdenTabulacionCud()
        {
            TxtCertBloque1.TabIndex = 0;
            TxtCertBloque2.TabIndex = 1;
            TxtCertBloque3.TabIndex = 2;
            TxtCertBloque4.TabIndex = 3;
            TxtCertBloque5.TabIndex = 4;
            TxtCertBloque6.TabIndex = 5;
            TxtCertBloque7.TabIndex = 6;

            TxtApellidoNombre.TabIndex = 7;
            TxtNumCuil.TabIndex = 8;
            TxtFechaNac.TabIndex = 9;
            TxtDiagnostico.TabIndex = 10;
            TxtFuncionesCorporales.TabIndex = 11;
            TxtEstructurasCorporales.TabIndex = 12;
            TxtActividad_Particip.TabIndex = 13;
            TxtFactoresAmbientales.TabIndex = 14;
            TxtOrientacion.TabIndex = 15;
            CmbAcomapante.TabIndex = 16;
            btnGrabar.TabIndex = 17;

            // No entran en el recorrido manual de carga.
            TxtFechaVencimiento.TabIndex = 90;
            TxtFechaEmision.TabIndex = 91;

            foreach (Control c in ObtenerControlesCargaCud())
            {
                c.TabStop = true;
            }

            TxtFechaVencimiento.TabStop = false;
            TxtFechaEmision.TabStop = false;

            btnGrabar.TabStop = true;
        }

        private List<Control> ObtenerControlesCargaCud()
        {
            return new List<Control>
    {
        TxtCertBloque1,
        TxtCertBloque2,
        TxtCertBloque3,
        TxtCertBloque4,
        TxtCertBloque5,
        TxtCertBloque6,
        TxtCertBloque7,

        TxtApellidoNombre,
        TxtNumCuil,
        TxtFechaNac,
        TxtDiagnostico,
        TxtFuncionesCorporales,
        TxtEstructurasCorporales,
        TxtActividad_Particip,
        TxtFactoresAmbientales,
        TxtOrientacion,
        CmbAcomapante
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

        private async void btnSeleccionarLote_Click(
        object sender,
        EventArgs e)
        {
            btnSeleccionarLote.Enabled = false;

            string loteSeleccionado = null;
            string indexadorActual =
                ObtenerNombreIndexadorActual();

            if (string.IsNullOrWhiteSpace(indexadorActual))
            {
                btnSeleccionarLote.Enabled = true;

                MessageBox.Show(
                    "No se pudo determinar el usuario/indexador actual. " +
                    "Verifique que el usuario esté seleccionado o cargado " +
                    "antes de tomar un lote.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            Debug.WriteLine(
                "[LOTE] Indexador actual: " +
                indexadorActual);

            try
            {
                await Task.Run(() =>
                {
                    ObtenerDatos datos = new ObtenerDatos();

                    // ==================================================
                    // 1. REVISAR SOLAMENTE EL ÚLTIMO LOTE DEL USUARIO
                    // ==================================================
                    string ultimoLoteUsuario =
                        datos.ConsultarUltimoLoteUsuario(
                            indexadorActual);

                    if (LoteTieneDocumentosPendientes(
                            ultimoLoteUsuario))
                    {
                        Debug.WriteLine(
                            "[LOTE] Se retoma el último lote activo: " +
                            ultimoLoteUsuario);

                        loteSeleccionado =
                            ultimoLoteUsuario;

                        return;
                    }

                    Debug.WriteLine(
                        "[LOTE] El usuario no tiene un último lote activo.");

                    // ==================================================
                    // 2. BUSCAR SOLAMENTE UN LOTE LIBRE
                    // ==================================================
                    if (!Directory.Exists(rutaCudProcesadas))
                        return;

                    DirectoryInfo carpetaPrincipal =
                        new DirectoryInfo(rutaCudProcesadas);

                    foreach (DirectoryInfo carpetaGrupo
                             in carpetaPrincipal.GetDirectories())
                    {
                        string grupo = carpetaGrupo.Name;

                        if (string.IsNullOrWhiteSpace(grupo))
                            continue;

                        foreach (DirectoryInfo loteDir
                                 in carpetaGrupo.GetDirectories())
                        {
                            string rutaLote =
                                loteDir.FullName;

                            string usuarioAsignado =
                                datos.Consultaloteasignado(
                                    rutaLote);

                            /*
                             * Si tiene cualquier asignación anterior,
                             * no se selecciona.
                             *
                             * Tampoco se retoman aquí lotes antiguos
                             * del mismo usuario.
                             */
                            if (!string.IsNullOrWhiteSpace(
                                    usuarioAsignado))
                            {
                                continue;
                            }

                            Debug.WriteLine(
                                "[LOTE] Asignando nuevo lote: " +
                                rutaLote);

                            Debug.WriteLine(
                                "[LOTE] Usuario: " +
                                indexadorActual);

                            new ProduccionAccess()
                                .loteasignado(
                                    rutaLote,
                                    indexadorActual);

                            loteSeleccionado =
                                rutaLote;

                            return;
                        }
                    }
                });

                if (!string.IsNullOrWhiteSpace(loteSeleccionado))
                {
                    txtRutaLote.Text =
                        loteSeleccionado;

                    await InicializarContadorCudHoyAsync(
                        indexadorActual);

                    CargarDocumentosEnSegundoPlano(
                        loteSeleccionado);
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
                    "Error al seleccionar lote CUD: " +
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private bool LoteTieneDocumentosPendientes(string rutaLote)
        {
            if (string.IsNullOrWhiteSpace(rutaLote))
                return false;

            if (!Directory.Exists(rutaLote))
                return false;

            try
            {
                return Directory
                    .EnumerateFiles(
                        rutaLote,
                        "*.pdf",
                        SearchOption.TopDirectoryOnly)
                    .Any();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "[LOTE][ERROR] No se pudo revisar el lote: " +
                    ex.Message);

                return false;
            }
        }


        private async Task InicializarContadorCudHoyAsync(string indexador)
        {
            _indexadorContadorActual = indexador?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_indexadorContadorActual))
            {
                _cantidadCudIndexadosHoy = 0;
                ActualizarLabelCudHoy();
                return;
            }

            _cantidadCudIndexadosHoy = await Task.Run(() =>
            {
                return new ObtenerDatos().ContarCudIndexadosHoy(_indexadorContadorActual);
            });

            ActualizarLabelCudHoy();
        }

        private void ActualizarLabelCudHoy()
        {
            lblCudHoyR.Text = _cantidadCudIndexadosHoy.ToString();
        }

        private void IncrementarContadorCudHoy()
        {
            _cantidadCudIndexadosHoy++;
            ActualizarLabelCudHoy();
        }
        private bool _cargandoDesdePreprocesamiento = false;



        private void CargarDatosPreprocesamientoSiExisten(string rutaPdfActual)
        {
            if (string.IsNullOrWhiteSpace(rutaPdfActual))
                return;

            try
            {
                _cargandoDesdePreprocesamiento = true;

                Debug.WriteLine("==================================================");
                Debug.WriteLine("[PREINDEX] Intentando cargar datos desde BDPreprocesamiento");
                Debug.WriteLine("[PREINDEX] Ruta PDF actual: " + rutaPdfActual);

                ObtenerDatos oconsulta = new ObtenerDatos();
                PreprocesamientoCudDto datos = oconsulta.ObtenerPreprocesamientoPorRutaSalida(rutaPdfActual);

                if (datos == null)
                {
                    Debug.WriteLine("[PREINDEX] No se encontró registro por RutaSalida.");
                    return;
                }

                Debug.WriteLine("[PREINDEX] Registro encontrado.");
                Debug.WriteLine("[PREINDEX] EstadoOCR: " + datos.EstadoOCR);
                Debug.WriteLine("[PREINDEX] RequiereQA informativo: " + datos.RequiereQA);
                Debug.WriteLine("[PREINDEX] ScoreGlobal: " + datos.ScoreGlobal);

                // ============================================================
                // REGLA PRINCIPAL:
                // Solo se precarga si EstadoOCR = OKAutomatico.
                // RequiereQA queda solo como dato informativo.
                // ============================================================
                if (!IndexadorCudService.EstadoOcrPermitePrecarga(datos.EstadoOCR))
                {
                    Debug.WriteLine("[PREINDEX] EstadoOCR distinto de OKAutomatico. No se carga ningún TextBox.");
                    return;
                }

                Debug.WriteLine("[PREINDEX] EstadoOCR=OKAutomatico. Precarga habilitada.");

                // ============================================================
                // NUMERO CERTIFICADO
                // ============================================================
                string numeroCertificado = IndexadorCudService.NormalizarNumeroCertificado(datos.NumeroCertificado);
                DatosNumeroCertificadoCud datosNumero = IndexadorCudService.ParseNumeroCertificado(numeroCertificado);

                if (datosNumero.EsValido)
                {
                    TxtNumeCert.Text = datosNumero.NumeroNormalizado;
                    CargarBloquesNumeroCertificadoDesdeTexto(datosNumero.NumeroNormalizado);

                    if (!string.IsNullOrWhiteSpace(datosNumero.Sexo) && CmbSexo.Items.Contains(datosNumero.Sexo))
                    {
                        CmbSexo.Text = datosNumero.Sexo;
                        Debug.WriteLine("[PREINDEX][Sexo] Cargado desde número certificado: " + datosNumero.Sexo);
                    }

                    Debug.WriteLine("[PREINDEX][NumeroCertificado] Cargado: " + datosNumero.NumeroNormalizado);
                    Debug.WriteLine("[PREINDEX][NumeroCertificado] Sexo interno: " + datosNumero.Sexo);
                    Debug.WriteLine("[PREINDEX][NumeroCertificado] DNI interno: " + datosNumero.Dni);
                    Debug.WriteLine("[PREINDEX][NumeroCertificado] Fecha emisión interna: " + datosNumero.FechaEmision);
                    Debug.WriteLine("[PREINDEX][NumeroCertificado] Fecha vencimiento interna: " + datosNumero.FechaVencimiento);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][NumeroCertificado] No cargado: " + datosNumero.Mensaje);
                }

                // ============================================================
                // APELLIDO Y NOMBRE
                // ============================================================
                string apellidoNombre = IndexadorCudService.NormalizarNombreApellido(datos.ApellidoNombre);
                ResultadoValidacion valApellido = IndexadorCudService.ValidarApellidoNombrePreprocesamiento(apellidoNombre);

                if (valApellido.EsValido && !string.IsNullOrWhiteSpace(apellidoNombre))
                {
                    TxtApellidoNombre.Text = apellidoNombre;
                    Debug.WriteLine("[PREINDEX][ApellidoNombre] Cargado: " + apellidoNombre);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][ApellidoNombre] No cargado: " + valApellido.Mensaje + " | Valor='" + datos.ApellidoNombre + "'");
                }

                // ============================================================
                // DNI
                // Si el número de certificado es válido, el DNI interno tiene prioridad.
                // ============================================================
                string dni = IndexadorCudService.NormalizarDni(datos.DNI);

                if (datosNumero.EsValido)
                {
                    if (string.IsNullOrWhiteSpace(dni))
                    {
                        dni = datosNumero.Dni;
                        Debug.WriteLine("[PREINDEX][DNI] DNI vacío en OCR. Se toma desde número de certificado: " + dni);
                    }
                    else if (!dni.Equals(datosNumero.Dni, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine("[PREINDEX][DNI] OCR no coincide con número certificado. OCR=" + dni + " | Cert=" + datosNumero.Dni);
                        dni = datosNumero.Dni;
                        Debug.WriteLine("[PREINDEX][DNI] Se prioriza DNI del número certificado: " + dni);
                    }
                }

                ResultadoValidacion valDni = IndexadorCudService.ValidarDni(dni, obligatorio: false);

                if (valDni.EsValido && !string.IsNullOrWhiteSpace(dni))
                {
                    TxtNumDNI.Text = dni;
                    Debug.WriteLine("[PREINDEX][DNI] Cargado: " + dni);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][DNI] No cargado: " + valDni.Mensaje + " | Valor='" + datos.DNI + "'");
                }

                // ============================================================
                // CUIL
                // Opcional.
                // ============================================================
                string cuil = IndexadorCudService.NormalizarCuil(datos.CUIL);
                ResultadoValidacion valCuil = IndexadorCudService.ValidarCuil(cuil, dni, obligatorio: false);

                if (valCuil.EsValido && !string.IsNullOrWhiteSpace(cuil))
                {
                    TxtNumCuil.Text = cuil;
                    Debug.WriteLine("[PREINDEX][CUIL] Cargado: " + cuil);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][CUIL] No cargado: " + valCuil.Mensaje + " | Valor='" + datos.CUIL + "'");
                }

                // ============================================================
                // FECHA NACIMIENTO
                // ============================================================
                string fechaNacimiento = datos.FechaNacimiento.HasValue
                    ? datos.FechaNacimiento.Value.ToString("dd/MM/yyyy")
                    : string.Empty;

                fechaNacimiento = IndexadorCudService.NormalizarFechaCud(
                    fechaNacimiento,
                    TipoFechaCud.Nacimiento
                );

                ResultadoValidacion valFechaNacimiento = IndexadorCudService.ValidarFecha(
                    fechaNacimiento,
                    "Fecha de nacimiento",
                    obligatorio: false,
                    permiteFutura: false,
                    minimo: new DateTime(1900, 1, 1),
                    maximo: DateTime.Today
                );

                if (valFechaNacimiento.EsValido && !string.IsNullOrWhiteSpace(fechaNacimiento))
                {
                    TxtFechaNac.Text = fechaNacimiento;
                    Debug.WriteLine("[PREINDEX][FechaNacimiento] Cargada: " + fechaNacimiento);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][FechaNacimiento] No cargada: " + valFechaNacimiento.Mensaje);
                }

                // ============================================================
                // FECHA EMISION
                // Si el número certificado es válido, se prioriza la fecha interna.
                // ============================================================
                string fechaEmision = string.Empty;

                if (datosNumero.EsValido)
                {
                    fechaEmision = datosNumero.FechaEmision;
                    Debug.WriteLine("[PREINDEX][FechaEmision] Se toma desde número certificado: " + fechaEmision);
                }
                else if (datos.FechaEmision.HasValue)
                {
                    fechaEmision = datos.FechaEmision.Value.ToString("dd/MM/yyyy");
                }

                fechaEmision = IndexadorCudService.NormalizarFechaCud(
                    fechaEmision,
                    TipoFechaCud.Emision
                );

                ResultadoValidacion valFechaEmision = IndexadorCudService.ValidarFecha(
                    fechaEmision,
                    "Fecha de emisión",
                    obligatorio: false,
                    permiteFutura: false,
                    minimo: new DateTime(1900, 1, 1),
                    maximo: DateTime.Today
                );

                if (valFechaEmision.EsValido && !string.IsNullOrWhiteSpace(fechaEmision))
                {
                    TxtFechaEmision.Text = fechaEmision;
                    Debug.WriteLine("[PREINDEX][FechaEmision] Cargada: " + fechaEmision);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][FechaEmision] No cargada: " + valFechaEmision.Mensaje);
                }

                // ============================================================
                // FECHA VENCIMIENTO
                // Si el número certificado es válido, se prioriza la fecha interna.
                // ============================================================
                string fechaVencimiento = string.Empty;

                if (datosNumero.EsValido)
                {
                    fechaVencimiento = datosNumero.FechaVencimiento;
                    Debug.WriteLine("[PREINDEX][FechaVencimiento] Se toma desde número certificado: " + fechaVencimiento);
                }
                else if (datos.FechaVencimiento.HasValue)
                {
                    fechaVencimiento = datos.FechaVencimiento.Value.ToString("dd/MM/yyyy");
                }

                fechaVencimiento = IndexadorCudService.NormalizarFechaCud(
                    fechaVencimiento,
                    TipoFechaCud.Vencimiento
                );

                ResultadoValidacion valFechaVencimiento = IndexadorCudService.ValidarFecha(
                    fechaVencimiento,
                    "Fecha de vencimiento",
                    obligatorio: false,
                    permiteFutura: true,
                    minimo: new DateTime(1900, 1, 1),
                    maximo: null
                );

                if (valFechaVencimiento.EsValido && !string.IsNullOrWhiteSpace(fechaVencimiento))
                {
                    TxtFechaVencimiento.Text = fechaVencimiento;
                    Debug.WriteLine("[PREINDEX][FechaVencimiento] Cargada: " + fechaVencimiento);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][FechaVencimiento] No cargada: " + valFechaVencimiento.Mensaje);
                }

                // ============================================================
                // ACOMPAÑANTE
                // ============================================================
                string acompaniante = IndexadorCudService.NormalizarAcompaniantePreprocesamiento(datos.Acompaniante);

                if (!string.IsNullOrWhiteSpace(acompaniante))
                {
                    CmbAcomapante.Text = acompaniante;
                    Debug.WriteLine("[PREINDEX][Acompaniante] Cargado: " + acompaniante);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][Acompaniante] No cargado. Valor='" + datos.Acompaniante + "'");
                }

                // ============================================================
                // CIF - FUNCIONES CORPORALES
                // ============================================================
                string funciones = IndexadorCudService.NormalizarCodigosCif(datos.FuncionesCorporales);

                ResultadoValidacion valFunciones = IndexadorCudService.ValidarCodigosCif(
                    funciones,
                    'b',
                    "Funciones corporales",
                    obligatorio: false
                );

                if (valFunciones.EsValido && !string.IsNullOrWhiteSpace(funciones))
                {
                    TxtFuncionesCorporales.Text = funciones;
                    Debug.WriteLine("[PREINDEX][FuncionesCorporales] Cargado: " + funciones);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][FuncionesCorporales] No cargado: " + valFunciones.Mensaje + " | Valor='" + datos.FuncionesCorporales + "'");
                }

                // ============================================================
                // CIF - ESTRUCTURAS CORPORALES
                // ============================================================
                string estructuras = IndexadorCudService.NormalizarCodigosCif(datos.EstructurasCorporales);

                ResultadoValidacion valEstructuras = IndexadorCudService.ValidarCodigosCif(
                    estructuras,
                    's',
                    "Estructuras corporales",
                    obligatorio: false
                );

                if (valEstructuras.EsValido && !string.IsNullOrWhiteSpace(estructuras))
                {
                    TxtEstructurasCorporales.Text = estructuras;
                    Debug.WriteLine("[PREINDEX][EstructurasCorporales] Cargado: " + estructuras);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][EstructurasCorporales] No cargado: " + valEstructuras.Mensaje + " | Valor='" + datos.EstructurasCorporales + "'");
                }

                // ============================================================
                // CIF - ACTIVIDAD / PARTICIPACIÓN
                // ============================================================
                string actividad = IndexadorCudService.NormalizarCodigosCif(datos.ActividadParticipacion);

                ResultadoValidacion valActividad = IndexadorCudService.ValidarCodigosCif(
                    actividad,
                    'd',
                    "Actividad y participación",
                    obligatorio: false
                );

                if (valActividad.EsValido && !string.IsNullOrWhiteSpace(actividad))
                {
                    TxtActividad_Particip.Text = actividad;
                    Debug.WriteLine("[PREINDEX][ActividadParticipacion] Cargado: " + actividad);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][ActividadParticipacion] No cargado: " + valActividad.Mensaje + " | Valor='" + datos.ActividadParticipacion + "'");
                }

                // ============================================================
                // CIF - FACTORES AMBIENTALES
                // ============================================================
                string factores = IndexadorCudService.NormalizarCodigosCif(datos.FactoresAmbientales);

                ResultadoValidacion valFactores = IndexadorCudService.ValidarCodigosCif(
                    factores,
                    'e',
                    "Factores ambientales",
                    obligatorio: false
                );

                if (valFactores.EsValido && !string.IsNullOrWhiteSpace(factores))
                {
                    TxtFactoresAmbientales.Text = factores;
                    Debug.WriteLine("[PREINDEX][FactoresAmbientales] Cargado: " + factores);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][FactoresAmbientales] No cargado: " + valFactores.Mensaje + " | Valor='" + datos.FactoresAmbientales + "'");
                }

                // ============================================================
                // ORIENTACIÓN PRESTACIONAL
                // ============================================================
                string orientacion = IndexadorCudService.NormalizarOrientacionDesdeOcr(
                    datos.OrientacionPrestacional
                );

                if (!string.IsNullOrWhiteSpace(orientacion))
                {
                    TxtOrientacion.Text = orientacion;
                    Debug.WriteLine("[PREINDEX][OrientacionPrestacional] Cargada: " + orientacion);
                }
                else
                {
                    Debug.WriteLine("[PREINDEX][OrientacionPrestacional] No cargada. Valor='" + datos.OrientacionPrestacional + "'");
                }

                Debug.WriteLine("[PREINDEX] Precarga finalizada.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PREINDEX][ERROR] " + ex);
            }
            finally
            {
                _cargandoDesdePreprocesamiento = false;
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
            {
                CargarDatosCudEditado(RutaNombreImagen);
            }
            else
            {
                CargarDatosPreprocesamientoSiExisten(RutaNombreImagen);
            }

            TxtCertBloque2.Focus();
        }

        private void CargarDatosFijosIndice()
        {
            if (lboxIndice.SelectedItem != null)
                txtNombreImagen.Text = lboxIndice.SelectedItem.ToString();
        }

        private void CargarFicha(string ruta)
        {
            LimpiarCacheOcrHd();

            _zonasAjustadasOcrCertificadoActual.Clear();
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
            {
                e.Handled = true;
                return;
            }

            if (!char.IsControl(e.KeyChar))
            {
                char c = char.ToLowerInvariant(e.KeyChar);

                if (c == 'b' || c == 's' || c == 'd' || c == 'e')
                    e.KeyChar = c;
            }
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

            foreach (char cOriginal in texto)
            {
                char c = cOriginal;

                if (char.IsLetter(c))
                    c = char.ToLowerInvariant(c);

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
           
            if (!string.IsNullOrWhiteSpace(datos.Sexo) && CmbSexo.Items.Contains(datos.Sexo))
            {
                CmbSexo.Text = datos.Sexo;
            }


            _ultimoDniAuto = datos.Dni;
            _ultimaEmisionAuto = datos.FechaEmision;
            _ultimoVencimientoAuto = datos.FechaVencimiento;

            // Como el DNI cambió por certificado, también actualizamos el CUIL si ya tenía prefijo o valor.
            ActualizarCuilDesdeDni(datos.Dni);

            _sincronizandoCamposCud = false;
         
            MarcarCampoAuto(CmbSexo, "Sexo sincronizado desde número de certificado.");
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

            MarcarCampo(
                TxtOrientacion,
                IndexadorCudService.ValidarObligatorio(
                    TxtOrientacion.Text,
                    "Orientación prestacional"));
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
            // ==========================================================
            // 1) RECONSTRUIR NÚMERO DESDE BLOQUES
            // IMPORTANTE:
            // validarYTriangular = false para NO pisar todavía
            // TxtFechaEmision / TxtFechaVencimiento / TxtNumDNI.
            // ==========================================================

            if (!SincronizarNumeroCertificadoDesdeBloques(validarYTriangular: false))
            {
                MessageBox.Show(
                    "El número de certificado no es válido. Revise los bloques del certificado.",
                    "Validación CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                TxtCertBloque1.Focus();
                return false;
            }

            DatosNumeroCertificadoCud datosNumero =
                IndexadorCudService.ParseNumeroCertificado(TxtNumeCert.Text);

            if (datosNumero == null || !datosNumero.EsValido)
            {
                MarcarBloquesNumeroCertificado(
                    ResultadoValidacion.Error(
                        datosNumero == null
                            ? "Número de certificado inválido."
                            : datosNumero.Mensaje));

                MessageBox.Show(
                    "El número de certificado no es válido. Revise los bloques del certificado.",
                    "Validación CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                TxtCertBloque1.Focus();
                return false;
            }

            // ==========================================================
            // 2) NORMALIZAR FORMULARIO
            // Ya con TxtNumeCert reconstruido.
            // ==========================================================

            NormalizarFormularioCudCompleto();

            Control primerError = null;
            bool valido = true;

            // ==========================================================
            // 3) VALIDAR COHERENCIA ENTRE CERTIFICADO Y TEXTBOX
            // Acá validamos que:
            // - DNI coincida con el bloque DNI del certificado.
            // - Fecha emisión coincida con la fecha del certificado.
            // - Fecha vencimiento coincida con la fecha del certificado.
            //
            // Si el TextBox está vacío, se autocompleta.
            // Si tiene otro valor, se marca como error.
            // ==========================================================

            valido &= ValidarCoherenciaNumeroCertificadoContraPantalla(
                datosNumero,
                ref primerError);

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
        private bool ValidarCoherenciaNumeroCertificadoContraPantalla(
    DatosNumeroCertificadoCud datosNumero,
    ref Control primerError)
        {
            if (datosNumero == null || !datosNumero.EsValido)
                return false;

            bool valido = true;


            // ==========================================================
            // SEXO
            // ==========================================================

            string sexoPantalla = IndexadorCudService.NormalizarSexo(CmbSexo.Text);
            string sexoCertificado = IndexadorCudService.NormalizarSexo(datosNumero.Sexo);

            if (string.IsNullOrWhiteSpace(sexoPantalla))
            {
                if (!string.IsNullOrWhiteSpace(sexoCertificado) && CmbSexo.Items.Contains(sexoCertificado))
                {
                    CmbSexo.Text = sexoCertificado;

                    MarcarCampoAuto(
                        CmbSexo,
                        "Sexo autocompletado desde número de certificado.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(sexoCertificado) &&
                     !sexoPantalla.Equals(sexoCertificado, StringComparison.OrdinalIgnoreCase))
            {
                valido = false;

                ResultadoValidacion r = ResultadoValidacion.Error(
                    "El sexo cargado no coincide con el tipo incluido en el número de certificado.");

                MarcarCampo(CmbSexo, r);
                MarcarCampo(TxtCertBloque2, r);

                if (primerError == null)
                    primerError = CmbSexo;
            }



            // ==========================================================
            // DNI
            // ==========================================================

            string dniPantalla = IndexadorCudService.NormalizarDni(TxtNumDNI.Text);
            string dniCertificado = IndexadorCudService.NormalizarDni(datosNumero.Dni);

            if (string.IsNullOrWhiteSpace(dniPantalla))
            {
                TxtNumDNI.Text = dniCertificado;
                _ultimoDniAuto = dniCertificado;

                MarcarCampoAuto(
                    TxtNumDNI,
                    "DNI autocompletado desde número de certificado.");
            }
            else if (!dniPantalla.Equals(dniCertificado, StringComparison.OrdinalIgnoreCase))
            {
                valido = false;

                ResultadoValidacion r = ResultadoValidacion.Error(
                    "El DNI cargado no coincide con el DNI incluido en el número de certificado.");

                MarcarCampo(TxtNumDNI, r);
                MarcarCampo(TxtCertBloque3, r);

                if (primerError == null)
                    primerError = TxtNumDNI;
            }

            // ==========================================================
            // FECHA EMISIÓN
            // datosNumero.FechaEmision ya viene como dd/MM/yyyy.
            // ==========================================================

            string emisionPantalla = IndexadorCudService.NormalizarFechaCud(
                TxtFechaEmision.Text,
                TipoFechaCud.Emision);

            string emisionCertificado = IndexadorCudService.NormalizarFechaCud(
                datosNumero.FechaEmision,
                TipoFechaCud.Emision);

            if (string.IsNullOrWhiteSpace(emisionPantalla))
            {
                TxtFechaEmision.Text = emisionCertificado;
                _ultimaEmisionAuto = emisionCertificado;

                MarcarCampoAuto(
                    TxtFechaEmision,
                    "Fecha de emisión autocompletada desde número de certificado.");
            }
            else if (!emisionPantalla.Equals(emisionCertificado, StringComparison.OrdinalIgnoreCase))
            {
                valido = false;

                ResultadoValidacion r = ResultadoValidacion.Error(
                    "La fecha de emisión cargada no coincide con la fecha incluida en el número de certificado.");

                MarcarCampo(TxtFechaEmision, r);
                MarcarCampo(TxtCertBloque4, r);

                if (primerError == null)
                    primerError = TxtFechaEmision;
            }
            else
            {
                TxtFechaEmision.Text = emisionPantalla;
            }

            // ==========================================================
            // FECHA VENCIMIENTO
            // datosNumero.FechaVencimiento ya viene como dd/MM/yyyy.
            // ==========================================================

            string vencimientoPantalla = IndexadorCudService.NormalizarFechaCud(
                TxtFechaVencimiento.Text,
                TipoFechaCud.Vencimiento);

            string vencimientoCertificado = IndexadorCudService.NormalizarFechaCud(
                datosNumero.FechaVencimiento,
                TipoFechaCud.Vencimiento);

            if (string.IsNullOrWhiteSpace(vencimientoPantalla))
            {
                TxtFechaVencimiento.Text = vencimientoCertificado;
                _ultimoVencimientoAuto = vencimientoCertificado;

                MarcarCampoAuto(
                    TxtFechaVencimiento,
                    "Fecha de vencimiento autocompletada desde número de certificado.");
            }
            else if (!vencimientoPantalla.Equals(vencimientoCertificado, StringComparison.OrdinalIgnoreCase))
            {
                valido = false;

                ResultadoValidacion r = ResultadoValidacion.Error(
                    "La fecha de vencimiento cargada no coincide con la fecha incluida en el número de certificado.");

                MarcarCampo(TxtFechaVencimiento, r);
                MarcarCampo(TxtCertBloque5, r);

                if (primerError == null)
                    primerError = TxtFechaVencimiento;
            }
            else
            {
                TxtFechaVencimiento.Text = vencimientoPantalla;
            }

            return valido;
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

            SincronizarNumeroCertificadoDesdeBloques(validarYTriangular: false);
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

            // ==========================================================
            // SI ESTÁ EN EDICIÓN, EL MISMO BOTÓN GRABAR HACE UPDATE
            // No suma al contador diario.
            // ==========================================================
            if (editar == 1)
            {
                ActualizarCudEditado(dto);
                return;
            }

            // ==========================================================
            // FLUJO NORMAL: INSERT NUEVO
            // ==========================================================
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

            // ==========================================================
            // CONTADOR DIARIO
            // Se suma solo si el INSERT fue correcto.
            // No se suma en edición porque arriba ya salió por return.
            // ==========================================================
            IncrementarContadorCudHoy();

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
        private string ObtenerIndexadorActual()
        {
            if (oIndexador != null && !string.IsNullOrWhiteSpace(oIndexador.NombreIndexador))
                return oIndexador.NombreIndexador.Trim();

            return string.Empty;
        }
        private void ActualizarCudEditado(CertificadoCudDto dto)
        {
            try
            {
                if (_idCudEditando <= 0)
                {
                    MessageBox.Show(
                        "No se pudo determinar el ID del certificado a modificar.",
                        "Indexador CUD",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                string rutaSalida = RutaNombreImagen;

                if (string.IsNullOrWhiteSpace(rutaSalida))
                    rutaSalida = txtRutaLote.Text;

                string indexador = ObtenerIndexadorActual();

                ProduccionAccess oDatos = new ProduccionAccess();

                bool actualizado = oDatos.EditarCertificadoCudVerificado(
                    _idCudEditando,
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
                    rutaSalida,
                    indexador,
                    "MODIFICADO"
                );

                if (!actualizado)
                {
                    MessageBox.Show(
                        "No se pudo modificar el certificado. No se actualizó ningún registro.",
                        "Indexador CUD",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                RestaurarColoresFormularioCud();

                MessageBox.Show(
                    "Certificado modificado correctamente.",
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al modificar el certificado: " + ex.Message,
                    "Indexador CUD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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
                    Debug.WriteLine("==================================================");
                    Debug.WriteLine("[CUD GUARDAR][DUPLICADO]");
                    Debug.WriteLine("[CUD GUARDAR] Id existente: " + idExistente);
                    Debug.WriteLine("[CUD GUARDAR] NumeroCertificado: " + dto.NumeroCertificado);
                    Debug.WriteLine("[CUD GUARDAR] DNI: " + dto.DNI);
                    Debug.WriteLine("[CUD GUARDAR] ArchivoEntrada: " + archivoEntrada);
                    Debug.WriteLine("[CUD GUARDAR] Se mueve el PDF a ILEGIBLES / DUPLICADOS.");
                    Debug.WriteLine("==================================================");

                    string carpetaDuplicados = ObtenerCarpetaDuplicadosCudIlegibles(rutaLote, nombreLote);

                    Directory.CreateDirectory(carpetaDuplicados);

                    string rutaDuplicadoBase = Path.Combine(
                        carpetaDuplicados,
                        Path.GetFileName(rutaOrigen));

                    string rutaDuplicadoFinal = ObtenerRutaDisponible(rutaDuplicadoBase);

                    File.Move(rutaOrigen, rutaDuplicadoFinal);

                    Debug.WriteLine("[CUD GUARDAR][DUPLICADO] PDF movido a: " + rutaDuplicadoFinal);
                    Debug.WriteLine("[CUD GUARDAR][DUPLICADO] Se considera resuelto y continúa el flujo.");

                    nombreFinalVerificado = Path.GetFileNameWithoutExtension(rutaDuplicadoFinal);

                    return true;
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

        private string ObtenerCarpetaDuplicadosCudIlegibles(string rutaLote, string nombreLote)
        {
            if (string.IsNullOrWhiteSpace(rutaLote))
                throw new Exception("No se pudo determinar la ruta del lote actual.");

            if (string.IsNullOrWhiteSpace(nombreLote))
                nombreLote = ObtenerNombreCarpetaLoteActual();

            string marcadorPendientes = Path.DirectorySeparatorChar + "02_PENDIENTES_INDEXAR";

            int posicion = rutaLote.IndexOf(
                marcadorPendientes,
                StringComparison.OrdinalIgnoreCase);

            if (posicion < 0)
            {
                throw new Exception(
                    "No se pudo determinar la carpeta raíz COPIDIS desde la ruta del lote:" +
                    Environment.NewLine +
                    rutaLote);
            }

            string raizCopidis = rutaLote.Substring(0, posicion);

            string carpetaDuplicados = Path.Combine(
                raizCopidis,
                "04_ILEGIBLES",
                "_DUPLICADOS_CUD",
                nombreLote);

            return carpetaDuplicados;
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
                CargarBloquesNumeroCertificadoDesdeTexto(TxtNumeCert.Text);
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

            if (destino == TxtNumeCert || EsBloqueNumeroCertificado(destino))
            {
                string numero = IndexadorCudService.NormalizarNumeroCertificado(textoOcr);

                TxtNumeCert.Text = numero;
                CargarBloquesNumeroCertificadoDesdeTexto(numero);
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
                TextBox txtCif = destino as TextBox;

                if (txtCif != null)
                {
                    string textoFinal = IndexadorCudService.NormalizarCodigosCif(textoOcr);

                    Debug.WriteLine("[OCR CIF][APLICAR] Campo destino: " + destino.Name);
                    Debug.WriteLine("[OCR CIF][APLICAR] Texto recibido:");
                    Debug.WriteLine(textoOcr);
                    Debug.WriteLine("[OCR CIF][APLICAR] Texto final aplicado:");
                    Debug.WriteLine(textoFinal);

                    txtCif.Text = textoFinal;
                }
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

            if (destino == TxtNumeCert || EsBloqueNumeroCertificado(destino))
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
            LimpiarCacheOcrHd();
            OcultarCuadranteOcr();
            pictureBox1.Image = null;

            _cargandoDatosCud = true;
            _idCudEditando = 0;
            TxtNumeCert.Clear();
            TxtCertBloque1.Text = "ARG";
            TxtCertBloque2.Clear();
            TxtCertBloque3.Clear();
            TxtCertBloque4.Clear();
            TxtCertBloque5.Clear();
            TxtCertBloque6.Text = "CBA";
            TxtCertBloque7.Text = "536";

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
                        Size renderCompleto = CalcularTamanioRenderCompletoParaVista(
                            pdfDocument,
                            pictureBox1.ClientSize);

                        Debug.WriteLine("==================================================");
                        Debug.WriteLine("[CUD VISTA] Renderizando parte alta del certificado");
                        Debug.WriteLine("[CUD VISTA] PDF: " + filePath);
                        Debug.WriteLine("[CUD VISTA] PictureBox: " + pictureBox1.Width + "x" + pictureBox1.Height);
                        Debug.WriteLine("[CUD VISTA] Render completo: " + renderCompleto.Width + "x" + renderCompleto.Height);
                        Debug.WriteLine("[CUD VISTA] Alto útil: " + ObtenerPorcentajeAltoUtilCertificado().ToString("0.00", CultureInfo.InvariantCulture));
                        Debug.WriteLine("==================================================");

                        using (Image render = pdfDocument.Render(
                            0,
                            renderCompleto.Width,
                            renderCompleto.Height,
                            DPI_RENDER_VISUAL_CERTIFICADO,
                            DPI_RENDER_VISUAL_CERTIFICADO,
                            true))
                        {
                            using (Bitmap bmpRender = new Bitmap(render))
                            {
                                pictureBox1.Image = RecortarAreaUtilCertificado(bmpRender);
                            }
                        }

                        AplicarZoomVisualCertificado();
                        pictureBox1.Invalidate();
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

        private bool TextoDiagnosticoOcrPareceBasura(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return true;

            string t = texto.Trim().ToUpperInvariant();

            t = t.Replace("Á", "A")
                 .Replace("É", "E")
                 .Replace("Í", "I")
                 .Replace("Ó", "O")
                 .Replace("Ú", "U")
                 .Replace("Ü", "U");

            int total = t.Length;

            if (total == 0)
                return true;

            int letras = t.Count(char.IsLetter);
            int digitos = t.Count(char.IsDigit);
            int espacios = t.Count(char.IsWhiteSpace);
            int simbolos = total - letras - digitos - espacios;

            // Demasiados símbolos mezclados.
            double ratioSimbolos = (double)simbolos / total;

            if (ratioSimbolos > 0.25)
                return true;

            // Muy pocas letras útiles.
            if (letras < 4)
                return true;

            // Muchas secuencias de la misma letra: ZZZZ, DDDD, LLLL.
            if (Regex.IsMatch(t, @"([A-Z])\1{3,}"))
                return true;

            // Secuencias largas de consonantes sin vocales.
            // Ejemplo: ZDDOHELHLEHT no cae entero, pero STSKS5FHDDDLSMDDL sí tiende a caer.
            if (Regex.IsMatch(t, @"[BCDFGHJKLMNPQRSTVWXYZÑ]{6,}"))
                return true;

            string textoSinSimbolos = Regex.Replace(t, @"[^\p{L}\d\s]", " ");
            textoSinSimbolos = Regex.Replace(textoSinSimbolos, @"\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(textoSinSimbolos))
                return true;

            string[] tokens = textoSinSimbolos
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
                return true;

            int tokensRaros = 0;
            int tokensUtiles = 0;

            foreach (string tokenOriginal in tokens)
            {
                string token = tokenOriginal.Trim();

                if (string.IsNullOrWhiteSpace(token))
                    continue;

                string soloLetras = Regex.Replace(token, @"[^A-ZÑ]", "");

                if (soloLetras.Length >= 3)
                    tokensUtiles++;

                bool tieneVocal = Regex.IsMatch(soloLetras, @"[AEIOU]");

                // Token largo sin vocal: STSKSFHDDDLSMDDL
                if (soloLetras.Length >= 5 && !tieneVocal)
                {
                    tokensRaros++;
                    continue;
                }

                // Token muy largo con aspecto OCR, pocas vocales.
                if (soloLetras.Length >= 10)
                {
                    int vocales = soloLetras.Count(c => "AEIOU".Contains(c));
                    double ratioVocales = (double)vocales / soloLetras.Length;

                    if (ratioVocales < 0.25)
                    {
                        tokensRaros++;
                        continue;
                    }
                }

                // Token con muchas consonantes seguidas.
                if (Regex.IsMatch(soloLetras, @"[BCDFGHJKLMNPQRSTVWXYZÑ]{5,}"))
                {
                    tokensRaros++;
                    continue;
                }

                // Token con repetición anormal.
                if (Regex.IsMatch(soloLetras, @"([A-ZÑ])\1{2,}"))
                {
                    tokensRaros++;
                    continue;
                }
            }

            if (tokensUtiles == 0)
                return true;

            double ratioTokensRaros = tokens.Length == 0 ? 1 : (double)tokensRaros / tokens.Length;

            if (tokens.Length >= 2 && ratioTokensRaros >= 0.50)
                return true;

            // Caso típico de basura OCR con muchas mayúsculas aisladas:
            // O T EAE E CC S SNÑA A D Ñ IZ O
            int tokensDeUnaLetra = tokens.Count(x => Regex.Replace(x, @"[^A-ZÑ]", "").Length == 1);

            if (tokens.Length >= 6 && ((double)tokensDeUnaLetra / tokens.Length) > 0.45)
                return true;

            return false;
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
            string resourceName = "IndexadorCopidis.pdfium.dll";
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



        private bool TextoLibreOcrEsBasura(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return true;

            string t = texto.Trim();

            int total = t.Length;

            if (total == 0)
                return true;

            int letras = t.Count(char.IsLetter);
            int digitos = t.Count(char.IsDigit);
            int espacios = t.Count(char.IsWhiteSpace);

            int utiles = letras + digitos + espacios;
            int simbolos = total - utiles;

            // ==========================================================
            // 1) Muy pocas letras reales
            // ==========================================================
            if (letras < 3)
                return true;

            // ==========================================================
            // 2) Demasiados símbolos respecto del total
            // Ejemplo: "]-];-]; ;]] ÚD E$G Z-;-;-]"
            // ==========================================================
            double ratioSimbolos = total == 0 ? 1 : (double)simbolos / total;

            if (ratioSimbolos > 0.35)
                return true;

            // ==========================================================
            // 3) Demasiados separadores repetidos
            // ==========================================================
            if (Regex.IsMatch(t, @"[\]\[\-;:,\._]{4,}"))
                return true;

            if (Regex.IsMatch(t, @"(?:[\]\[\-;:,\._]\s*){6,}"))
                return true;

            // ==========================================================
            // 4) Texto casi sin palabras útiles
            // ==========================================================
            string soloPalabras = Regex.Replace(t, @"[^\p{L}\d\s]", " ");
            soloPalabras = Regex.Replace(soloPalabras, @"\s+", " ").Trim();

            string[] palabras = soloPalabras
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length >= 3)
                .ToArray();

            if (palabras.Length == 0)
                return true;

            // ==========================================================
            // 5) Muchas letras sueltas o fragmentos raros
            // ==========================================================
            string[] tokens = t
                .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length > 0)
            {
                int tokensRaros = 0;

                foreach (string token in tokens)
                {
                    string limpio = Regex.Replace(token, @"[^\p{L}\d]", "");

                    if (limpio.Length <= 1)
                        tokensRaros++;
                }

                double ratioTokensRaros = (double)tokensRaros / tokens.Length;

                if (tokens.Length >= 5 && ratioTokensRaros > 0.60)
                    return true;
            }

            return false;
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

            // Coordenadas originales del certificado completo:
            // X, Y, Ancho, Alto sobre el PDF entero.
            // En versión 2 se convierten automáticamente al área superior visible.
            // Si cambiás PORCENTAJE_ALTO_UTIL_CERTIFICADO, las zonas se recalculan solas.

            RectangleF zonaNumeroCertificado = CrearZonaOcrDesdeCertificadoCompleto(0.49f, 0.052f, 0.46f, 0.038f);

            _zonasSugeridasOcrCud[TxtNumeCert] = zonaNumeroCertificado;

            _zonasSugeridasOcrCud[TxtCertBloque1] = zonaNumeroCertificado;
            _zonasSugeridasOcrCud[TxtCertBloque2] = zonaNumeroCertificado;
            _zonasSugeridasOcrCud[TxtCertBloque3] = zonaNumeroCertificado;
            _zonasSugeridasOcrCud[TxtCertBloque4] = zonaNumeroCertificado;
            _zonasSugeridasOcrCud[TxtCertBloque5] = zonaNumeroCertificado;
            _zonasSugeridasOcrCud[TxtCertBloque6] = zonaNumeroCertificado;
            _zonasSugeridasOcrCud[TxtCertBloque7] = zonaNumeroCertificado;

            _zonasSugeridasOcrCud[TxtApellidoNombre] =
                CrearZonaOcrDesdeCertificadoCompleto(0.27f, 0.11f, 0.30f, 0.025f);

            _zonasSugeridasOcrCud[CmbSexo] =
                CrearZonaOcrDesdeCertificadoCompleto(0.25f, 0.13f, 0.20f, 0.025f);

            _zonasSugeridasOcrCud[TxtNumDNI] =
                CrearZonaOcrDesdeCertificadoCompleto(0.25f, 0.13f, 0.20f, 0.025f);

            _zonasSugeridasOcrCud[TxtNumCuil] =
                CrearZonaOcrDesdeCertificadoCompleto(0.52f, 0.13f, 0.20f, 0.025f);

            _zonasSugeridasOcrCud[TxtFechaNac] =
                CrearZonaOcrDesdeCertificadoCompleto(0.82f, 0.12f, 0.13f, 0.038f);

            _zonasSugeridasOcrCud[TxtDiagnostico] =
                CrearZonaOcrDesdeCertificadoCompleto(0.22f, 0.16f, 0.46f, 0.045f);

            _zonasSugeridasOcrCud[TxtFuncionesCorporales] =
                CrearZonaOcrDesdeCertificadoCompleto(0.28f, 0.24f, 0.40f, 0.045f);

            _zonasSugeridasOcrCud[TxtEstructurasCorporales] =
                CrearZonaOcrDesdeCertificadoCompleto(0.28f, 0.28f, 0.40f, 0.045f);

            _zonasSugeridasOcrCud[TxtActividad_Particip] =
                CrearZonaOcrDesdeCertificadoCompleto(0.08f, 0.32f, 0.84f, 0.042f);

            _zonasSugeridasOcrCud[TxtFactoresAmbientales] =
                CrearZonaOcrDesdeCertificadoCompleto(0.28f, 0.36f, 0.40f, 0.045f);

            _zonasSugeridasOcrCud[TxtOrientacion] =
                CrearZonaOcrDesdeCertificadoCompleto(0.095f, 0.42f, 0.84f, 0.050f);

            _zonasSugeridasOcrCud[TxtFechaVencimiento] =
                CrearZonaOcrDesdeCertificadoCompleto(0.25f, 0.465f, 0.20f, 0.025f);

            _zonasSugeridasOcrCud[CmbAcomapante] =
                CrearZonaOcrDesdeCertificadoCompleto(0.16f, 0.495f, 0.19f, 0.035f);

            _zonasSugeridasOcrCud[TxtFechaEmision] =
                CrearZonaOcrDesdeCertificadoCompleto(0.695f, 0.55f, 0.25f, 0.040f);
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

            if (!_zonasAjustadasOcrCertificadoActual.TryGetValue(control, out zonaNormalizada))
            {
                if (!_zonasSugeridasOcrCud.TryGetValue(control, out zonaNormalizada))
                {
                    OcultarCuadranteOcr();
                    return;
                }
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

        private RectangleF ConvertirRectanguloVistaAZonaNormalizada(Rectangle rectVista)
        {
            Rectangle areaImagen = ObtenerRectanguloImagenVisibleEnPictureBox();

            if (areaImagen == Rectangle.Empty)
                return RectangleF.Empty;

            Rectangle rectLimitado = Rectangle.Intersect(rectVista, areaImagen);

            if (rectLimitado.Width <= 0 || rectLimitado.Height <= 0)
                return RectangleF.Empty;

            float x = (float)(rectLimitado.X - areaImagen.X) / areaImagen.Width;
            float y = (float)(rectLimitado.Y - areaImagen.Y) / areaImagen.Height;
            float width = (float)rectLimitado.Width / areaImagen.Width;
            float height = (float)rectLimitado.Height / areaImagen.Height;

            return new RectangleF(x, y, width, height);
        }
        private void RecordarCuadranteActualParaControlActivo()
        {
            if (_controlDestinoOcrActivo == null)
                return;

            if (!CuadranteOcrTieneTamanioValido(_rectanguloSeleccionVistaOcr))
                return;

            RectangleF zonaNormalizada =
                ConvertirRectanguloVistaAZonaNormalizada(_rectanguloSeleccionVistaOcr);

            if (zonaNormalizada == RectangleF.Empty)
                return;

            _zonasAjustadasOcrCertificadoActual[_controlDestinoOcrActivo] = zonaNormalizada;

            Debug.WriteLine("[OCR CUADRANTE] Zona ajustada recordada para: " + _controlDestinoOcrActivo.Name);
        }



        private void ActualizarPreviewZoomOcr()
        {
            // Versión 2: sin preview inferior. El recuadro OCR se dibuja sobre pictureBox1.
        }

        private Rectangle LimitarRectanguloAImagen(Rectangle rect, Image imagen)
        {
            if (imagen == null)
                return Rectangle.Empty;

            Rectangle areaImagen = new Rectangle(0, 0, imagen.Width, imagen.Height);
            Rectangle interseccion = Rectangle.Intersect(rect, areaImagen);

            if (interseccion.Width <= 0 || interseccion.Height <= 0)
                return Rectangle.Empty;

            return interseccion;
        }

        private Rectangle ConvertirRectanguloVistaAImagenParaZoom(Rectangle rectanguloVista)
        {
            Rectangle areaImagen = ObtenerRectanguloImagenVisibleEnPictureBox();

            if (areaImagen == Rectangle.Empty)
                return Rectangle.Empty;

            Rectangle interseccion = Rectangle.Intersect(rectanguloVista, areaImagen);

            if (interseccion.Width <= 0 || interseccion.Height <= 0)
                return Rectangle.Empty;

            float escalaX = (float)pictureBox1.Image.Width / areaImagen.Width;
            float escalaY = (float)pictureBox1.Image.Height / areaImagen.Height;

            int x = (int)Math.Round((interseccion.X - areaImagen.X) * escalaX);
            int y = (int)Math.Round((interseccion.Y - areaImagen.Y) * escalaY);
            int width = (int)Math.Round(interseccion.Width * escalaX);
            int height = (int)Math.Round(interseccion.Height * escalaY);

            if (x < 0) x = 0;
            if (y < 0) y = 0;

            if (x + width > pictureBox1.Image.Width)
                width = pictureBox1.Image.Width - x;

            if (y + height > pictureBox1.Image.Height)
                height = pictureBox1.Image.Height - y;

            if (width <= 0 || height <= 0)
                return Rectangle.Empty;

            return new Rectangle(x, y, width, height);
        }

        private void OcultarPreviewZoomOcr()
        {
            // Versión 2: sin preview inferior.
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

            RecordarCuadranteActualParaControlActivo();

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

                Bitmap imagenParaOcr = ObtenerImagenOcrHdDesdeCache();

                if (imagenParaOcr == null)
                {
                    Debug.WriteLine("[OCR HD] Falló render HD. Se usa fallback con imagen actual del PictureBox.");

                    imagenParaOcr = ClonarImagenActualPictureBoxParaOcr();
                }

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

            if (destino == TxtNumeCert || EsBloqueNumeroCertificado(destino))
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
                return PrepararOcrOrientacion(crudo, confianza);

            if (destino == CmbSexo)
                return PrepararOcrSexo(crudo);

            if (destino == CmbAcomapante)
                return PrepararOcrSiNo(crudo);

            return ResultadoAplicacionOcr.Rechazar("Campo destino no soportado para OCR.");
        }

        private ResultadoAplicacionOcr PrepararOcrOrientacion(string textoOcr, float confianza)
        {
            string crudo = textoOcr ?? string.Empty;

            Debug.WriteLine("==================================================");
            Debug.WriteLine("[OCR ORIENTACION] Confianza: " + confianza);
            Debug.WriteLine("[OCR ORIENTACION] Crudo:");
            Debug.WriteLine(crudo);

            if (string.IsNullOrWhiteSpace(crudo))
            {
                Debug.WriteLine("[OCR ORIENTACION] Rechazado: texto vacío.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "El OCR no detectó texto para Orientación prestacional.");
            }

            // ==========================================================
            // CLAVE:
            // Primero intentamos detectar una orientación conocida.
            // Aunque el OCR traiga basura visual después, si aparece una opción
            // válida como PRESTACIONES DE REHABILITACION, debe aceptarse.
            // ==========================================================
            string orientacionCanonica = IndexadorCudService.NormalizarOrientacionDesdeOcr(crudo);

            Debug.WriteLine("[OCR ORIENTACION] Canonizada desde crudo:");
            Debug.WriteLine(orientacionCanonica);

            if (!string.IsNullOrWhiteSpace(orientacionCanonica))
            {
                Debug.WriteLine("[OCR ORIENTACION] OK por opción conocida.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Ok(
                    orientacionCanonica,
                    "Orientación prestacional OCR aceptada.");
            }

            // ==========================================================
            // Segundo intento: limpieza suave y nueva canonización.
            // Esto ayuda cuando el encabezado viene mal leído:
            // DIRIEN TACIÓN PRESIACIONAL, ORIENTACION PRESTACIONAL, etc.
            // ==========================================================
            string limpio = LimpiarRuidoTextoLibreOcr(crudo);

            Debug.WriteLine("[OCR ORIENTACION] Limpio:");
            Debug.WriteLine(limpio);

            orientacionCanonica = IndexadorCudService.NormalizarOrientacionDesdeOcr(limpio);

            Debug.WriteLine("[OCR ORIENTACION] Canonizada desde limpio:");
            Debug.WriteLine(orientacionCanonica);

            if (!string.IsNullOrWhiteSpace(orientacionCanonica))
            {
                Debug.WriteLine("[OCR ORIENTACION] OK por opción conocida luego de limpieza.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Ok(
                    orientacionCanonica,
                    "Orientación prestacional OCR aceptada.");
            }

            // ==========================================================
            // Recién acá aplicamos filtro de basura.
            // Para orientación no debe bloquear antes, porque el OCR puede traer
            // símbolos extra pero igual contener una opción válida.
            // ==========================================================
            if (TextoLibreOcrEsBasura(crudo))
            {
                Debug.WriteLine("[OCR ORIENTACION] Rechazado: no se detectó opción conocida y el texto parece basura OCR.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "El OCR no detectó una orientación prestacional válida. Ajuste el recorte.",
                    crudo);
            }

            if (confianza < 0.20f)
            {
                Debug.WriteLine("[OCR ORIENTACION] Rechazado: confianza baja y sin opción conocida.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "Confianza OCR muy baja para Orientación prestacional. Revise el recorte.",
                    crudo);
            }

            Debug.WriteLine("[OCR ORIENTACION] Rechazado: texto no coincide con opciones conocidas.");
            Debug.WriteLine("==================================================");

            return ResultadoAplicacionOcr.Rechazar(
                "El OCR no detectó una orientación prestacional válida.",
                crudo);
        }

        private ResultadoAplicacionOcr PrepararOcrNumeroCertificado(string textoOcr, float confianza)
        {
            string t = textoOcr ?? string.Empty;

            t = t.ToUpperInvariant();

            t = t.Replace("—", "-")
                 .Replace("–", "-")
                 .Replace("−", "-")
                 .Replace("_", "-");

            t = Regex.Replace(t, @"\s+", "");

            Match m = Regex.Match(
                t,
                @"[A-Z]{3}-\d{2}-\d{11}-\d{8}-\d{8}-[A-Z]{3}-\d{3}",
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
            letraEsperada = char.ToLowerInvariant(letraEsperada);

            string crudo = textoOcr ?? string.Empty;

            Debug.WriteLine("==================================================");
            Debug.WriteLine("[OCR CIF] INICIO");
            Debug.WriteLine("[OCR CIF] Campo: " + nombreCampo);
            Debug.WriteLine("[OCR CIF] Letra esperada: " + letraEsperada);
            Debug.WriteLine("[OCR CIF] Confianza OCR: " + confianza);
            Debug.WriteLine("[OCR CIF] Texto OCR crudo:");
            Debug.WriteLine(crudo);

            string preparado = IndexadorCudService.PrepararTextoCifDesdeOcr(
                crudo,
                letraEsperada);

            Debug.WriteLine("[OCR CIF] Texto preparado por servicio:");
            Debug.WriteLine(preparado);

            if (string.IsNullOrWhiteSpace(preparado))
            {
                Debug.WriteLine("[OCR CIF] Rechazado: el servicio devolvió texto preparado vacío.");
                Debug.WriteLine("[OCR CIF] Esto indica que PrepararTextoCifDesdeOcr no detectó candidatos válidos o los descartó todos.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "El OCR no produjo códigos CIF recuperables para " + nombreCampo + ". Revise el recorte o el patrón de normalización.",
                    preparado);
            }

            ResultadoValidacion validacion = IndexadorCudService.ValidarCodigosCif(
                preparado,
                letraEsperada,
                nombreCampo,
                true);

            Debug.WriteLine("[OCR CIF] Resultado validación:");
            Debug.WriteLine("[OCR CIF] EsValido: " + validacion.EsValido);
            Debug.WriteLine("[OCR CIF] Severidad: " + validacion.Severidad);
            Debug.WriteLine("[OCR CIF] Mensaje: " + validacion.Mensaje);

            if (!validacion.EsValido)
            {
                Debug.WriteLine("[OCR CIF] Rechazado por validación final.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "El OCR detectó texto CIF, pero no pasó la validación para " + nombreCampo + ": " + validacion.Mensaje,
                    preparado);
            }

            Debug.WriteLine("[OCR CIF] OK: " + preparado);
            Debug.WriteLine("==================================================");

            return ResultadoAplicacionOcr.Ok(preparado, "CIF OCR válido.");
        }


        private ResultadoAplicacionOcr PrepararOcrDiagnostico(string textoOcr, float confianza)
        {
            string crudo = textoOcr ?? string.Empty;

            Debug.WriteLine("==================================================");
            Debug.WriteLine("[OCR DIAGNOSTICO] INICIO");
            Debug.WriteLine("[OCR DIAGNOSTICO] Confianza OCR: " + confianza);
            Debug.WriteLine("[OCR DIAGNOSTICO] Texto OCR crudo:");
            Debug.WriteLine(crudo);

            // ==========================================================
            // IMPORTANTE:
            // No rechazamos el texto crudo completo por basura.
            // Puede venir una línea basura arriba y una línea útil abajo.
            // Primero limpiamos por líneas y después validamos el resultado.
            // ==========================================================

            string t = LimpiarDiagnosticoDesdeOcr(crudo);

            Debug.WriteLine("[OCR DIAGNOSTICO] Texto limpio:");
            Debug.WriteLine(t);

            if (string.IsNullOrWhiteSpace(t))
            {
                Debug.WriteLine("[OCR DIAGNOSTICO] Rechazado: no quedó texto útil luego de limpiar.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "El OCR no detectó texto útil para Diagnóstico.",
                    string.Empty);
            }

            if (TextoDiagnosticoLimpioSigueSiendoBasura(t))
            {
                Debug.WriteLine("[OCR DIAGNOSTICO] Rechazado: el texto limpio sigue pareciendo basura OCR.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "El OCR no produjo texto médico útil para Diagnóstico. Revise el recorte.",
                    t);
            }

            if (t.Length < 3)
            {
                Debug.WriteLine("[OCR DIAGNOSTICO] Rechazado: texto demasiado corto.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "El texto OCR para Diagnóstico es demasiado corto.",
                    t);
            }

            if (confianza < 0.25f)
            {
                Debug.WriteLine("[OCR DIAGNOSTICO] Rechazado: confianza OCR baja.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "Confianza OCR muy baja para Diagnóstico. Revise el recorte o amplíe la selección.",
                    t);
            }

            Debug.WriteLine("[OCR DIAGNOSTICO] OK:");
            Debug.WriteLine(t);
            Debug.WriteLine("==================================================");

            return ResultadoAplicacionOcr.Ok(t, "Diagnóstico OCR aceptado.");
        }

        private bool TextoDiagnosticoLimpioSigueSiendoBasura(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return true;

            string t = texto.Trim().ToUpperInvariant();

            t = t.Replace("Á", "A")
                 .Replace("É", "E")
                 .Replace("Í", "I")
                 .Replace("Ó", "O")
                 .Replace("Ú", "U")
                 .Replace("Ü", "U");

            string limpio = Regex.Replace(t, @"[^\p{L}\d\s]", " ");
            limpio = Regex.Replace(limpio, @"\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(limpio))
                return true;

            string[] tokens = limpio
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int palabrasBuenas = 0;

            foreach (string tokenOriginal in tokens)
            {
                string token = Regex.Replace(tokenOriginal, @"[^A-ZÑ]", "");

                if (token.Length >= 3 && Regex.IsMatch(token, @"[AEIOU]"))
                    palabrasBuenas++;
            }

            return palabrasBuenas == 0;
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

                Debug.WriteLine("[OCR DIAGNOSTICO] Línea cruda:");
                Debug.WriteLine(linea);

                // ==========================================================
                // 1) NORMALIZACIÓN DE BASURA VISUAL OCR
                // ==========================================================

                linea = linea.Replace("“", "\"")
                             .Replace("”", "\"")
                             .Replace("„", "\"")
                             .Replace("´", "'")
                             .Replace("`", "'")
                             .Replace("—", " ")
                             .Replace("–", " ")
                             .Replace("_", " ")
                             .Replace("¬", " ")
                             .Replace("°", " ")
                             .Replace("|", " ")
                             .Replace(">", " ")
                             .Replace("<", " ")
                             .Replace("[", " ")
                             .Replace("]", " ")
                             .Replace("{", " ")
                             .Replace("}", " ")
                             .Replace("»", " ")
                             .Replace("«", " ")
                             .Replace("+", " ")
                             .Replace("$", " ")
                             .Replace("%", " ");

                // Quita comillas sueltas y restos tipo 4” > 7 ”,-, |
                linea = Regex.Replace(linea, @"[""']+", " ");
                linea = Regex.Replace(linea, @"\b\d{1,2}\s*[""']\s*", " ");

                // ==========================================================
                // 2) QUITAR RÓTULO DIAGNÓSTICO EN VARIANTES OCR
                // ==========================================================

                linea = Regex.Replace(
                    linea,
                    @"\bD\s*I\s*A?\s*G\s*N\s*[OÓ0]\s*S\s*T\s*I\s*C\s*[O0]\b\s*[:\-]?",
                    " ",
                    RegexOptions.IgnoreCase);

                linea = Regex.Replace(
                    linea,
                    @"\bDI\s+GN[OÓ0]STIC[O0]\b\s*[:\-]?",
                    " ",
                    RegexOptions.IgnoreCase);

                linea = Regex.Replace(
                    linea,
                    @"\bDIAGN[OÓ0]STIC[O0]\b\s*[:\-]?",
                    " ",
                    RegexOptions.IgnoreCase);

                // ==========================================================
                // 3) LIMPIEZA DE SEPARADORES
                // ==========================================================

                linea = Regex.Replace(linea, @"(?:[\.;,:/\-]\s*){2,}", " ");
                linea = Regex.Replace(linea, @"\s+", " ").Trim();

                linea = Regex.Replace(linea, @"^[\-\.:;,/\\\|> <""' ]+", "").Trim();
                linea = Regex.Replace(linea, @"[\-\.:;,/\\\|> <""' ]+$", "").Trim();

                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                Debug.WriteLine("[OCR DIAGNOSTICO] Línea prefiltrada:");
                Debug.WriteLine(linea);

                // ==========================================================
                // 4) DESCARTAR LÍNEAS BASURA, NO TODO EL TEXTO
                // ==========================================================

                if (EsLineaRuidoDiagnosticoOcr(linea))
                {
                    Debug.WriteLine("[OCR DIAGNOSTICO] Línea descartada por ruido:");
                    Debug.WriteLine(linea);
                    continue;
                }

                // ==========================================================
                // 5) LIMPIEZA EXTRA DE FRAGMENTOS RAROS DENTRO DE LA LÍNEA
                // ==========================================================

                linea = LimpiarFragmentosBasuraDiagnostico(linea);

                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                if (EsLineaRuidoDiagnosticoOcr(linea))
                {
                    Debug.WriteLine("[OCR DIAGNOSTICO] Línea descartada luego de limpiar fragmentos:");
                    Debug.WriteLine(linea);
                    continue;
                }

                Debug.WriteLine("[OCR DIAGNOSTICO] Línea aceptada:");
                Debug.WriteLine(linea);

                lineasUtiles.Add(linea);
            }

            string resultado = string.Join(" ", lineasUtiles);

            resultado = Regex.Replace(resultado, @"\s+", " ").Trim();

            // Limpieza final de rótulos por si quedaron después de unir líneas.
            resultado = Regex.Replace(
                resultado,
                @"\bD\s*I\s*A?\s*G\s*N\s*[OÓ0]\s*S\s*T\s*I\s*C\s*[O0]\b\s*[:\-]?",
                " ",
                RegexOptions.IgnoreCase);

            resultado = Regex.Replace(
                resultado,
                @"\bDIAGN[OÓ0]STIC[O0]\b\s*[:\-]?",
                " ",
                RegexOptions.IgnoreCase);

            resultado = Regex.Replace(resultado, @"\s+", " ").Trim();

            resultado = Regex.Replace(resultado, @"^[\-\.:;,/\\\|> <""' ]+", "").Trim();
            resultado = Regex.Replace(resultado, @"[\-\.:;,/\\\|> <""' ]+$", "").Trim();

            return resultado;
        }

        private string LimpiarFragmentosBasuraDiagnostico(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string[] tokens = texto
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> utiles = new List<string>();

            foreach (string tokenOriginal in tokens)
            {
                string token = tokenOriginal.Trim();

                if (string.IsNullOrWhiteSpace(token))
                    continue;

                string tokenEval = token.ToUpperInvariant();

                tokenEval = tokenEval.Replace("Á", "A")
                                     .Replace("É", "E")
                                     .Replace("Í", "I")
                                     .Replace("Ó", "O")
                                     .Replace("Ú", "U")
                                     .Replace("Ü", "U");

                string soloLetras = Regex.Replace(tokenEval, @"[^A-ZÑ]", "");
                string soloLetrasNumeros = Regex.Replace(tokenEval, @"[^A-ZÑ0-9]", "");

                // Conserva conectores médicos/castellanos cortos.
                if (EsTokenCortoPermitidoDiagnostico(tokenEval))
                {
                    utiles.Add(token);
                    continue;
                }

                // Descarta tokens vacíos luego de limpiar.
                if (string.IsNullOrWhiteSpace(soloLetrasNumeros))
                    continue;

                // Descarta tokens larguísimos pegados de OCR:
                // MDDMDOCCqAADO, tttZZDeqepegeohidEooooooilio
                if (soloLetras.Length >= 10)
                {
                    int vocales = soloLetras.Count(c => "AEIOU".Contains(c));
                    double ratioVocales = soloLetras.Length == 0 ? 0 : (double)vocales / soloLetras.Length;

                    bool repeticion = Regex.IsMatch(soloLetras, @"([A-ZÑ])\1{3,}");
                    bool consonantesLargas = Regex.IsMatch(soloLetras, @"[BCDFGHJKLMNPQRSTVWXYZÑ]{6,}");

                    if (ratioVocales < 0.30 || repeticion || consonantesLargas)
                        continue;
                }

                // Descarta tokens de símbolos/números tipo ;—1
                int letras = token.Count(char.IsLetter);
                int digitos = token.Count(char.IsDigit);
                int simbolos = token.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

                if (letras == 0 && (digitos > 0 || simbolos > 0))
                    continue;

                if (letras <= 1 && simbolos >= 1)
                    continue;

                // Descarta cadenas cortas raras sin vocales, salvo siglas conocidas.
                if (soloLetras.Length >= 3 &&
                    !Regex.IsMatch(soloLetras, @"[AEIOU]") &&
                    !EsSiglaMedicaPermitidaDiagnostico(soloLetras))
                {
                    continue;
                }

                utiles.Add(token);
            }

            string resultado = string.Join(" ", utiles);
            resultado = Regex.Replace(resultado, @"\s+", " ").Trim();

            return resultado;
        }

        private bool EsTokenCortoPermitidoDiagnostico(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            string t = token.Trim().ToUpperInvariant();

            t = t.Replace(".", "")
                 .Replace(",", "")
                 .Replace(";", "")
                 .Replace(":", "");

            string[] permitidos =
            {
        "DE",
        "DEL",
        "LA",
        "LAS",
        "LOS",
        "EL",
        "Y",
        "E",
        "EN",
        "CON",
        "POR",
        "SIN",
        "A",
        "AL"
    };

            return permitidos.Contains(t);
        }

        private bool EsSiglaMedicaPermitidaDiagnostico(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            string t = token.Trim().ToUpperInvariant();

            string[] siglas =
            {
        "TEA",
        "TGD",
        "HIV",
        "ACV",
        "EPOC",
        "ELA",
        "DBT",
        "HTA",
        "IAM"
    };

            return siglas.Contains(t);
        }

        private bool EsLineaRuidoDiagnosticoOcr(string linea)
        {
            if (string.IsNullOrWhiteSpace(linea))
                return true;

            string l = linea.Trim();

            // Líneas hechas solo de separadores.
            if (Regex.IsMatch(l, @"^[\-\._=~:,;|\\/ > <""' ]+$"))
                return true;

            int total = l.Length;
            int letras = l.Count(char.IsLetter);
            int digitos = l.Count(char.IsDigit);
            int espacios = l.Count(char.IsWhiteSpace);
            int simbolos = total - letras - digitos - espacios;

            // Ejemplo: 4” > 7 ”,-, |
            if (letras == 0 && digitos > 0 && simbolos > 0)
                return true;

            if (letras <= 1 && simbolos >= 2)
                return true;

            string soloLetrasNumeros = Regex.Replace(l, @"[^\p{L}\d]", "");

            if (string.IsNullOrWhiteSpace(soloLetrasNumeros))
                return true;

            // Si tiene una cantidad razonable de palabras con vocales, no es basura.
            string limpio = Regex.Replace(l, @"[^\p{L}\d\s]", " ");
            limpio = Regex.Replace(limpio, @"\s+", " ").Trim();

            string[] tokens = limpio
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int palabrasBuenas = 0;

            foreach (string tokenOriginal in tokens)
            {
                string token = tokenOriginal.ToUpperInvariant();

                token = token.Replace("Á", "A")
                             .Replace("É", "E")
                             .Replace("Í", "I")
                             .Replace("Ó", "O")
                             .Replace("Ú", "U")
                             .Replace("Ü", "U");

                string soloLetras = Regex.Replace(token, @"[^A-ZÑ]", "");

                if (soloLetras.Length >= 3 && Regex.IsMatch(soloLetras, @"[AEIOU]"))
                    palabrasBuenas++;
            }

            if (palabrasBuenas >= 2)
                return false;

            // Caso del log: "rs", "oo", "uciw oo", etc.
            if (soloLetrasNumeros.Length <= 3 &&
                !Regex.IsMatch(soloLetrasNumeros, @"[aeiouáéíóúAEIOUÁÉÍÓÚ]") &&
                !Regex.IsMatch(soloLetrasNumeros, @"\d"))
            {
                return true;
            }

            // Muchas repeticiones: ZZZZ, DDDD.
            string normal = l.ToUpperInvariant()
                             .Replace("Á", "A")
                             .Replace("É", "E")
                             .Replace("Í", "I")
                             .Replace("Ó", "O")
                             .Replace("Ú", "U");

            if (Regex.IsMatch(normal, @"([A-ZÑ])\1{4,}"))
                return true;

            return false;
        }



        private ResultadoAplicacionOcr PrepararOcrTextoLibre(
     string textoOcr,
     string nombreCampo,
     float confianza,
     int minimoCaracteres)
        {
            string crudo = textoOcr ?? string.Empty;

            Debug.WriteLine("==================================================");
            Debug.WriteLine("[OCR TEXTO LIBRE] Campo: " + nombreCampo);
            Debug.WriteLine("[OCR TEXTO LIBRE] Confianza: " + confianza);
            Debug.WriteLine("[OCR TEXTO LIBRE] Crudo:");
            Debug.WriteLine(crudo);

            if (TextoLibreOcrEsBasura(crudo))
            {
                Debug.WriteLine("[OCR TEXTO LIBRE] Rechazado: texto crudo parece basura OCR.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "El OCR devolvió mayormente símbolos o texto basura para " + nombreCampo + ". Amplíe o ajuste el recorte.",
                    crudo);
            }

            string t = IndexadorCudService.NormalizarTextoLibre(crudo);
            t = LimpiarRuidoTextoLibreOcr(t);

            Debug.WriteLine("[OCR TEXTO LIBRE] Limpio:");
            Debug.WriteLine(t);

            if (TextoLibreOcrEsBasura(t))
            {
                Debug.WriteLine("[OCR TEXTO LIBRE] Rechazado: texto limpio sigue pareciendo basura OCR.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "El OCR no produjo texto útil para " + nombreCampo + ". Revise el recorte.",
                    t);
            }

            if (string.IsNullOrWhiteSpace(t))
            {
                Debug.WriteLine("[OCR TEXTO LIBRE] Rechazado: texto vacío.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "El OCR no detectó texto útil para " + nombreCampo + ".");
            }

            if (t.Length < minimoCaracteres)
            {
                Debug.WriteLine("[OCR TEXTO LIBRE] Rechazado: texto demasiado corto.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "El texto OCR para " + nombreCampo + " es demasiado corto.",
                    t);
            }

            // Umbral bajo porque Tesseract puede dar confianza baja en texto correcto.
            if (confianza < 0.25f)
            {
                Debug.WriteLine("[OCR TEXTO LIBRE] Rechazado: confianza baja.");
                Debug.WriteLine("==================================================");

                return ResultadoAplicacionOcr.Rechazar(
                    "Confianza OCR muy baja para " + nombreCampo + ". Revise el recorte o amplíe la selección.",
                    t);
            }

            if (nombreCampo == "Apellido y nombre")
            {
                t = IndexadorCudService.NormalizarNombreApellido(t);

                ResultadoValidacion valNombre =
                    IndexadorCudService.ValidarApellidoNombrePreprocesamiento(t);

                if (!valNombre.EsValido)
                {
                    Debug.WriteLine("[OCR TEXTO LIBRE] Rechazado por validación de apellido/nombre: " + valNombre.Mensaje);
                    Debug.WriteLine("==================================================");

                    return ResultadoAplicacionOcr.Rechazar(
                        valNombre.Mensaje,
                        t);
                }
            }

            if (nombreCampo == "Orientación prestacional")
            {
                // Para orientación no conviene aceptar texto libre desde OCR.
                // Usamos método estricto: si no detecta opción conocida, rechaza.
                t = IndexadorCudService.NormalizarOrientacionDesdeOcr(t);

                if (string.IsNullOrWhiteSpace(t))
                {
                    Debug.WriteLine("[OCR TEXTO LIBRE] Rechazado: orientación no coincide con opciones conocidas.");
                    Debug.WriteLine("==================================================");

                    return ResultadoAplicacionOcr.Rechazar(
                        "El OCR no detectó una orientación prestacional válida.",
                        textoOcr);
                }
            }

            Debug.WriteLine("[OCR TEXTO LIBRE] OK:");
            Debug.WriteLine(t);
            Debug.WriteLine("==================================================");

            return ResultadoAplicacionOcr.Ok(t, nombreCampo + " OCR aceptado.");
        }

        private string LimpiarRuidoTextoLibreOcr(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim();

            t = t.Replace("\r", " ")
                 .Replace("\n", " ")
                 .Replace("\t", " ");

            t = t.Replace("—", " ")
                 .Replace("–", " ")
                 .Replace("_", " ")
                 .Replace("¬", " ")
                 .Replace("°", " ")
                 .Replace("]", " ")
                 .Replace("[", " ")
                 .Replace(">", " ")
                 .Replace("<", " ")
                 .Replace("$", " ")
                 .Replace("%", " ")
                 .Replace("|", " ");

            // Borra cadenas largas de separadores.
            t = Regex.Replace(t, @"[-_;:,\.\=]{2,}", " ");

            // Deja letras, números, espacios y algunos signos razonables.
            t = Regex.Replace(t, @"[^\p{L}\d\s\-/\(\)\.,]", " ");

            t = Regex.Replace(t, @"\s+", " ").Trim();

            // Limpieza de separadores colgados al inicio/final.
            t = Regex.Replace(t, @"^[\-\.,;:/ ]+", "").Trim();
            t = Regex.Replace(t, @"[\-\.,;:/ ]+$", "").Trim();

            return t;
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



        // PARTE NUEVA DIVISION DEL NUMERO DE CERTIFICADO EN BLOQUES PARA MEJORAR OCR Y VALIDACION


       
        private void ConfigurarBloquesNumeroCertificado()
        {
            _configurandoBloquesCertificado = true;

            try
            {
                // Valores por defecto
                TxtCertBloque1.Text = "ARG";
                TxtCertBloque6.Text = "CBA";
                TxtCertBloque7.Text = "536";

                // Largo máximo por bloque
                TxtCertBloque1.MaxLength = 3;   // ARG
                TxtCertBloque2.MaxLength = 2;   // 01 / 02
                TxtCertBloque3.MaxLength = 11;  // DNI interno con ceros
                TxtCertBloque4.MaxLength = 8;   // Fecha emisión yyyyMMdd
                TxtCertBloque5.MaxLength = 8;   // Fecha vencimiento yyyyMMdd
                TxtCertBloque6.MaxLength = 3;   // CBA
                TxtCertBloque7.MaxLength = 3;   // 536

                // Mayúsculas
                TxtCertBloque1.CharacterCasing = CharacterCasing.Upper;
                TxtCertBloque6.CharacterCasing = CharacterCasing.Upper;

                // Alineación visual
                TxtCertBloque1.TextAlign = HorizontalAlignment.Center;
                TxtCertBloque2.TextAlign = HorizontalAlignment.Center;
                TxtCertBloque3.TextAlign = HorizontalAlignment.Center;
                TxtCertBloque4.TextAlign = HorizontalAlignment.Center;
                TxtCertBloque5.TextAlign = HorizontalAlignment.Center;
                TxtCertBloque6.TextAlign = HorizontalAlignment.Center;
                TxtCertBloque7.TextAlign = HorizontalAlignment.Center;

                // Evita que el usuario modifique los bloques que casi siempre son fijos.
                // Si preferís dejarlos editables, cambiá ReadOnly = false.
                TxtCertBloque1.ReadOnly = false;
                TxtCertBloque6.ReadOnly = false;
                TxtCertBloque7.ReadOnly = false;

                // Escritura permitida
                TxtCertBloque1.KeyPress += KeyPressLetrasCertificadoCud;
                TxtCertBloque6.KeyPress += KeyPressLetrasCertificadoCud;

                TxtCertBloque2.KeyPress += KeyPressSoloNumerosCud;
                TxtCertBloque3.KeyPress += KeyPressSoloNumerosCud;
                TxtCertBloque4.KeyPress += KeyPressSoloNumerosCud;
                TxtCertBloque5.KeyPress += KeyPressSoloNumerosCud;
                TxtCertBloque7.KeyPress += KeyPressSoloNumerosCud;

                // TextChanged con salto automático
                TxtCertBloque1.TextChanged += BloqueNumeroCertificado_TextChangedCud;
                TxtCertBloque2.TextChanged += BloqueNumeroCertificado_TextChangedCud;
                TxtCertBloque3.TextChanged += BloqueNumeroCertificado_TextChangedCud;
                TxtCertBloque4.TextChanged += BloqueNumeroCertificado_TextChangedCud;
                TxtCertBloque5.TextChanged += BloqueNumeroCertificado_TextChangedCud;
                TxtCertBloque6.TextChanged += BloqueNumeroCertificado_TextChangedCud;
                TxtCertBloque7.TextChanged += BloqueNumeroCertificado_TextChangedCud;

                // Leave para normalizar cada bloque
                TxtCertBloque1.Leave += BloqueNumeroCertificado_LeaveCud;
                TxtCertBloque2.Leave += BloqueNumeroCertificado_LeaveCud;
                TxtCertBloque3.Leave += BloqueNumeroCertificado_LeaveCud;
                TxtCertBloque4.Leave += BloqueNumeroCertificado_LeaveCud;
                TxtCertBloque5.Leave += BloqueNumeroCertificado_LeaveCud;
                TxtCertBloque6.Leave += BloqueNumeroCertificado_LeaveCud;
                TxtCertBloque7.Leave += BloqueNumeroCertificado_LeaveCud;

                // Navegación Enter / flechas
                TxtCertBloque1.KeyDown += ControlCarga_KeyDownCud;
                TxtCertBloque2.KeyDown += ControlCarga_KeyDownCud;
                TxtCertBloque3.KeyDown += ControlCarga_KeyDownCud;
                TxtCertBloque4.KeyDown += ControlCarga_KeyDownCud;
                TxtCertBloque5.KeyDown += ControlCarga_KeyDownCud;
                TxtCertBloque6.KeyDown += ControlCarga_KeyDownCud;
                TxtCertBloque7.KeyDown += ControlCarga_KeyDownCud;

                _toolTipCud.SetToolTip(TxtCertBloque1, "Prefijo del certificado. Normalmente ARG.");
                _toolTipCud.SetToolTip(TxtCertBloque2, "Tipo de certificado. Ingrese 01 o 02.");
                _toolTipCud.SetToolTip(TxtCertBloque3, "DNI interno. Puede cargar el DNI sin ceros; al salir se completa a 11 dígitos.");
                _toolTipCud.SetToolTip(TxtCertBloque4, "Fecha de emisión en formato AAAAMMDD.");
                _toolTipCud.SetToolTip(TxtCertBloque5, "Fecha de vencimiento en formato AAAAMMDD.");
                _toolTipCud.SetToolTip(TxtCertBloque6, "Jurisdicción. Normalmente CBA.");
                _toolTipCud.SetToolTip(TxtCertBloque7, "Código final. Normalmente 536.");
            }
            finally
            {
                _configurandoBloquesCertificado = false;
            }
        }
        private void KeyPressLetrasCertificadoCud(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if (!char.IsLetter(e.KeyChar))
            {
                e.Handled = true;
                return;
            }

            e.KeyChar = char.ToUpperInvariant(e.KeyChar);
        }
        private void BloqueNumeroCertificado_TextChangedCud(object sender, EventArgs e)
        {
            if (_cargandoDatosCud || _normalizandoCud || _sincronizandoCamposCud || _configurandoBloquesCertificado)
                return;

            TextBox txt = sender as TextBox;

            if (txt == null)
                return;

            NormalizarBloqueCertificadoEnVivo(txt);

            string numero = ReconstruirNumeroCertificadoDesdeBloques();

            _sincronizandoCamposCud = true;

            try
            {
                TxtNumeCert.Text = numero;
            }
            finally
            {
                _sincronizandoCamposCud = false;
            }

            DatosNumeroCertificadoCud datos = IndexadorCudService.ParseNumeroCertificado(numero);

            if (datos.EsValido)
            {
                MarcarBloquesNumeroCertificado(
                    ResultadoValidacion.Ok("Número de certificado válido."));
            }
            else
            {
                LimpiarErroresBloquesNumeroCertificado();
            }

            AvanzarSiBloqueCertificadoCompleto(txt);
        }
        private void NormalizarBloqueCertificadoEnVivo(TextBox txt)
        {
            if (txt == null)
                return;

            string original = txt.Text;
            string limpio = original;

            if (txt == TxtCertBloque1 || txt == TxtCertBloque6)
            {
                limpio = Regex.Replace(original.ToUpperInvariant(), @"[^A-Z]", "");
            }
            else
            {
                limpio = Regex.Replace(original, @"\D", "");
            }

            if (limpio == original)
                return;

            int pos = txt.SelectionStart;

            _sincronizandoCamposCud = true;

            try
            {
                txt.Text = limpio;
                txt.SelectionStart = Math.Min(pos, txt.Text.Length);
                txt.SelectionLength = 0;
            }
            finally
            {
                _sincronizandoCamposCud = false;
            }
        }

        private void BloqueNumeroCertificado_LeaveCud(object sender, EventArgs e)
        {
            TextBox txt = sender as TextBox;

            if (txt == null)
                return;

            NormalizarBloqueCertificadoAlSalir(txt);

            SincronizarNumeroCertificadoDesdeBloques(validarYTriangular: true);
        }
        private void NormalizarBloqueCertificadoAlSalir(TextBox txt)
        {
            if (txt == null)
                return;

            _sincronizandoCamposCud = true;

            try
            {
                if (txt == TxtCertBloque1)
                {
                    txt.Text = Regex.Replace(txt.Text.ToUpperInvariant(), @"[^A-Z]", "");

                    if (string.IsNullOrWhiteSpace(txt.Text))
                        txt.Text = "ARG";
                }
                else if (txt == TxtCertBloque2)
                {
                    string tipo = Regex.Replace(txt.Text, @"\D", "");

                    if (tipo.Length == 1)
                        tipo = tipo.PadLeft(2, '0');

                    txt.Text = tipo;
                }
                else if (txt == TxtCertBloque3)
                {
                    string dni = Regex.Replace(txt.Text, @"\D", "");

                    if (!string.IsNullOrWhiteSpace(dni) && dni.Length <= 11)
                        dni = dni.PadLeft(11, '0');

                    txt.Text = dni;
                }
                else if (txt == TxtCertBloque4 || txt == TxtCertBloque5)
                {
                    txt.Text = Regex.Replace(txt.Text, @"\D", "");
                }
                else if (txt == TxtCertBloque6)
                {
                    txt.Text = Regex.Replace(txt.Text.ToUpperInvariant(), @"[^A-Z]", "");

                    if (string.IsNullOrWhiteSpace(txt.Text))
                        txt.Text = "CBA";
                }
                else if (txt == TxtCertBloque7)
                {
                    string final = Regex.Replace(txt.Text, @"\D", "");

                    if (string.IsNullOrWhiteSpace(final))
                        final = "536";

                    txt.Text = final;
                }
            }
            finally
            {
                _sincronizandoCamposCud = false;
            }
        }
        private string ReconstruirNumeroCertificadoDesdeBloques()
        {
            string b1 = (TxtCertBloque1.Text ?? "").Trim().ToUpperInvariant();
            string b2 = Regex.Replace(TxtCertBloque2.Text ?? "", @"\D", "");
            string b3 = Regex.Replace(TxtCertBloque3.Text ?? "", @"\D", "");
            string b4 = Regex.Replace(TxtCertBloque4.Text ?? "", @"\D", "");
            string b5 = Regex.Replace(TxtCertBloque5.Text ?? "", @"\D", "");
            string b6 = (TxtCertBloque6.Text ?? "").Trim().ToUpperInvariant();
            string b7 = Regex.Replace(TxtCertBloque7.Text ?? "", @"\D", "");

            if (string.IsNullOrWhiteSpace(b1))
                b1 = "ARG";

            if (string.IsNullOrWhiteSpace(b6))
                b6 = "CBA";

            if (string.IsNullOrWhiteSpace(b7))
                b7 = "536";

            if (!string.IsNullOrWhiteSpace(b3) && b3.Length <= 11)
                b3 = b3.PadLeft(11, '0');

            string numero = $"{b1}-{b2}-{b3}-{b4}-{b5}-{b6}-{b7}";

            return IndexadorCudService.NormalizarNumeroCertificado(numero);
        }

        private void CargarBloquesNumeroCertificadoDesdeTexto(string numeroCertificado)
        {
            string n = IndexadorCudService.NormalizarNumeroCertificado(numeroCertificado);

            string[] partes = n.Split('-');

            if (partes.Length != 7)
            {
                Debug.WriteLine("[CERT BLOQUES] No se pudo dividir número certificado: " + numeroCertificado);
                return;
            }

            _sincronizandoCamposCud = true;

            try
            {
                TxtCertBloque1.Text = partes[0];
                TxtCertBloque2.Text = partes[1];
                TxtCertBloque3.Text = partes[2];
                TxtCertBloque4.Text = partes[3];
                TxtCertBloque5.Text = partes[4];
                TxtCertBloque6.Text = partes[5];
                TxtCertBloque7.Text = partes[6];

                TxtNumeCert.Text = n;

                Debug.WriteLine("[CERT BLOQUES] Número dividido correctamente: " + n);
            }
            finally
            {
                _sincronizandoCamposCud = false;
            }
        }
        private void AvanzarSiBloqueCertificadoCompleto(TextBox txt)
        {
            if (txt == null)
                return;

            if (!txt.Focused)
                return;

            if (txt == TxtCertBloque1 && txt.Text.Length == 3)
            {
                TxtCertBloque2.Focus();
                TxtCertBloque2.SelectAll();
                return;
            }

            if (txt == TxtCertBloque2 && txt.Text.Length == 2)
            {
                TxtCertBloque3.Focus();
                TxtCertBloque3.SelectAll();
                return;
            }

            // En DNI no conviene saltar con 8 dígitos porque al salir se completa a 11.
            // Si el OCR o el usuario pegó 11 dígitos, ahí sí puede avanzar.
            if (txt == TxtCertBloque3 && txt.Text.Length == 11)
            {
                TxtCertBloque4.Focus();
                TxtCertBloque4.SelectAll();
                return;
            }

            if (txt == TxtCertBloque4 && txt.Text.Length == 8)
            {
                TxtCertBloque5.Focus();
                TxtCertBloque5.SelectAll();
                return;
            }

            if (txt == TxtCertBloque5 && txt.Text.Length == 8)
            {
                TxtCertBloque6.Focus();
                TxtCertBloque6.SelectAll();
                return;
            }

            if (txt == TxtCertBloque6 && txt.Text.Length == 3)
            {
                TxtCertBloque7.Focus();
                TxtCertBloque7.SelectAll();
                return;
            }

            if (txt == TxtCertBloque7 && txt.Text.Length == 3)
            {
                TxtApellidoNombre.Focus();
                TxtApellidoNombre.SelectAll();
                return;
            }
        }


        private bool SincronizarNumeroCertificadoDesdeBloques(bool validarYTriangular)
        {
            string numero = ReconstruirNumeroCertificadoDesdeBloques();

            _sincronizandoCamposCud = true;

            try
            {
                TxtNumeCert.Text = numero;
            }
            finally
            {
                _sincronizandoCamposCud = false;
            }

            DatosNumeroCertificadoCud datos = IndexadorCudService.ParseNumeroCertificado(numero);

            if (!datos.EsValido)
            {
                if (validarYTriangular)
                    MarcarBloquesNumeroCertificado(ResultadoValidacion.Error(datos.Mensaje));

                return false;
            }

            MarcarBloquesNumeroCertificado(
                ResultadoValidacion.Ok("Número de certificado válido."));

            if (validarYTriangular)
            {
                AplicarTriangulacionCertificadoForzada(datos);
                ValidarFechasCruzadasEnPantalla();
            }

            return true;
        }


        private List<Control> ObtenerBloquesNumeroCertificado()
        {
            return new List<Control>
    {
        TxtCertBloque1,
        TxtCertBloque2,
        TxtCertBloque3,
        TxtCertBloque4,
        TxtCertBloque5,
        TxtCertBloque6,
        TxtCertBloque7
    };
        }

        private void MarcarBloquesNumeroCertificado(ResultadoValidacion resultado)
        {
            foreach (Control c in ObtenerBloquesNumeroCertificado())
            {
                MarcarCampo(c, resultado);
            }

            MarcarCampo(TxtNumeCert, resultado);
        }

        private void LimpiarErroresBloquesNumeroCertificado()
        {
            foreach (Control c in ObtenerBloquesNumeroCertificado())
            {
                c.BackColor = _colorOk;
                _erroresCud.SetError(c, string.Empty);
            }

            _erroresCud.SetError(TxtNumeCert, string.Empty);
        }

        private bool EsBloqueNumeroCertificado(Control control)
        {
            return control == TxtCertBloque1 ||
                   control == TxtCertBloque2 ||
                   control == TxtCertBloque3 ||
                   control == TxtCertBloque4 ||
                   control == TxtCertBloque5 ||
                   control == TxtCertBloque6 ||
                   control == TxtCertBloque7;
        }



        private void AplicarPerfilResolucion(PerfilResolucion perfil)
        {
            if (!_layoutCapturado)
                return;

            _aplicandoResolucion = true;

            float escala = ObtenerEscalaPerfil(perfil);

            this.SuspendLayout();

            AplicarEscalaControl(this, escala);

            this.ResumeLayout(true);

            _aplicandoResolucion = false;

            ForzarMaximizado();

            if (pictureBox1.Image != null)
            {
                AplicarZoomVisualCertificado();
                pictureBox1.Invalidate();
            }
        }


        private void ForzarMaximizado()
        {
            if (this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;

            this.BeginInvoke(new Action(delegate
            {
                if (!this.IsDisposed)
                    this.WindowState = FormWindowState.Maximized;
            }));
        }

        private string ObtenerClaveCacheOcrHdActual()
        {
            return (RutaNombreImagen ?? string.Empty) +
                   "|TOP=" + PORCENTAJE_MARGEN_SUPERIOR_CERTIFICADO.ToString("0.000", CultureInfo.InvariantCulture) +
                   "|LEFT=" + PORCENTAJE_MARGEN_IZQUIERDO_CERTIFICADO.ToString("0.000", CultureInfo.InvariantCulture) +
                   "|RIGHT=" + PORCENTAJE_MARGEN_DERECHO_CERTIFICADO.ToString("0.000", CultureInfo.InvariantCulture) +
                   "|ALTO_UTIL=" + ObtenerPorcentajeAltoUtilCertificado().ToString("0.000", CultureInfo.InvariantCulture);
        }

        private Bitmap ObtenerImagenOcrHdDesdeCache()
        {
            string claveCache = ObtenerClaveCacheOcrHdActual();

            if (_imagenOcrHdCache != null &&
                string.Equals(_rutaImagenOcrHdCache, claveCache, StringComparison.OrdinalIgnoreCase))
            {
                // Devolvemos una copia para que el OCR pueda disponerla sin romper la caché.
                return new Bitmap(_imagenOcrHdCache);
            }

            LimpiarCacheOcrHd();

            Bitmap render = RenderizarPdfActualAltaDefinicionParaOcr();

            if (render == null)
                return null;

            _imagenOcrHdCache = render;
            _rutaImagenOcrHdCache = claveCache;

            return new Bitmap(_imagenOcrHdCache);
        }

        private Bitmap ObtenerImagenOcrHdCacheParaPreview()
        {
            string claveCache = ObtenerClaveCacheOcrHdActual();

            if (_imagenOcrHdCache != null &&
                string.Equals(_rutaImagenOcrHdCache, claveCache, StringComparison.OrdinalIgnoreCase))
            {
                return _imagenOcrHdCache;
            }

            LimpiarCacheOcrHd();

            Bitmap render = RenderizarPdfActualAltaDefinicionParaOcr();

            if (render == null)
                return null;

            _imagenOcrHdCache = render;
            _rutaImagenOcrHdCache = claveCache;

            return _imagenOcrHdCache;
        }

        private void LimpiarCacheOcrHd()
        {
            if (_imagenOcrHdCache != null)
            {
                _imagenOcrHdCache.Dispose();
                _imagenOcrHdCache = null;
            }

            _rutaImagenOcrHdCache = string.Empty;
        }
        private void CapturarLayoutOriginal()
        {
            if (_layoutCapturado)
                return;

            _clientSizeOriginal = this.ClientSize;

            CapturarControlOriginal(this);

            _layoutCapturado = true;
        }

        private void CapturarControlOriginal(Control control)
        {
            foreach (Control hijo in control.Controls)
            {
                _boundsOriginales[hijo] = hijo.Bounds;

                if (hijo.Font != null)
                    _fuentesOriginales[hijo] = hijo.Font.Size;

                if (hijo.HasChildren)
                    CapturarControlOriginal(hijo);
            }
        }


        private void AplicarEscalaControl(Control control, float escala)
        {
            float escalaVertical = escala;

            // El ancho no escala igual que el alto.
            // Ejemplo:
            // Escala 1.45 -> ancho escala aprox. 1.1575
            // Escala 1.60 -> ancho escala aprox. 1.21
            float escalaHorizontal = 1.0f + ((escala - 1.0f) * FACTOR_ESCALA_HORIZONTAL_CONTROLES);

            foreach (Control hijo in control.Controls)
            {
                if (_boundsOriginales.TryGetValue(hijo, out Rectangle original))
                {
                    hijo.Bounds = new Rectangle(
                        (int)(original.X * escalaHorizontal),
                        (int)(original.Y * escalaVertical),
                        (int)(original.Width * escalaHorizontal),
                        (int)(original.Height * escalaVertical)
                    );
                }

                if (_fuentesOriginales.TryGetValue(hijo, out float fuenteOriginal))
                {
                    float nuevaFuente = fuenteOriginal * escala;

                    if (nuevaFuente < 7f)
                        nuevaFuente = 7f;

                    hijo.Font = new Font(
                        hijo.Font.FontFamily,
                        nuevaFuente,
                        hijo.Font.Style
                    );
                }

                if (hijo.HasChildren)
                    AplicarEscalaControl(hijo, escala);
            }
        }

        private void CmbReso_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_aplicandoResolucion)
                return;

            if (CmbReso.SelectedIndex < 0)
                return;

            _perfilActual = (PerfilResolucion)  CmbReso.SelectedIndex;

            AplicarPerfilResolucion(_perfilActual);
        }

        private void FormCopidis_Shown_1(object sender, EventArgs e)
        {
            ForzarMaximizado();
            AplicarPerfilResolucion(_perfilActual);
            ForzarMaximizado();
        }

        private void button9_Click_1(object sender, EventArgs e)
        {

        }
    }
}