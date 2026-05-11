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

            string t = NormalizarTextoLibre(texto);
            return t.ToUpperInvariant();
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

            t = Regex.Replace(t, @"\s+", " ");

            string[] partes = t.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> normalizados = new List<string>();

            foreach (string parte in partes)
            {
                string p = parte.Trim();

                if (string.IsNullOrWhiteSpace(p))
                    continue;

                p = p.ToLowerInvariant();

                p = p.Replace("—", "-")
                     .Replace("–", "-")
                     .Replace("_", "");

                // Corrige e.5801.+3 -> e5801.+3
                p = Regex.Replace(p, @"^([bsde])\.(?=\d)", "$1");

                // Corrige espacios alrededor del signo:
                // e5801. +3 -> e5801.+3
                // e5801. -3 -> e5801.-3
                p = Regex.Replace(p, @"\.\s*([+-])\s*", ".$1");

                // Corrige ruido OCR típico:
                // e5801.-+3 -> e5801.+3
                // e5801.+-3 -> e5801.-3
                p = p.Replace(".-+", ".+")
                     .Replace(".+-", ".-");

                p = Regex.Replace(p, @"\s+", "");

                normalizados.Add(p);
            }

            return string.Join(" ", normalizados);
        }

        public static string PrepararTextoCifDesdeOcr(string textoOcr, char letraEsperada)
        {
            if (string.IsNullOrWhiteSpace(textoOcr))
                return string.Empty;

            string t = textoOcr;

            // Normalizamos separadores, pero NO convertimos letras a números globalmente.
            t = t.Replace("\r", " ")
                 .Replace("\n", " ")
                 .Replace("\t", " ")
                 .Replace(",", " ")
                 .Replace(";", " ")
                 .Replace("|", " ")
                 .Replace("—", " ")
                 .Replace("–", " ")
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

            t = Regex.Replace(t, @"\s+", " ").Trim();

            List<string> codigos = new List<string>();

            string letra = Regex.Escape(letraEsperada.ToString());

            // Caracteres que el OCR suele confundir dentro de números.
            // OJO: solo se usan dentro de patrones candidatos, no sobre todo el texto.
            string digitoOcr = @"0-9oOiIlLzZ";

            // ==========================================================
            // 1) Códigos que ya tienen la letra esperada.
            // Ejemplos:
            // d240.23
            // d7202.23
            // s110.888
            // d 240.23
            // ==========================================================
            string patronConLetra =
                @"(?<![a-zA-Z0-9])" +
                letra +
                @"\s*[" + digitoOcr + @"]{2,4}" +
                @"(?:\s*\.\s*[" + digitoOcr + @"]{1,4})*" +
                @"(?:\+)?" +
                @"(?![a-zA-Z])";

            foreach (Match m in Regex.Matches(t, patronConLetra, RegexOptions.IgnoreCase))
            {
                string codigo = m.Value.Replace(" ", "").ToLowerInvariant();

                if (codigo.Length < 2)
                    continue;

                string cuerpo = codigo.Substring(1);
                cuerpo = NormalizarSoloNumeroCifDesdeOcr(cuerpo);

                string codigoFinal = letraEsperada + cuerpo;

                if (CodigoCifOcrPareceValido(codigoFinal, letraEsperada))
                    codigos.Add(codigoFinal);
            }

            // ==========================================================
            // 2) Códigos donde el OCR perdió la letra.
            // Ejemplos:
            // 1160.23  -> d1160.23
            // 110.888  -> s110.888
            //
            // IMPORTANTE:
            // Acá exigimos punto decimal/clasificador.
            // Así evitamos falsos positivos como:
            // LAC -> 1AC -> d1
            // ==========================================================
            string patronSinLetra =
                @"(?<![a-zA-Z0-9])" +
                @"[" + digitoOcr + @"]{2,4}" +
                @"\s*\.\s*[" + digitoOcr + @"]{1,4}" +
                @"(?:\s*\.\s*[" + digitoOcr + @"]{1,4})*" +
                @"(?:\+)?" +
                @"(?![a-zA-Z])";

            foreach (Match m in Regex.Matches(t, patronSinLetra, RegexOptions.IgnoreCase))
            {
                string numero = m.Value.Replace(" ", "").ToLowerInvariant();
                numero = NormalizarSoloNumeroCifDesdeOcr(numero);

                string codigoFinal = letraEsperada + numero;

                if (CodigoCifOcrPareceValido(codigoFinal, letraEsperada))
                    codigos.Add(codigoFinal);
            }

            // ==========================================================
            // 3) Limpieza final y evitar duplicados.
            // ==========================================================
            List<string> codigosLimpios = new List<string>();

            foreach (string codigo in codigos)
            {
                string limpio = new string(
                    codigo
                        .ToLowerInvariant()
                        .Where(c => c == letraEsperada || char.IsDigit(c) || c == '.' || c == '+')
                        .ToArray());

                if (!CodigoCifOcrPareceValido(limpio, letraEsperada))
                    continue;

                if (!codigosLimpios.Contains(limpio))
                    codigosLimpios.Add(limpio);
            }

            return IndexadorCudService.NormalizarCodigosCif(
                string.Join(" ", codigosLimpios));
        }

        private static bool CodigoCifOcrPareceValido(string codigo, char letraEsperada)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return false;

            codigo = codigo.ToLowerInvariant().Trim();

            if (!codigo.StartsWith(letraEsperada.ToString()))
                return false;

            string cuerpo = codigo.Substring(1);

            // Evita falsos positivos como d1, s1, b1, e1.
            if (Regex.IsMatch(cuerpo, @"^\d{1}$"))
                return false;

            // Acepta:
            // d240.23
            // d1160.23
            // d7202.23
            // d155.213
            // s110.888
            // b167.3+
            return Regex.IsMatch(
                codigo,
                @"^" + Regex.Escape(letraEsperada.ToString()) + @"\d{2,4}(?:\.\d{1,4})*(?:\+)?$");
        }



        private static string NormalizarSoloNumeroCifDesdeOcr(string texto)
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
            }

            return new string(chars);
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

            string dni11 = partes[2];
            string fechaEmision8 = partes[3];
            string fechaVencimiento8 = partes[4];

            string dni = dni11.TrimStart('0');

            if (dni.Length < 7 || dni.Length > 8)
            {
                r.Mensaje = "El DNI incluido en el número de certificado no tiene longitud válida.";
                return r;
            }

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
            r.Dni = dni;
            r.FechaEmision = fechaEmision;
            r.FechaVencimiento = fechaVencimiento;

            return r;
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
            texto = NormalizarCodigosCif(texto);

            if (string.IsNullOrWhiteSpace(texto))
            {
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

                if (c[0] != char.ToLowerInvariant(letraEsperada))
                {
                    return ResultadoValidacion.Error(
                        nombreCampo + " debe contener códigos que comiencen con '" +
                        char.ToLowerInvariant(letraEsperada) + "'. Código detectado: " + codigo);
                }

                if (!EsCodigoCifValidoPorLetra(c, letraEsperada))
                {
                    return ResultadoValidacion.Error(
                        "Código CIF inválido en " + nombreCampo + ": " + codigo);
                }
            }

            return ResultadoValidacion.Ok();
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
                    return Regex.IsMatch(codigo, @"^s\d{2,4}\.\d{1,4}$");

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