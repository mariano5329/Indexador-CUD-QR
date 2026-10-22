using System;
using System.Data.OleDb;
using System.IO;

namespace IndexadorCopidis.Datos
{
    class ConexionAccess
    {
        // ==========================================================
        // MODO PRUEBAS - CUD QR
        // Las rutas J:\COPIDIS\... quedan comentadas debajo.
        // NO utiliza las bases Access originales para grabar.
        // ==========================================================
        internal static readonly string RutaBasePruebas = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "Indexador-CUD-QR-PRUEBAS");

        internal static readonly string RutaBDCertificadosPruebas =
            Path.Combine(RutaBasePruebas, "00_BD", "BDCertificados.accdb");

        internal static readonly string RutaBDPreprocesamientoPruebas =
            Path.Combine(RutaBasePruebas, "00_BD", "BDPreprocesamiento.accdb");

        internal static void PrepararCarpetasPrueba()
        {
            Directory.CreateDirectory(RutaBasePruebas);
            Directory.CreateDirectory(Path.Combine(RutaBasePruebas, "00_BD"));
            // El indexador espera GRUPO/LOTE/archivos.pdf.
            // Colocar las carpetas de lote dentro de "PRUEBA".
            Directory.CreateDirectory(Path.Combine(
                RutaBasePruebas, "02_PENDIENTES_INDEXAR", "PRUEBA"));
            Directory.CreateDirectory(Path.Combine(RutaBasePruebas, "03_CERTIFICADOS_VERIFICADOS"));
            Directory.CreateDirectory(Path.Combine(RutaBasePruebas, "04_ILEGIBLES"));
            Directory.CreateDirectory(Path.Combine(RutaBasePruebas, "SepararQR"));

            // Guía local: no se sobrescribe si el usuario la modificó.
            string guia = Path.Combine(RutaBasePruebas, "LEEME_PRUEBAS.txt");

            if (!File.Exists(guia))
            {
                File.WriteAllText(
                    guia,
                    "INDEXADOR CUD QR - MODO PRUEBAS" + Environment.NewLine +
                    Environment.NewLine +
                    "1) Poner carpetas de lote con PDF en:" + Environment.NewLine +
                    Path.Combine(RutaBasePruebas, "02_PENDIENTES_INDEXAR", "PRUEBA") + Environment.NewLine +
                    "   Ejemplo: PRUEBA\\116D1\\2957258.pdf" + Environment.NewLine +
                    Environment.NewLine +
                    "2) Colocar una base BDCertificados.accdb de prueba (con tablas e indexadores) en:" +
                    Environment.NewLine + Path.Combine(RutaBasePruebas, "00_BD") + Environment.NewLine +
                    "   No se utiliza automaticamente la base original de produccion." + Environment.NewLine +
                    "   La base BDPreprocesamiento.accdb es opcional en OCR manual." + Environment.NewLine +
                    Environment.NewLine +
                    "3) Los PDF indexados se guardan en 03_CERTIFICADOS_VERIFICADOS." + Environment.NewLine +
                    "   Los descartados se guardan en 04_ILEGIBLES o SepararQR." + Environment.NewLine +
                    "   Al procesar un PDF, el original de la carpeta de prueba se elimina." +
                    Environment.NewLine);
            }
        }

        internal static bool EsRutaDeEntradaPrueba(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return false;

            try
            {
                string carpetaEntrada = Path.GetFullPath(
                    Path.Combine(RutaBasePruebas, "02_PENDIENTES_INDEXAR"))
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                string rutaNormalizada = Path.GetFullPath(ruta);

                // La ruta debe estar DENTRO del directorio de entradas,
                // no en la misma carpeta ni en una ruta de producción.
                return rutaNormalizada.StartsWith(
                    carpetaEntrada,
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static OleDbConnection ObtenerConexion1()
        {
            // ==========================================================
            // BASE INDEXADOR CUD
            // ==========================================================

            // ORIGINAL - PRODUCCIÓN (restaurar al terminar las pruebas):
            // string rutaBase = @"J:\COPIDIS\00_BD\BDCertificados.accdb";

            // ACTIVA - PRUEBAS EN EL ESCRITORIO:
            string rutaBase = RutaBDCertificadosPruebas;

            if (!File.Exists(rutaBase))
                throw new FileNotFoundException(
                    "Falta la base de datos de PRUEBAS. Copie una base Access de prueba con las tablas del indexador a: " + rutaBase +
                    ". No se utilizará automáticamente la base de producción.",
                    rutaBase);

            string cadenaConexion =
                @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                @"Data Source=" + rutaBase + ";" +
                @"Persist Security Info=False;" +
                @"Mode=Share Deny None;" +
                @"OLE DB Services=-2;";

            return new OleDbConnection(cadenaConexion);
        }

        public static OleDbConnection ObtenerConexionPreprocesamiento()
        {
            // ==========================================================
            // BASE PREPROCESAMIENTO (opcional para OCR manual)
            // ==========================================================

            // ORIGINAL - PRODUCCIÓN (restaurar al terminar las pruebas):
            // string rutaBase = @"J:\COPIDIS\00_BD\BDPreprocesamiento.accdb";

            // ACTIVA - PRUEBAS EN EL ESCRITORIO:
            string rutaBase = RutaBDPreprocesamientoPruebas;

            if (!File.Exists(rutaBase))
                throw new FileNotFoundException(
                    "No hay base de preprocesamiento local de pruebas. La precarga OCR se omitirá.",
                    rutaBase);

            string cadenaConexion =
                @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                @"Data Source=" + rutaBase + ";" +
                @"Persist Security Info=False;";

            return new OleDbConnection(cadenaConexion);
        }
    }
}
