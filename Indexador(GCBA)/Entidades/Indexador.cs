using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexadorCopidis.Entidades
{
    public class Indexador
    {
        int idIndexador;
        string nombreIndexador;

        public int IdIndexador
        {
            get { return idIndexador; }
            set { idIndexador = value; }
        }

        public string NombreIndexador
        {
            get { return nombreIndexador; }
            set { nombreIndexador = value; }
        }

        public override string ToString()
        {
            return NombreIndexador;
        }
    }
}
