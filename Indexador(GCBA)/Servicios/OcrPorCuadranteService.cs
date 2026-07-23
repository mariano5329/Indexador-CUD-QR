using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Tesseract;

namespace IndexadorCopidis.Servicios
{
    public class ResultadoOcrCuadrante
    {
        public bool HayTexto
        {
            get { return !string.IsNullOrWhiteSpace(Texto); }
        }

        public string Texto { get; set; }
        public string MensajeTecnico { get; set; }

        public float ConfianzaPromedio { get; set; }

        public Rectangle RectanguloVista { get; set; }
        public Rectangle RectanguloImagen { get; set; }

        public int AnchoRecorte { get; set; }
        public int AltoRecorte { get; set; }

        public string RutaRecorteDebug { get; set; }
        public string RutaRecortePreparadoDebug { get; set; }
    }

    public class OcrPorCuadranteService
    {
        private readonly string _rutaTessdata;
        private readonly string _idioma;

        public OcrPorCuadranteService()
        {
            _rutaTessdata = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "tessdata");

            _idioma = "spa";
        }

        public string LeerTextoDesdeSeleccion(
            PictureBox pictureBox,
            Rectangle rectanguloSeleccionVista)
        {
            ResultadoOcrCuadrante resultado =
                LeerTextoDesdeSeleccionDetalle(
                    pictureBox,
                    rectanguloSeleccionVista,
                    guardarDebug: false);

            return resultado.Texto;
        }

        public ResultadoOcrCuadrante LeerTextoDesdeSeleccionDetalle(
            PictureBox pictureBox,
            Rectangle rectanguloSeleccionVista,
            bool guardarDebug)
        {
            ResultadoOcrCuadrante resultado =
                CrearResultadoInicial(rectanguloSeleccionVista);

            try
            {
                if (pictureBox == null)
                {
                    resultado.MensajeTecnico = "PictureBox nulo.";
                    return resultado;
                }

                if (pictureBox.Image == null)
                {
                    resultado.MensajeTecnico =
                        "No hay imagen cargada en el PictureBox.";

                    return resultado;
                }

                if (rectanguloSeleccionVista.Width <= 0 ||
                    rectanguloSeleccionVista.Height <= 0)
                {
                    resultado.MensajeTecnico =
                        "Rectángulo de selección vacío.";

                    return resultado;
                }

                Rectangle rectanguloImagen =
                    ConvertirRectanguloVistaAImagen(
                        pictureBox,
                        rectanguloSeleccionVista);

                resultado.RectanguloImagen = rectanguloImagen;

                if (rectanguloImagen == Rectangle.Empty)
                {
                    resultado.MensajeTecnico =
                        "La selección no coincide con el área real de la imagen mostrada.";

                    return resultado;
                }

                using (Bitmap recorte = RecortarImagen(
                    pictureBox.Image,
                    rectanguloImagen))
                {
                    resultado.AnchoRecorte = recorte.Width;
                    resultado.AltoRecorte = recorte.Height;

                    using (Bitmap preparada =
                           PrepararImagenParaOcr(recorte))
                    {
                        ResultadoOcrCuadrante intento =
                            EjecutarOcrConFallback(preparada);

                        resultado.Texto =
                            intento.Texto ?? string.Empty;

                        resultado.ConfianzaPromedio =
                            intento.ConfianzaPromedio;

                        resultado.MensajeTecnico =
                            intento.MensajeTecnico ?? string.Empty;

                        return resultado;
                    }
                }
            }
            catch (Exception ex)
            {
                CapturarErrorOcr(
                    resultado,
                    ex,
                    "LeerTextoDesdeSeleccionDetalle(PictureBox)",
                    rectanguloSeleccionVista);

                return resultado;
            }
        }

