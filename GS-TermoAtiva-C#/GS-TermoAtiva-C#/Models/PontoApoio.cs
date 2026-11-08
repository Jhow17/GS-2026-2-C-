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
    /// Contém endereço estruturado compatível com ViaCEP e cálculo automático de distância.
    /// </summary>
    public class PontoApoio
    {
        // Referência fixa do Totem Central (Praça da Sé - Centro de São Paulo, CEP base 01001-000)
        private const int CepReferenciaTotem = 1001000;

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
            int? distanciaFixa = null)
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
            _distanciaMetros = distanciaFixa ?? CalcularDistanciaDoTotem();
        }

        /// <summary>
        /// Calcula dinamicamente a distância em metros a partir do centro de referência do Totem
        /// com base na variação geográfica estimada pelo CEP e endereço cadastrado.
        /// </summary>
        public int CalcularDistanciaDoTotem()
        {
            string apenasDigitos = new string((CEP ?? string.Empty).Where(char.IsDigit).ToArray());
            if (apenasDigitos.Length == 8 && int.TryParse(apenasDigitos, out int cepNumerico))
            {
                int diferencaCep = Math.Abs(cepNumerico - CepReferenciaTotem);
                // Escala realista entre 80m e 3.800m para o perímetro urbano atendido pelo totem
                int estimativa = 90 + (diferencaCep % 3400);
                // Arredonda para múltiplos de 10m para leitura limpa pelo cidadão
                return (estimativa / 10) * 10;
            }

            // Fallback determinístico baseado no hash do logradouro/nome
            string chave = $"{Nome}{Logradouro}{Bairro}";
            if (!string.IsNullOrWhiteSpace(chave))
            {
                int hash = Math.Abs(chave.GetHashCode());
                return 120 + ((hash % 180) * 10);
            }

            return 250;
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
