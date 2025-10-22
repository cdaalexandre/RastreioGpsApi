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
using Azure; // Necessário para a resposta da verificação

namespace RastreioGpsApi
{
    public static class ReceberCoordenadas
    {
        [FunctionName("ReceberCoordenadas")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function 'ReceberCoordenadas' processou um request.");

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
                // 3. NOVO: Verificação de Autorização
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var authTableClient = new TableClient(connectionString, "FuncionariosPermitidos");
                await authTableClient.CreateIfNotExistsAsync();

                // Normaliza o número para usar como chave
                string celularNormalizado = data.Celular.Replace("+", "");

                // Usamos "FUNCIONARIO" como PartitionKey para manter todos em um grupo
                // e o número do celular como RowKey.
                
                // **** LINHA CORRIGIDA ABAIXO ****
                Azure.NullableResponse<FuncionarioPermitidoEntidade> authResult = await authTableClient.GetEntityIfExistsAsync<FuncionarioPermitidoEntidade>("FUNCIONARIO", celularNormalizado);

                if (!authResult.HasValue)
                {
                    // Se authResult.HasValue é false, o celular NÃO FOI ENCONTRADO
                    log.LogWarning($"Tentativa de envio não autorizada do celular: {data.Celular}");
                    return new ObjectResult("Celular não autorizado.") { StatusCode = StatusCodes.Status403Forbidden };
                }

                // 4. Prepara a conexão com o Table Storage (se passou na verificação)
                var coordTableClient = new TableClient(connectionString, "Coordenadas");
                
                // 5. Cria a entidade (a linha da nossa tabela)
                var entidade = new CoordenadaEntidade
                {
                    // Chave de Partição: Agrupa por número de celular (ótimo para performance)
                    PartitionKey = celularNormalizado, 
                    
                    // Chave de Linha: Um ID único para esta entrada específica.
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

    // Classe auxiliar para receber os dados do JSON
    public class CoordenadaData
    {
        public string Celular { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    // Classe que representa a ESTRUTURA da nossa tabela "Coordenadas"
    public class CoordenadaEntidade : ITableEntity
    {
        public string PartitionKey { get; set; } // O número do celular
        public string RowKey { get; set; }       // O ID único (timestamp reverso)
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }
    }

    // NOVO: Classe que representa a ESTRUTURA da nossa tabela "FuncionariosPermitidos"
    public class FuncionarioPermitidoEntidade : ITableEntity
    {
        public string PartitionKey { get; set; } // Será "FUNCIONARIO"
        public string RowKey { get; set; }       // O número do celular
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }

        // Você pode adicionar mais colunas aqui no futuro (ex: NomeDoFuncionario)
        // public string Nome { get; set; }
    }
}