        public ResultadoOcrCuadrante LeerTextoDesdeSeleccionDetalle(
            Image imagen,
            Size tamanioPictureBox,
            PictureBoxSizeMode sizeMode,
            Rectangle rectanguloSeleccionVista,
            bool guardarDebug)
        {
            ResultadoOcrCuadrante resultado =
                CrearResultadoInicial(rectanguloSeleccionVista);

            try
            {
                if (imagen == null)
                {
                    resultado.MensajeTecnico = "Imagen nula.";
                    return resultado;
                }

                if (imagen.Width <= 0 || imagen.Height <= 0)
                {
                    resultado.MensajeTecnico = "Imagen inválida.";
                    return resultado;
                }

                if (tamanioPictureBox.Width <= 0 ||
                    tamanioPictureBox.Height <= 0)
                {
                    resultado.MensajeTecnico =
                        "Tamaño del PictureBox inválido.";

                    return resultado;
                }

                if (rectanguloSeleccionVista.Width <= 0 ||
                    rectanguloSeleccionVista.Height <= 0)
                {
                    resultado.MensajeTecnico =
                        "Rectángulo de selección vacío.";

                    return resultado;
                }

                Rectangle rectanguloImagen =
                    ConvertirRectanguloVistaAImagen(
                        imagen,
                        tamanioPictureBox,
                        sizeMode,
                        rectanguloSeleccionVista);

                resultado.RectanguloImagen = rectanguloImagen;

                if (rectanguloImagen == Rectangle.Empty)
                {
                    resultado.MensajeTecnico =
                        "La selección no coincide con el área real de la imagen mostrada.";

                    return resultado;
                }

                using (Bitmap recorte =
                       RecortarImagen(imagen, rectanguloImagen))
                {
                    resultado.AnchoRecorte = recorte.Width;
                    resultado.AltoRecorte = recorte.Height;

                    using (Bitmap preparada =
                           PrepararImagenParaOcr(recorte))
                    {
                        ResultadoOcrCuadrante intento =
                            EjecutarOcrConFallback(preparada);

                        resultado.Texto =
                            intento.Texto ?? string.Empty;

                        resultado.ConfianzaPromedio =
                            intento.ConfianzaPromedio;

                        resultado.MensajeTecnico =
                            intento.MensajeTecnico ?? string.Empty;

                        return resultado;
                    }
                }
            }
            catch (Exception ex)
            {
                CapturarErrorOcr(
                    resultado,
                    ex,
                    "LeerTextoDesdeSeleccionDetalle(Image)",
                    rectanguloSeleccionVista);

                return resultado;
            }
        }

        private ResultadoOcrCuadrante CrearResultadoInicial(
            Rectangle rectanguloSeleccionVista)
        {
            return new ResultadoOcrCuadrante
            {
                Texto = string.Empty,
                MensajeTecnico = string.Empty,
                ConfianzaPromedio = 0,
                RectanguloVista = rectanguloSeleccionVista,
                RectanguloImagen = Rectangle.Empty,
                AnchoRecorte = 0,
                AltoRecorte = 0,
                RutaRecorteDebug = string.Empty,
                RutaRecortePreparadoDebug = string.Empty
            };
        }

