using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Data.Tables;
using System;
using System.Text.Json;
using Azure;
using System.Text; // Para criar nosso HTML de status

namespace RastreioGpsApi
{
    public static class ReceberCoordenadas
    {
        [FunctionName("ReceberCoordenadas")]
        public static async Task<IActionResult> Run(
            // Adicionamos "get" para o navegador poder acessar
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Lógica para tratar o GET (navegador)
            if (req.Method == "GET")
            {
                log.LogInformation("C# HTTP trigger 'ReceberCoordenadas' processou um request GET (status page).");
                var html = new StringBuilder();
                html.Append("<html><head><title>API de Rastreio</title></head>");
                html.Append("<body style='font-family: sans-serif; text-align: center; margin-top: 50px;'>");
                html.Append("<h1>API de Recebimento de Coordenadas</h1>");
                html.Append("<p style='font-size: 1.2em;'>Esta API está <strong>online e funcionando</strong>.</p>");
                html.Append("<p>Ela está esperando dados (via POST) do aplicativo de rastreamento.</p>");
                html.Append("</body></html>");

                return new ContentResult
                {
                    Content = html.ToString(),
                    ContentType = "text/html; charset=utf-8",
                    StatusCode = 200
                };
            }

            // --- Daqui para baixo, é o código original que trata o POST do celular ---
            
            log.LogInformation("C# HTTP trigger function 'ReceberCoordenadas' processou um request POST.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            // 1. Recebe os dados do celular
            CoordenadaData data;
            try
            {
                data = System.Text.Json.JsonSerializer.Deserialize<CoordenadaData>(requestBody);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Erro ao deserializar o JSON.");
                return new BadRequestObjectResult("JSON mal formatado.");
            }

            // 2. Validação simples
            if (string.IsNullOrEmpty(data?.Celular) || data.Latitude == 0 || data.Longitude == 0)
            {
                return new BadRequestObjectResult("Dados inválidos. 'Celular', 'Latitude' e 'Longitude' são obrigatórios.");
            }

            try
            {
                // 3. Verificação de Autorização
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var authTableClient = new TableClient(connectionString, "FuncionariosPermitidos");
                await authTableClient.CreateIfNotExistsAsync();

                string celularNormalizado = data.Celular.Replace("+", "");
                Azure.NullableResponse<FuncionarioPermitidoEntidade> authResult = await authTableClient.GetEntityIfExistsAsync<FuncionarioPermitidoEntidade>("FUNCIONARIO", celularNormalizado);

                if (!authResult.HasValue)
                {
                    log.LogWarning($"Tentativa de envio não autorizada do celular: {data.Celular}");
                    return new ObjectResult("Celular não autorizado.") { StatusCode = StatusCodes.Status403Forbidden };
                }

                // 4. Prepara a conexão com o Table Storage
                var coordTableClient = new TableClient(connectionString, "Coordenadas");
                
                // 5. Cria a entidade
                var entidade = new CoordenadaEntidade
                {
                    PartitionKey = celularNormalizado, 
                    RowKey = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d20"),
                    Latitude = data.Latitude,
                    Longitude = data.Longitude,
                    Timestamp = DateTime.UtcNow 
                };

                // 6. Salva no banco de dados
                await coordTableClient.UpsertEntityAsync(entidade);

                return new OkObjectResult("Coordenada recebida com sucesso.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Erro ao processar o request.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    } 

    // ========= IMPORTANTE: ESTAS CLASSES ESTAVAM FALTANDO =========
    // Elas definem o que é "CoordenadaData", "CoordenadaEntidade", etc.

    public class CoordenadaData
    {
        public string Celular { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class CoordenadaEntidade : ITableEntity
    {
        public string PartitionKey { get; set; } 
        public string RowKey { get; set; }       
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }
    }

    public class FuncionarioPermitidoEntidade : ITableEntity
    {
        public string PartitionKey { get; set; } 
        public string RowKey { get; set; }       
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }
    }
}