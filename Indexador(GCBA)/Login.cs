using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IndexadorCopidis
{
    public partial class FormLogin : Form
    {
        public FormLogin()
        {
            InitializeComponent();
        }

        private void FormLogin_Load(object sender, EventArgs e)
        {
            try
            {
                Datos.ProduccionAccess oProduccionAccess = new Datos.ProduccionAccess();
                cboxUsuarios.DataSource = oProduccionAccess.CargarListaIndexadores();
                cboxUsuarios.SelectedIndex = -1;
            }
            catch (FileNotFoundException ex)
            {
                cboxUsuarios.Enabled = false;
                btnIngresar.Enabled = false;

                MessageBox.Show(
                    "El indexador CUD QR está en MODO PRUEBAS." + Environment.NewLine +
                    "Las carpetas locales del Escritorio ya están preparadas." +
                    Environment.NewLine + Environment.NewLine +
                    "Para habilitar el acceso, colocá una copia de la base Access de pruebas " +
                    "con la estructura y usuarios del indexador en:" + Environment.NewLine +
                    Datos.ConexionAccess.RutaBDCertificadosPruebas +
                    Environment.NewLine + Environment.NewLine +
                    "No se utilizará ni modificará la base original de producción." +
                    Environment.NewLine + Environment.NewLine + ex.Message,
                    "CUD QR - Base local de pruebas requerida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void btnIngresar_Click(object sender, EventArgs e)
        {
            if (cboxUsuarios.SelectedIndex == -1)
            {
                MessageBox.Show("Debe seleccionar un nombre", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                Entidades.Indexador oIndexador = new Entidades.Indexador();
                oIndexador = cboxUsuarios.SelectedItem as Entidades.Indexador;

                FormCopidis oFrmPrincipal = new FormCopidis(oIndexador);                

                this.Hide();
                oFrmPrincipal.Show();
            }
            
                        
        }

        
    }
}
