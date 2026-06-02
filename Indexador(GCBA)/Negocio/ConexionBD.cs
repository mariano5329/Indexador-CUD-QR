using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexadorCopidis.Negocio
{
    public class ConexionBD
    {
        public string RecuperarNombreBD()
        {

            OleDbConnection oCon = Datos.ConexionAccess.ObtenerConexion1();
            string RutaNombreBD = oCon.DataSource;
            string NombreBD = Path.GetFileName(RutaNombreBD);
            return NombreBD;
        }
    }
}
