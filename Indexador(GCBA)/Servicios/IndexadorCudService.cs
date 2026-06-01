using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Indexador_GCBA_.Servicios
{
    public enum SeveridadValidacion
    {
        Ok = 0,
        Advertencia = 1,
        Error = 2
    }

    public class ResultadoValidacion
    {
        public bool EsValido
        {
            get { return Severidad != SeveridadValidacion.Error; }
        }

        public SeveridadValidacion Severidad { get; set; }
        public string Mensaje { get; set; }

        public static ResultadoValidacion Ok(string mensaje = "")
        {
            return new ResultadoValidacion
            {
                Severidad = SeveridadValidacion.Ok,
                Mensaje = mensaje
            };
        }

        public static ResultadoValidacion Advertencia(string mensaje)
        {
            return new ResultadoValidacion
            {
                Severidad = SeveridadValidacion.Advertencia,
                Mensaje = mensaje
            };
        }

        public static ResultadoValidacion Error(string mensaje)
        {
            return new ResultadoValidacion
            {
                Severidad = SeveridadValidacion.Error,
                Mensaje = mensaje
            };
        }
    }

    public class DatosNumeroCertificadoCud
    {
        public bool EsValido { get; set; }
        public string Mensaje { get; set; }

        public string Sexo { get; set; }

        public string NumeroNormalizado { get; set; }
        public string Dni { get; set; }
        public string FechaEmision { get; set; }
        public string FechaVencimiento { get; set; }
    }

    public enum TipoFechaCud
    {
        Nacimiento,
        Emision,
        Vencimiento
    }

    public class CertificadoCudDto
    {
        public string NumeroCertificado { get; set; }
        public string ApellidoNombre { get; set; }
        public string Sexo { get; set; }
        public string DNI { get; set; }
        public string CUIL { get; set; }
        public string FechaNacimiento { get; set; }
        public string Diagnostico { get; set; }
        public string FuncionesCorporales { get; set; }
        public string EstructurasCorporales { get; set; }
        public string ActividadParticipacion { get; set; }
        public string FactoresAmbientales { get; set; }
        public string OrientacionPrestacional { get; set; }
        public string FechaEmision { get; set; }
        public string FechaVencimiento { get; set; }
        public string Acompaniante { get; set; }
    }

    public static class IndexadorCudService
    {
        private static readonly Regex RegexNumeroCertificado = new Regex(
            @"^ARG-\d{2}-\d{11}-\d{8}-\d{8}-[A-Z]{3}-\d{3}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexCodigoCif = new Regex(
            @"^[bsde]\d{2,4}(\.[+-]?\d{1,4})+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ==========================================================
        // CARACTERES PERMITIDOS
        // ==========================================================

        public static bool CaracterPermitidoNumerico(char c)
        {
            return char.IsControl(c) || char.IsDigit(c);
        }

        public static bool CaracterPermitidoFecha(char c)
        {
            return char.IsControl(c) || char.IsDigit(c) || c == '/';
        }

        public static bool CaracterPermitidoNumeroCertificado(char c)
        {
            return char.IsControl(c) ||
                   char.IsLetterOrDigit(c) ||
                   c == '-' ||
                   c == '_';
        }

        public static bool CaracterPermitidoCif(char c)
        {
            if (char.IsControl(c))
                return true;

            c = char.ToLowerInvariant(c);

            return char.IsDigit(c) ||
          c == 'b' ||
          c == 's' ||
          c == 'd' ||
          c == 'e' ||
          c == '+' ||
          c == '-' ||
          c == '.' ||
          c == ' ' ||
          c == ',';
        }

        // ==========================================================
        // NORMALIZADORES
        // ==========================================================

        public static string NormalizarNumeroCertificado(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim().ToUpperInvariant();

            t = t.Replace('—', '-')
                 .Replace('–', '-')
                 .Replace('−', '-')
                 .Replace('_', '-');

            t = Regex.Replace(t, @"\s+", "");
            t = Regex.Replace(t, @"-+", "-");

            return t;
        }
      
        public static string NormalizarFechaVencimiento(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim();

            t = t.Replace("-", "/")
                 .Replace(".", "/")
                 .Replace("\\", "/");

            t = Regex.Replace(t, @"\s+", "");

            DateTime dt;

            // Caso: 24/9/17
            Match m = Regex.Match(t, @"^(\d{1,2})/(\d{1,2})/(\d{1,2})$");

            if (m.Success)
            {
                int dia = Convert.ToInt32(m.Groups[1].Value);
                int mes = Convert.ToInt32(m.Groups[2].Value);
                int anioCorto = Convert.ToInt32(m.Groups[3].Value);

                int anio = 2000 + anioCorto;

                // Regla específica para vencimientos:
                // Si queda una fecha demasiado vieja, asumimos que el OCR o la carga abreviada
                // corresponde a la década siguiente.
                // Ejemplo: 24/9/17 -> 24/09/2027
                if (anio < DateTime.Today.Year)
                    anio += 10;

                try
                {
                    dt = new DateTime(anio, mes, dia);
                    return dt.ToString("dd/MM/yyyy");
                }
                catch
                {
                    return t;
                }
            }

            // Caso: 24917 o 240917 no lo forzaría todavía.
            // Para fechas compactas seguimos usando la normalización general.
            return NormalizarFecha(t);
        }
        public static string NormalizarFechaCud(string texto, TipoFechaCud tipoFecha)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim();

            t = t.Replace("-", "/")
                 .Replace(".", "/")
                 .Replace("\\", "/");

            t = Regex.Replace(t, @"\s+", "");

            DateTime dt;

            // Caso: 24/9/17
            Match m = Regex.Match(t, @"^(\d{1,2})/(\d{1,2})/(\d{1,2})$");

            if (m.Success)
            {
                int dia = Convert.ToInt32(m.Groups[1].Value);
                int mes = Convert.ToInt32(m.Groups[2].Value);
                int anioCorto = Convert.ToInt32(m.Groups[3].Value);

                int anio = 2000 + anioCorto;

                if (tipoFecha == TipoFechaCud.Vencimiento)
                {
                    // Para vencimiento, si el año queda pasado,
                    // asumimos la década siguiente.
                    // Ejemplo: 24/9/17 -> 24/09/2027
                    if (anio < DateTime.Today.Year)
                        anio += 10;
                }

                try
                {
                    dt = new DateTime(anio, mes, dia);
                    return dt.ToString("dd/MM/yyyy");
                }
                catch
                {
                    return t;
                }
            }

            // Caso: 24/9/2017
            if (DateTime.TryParseExact(t, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.ToString("dd/MM/yyyy");

            // Caso: 24092017 o 20270924
            return NormalizarFecha(t);
        }


        public static string NormalizarDni(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            return SoloDigitos(texto);
        }

        public static string NormalizarCuil(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim();

            if (t == "-")
                return string.Empty;

            return SoloDigitos(t);
        }

        public static string NormalizarFecha(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim();

            t = t.Replace("-", "/")
                 .Replace(".", "/")
                 .Replace("\\", "/");

            t = Regex.Replace(t, @"\s+", "");

            if (Regex.IsMatch(t, @"^\d{8}$"))
            {
                // ddMMyyyy
                string posibleDia = t.Substring(0, 2);
                string posibleMes = t.Substring(2, 2);
                string posibleAnio = t.Substring(4, 4);

                string fecha1 = posibleDia + "/" + posibleMes + "/" + posibleAnio;

                DateTime aux;
                if (DateTime.TryParseExact(fecha1, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out aux))
                    return fecha1;

                // yyyyMMdd
                string anio = t.Substring(0, 4);
                string mes = t.Substring(4, 2);
                string dia = t.Substring(6, 2);

                string fecha2 = dia + "/" + mes + "/" + anio;

                if (DateTime.TryParseExact(fecha2, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out aux))
                    return fecha2;
            }

            DateTime dt;
            if (DateTime.TryParseExact(t, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.ToString("dd/MM/yyyy");

            if (DateTime.TryParseExact(t, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.ToString("dd/MM/yyyy");

            return t;
        }

        public static string NormalizarNombreApellido(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim();
            t = t.Replace("'", "");
            t = Regex.Replace(t, @"\s+", " ");

            return t.ToUpperInvariant();
        }

        public static string NormalizarTextoLibre(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim();
            t = t.Replace("'", "");
            t = Regex.Replace(t, @"\s+", " ");

            return t;
        }

        public static string NormalizarOrientacion(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            // ==========================================================
            // MODO MANUAL / INDEXADOR HUMANO
            // ==========================================================
            // Intenta canonizar contra opciones conocidas.
            // Si no encuentra nada, conserva el texto libre escrito por el operador.
            // Esto permite que el humano cargue una orientación no contemplada.
            // ==========================================================

            string canonico = CanonizarOrientacionPrestacional(texto);

            if (!string.IsNullOrWhiteSpace(canonico))
                return canonico;

            string t = NormalizarTextoLibre(texto);
            return t.ToUpperInvariant();
        }
        public static string NormalizarOrientacionDesdeOcr(string textoOcr)
        {
            if (string.IsNullOrWhiteSpace(textoOcr))
                return string.Empty;

            // ==========================================================
            // MODO OCR ESTRICTO
            // ==========================================================
            // A diferencia del Leave manual, acá NO conservamos texto libre.
            // Si el OCR no coincide con alguna opción conocida, devolvemos vacío.
            // Esto evita que se cargue basura institucional o texto de otras zonas.
            // ==========================================================

            string limpio = LimpiarTextoBaseOrientacion(textoOcr);
            string canonico = CanonizarOrientacionPrestacional(limpio);

            System.Diagnostics.Debug.Print("======================================");
            System.Diagnostics.Debug.Print("[ORIENTACION OCR] Texto crudo: " + textoOcr);
            System.Diagnostics.Debug.Print("[ORIENTACION OCR] Texto limpio: " + limpio);
            System.Diagnostics.Debug.Print("[ORIENTACION OCR] Canonico: " + canonico);
            System.Diagnostics.Debug.Print("======================================");

            return canonico;
        }


        private static string LimpiarTextoBaseOrientacion(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto;

            t = t.Replace("\r", " ")
                 .Replace("\n", " ")
                 .Replace("\t", " ");

            t = Regex.Replace(
                t,
                @"ORIENTACI[ÓO]N\s+PRESTACIONAL\s*[:\-]?",
                " ",
                RegexOptions.IgnoreCase);

            t = Regex.Replace(
                t,
                @"ORIENTACION\s+PRESTACIONAL\s*[:\-]?",
                " ",
                RegexOptions.IgnoreCase);

            // Corta textos institucionales comunes que suelen aparecer por OCR sucio.
            t = Regex.Replace(
                t,
                @"PARA\s+CONSTATAR\s+LA\s+VERACIDAD.*?(?=(ASISTENCIA|HOGAR|PRESTACIONES|TRANSPORTE|CENTRO|RESIDENCIA|SERVICIO|ESTIMULACION|FORMACION|REHABILITACION))",
                " ",
                RegexOptions.IgnoreCase);

            t = Regex.Replace(
                t,
                @"CONSULTE\s+.*?(?=(ASISTENCIA|HOGAR|PRESTACIONES|TRANSPORTE|CENTRO|RESIDENCIA|SERVICIO|ESTIMULACION|FORMACION|REHABILITACION))",
                " ",
                RegexOptions.IgnoreCase);

            // Quitar códigos CIF que no pertenecen a orientación.
            t = Regex.Replace(t, @"\b[bsde]\s*\d{2,5}\s*\.\s*[\+\-]?\s*\d{1,4}\b", " ", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"\b[bsde]\s*\d{2,5}\b", " ", RegexOptions.IgnoreCase);

            t = CortarAntesDeOrientacion(
                t,
                "ACTUALIZACION",
                "ACTUALIZACIÓN",
                "ACOMPANANTE",
                "ACOMPAÑANTE",
                "LUGAR",
                "FECHA DE EMISION",
                "FECHA DE EMISIÓN",
                "FECHA",
                "VENCIMIENTO",
                "VALIDEZ",
                "DIAGNOSTICO",
                "DIAGNÓSTICO",
                "FUNCIONES CORPORALES",
                "ESTRUCTURAS CORPORALES",
                "ACTIVIDAD",
                "FACTORES AMBIENTALES"
            );

            t = t.Replace("—", "-")
                 .Replace("–", "-")
                 .Replace("_", " ")
                 .Replace("|", " ")
                 .Replace(":", " ")
                 .Replace(";", " ")
                 .Replace(",", " ");

            t = Regex.Replace(t, @"[^\p{L}\d\s\-/\(\)\.]", " ");
            t = Regex.Replace(t, @"\s{2,}", " ").Trim();

            return t;
        }

        private static string CanonizarOrientacionPrestacional(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = NormalizarParaOrientacion(texto);

            List<CandidatoOrientacion> encontrados = new List<CandidatoOrientacion>();

            int idxRehabVisual = BuscarIndiceMinimo(
                t,
                "CENTRO DE REHABILITACION PERS DISCAPACIDAD VISUAL",
                "CENTRO DE REHABILITACION PERS. DISCAPACIDAD VISUAL",
                "DISCAPACIDAD VISUAL",
                "PERS DISCAPACIDAD VISUAL",
                "PERS. DISCAPACIDAD VISUAL"
            );

            AgregarOrientacionSi(
                encontrados,
                "CENTRO DE REHABILITACION PERS. DISCAPACIDAD VISUAL",
                idxRehabVisual >= 0 ||
                ContieneTodosOrientacion(t, "CENTRO", "REHABILITACION", "DISCAPACIDAD", "VISUAL"),
                idxRehabVisual
            );

            int idxRehab = BuscarIndiceMinimo(t, "REHABILIT");

            AgregarOrientacionSi(
                encontrados,
                "PRESTACIONES DE REHABILITACION",
                idxRehab >= 0 &&
                idxRehabVisual < 0 &&
                !ContieneTodosOrientacion(t, "CENTRO", "REHABILITACION", "DISCAPACIDAD", "VISUAL"),
                idxRehab
            );

            int idxAsistencia = BuscarIndiceMinimo(t, "ASISTENCIA", "DOMICILIARIA", "DOMICIL");

            AgregarOrientacionSi(
                encontrados,
                "ASISTENCIA DOMICILIARIA",
                ContieneTodosOrientacion(t, "ASISTENCIA", "DOMICIL"),
                idxAsistencia
            );

            int idxHogar = BuscarIndiceMinimo(t, "HOGAR");

            AgregarOrientacionSi(
                encontrados,
                "HOGAR",
                Regex.IsMatch(t, @"\bHOGAR\b", RegexOptions.IgnoreCase),
                idxHogar
            );

            int idxTransporte = BuscarIndiceMinimo(t, "TRANSPORTE", "TRANSPORT");

            AgregarOrientacionSi(
                encontrados,
                "TRANSPORTE",
                idxTransporte >= 0,
                idxTransporte
            );

            int idxCentroDia = BuscarIndiceMinimo(t, "CENTRO DE DIA", "CENTRO DIA");

            AgregarOrientacionSi(
                encontrados,
                "CENTRO DE DIA",
                Regex.IsMatch(t, @"CENTRO\s+(DE\s+)?D[I1]A", RegexOptions.IgnoreCase),
                idxCentroDia
            );

            int idxResidencia = BuscarIndiceMinimo(t, "RESIDENCIA", "RESIDEN");

            AgregarOrientacionSi(
                encontrados,
                "RESIDENCIA",
                idxResidencia >= 0,
                idxResidencia
            );

            int idxCet = BuscarIndiceMinimo(
                t,
                "CENTRO EDUCATIVO TERAPEUTICO",
                "EDUCATIVO TERAPEUTICO",
                "TERAPEUTICO"
            );

            AgregarOrientacionSi(
                encontrados,
                "CENTRO EDUCATIVO TERAPEUTICO",
                ContieneTodosOrientacion(t, "CENTRO", "EDUCAT", "TERAPEUT"),
                idxCet
            );

            int idxFormacion = BuscarIndiceMinimo(
                t,
                "FORMACION",
                "APRESTAMIENTO",
                "LABORAL",
                "PROFESIONAL"
            );

            AgregarOrientacionSi(
                encontrados,
                "FORMACION/APRESTAMIENTO LABORAL Y/O PROFESIONAL",
                idxFormacion >= 0 &&
                (
                    t.Contains("FORMACION") ||
                    t.Contains("APRESTAMIENTO") ||
                    ContieneTodosOrientacion(t, "LABORAL", "PROFESIONAL")
                ),
                idxFormacion
            );

            int idxInicialEgb = BuscarIndiceMinimo(t, "INICIAL", "EGB");

            AgregarOrientacionSi(
                encontrados,
                "PRESTACIONES EDUCATIVAS (INICIAL/EGB)",
                ContieneTodosOrientacion(t, "EDUCAT") &&
                (t.Contains("INICIAL") || t.Contains("EGB")),
                idxInicialEgb
            );

            int idxApoyo = BuscarIndiceMinimo(t, "APOYO", "INTEGRACION", "ESCOLAR");

            AgregarOrientacionSi(
                encontrados,
                "SERVICIO DE APOYO A LA INTEGRACION ESCOLAR",
                ContieneTodosOrientacion(t, "APOYO", "INTEGR", "ESCOLAR") ||
                ContieneTodosOrientacion(t, "INTEGR", "ESCOLAR"),
                idxApoyo
            );

            int idxEstimulacion = BuscarIndiceMinimo(t, "ESTIMULACION", "TEMPRANA");

            AgregarOrientacionSi(
                encontrados,
                "ESTIMULACION TEMPRANA",
                ContieneTodosOrientacion(t, "ESTIMUL", "TEMPR"),
                idxEstimulacion
            );

            List<string> finales = encontrados
                .OrderBy(x => x.Indice)
                .Select(x => x.Valor)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return finales.Count > 0
                ? string.Join(" - ", finales)
                : string.Empty;
        }

        private static void AgregarOrientacionSi(
            List<CandidatoOrientacion> encontrados,
            string canonico,
            bool condicion,
            int indice)
        {
            if (!condicion)
                return;

            if (indice < 0)
                indice = 99999;

            bool yaExiste = encontrados.Any(x =>
                string.Equals(x.Valor, canonico, StringComparison.OrdinalIgnoreCase));

            if (yaExiste)
                return;

            encontrados.Add(new CandidatoOrientacion
            {
                Valor = canonico,
                Indice = indice
            });
        }

        private static string NormalizarParaOrientacion(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.ToUpperInvariant();

            t = t.Normalize(System.Text.NormalizationForm.FormD);

            t = new string(t.Where(c =>
                System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());

            t = t.Normalize(System.Text.NormalizationForm.FormC);

            t = t.Replace("—", "-")
                 .Replace("–", "-")
                 .Replace("_", " ")
                 .Replace("/", " / ")
                 .Replace("\\", " ")
                 .Replace(".", " ")
                 .Replace(",", " ")
                 .Replace(":", " ")
                 .Replace(";", " ")
                 .Replace("|", " ")
                 .Replace("(", " ")
                 .Replace(")", " ");

            // Correcciones OCR frecuentes.
            t = Regex.Replace(t, @"RENABUITACION", "REHABILITACION", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"RENABILITACION", "REHABILITACION", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"REHABUITACION", "REHABILITACION", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"REHABIL1TACION", "REHABILITACION", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"REHABlLITACION", "REHABILITACION", RegexOptions.IgnoreCase);

            t = Regex.Replace(t, @"DOMICIL1ARIA", "DOMICILIARIA", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"D0MICILIARIA", "DOMICILIARIA", RegexOptions.IgnoreCase);

            t = Regex.Replace(t, @"DISCAPAC[I1]DAD", "DISCAPACIDAD", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"V[I1]SUAL", "VISUAL", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"PERS\s*\.", "PERS", RegexOptions.IgnoreCase);

            t = Regex.Replace(t, @"EDUCAT1VO", "EDUCATIVO", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"TERAPEUT1CO", "TERAPEUTICO", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"INTEGRAC1ON", "INTEGRACION", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"ESTIMULAC1ON", "ESTIMULACION", RegexOptions.IgnoreCase);

            t = Regex.Replace(t, @"\s{2,}", " ").Trim();

            return t;
        }

        private static bool ContieneTodosOrientacion(string texto, params string[] partes)
        {
            if (string.IsNullOrWhiteSpace(texto) || partes == null || partes.Length == 0)
                return false;

            foreach (string p in partes)
            {
                if (string.IsNullOrWhiteSpace(p))
                    continue;

                if (texto.IndexOf(p, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            return true;
        }

        private static int BuscarIndiceMinimo(string texto, params string[] patrones)
        {
            if (string.IsNullOrWhiteSpace(texto) || patrones == null || patrones.Length == 0)
                return -1;

            int menor = -1;

            foreach (string patron in patrones)
            {
                if (string.IsNullOrWhiteSpace(patron))
                    continue;

                int idx = texto.IndexOf(patron, StringComparison.OrdinalIgnoreCase);

                if (idx >= 0 && (menor < 0 || idx < menor))
                    menor = idx;
            }

            return menor;
        }

        private static string CortarAntesDeOrientacion(string texto, params string[] cortes)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            if (cortes == null || cortes.Length == 0)
                return texto;

            int menor = -1;

            foreach (string corte in cortes)
            {
                if (string.IsNullOrWhiteSpace(corte))
                    continue;

                int idx = texto.IndexOf(corte, StringComparison.OrdinalIgnoreCase);

                if (idx >= 0 && (menor < 0 || idx < menor))
                    menor = idx;
            }

            if (menor >= 0)
                return texto.Substring(0, menor);

            return texto;
        }

        private class CandidatoOrientacion
        {
            public string Valor { get; set; }
            public int Indice { get; set; }
        }




        public static string NormalizarSexo(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim().ToUpperInvariant();

            if (t == "F" || t == "FEM" || t == "FEMENINO")
                return "FEMENINO";

            if (t == "M" || t == "MASC" || t == "MASCULINO")
                return "MASCULINO";

            if (t == "X")
                return "X";

            if (t.Contains("NO"))
                return "NO ESPECIFICA";

            return t;
        }

        public static string NormalizarSiNo(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim().ToUpperInvariant();

            if (t == "S" || t == "SI" || t == "SÍ")
                return "SI";

            if (t == "N" || t == "NO")
                return "NO";

            return t;
        }


        public static string NormalizarCodigosCif(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim();

            t = t.Replace("\r", " ")
                 .Replace("\n", " ")
                 .Replace("\t", " ")
                 .Replace(";", " ")
                 .Replace("|", " ")
                 .Replace(",", " ");

            t = t.Replace("—", "-")
                 .Replace("–", "-")
                 .Replace("−", "-")
                 .Replace("_", "");

            // Normaliza espacios alrededor de punto y signos ANTES de separar por espacios.
            // Ej: e5801. +3 -> e5801.+3
            // Ej: e5801. -3 -> e5801.-3
            t = Regex.Replace(t, @"\s*\.\s*", ".");
            t = Regex.Replace(t, @"\s*([+-])\s*", "$1");

            // Corrige ruido típico OCR:
            // e5801.-+3 -> e5801.+3
            // e5801.+-3 -> e5801.-3
            // e1101.-.+3 -> e1101.+3
            t = t.Replace(".-+.", ".+")
                 .Replace(".+-.", ".-")
                 .Replace(".-+", ".+")
                 .Replace(".+-", ".-")
                 .Replace(".-.+", ".+")
                 .Replace(".+.-", ".-");

            t = Regex.Replace(t, @"\s+", " ");

            string[] partes = t.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> normalizados = new List<string>();

            foreach (string parte in partes)
            {
                string p = parte.Trim();

                if (string.IsNullOrWhiteSpace(p))
                    continue;

                p = p.ToLowerInvariant();

                // Corrige 0e1201.+3 -> e1201.+3
                // Corrige oe1201.+3 -> e1201.+3
                p = Regex.Replace(p, @"^[0o]+(?=[bsde])", "");

                // Corrige e.5801.+3 -> e5801.+3
                p = Regex.Replace(p, @"^([bsde])\.(?=\d)", "$1");

                // Corrige e5801+3 -> e5801.+3
                // Corrige e5801-3 -> e5801.-3
                p = Regex.Replace(
                    p,
                    @"^([bsde]\d{2,4}(?:\.\d{1,4})*)([+-]\d{1,2})$",
                    "$1.$2");

                // Limpieza final suave: no elimina punto, + ni -
                p = Regex.Replace(p, @"\s+", "");

                if (!string.IsNullOrWhiteSpace(p))
                    normalizados.Add(p);
            }

            return string.Join(" ", normalizados);
        }


        public static string PrepararTextoCifDesdeOcr(string textoOcr, char letraEsperada)
        {
            letraEsperada = char.ToLowerInvariant(letraEsperada);

            if (string.IsNullOrWhiteSpace(textoOcr))
                return string.Empty;

            string t = textoOcr;

            System.Diagnostics.Debug.Print("======================================");
            System.Diagnostics.Debug.Print("[CIF OCR] Inicio PrepararTextoCifDesdeOcr");
            System.Diagnostics.Debug.Print("[CIF OCR] Letra esperada: " + letraEsperada);
            System.Diagnostics.Debug.Print("[CIF OCR] Texto crudo: " + textoOcr);

            t = t.Replace("\r", " ")
                 .Replace("\n", " ")
                 .Replace("\t", " ")
                 .Replace(",", " ")
                 .Replace(";", " ")
                 .Replace("|", " ")
                 .Replace("—", "-")
                 .Replace("–", "-")
                 .Replace("−", "-")
                 .Replace("_", " ")
                 .Replace(">", " ")
                 .Replace("<", " ")
                 .Replace("=", " ")
                 .Replace(":", " ")
                 .Replace("[", " ")
                 .Replace("]", " ")
                 .Replace("(", " ")
                 .Replace(")", " ")
                 .Replace("/", " ");

            // Corrige ruido OCR antes de buscar candidatos.
            // Ej: e1101.-.+3 -> e1101.+3
            t = Regex.Replace(t, @"\.\s*-\s*\.\s*\+\s*", ".+");
            t = Regex.Replace(t, @"\.\s*-\s*\+\s*", ".+");
            t = Regex.Replace(t, @"\.\s*\+\s*-\s*", ".-");

            t = Regex.Replace(t, @"\s+", " ").Trim();

            System.Diagnostics.Debug.Print("[CIF OCR] Texto pre-limpieza: " + t);

            List<string> codigos = new List<string>();

            // Guarda los rangos del texto que ya fueron tomados como código con letra.
            // Esto evita que el mismo "s110.888" sea leído después como "sin letra".
            List<Tuple<int, int>> rangosYaTomados = new List<Tuple<int, int>>();

            string letra = Regex.Escape(letraEsperada.ToString());

            // Para cuerpos numéricos internos permitimos letras confundibles.
            // Ejemplo: e5S700.+3 puede normalizarse bien.
            string digitoOcr = @"0-9oOiIlLzZsS";

            // Para códigos SIN letra inicial, no permitimos que empiecen con s/S,
            // porque si no "s110.888" se interpreta como número y termina en "s7110.888".
            string digitoOcrInicioSinLetra = @"0-9oOiIlLzZ";

            // ==========================================================
            // 1) Códigos que ya tienen la letra esperada.
            // ==========================================================
            string patronConLetra =
                @"(?<![a-zA-Z0-9.])" +
                @"(?:[0oO]\s*)?" +
                letra +
                @"\s*[" + digitoOcr + @"]{2,6}" +
                @"(?:\s*\.\s*[" + digitoOcr + @"]{1,4}){0,3}" +
                @"(?:" +
                    @"\s*\.\s*[+-]\s*[" + digitoOcr + @"]{1,2}" +
                    @"|" +
                    @"\s*[+-]\s*[" + digitoOcr + @"]{1,2}" +
                @")?" +
                @"(?:\s*\+)?" +
                @"(?![a-zA-Z])";

            foreach (Match m in Regex.Matches(t, patronConLetra, RegexOptions.IgnoreCase))
            {
                string candidatoCrudo = m.Value;
                string codigoFinal = NormalizarCandidatoCifOcr(candidatoCrudo, letraEsperada);

                System.Diagnostics.Debug.Print("[CIF OCR] Candidato con letra: " + candidatoCrudo + " -> " + codigoFinal);

                if (CodigoCifOcrPareceValido(codigoFinal, letraEsperada))
                {
                    if (!codigos.Contains(codigoFinal))
                    {
                        codigos.Add(codigoFinal);
                        rangosYaTomados.Add(new Tuple<int, int>(m.Index, m.Length));

                        System.Diagnostics.Debug.Print("[CIF OCR] Aceptado: " + codigoFinal);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print("[CIF OCR] Ya existía, no se duplica: " + codigoFinal);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Print("[CIF OCR] Descartado: " + codigoFinal);
                }
            }

            // ==========================================================
            // 1.B) Códigos donde el OCR confundió la letra inicial.
            // ==========================================================
            if (letraEsperada == 'b' ||
                letraEsperada == 's' ||
                letraEsperada == 'd' ||
                letraEsperada == 'e')
            {
                string patronLetraInicialConfundida =
                    @"(?<![a-zA-Z0-9])" +
                    @"([a-zA-Z])\s*" +
                    @"([" + digitoOcr + @"]{2,6}" +
                    @"(?:\s*\.\s*[" + digitoOcr + @"]{1,4}){0,3}" +
                    @"(?:" +
                        @"\s*\.\s*[+-]\s*[" + digitoOcr + @"]{1,2}" +
                        @"|" +
                        @"\s*[+-]\s*[" + digitoOcr + @"]{1,2}" +
                    @")?" +
                    @"(?:\s*\+)?" +
                    @")" +
                    @"(?![a-zA-Z])";

                foreach (Match m in Regex.Matches(t, patronLetraInicialConfundida, RegexOptions.IgnoreCase))
                {
                    string candidatoCrudo = m.Value;
                    string letraDetectada = m.Groups[1].Value;
                    string cuerpoDetectado = m.Groups[2].Value;

                    if (string.IsNullOrWhiteSpace(letraDetectada) ||
                        string.IsNullOrWhiteSpace(cuerpoDetectado))
                    {
                        continue;
                    }

                    char letraOcr = char.ToLowerInvariant(letraDetectada[0]);

                    // Si ya era la letra esperada, este caso ya lo toma el bloque anterior.
                    if (letraOcr == letraEsperada)
                        continue;

                    string cuerpo = cuerpoDetectado.Trim().ToLowerInvariant();
                    cuerpo = Regex.Replace(cuerpo, @"\s+", "");

                    NormalizarSoloNumeroCifDesdeOcr(cuerpo, letraEsperada);

                    string codigoFinal = letraEsperada + cuerpo;
                    codigoFinal = NormalizarCodigosCif(codigoFinal);

                    System.Diagnostics.Debug.Print("[CIF OCR] Candidato con letra inicial confundida:");
                    System.Diagnostics.Debug.Print("[CIF OCR] Crudo: " + candidatoCrudo);
                    System.Diagnostics.Debug.Print("[CIF OCR] Letra OCR detectada: " + letraOcr);
                    System.Diagnostics.Debug.Print("[CIF OCR] Letra esperada real: " + letraEsperada);
                    System.Diagnostics.Debug.Print("[CIF OCR] Probando código corregido: " + codigoFinal);

                    if (CodigoCifOcrPareceValido(codigoFinal, letraEsperada))
                    {
                        if (!codigos.Contains(codigoFinal))
                        {
                            codigos.Add(codigoFinal);
                            rangosYaTomados.Add(new Tuple<int, int>(m.Index, m.Length));

                            System.Diagnostics.Debug.Print("[CIF OCR] Aceptado con letra inicial corregida: " + codigoFinal);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Print("[CIF OCR] Ya existía, no se duplica: " + codigoFinal);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print("[CIF OCR] Descartado con letra inicial corregida: " + codigoFinal);
                    }
                }
            }

            // ==========================================================
            // 2) Códigos donde el OCR pudo haber perdido la letra.
            //
            // Importante:
            // - No debe volver a tomar códigos ya encontrados con letra.
            // - No debe aceptar candidatos que arranquen con letra.
            // ==========================================================
            string patronSinLetra =
                @"(?<![a-zA-Z0-9])" +
                @"[" + digitoOcrInicioSinLetra + @"]" +
                @"[" + digitoOcr + @"]{1,5}" +
                @"(?:\s*\.\s*[" + digitoOcr + @"]{1,4}){1,3}" +
                @"(?:" +
                    @"\s*\.\s*[+-]\s*[" + digitoOcr + @"]{1,2}" +
                    @"|" +
                    @"\s*[+-]\s*[" + digitoOcr + @"]{1,2}" +
                @")?" +
                @"(?:\s*\+)?" +
                @"(?![a-zA-Z])";

            foreach (Match m in Regex.Matches(t, patronSinLetra, RegexOptions.IgnoreCase))
            {
                if (HaySolapamientoConRangosTomados(m.Index, m.Length, rangosYaTomados))
                {
                    System.Diagnostics.Debug.Print("[CIF OCR] Candidato sin letra omitido por solapamiento: " + m.Value);
                    continue;
                }

                string candidatoCrudo = m.Value.Trim();

                if (string.IsNullOrWhiteSpace(candidatoCrudo))
                    continue;

                // Defensa extra:
                // si empieza con letra, no es realmente un candidato sin letra.
                if (char.IsLetter(candidatoCrudo[0]))
                {
                    System.Diagnostics.Debug.Print("[CIF OCR] Candidato sin letra descartado porque empieza con letra: " + candidatoCrudo);
                    continue;
                }

                string numero = NormalizarSoloNumeroCifDesdeOcr(
           candidatoCrudo.Replace(" ", ""),
           letraEsperada);

                string codigoFinal = letraEsperada + numero;
                codigoFinal = NormalizarCodigosCif(codigoFinal);

                System.Diagnostics.Debug.Print("[CIF OCR] Candidato sin letra: " + candidatoCrudo + " -> " + codigoFinal);

                if (CodigoCifOcrPareceValido(codigoFinal, letraEsperada))
                {
                    if (!codigos.Contains(codigoFinal))
                    {
                        codigos.Add(codigoFinal);
                        System.Diagnostics.Debug.Print("[CIF OCR] Aceptado sin letra: " + codigoFinal);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print("[CIF OCR] Ya existía, no se duplica: " + codigoFinal);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Print("[CIF OCR] Descartado sin letra: " + codigoFinal);
                }
            }

            string resultado = NormalizarCodigosCif(string.Join(" ", codigos));

            System.Diagnostics.Debug.Print("[CIF OCR] Texto final preparado: " + resultado);
            System.Diagnostics.Debug.Print("======================================");

            return resultado;
        }
        private static bool HaySolapamientoConRangosTomados(
    int indice,
    int longitud,
    List<Tuple<int, int>> rangosYaTomados)
        {
            int inicio = indice;
            int fin = indice + longitud;

            foreach (Tuple<int, int> rango in rangosYaTomados)
            {
                int inicioTomado = rango.Item1;
                int finTomado = rango.Item1 + rango.Item2;

                bool solapa = inicio < finTomado && fin > inicioTomado;

                if (solapa)
                    return true;
            }

            return false;
        }

        private static string NormalizarCandidatoCifOcr(string candidato, char letraEsperada)
        {
            letraEsperada = char.ToLowerInvariant(letraEsperada);

            if (string.IsNullOrWhiteSpace(candidato))
                return string.Empty;

            string p = candidato.Trim().ToLowerInvariant();

            p = p.Replace("—", "-")
                 .Replace("–", "-")
                 .Replace("−", "-")
                 .Replace("_", "");

            p = Regex.Replace(p, @"\s+", "");

            // Si vino 0e1201.+3, oe1201.+3, etc.,
            // conserva desde la letra esperada.
            int indiceLetra = p.IndexOf(letraEsperada);
            if (indiceLetra > 0)
                p = p.Substring(indiceLetra);

            if (!p.StartsWith(letraEsperada.ToString()))
                return string.Empty;

            // Corrige e.5801.+3 -> e5801.+3
            p = Regex.Replace(p, @"^([bsde])\.(?=\d)", "$1");

            // Corrige variantes rotas de signos.
            p = p.Replace(".-+.", ".+")
                 .Replace(".+-.", ".-")
                 .Replace(".-+", ".+")
                 .Replace(".+-", ".-")
                 .Replace(".-.+", ".+")
                 .Replace(".+.-", ".-");

            string cuerpo = p.Substring(1);

            cuerpo = NormalizarSoloNumeroCifDesdeOcr(cuerpo, letraEsperada);

            p = letraEsperada + cuerpo;

            // Corrige e5801+3 -> e5801.+3
            // Corrige e5801-3 -> e5801.-3
            p = Regex.Replace(
                p,
                @"^([bsde]\d{2,4}(?:\.\d{1,4})*)([+-]\d{1,2})$",
                "$1.$2");

            return p;
        }



        private static string NormalizarSoloNumeroCifDesdeOcr(string texto, char letraEsperada)
        {
            letraEsperada = char.ToLowerInvariant(letraEsperada);

            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string original = texto;

            // ==========================================================
            // REGLA IMPORTANTE:
            //
            // Si la letra esperada es 's' estamos en Estructuras corporales.
            // En ese caso NO conviene convertir s/S dentro del cuerpo numérico,
            // porque puede generar falsos códigos como:
            // s110.888 -> s7110.888
            //
            // Si la letra esperada es b, d o e, estamos en:
            // b = Funciones corporales
            // d = Actividad y participación
            // e = Factores ambientales
            //
            // En esos casos una S dentro del cuerpo suele ser ruido OCR o un 5 mal leído.
            // ==========================================================

            if (letraEsperada == 's')
            {
                texto = NormalizarCaracteresNumericosCifBasico(texto, convertirS: false);

                texto = new string(
                    texto
                        .Where(c => char.IsDigit(c) || c == '.' || c == '+' || c == '-')
                        .ToArray());

                return texto;
            }

            // ==========================================================
            // Para b, d y e probamos alternativas.
            // El caso típico es:
            //
            // eS5700.+2
            //
            // Alternativa correcta probable:
            // e5700.+2
            //
            // No hacemos solamente S -> 5 porque:
            // S5700 -> 55700, que puede quedar inválido por longitud.
            // ==========================================================

            List<string> alternativas = new List<string>();

            // Alternativa 1:
            // Si la S está inmediatamente al inicio del cuerpo y antes de un número,
            // se interpreta como ruido pegado a la letra CIF.
            // eS5700.+2 -> 5700.+2
            string sinSInicialPegada = Regex.Replace(original, @"^[sS](?=\d)", "");
            alternativas.Add(sinSInicialPegada);

            // Alternativa 2:
            // Caso interno tipo e5S700, donde la S puede ser ruido antes de 7.
            // e5S700 -> e5700
            string sinSAntesDe7 = Regex.Replace(original, @"(?i)s(?=7)", "");
            alternativas.Add(sinSAntesDe7);

            // Alternativa 3:
            // S/s como posible 5.
            string sComo5 = Regex.Replace(original, @"[sS]", "5");
            alternativas.Add(sComo5);

            // Alternativa 4:
            // Fallback viejo: S/s como 7.
            string sComo7 = Regex.Replace(original, @"[sS]", "7");
            alternativas.Add(sComo7);

            foreach (string alternativa in alternativas)
            {
                string normalizada = NormalizarCaracteresNumericosCifBasico(alternativa, convertirS: false);

                normalizada = new string(
                    normalizada
                        .Where(c => char.IsDigit(c) || c == '.' || c == '+' || c == '-')
                        .ToArray());

                if (!string.IsNullOrWhiteSpace(normalizada))
                    return normalizada;
            }

            return string.Empty;
        }

        private static string NormalizarCaracteresNumericosCifBasico(string texto, bool convertirS)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            char[] chars = texto.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];

                if (c == 'o' || c == 'O')
                {
                    chars[i] = '0';
                }
                else if (c == 'z' || c == 'Z')
                {
                    chars[i] = '2';
                }
                else if (c == 'i' || c == 'I' || c == 'l' || c == 'L')
                {
                    chars[i] = '1';
                }
                else if (convertirS && (c == 's' || c == 'S'))
                {
                    chars[i] = '5';
                }
            }

            return new string(chars);
        }

        private static bool CodigoCifOcrPareceValido(string codigo, char letraEsperada)
        {
            letraEsperada = char.ToLowerInvariant(letraEsperada);

            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            codigo = NormalizarCodigosCif(codigo).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            if (!codigo.StartsWith(letraEsperada.ToString()))
                return false;

            string cuerpo = codigo.Substring(1);

            // Evita falsos positivos como d1, s1, b1, e1.
            if (Regex.IsMatch(cuerpo, @"^\d{1}$"))
                return false;

            int maxDigitosBase = ObtenerMaxDigitosBaseCif(letraEsperada);

            string patron =
                @"^" + Regex.Escape(letraEsperada.ToString()) +
                @"\d{2," + maxDigitosBase + "}" +
                @"(?:" +
                    // s73023.373 / s75002.372 / d240.23 / s110.888 / b167.3+
                    @"(?:\.\d{1,4})+(?:\+)?" +
                    @"|" +
                    // e5801.+3 / e11.1.+3
                    @"(?:\.\d{1,4})*\.[+-]\d{1,2}" +
                    @"|" +
                    // e5801-3 / e5801+3, por si el punto se perdió
                    @"(?:\.\d{1,4})*[+-]\d{1,2}" +
                @")$";

            return Regex.IsMatch(codigo, patron);
        }


        public static string SanitizarNombreArchivo(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim();

            foreach (char c in PathInvalidChars())
                t = t.Replace(c.ToString(), "");

            t = Regex.Replace(t, @"\s+", " ");
            t = t.Replace(" ", "_");

            return t;
        }

        private static IEnumerable<char> PathInvalidChars()
        {
            return new[]
            {
                '\\', '/', ':', '*', '?', '"', '<', '>', '|'
            };
        }

        private static string SoloDigitos(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            return new string(texto.Where(char.IsDigit).ToArray());
        }

        // ==========================================================
        // PARSEO NÚMERO CERTIFICADO
        // ==========================================================

        public static DatosNumeroCertificadoCud ParseNumeroCertificado(string texto)
        {
            DatosNumeroCertificadoCud r = new DatosNumeroCertificadoCud
            {
                EsValido = false,
                Mensaje = string.Empty,
                NumeroNormalizado = string.Empty,
                Sexo = string.Empty,
                Dni = string.Empty,
                FechaEmision = string.Empty,
                FechaVencimiento = string.Empty
            };

            string n = NormalizarNumeroCertificado(texto);
            r.NumeroNormalizado = n;

            if (string.IsNullOrWhiteSpace(n))
            {
                r.Mensaje = "Ingrese número de certificado.";
                return r;
            }

            if (!RegexNumeroCertificado.IsMatch(n))
            {
                r.Mensaje = "Formato inválido. Ejemplo: ARG-02-00010400280-20151118-20201118-CBA-536.";
                return r;
            }

            string[] partes = n.Split('-');

            if (partes.Length != 7)
            {
                r.Mensaje = "El número de certificado no tiene la cantidad esperada de segmentos.";
                return r;
            }


            string tipoSexo = partes[1];
            string dni11 = partes[2];
            string fechaEmision8 = partes[3];
            string fechaVencimiento8 = partes[4];

            string dni = dni11.TrimStart('0');

            if (dni.Length < 7 || dni.Length > 8)
            {
                r.Mensaje = "El DNI incluido en el número de certificado no tiene longitud válida.";
                return r;
            }
           
            string sexo = ObtenerSexoDesdeTipoCertificado(tipoSexo);
            string fechaEmision = ConvertirFechaYyyyMMddADdMmYyyy(fechaEmision8);
            string fechaVencimiento = ConvertirFechaYyyyMMddADdMmYyyy(fechaVencimiento8);

            if (string.IsNullOrWhiteSpace(fechaEmision))
            {
                r.Mensaje = "La fecha de emisión del número de certificado no es válida.";
                return r;
            }

            if (string.IsNullOrWhiteSpace(fechaVencimiento))
            {
                r.Mensaje = "La fecha de vencimiento del número de certificado no es válida.";
                return r;
            }


          
            r.EsValido = true;
            r.Mensaje = "Número de certificado válido.";
            r.Sexo = sexo;
            r.Dni = dni;
            r.FechaEmision = fechaEmision;
            r.FechaVencimiento = fechaVencimiento;

            return r;
        }

        private static string ObtenerSexoDesdeTipoCertificado(string tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                return string.Empty;

            string t = Regex.Replace(tipo, @"\D", "");

            if (t.Length == 1)
                t = t.PadLeft(2, '0');

            // Según regla CUD:
            // 01 = Mujer
            // 02 = Hombre
            if (t == "01")
                return "FEMENINO";

            if (t == "02")
                return "MASCULINO";

            return string.Empty;
        }


        private static string ConvertirFechaYyyyMMddADdMmYyyy(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            DateTime dt;

            if (DateTime.TryParseExact(texto, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.ToString("dd/MM/yyyy");

            return string.Empty;
        }

        // ==========================================================
        // VALIDADORES
        // ==========================================================

        public static ResultadoValidacion ValidarObligatorio(string valor, string nombreCampo)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return ResultadoValidacion.Error(nombreCampo + " es obligatorio.");

            return ResultadoValidacion.Ok();
        }

        public static ResultadoValidacion ValidarDni(string dni, bool obligatorio)
        {
            dni = NormalizarDni(dni);

            if (string.IsNullOrWhiteSpace(dni))
            {
                return obligatorio
                    ? ResultadoValidacion.Error("DNI obligatorio.")
                    : ResultadoValidacion.Ok();
            }

            if (dni.Length < 7 || dni.Length > 8)
                return ResultadoValidacion.Error("El DNI debe tener 7 u 8 dígitos.");

            return ResultadoValidacion.Ok();
        }

        public static ResultadoValidacion ValidarCuil(string cuil, string dni, bool obligatorio)
        {
            cuil = NormalizarCuil(cuil);
            dni = NormalizarDni(dni);

            if (string.IsNullOrWhiteSpace(cuil))
            {
                return obligatorio
                    ? ResultadoValidacion.Error("CUIL obligatorio.")
                    : ResultadoValidacion.Ok();
            }

            if (cuil.Length != 11)
                return ResultadoValidacion.Error("El CUIL debe tener exactamente 11 dígitos.");

            if (!cuil.All(char.IsDigit))
                return ResultadoValidacion.Error("El CUIL solo puede contener números.");

            // Validación principal: que el CUIL contenga el mismo DNI cargado.
            // Formato esperado: PP + DNI(8 dígitos) + DV
            // Ejemplo: 20 + 11132502 + 0 = 20111325020
            if (!string.IsNullOrWhiteSpace(dni))
            {
                string dniDentroCuil = cuil.Substring(2, 8).TrimStart('0');
                string dniNormalizado = dni.TrimStart('0');

                if (dniDentroCuil != dniNormalizado)
                {
                    return ResultadoValidacion.Error(
                        "El CUIL no coincide con el DNI cargado. " +
                        "DNI: " + dniNormalizado + " / DNI dentro del CUIL: " + dniDentroCuil);
                }
            }

            return ResultadoValidacion.Ok();
        }


        public static ResultadoValidacion ValidarFecha(
            string fecha,
            string nombreCampo,
            bool obligatorio,
            bool permiteFutura,
            DateTime? minimo,
            DateTime? maximo)
        {
            fecha = NormalizarFecha(fecha);

            if (string.IsNullOrWhiteSpace(fecha))
            {
                return obligatorio
                    ? ResultadoValidacion.Error(nombreCampo + " obligatoria.")
                    : ResultadoValidacion.Ok();
            }

            DateTime dt;

            if (!DateTime.TryParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return ResultadoValidacion.Error(nombreCampo + " inválida. Use formato dd/MM/yyyy.");

            if (minimo.HasValue && dt < minimo.Value)
                return ResultadoValidacion.Error(nombreCampo + " no puede ser anterior a " + minimo.Value.ToString("dd/MM/yyyy") + ".");

            if (!permiteFutura && dt.Date > DateTime.Today)
                return ResultadoValidacion.Error(nombreCampo + " no puede ser futura.");

            if (maximo.HasValue && dt.Date > maximo.Value.Date)
                return ResultadoValidacion.Error(nombreCampo + " excede el máximo permitido.");

            return ResultadoValidacion.Ok();
        }

        public static ResultadoValidacion ValidarFechasCruzadas(string fechaNacimiento, string fechaEmision, string fechaVencimiento)
        {
            DateTime nac;
            DateTime emi;
            DateTime ven;

            bool okNac = DateTime.TryParseExact(NormalizarFecha(fechaNacimiento), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out nac);
            bool okEmi = DateTime.TryParseExact(NormalizarFecha(fechaEmision), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out emi);
            bool okVen = DateTime.TryParseExact(NormalizarFecha(fechaVencimiento), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out ven);

            if (okNac && okEmi && emi < nac)
                return ResultadoValidacion.Error("La fecha de emisión no puede ser anterior a la fecha de nacimiento.");

            if (okEmi && okVen && ven < emi)
                return ResultadoValidacion.Error("La fecha de vencimiento no puede ser anterior a la fecha de emisión.");

            return ResultadoValidacion.Ok();
        }

        public static ResultadoValidacion ValidarCodigosCif(string texto, char letraEsperada, string nombreCampo, bool obligatorio)
        {
            letraEsperada = char.ToLowerInvariant(letraEsperada);

            string textoOriginal = texto ?? string.Empty;
            texto = NormalizarCodigosCif(texto);

            System.Diagnostics.Debug.Print("======================================");
            System.Diagnostics.Debug.Print("[CIF VALIDACION] Campo: " + nombreCampo);
            System.Diagnostics.Debug.Print("[CIF VALIDACION] Letra esperada: " + letraEsperada);
            System.Diagnostics.Debug.Print("[CIF VALIDACION] Texto original: " + textoOriginal);
            System.Diagnostics.Debug.Print("[CIF VALIDACION] Texto normalizado: " + texto);

            if (string.IsNullOrWhiteSpace(texto))
            {
                System.Diagnostics.Debug.Print("[CIF VALIDACION] Texto vacío.");

                return obligatorio
                    ? ResultadoValidacion.Error(nombreCampo + " es obligatorio.")
                    : ResultadoValidacion.Ok();
            }

            string[] codigos = texto.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string codigo in codigos)
            {
                string c = codigo.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(c))
                    continue;

                System.Diagnostics.Debug.Print("[CIF VALIDACION] Validando código: " + c);

                if (c[0] != letraEsperada)
                {
                    System.Diagnostics.Debug.Print("[CIF VALIDACION] Rechazado por letra incorrecta: " + c);

                    return ResultadoValidacion.Error(
                        nombreCampo + " debe contener códigos que comiencen con '" +
                        letraEsperada + "'. Código detectado: " + codigo);
                }

                bool validoPorValidadorPrincipal = EsCodigoCifValidoPorLetra(c, letraEsperada);
                bool validoPorFormatoOcr = CodigoCifOcrPareceValido(c, letraEsperada);

                System.Diagnostics.Debug.Print(
                    "[CIF VALIDACION] Resultado " + c +
                    " | EsCodigoCifValidoPorLetra=" + validoPorValidadorPrincipal +
                    " | CodigoCifOcrPareceValido=" + validoPorFormatoOcr);

                if (!validoPorValidadorPrincipal && !validoPorFormatoOcr)
                {
                    return ResultadoValidacion.Error(
                        "Código CIF inválido en " + nombreCampo + ": " + codigo);
                }
            }

            System.Diagnostics.Debug.Print("[CIF VALIDACION] OK");
            System.Diagnostics.Debug.Print("======================================");

            return ResultadoValidacion.Ok();
        }

        private static int ObtenerMaxDigitosBaseCif(char letraEsperada)
        {
            letraEsperada = char.ToLowerInvariant(letraEsperada);

            // Estructuras corporales:
            // s4101.370
            // s73023.373
            // s75002.372
            if (letraEsperada == 's')
                return 5;

            // Funciones corporales:
            // b167.3
            // b28016.3
            if (letraEsperada == 'b')
                return 5;

            // Actividad/participación y factores ambientales:
            // d240.23
            // e5801.+3
            return 4;
        }


        public static bool EstadoOcrPermitePrecarga(string estadoOcr)
        {
            if (string.IsNullOrWhiteSpace(estadoOcr))
                return false;

            return estadoOcr.Trim().Equals("OKAutomatico", StringComparison.OrdinalIgnoreCase);
        }

        public static ResultadoValidacion ValidarApellidoNombrePreprocesamiento(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return ResultadoValidacion.Ok("Apellido y nombre vacío. No se carga.");

            string t = NormalizarNombreApellido(texto);

            if (t.Length < 5)
                return ResultadoValidacion.Error("Apellido y nombre demasiado corto.");

            if (t.Length > 120)
                return ResultadoValidacion.Error("Apellido y nombre demasiado largo.");

            if (t.All(char.IsDigit))
                return ResultadoValidacion.Error("Apellido y nombre contiene solo números.");

            string[] basuraInstitucional =
            {
        "GOBIERNO",
        "CERTIFICADO",
        "DISCAPACIDAD",
        "MINISTERIO",
        "SALUD",
        "LEY",
        "CUD",
        "DNI",
        "CUIL",
        "DIAGNOSTICO",
        "FECHA",
        "NACIMIENTO",
        "DOMICILIO",
        "NACIONALIDAD",
        "ARGENTINA",
        "JUNTA",
        "EVALUADORA"
    };

            foreach (string palabra in basuraInstitucional)
            {
                if (t.Contains(palabra))
                    return ResultadoValidacion.Error("Apellido y nombre contiene texto institucional: " + palabra);
            }

            int letras = t.Count(char.IsLetter);
            int simbolos = t.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

            if (letras < 4)
                return ResultadoValidacion.Error("Apellido y nombre tiene pocas letras útiles.");

            if (simbolos > 5)
                return ResultadoValidacion.Error("Apellido y nombre tiene demasiados símbolos.");

            return ResultadoValidacion.Ok();
        }

        public static string NormalizarAcompaniantePreprocesamiento(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string t = texto.Trim().ToUpperInvariant();

            t = t.Replace("Í", "I")
                 .Replace("Á", "A")
                 .Replace("É", "E")
                 .Replace("Ó", "O")
                 .Replace("Ú", "U");

            if (t == "S" || t == "SI" || t.Contains("CON ACOMP"))
                return "SI";

            if (t == "N" || t == "NO" || t.Contains("SIN ACOMP"))
                return "NO";

            return string.Empty;
        }


        private static bool EsCodigoCifValidoPorLetra(string codigo, char letraEsperada)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            codigo = codigo.ToLowerInvariant().Trim();
            letraEsperada = char.ToLowerInvariant(letraEsperada);

            switch (letraEsperada)
            {
                case 'b':
                    // Ejemplo: b167.3
                    return Regex.IsMatch(codigo, @"^b\d{2,4}\.\d{1,4}$");

                case 's':
                    // Ejemplo: s110.888
                    return Regex.IsMatch(codigo, @"^s\d{2,5}\.\d{1,4}$");

                case 'd':
                    // Ejemplos: d240.23, d155.213
                    return Regex.IsMatch(codigo, @"^d\d{2,4}\.\d{1,4}$");

                case 'e':
                    // Ejemplos: e355.+3, e310.+3, e5801.+3, e5801.-3
                    return Regex.IsMatch(codigo, @"^e\d{2,4}\.[+-]?\d$");

                default:
                    return false;
            }
        }


    }
}