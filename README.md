# 📍 RastreioGPS — Sistema de Rastreamento GPS Serverless na Plataforma Azure

**Atividade de Extensão: Integração de Competências em Engenharia de Software**
Cruzeiro do Sul Educacional — Bacharelado em Engenharia de Software

**Aluno:** Alexandre Calzetta
**Comunidade atendida:** Diretoria de Ensino Centro Oeste - SEDUC/SP

---

## O Problema

A Diretoria de Ensino Centro Oeste da SEDUC/SP gerencia 11 colaboradores que realizam visitas presenciais a escolas da rede estadual. Antes deste projeto, **não existia nenhuma ferramenta** para acompanhar os deslocamentos dessas equipes em campo. O controle era manual, baseado em relatos verbais, sem comprovação ou transparência.

Isso gerava dois problemas concretos: a gestão não conseguia otimizar rotas e distribuir melhor as visitas, e não havia como comprovar que o serviço público estava sendo executado.

## A Solução

Um sistema de rastreamento GPS em tempo real, 100% serverless, hospedado no Microsoft Azure com **custo zero** (Azure for Students). O colaborador acessa uma página web pelo celular, digita seu número e o sistema transmite sua localização a cada 10 segundos automaticamente. O gestor acompanha tudo por um relatório web com mapa interativo.

### Como funciona

```
┌─────────────────┐       POST /api/recebercoordenadas       ┌──────────────────────┐
│   📱 Celular    │ ──────────────────────────────────────▶   │  ⚡ Azure Functions  │
│   (index.html)  │   JSON: {Celular, Lat, Lng}              │  (ReceberCoordenadas) │
│   GPS a cada    │                                           │  Valida celular na    │
│   10 segundos   │                                           │  tabela de permitidos │
└─────────────────┘                                           └──────────┬───────────┘
                                                                         │
                                                                         ▼
┌─────────────────┐       GET /api/verrelatorio               ┌──────────────────────┐
│   🖥️ Gestor     │ ◀──────────────────────────────────────   │  📦 Azure Table      │
│   (Relatório)   │   HTML com mapa Leaflet.js                │     Storage           │
│   Mapa + Tabela │   Atualiza a cada 30s                     │  • Coordenadas        │
│   Auto-refresh  │                                           │  • Funcionarios       │
└─────────────────┘                                           │    Permitidos         │
                                                              └──────────────────────┘
```

### Recursos da plataforma Azure utilizados

| Recurso | Serviço Azure | SKU/Plano | Custo |
|---|---|---|---|
| API Backend | Azure Functions | Plano de Consumo (Y1) | Gratuito |
| Banco de Dados | Azure Table Storage | Standard LRS | Gratuito |
| Site do Colaborador | Static Website (Storage) | — | Gratuito |
| Monitoramento | Application Insights | — | Gratuito |

**Custo total da solução: R$ 0,00** — viável para qualquer instituição pública.

---

## Alinhamento com os Objetivos de Desenvolvimento Sustentável (ODS)

### 🎓 ODS 4 — Educação de Qualidade

O projeto é uma **ferramenta de gestão educacional**. A Diretoria de Ensino existe para dar suporte às escolas estaduais. Ao permitir que o gestor acompanhe em tempo real onde seus colaboradores estão, o sistema garante que as visitas de suporte às escolas sejam realizadas com eficiência. Escolas melhor assistidas resultam em educação de melhor qualidade.

A otimização dos deslocamentos também libera tempo para que os colaboradores possam atender mais escolas, ampliando o alcance do serviço público educacional.

### 🏛️ ODS 16 — Paz, Justiça e Instituições Eficazes

O sistema promove **transparência e eficiência na administração pública** de três formas:

1. **Transparência:** o relatório web é acessível e comprova a execução do serviço público, criando accountability
2. **Eficiência:** a visibilidade em tempo real permite otimizar rotas e redistribuir demandas entre os 11 colaboradores
3. **Respeito à privacidade:** o design é opt-in — o colaborador ativa o rastreio voluntariamente pelo celular, equilibrando gestão com direitos individuais

---

## Evolução do Projeto

### v1 — Extensão II (2º semestre 2025)

Branch: [`v1-extensao-ii-2025s2`](../../tree/v1-extensao-ii-2025s2)

Entrega da versão funcional mínima:
- API de recebimento de coordenadas com validação de celular (whitelist)
- Relatório HTML com tabela de histórico e auto-refresh a cada 30 segundos
- Site estático para o colaborador iniciar o rastreamento
- Provisionamento completo via Azure CLI (Infrastructure as Code)
- Depuração de CORS e erros de autorização 403

**Limitações mapeadas:** relatório sem proteção, coordenadas apenas em texto (sem mapa), sem consulta por período, sem conformidade com LGPD.

### v2 — Extensão III (1º semestre 2026)

Branch: [`v2-extensao-iii-2026s1`](../../tree/v2-extensao-iii-2026s1)

Trilha: **Inovação e Sustentabilidade**

Incremento implementado a partir das limitações da v1:

- **🗺️ Mapa interativo com Leaflet.js** — as coordenadas agora são exibidas visualmente em um mapa OpenStreetMap, com marcadores coloridos por colaborador, popup com detalhes ao clicar, legenda de cores e zoom automático. Custo: zero (biblioteca open-source via CDN)

---

## Estrutura do Repositório

```
RastreioGpsApi/
├── ReceberCoordenadas.cs   → API que recebe POST com {Celular, Latitude, Longitude}
├── VerRelatorio.cs         → Gera relatório HTML com mapa Leaflet + tabelas
├── index.html              → Site estático para o colaborador (captura GPS)
├── host.json               → Configuração do Azure Functions
├── local.settings.json     → Configurações locais (connection string)
├── RastreioGpsApi.csproj   → Projeto .NET 8
└── README.md               → Este arquivo
```

## URLs em Produção

| Componente | URL |
|---|---|
| Site do Colaborador | https://rastreiogpsstdbad1.z15.web.core.windows.net/ |
| Relatório com Mapa (Gestor) | https://rastreiogps-app-std-bad1.azurewebsites.net/api/verrelatorio |
| API de Coordenadas | https://rastreiogps-app-std-bad1.azurewebsites.net/api/recebercoordenadas |

## Tecnologias

- **Backend:** C# / .NET 8 / Azure Functions v4
- **Frontend:** HTML5 / JavaScript / CSS
- **Mapa:** Leaflet.js 1.9.4 + OpenStreetMap (open-source, custo zero)
- **Banco de Dados:** Azure Table Storage
- **Hospedagem:** Azure Static Website + Azure Functions (Plano de Consumo)
- **Infraestrutura:** Azure CLI (Infrastructure as Code)
- **Ambiente de desenvolvimento:** Linux Mint / VS Code / bash

## Contexto Acadêmico

| Item | Detalhe |
|---|---|
| Curso | Bacharelado em Engenharia de Software |
| Instituição | Cruzeiro do Sul Educacional (EaD) |
| Disciplina | Atividade de Extensão: Integração de Competências em Engenharia de Software III |
| Turma | Turma_001 |
| Semestre | 1º semestre 2026 |
| Trilha | Inovação e Sustentabilidade |
| Assinatura Azure | Azure for Students (institucional) |
| Região | Brazil South |

---

> *"A escolha de uma arquitetura Serverless não é só técnica, mas também econômica e ambiental. É possível inovar sem grandes recursos financeiros, apenas com o conhecimento acadêmico."*
