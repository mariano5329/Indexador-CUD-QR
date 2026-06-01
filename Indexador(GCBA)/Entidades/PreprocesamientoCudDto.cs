using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indexador_GCBA_.Entidades
{
    public class PreprocesamientoCudDto
    {
        public string Caja { get; set; }
        public string ArchivoEntrada { get; set; }
        public string RutaEntrada { get; set; }
        public string RutaSalida { get; set; }

        public string NumeroCertificado { get; set; }
        public string ApellidoNombre { get; set; }
        public string DNI { get; set; }
        public string CUIL { get; set; }

        public DateTime? FechaNacimiento { get; set; }
        public DateTime? FechaEmision { get; set; }
        public DateTime? FechaVencimiento { get; set; }

        public string FuncionesCorporales { get; set; }
        public string EstructurasCorporales { get; set; }
        public string ActividadParticipacion { get; set; }
        public string FactoresAmbientales { get; set; }

        public string Acompaniante { get; set; }



         public string OrientacionPrestacional { get; set; }

        public double ScoreGlobal { get; set; }
        public string EstadoOCR { get; set; }
        public bool RequiereQA { get; set; }
        public string Observaciones { get; set; }
    }
}
