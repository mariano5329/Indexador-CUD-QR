using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexadorCopidis.Entidades
{
    class Persona
    {

        // Propiedades

        public string ID { get; set; }
        public string ApellidoNombre { get; set; }        
        public string DNI { get; set; }
        public string CUIL { get; set; }        

        // Constructor Vacio

        public Persona() { }

        // Constructor que recibe todos los parametros

        public Persona(string pID, string pApellidoNombre, string pDNI, string pCUIL)
        {
            this.ID = pID;
            this.ApellidoNombre = pApellidoNombre;            
            this.DNI = pDNI;
            this.CUIL =  pCUIL;            
        }



    }
}
