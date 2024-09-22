using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "TodoAPI";
    config.Title = "TodoAPI v1";
    config.Version = "v1";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "TodoAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

// Uygulama başlatıldığında tarayıcıda Swagger'ı aç
app.Lifetime.ApplicationStarted.Register(() =>
{
    var url = $"{app.Urls.First()}/swagger";
    try
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch
    {
        Console.WriteLine($"Unable to open browser to {url}");
    }
});

app.MapGet("/Todoitems", async (TodoDb db) =>
    await db.Todos.ToListAsync());

app.MapGet("/Todoitems/complete", async (TodoDb db) =>
    await db.Todos.Where(t => t.IsComplete).ToListAsync());

app.MapGet("/Todoitems/{id}", async (int id, TodoDb db) =>
    await db.Todos.FindAsync(id) is Todo todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.MapPost("/Todoitems", async (Todo todo, TodoDb db) =>
{
    // ID kontrolü
    if (await db.Todos.FindAsync(todo.Id) != null)
    {
        return Results.BadRequest(new
        {
            Message = "Hata: Bu ID zaten mevcut."
        });
    }

    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/Todoitems/{todo.Id}", new
    {
        Message = "Tebrikler! Todo kaydedildi.",
        Todo = todo
    });
});



app.MapPut("/Todoitems/{id}", async (int id, Todo inputTodo, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/Todoitems/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();
