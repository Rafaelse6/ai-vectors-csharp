using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Embeddings;
using MyPgVectorStore.Data;
using MyPgVectorStore.Models;
using MyPgVectorStore.ViewModels;
using OllamaSharp;
using Pgvector;
using Pgvector.EntityFrameworkCore;

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


app.MapPost("/v1/products", async (
    CreateProductViewModel model,
    ApplicationDbContext db,
    OllamaApiClient ollamaClient) =>
{
    var textEmbeddingGenerationService = ollamaClient.AsTextEmbeddingGenerationService();
    var embeddings = await textEmbeddingGenerationService.GenerateEmbeddingAsync(model.Category);

    var product = new Product
    {
        Id = 23,
        Title = model.Title,
        Category = model.Category,
        Summary = model.Summary,
        Description = model.Description
    };

    await db.Products.AddAsync(product);
    await db.SaveChangesAsync();

    return Results.Created();
});


app.MapPost("/v1/recomendations", async (
    CreateProductViewModel model,
    ApplicationDbContext db,
    OllamaApiClient ollamaClient) =>
{
    var textEmbeddingGenerationService = ollamaClient.AsTextEmbeddingGenerationService();
    var embeddings = await textEmbeddingGenerationService.GenerateEmbeddingAsync(model.Category);

    var recomendation = new Recomendation
    {
        Title = model.Title,
        Category = model.Category,
        Embedding = new Vector(embeddings)
    };

    await db.Recomendations.AddAsync(recomendation);
    await db.SaveChangesAsync();

    return Results.Created($"/v1/recomendations", null);
});

app.MapPost("/v1/prompt", async (
    QuestionViewModel model,
    ApplicationDbContext db,
    OllamaApiClient ollamaClient) =>
{
    var textEmbeddingGenerationService = ollamaClient.AsTextEmbeddingGenerationService();
    var embeddings = await textEmbeddingGenerationService.GenerateEmbeddingAsync(model.Prompt);

    var recomendations = await db.Recomendations
        .OrderBy(d => d.Embedding.CosineDistance(new Vector(embeddings.ToArray())))
        .Take(3)
        .Select(x => new
        {
            x.Title,
            x.Category
        })
        .ToListAsync();

    return Results.Ok(recomendations);
});

app.Run();
