using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    // Adiciona o suporte a User Secrets quando estiver em ambiente de desenvolvimento
    .ConfigureAppConfiguration((context, config) =>
    {
        if (context.HostingEnvironment.IsDevelopment())
        {
            config.AddUserSecrets<Program>();
        }
    })
    .Build();

var config = host.Services.GetRequiredService<IConfiguration>();

// 1. Lê as configurações dos arquivos e do User Secrets
var uri = config["Elasticsearch:Uri"];
var user = config["Elasticsearch:Username"];
var password = config["Elasticsearch:Password"]; // <-- Vem do User Secrets!
var fingerprint = config["Elasticsearch:Fingerprint"]; // <-- Vem do User Secrets!

Console.WriteLine("--> Lendo configurações...");

// Valida se as configurações foram carregadas
if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fingerprint))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERRO: Senha ou Fingerprint não encontrados. Verifique seu appsettings.json e User Secrets.");
    Console.ResetColor();
    return;
}

// 2. Configurações da Conexão
var settings = new ElasticsearchClientSettings(new Uri("https://localhost:9200"))
    .Authentication(new BasicAuthentication(user, password))
    .CertificateFingerprint(fingerprint)
    .DefaultIndex("meus-produtos");

var client = new ElasticsearchClient(settings);

Console.WriteLine("--> Cliente Elasticsearch configurado.");

// 3. Indexando um novo documento
Console.WriteLine("Indexando um novo produto...");
var novoProduto = new Produto
{
    Id = 1,
    Nome = "Notebook Gamer",
    Preco = 9500.00,
    Tags = { "gamer", "notebook", "alta performance" }
};

var indexResponse = await client.IndexAsync(novoProduto);

if (!indexResponse.IsSuccess())
{
    Console.WriteLine($"Erro ao indexar: {indexResponse.DebugInformation}");
    return;
}

Console.WriteLine($"Produto indexado com sucesso no índice '{indexResponse.Index}'");

// É uma boa prática dar um tempo para o documento ser indexado antes de buscar
await Task.Delay(1000);

// 4. Buscando pelo produto
Console.WriteLine("\nBuscando por produtos 'gamer'...");
var searchResponse = await client.SearchAsync<Produto>(s => s
    .Query(q => q
        .Match(m => m
            .Field(f => f.Tags)
            .Query("gamer")
        )
    )
);

if (searchResponse.IsSuccess())
{
    Console.WriteLine($"Total de produtos encontrados: {searchResponse.Total}");
    foreach (var produto in searchResponse.Documents)
    {
        Console.WriteLine($"- ID: {produto.Id}, Nome: {produto.Nome}, Preço: {produto.Preco:C}");
    }
}
else
{
    Console.WriteLine($"Erro na busca: {searchResponse.DebugInformation}");
}

// 5. Deletando o documento
Console.WriteLine("\nDeletando o produto...");
var deleteResponse = await client.DeleteAsync<Produto>(novoProduto.Id);

if (deleteResponse.IsSuccess())
{
    Console.WriteLine("Produto deletado com sucesso.");
}
else
{
    Console.WriteLine($"Erro ao deletar: {deleteResponse.DebugInformation}");
}