using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indexador_GCBA_.Entidades
{
    public class Proyecto
    {
        int idproyecto;
        string nombreProyecto;
        string nombreMinisterio;
        string tipoDocumento;

        public int IdProyecto
        {
            get { return idproyecto; }
            set { idproyecto = value; }
        }

        public string NombreProyecto
        {
            get { return nombreProyecto; }
            set { nombreProyecto = value; }
        }

        public string NombreMinisterio
        {
            get { return nombreMinisterio; }
            set { nombreMinisterio = value; }
        }

        public string TipoDocumento
        {
            get { return tipoDocumento; }
            set { tipoDocumento = value; }
        }

        public override string ToString()
        {
            return NombreProyecto + " - " + TipoDocumento;
        }

    }
}