        private void CapturarErrorOcr(
            ResultadoOcrCuadrante resultado,
            Exception ex,
            string metodo,
            Rectangle rectanguloVista)
        {
            string detalleExcepcion =
                ObtenerDetalleCompletoExcepcion(ex);

            resultado.Texto = string.Empty;
            resultado.ConfianzaPromedio = 0;
            resultado.MensajeTecnico =
                "Se produjo un error al ejecutar el OCR. " +
                "Se generó un archivo de diagnóstico en el Escritorio.";

            StringBuilder detalleLog = new StringBuilder();

            detalleLog.AppendLine("MÉTODO: " + metodo);
            detalleLog.AppendLine(
                "Directorio aplicación: " +
                AppDomain.CurrentDomain.BaseDirectory);

            detalleLog.AppendLine(
                "Tessdata: " + _rutaTessdata);

            detalleLog.AppendLine(
                "Existe tessdata: " +
                Directory.Exists(_rutaTessdata));

            string rutaIdioma = Path.Combine(
                _rutaTessdata,
                _idioma + ".traineddata");

            detalleLog.AppendLine(
                "Archivo de idioma: " + rutaIdioma);

            detalleLog.AppendLine(
                "Existe archivo de idioma: " +
                File.Exists(rutaIdioma));

            detalleLog.AppendLine(
                "Proceso 64 bits: " +
                Environment.Is64BitProcess);

            detalleLog.AppendLine(
                "Sistema operativo 64 bits: " +
                Environment.Is64BitOperatingSystem);

            detalleLog.AppendLine(
                "Rectángulo vista: " +
                rectanguloVista);

            detalleLog.AppendLine(
                "Rectángulo imagen: " +
                resultado.RectanguloImagen);

            detalleLog.AppendLine(
                "Tamaño recorte: " +
                resultado.AnchoRecorte +
                "x" +
                resultado.AltoRecorte);

            detalleLog.AppendLine();
            detalleLog.AppendLine("DETALLE DE LA EXCEPCIÓN:");
            detalleLog.AppendLine(detalleExcepcion);

            RegistrarErrorOcr(detalleLog.ToString());
        }

        private string ObtenerDetalleCompletoExcepcion(Exception ex)
        {
            if (ex == null)
                return "Excepción desconocida.";

            StringBuilder detalle = new StringBuilder();

            Exception actual = ex;
            int nivel = 0;

            while (actual != null)
            {
                detalle.AppendLine(
                    "--------------------------------------");

                detalle.AppendLine(
                    "Nivel: " + nivel);

                detalle.AppendLine(
                    "Tipo: " + actual.GetType().FullName);

                detalle.AppendLine(
                    "Mensaje: " + actual.Message);

                detalle.AppendLine("StackTrace:");

                detalle.AppendLine(
                    actual.StackTrace ?? "Sin StackTrace.");

                actual = actual.InnerException;
                nivel++;
            }

            return detalle.ToString();
        }

        private void RegistrarErrorOcr(string detalle)
        {
            try
            {
                string escritorio =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.DesktopDirectory);

                string archivo = Path.Combine(
                    escritorio,
                    "OCR_ERROR_" +
                    DateTime.Now.ToString("yyyyMMdd") +
                    ".log");

                StringBuilder bloque = new StringBuilder();

                bloque.AppendLine();
                bloque.AppendLine(
                    "==================================================");

                bloque.AppendLine(
                    "Fecha: " +
                    DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                bloque.AppendLine(
                    "Equipo: " +
                    Environment.MachineName);

                bloque.AppendLine(
                    "Usuario: " +
                    Environment.UserName);

                bloque.AppendLine(
                    "Versión aplicación: " +
                    Application.ProductVersion);

                bloque.AppendLine(detalle);

                File.AppendAllText(
                    archivo,
                    bloque.ToString());
            }
            catch
            {
                // Si falla la escritura del log,
                // no se genera una segunda excepción.
            }
        }

        private ResultadoOcrCuadrante EjecutarOcrConFallback(
            Bitmap imagen)
        {
            ResultadoOcrCuadrante mejor =
                new ResultadoOcrCuadrante
                {
                    Texto = string.Empty,
                    ConfianzaPromedio = 0,
                    MensajeTecnico = string.Empty
                };

            PageSegMode[] modos =
            {
                PageSegMode.SingleBlock,
                PageSegMode.SingleLine,
                PageSegMode.Auto
            };

            foreach (PageSegMode modo in modos)
            {
                ResultadoOcrCuadrante actual =
                    EjecutarOcr(imagen, modo);

                if (!string.IsNullOrWhiteSpace(actual.Texto))
                    return actual;

                if (actual.ConfianzaPromedio >
                    mejor.ConfianzaPromedio)
                {
                    mejor = actual;
                }
            }

            return mejor;
        }

