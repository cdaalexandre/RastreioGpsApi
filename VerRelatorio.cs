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
                htmlBuilder.Append("<html><head><title>Relatório - Atividade de Extensão</title>"); // Título atualizado
                htmlBuilder.Append("<meta charset='UTF-8'>"); 
                htmlBuilder.Append("<meta http-equiv='refresh' content='30'>"); 
                
                // Estilo do subtítulo adicionado
                htmlBuilder.Append("<style>body { font-family: sans-serif; } table { border-collapse: collapse; }");
                htmlBuilder.Append("th, td { border: 1px solid #ddd; padding: 8px; }");
                htmlBuilder.Append("th { background-color: #f2f2f2; } h2 { color: #333; }");
                htmlBuilder.Append(".subtitle { color: #555; font-size: 0.9em; margin-top: -10px; border-bottom: 1px solid #eee; padding-bottom: 10px; }");
                htmlBuilder.Append("</style>");
                
                htmlBuilder.Append("</head><body>");
                htmlBuilder.Append($"<h1>Relatório de Histórico de Localização</h1>");

                // TEXTO ADICIONADO AQUI
                htmlBuilder.Append("<div class='subtitle'>");
                // htmlBuilder.Append("<b>Instituição:</b> Diretoria de Ensino Centro Oeste - SEDUC/SP<br>");
                htmlBuilder.Append("<b>Instituição:</b> Diretoria de Ensino Centro Oeste - SEDUC/SP<br>");

                htmlBuilder.Append("<b>Projeto:</b> Atividade de Extensão: Integração de Competências em Engenharia de Software III - Turma_001");
                htmlBuilder.Append("</div><br>"); // Adiciona o texto e uma quebra de linha

                htmlBuilder.Append($"<p>Atualizado em: {agoraBrasilia.ToString("dd/MM/yyyy HH:mm:ss")} (Horário de Brasília / Página atualiza a cada 30 segundos)</p>");

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