
// 2. Definição do Modelo de Dados
public record Produto
{
    public int Id { get; set; }
    public required string Nome { get; init; }
    public double Preco { get; init; }
    public List<string> Tags { get; init; } = new();
}
