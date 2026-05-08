using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Windows.Forms;

namespace Indexador_GCBA_.Datos
{
    class CargarBD
    {
    
            public static void NombrarTitulos(DataTable oTabla, string[] titulos)
            {
                int x = 0;
                for (x = 0; x <= titulos.Length - 1; x++)
                {
                    string Titulo = titulos[x];
                    oTabla.Columns.Add(titulos[x]);
                }

            }

    }
}
