# TermoAtiva — Totem Urbano de Refúgio Climático (CitizenTech)

O **TermoAtiva** é um sistema de autoatendimento em **Windows Forms (.NET 8)** projetado para operar em totens físicos instalados em praças, terminais de transporte e centros comerciais. A solução conecta a população a pontos gratuitos de hidratação, descanso sombreado e atendimento climatizado durante ondas de calor extremo.

---

## 1. O Problema Escolhido

O avanço das mudanças climáticas e as **ilhas de calor urbano** tornaram as ondas de calor extremo (com sensação térmica acima de 40 °C) um risco grave de saúde pública nas grandes cidades, causando desidratação aguda, exaustão térmica e insolação.

Embora existam alertas em smartphones, parte expressiva das pessoas mais expostas ao calor enfrenta **exclusão digital ou restrição de acesso na rua**: não possui pacote de dados móveis, não tem familiaridade com aplicativos ou trabalha sob o sol sem conseguir manusear o celular continuamente. Falta, no espaço público, um ponto físico de fácil leitura que informe o risco térmico atual, indique onde beber água ou descansar perto dali e permita avisar quando um bebedouro público está quebrado.

---

## 2. Pessoas Reais Beneficiadas

- **Garis, varredores e trabalhadores da limpeza e manutenção urbana**: passam o expediente inteiro expostos ao asfalto quente e dependem de bebedouros públicos e áreas de sombra para pausas e hidratação.
- **Entregadores, ciclistas e trabalhadores ambulantes**: circulam sob radiação solar intensa e precisam localizar rapidamente pontos ativos para reabastecer água potável.
- **Idosos e aposentados**: grupo com maior vulnerabilidade fisiológica ao calor extremo e menor adesão a aplicativos móveis.
- **Cidadãos em trânsito e população em vulnerabilidade**: pessoas sem acesso imediato a água refrigerada ou ambientes climatizados.

---

## 3. O Que Foi Desenvolvido para Resolver o Problema

O sistema foi construído como uma aplicação desktop **Windows Forms em modo quiosque (tela cheia, alto contraste solar e botões amplos para toque)**, dividida em duas jornadas:

### Visão Pública — Autoatendimento do Munícipe (`MenuForm`)
- **Painel de Telemetria Climática no Topo**: mostra em tempo real a Temperatura Atual, a Umidade Relativa e a Sensação Térmica calculada pela **equação científica de Rothfusz / NOAA**, classificando o alerta em *Risco Moderado*, *Risco Alto* ou *Risco Extremo*.
- **Módulo 01 — Alerta de Calor Agora (`AlertaForm`)**: detalha as métricas térmicas locais e exibe diretrizes práticas de proteção à saúde (intervalos de hidratação, pausas recomendadas e sinais de insolação).
- **Módulo 02 — Onde Beber Água / Descansar (`PontosApoioForm`)**: listagem pública (somente leitura) ordenada por proximidade ao totem, com filtros rápidos e diferenciação por cor:
  - **Bebedouros Públicos** (azul);
  - **Áreas de Sombra e Descanso** (verde);
  - **Postos de Saúde Climatizados / UBS** (âmbar).
- **Módulo 03 — Mapa de Refúgio e Recursos (`MapaForm`)**: mapa interativo em projeção Web Mercator (WGS-84) integrado à **Weather Maps 1.0 API da OpenWeather** (`https://tile.openweathermap.org/map/{layer}/{z}/{x}/{y}.png?appid={API key}`), permitindo alternar camadas de **Temperatura (`temp_new`)**, **Precipitação (`precipitation_new`)**, **Nuvens (`clouds_new`)**, **Vento (`wind_new`)** e **Pressão (`pressure_new`)**, aplicar zoom/arraste e clicar nos pinos georreferenciados dos pontos de apoio.
- **Módulo 04 — Informar Problema em Bebedouro (`OcorrenciaForm`)**: canal de zeladoria participativa onde o cidadão seleciona o ponto de apoio e o defeito (*Sem Água*, *Torneira Quebrada*, *Água Suja / Imprópria* ou *Local Fechado*) em 2 toques, com campos opcionais para nome e observações (permitindo relatos anônimos).

