using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Indexador_GCBA_
{
    public partial class FrmPrincipal : Form
    {

        Entidades.Indexador oIndexador = new Entidades.Indexador();

        public FrmPrincipal(Entidades.Indexador pIndexador)
        {
            InitializeComponent();
            oIndexador = pIndexador;
            Negocio.ConexionBD oConexionBD = new Negocio.ConexionBD();
            toolStripStatusLabel1.Text = "Versión: " + System.IO.File.GetLastWriteTime(Application.ExecutablePath);
            toolStripStatusLabel3.Text = "Base de Datos: " + oConexionBD.RecuperarNombreBD();
        }



        private void FrmPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void haciendaToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void salirToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

      

        private void configurarOperadoresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormUsuarios oFormUsuarios = new FormUsuarios();
            oFormUsuarios.Show();
        }


        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void FrmPrincipal_Load(object sender, EventArgs e)
        {
          
        }

        private void fICHASTOPOGRAFICASToolStripMenuItem_Click(object sender, EventArgs e)
        {

            FormEducacionCAD oFormActasDeNacimiento = new FormEducacionCAD (oIndexador);

            oFormActasDeNacimiento.txtUsuario.Text = toolStripStatusLabel2.Text;

            oFormActasDeNacimiento.MdiParent = this;
            oFormActasDeNacimiento.Show();
        }

        private void aCTASDENACIMIENTOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormActasDeNacimiento oFormActasDeNacimiento = new FormActasDeNacimiento(oIndexador);

            oFormActasDeNacimiento.txtUsuario.Text = toolStripStatusLabel2.Text;
            menuStrip1.Visible = false;
            statusStrip1.Visible = false;
            oFormActasDeNacimiento.MdiParent = this;
            oFormActasDeNacimiento.Show();
        }
    }
}

