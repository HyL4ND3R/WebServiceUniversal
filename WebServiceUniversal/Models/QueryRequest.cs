namespace WebServiceUniversal.Models
{
    public class QueryRequest
    {
        public string Query { get; set; }
        // Adicionamos um dicionário para os parâmetros (ex: @Usuario, @Senha)
        public Dictionary<string, object> Parametros { get; set; }
    }
}