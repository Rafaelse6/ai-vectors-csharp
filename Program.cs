using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Embeddings;
using MyPgVectorStore.Data;
using MyPgVectorStore.Models;
using OllamaSharp;
using Pgvector;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, o => o.UseVector());
});

builder.Services.AddTransient<OllamaApiClient>(x => new OllamaApiClient(
    uriString: "http://localhost:11434",
    defaultModel: "mxbai-embed-large"
));


var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/v1/seed", async (
    ApplicationDbContext db,
    OllamaApiClient ollamaClient) =>
{
    var products = await db.Products.ToListAsync();

    foreach (var product in products)
    {
        var textEmbeddingGenerationService = ollamaClient.AsTextEmbeddingGenerationService();
        var embeddings = await textEmbeddingGenerationService.GenerateEmbeddingAsync(product.Category);

        var recomendation = new Recomendation
        {
            Title = product.Title,
            Category = product.Category,
            Embedding = new Vector(embeddings)
        };

        await db.Recomendations.AddAsync(recomendation);
        await db.SaveChangesAsync();
    }

    return Results.Ok(new { message = "Seeded" });
});

app.Run();