        private ResultadoOcrCuadrante EjecutarOcr(
            Bitmap imagen,
            PageSegMode modo)
        {
            ResultadoOcrCuadrante resultado =
                new ResultadoOcrCuadrante
                {
                    Texto = string.Empty,
                    ConfianzaPromedio = 0,
                    MensajeTecnico = string.Empty
                };

            if (imagen == null)
                throw new ArgumentNullException("imagen");

            if (!Directory.Exists(_rutaTessdata))
            {
                throw new DirectoryNotFoundException(
                    "No se encontró la carpeta tessdata: " +
                    _rutaTessdata);
            }

            string rutaIdioma = Path.Combine(
                _rutaTessdata,
                _idioma + ".traineddata");

            if (!File.Exists(rutaIdioma))
            {
                throw new FileNotFoundException(
                    "No se encontró el archivo de idioma de Tesseract.",
                    rutaIdioma);
            }

            using (TesseractEngine engine =
                   new TesseractEngine(
                       _rutaTessdata,
                       _idioma,
                       EngineMode.Default))
            {
                engine.SetVariable(
                    "preserve_interword_spaces",
                    "1");

                engine.SetVariable(
                    "user_defined_dpi",
                    "300");

                using (Pix pix = PixConverter.ToPix(imagen))
                using (Page page = engine.Process(pix, modo))
                {
                    string texto = page.GetText();

                    resultado.Texto =
                        texto == null
                            ? string.Empty
                            : texto.Trim();

                    resultado.ConfianzaPromedio =
                        page.GetMeanConfidence();

                    return resultado;
                }
            }
        }

        private Rectangle ConvertirRectanguloVistaAImagen(
            Image imagen,
            Size tamanioPictureBox,
            PictureBoxSizeMode sizeMode,
            Rectangle rectanguloVista)
        {
            Rectangle areaPictureBox = new Rectangle(
                0,
                0,
                tamanioPictureBox.Width,
                tamanioPictureBox.Height);

            Rectangle rectImagenMostrada =
                ObtenerRectanguloImagenMostrada(
                    imagen,
                    areaPictureBox,
                    sizeMode);

            if (rectImagenMostrada == Rectangle.Empty)
                return Rectangle.Empty;

            Rectangle interseccion =
                Rectangle.Intersect(
                    rectanguloVista,
                    rectImagenMostrada);

            if (interseccion.Width <= 0 ||
                interseccion.Height <= 0)
            {
                return Rectangle.Empty;
            }

            float escalaX =
                (float)imagen.Width /
                rectImagenMostrada.Width;

            float escalaY =
                (float)imagen.Height /
                rectImagenMostrada.Height;

            int x = (int)Math.Round(
                (interseccion.X - rectImagenMostrada.X) *
                escalaX);

            int y = (int)Math.Round(
                (interseccion.Y - rectImagenMostrada.Y) *
                escalaY);

            int width = (int)Math.Round(
                interseccion.Width * escalaX);

            int height = (int)Math.Round(
                interseccion.Height * escalaY);

            if (x < 0)
                x = 0;

            if (y < 0)
                y = 0;

            if (x + width > imagen.Width)
                width = imagen.Width - x;

            if (y + height > imagen.Height)
                height = imagen.Height - y;

            if (width <= 0 || height <= 0)
                return Rectangle.Empty;

            return new Rectangle(
                x,
                y,
                width,
                height);
        }

        private Rectangle ObtenerRectanguloImagenMostrada(
            Image imagen,
            Rectangle area,
            PictureBoxSizeMode sizeMode)
        {
            if (imagen == null)
                return Rectangle.Empty;

            int imageWidth = imagen.Width;
            int imageHeight = imagen.Height;

            if (imageWidth <= 0 ||
                imageHeight <= 0 ||
                area.Width <= 0 ||
                area.Height <= 0)
            {
                return Rectangle.Empty;
            }

            if (sizeMode != PictureBoxSizeMode.Zoom)
                return area;

            float ratioImagen =
                (float)imageWidth / imageHeight;

            float ratioControl =
                (float)area.Width / area.Height;

            int anchoMostrado;
            int altoMostrado;
            int x;
            int y;

            if (ratioControl > ratioImagen)
            {
                altoMostrado = area.Height;

                anchoMostrado =
                    (int)(altoMostrado * ratioImagen);

                x =
                    area.X +
                    (area.Width - anchoMostrado) / 2;

                y = area.Y;
            }
            else
            {
                anchoMostrado = area.Width;

                altoMostrado =
                    (int)(anchoMostrado / ratioImagen);

                x = area.X;

                y =
                    area.Y +
                    (area.Height - altoMostrado) / 2;
            }

            return new Rectangle(
                x,
                y,
                anchoMostrado,
                altoMostrado);
        }

