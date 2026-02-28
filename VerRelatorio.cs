using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System;
using System.Text; 
using System.Linq;
using System.Globalization; // NOVO: garante que latitude/longitude usem ponto decimal no JSON

namespace RastreioGpsApi
{
    public static class VerRelatorio
    {
        [FunctionName("VerRelatorio")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function 'VerRelatorio' processou um request.");

            try
            {
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var tableClient = new TableClient(connectionString, "Coordenadas");
                var todosRegistros = tableClient.Query<CoordenadaEntidade>().ToList();
                var agrupadosPorCelular = todosRegistros
                    .GroupBy(r => r.PartitionKey)
                    .OrderBy(g => g.Key);
                
                TimeZoneInfo fusoBrasilia = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                DateTimeOffset agoraBrasilia = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, fusoBrasilia);

                var htmlBuilder = new StringBuilder();

                // =====================================================================
                // CABEÇALHO HTML — inclui Leaflet.js (CSS e JS) via CDN gratuito
                // =====================================================================
                htmlBuilder.Append("<html><head><title>Relatório - Atividade de Extensão</title>");
                htmlBuilder.Append("<meta charset='UTF-8'>"); 
                htmlBuilder.Append("<meta http-equiv='refresh' content='30'>");

                // NOVO: CSS do Leaflet (biblioteca de mapas)
                htmlBuilder.Append("<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />");

                // Estilos da página
                htmlBuilder.Append("<style>");
                htmlBuilder.Append("body { font-family: sans-serif; margin: 20px; background-color: #f4f4f4; }");
                htmlBuilder.Append(".container { max-width: 960px; margin: 0 auto; background-color: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }");
                htmlBuilder.Append("table { border-collapse: collapse; width: 100%; }");
                htmlBuilder.Append("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                htmlBuilder.Append("th { background-color: #f2f2f2; }");
                htmlBuilder.Append("h1 { color: #333; }");
                htmlBuilder.Append("h2 { color: #333; margin-top: 30px; }");
                htmlBuilder.Append(".subtitle { color: #555; font-size: 0.9em; margin-top: -10px; border-bottom: 1px solid #eee; padding-bottom: 10px; }");

                // NOVO: estilo do mapa e da legenda
                htmlBuilder.Append("#mapa { height: 500px; width: 100%; border-radius: 8px; margin: 15px 0; border: 1px solid #ddd; }");
                htmlBuilder.Append(".legenda { display: flex; flex-wrap: wrap; gap: 10px; margin: 10px 0 20px 0; }");
                htmlBuilder.Append(".legenda-item { display: flex; align-items: center; gap: 5px; font-size: 0.85em; }");
                htmlBuilder.Append(".legenda-cor { width: 14px; height: 14px; border-radius: 50%; border: 1px solid #333; }");

                htmlBuilder.Append("</style>");
                htmlBuilder.Append("</head><body>");

                // =====================================================================
                // CORPO DA PÁGINA — título, subtítulo, mapa, tabelas
                // =====================================================================
                htmlBuilder.Append("<div class='container'>");
                htmlBuilder.Append("<h1>Relatório de Histórico de Localização</h1>");

                htmlBuilder.Append("<div class='subtitle'>");
                htmlBuilder.Append("<b>Instituição:</b> Diretoria de Ensino Centro Oeste - SEDUC/SP<br>");
                htmlBuilder.Append("<b>Projeto:</b> Atividade de Extensão: Integração de Competências em Engenharia de Software III - Turma_001");
                htmlBuilder.Append("</div>");

                htmlBuilder.Append($"<p>Atualizado em: {agoraBrasilia.ToString("dd/MM/yyyy HH:mm:ss")} (Horário de Brasília / Página atualiza a cada 30 segundos)</p>");

                // =====================================================================
                // NOVO: DIV DO MAPA — aqui o Leaflet vai renderizar o mapa interativo
                // =====================================================================
                htmlBuilder.Append("<h2>Mapa de Localização</h2>");
                htmlBuilder.Append("<div id='mapa'></div>");

                // NOVO: Legenda — mostra qual cor pertence a qual colaborador
                htmlBuilder.Append("<div class='legenda' id='legenda'></div>");

                // =====================================================================
                // NOVO: Monta os dados de coordenadas como JSON para o JavaScript
                // Analogia: estamos "empacotando" os dados da tabela Azure num formato
                // que o JavaScript do navegador consegue ler para desenhar no mapa
                // =====================================================================
                var mapDataBuilder = new StringBuilder();
                mapDataBuilder.Append("[");
                bool primeirGrupo = true;

                foreach (var grupo in agrupadosPorCelular)
                {
                    if (!primeirGrupo) mapDataBuilder.Append(",");
                    primeirGrupo = false;

                    // Escapa o número do celular para uso seguro em JSON
                    string celular = grupo.Key.Replace("\"", "\\\"");
                    mapDataBuilder.Append($"{{\"celular\":\"{celular}\",\"pontos\":[");

                    var pontos = grupo.OrderBy(g => g.RowKey).Take(10);
                    bool primeiroPonto = true;

                    foreach (var p in pontos)
                    {
                        if (!primeiroPonto) mapDataBuilder.Append(",");
                        primeiroPonto = false;

                        // Formata hora no fuso de Brasília
                        string horaFormatada = "N/A";
                        if (p.Timestamp.HasValue)
                        {
                            DateTimeOffset horaLocal = TimeZoneInfo.ConvertTime(p.Timestamp.Value, fusoBrasilia);
                            horaFormatada = horaLocal.ToString("dd/MM/yyyy HH:mm:ss");
                        }

                        // IMPORTANTE: usa InvariantCulture para garantir ponto decimal
                        // (no Brasil o padrão é vírgula, mas JSON exige ponto)
                        string latStr = Convert.ToDouble(p.Latitude).ToString(CultureInfo.InvariantCulture);
                        string lngStr = Convert.ToDouble(p.Longitude).ToString(CultureInfo.InvariantCulture);

                        mapDataBuilder.Append($"{{\"lat\":{latStr},\"lng\":{lngStr},\"hora\":\"{horaFormatada}\"}}");
                    }

                    mapDataBuilder.Append("]}");
                }
                mapDataBuilder.Append("]");

                // =====================================================================
                // TABELAS — mesma lógica da v1, sem alteração
                // =====================================================================
                foreach (var grupo in agrupadosPorCelular)
                {
                    string celular = grupo.Key;
                    htmlBuilder.Append($"<h2>Colaborador: {celular}</h2>");
                    
                    var historicoRecente = grupo.OrderBy(g => g.RowKey).Take(10);

                    if (historicoRecente.Any())
                    {
                        htmlBuilder.Append("<h3>Histórico Recente (Últimos 10 registros):</h3>");
                        htmlBuilder.Append("<table><tr><th>Horário (Brasília)</th><th>Latitude</th><th>Longitude</th></tr>");
                        
                        foreach (var registro in historicoRecente)
                        {
                            string horaFormatada = "N/A";
                            if (registro.Timestamp.HasValue)
                            {
                                DateTimeOffset horaLocal = TimeZoneInfo.ConvertTime(registro.Timestamp.Value, fusoBrasilia);
                                horaFormatada = horaLocal.ToString("dd/MM/yyyy HH:mm:ss");
                            }
                            htmlBuilder.Append($"<tr><td>{horaFormatada}</td><td>{registro.Latitude}</td><td>{registro.Longitude}</td></tr>");
                        }
                        
                        htmlBuilder.Append("</table>");
                    }
                    else
                    {
                        htmlBuilder.Append("<p>Nenhum registro encontrado para este colaborador.</p>");
                    }
                }

                // =====================================================================
                // NOVO: JavaScript do Leaflet — cria o mapa e plota os marcadores
                // =====================================================================
                htmlBuilder.Append("<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
                htmlBuilder.Append("<script>");

                // Injeta os dados JSON gerados pelo C# acima
                htmlBuilder.Append($"var dados = {mapDataBuilder.ToString()};");

                // Array de cores — cada colaborador recebe uma cor diferente
                htmlBuilder.Append("var cores = ['#e6194b','#3cb44b','#4363d8','#f58231','#911eb4','#42d4f4','#f032e6','#bfef45','#fabed4','#469990'];");

                // Cria o mapa centrado em São Paulo (ajusta automaticamente depois)
                htmlBuilder.Append("var mapa = L.map('mapa').setView([-23.55, -46.63], 12);");

                // Adiciona o "fundo" do mapa — os blocos de imagem do OpenStreetMap (gratuito)
                htmlBuilder.Append("L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {");
                htmlBuilder.Append("  attribution: '&copy; OpenStreetMap'");
                htmlBuilder.Append("}).addTo(mapa);");

                // Variável para calcular o zoom ideal (mostrar todos os pontos)
                htmlBuilder.Append("var limites = L.latLngBounds();");
                htmlBuilder.Append("var legendaEl = document.getElementById('legenda');");

                // Loop principal: para cada colaborador, plota seus pontos no mapa
                htmlBuilder.Append("dados.forEach(function(colab, i) {");
                htmlBuilder.Append("  var cor = cores[i % cores.length];");

                // Monta a legenda com a cor do colaborador
                htmlBuilder.Append("  legendaEl.innerHTML += \"<div class='legenda-item'><div class='legenda-cor' style='background:\" + cor + \"'></div>\" + colab.celular + \"</div>\";");

                // Para cada ponto do colaborador, cria um marcador circular no mapa
                htmlBuilder.Append("  colab.pontos.forEach(function(p, j) {");

                // O último ponto (mais recente) fica maior e mais destacado
                htmlBuilder.Append("    var ehUltimo = (j === colab.pontos.length - 1);");
                htmlBuilder.Append("    var marcador = L.circleMarker([p.lat, p.lng], {");
                htmlBuilder.Append("      radius: ehUltimo ? 10 : 6,");
                htmlBuilder.Append("      fillColor: cor,");
                htmlBuilder.Append("      color: '#333',");
                htmlBuilder.Append("      weight: ehUltimo ? 2 : 1,");
                htmlBuilder.Append("      opacity: 1,");
                htmlBuilder.Append("      fillOpacity: ehUltimo ? 0.9 : 0.5");
                htmlBuilder.Append("    }).addTo(mapa);");

                // Popup: ao clicar no ponto, mostra o celular e o horário
                htmlBuilder.Append("    marcador.bindPopup('<b>' + colab.celular + '</b><br>' + p.hora + (ehUltimo ? '<br><i>(mais recente)</i>' : ''));");

                // Expande os limites para incluir este ponto
                htmlBuilder.Append("    limites.extend([p.lat, p.lng]);");
                htmlBuilder.Append("  });");
                htmlBuilder.Append("});");

                // Ajusta o zoom para mostrar todos os marcadores (com margem de 10%)
                htmlBuilder.Append("if (limites.isValid()) { mapa.fitBounds(limites.pad(0.1)); }");

                htmlBuilder.Append("</script>");

                htmlBuilder.Append("</div>"); // fecha .container
                htmlBuilder.Append("</body></html>");

                return new ContentResult
                {
                    Content = htmlBuilder.ToString(),
                    ContentType = "text/html; charset=utf-8", 
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Erro ao gerar o relatório.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}