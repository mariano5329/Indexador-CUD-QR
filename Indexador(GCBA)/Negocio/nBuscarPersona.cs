using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Indexador_RC._05_Entidades;
using Indexador_RC._04_Datos;

namespace Indexador_RC._03_Negocio
{
    public class nBuscarPersona
    {

        public void BuscarPersona(int pDNI, string pSexo, TextBox pNombre1, TextBox pNombre2, TextBox pNombre3, TextBox pApellido1, TextBox pApellido2, TextBox pApellido3, TextBox pDiaNacimiento, TextBox pMesNacimiento, TextBox pAnoNacimiento)
        {
            ePersona ePersona = new ePersona();
            ConsultaRENAPER consultaRENAPER = new ConsultaRENAPER();

            ePersona = consultaRENAPER.BuscarPersonaRENAPER(pDNI, pSexo);

            if (!string.IsNullOrEmpty(ePersona.Nombre))
            {
                SeperarNombres(ePersona, pNombre1, pNombre2, pNombre3);
                SeperarApellidos(ePersona, pApellido1, pApellido2, pApellido3);
                FechaNacimiento(ePersona, pDiaNacimiento, pMesNacimiento, pAnoNacimiento);
            }           
        }

        public void BuscarPersona(int pDNI, string pSexo, TextBox pNombre1, TextBox pNombre2, TextBox pNombre3, TextBox pApellido1, TextBox pApellido2, TextBox pApellido3)
        {
            ePersona ePersona = new ePersona();
            ConsultaRENAPER consultaRENAPER = new ConsultaRENAPER();

            ePersona = consultaRENAPER.BuscarPersonaRENAPER(pDNI, pSexo);

            if (!string.IsNullOrEmpty(ePersona.Nombre))
            {
                SeperarNombres(ePersona, pNombre1, pNombre2, pNombre3);
                SeperarApellidos(ePersona, pApellido1, pApellido2, pApellido3);                
            }
        }

        public void BuscarPersonaPadres(int pDNI, string pSexo, TextBox pNombres, TextBox pApellidos)
        {
            ePersona ePersona = new ePersona();
            ConsultaRENAPER consultaRENAPER = new ConsultaRENAPER();

            ePersona = consultaRENAPER.BuscarPersonaRENAPER(pDNI, pSexo);

            pNombres.Text = ePersona.Nombre;
            pApellidos.Text = ePersona.Apellido;             
        }

        public void SeperarNombres(ePersona pPersona, TextBox pNombre1, TextBox pNombre2, TextBox pNombre3)
        {
            string[] Nombres = pPersona.Nombre.Split(' ');

            if (Nombres.Length > 0)
            {
                pNombre1.Text = Nombres[0];
            }

            if (Nombres.Length > 1)
            {
                pNombre2.Text = Nombres[1];
            }

            if (Nombres.Length > 2)
            {
                pNombre3.Text = Nombres[2];
            }
        }

        public void SeperarApellidos(ePersona pPersona, TextBox pApellido1, TextBox pApellido2, TextBox pApellido3)
        {
            string[] Apellidos = pPersona.Apellido.Split(' ');

            if (Apellidos.Length > 0)
            {
                pApellido1.Text = Apellidos[0];
            }

            if (Apellidos.Length > 1)
            {
                pApellido2.Text = Apellidos[1];
            }

            if (Apellidos.Length > 2)
            {
                pApellido3.Text = Apellidos[2];
            }
        }

        public void FechaNacimiento(ePersona pPersona, TextBox pDiaNacimiento, TextBox pMesNacimiento, TextBox pAnoNacimiento)
        {
            string[] Fecha = pPersona.FechaNacimiento.Split('-');

            string FechaNacimiento = Fecha[2] + "/" + Fecha[1] + "/" + Fecha[0];

            pDiaNacimiento.Text = Fecha[2];
            pMesNacimiento.Text = Fecha[1];
            pAnoNacimiento.Text = Fecha[0];
        }

    }
}
