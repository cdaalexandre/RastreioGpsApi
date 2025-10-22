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
                // 1. Conecta na tabela "Coordenadas"
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var tableClient = new TableClient(connectionString, "Coordenadas");

                // 2. Busca TODOS os registros da tabela.
                var todosRegistros = tableClient.Query<CoordenadaEntidade>().ToList();

                // 3. Agrupa os registros por funcionário (PartitionKey)
                var agrupadosPorCelular = todosRegistros
                    .GroupBy(r => r.PartitionKey)
                    .OrderBy(g => g.Key); // Ordena pelo número do celular

                
                // NOVO: Pega o fuso horário de Brasília/SP
                TimeZoneInfo fusoBrasilia = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                // NOVO: Calcula a hora atual nesse fuso
                DateTimeOffset agoraBrasilia = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, fusoBrasilia);


                // 4. Constrói o HTML
                var htmlBuilder = new StringBuilder();
                htmlBuilder.Append("<html><head><title>Relatório de Localização</title>");
                htmlBuilder.Append("<meta charset='UTF-8'>"); 
                htmlBuilder.Append("<meta http-equiv='refresh' content='30'>"); 
                htmlBuilder.Append("<style>body { font-family: sans-serif; } table { border-collapse: collapse; }");
                htmlBuilder.Append("th, td { border: 1px solid #ddd; padding: 8px; }");
                htmlBuilder.Append("th { background-color: #f2f2f2; } h2 { color: #333; }</style>");
                htmlBuilder.Append("</head><body>");
                htmlBuilder.Append($"<h1>Relatório de Histórico de Localização</h1>");
                
                // ATUALIZADO: Mostra a hora de Brasília
                htmlBuilder.Append($"<p>Atualizado em: {agoraBrasilia.ToString("dd/MM/yyyy HH:mm:ss")} (Horário de Brasília / Página atualiza a cada 30 segundos)</p>");

                foreach (var grupo in agrupadosPorCelular)
                {
                    string celular = grupo.Key;
                    htmlBuilder.Append($"<h2>Funcionário: {celular}</h2>");
                    
                    var historicoRecente = grupo.OrderBy(g => g.RowKey).Take(10);

                    if (historicoRecente.Any())
                    {
                        htmlBuilder.Append("<h3>Histórico Recente (Últimos 10 registros):</h3>");
                        // ATUALIZADO: Muda o título da coluna
                        htmlBuilder.Append("<table><tr><th>Horário (Brasília)</th><th>Latitude</th><th>Longitude</th></tr>");
                        
                        foreach (var registro in historicoRecente)
                        {
                            // ATUALIZADO: Converte a hora de cada registro
                            string horaFormatada = "N/A";
                            if (registro.Timestamp.HasValue)
                            {
                                // Converte o Timestamp (que é UTC) para o fuso de Brasília
                                DateTimeOffset horaLocal = TimeZoneInfo.ConvertTime(registro.Timestamp.Value, fusoBrasilia);
                                // Formata
                                horaFormatada = horaLocal.ToString("dd/MM/yyyy HH:mm:ss");
                            }
                            htmlBuilder.Append($"<tr><td>{horaFormatada}</td><td>{registro.Latitude}</td><td>{registro.Longitude}</td></tr>");
                        }
                        
                        htmlBuilder.Append("</table>");
                    }
                    else
                    {
                        htmlBuilder.Append("<p>Nenhum registro encontrado para este funcionário.</p>");
                    }
                }

                htmlBuilder.Append("</body></html>");

                // 5. Retorna o HTML
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