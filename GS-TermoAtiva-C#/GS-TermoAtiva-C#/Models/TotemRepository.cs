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
    /// Implementa persistência local integral em arquivos JSON (pontos_apoio.json, ocorrencias.json,
    /// operadores.json e alerta_climatico.json) para garantir operação 100% autônoma e resiliente.
    /// </summary>
    public class TotemRepository
    {
        private readonly string _diretorioData;
        private readonly string _arquivoPontosJson;
        private readonly string _arquivoOcorrenciasJson;
        private readonly string _arquivoOperadoresJson;
        private readonly string _arquivoAlertaJson;
        private readonly object _lockArquivo = new();

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        private List<PontoApoio> _pontosMemoria = new();
        private List<OcorrenciaMunicipe> _ocorrenciasMemoria = new();
        private List<OperadorMunicipal> _operadoresMemoria = new();
        private AlertaClimatico _alertaMemoria = null!;

        public TotemRepository()
        {
            _diretorioData = ResolverDiretorioData();
            _arquivoPontosJson = Path.Combine(_diretorioData, "pontos_apoio.json");
            _arquivoOcorrenciasJson = Path.Combine(_diretorioData, "ocorrencias.json");
            _arquivoOperadoresJson = Path.Combine(_diretorioData, "operadores.json");
            _arquivoAlertaJson = Path.Combine(_diretorioData, "alerta_climatico.json");

            GarantirDiretorioECargaInicial();
        }

        /// <summary>
        /// Localiza o caminho de persistência da pasta Data/, seja em ambiente de desenvolvimento
        /// junto ao código-fonte ou no diretório base da aplicação compilada.
        /// </summary>
        public static string ResolverDiretorioData()
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
        /// Cria o diretório Data/ caso não exista e carrega/inicializa os 4 arquivos JSON de persistência.
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

                    CarregarOuInicializarPontos();
                    CarregarOuInicializarOcorrencias();
                    CarregarOuInicializarOperadores();
                    CarregarOuInicializarAlertaClimatico();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TotemRepository] Aviso na inicialização: {ex.Message}");
                    if (_pontosMemoria.Count == 0) _pontosMemoria = ObterSeedInicialPontos();
                    if (_operadoresMemoria.Count == 0) _operadoresMemoria = ObterSeedInicialOperadores();
                    _alertaMemoria ??= ObterSeedInicialAlerta();
                }
            }
        }

        private void CarregarOuInicializarPontos()
        {
            if (File.Exists(_arquivoPontosJson))
            {
                string json = File.ReadAllText(_arquivoPontosJson);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var carregados = JsonSerializer.Deserialize<List<PontoApoio>>(json, _jsonOptions);
                    if (carregados != null && carregados.Count > 0)
                    {
                        bool precisaAtualizarSeed = carregados.All(p => string.IsNullOrWhiteSpace(p.CEP) && string.IsNullOrWhiteSpace(p.Logradouro));
                        if (precisaAtualizarSeed)
                        {
                            _pontosMemoria = ObterSeedInicialPontos();
                            SalvarPontosNoDisco();
                        }
                        else
                        {
                            bool ajustouCoordenadas = false;
                            var seedCoords = ObterSeedInicialPontos().ToDictionary(s => s.Id, s => (s.Latitude, s.Longitude));
                            foreach (var p in carregados)
                            {
                                if (Math.Abs(p.Latitude) < 0.0001 && Math.Abs(p.Longitude) < 0.0001)
                                {
                                    if (seedCoords.TryGetValue(p.Id, out var coord))
                                    {
                                        p.Latitude = coord.Latitude;
                                        p.Longitude = coord.Longitude;
                                    }
                                    else
                                    {
                                        p.GarantirCoordenadasGeograficas();
                                    }
                                    ajustouCoordenadas = true;
                                }
                            }

                            _pontosMemoria = carregados;
                            if (ajustouCoordenadas)
                            {
                                SalvarPontosNoDisco();
                            }
                        }
                    }
                }
            }

            if (_pontosMemoria.Count == 0)
            {
                _pontosMemoria = ObterSeedInicialPontos();
                SalvarPontosNoDisco();
            }
        }

        private void CarregarOuInicializarOcorrencias()
        {
            if (File.Exists(_arquivoOcorrenciasJson))
            {
                string json = File.ReadAllText(_arquivoOcorrenciasJson);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var ocorrencias = JsonSerializer.Deserialize<List<OcorrenciaMunicipe>>(json, _jsonOptions);
                    if (ocorrencias != null)
                    {
                        _ocorrenciasMemoria = ocorrencias;
                        return;
                    }
                }
            }

            SalvarOcorrenciasNoDisco();
        }

        private void CarregarOuInicializarOperadores()
        {
            if (File.Exists(_arquivoOperadoresJson))
            {
                string json = File.ReadAllText(_arquivoOperadoresJson);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var operadores = JsonSerializer.Deserialize<List<OperadorMunicipal>>(json, _jsonOptions);
                    if (operadores != null && operadores.Count > 0)
                    {
                        _operadoresMemoria = operadores;
                        return;
                    }
                }
            }

            _operadoresMemoria = ObterSeedInicialOperadores();
            SalvarOperadoresNoDisco();
        }

        private void CarregarOuInicializarAlertaClimatico()
        {
            if (File.Exists(_arquivoAlertaJson))
            {
                string json = File.ReadAllText(_arquivoAlertaJson);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var alerta = JsonSerializer.Deserialize<AlertaClimatico>(json, _jsonOptions);
                    if (alerta != null && alerta.TemperaturaAtual > -30)
                    {
                        _alertaMemoria = alerta;
                        return;
                    }
                }
            }

            _alertaMemoria = ObterSeedInicialAlerta();
            SalvarAlertaNoDisco();
        }

        /// <summary>
        /// Fornece carga inicial realista com 5 pontos urbanos estratégicos de alívio térmico com endereço e coordenadas WGS-84.
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
                    120,
                    -23.5503,
                    -46.6339),
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
                    350,
                    -23.5614,
                    -46.6559),
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
                    580,
                    -23.5328,
                    -46.6518),
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
                    850,
                    -23.5162,
                    -46.6250),
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
                    420,
                    -23.5419,
                    -46.6448)
            };
        }

        private static List<OperadorMunicipal> ObterSeedInicialOperadores()
        {
            return new List<OperadorMunicipal>
            {
                new OperadorMunicipal(
                    1,
                    "admin",
                    "1234",
                    "Coordenação de Zeladoria Urbana",
                    "Operador Chefe Municipal",
                    true,
                    DateTime.Now),
                new OperadorMunicipal(
                    2,
                    "zeladoria",
                    "2026",
                    "Equipe Técnica de Manutenção Hidráulica",
                    "Agente de Campo - Defesa Civil",
                    true,
                    null)
            };
        }

        private static AlertaClimatico ObterSeedInicialAlerta()
        {
            return new AlertaClimatico(
                temperaturaAtual: 36.5,
                umidadeRelativa: 48,
                sensacaoTermica: 40.2,
                ultimaAtualizacao: DateTime.Now,
                bairroReferencia: "Sé - Centro",
                camadaMapaPadrao: "temp_new");
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

        private void SalvarOperadoresNoDisco()
        {
            string json = JsonSerializer.Serialize(_operadoresMemoria, _jsonOptions);
            File.WriteAllText(_arquivoOperadoresJson, json);
        }

        private void SalvarAlertaNoDisco()
        {
            string json = JsonSerializer.Serialize(_alertaMemoria, _jsonOptions);
            File.WriteAllText(_arquivoAlertaJson, json);
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
                p.DistanciaMetros,
                p.Latitude,
                p.Longitude);

            if (string.IsNullOrWhiteSpace(p.Logradouro) && !string.IsNullOrWhiteSpace(p.Endereco))
            {
                clone.Endereco = p.Endereco;
            }

            return clone;
        }

        private static OperadorMunicipal ClonarOperador(OperadorMunicipal op)
        {
            return new OperadorMunicipal(
                op.Id,
                op.Usuario,
                op.Senha,
                op.NomeCompleto,
                op.Cargo,
                op.Ativo,
                op.UltimoAcesso);
        }

        // -------------------------------------------------------------
        // PONTOS DE APOIO (Data/pontos_apoio.json)
        // -------------------------------------------------------------

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

        public PontoApoio? ObterPontoPorId(int id)
        {
            lock (_lockArquivo)
            {
                var ponto = _pontosMemoria.FirstOrDefault(p => p.Id == id);
                return ponto != null ? ClonarPonto(ponto) : null;
            }
        }

        public PontoApoio CadastrarPontoApoio(PontoApoio novoPonto)
        {
            if (novoPonto == null) throw new ArgumentNullException(nameof(novoPonto));

            lock (_lockArquivo)
            {
                int proximoId = _pontosMemoria.Count > 0 ? _pontosMemoria.Max(p => p.Id) + 1 : 1;
                novoPonto.Id = proximoId;
                novoPonto.GarantirCoordenadasGeograficas();
                novoPonto.DistanciaMetros = novoPonto.CalcularDistanciaDoTotem();
                _pontosMemoria.Add(novoPonto);
                SalvarPontosNoDisco();
                return ClonarPonto(novoPonto);
            }
        }

        public bool AtualizarPontoApoio(PontoApoio pontoAtualizado)
        {
            if (pontoAtualizado == null) throw new ArgumentNullException(nameof(pontoAtualizado));

            lock (_lockArquivo)
            {
                var existente = _pontosMemoria.FirstOrDefault(p => p.Id == pontoAtualizado.Id);
                if (existente == null) return false;

                bool cepMudou = !string.Equals(existente.CEP, pontoAtualizado.CEP, StringComparison.OrdinalIgnoreCase);

                existente.Nome = pontoAtualizado.Nome;
                existente.Tipo = pontoAtualizado.Tipo;
                existente.CEP = pontoAtualizado.CEP;
                existente.Logradouro = pontoAtualizado.Logradouro;
                existente.Numero = pontoAtualizado.Numero;
                existente.Bairro = pontoAtualizado.Bairro;
                existente.Cidade = pontoAtualizado.Cidade;
                existente.UF = pontoAtualizado.UF;
                existente.Disponivel = pontoAtualizado.Disponivel;

                if (cepMudou || (Math.Abs(existente.Latitude) < 0.0001 && Math.Abs(existente.Longitude) < 0.0001))
                {
                    (existente.Latitude, existente.Longitude) = PontoApoio.EstimarCoordenadasPorCep(
                        existente.CEP,
                        existente.Nome,
                        existente.Logradouro,
                        existente.Bairro);
                }

                existente.DistanciaMetros = existente.CalcularDistanciaDoTotem();
                SalvarPontosNoDisco();
                return true;
            }
        }

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

        public bool RemoverPontoApoio(int id)
        {
            lock (_lockArquivo)
            {
                var ponto = _pontosMemoria.FirstOrDefault(p => p.Id == id);
                if (ponto == null) return false;

                _pontosMemoria.Remove(ponto);
                SalvarPontosNoDisco();
                return true;
            }
        }

        // -------------------------------------------------------------
        // OCORRÊNCIAS DE ZELADORIA (Data/ocorrencias.json)
        // -------------------------------------------------------------

        public void RegistrarOcorrencia(OcorrenciaMunicipe ocorrencia)
        {
            if (ocorrencia == null) throw new ArgumentNullException(nameof(ocorrencia));

            lock (_lockArquivo)
            {
                _ocorrenciasMemoria.Insert(0, ocorrencia);
                SalvarOcorrenciasNoDisco();
            }
        }

        public List<OcorrenciaMunicipe> ObterTodasOcorrencias()
        {
            lock (_lockArquivo)
            {
                return _ocorrenciasMemoria
                    .OrderByDescending(o => o.DataHora)
                    .ToList();
            }
        }

        public bool AlternarStatusOcorrencia(Guid id, bool resolvido)
        {
            lock (_lockArquivo)
            {
                var ocorrencia = _ocorrenciasMemoria.FirstOrDefault(o => o.Id == id);
                if (ocorrencia == null) return false;

                ocorrencia.Resolvido = resolvido;
                SalvarOcorrenciasNoDisco();
                return true;
            }
        }

        public bool RemoverOcorrencia(Guid id)
        {
            lock (_lockArquivo)
            {
                var ocorrencia = _ocorrenciasMemoria.FirstOrDefault(o => o.Id == id);
                if (ocorrencia == null) return false;

                _ocorrenciasMemoria.Remove(ocorrencia);
                SalvarOcorrenciasNoDisco();
                return true;
            }
        }

        // -------------------------------------------------------------
        // OPERADORES E AUTENTICAÇÃO (Data/operadores.json)
        // -------------------------------------------------------------

        public OperadorMunicipal? AutenticarOperador(string usuarioInformado, string senhaInformada)
        {
            lock (_lockArquivo)
            {
                var operador = _operadoresMemoria.FirstOrDefault(op => op.ValidarCredenciais(usuarioInformado, senhaInformada));
                if (operador == null) return null;

                operador.UltimoAcesso = DateTime.Now;
                SalvarOperadoresNoDisco();
                return ClonarOperador(operador);
            }
        }

        public List<OperadorMunicipal> ObterTodosOperadores()
        {
            lock (_lockArquivo)
            {
                return _operadoresMemoria
                    .OrderBy(op => op.Id)
                    .Select(ClonarOperador)
                    .ToList();
            }
        }

        public OperadorMunicipal CadastrarOperador(OperadorMunicipal novoOperador)
        {
            if (novoOperador == null) throw new ArgumentNullException(nameof(novoOperador));

            lock (_lockArquivo)
            {
                if (_operadoresMemoria.Any(op => string.Equals(op.Usuario, novoOperador.Usuario, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Já existe um operador cadastrado com o usuário '{novoOperador.Usuario}'.");
                }

                int proximoId = _operadoresMemoria.Count > 0 ? _operadoresMemoria.Max(op => op.Id) + 1 : 1;
                novoOperador.Id = proximoId;
                _operadoresMemoria.Add(novoOperador);
                SalvarOperadoresNoDisco();
                return ClonarOperador(novoOperador);
            }
        }

        public bool AlternarStatusOperador(int id, bool ativo)
        {
            lock (_lockArquivo)
            {
                var operador = _operadoresMemoria.FirstOrDefault(op => op.Id == id);
                if (operador == null) return false;

                if (!ativo && _operadoresMemoria.Count(op => op.Ativo && op.Id != id) == 0)
                {
                    throw new InvalidOperationException("Não é permitido desativar o único operador ativo do terminal.");
                }

                operador.Ativo = ativo;
                SalvarOperadoresNoDisco();
                return true;
            }
        }

        // -------------------------------------------------------------
        // ALERTA CLIMÁTICO E TELEMETRIA (Data/alerta_climatico.json)
        // -------------------------------------------------------------

        public AlertaClimatico ObterAlertaAtual()
        {
            lock (_lockArquivo)
            {
                _alertaMemoria ??= ObterSeedInicialAlerta();
                return new AlertaClimatico(
                    _alertaMemoria.TemperaturaAtual,
                    _alertaMemoria.UmidadeRelativa,
                    _alertaMemoria.SensacaoTermica,
                    _alertaMemoria.UltimaAtualizacao,
                    _alertaMemoria.BairroReferencia,
                    _alertaMemoria.CamadaMapaPadrao);
            }
        }

        public AlertaClimatico AtualizarAlertaClimatico(
            double temperaturaAtual,
            int umidadeRelativa,
            double? sensacaoTermicaManual = null,
            string? bairroReferencia = null,
            string? camadaMapaPadrao = null)
        {
            lock (_lockArquivo)
            {
                string bairro = string.IsNullOrWhiteSpace(bairroReferencia)
                    ? (_alertaMemoria?.BairroReferencia ?? "Sé - Centro")
                    : bairroReferencia.Trim();

                string camada = string.IsNullOrWhiteSpace(camadaMapaPadrao)
                    ? (_alertaMemoria?.CamadaMapaPadrao ?? "temp_new")
                    : camadaMapaPadrao.Trim();

                double sensacao = sensacaoTermicaManual ?? AlertaClimatico.CalcularIndiceCalor(temperaturaAtual, umidadeRelativa);

                _alertaMemoria = new AlertaClimatico(
                    temperaturaAtual,
                    umidadeRelativa,
                    sensacao,
                    DateTime.Now,
                    bairro,
                    camada);

                SalvarAlertaNoDisco();
                return ObterAlertaAtual();
            }
        }

        public void SalvarCamadaMapaPadrao(string camadaMapa)
        {
            if (string.IsNullOrWhiteSpace(camadaMapa)) return;

            lock (_lockArquivo)
            {
                _alertaMemoria ??= ObterSeedInicialAlerta();
                _alertaMemoria.CamadaMapaPadrao = camadaMapa.Trim();
                SalvarAlertaNoDisco();
            }
        }
    }
}