        private Rectangle ConvertirRectanguloVistaAImagen(
            PictureBox pictureBox,
            Rectangle rectanguloVista)
        {
            Rectangle rectImagenMostrada =
                ObtenerRectanguloImagenMostradaEnPictureBox(
                    pictureBox);

            if (rectImagenMostrada == Rectangle.Empty)
                return Rectangle.Empty;

            Rectangle interseccion =
                Rectangle.Intersect(
                    rectanguloVista,
                    rectImagenMostrada);

            if (interseccion.Width <= 0 ||
                interseccion.Height <= 0)
            {
                return Rectangle.Empty;
            }

            float escalaX =
                (float)pictureBox.Image.Width /
                rectImagenMostrada.Width;

            float escalaY =
                (float)pictureBox.Image.Height /
                rectImagenMostrada.Height;

            int x = (int)Math.Round(
                (interseccion.X - rectImagenMostrada.X) *
                escalaX);

            int y = (int)Math.Round(
                (interseccion.Y - rectImagenMostrada.Y) *
                escalaY);

            int width = (int)Math.Round(
                interseccion.Width * escalaX);

            int height = (int)Math.Round(
                interseccion.Height * escalaY);

            if (x < 0)
                x = 0;

            if (y < 0)
                y = 0;

            if (x + width > pictureBox.Image.Width)
                width = pictureBox.Image.Width - x;

            if (y + height > pictureBox.Image.Height)
                height = pictureBox.Image.Height - y;

            if (width <= 0 || height <= 0)
                return Rectangle.Empty;

            return new Rectangle(
                x,
                y,
                width,
                height);
        }

        private Rectangle ObtenerRectanguloImagenMostradaEnPictureBox(
            PictureBox pictureBox)
        {
            if (pictureBox == null ||
                pictureBox.Image == null)
            {
                return Rectangle.Empty;
            }

            Rectangle area =
                pictureBox.ClientRectangle;

            int imageWidth =
                pictureBox.Image.Width;

            int imageHeight =
                pictureBox.Image.Height;

            if (imageWidth <= 0 ||
                imageHeight <= 0 ||
                area.Width <= 0 ||
                area.Height <= 0)
            {
                return Rectangle.Empty;
            }

            if (pictureBox.SizeMode !=
                PictureBoxSizeMode.Zoom)
            {
                return area;
            }

            float ratioImagen =
                (float)imageWidth / imageHeight;

            float ratioControl =
                (float)area.Width / area.Height;

            int anchoMostrado;
            int altoMostrado;
            int x;
            int y;

            if (ratioControl > ratioImagen)
            {
                altoMostrado = area.Height;

                anchoMostrado =
                    (int)(altoMostrado * ratioImagen);

                x =
                    area.X +
                    (area.Width - anchoMostrado) / 2;

                y = area.Y;
            }
            else
            {
                anchoMostrado = area.Width;

                altoMostrado =
                    (int)(anchoMostrado / ratioImagen);

                x = area.X;

                y =
                    area.Y +
                    (area.Height - altoMostrado) / 2;
            }

            return new Rectangle(
                x,
                y,
                anchoMostrado,
                altoMostrado);
        }

