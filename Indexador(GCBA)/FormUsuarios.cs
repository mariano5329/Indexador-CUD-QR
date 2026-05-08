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

namespace Indexador_GCBA_
{
    public partial class FormUsuarios : Form
    {
        public FormUsuarios()
        {
            InitializeComponent();
            CargarArchivoUsuario();
        }

        private void btnGrabarCerrar_Click(object sender, EventArgs e)
        {
            GrabarCerrar();
        }

        public void CargarArchivoUsuario()
        {
            txtArchivoUsuario.Text = File.ReadAllText(Application.StartupPath + "\\Archivos\\USUARIOS.txt");
            // btnGrabarCerrar.Focus();
            //txtArchivoUsuario.Focus();
        }

        public void GrabarCerrar()
        {
            StreamWriter oEscribirArchivo = new StreamWriter(Application.StartupPath + "\\Archivos\\USUARIOS.txt");
            oEscribirArchivo.Write(txtArchivoUsuario.Text);
            oEscribirArchivo.Close();

            this.Close();
        }

        
    }
}
