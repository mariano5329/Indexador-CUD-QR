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
    public partial class FormLogin : Form
    {
        public FormLogin()
        {
            InitializeComponent();
        }

        private void FormLogin_Load(object sender, EventArgs e)
        {
            Datos.ProduccionAccess oProduccionAccess = new Datos.ProduccionAccess();
           cboxUsuarios.DataSource = oProduccionAccess.CargarListaIndexadores();
            cboxUsuarios.SelectedIndex = -1;
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

                FormActasDeNacimiento oFrmPrincipal = new FormActasDeNacimiento(oIndexador);                

                this.Hide();
                oFrmPrincipal.Show();
            }
            
                        
        }

        
    }
}