        private Bitmap RecortarImagen(
            Image imagen,
            Rectangle rectanguloImagen)
        {
            Bitmap recorte = new Bitmap(
                rectanguloImagen.Width,
                rectanguloImagen.Height,
                PixelFormat.Format24bppRgb);

            using (Graphics g =
                   Graphics.FromImage(recorte))
            {
                g.Clear(Color.White);

                g.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

                g.SmoothingMode =
                    SmoothingMode.HighQuality;

                g.PixelOffsetMode =
                    PixelOffsetMode.HighQuality;

                g.DrawImage(
                    imagen,
                    new Rectangle(
                        0,
                        0,
                        rectanguloImagen.Width,
                        rectanguloImagen.Height),
                    rectanguloImagen,
                    GraphicsUnit.Pixel);
            }

            return recorte;
        }

        private Bitmap PrepararImagenParaOcr(
            Bitmap origen)
        {
            if (origen == null)
                throw new ArgumentNullException("origen");

            int escala = 2;

            if (origen.Width < 700 ||
                origen.Height < 140)
            {
                escala = 3;
            }

            int nuevoAncho =
                origen.Width * escala;

            int nuevoAlto =
                origen.Height * escala;

            Bitmap ampliada = new Bitmap(
                nuevoAncho,
                nuevoAlto,
                PixelFormat.Format24bppRgb);

            try
            {
                using (Graphics g =
                       Graphics.FromImage(ampliada))
                {
                    g.Clear(Color.White);

                    g.InterpolationMode =
                        InterpolationMode.HighQualityBicubic;

                    g.SmoothingMode =
                        SmoothingMode.HighQuality;

                    g.PixelOffsetMode =
                        PixelOffsetMode.HighQuality;

                    g.DrawImage(
                        origen,
                        new Rectangle(
                            0,
                            0,
                            nuevoAncho,
                            nuevoAlto),
                        new Rectangle(
                            0,
                            0,
                            origen.Width,
                            origen.Height),
                        GraphicsUnit.Pixel);
                }

                using (Bitmap gris =
                       PasarAEscalaDeGrises(ampliada))
                {
                    return BinarizarSimple(gris);
                }
            }
            finally
            {
                ampliada.Dispose();
            }
        }

        private Bitmap PasarAEscalaDeGrises(
            Bitmap origen)
        {
            Bitmap salida = new Bitmap(
                origen.Width,
                origen.Height,
                PixelFormat.Format24bppRgb);

            using (Graphics g =
                   Graphics.FromImage(salida))
            {
                ColorMatrix matriz =
                    new ColorMatrix(
                        new float[][]
                        {
                            new float[]
                            {
                                0.299f, 0.299f, 0.299f, 0, 0
                            },
                            new float[]
                            {
                                0.587f, 0.587f, 0.587f, 0, 0
                            },
                            new float[]
                            {
                                0.114f, 0.114f, 0.114f, 0, 0
                            },
                            new float[]
                            {
                                0, 0, 0, 1, 0
                            },
                            new float[]
                            {
                                0, 0, 0, 0, 1
                            }
                        });

                using (ImageAttributes atributos =
                       new ImageAttributes())
                {
                    atributos.SetColorMatrix(matriz);

                    g.DrawImage(
                        origen,
                        new Rectangle(
                            0,
                            0,
                            origen.Width,
                            origen.Height),
                        0,
                        0,
                        origen.Width,
                        origen.Height,
                        GraphicsUnit.Pixel,
                        atributos);
                }
            }

            return salida;
        }

        private Bitmap BinarizarSimple(
            Bitmap origen)
        {
            Bitmap salida = new Bitmap(
                origen.Width,
                origen.Height,
                PixelFormat.Format24bppRgb);

            int umbral = 180;

            for (int y = 0;
                 y < origen.Height;
                 y++)
            {
                for (int x = 0;
                     x < origen.Width;
                     x++)
                {
                    Color c =
                        origen.GetPixel(x, y);

                    Color nuevo =
                        c.R < umbral
                            ? Color.Black
                            : Color.White;

                    salida.SetPixel(
                        x,
                        y,
                        nuevo);
                }
            }

            return salida;
        }
    }
}