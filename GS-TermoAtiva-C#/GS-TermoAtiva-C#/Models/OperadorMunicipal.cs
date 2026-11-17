using System;

namespace GS_TermoAtiva_C_.Models
{
    /// <summary>
    /// Representa uma credencial de operador municipal autorizada a acessar o painel administrativo do Totem.
    /// Persistida localmente em Data/operadores.json para eliminar credenciais estáticas hardcoded.
    /// </summary>
    public class OperadorMunicipal
    {
        public int Id { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
        public DateTime? UltimoAcesso { get; set; }

        public OperadorMunicipal()
        {
        }

        public OperadorMunicipal(
            int id,
            string usuario,
            string senha,
            string nomeCompleto,
            string cargo,
            bool ativo = true,
            DateTime? ultimoAcesso = null)
        {
            Id = id;
            Usuario = usuario?.Trim() ?? string.Empty;
            Senha = senha?.Trim() ?? string.Empty;
            NomeCompleto = nomeCompleto?.Trim() ?? string.Empty;
            Cargo = cargo?.Trim() ?? string.Empty;
            Ativo = ativo;
            UltimoAcesso = ultimoAcesso;
        }

        public bool ValidarCredenciais(string usuarioInformado, string senhaInformada)
        {
            if (!Ativo) return false;
            if (string.IsNullOrWhiteSpace(usuarioInformado) || string.IsNullOrWhiteSpace(senhaInformada)) return false;

            return string.Equals(Usuario, usuarioInformado.Trim(), StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Senha, senhaInformada.Trim(), StringComparison.Ordinal);
        }

        public override string ToString()
        {
            string status = Ativo ? "Ativo" : "Inativo";
            return $"{NomeCompleto} ({Usuario}) - {Cargo} [{status}]";
        }
    }
}
