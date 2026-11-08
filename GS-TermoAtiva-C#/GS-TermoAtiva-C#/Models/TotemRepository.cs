using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace GS_TermoAtiva_C_.Models
{
    /// <summary>
    /// Repositório central de dados do Totem TermoAtiva.
    /// Implementa persistência local em arquivos JSON para garantir operação 100% autônoma e resiliente
    /// em cenários de emergência climática urbana ou queda de conectividade externa.
    /// </summary>
    public class TotemRepository
    {
        private readonly string _diretorioData;
        private readonly string _arquivoPontosJson;
        private readonly string _arquivoOcorrenciasJson;
        private readonly object _lockArquivo = new();

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        private List<PontoApoio> _pontosMemoria = new();
        private List<OcorrenciaMunicipe> _ocorrenciasMemoria = new();

        public TotemRepository()
        {
            _diretorioData = ResolverDiretorioData();
            _arquivoPontosJson = Path.Combine(_diretorioData, "pontos_apoio.json");
            _arquivoOcorrenciasJson = Path.Combine(_diretorioData, "ocorrencias.json");

            GarantirDiretorioECargaInicial();
        }

        /// <summary>
        /// Localiza o caminho de persistência da pasta Data/, seja em ambiente de desenvolvimento
        /// junto ao código-fonte ou no diretório base da aplicação compilada.
        /// </summary>
        private static string ResolverDiretorioData()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            DirectoryInfo? atual = new DirectoryInfo(baseDir);
            while (atual != null)
            {
                string csprojPath = Path.Combine(atual.FullName, "GS-TermoAtiva-C#.csproj");
                if (File.Exists(csprojPath))
                {
                    return Path.Combine(atual.FullName, "Data");
                }

                string subProjeto = Path.Combine(atual.FullName, "GS-TermoAtiva-C#", "GS-TermoAtiva-C#");
                if (Directory.Exists(subProjeto) && File.Exists(Path.Combine(subProjeto, "GS-TermoAtiva-C#.csproj")))
                {
                    return Path.Combine(subProjeto, "Data");
                }

                atual = atual.Parent;
            }

            return Path.Combine(baseDir, "Data");
        }

        /// <summary>
        /// Cria o diretório Data/ caso não exista e popula pontos_apoio.json com 5 pontos realistas se estiver vazio.
        /// </summary>
        private void GarantirDiretorioECargaInicial()
        {
            lock (_lockArquivo)
            {
                try
                {
                    if (!Directory.Exists(_diretorioData))
                    {
                        Directory.CreateDirectory(_diretorioData);
                    }

                    if (File.Exists(_arquivoPontosJson))
                    {
                        string json = File.ReadAllText(_arquivoPontosJson);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var carregados = JsonSerializer.Deserialize<List<PontoApoio>>(json, _jsonOptions);
                            if (carregados != null && carregados.Count > 0)
                            {
                                // Se o arquivo antigo não possuía os campos decompostos de CEP/Logradouro nos itens de seed, atualiza
                                bool precisaAtualizarSeed = carregados.All(p => string.IsNullOrWhiteSpace(p.CEP) && string.IsNullOrWhiteSpace(p.Logradouro));
                                if (precisaAtualizarSeed)
                                {
                                    _pontosMemoria = ObterSeedInicialPontos();
                                    SalvarPontosNoDisco();
                                }
                                else
                                {
                                    _pontosMemoria = carregados;
                                }
                            }
                        }
                    }

                    if (_pontosMemoria.Count == 0)
                    {
                        _pontosMemoria = ObterSeedInicialPontos();
                        SalvarPontosNoDisco();
                    }

                    if (File.Exists(_arquivoOcorrenciasJson))
                    {
                        string json = File.ReadAllText(_arquivoOcorrenciasJson);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var ocorrencias = JsonSerializer.Deserialize<List<OcorrenciaMunicipe>>(json, _jsonOptions);
                            if (ocorrencias != null)
                            {
                                _ocorrenciasMemoria = ocorrencias;
                            }
                        }
                    }
                    else
                    {
                        SalvarOcorrenciasNoDisco();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TotemRepository] Aviso na inicialização: {ex.Message}");
                    if (_pontosMemoria.Count == 0)
                    {
                        _pontosMemoria = ObterSeedInicialPontos();
                    }
                }
            }
        }

        /// <summary>
        /// Fornece carga inicial realista com 5 pontos urbanos estratégicos de alívio térmico com endereço estruturado.
        /// </summary>
        private static List<PontoApoio> ObterSeedInicialPontos()
        {
            return new List<PontoApoio>
            {
                new PontoApoio(
                    1,
                    "Bebedouro Público - Praça da Sé",
                    TipoPontoApoio.BebedouroPublico,
                    "01001-000",
                    "Praça da Sé",
                    "s/n",
                    "Sé",
                    "São Paulo",
                    "SP",
                    true,
                    120),
                new PontoApoio(
                    2,
                    "Refúgio Verde - Parque Trianon",
                    TipoPontoApoio.AreaSombraDescanso,
                    "01407-200",
                    "Avenida Paulista",
                    "1578",
                    "Bela Vista",
                    "São Paulo",
                    "SP",
                    true,
                    350),
                new PontoApoio(
                    3,
                    "Posto Climatizado - UBS Santa Cecília",
                    TipoPontoApoio.PostoSaudeClimatizado,
                    "01153-000",
                    "Rua Vitorino Carmilo",
                    "599",
                    "Santa Cecília",
                    "São Paulo",
                    "SP",
                    true,
                    580),
                new PontoApoio(
                    4,
                    "Bebedouro Público - Terminal Rodoviário Tietê",
                    TipoPontoApoio.BebedouroPublico,
                    "02021-001",
                    "Avenida Cruzeiro do Sul",
                    "1800",
                    "Santana",
                    "São Paulo",
                    "SP",
                    true,
                    850),
                new PontoApoio(
                    5,
                    "Área de Descanso Sombreada - Largo do Arouche",
                    TipoPontoApoio.AreaSombraDescanso,
                    "01219-010",
                    "Largo do Arouche",
                    "100",
                    "República",
                    "São Paulo",
                    "SP",
                    true,
                    420)
            };
        }

        private void SalvarPontosNoDisco()
        {
            string json = JsonSerializer.Serialize(_pontosMemoria, _jsonOptions);
            File.WriteAllText(_arquivoPontosJson, json);
        }

        private void SalvarOcorrenciasNoDisco()
        {
            string json = JsonSerializer.Serialize(_ocorrenciasMemoria, _jsonOptions);
            File.WriteAllText(_arquivoOcorrenciasJson, json);
        }

        private static PontoApoio ClonarPonto(PontoApoio p)
        {
            var clone = new PontoApoio(
                p.Id,
                p.Nome,
                p.Tipo,
                p.CEP,
                p.Logradouro,
                p.Numero,
                p.Bairro,
                p.Cidade,
                p.UF,
                p.Disponivel,
                p.DistanciaMetros);

            if (string.IsNullOrWhiteSpace(p.Logradouro) && !string.IsNullOrWhiteSpace(p.Endereco))
            {
                clone.Endereco = p.Endereco;
            }

            return clone;
        }

        // -------------------------------------------------------------
        // MÉTODOS PÚBLICOS DE CONSULTA E OPERAÇÃO (MUNÍCIPE E ADMIN)
        // -------------------------------------------------------------

        /// <summary>
        /// Retorna a lista completa de pontos de apoio cadastrados, ordenados por proximidade.
        /// </summary>
        public List<PontoApoio> ObterTodosPontos()
        {
            lock (_lockArquivo)
            {
                return _pontosMemoria
                    .OrderBy(p => p.DistanciaMetros)
                    .Select(ClonarPonto)
                    .ToList();
            }
        }

        /// <summary>
        /// Retorna pontos de apoio filtrados por categoria ou todos caso o filtro seja nulo.
        /// </summary>
        public List<PontoApoio> ObterPontosPorTipo(TipoPontoApoio? tipo)
        {
            lock (_lockArquivo)
            {
                var query = _pontosMemoria.AsEnumerable();
                if (tipo.HasValue)
                {
                    query = query.Where(p => p.Tipo == tipo.Value);
                }

                return query
                    .OrderBy(p => p.DistanciaMetros)
                    .Select(ClonarPonto)
                    .ToList();
            }
        }

        /// <summary>
        /// Cadastra um novo recurso de apoio gerando próximo ID sequencial e calculando distância automática.
        /// </summary>
        public PontoApoio CadastrarPontoApoio(PontoApoio novoPonto)
        {
            if (novoPonto == null) throw new ArgumentNullException(nameof(novoPonto));

            lock (_lockArquivo)
            {
                int proximoId = _pontosMemoria.Count > 0 ? _pontosMemoria.Max(p => p.Id) + 1 : 1;
                novoPonto.Id = proximoId;
                novoPonto.DistanciaMetros = novoPonto.CalcularDistanciaDoTotem();
                _pontosMemoria.Add(novoPonto);
                SalvarPontosNoDisco();
                return novoPonto;
            }
        }

        /// <summary>
        /// Alterna ou define a disponibilidade de um ponto de apoio. Exclusivo de administrador.
        /// </summary>
        public bool AlternarDisponibilidadePonto(int id, bool disponivel)
        {
            lock (_lockArquivo)
            {
                var ponto = _pontosMemoria.FirstOrDefault(p => p.Id == id);
                if (ponto != null)
                {
                    ponto.Disponivel = disponivel;
                    SalvarPontosNoDisco();
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Registra e persiste uma nova ocorrência de zeladoria aberta por munícipe ou trabalhador.
        /// </summary>
        public void RegistrarOcorrencia(OcorrenciaMunicipe ocorrencia)
        {
            if (ocorrencia == null) throw new ArgumentNullException(nameof(ocorrencia));

            lock (_lockArquivo)
            {
                _ocorrenciasMemoria.Insert(0, ocorrencia);
                SalvarOcorrenciasNoDisco();
            }
        }

        /// <summary>
        /// Retorna todas as ocorrências cadastradas para auditoria no painel administrativo.
        /// </summary>
        public List<OcorrenciaMunicipe> ObterTodasOcorrencias()
        {
            lock (_lockArquivo)
            {
                return _ocorrenciasMemoria
                    .OrderByDescending(o => o.DataHora)
                    .ToList();
            }
        }

        /// <summary>
        /// Retorna os dados climáticos atuais do totem simulando cache local de alta resiliência
        /// (Temperatura 36.5°C, Sensação Térmica 40.2°C, Umidade Relativa 28%).
        /// </summary>
        public AlertaClimatico ObterAlertaAtual()
        {
            return new AlertaClimatico
            {
                TemperaturaAtual = 36.5,
                SensacaoTermica = 40.2,
                UmidadeRelativa = 28,
                UltimaAtualizacao = DateTime.Now
            };
        }
    }
}
