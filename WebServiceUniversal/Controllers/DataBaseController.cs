using System.Data;
using WebServiceUniversal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Necessário instalar o pacote NuGet: Microsoft.Data.SqlClient

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

            /*Criar um função separada para fazer a criptografia e descriptografia da string de conexão, ao receber a query descriptografar e ver se o comando faz sentido
            desse modo tenho certeza que sómente o meu app vai conseguir acessar o banco de dados, e mesmo que alguém consiga acessar o endpoint,
            não vai conseguir acessar o banco de dados sem a chave de criptografia*/

            // Validação básica de segurança (MUITO IMPORTANTE)
            if (string.IsNullOrEmpty(request.Query))
                return BadRequest("A query não pode estar vazia.");

            // Bloqueio simples de comandos destrutivos (Não substitui segurança real)
            string upperQuery = request.Query.ToUpper();
            if (upperQuery.Contains("DROP ") || upperQuery.Contains("DELETE ") || upperQuery.Contains("TRUNCATE "))
            {
                return BadRequest("Comandos destrutivos não são permitidos neste endpoint.");
            }

            var listaResultados = new List<Dictionary<string, object>>();
            string connectionString = _configuration.GetConnectionString("MinhaConexao");

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(request.Query, conn))
                    {
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