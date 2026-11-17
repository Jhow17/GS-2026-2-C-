TermoAtiva - Totem Urbano de Refúgio Climático (CitizenTech)

Sumário

Sobre o Projeto e Problema Social

Público-Alvo e Personas Beneficiadas

Funcionalidades do Sistema

Visão Pública (Munícipe)

Modo Administrador (Operador Municipal)

Arquitetura Orientada a Objetos (POO)

Resiliência Offline e Persistência Local JSON

Design System de Alto Contraste Solar (Tema.cs + ReaLTaiizor)

Estrutura de Pastas e Diretórios

Instruções de Compilação e Execução

Credenciais de Teste para Avaliação

Integrantes da Equipe FIAP

Sobre o Projeto e Problema Social

As ondas de calor extremo tornaram-se desastres climáticos cada vez mais frequentes nos grandes centros urbanos devido ao avanço das mudanças climáticas e à formação de ilhas de calor. Em dias com sensações térmicas superiores a 40°C, a exposição contínua ao ar livre provoca:

Choque térmico e exaustão por calor (heat exhaustion);

Insolação e desidratação aguda;

Pico de internações hospitalares e aumento da mortalidade de populações vulneráveis.

Embora existam alertas meteorológicos emitidos por aplicativos de smartphone, milhões de cidadãos vivem em exclusão digital: pessoas idosas sem familiaridade com celulares, trabalhadores braçais ao ar livre que não podem manusear aparelhos sob o sol, garis, entregadores e munícipes de baixa renda sem pacote de dados móveis ativo.

O TermoAtiva é uma solução de CitizenTech concebida como um totem físico interativo (quiosque urbano) instalado estrategicamente em vias públicas de grande circulação, praças, terminais de ônibus e centros comerciais. Ele atua como uma rede de proteção climática na cidade, oferecendo informação instantânea, geolocalização de pontos de alívio térmico gratuitos e zeladoria urbana participativa.

Público-Alvo e Personas Beneficiadas

Garis, Varredores e Trabalhadores da Limpeza Urbana: Profissionais expostos ao asfalto quente durante todo o expediente de trabalho, dependentes de bebedouros e áreas de sombra para hidratação frequente e pausas laborais;

Idosos e Aposentados: Cidadãos com menor capacidade termorreguladora natural, alta suscetibilidade à queda de pressão e desidratação, frequentemente desprovidos de smartphones;

Entregadores de Aplicativo e Ciclistas: Trabalhadores em deslocamento contínuo sob radiação solar que precisam reabastecer garrafas de água potável ao longo do dia;

Cidadãos em Situação de Vulnerabilidade / População em Trânsito: Pessoas sem acesso a ambientes com ar-condicionado ou água refrigerada em suas residências.

Funcionalidades do Sistema

O sistema é dividido estritamente em duas jornadas funcionais: a visão pública e acessível do munícipe e o painel restrito de gestão do operador municipal.

1. Visão Pública (Autoatendimento do Munícipe)

Monitoramento Climático em Tempo Real (Cabeçalho do Menu):

Exibição de Temperatura Atual, Sensação Térmica e Umidade Relativa do Ar em blocos de telemetria;

Indicador oficial dinâmico com classificação do risco térmico segundo protocolos da Defesa Civil e OMS;

Relógio digital com data e hora sincronizados.

Módulo 01 — Alerta de Calor Agora (AlertaForm):

Detalhamento térmico em tipografia clara e cores de advertência solar;

Lista de recomendações práticas de saúde (intervalos para hidratação obrigatória, pausa laboral e identificação imediata de sintomas de insolação);

Botão de toque de retorno ao menu.

Módulo 02 — Onde Beber Água / Descansar (PontosApoioForm):

Consulta pública SOMENTE LEITURA: o munícipe não corre risco de alterar acidentalmente dados no totem;

Filtros rápidos por toque no topo: Todos os Recursos, Bebedouros Públicos, Sombra e Descanso e Postos Climatizados;

Diferenciação Visual por Categoria:

Bebedouros Públicos: Destaque sutil em tons de Azul;

Áreas de Sombra / Descanso: Destaque sutil em tons de Verde;

Postos de Saúde Climatizados: Destaque sutil em tons de Laranja / Âmbar;

Tabela exibindo Nome do Local, Categoria, Endereço Completo, Distância Estimada e Situação Operacional (DISPONÍVEL ou EM MANUTENÇÃO).

Módulo 03 — Mapa de Refúgio e Recursos (MapaForm):

