using System.Data;
using WebServiceUniversal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Necessário instalar o pacote NuGet: Microsoft.Data.SqlClient
using System.Text.Json;

namespace MeuUniversalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DatabaseController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("executar")]
        public IActionResult ExecutarQuery([FromBody] QueryRequest request)
        {

            // Validação básica de segurança (MUITO IMPORTANTE)
            if (string.IsNullOrEmpty(request.Query))
                return BadRequest("A query não pode estar vazia.");


            /*Criar um função separada para fazer a criptografia e descriptografia da string de conexão, ao receber a query descriptografar e ver se o comando faz sentido
            desse modo tenho certeza que sómente o meu app vai conseguir acessar o banco de dados, e mesmo que alguém consiga acessar o endpoint,
            não vai conseguir acessar o banco de dados sem a chave de criptografia*/


            // Só Bloqueando DROP e TRUNCATE
            string upperQuery = request.Query.ToUpper();
            if (upperQuery.Contains("DROP ") || upperQuery.Contains("TRUNCATE "))
            {
                return BadRequest("Comandos destrutivos não são permitidos neste endpoint.");
            }

            var listaResultados = new List<Dictionary<string, object>>();
            string connectionString = _configuration.GetConnectionString("ConexaoBanco");

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(request.Query, conn))
                    {

                        // Adiciona os parâmetros de forma segura
                        if (request.Parametros != null)
                        {
                            foreach (var param in request.Parametros)
                            {
                                object valorFinal = param.Value;

                                // Verificar se é um elemento JSON e converter
                                if (param.Value is JsonElement element)
                                {
                                    switch (element.ValueKind)
                                    {
                                        //Feito case para cada tipo de dados JSON, porém
                                        // acreidito que possa replicar tudo por String
                                        case JsonValueKind.String:
                                            valorFinal = element.GetString();
                                            // O SQL Server converte string para DateTime automaticamente se o formato estiver correto
                                            break;

                                        case JsonValueKind.Number:
                                            // Tenta manter a precisão do número
                                            if (element.TryGetInt32(out int valInt))
                                                valorFinal = valInt;
                                            else if (element.TryGetInt64(out long valLong))
                                                valorFinal = valLong;
                                            else
                                                valorFinal = element.GetDouble();
                                            break;

                                        case JsonValueKind.True:
                                        case JsonValueKind.False:
                                            valorFinal = element.GetBoolean();
                                            break;

                                        case JsonValueKind.Null:
                                            valorFinal = DBNull.Value;
                                            break;

                                        default:
                                            valorFinal = element.ToString(); // Fallback
                                            break;
                                    }
                                }

                                // Agora sim, adicionamos um tipo nativo (int, string, bool) que o SQL aceita
                                cmd.Parameters.AddWithValue(param.Key, valorFinal ?? DBNull.Value);
                            }
                        }

                        // Verifica se é um comando de LEITURA (SELECT) ou ESCRITA (INSERT/UPDATE)
                        if (upperQuery.Trim().StartsWith("SELECT"))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var linha = new Dictionary<string, object>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        linha.Add(reader.GetName(i), reader.GetValue(i));
                                    }
                                    listaResultados.Add(linha);
                                }
                            }
                            return Ok(listaResultados); // Retorna JSON com os dados
                        }
                        else
                        {
                            int linhasAfetadas = cmd.ExecuteNonQuery();
                            return Ok(new { Mensagem = "Comando executado com sucesso", LinhasAfetadas = linhasAfetadas });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Erro = ex.Message });
            }
        }
    }
}