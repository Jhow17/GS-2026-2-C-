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
    /// </summary>
    public class PontoApoio
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public TipoPontoApoio Tipo { get; set; }
        public string Endereco { get; set; } = string.Empty;
        public int DistanciaMetros { get; set; }
        public bool Disponivel { get; set; } = true;

        public PontoApoio()
        {
        }

        public PontoApoio(int id, string nome, TipoPontoApoio tipo, string endereco, int distanciaMetros, bool disponivel = true)
        {
            Id = id;
            Nome = nome;
            Tipo = tipo;
            Endereco = endereco;
            DistanciaMetros = distanciaMetros;
            Disponivel = disponivel;
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
