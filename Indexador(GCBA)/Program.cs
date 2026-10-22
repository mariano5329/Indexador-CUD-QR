using System;
using System.Windows.Forms;
using IndexadorCopidis.Datos;

namespace IndexadorCopidis
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// En esta etapa de pruebas QR, crea las carpetas locales del Escritorio
        /// antes de abrir el Login, sin acceder a rutas de producción.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                ConexionAccess.PrepararCarpetasPrueba();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo crear la estructura de pruebas del indexador QR en el Escritorio." +
                    Environment.NewLine + ex.Message,
                    "Indexador CUD QR - PRUEBAS",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new FormLogin());
        }
    }
}