Renderização cartográfica interativa (projeção Web Mercator / WGS-84) integrada à API Weather Maps 1.0 da OpenWeather (`https://tile.openweathermap.org/map/{layer}/{z}/{x}/{y}.png?appid={API key}`), com seletor de camadas climáticas (`temp_new`, `precipitation_new`, `clouds_new`, `wind_new`, `pressure_new`), controles de zoom/arraste e pinos georreferenciados clicáveis carregados de `Data/pontos_apoio.json`.

Módulo 04 — Informar Problema em Bebedouro (OcorrenciaForm):

Seleção obrigatória em 2 toques (Local + Condição: Sem Água, Torneira Quebrada, Água Suja / Imprópria ou Local Fechado);

Campos estritamente opcionais para Nome do Cidadão e Descrição Adicional do Problema (Multiline), permitindo tanto relatos anônimos rápidos quanto relatos detalhados;

Gravação resiliente imediata no arquivo JSON de ocorrências.

2. Modo Administrador Restrito (Operador de Zeladoria)

Autenticação Segura com Persistência JSON e Teclado Virtual Sob Demanda (AdminAuthForm):

Modal com autenticação validada diretamente em `Data/operadores.json` (com registro automático de `UltimoAcesso`): Usuário `admin` (Senha `1234`) ou `zeladoria` (Senha `2026`);

Teclado Virtual Sob Demanda: oculto por padrão (Visible = false) para privilegiar a limpeza visual, podendo ser exibido/ocultado a qualquer momento através do botão [ Teclado Virtual ].

Painel Administrativo e Gestão de Recursos (AdminPontosForm):

Integração Assíncrona com API ViaCEP: ao digitar o CEP ou acionar [ Buscar CEP ], o sistema consome https://viacep.com.br/ws/{cep}/json/ via HttpClient e preenche automaticamente Logradouro/Rua, Bairro, Cidade e UF;

Cálculo Automático de Distância e Coordenadas WGS-84: a distância até o totem e as coordenadas geográficas (`Latitude` e `Longitude`) são calculadas dinamicamente pelo domínio com base no CEP/endereço;

CRUD Completo de Pontos de Apoio (`Data/pontos_apoio.json`): cadastro, edição de pontos existentes, alternância rápida entre DISPONÍVEL e INTERDITADO e exclusão com confirmação;

Auditoria e Resolução de Ocorrências (`Data/ocorrencias.json`): histórico das notificações enviadas pela população, com botões para alternar situação entre PENDENTE e ATENDIDO ou excluir registros improcedentes;

Gestão de Telemetria Climática (`Data/alerta_climatico.json`) e Operadores (`Data/operadores.json`): aba dedicada para atualizar temperatura/umidade (com cálculo automático do Índice de Calor NOAA ou sincronização com a OpenWeather) e cadastrar/ativar/inativar operadores municipais.

Arquitetura Orientada a Objetos (POO)

O projeto adota uma arquitetura em duas camadas claras e desacopladas (Models e Views):

GS_TermoAtiva_C_
├── Models/
│   ├── PontoApoio.cs (Entidade com endereço decomposto ViaCEP, coordenadas WGS-84 Latitude/Longitude e cálculo de distância)
│   ├── AlertaClimatico.cs (Cálculo de Índice de Calor Rothfusz/NOAA, classificação de risco térmico e recomendações)
│   ├── OcorrenciaMunicipe.cs (Entidade de zeladoria com suporte opcional a NomeMunicipe, DescricaoDetalhada e status Resolvido)
│   ├── OperadorMunicipal.cs (Entidade de credenciais e controle de acesso de operadores persistida em JSON)
│   ├── OpenWeatherMapService.cs (Consumo de tiles da Weather Maps 1.0 API da OpenWeather e projeção Web Mercator)
│   └── TotemRepository.cs (Persistência atômica JSON dos 4 arquivos, seed offline e isolamento de I/O)
└── Views/
    ├── Tema.cs (Design System: integração ReaLTaiizor, KioskCardPanel, KioskActionTile e cabeçalhos padronizados)
    ├── SplashForm.cs (Inicialização diagnóstica com HopeProgressBar)
    ├── MenuForm.cs (Dashboard público em grade 2x2 com telemetria climática)
    ├── AlertaForm.cs (Orientações de acolhimento térmico e hidratação)
    ├── PontosApoioForm.cs (Consulta pública com diferenciação cromática por categoria)
    ├── MapaForm.cs (Mapa interativo com tiles da OpenWeather Weather Maps 1.0 API, zoom/pan e pinos clicáveis)
    ├── OcorrenciaForm.cs (Relato ágil de falhas com campos opcionais de cidadão e descrição)
    ├── AdminAuthForm.cs (Modal de login validado em JSON com teclado virtual sob demanda)
    └── AdminPontosForm.cs (Painel administrativo com ViaCEP, CRUD de pontos, resolução de ocorrências, clima e operadores)


