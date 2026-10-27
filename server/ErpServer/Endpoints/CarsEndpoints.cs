using System.Data;
using ErpServer.Data;
using ErpServer.Dtos.Cars;
using ErpServer.Models;
using ErpServer.Services;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;

namespace ErpServer.Endpoints;

public static class CarsEndpoints
{
    const string GetCarsEndpointName = "GetCars"; 
        
    public static void MapCarsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cars");
        
        // GET cars
        group.MapGet("/", async (ErpDbContext dbContext) => 
            await dbContext.Cars
                .Include(c => c.ResponsibleUser)
                .Select(car => new CarDto(
                    car.Id,
                    car.Brand,
                    car.Model,
                    car.FabricationYear,
                    car.VIN,
                    car.RegistrationNumber,
                    car.Status,
                    car.ResponsibleUserId,
                    car.ResponsibleUser != null ? (car.ResponsibleUser.FirstName + " " + car.ResponsibleUser.LastName).Trim() : null
                ))
                .AsNoTracking()
                .ToListAsync()
        );
        
        // GET cars/{id}
        group.MapGet("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var car = await dbContext.Cars
                .Include(c => c.ResponsibleUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
            
            return car is null
                ? Results.NotFound()
                : Results.Ok(new CarDto(
                    car.Id,
                    car.Brand,
                    car.Model,
                    car.FabricationYear,
                    car.VIN,
                    car.RegistrationNumber,
                    car.Status,
                    car.ResponsibleUserId,
                    car.ResponsibleUser != null ? (car.ResponsibleUser.FirstName + " " + car.ResponsibleUser.LastName).Trim() : null
                ));
        })
        .WithName(GetCarsEndpointName);

