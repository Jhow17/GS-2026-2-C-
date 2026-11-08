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

            // Busca diretório de projeto navegando para cima na hierarquia
            DirectoryInfo? atual = new DirectoryInfo(baseDir);
            while (atual != null)
            {
                // Verifica se este diretório contém o .csproj do projeto
                string csprojPath = Path.Combine(atual.FullName, "GS-TermoAtiva-C#.csproj");
                if (File.Exists(csprojPath))
                {
                    return Path.Combine(atual.FullName, "Data");
                }

                // Verifica se há subpasta com o projeto
                string subProjeto = Path.Combine(atual.FullName, "GS-TermoAtiva-C#", "GS-TermoAtiva-C#");
                if (Directory.Exists(subProjeto) && File.Exists(Path.Combine(subProjeto, "GS-TermoAtiva-C#.csproj")))
                {
                    return Path.Combine(subProjeto, "Data");
                }

                atual = atual.Parent;
            }

            // Fallback para pasta Data no diretório de execução atual
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

                    // Carrega ou gera seed para Pontos de Apoio
                    if (File.Exists(_arquivoPontosJson))
                    {
                        string json = File.ReadAllText(_arquivoPontosJson);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var carregados = JsonSerializer.Deserialize<List<PontoApoio>>(json, _jsonOptions);
                            if (carregados != null && carregados.Count > 0)
                            {
                                _pontosMemoria = carregados;
                            }
                        }
                    }

                    // Se continuou vazio ou o arquivo não existia, aplica Seed Inicial
                    if (_pontosMemoria.Count == 0)
                    {
                        _pontosMemoria = ObterSeedInicialPontos();
                        SalvarPontosNoDisco();
                    }

                    // Carrega ou inicializa Ocorrências
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
                    // Fallback em caso de erro de I/O para garantir que a interface do totem continue funcional
                    System.Diagnostics.Debug.WriteLine($"[TotemRepository] Aviso na inicialização: {ex.Message}");
                    if (_pontosMemoria.Count == 0)
                    {
                        _pontosMemoria = ObterSeedInicialPontos();
                    }
                }
            }
        }

        /// <summary>
        /// Fornece carga inicial realista com 5 pontos urbanos estratégicos de alívio térmico.
        /// </summary>
        private static List<PontoApoio> ObterSeedInicialPontos()
        {
            return new List<PontoApoio>
            {
                new PontoApoio(1, "Bebedouro Público - Praça da Sé", TipoPontoApoio.BebedouroPublico, "Praça da Sé, s/n - Centro Histórico", 120, true),
                new PontoApoio(2, "Refúgio Verde - Parque Trianon", TipoPontoApoio.AreaSombraDescanso, "Av. Paulista, 1578 - Bela Vista", 350, true),
                new PontoApoio(3, "Posto Climatizado - UBS Santa Cecília", TipoPontoApoio.PostoSaudeClimatizado, "R. Vitorino Carmilo, 599 - Santa Cecília", 580, true),
                new PontoApoio(4, "Bebedouro Público - Terminal Rodoviário Tietê", TipoPontoApoio.BebedouroPublico, "Av. Cruzeiro do Sul, 1800 - Santana", 850, true),
                new PontoApoio(5, "Área de Descanso Sombreada - Largo do Arouche", TipoPontoApoio.AreaSombraDescanso, "Largo do Arouche, Centro", 420, true)
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
                    .Select(p => new PontoApoio(p.Id, p.Nome, p.Tipo, p.Endereco, p.DistanciaMetros, p.Disponivel))
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
                    .Select(p => new PontoApoio(p.Id, p.Nome, p.Tipo, p.Endereco, p.DistanciaMetros, p.Disponivel))
                    .ToList();
            }
        }

        /// <summary>
        /// Cadastra um novo recurso de apoio gerando próximo ID sequencial. Exclusivo de administrador.
        /// </summary>
        public PontoApoio CadastrarPontoApoio(PontoApoio novoPonto)
        {
            if (novoPonto == null) throw new ArgumentNullException(nameof(novoPonto));

            lock (_lockArquivo)
            {
                int proximoId = _pontosMemoria.Count > 0 ? _pontosMemoria.Max(p => p.Id) + 1 : 1;
                novoPonto.Id = proximoId;
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
                _ocorrenciasMemoria.Insert(0, ocorrencia); // Mais recentes primeiro
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