### Visão Restrita — Painel do Operador Municipal (`AdminAuthForm` e `AdminPontosForm`)
- **Login Validado em JSON com Teclado Virtual (`AdminAuthForm`)**: autentica operadores cadastrados em `Data/operadores.json`, registra a data/hora do último acesso e oferece teclado virtual na tela sob demanda.
- **Gestão Completa de Pontos com ViaCEP**: ao informar o CEP, o sistema consulta a API **ViaCEP** (`https://viacep.com.br/ws/{cep}/json/`), preenche rua, bairro, cidade e UF automaticamente, estima coordenadas WGS-84 e calcula a distância até o totem. Permite cadastrar, editar, interditar/reativar e excluir pontos.
- **Auditoria de Ocorrências, Clima e Operadores**: permite marcar relatos de cidadãos como *Atendido* ou *Pendente*, atualizar a telemetria climática do totem (manualmente ou sincronizando com a OpenWeather) e gerenciar operadores autorizados.

### Persistência Local Resiliente em JSON (`TotemRepository`)
Para garantir funcionamento mesmo sem banco de dados externo ou durante oscilações de rede, todos os dados são persistidos automaticamente na pasta `Data/` com *seed* inicial no primeiro uso:
- `Data/pontos_apoio.json` — pontos de hidratação/refúgio com endereço e coordenadas (`Latitude` / `Longitude`);
- `Data/ocorrencias.json` — relatos de manutenção abertos pelos cidadãos e status de atendimento;
- `Data/operadores.json` — credenciais, cargos e registro de acesso dos operadores municipais;
- `Data/alerta_climatico.json` — leitura climática vigente e camada padrão do mapa.

---

## 4. Tecnologias Utilizadas

- **Linguagem e Plataforma**: C# 12 e **.NET 8.0 (`net8.0-windows`)**
- **Interface Gráfica**: **Windows Forms (WinForms)**, renderização customizada **GDI+** (`Tema.cs`) e biblioteca visual **ReaLTaiizor `3.8.2.1`**
- **Persistência de Dados**: arquivos JSON locais com **`System.Text.Json`** e controle de concorrência (`TotemRepository.cs`)
- **APIs e Serviços Integrados (`HttpClient`)**:
  - **OpenWeather Weather Maps 1.0 API** e **Current Weather API** (`OpenWeatherMapService.cs`)
  - **ViaCEP REST API** (autopreenchimento de endereços por CEP no painel administrativo)

---

## 5. Instruções de Execução (Sistema Windows Forms)

### Pré-requisitos
- **Windows 10 ou 11** com **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** instalado.
- *(Opcional)* Para carregar os tiles meteorológicos ao vivo da OpenWeather no módulo de Mapa, crie um arquivo `.env` na raiz do repositório (copiando o `.env.example`) com sua chave gratuita da OpenWeather:
  ```env
  OPENWEATHER_API_KEY=sua_chave_openweather_aqui
  ```
  > Caso o arquivo `.env` não seja configurado ou não haja internet, o mapa funciona normalmente em **modo resiliente offline**, exibindo a malha cartográfica local, a mancha térmica simulada e todos os pinos interativos.

### Passo a Passo no Terminal (PowerShell ou CMD)

```powershell
# 1. Acesse a pasta raiz do repositório
cd "C:\Users\Jonathan Silva\FIAP\GS\C#"

# 2. Compile a solução (.slnx)
dotnet build "GS-TermoAtiva-C#/GS-TermoAtiva-C#.slnx"

# 3. Execute o sistema Windows Forms
dotnet run --project "GS-TermoAtiva-C#/GS-TermoAtiva-C#/GS-TermoAtiva-C#.csproj"
```

### Credenciais de Acesso para Testes

| Perfil | Onde Acessar | Usuário | Senha |
|---|---|---|---|
| **Munícipe / Trabalhador** | Tela inicial (4 módulos públicos do totem) | *Acesso livre* | *Acesso livre* |
| **Operador Chefe (Admin)** | Botão `[ Acesso Operador ]` no rodapé do menu | `admin` | `1234` |
| **Equipe de Zeladoria** | Botão `[ Acesso Operador ]` no rodapé do menu | `zeladoria` | `2026` |

---

## 6. Equipe FIAP

| Nome Completo | RM |
|---|---|
| **Jonathan Henrique da Silva** | 561139 |
| **Musashi Nascimento Mabuti** | 559444 |
| **Marcos Vinicius Gonçalves Santos** | 560062 |
| **Felipe Gimenes** | 99799 |

**Repositório Oficial:** [https://github.com/Jhow17/GS-2026-2-C-](https://github.com/Jhow17/GS-2026-2-C-)