        // DELETE cars/{id}
        group.MapDelete("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var car = await dbContext.Cars.FindAsync(id);
            if (car is null) return Results.NotFound();

            dbContext.Cars.Remove(car);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });
        
        // POST cars
        group.MapPost("/", async (CreateCarDto dto, ErpDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Brand))
            {
                return Results.BadRequest("Car brand is required.");
            }
            
            if (string.IsNullOrWhiteSpace(dto.Model))
            {
                return Results.BadRequest("Car model is required.");
            }
            
            if (string.IsNullOrWhiteSpace(dto.VIN))
            {
                return Results.BadRequest("VIN is required.");
            }
            
            if (string.IsNullOrWhiteSpace(dto.RegistrationNumber))
            {
                return Results.BadRequest("Registration number is required.");
            }

            var car = new Car
            {
                Brand =  dto.Brand,
                Model =  dto.Model,
                FabricationYear = dto.FabricationYear,
                VIN = dto.VIN,
                RegistrationNumber = dto.RegistrationNumber,
                Status = dto.Status,
                ResponsibleUserId = dto.ResponsibleUserId
            };
            
            dbContext.Cars.Add(car);
            await dbContext.SaveChangesAsync();

            string? responsibleUserName = null;
            if (car.ResponsibleUserId.HasValue)
            {
                var user = await dbContext.Users.FindAsync(car.ResponsibleUserId.Value);
                if (user != null)
                {
                    responsibleUserName = $"{user.FirstName} {user.LastName}".Trim();
                }
            }

            var resultDto = new CarDto(
                car.Id,
                car.Brand,
                car.Model,
                car.FabricationYear,
                car.VIN,
                car.RegistrationNumber,
                car.Status,
                car.ResponsibleUserId,
                responsibleUserName
            );
            
            return Results.CreatedAtRoute(GetCarsEndpointName, new { id = car.Id }, resultDto);
        });
        
        // PUT cars/{id}
        group.MapPut("/{id:int}", async (int id, UpdateCarDto dto, ErpDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Brand))
            {
                return Results.BadRequest("Brand field cannot be empty");
            }
            
            if (string.IsNullOrWhiteSpace(dto.Model))
            {
                return Results.BadRequest("Model field cannot be empty");
            }
            
            if (dto.FabricationYear < 1990 || dto.FabricationYear > DateTime.UtcNow.Year + 1)
            {
                return Results.BadRequest("Please provide a valid fabrication year");
            }

            if (string.IsNullOrWhiteSpace(dto.VIN))
            {
                return Results.BadRequest("VIN cannot be empty");
            }
            
            if (string.IsNullOrWhiteSpace(dto.RegistrationNumber))
            {
                return Results.BadRequest("Registration number cannot be empty");
            }
            
            if (!Enum.IsDefined(dto.Status))
            {
                return Results.BadRequest("Invalid car status");
            }

            var car = await dbContext.Cars.FindAsync(id);
            if (car is null)
            {
                return Results.NotFound();
            }
            
            car.Brand = dto.Brand;
            car.Model = dto.Model;
            car.FabricationYear = dto.FabricationYear;
            car.VIN = dto.VIN;
            car.RegistrationNumber = dto.RegistrationNumber;
            car.Status = dto.Status;
            car.ResponsibleUserId = dto.ResponsibleUserId;
            
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });

        // GET cars/{id}/documents
        group.MapGet("/{id:int}/documents", async (int id, ErpDbContext dbContext) =>
        {
            var carExists = await dbContext.Cars.AnyAsync(c => c.Id == id);
            if (!carExists) return Results.NotFound("Car not found.");

            var documents = await dbContext.CarDocuments
                .Where(d => d.CarId == id)
                .AsNoTracking()
                .Select(d => new CarDocumentDto(
                    d.Id,
                    d.CarId,
                    d.DocumentType.ToString(),
                    d.FileName,
                    d.StoragePath,
                    d.FileUrl,
                    d.FileSizeBytes,
                    d.UploadedAt
                ))
                .ToListAsync();

            return Results.Ok(documents);
        });

        // POST cars/{id}/documents
        group.MapPost("/{id:int}/documents", async (
            int id, 
            HttpRequest request,
            ErpDbContext dbContext,
            ISupabaseStorageService storageService,
            IConfiguration configuration) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest("Expected multipart/form-data content type.");
            }

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            var documentTypeStr = form["documentType"].ToString();

            if (file is null || file.Length == 0)
            {
                return Results.BadRequest("No file provided or file is empty.");
            }

            // STRICT PDF VALIDATION: extension and content-type must be PDF only
            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest("Only PDF files (.pdf) are allowed.");
            }

            if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest("Invalid content type. Only application/pdf is allowed.");
            }

            if (!Enum.TryParse<CarDocumentType>(documentTypeStr, true, out var documentType))
            {
                return Results.BadRequest($"Invalid document type '{documentTypeStr}'. Allowed types: Insurance, TechnicalBook, RegistrationCertificate.");
            }

            var car = await dbContext.Cars.FindAsync(id);
            if (car is null)
            {
                return Results.NotFound("Car not found.");
            }

            var bucketName = configuration["Supabase:Bucket"] ?? "car-documents";
            var storagePath = $"cars/{id}/{documentType}.pdf";

            string fileUrl;
            try
            {
                await using (var stream = file.OpenReadStream())
                {
                    fileUrl = await storageService.UploadFileAsync(bucketName, storagePath, stream, "application/pdf");
                }
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Eroare la stocarea în Supabase: {ex.Message}");
            }

            var existingDoc = await dbContext.CarDocuments
                .FirstOrDefaultAsync(d => d.CarId == id && d.DocumentType == documentType);

            if (existingDoc is not null)
            {
                existingDoc.FileName = file.FileName;
                existingDoc.StoragePath = storagePath;
                existingDoc.FileUrl = fileUrl;
                existingDoc.FileSizeBytes = file.Length;
                existingDoc.UploadedAt = DateTime.UtcNow;
            }
            else
            {
                existingDoc = new CarDocument
                {
                    CarId = id,
                    DocumentType = documentType,
                    FileName = file.FileName,
                    StoragePath = storagePath,
                    FileUrl = fileUrl,
                    FileSizeBytes = file.Length,
                    UploadedAt = DateTime.UtcNow
                };
                dbContext.CarDocuments.Add(existingDoc);
            }

            await dbContext.SaveChangesAsync();

            var resultDto = new CarDocumentDto(
                existingDoc.Id,
                existingDoc.CarId,
                existingDoc.DocumentType.ToString(),
                existingDoc.FileName,
                existingDoc.StoragePath,
                existingDoc.FileUrl,
                existingDoc.FileSizeBytes,
                existingDoc.UploadedAt
            );

            return Results.Ok(resultDto);
        }).DisableAntiforgery();

        // DELETE cars/{id}/documents/{documentType}
        group.MapDelete("/{id:int}/documents/{documentType}", async (
            int id, 
            string documentType, 
            ErpDbContext dbContext,
            ISupabaseStorageService storageService,
            IConfiguration configuration) =>
        {
            if (!Enum.TryParse<CarDocumentType>(documentType, true, out var parsedType))
            {
                return Results.BadRequest($"Invalid document type '{documentType}'.");
            }

            var doc = await dbContext.CarDocuments
                .FirstOrDefaultAsync(d => d.CarId == id && d.DocumentType == parsedType);

            if (doc is null)
            {
                return Results.NotFound("Document not found.");
            }

            var bucketName = configuration["Supabase:Bucket"] ?? "car-documents";
            await storageService.DeleteFileAsync(bucketName, doc.StoragePath);

            dbContext.CarDocuments.Remove(doc);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

    }
}