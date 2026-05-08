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
            @"^[bsde]\d+(\.\+?\d+)?$",
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

                // Correcciones típicas OCR:
                p = p.Replace("B", "b")
                     .Replace("S", "s")
                     .Replace("D", "d")
                     .Replace("E", "e");

                normalizados.Add(p);
            }

            return string.Join(" ", normalizados);
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

                if (!RegexCodigoCif.IsMatch(c))
                    return ResultadoValidacion.Error("Código CIF inválido en " + nombreCampo + ": " + codigo);

                if (c[0] != char.ToLowerInvariant(letraEsperada))
                {
                    return ResultadoValidacion.Error(
                        nombreCampo + " debe contener códigos que comiencen con '" +
                        char.ToLowerInvariant(letraEsperada) + "'. Código detectado: " + codigo);
                }
            }

            return ResultadoValidacion.Ok();
        }

        private static bool DigitoVerificadorCuilValido(string cuil)
        {
            if (string.IsNullOrWhiteSpace(cuil) || cuil.Length != 11 || !cuil.All(char.IsDigit))
                return false;

            int[] pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };

            int suma = 0;

            for (int i = 0; i < 10; i++)
                suma += int.Parse(cuil[i].ToString()) * pesos[i];

            int resto = suma % 11;
            int verificador = 11 - resto;

            if (verificador == 11)
                verificador = 0;
            else if (verificador == 10)
                verificador = 9;

            int digito = int.Parse(cuil[10].ToString());

            return digito == verificador;
        }
    }
}