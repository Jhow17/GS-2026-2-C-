using System;
using System.Linq;

namespace GS_TermoAtiva_C_.Models
{
    /// <summary>
    /// Categorias de refúgio e alívio térmico urbano disponíveis para munícipes e trabalhadores.
    /// </summary>
    public enum TipoPontoApoio
    {
        BebedouroPublico = 1,
        AreaSombraDescanso = 2,
        PostoSaudeClimatizado = 3
    }

    /// <summary>
    /// Representa um recurso urbano de alívio térmico georreferenciado e monitorado pelo totem.
    /// Contém endereço estruturado compatível com ViaCEP, coordenadas geográficas (WGS-84)
    /// e cálculo automático de distância ao Totem Central.
    /// </summary>
    public class PontoApoio
    {
        // Referência fixa do Totem Central (Praça da Sé - Centro de São Paulo, CEP base 01001-000)
        private const int CepReferenciaTotem = 1001000;
        public const double LatitudeTotemCentral = -23.5505;
        public const double LongitudeTotemCentral = -46.6333;

        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public TipoPontoApoio Tipo { get; set; }

        // Endereço decomposto (integração ViaCEP)
        public string CEP { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string UF { get; set; } = string.Empty;

        // Coordenadas geográficas para plotagem no mapa com camadas da OpenWeather
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        private string _enderecoLegado = string.Empty;

        /// <summary>
        /// Retorna o endereço completo formatado a partir dos campos decompostos ou o valor legado.
        /// </summary>
        public string Endereco
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Logradouro))
                {
                    string num = string.IsNullOrWhiteSpace(Numero) ? "s/n" : Numero;
                    string bairroParte = !string.IsNullOrWhiteSpace(Bairro) ? $" - {Bairro}" : string.Empty;
                    string cidadeUf = !string.IsNullOrWhiteSpace(Cidade)
                        ? $", {Cidade}/{UF}"
                        : string.Empty;
                    return $"{Logradouro}, {num}{bairroParte}{cidadeUf}";
                }
                return _enderecoLegado;
            }
            set => _enderecoLegado = value ?? string.Empty;
        }

        private int _distanciaMetros;

        /// <summary>
        /// Distância estimada em metros em relação ao ponto fixo de instalação do Totem.
        /// Se não definida explicitamente, é calculada dinamicamente.
        /// </summary>
        public int DistanciaMetros
        {
            get => _distanciaMetros > 0 ? _distanciaMetros : CalcularDistanciaDoTotem();
            set => _distanciaMetros = value;
        }

        public bool Disponivel { get; set; } = true;

        public PontoApoio()
        {
        }

        public PontoApoio(
            int id,
            string nome,
            TipoPontoApoio tipo,
            string cep,
            string logradouro,
            string numero,
            string bairro,
            string cidade,
            string uf,
            bool disponivel = true,
            int? distanciaFixa = null,
            double? latitude = null,
            double? longitude = null)
        {
            Id = id;
            Nome = nome;
            Tipo = tipo;
            CEP = cep;
            Logradouro = logradouro;
            Numero = numero;
            Bairro = bairro;
            Cidade = cidade;
            UF = uf;
            Disponivel = disponivel;

            if (latitude.HasValue && longitude.HasValue && (Math.Abs(latitude.Value) > 0.0001 || Math.Abs(longitude.Value) > 0.0001))
            {
                Latitude = latitude.Value;
                Longitude = longitude.Value;
            }
            else
            {
                (Latitude, Longitude) = EstimarCoordenadasPorCep(cep, nome, logradouro, bairro);
            }

            _distanciaMetros = distanciaFixa ?? CalcularDistanciaDoTotem();
        }

        /// <summary>
        /// Garante que o ponto possua coordenadas geográficas válidas (WGS-84) para exibição no mapa.
        /// </summary>
        public void GarantirCoordenadasGeograficas()
        {
            if (Math.Abs(Latitude) < 0.0001 && Math.Abs(Longitude) < 0.0001)
            {
                (Latitude, Longitude) = EstimarCoordenadasPorCep(CEP, Nome, Logradouro, Bairro);
            }
        }

        /// <summary>
        /// Estima coordenadas geográficas determinísticas na malha urbana de São Paulo a partir do CEP/endereço.
        /// </summary>
        public static (double Latitude, double Longitude) EstimarCoordenadasPorCep(
            string? cep,
            string? nome = null,
            string? logradouro = null,
            string? bairro = null)
        {
            string digitos = new string((cep ?? string.Empty).Where(char.IsDigit).ToArray());
            int semente = 1001000;

            if (digitos.Length == 8 && int.TryParse(digitos, out int cepNum))
            {
                semente = cepNum;
            }
            else
            {
                string texto = $"{nome}{logradouro}{bairro}";
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    semente = Math.Abs(texto.GetHashCode());
                }
            }

            int delta = semente - CepReferenciaTotem;
            double angulo = ((Math.Abs(semente * 37) % 360) * Math.PI) / 180.0;
            double raioGraus = 0.0035 + ((Math.Abs(delta) % 2400) / 2400.0) * 0.022;

            double lat = Math.Round(LatitudeTotemCentral + (Math.Sin(angulo) * raioGraus), 5);
            double lon = Math.Round(LongitudeTotemCentral + (Math.Cos(angulo) * raioGraus), 5);
            return (lat, lon);
        }

        /// <summary>
        /// Calcula dinamicamente a distância em metros a partir do centro de referência do Totem
        /// com base no CEP/endereço ou coordenadas geográficas.
        /// </summary>
        public int CalcularDistanciaDoTotem()
        {
            string apenasDigitos = new string((CEP ?? string.Empty).Where(char.IsDigit).ToArray());
            if (apenasDigitos.Length == 8 && int.TryParse(apenasDigitos, out int cepNumerico))
            {
                int diferencaCep = Math.Abs(cepNumerico - CepReferenciaTotem);
                int estimativa = 90 + (diferencaCep % 3400);
                return (estimativa / 10) * 10;
            }

            if (Math.Abs(Latitude) > 0.0001 && Math.Abs(Longitude) > 0.0001)
            {
                double metros = CalcularHaversineMetros(LatitudeTotemCentral, LongitudeTotemCentral, Latitude, Longitude);
                return Math.Max(50, ((int)Math.Round(metros / 10.0)) * 10);
            }

            string chave = $"{Nome}{Logradouro}{Bairro}";
            if (!string.IsNullOrWhiteSpace(chave))
            {
                int hash = Math.Abs(chave.GetHashCode());
                return 120 + ((hash % 180) * 10);
            }

            return 250;
        }

        private static double CalcularHaversineMetros(double lat1, double lon1, double lat2, double lon2)
        {
            const double raioTerraMetros = 6371000.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;

            double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);
            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            return raioTerraMetros * c;
        }

        /// <summary>
        /// Retorna a descrição legível e amigável da tipologia do ponto de apoio para munícipes.
        /// </summary>
        public string ObterTipoFormatado()
        {
            return Tipo switch
            {
                TipoPontoApoio.BebedouroPublico => "Bebedouro Público",
                TipoPontoApoio.AreaSombraDescanso => "Área de Sombra / Descanso",
                TipoPontoApoio.PostoSaudeClimatizado => "Posto Climatizado",
                _ => "Ponto de Apoio"
            };
        }

        /// <summary>
        /// Retorna resumo legível para exibição rápida em listas, seletores e notificações do totem.
        /// </summary>
        public string FormatarExibicao()
        {
            string status = Disponivel ? "Disponível" : "Em Manutenção";
            return $"{Nome} ({ObterTipoFormatado()}) - {DistanciaMetros}m [{status}]";
        }

        public override string ToString()
        {
            return FormatarExibicao();
        }
    }
}