Resiliência Offline e Persistência Local JSON

O TermoAtiva utiliza o motor nativo System.Text.Json para persistir todos os dados em arquivos JSON no próprio projeto:

Data/pontos_apoio.json: Armazena os pontos de hidratação e refúgio com endereço estruturado e coordenadas geográficas (`Latitude` e `Longitude`).

Data/ocorrencias.json: Armazena o registro de falhas reportadas pelos munícipes e seu status de atendimento (`Resolvido`).

Data/operadores.json: Armazena os logins, cargos, status ativo/inativo e data/hora do último acesso dos operadores municipais.

Data/alerta_climatico.json: Armazena a telemetria climática atual do totem (`TemperaturaAtual`, `UmidadeRelativa`, `SensacaoTermica`), bairro de referência e a camada padrão do mapa OpenWeather (`CamadaMapaPadrao`).

Seed Inicial Automático: Caso os arquivos não existam no primeiro boot, o TotemRepository cria automaticamente a pasta Data/ e popula a base inicial completa.

Design System de Alto Contraste Solar (Tema.cs + ReaLTaiizor)

Biblioteca Visual ReaLTaiizor: Uso de botões arredondados (ParrotButton), barras de progresso (HopeProgressBar) e painéis customizados (KioskCardPanel e KioskActionTile) com renderização GDI+ anti-aliased.

Cabeçalhos Padronizados Sem Truncamento: Todos os formulários utilizam Tema.CriarCabecalhoPadrao(...) com dimensionamento estruturado em TableLayoutPanel, garantindo que nenhum título seja cortado em diferentes resoluções ou escalas de DPI.

Interface Tipográfica Pura (Zero Emojis): Todos os elementos visuais utilizam hierarquia tipográfica, indicadores numéricos e cores semânticas institucionais, sem uso de caracteres de emoji.

Estrutura de Pastas e Diretórios

C:/Users/Jonathan Silva/FIAP/GS/C#/
├── .env.example
├── .gitignore
├── integrantes.txt
├── README.md
└── GS-TermoAtiva-C#/
    ├── GS-TermoAtiva-C#.slnx
    └── GS-TermoAtiva-C#/
        ├── GS-TermoAtiva-C#.csproj
        ├── Program.cs
        ├── Data/
        │   ├── alerta_climatico.json
        │   ├── ocorrencias.json
        │   ├── operadores.json
        │   └── pontos_apoio.json
        ├── Models/
        │   ├── AlertaClimatico.cs
        │   ├── OcorrenciaMunicipe.cs
        │   ├── OpenWeatherMapService.cs
        │   ├── OperadorMunicipal.cs
        │   ├── PontoApoio.cs
        │   └── TotemRepository.cs
        └── Views/
            ├── Tema.cs
            ├── SplashForm.cs
            ├── MenuForm.cs
            ├── AlertaForm.cs
            ├── PontosApoioForm.cs
            ├── MapaForm.cs
            ├── OcorrenciaForm.cs
            ├── AdminAuthForm.cs
            └── AdminPontosForm.cs


Instruções de Compilação e Execução

# 1. Acessar a raiz do repositório
cd "C:\Users\Jonathan Silva\FIAP\GS\C#"

# 2. Compilar a Solution (0 erros, 0 avisos)
dotnet build "GS-TermoAtiva-C#/GS-TermoAtiva-C#.slnx"

# 3. Executar a aplicação do Totem
dotnet run --project "GS-TermoAtiva-C#/GS-TermoAtiva-C#/GS-TermoAtiva-C#.csproj"


Credenciais de Teste para Avaliação

Perfil

Mecanismo de Acesso

Usuário

Senha

Munícipe / Trabalhador

Acesso direto nos 4 módulos centrais do totem

Livre

Livre

Operador Municipal (Admin)

Botão [ Acesso Operador ] no rodapé

admin

1234

Integrantes da Equipe FIAP

Jonathan Henrique da Silva — RM: 561139

Musashi Nascimento Mabuti — RM: 559444

Marcos Vinicius Gonçalves Santos — RM: 560062

Felipe Gimenes — RM: 99799

Repositório Oficial no GitHub:

https://github.com/Jhow17/GS-2026-2-C-.git