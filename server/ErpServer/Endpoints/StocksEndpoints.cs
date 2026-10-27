using System.Security.Claims;
using ErpServer.Data;
using ErpServer.Dtos.Stocks;
using ErpServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpServer.Endpoints;

public static class StocksEndpoints
{
    public static void MapStocksEndpoints(this WebApplication app)
    {
        // ==========================================
        // 1. SITES ENDPOINTS (/api/sites)
        // ==========================================
        var sitesGroup = app.MapGroup("/api/sites");

        // GET all sites with totals
        sitesGroup.MapGet("/", async (ErpDbContext dbContext) =>
        {
            var sites = await dbContext.Sites
                .AsNoTracking()
                .Include(s => s.Stocks)
                .OrderByDescending(s => s.IsBase)
                .ThenBy(s => s.Name)
                .Select(s => new ConstructionSiteDto(
                    s.Id,
                    s.Name,
                    s.Description,
                    s.Location,
                    s.IsBase,
                    s.Stocks.Count,
                    s.Stocks.Sum(st => st.TotalPrice),
                    s.CreatedAt
                ))
                .ToListAsync();

            return Results.Ok(sites);
        });

        // GET single site
        sitesGroup.MapGet("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var site = await dbContext.Sites
                .AsNoTracking()
                .Include(s => s.Stocks)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (site is null) return Results.NotFound();

            return Results.Ok(new ConstructionSiteDto(
                site.Id,
                site.Name,
                site.Description,
                site.Location,
                site.IsBase,
                site.Stocks.Count,
                site.Stocks.Sum(st => st.TotalPrice),
                site.CreatedAt
            ));
        });

        // POST create new site
        sitesGroup.MapPost("/", async (CreateConstructionSiteDto dto, ErpDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return Results.BadRequest("Numele șantierului este obligatoriu.");
            }

            var site = new ConstructionSite
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Location = dto.Location?.Trim(),
                IsBase = false,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Sites.Add(site);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new ConstructionSiteDto(
                site.Id,
                site.Name,
                site.Description,
                site.Location,
                site.IsBase,
                0,
                0m,
                site.CreatedAt
            ));
        });

        // DELETE site
        sitesGroup.MapDelete("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var site = await dbContext.Sites.Include(s => s.Stocks).FirstOrDefaultAsync(s => s.Id == id);
            if (site is null) return Results.NotFound();
            if (site.IsBase) return Results.BadRequest("Nu se poate șterge baza principală.");

            if (site.Stocks.Any())
            {
                var baseSite = await dbContext.Sites.FirstOrDefaultAsync(s => s.IsBase);
                if (baseSite != null)
                {
                    foreach (var stock in site.Stocks)
                    {
                        stock.SiteId = baseSite.Id;
                    }
                }
            }

            dbContext.Sites.Remove(site);
            await dbContext.SaveChangesAsync();
            return Results.Ok(new { message = "Șantier șters cu succes." });
        });

        // ==========================================
        // 2. STOCKS ENDPOINTS (/api/stocks)
        // ==========================================
        var stocksGroup = app.MapGroup("/api/stocks");

        // GET stocks with optional filters (siteId, category, search)
        stocksGroup.MapGet("/", async (
            int? siteId,
            StockCategory? category,
            string? search,
            ErpDbContext dbContext) =>
        {
            var query = dbContext.Stocks
                .AsNoTracking()
                .Include(s => s.Site)
                .AsQueryable();

            if (siteId.HasValue)
            {
                query = query.Where(s => s.SiteId == siteId.Value);
            }

            if (category.HasValue)
            {
                query = query.Where(s => s.Category == category.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(q) ||
                    (s.Code != null && s.Code.ToLower().Contains(q)) ||
                    (s.SerialNumber != null && s.SerialNumber.ToLower().Contains(q)) ||
                    (s.Supplier != null && s.Supplier.ToLower().Contains(q)) ||
                    (s.InvoiceNumber != null && s.InvoiceNumber.ToLower().Contains(q))
                );
            }

            var stocks = await query
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => MapToDto(s))
                .ToListAsync();

            return Results.Ok(stocks);
        });

        // GET single stock by id
        stocksGroup.MapGet("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var stock = await dbContext.Stocks
                .AsNoTracking()
                .Include(s => s.Site)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stock is null) return Results.NotFound();

            return Results.Ok(MapToDto(stock));
        });

        // POST create new stock (defaults to Baza Tomesti if siteId not specified)
        stocksGroup.MapPost("/", async (
            CreateStockDto dto,
            ClaimsPrincipal userClaims,
            ErpDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return Results.BadRequest("Denumirea produsului/sculei este obligatorie.");
            }

            if (string.IsNullOrWhiteSpace(dto.UnitOfMeasurement))
            {
                return Results.BadRequest("Unitatea de măsură este obligatorie.");
            }

            if (dto.Quantity < 0)
            {
                return Results.BadRequest("Cantitatea nu poate fi negativă.");
            }

            if (dto.UnitPrice < 0)
            {
                return Results.BadRequest("Prețul unitar nu poate fi negativ.");
            }

            // Determine site: if not provided or 0, find base site (Baza Tomesti)
            int targetSiteId = dto.SiteId ?? 0;
            if (targetSiteId <= 0)
            {
                var baseSite = await dbContext.Sites.FirstOrDefaultAsync(s => s.IsBase)
                               ?? await dbContext.Sites.FirstOrDefaultAsync();

                if (baseSite == null)
                {
                    // Fallback create Baza Tomesti
                    baseSite = new ConstructionSite
                    {
                        Name = "Baza Tomești",
                        Description = "Baza centrală de unde pleacă toate echipamentele și materialele",
                        Location = "Tomești, Iași",
                        IsBase = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Sites.Add(baseSite);
                    await dbContext.SaveChangesAsync();
                }

                targetSiteId = baseSite.Id;
            }

            var site = await dbContext.Sites.FindAsync(targetSiteId);
            if (site == null)
            {
                return Results.BadRequest("Șantierul specificat nu există.");
            }

            var stock = new Stock
            {
                Name = dto.Name.Trim(),
                Category = dto.Category,
                SerialNumber = dto.SerialNumber?.Trim(),
                Code = dto.Code?.Trim(),
                UnitOfMeasurement = dto.UnitOfMeasurement.Trim(),
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                TotalPrice = dto.Quantity * dto.UnitPrice,
                Supplier = dto.Supplier?.Trim(),
                InvoiceDate = ToUtc(dto.InvoiceDate),
                InvoiceNumber = dto.InvoiceNumber?.Trim(),
                DeliveryNoteDate = ToUtc(dto.DeliveryNoteDate),
                DeliveryNoteNumber = dto.DeliveryNoteNumber?.Trim(),
                Condition = dto.Condition,
                SiteId = targetSiteId,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Stocks.Add(stock);
            await dbContext.SaveChangesAsync();

            // Record initial entry in stock_movements history
            var (userId, userName) = ExtractUser(userClaims);
            var initialMovement = new StockMovement
            {
                StockId = stock.Id,
                StockName = stock.Name,
                FromSiteId = null,
                ToSiteId = targetSiteId,
                Quantity = stock.Quantity,
                MovedAt = DateTime.UtcNow,
                MovedByUserId = userId,
                MovedByUserName = userName,
                Notes = string.IsNullOrWhiteSpace(dto.InitialNotes)
                    ? $"Intrare inițială în gestiune la {site.Name}"
                    : dto.InitialNotes.Trim(),
                MovementType = "Intrare în gestiune"
            };

            dbContext.StockMovements.Add(initialMovement);
            await dbContext.SaveChangesAsync();

            stock.Site = site;
            return Results.Ok(MapToDto(stock));
        });

        // PUT update stock details
        stocksGroup.MapPut("/{id:int}", async (
            int id,
            UpdateStockDto dto,
            ErpDbContext dbContext) =>
        {
            var stock = await dbContext.Stocks
                .Include(s => s.Site)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stock is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return Results.BadRequest("Denumirea este obligatorie.");
            }

            if (string.IsNullOrWhiteSpace(dto.UnitOfMeasurement))
            {
                return Results.BadRequest("Unitatea de măsură este obligatorie.");
            }

            if (dto.Quantity < 0)
            {
                return Results.BadRequest("Cantitatea nu poate fi negativă.");
            }

            if (dto.UnitPrice < 0)
            {
                return Results.BadRequest("Prețul unitar nu poate fi negativ.");
            }

            stock.Name = dto.Name.Trim();
            stock.Category = dto.Category;
            stock.SerialNumber = dto.SerialNumber?.Trim();
            stock.Code = dto.Code?.Trim();
            stock.UnitOfMeasurement = dto.UnitOfMeasurement.Trim();
            stock.Quantity = dto.Quantity;
            stock.UnitPrice = dto.UnitPrice;
            stock.TotalPrice = dto.Quantity * dto.UnitPrice;
            stock.Supplier = dto.Supplier?.Trim();
            stock.InvoiceDate = ToUtc(dto.InvoiceDate);
            stock.InvoiceNumber = dto.InvoiceNumber?.Trim();
            stock.DeliveryNoteDate = ToUtc(dto.DeliveryNoteDate);
            stock.DeliveryNoteNumber = dto.DeliveryNoteNumber?.Trim();
            stock.Condition = dto.Condition;
            stock.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return Results.Ok(MapToDto(stock));
        });

        // DELETE stock item
        stocksGroup.MapDelete("/{id:int}", async (int id, ErpDbContext dbContext) =>
        {
            var stock = await dbContext.Stocks.FindAsync(id);
            if (stock is null) return Results.NotFound();

            dbContext.Stocks.Remove(stock);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        // POST move stock between sites
        stocksGroup.MapPost("/{id:int}/move", async (
            int id,
            MoveStockDto dto,
            ClaimsPrincipal userClaims,
            ErpDbContext dbContext) =>
        {
            var stock = await dbContext.Stocks
                .Include(s => s.Site)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stock is null) return Results.NotFound("Elementul de stoc nu a fost găsit.");

            if (dto.Quantity <= 0)
            {
                return Results.BadRequest("Cantitatea mutată trebuie să fie mai mare decât 0.");
            }

            if (dto.Quantity > stock.Quantity)
            {
                return Results.BadRequest($"Cantitatea specificată ({dto.Quantity}) depășește stocul disponibil ({stock.Quantity}).");
            }

            if (dto.ToSiteId == stock.SiteId)
            {
                return Results.BadRequest("Șantierul destinație nu poate fi același cu șantierul sursă.");
            }

            var targetSite = await dbContext.Sites.FindAsync(dto.ToSiteId);
            if (targetSite == null)
            {
                return Results.BadRequest("Șantierul destinație nu există.");
            }

            var fromSiteId = stock.SiteId;
            var fromSiteName = stock.Site?.Name ?? $"Șantier #{fromSiteId}";
            var (userId, userName) = ExtractUser(userClaims);

            // Resolve driver name if driver ID provided
            string? driverName = dto.DriverName?.Trim();
            if (dto.DriverId.HasValue && string.IsNullOrWhiteSpace(driverName))
            {
                var driver = await dbContext.Users.FindAsync(dto.DriverId.Value);
                if (driver != null)
                {
                    driverName = $"{driver.FirstName} {driver.LastName}".Trim();
                }
            }

            if (dto.Quantity == stock.Quantity)
            {
                // Full move: update location of this record
                stock.SiteId = dto.ToSiteId;
                stock.UpdatedAt = DateTime.UtcNow;

                var movement = new StockMovement
                {
                    StockId = stock.Id,
                    StockName = stock.Name,
                    FromSiteId = fromSiteId,
                    ToSiteId = dto.ToSiteId,
                    Quantity = dto.Quantity,
                    MovedAt = DateTime.UtcNow,
                    MovedByUserId = userId,
                    MovedByUserName = userName,
                    DriverId = dto.DriverId,
                    DriverName = driverName,
                    Notes = dto.Notes?.Trim(),
                    MovementType = "Transfer complet"
                };

                dbContext.StockMovements.Add(movement);
                await dbContext.SaveChangesAsync();

                stock.Site = targetSite;
                return Results.Ok(MapToDto(stock));
            }
            else
            {
                // Partial move: decrease origin stock quantity and create a new record at target site
                stock.Quantity -= dto.Quantity;
                stock.TotalPrice = stock.Quantity * stock.UnitPrice;
                stock.UpdatedAt = DateTime.UtcNow;

                var targetStock = new Stock
                {
                    Name = stock.Name,
                    Category = stock.Category,
                    SerialNumber = stock.SerialNumber,
                    Code = stock.Code,
                    UnitOfMeasurement = stock.UnitOfMeasurement,
                    Quantity = dto.Quantity,
                    UnitPrice = stock.UnitPrice,
                    TotalPrice = dto.Quantity * stock.UnitPrice,
                    Supplier = stock.Supplier,
                    InvoiceDate = stock.InvoiceDate,
                    InvoiceNumber = stock.InvoiceNumber,
                    DeliveryNoteDate = stock.DeliveryNoteDate,
                    DeliveryNoteNumber = stock.DeliveryNoteNumber,
                    Condition = stock.Condition,
                    SiteId = dto.ToSiteId,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Stocks.Add(targetStock);
                await dbContext.SaveChangesAsync();

                // Record movement for origin item
                var movementOrigin = new StockMovement
                {
                    StockId = stock.Id,
                    StockName = stock.Name,
                    FromSiteId = fromSiteId,
                    ToSiteId = dto.ToSiteId,
                    Quantity = dto.Quantity,
                    MovedAt = DateTime.UtcNow,
                    MovedByUserId = userId,
                    MovedByUserName = userName,
                    DriverId = dto.DriverId,
                    DriverName = driverName,
                    Notes = string.IsNullOrWhiteSpace(dto.Notes)
                        ? $"Transfer parțial de {dto.Quantity} {stock.UnitOfMeasurement} către {targetSite.Name}"
                        : dto.Notes.Trim(),
                    MovementType = "Transfer parțial"
                };
                dbContext.StockMovements.Add(movementOrigin);

                // Also copy full history to target stock + add this incoming transfer
                var priorMovements = await dbContext.StockMovements
                    .Where(sm => sm.StockId == stock.Id && sm.Id != movementOrigin.Id)
                    .OrderBy(sm => sm.MovedAt)
                    .ToListAsync();

                foreach (var pm in priorMovements)
                {
                    dbContext.StockMovements.Add(new StockMovement
                    {
                        StockId = targetStock.Id,
                        StockName = targetStock.Name,
                        FromSiteId = pm.FromSiteId,
                        ToSiteId = pm.ToSiteId,
                        Quantity = pm.Quantity,
                        MovedAt = pm.MovedAt,
                        MovedByUserId = pm.MovedByUserId,
                        MovedByUserName = pm.MovedByUserName,
                        DriverId = pm.DriverId,
                        DriverName = pm.DriverName,
                        Notes = pm.Notes,
                        MovementType = pm.MovementType
                    });
                }

                var movementTarget = new StockMovement
                {
                    StockId = targetStock.Id,
                    StockName = targetStock.Name,
                    FromSiteId = fromSiteId,
                    ToSiteId = dto.ToSiteId,
                    Quantity = dto.Quantity,
                    MovedAt = DateTime.UtcNow,
                    MovedByUserId = userId,
                    MovedByUserName = userName,
                    DriverId = dto.DriverId,
                    DriverName = driverName,
                    Notes = string.IsNullOrWhiteSpace(dto.Notes)
                        ? $"Transferat din {fromSiteName}"
                        : dto.Notes.Trim(),
                    MovementType = "Transfer recepționat"
                };
                dbContext.StockMovements.Add(movementTarget);

                await dbContext.SaveChangesAsync();

                return Results.Ok(MapToDto(stock));
            }
        });

        // POST batch move stock items between sites (Aviz transfer)
        stocksGroup.MapPost("/move-batch", async (
            BatchMoveStockDto dto,
            ClaimsPrincipal userClaims,
            ErpDbContext dbContext) =>
        {
            if (dto.Items == null || dto.Items.Count == 0)
            {
                return Results.BadRequest("Trebuie să selectați cel puțin un articol pentru transfer.");
            }

            if (dto.FromSiteId == dto.ToSiteId)
            {
                return Results.BadRequest("Șantierul destinație nu poate fi același cu șantierul sursă.");
            }

            var fromSite = await dbContext.Sites.FindAsync(dto.FromSiteId);
            if (fromSite == null)
            {
                return Results.BadRequest("Șantierul sursă nu există.");
            }

            var targetSite = await dbContext.Sites.FindAsync(dto.ToSiteId);
            if (targetSite == null)
            {
                return Results.BadRequest("Șantierul destinație nu există.");
            }

            // Validate all items exist and have sufficient quantity
            var stockIds = dto.Items.Select(i => i.StockId).Distinct().ToList();
            var stocks = await dbContext.Stocks
                .Where(s => stockIds.Contains(s.Id))
                .ToListAsync();

            if (stocks.Count != stockIds.Count)
            {
                return Results.BadRequest("Unul sau mai multe articole specificate nu au fost găsite în baza de date.");
            }

            foreach (var reqItem in dto.Items)
            {
                if (reqItem.Quantity <= 0)
                {
                    return Results.BadRequest("Cantitatea transferată trebuie să fie mai mare decât 0.");
                }

                var stock = stocks.First(s => s.Id == reqItem.StockId);
                if (stock.SiteId != dto.FromSiteId)
                {
                    return Results.BadRequest($"Articolul \"{stock.Name}\" nu se află la punctul de plecare selectat ({fromSite.Name}).");
                }

                if (reqItem.Quantity > stock.Quantity)
                {
                    return Results.BadRequest($"Cantitatea specificată ({reqItem.Quantity}) depășește stocul disponibil ({stock.Quantity} {stock.UnitOfMeasurement}) pentru \"{stock.Name}\".");
                }
            }

            var (userId, userName) = ExtractUser(userClaims);

            // Resolve driver name if driver ID provided
            string? driverName = dto.DriverName?.Trim();
            if (dto.DriverId.HasValue && string.IsNullOrWhiteSpace(driverName))
            {
                var driver = await dbContext.Users.FindAsync(dto.DriverId.Value);
                if (driver != null)
                {
                    driverName = $"{driver.FirstName} {driver.LastName}".Trim();
                }
            }

            var moveDate = dto.MovedAt.HasValue
                ? DateTime.SpecifyKind(dto.MovedAt.Value, DateTimeKind.Utc)
                : DateTime.UtcNow;

            string notesCombined = dto.Notes?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(dto.CarPlate))
            {
                string carInfo = $"Mijloc transport: {dto.CarPlate.Trim()}";
                notesCombined = string.IsNullOrWhiteSpace(notesCombined)
                    ? carInfo
                    : $"{notesCombined} | {carInfo}";
            }

            var updatedStocks = new List<StockDto>();
            var createdMovements = new List<StockMovementDto>();

            foreach (var reqItem in dto.Items)
            {
                var stock = stocks.First(s => s.Id == reqItem.StockId);

                if (reqItem.Quantity == stock.Quantity)
                {
                    // Full move
                    stock.SiteId = dto.ToSiteId;
                    stock.UpdatedAt = DateTime.UtcNow;

                    var movement = new StockMovement
                    {
                        StockId = stock.Id,
                        StockName = stock.Name,
                        FromSiteId = dto.FromSiteId,
                        ToSiteId = dto.ToSiteId,
                        Quantity = reqItem.Quantity,
                        MovedAt = moveDate,
                        CreatedAt = DateTime.UtcNow,
                        MovedByUserId = userId,
                        MovedByUserName = userName,
                        DriverId = dto.DriverId,
                        DriverName = driverName,
                        Notes = string.IsNullOrWhiteSpace(notesCombined) ? null : notesCombined,
                        MovementType = "Transfer complet"
                    };

                    dbContext.StockMovements.Add(movement);
                    await dbContext.SaveChangesAsync();

                    stock.Site = targetSite;
                    updatedStocks.Add(MapToDto(stock));
                    createdMovements.Add(new StockMovementDto(
                        movement.Id,
                        movement.StockId,
                        movement.StockName,
                        movement.FromSiteId,
                        fromSite.Name,
                        movement.ToSiteId,
                        targetSite.Name,
                        movement.Quantity,
                        movement.MovedAt,
                        movement.MovedByUserId,
                        movement.MovedByUserName,
                        movement.Notes,
                        movement.MovementType,
                        movement.DriverId,
                        movement.DriverName
                    ));
                }
                else
                {
                    // Partial move
                    stock.Quantity -= reqItem.Quantity;
                    stock.TotalPrice = stock.Quantity * stock.UnitPrice;
                    stock.UpdatedAt = DateTime.UtcNow;

                    var targetStock = new Stock
                    {
                        Name = stock.Name,
                        Category = stock.Category,
                        SerialNumber = stock.SerialNumber,
                        Code = stock.Code,
                        UnitOfMeasurement = stock.UnitOfMeasurement,
                        Quantity = reqItem.Quantity,
                        UnitPrice = stock.UnitPrice,
                        TotalPrice = reqItem.Quantity * stock.UnitPrice,
                        Supplier = stock.Supplier,
                        InvoiceDate = stock.InvoiceDate,
                        InvoiceNumber = stock.InvoiceNumber,
                        DeliveryNoteDate = stock.DeliveryNoteDate,
                        DeliveryNoteNumber = stock.DeliveryNoteNumber,
                        Condition = stock.Condition,
                        SiteId = dto.ToSiteId,
                        CreatedAt = DateTime.UtcNow
                    };

                    dbContext.Stocks.Add(targetStock);
                    await dbContext.SaveChangesAsync();

                    var movementOrigin = new StockMovement
                    {
                        StockId = stock.Id,
                        StockName = stock.Name,
                        FromSiteId = dto.FromSiteId,
                        ToSiteId = dto.ToSiteId,
                        Quantity = reqItem.Quantity,
                        MovedAt = moveDate,
                        CreatedAt = DateTime.UtcNow,
                        MovedByUserId = userId,
                        MovedByUserName = userName,
                        DriverId = dto.DriverId,
                        DriverName = driverName,
                        Notes = string.IsNullOrWhiteSpace(notesCombined)
                            ? $"Transfer parțial de {reqItem.Quantity} {stock.UnitOfMeasurement} către {targetSite.Name}"
                            : notesCombined,
                        MovementType = "Transfer parțial"
                    };
                    dbContext.StockMovements.Add(movementOrigin);

                    var movementTarget = new StockMovement
                    {
                        StockId = targetStock.Id,
                        StockName = targetStock.Name,
                        FromSiteId = dto.FromSiteId,
                        ToSiteId = dto.ToSiteId,
                        Quantity = reqItem.Quantity,
                        MovedAt = moveDate,
                        CreatedAt = DateTime.UtcNow,
                        MovedByUserId = userId,
                        MovedByUserName = userName,
                        DriverId = dto.DriverId,
                        DriverName = driverName,
                        Notes = string.IsNullOrWhiteSpace(notesCombined)
                            ? $"Transferat din {fromSite.Name}"
                            : notesCombined,
                        MovementType = "Transfer recepționat"
                    };
                    dbContext.StockMovements.Add(movementTarget);

                    await dbContext.SaveChangesAsync();

                    stock.Site = fromSite;
                    targetStock.Site = targetSite;
                    updatedStocks.Add(MapToDto(stock));
                    createdMovements.Add(new StockMovementDto(
                        movementOrigin.Id,
                        movementOrigin.StockId,
                        movementOrigin.StockName,
                        movementOrigin.FromSiteId,
                        fromSite.Name,
                        movementOrigin.ToSiteId,
                        targetSite.Name,
                        movementOrigin.Quantity,
                        movementOrigin.MovedAt,
                        movementOrigin.MovedByUserId,
                        movementOrigin.MovedByUserName,
                        movementOrigin.Notes,
                        movementOrigin.MovementType,
                        movementOrigin.DriverId,
                        movementOrigin.DriverName
                    ));
                }
            }

            return Results.Ok(new
            {
                success = true,
                message = $"{dto.Items.Count} articole au fost transferate cu succes.",
                updatedStocks,
                movements = createdMovements
            });
        });

        // GET drivers list for transport selection
        stocksGroup.MapGet("/drivers", async (ErpDbContext dbContext) =>
        {
            var driverTitles = await dbContext.JobTitles
                .Where(jt => jt.Name.ToLower().Contains("sofer") || jt.Name.ToLower().Contains("șofer") || jt.Name.ToLower().Contains("driver"))
                .Select(jt => jt.Id)
                .ToListAsync();

            var drivers = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.DeletedAt == null && u.IsActive && driverTitles.Contains(u.JobTitleId))
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new DriverDto(u.Id, $"{u.FirstName} {u.LastName}".Trim(), u.Email))
                .ToListAsync();

            if (!drivers.Any())
            {
                drivers = await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.DeletedAt == null && u.IsActive)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Select(u => new DriverDto(u.Id, $"{u.FirstName} {u.LastName}".Trim(), u.Email))
                    .ToListAsync();
            }

            return Results.Ok(drivers);
        });

        // GET all movements (transports log with optional filters)
        stocksGroup.MapGet("/movements", async (int? siteId, bool? transfersOnly, ErpDbContext dbContext) =>
        {
            var query = dbContext.StockMovements
                .AsNoTracking()
                .Include(sm => sm.FromSite)
                .Include(sm => sm.ToSite)
                .AsQueryable();

            if (siteId.HasValue)
            {
                query = query.Where(sm => sm.FromSiteId == siteId.Value || sm.ToSiteId == siteId.Value);
            }

            if (transfersOnly == true)
            {
                query = query.Where(sm => sm.FromSiteId != null);
            }

            var movements = await query
                .OrderByDescending(sm => sm.MovedAt)
                .Take(250)
                .Select(sm => new StockMovementDto(
                    sm.Id,
                    sm.StockId,
                    sm.StockName,
                    sm.FromSiteId,
                    sm.FromSite != null ? sm.FromSite.Name : "Intrare inițială",
                    sm.ToSiteId,
                    sm.ToSite != null ? sm.ToSite.Name : $"Șantier #{sm.ToSiteId}",
                    sm.Quantity,
                    sm.MovedAt,
                    sm.MovedByUserId,
                    sm.MovedByUserName,
                    sm.Notes,
                    sm.MovementType,
                    sm.DriverId,
                    sm.DriverName
                ))
                .ToListAsync();

            return Results.Ok(movements);
        });

        // GET history / movements for a specific stock item
        stocksGroup.MapGet("/{id:int}/movements", async (int id, ErpDbContext dbContext) =>
        {
            var movements = await dbContext.StockMovements
                .AsNoTracking()
                .Include(sm => sm.FromSite)
                .Include(sm => sm.ToSite)
                .Where(sm => sm.StockId == id)
                .OrderBy(sm => sm.MovedAt)
                .Select(sm => new StockMovementDto(
                    sm.Id,
                    sm.StockId,
                    sm.StockName,
                    sm.FromSiteId,
                    sm.FromSite != null ? sm.FromSite.Name : "Intrare inițială",
                    sm.ToSiteId,
                    sm.ToSite != null ? sm.ToSite.Name : $"Șantier #{sm.ToSiteId}",
                    sm.Quantity,
                    sm.MovedAt,
                    sm.MovedByUserId,
                    sm.MovedByUserName,
                    sm.Notes,
                    sm.MovementType,
                    sm.DriverId,
                    sm.DriverName
                ))
                .ToListAsync();

            return Results.Ok(movements);
        });

        // Seed mock data endpoint
        stocksGroup.MapPost("/seed-mock-data", async (ErpDbContext dbContext) =>
        {
            await StockMockSeeder.SeedMockDataAsync(dbContext, force: true);
            return Results.Ok(new { message = "100 elemente mock au fost generate și împărțite pe cele 5 șantiere." });
        });
    }

    private static StockDto MapToDto(Stock s)
    {
        return new StockDto(
            s.Id,
            s.Name,
            s.Category,
            GetCategoryDisplayName(s.Category),
            s.SerialNumber,
            s.Code,
            s.UnitOfMeasurement,
            s.Quantity,
            s.UnitPrice,
            s.TotalPrice,
            s.Supplier,
            s.InvoiceDate,
            s.InvoiceNumber,
            s.DeliveryNoteDate,
            s.DeliveryNoteNumber,
            s.Condition,
            GetConditionDisplayName(s.Condition),
            s.SiteId,
            s.Site?.Name ?? $"Șantier #{s.SiteId}",
            s.CreatedAt,
            s.UpdatedAt
        );
    }

    private static string GetCategoryDisplayName(StockCategory category) => category switch
    {
        StockCategory.HandTools => "Unelte",
        StockCategory.PowerTools => "Scule",
        StockCategory.ProtectiveEquipment => "Echipament de protecție",
        StockCategory.Formwork => "Cofraje",
        StockCategory.Consumables => "Consumabile",
        _ => category.ToString()
    };

    private static string GetConditionDisplayName(StockCondition condition) => condition switch
    {
        StockCondition.New => "Nou",
        StockCondition.Good => "Bună",
        StockCondition.Used => "Uzat",
        StockCondition.Damaged => "Deteriorat",
        StockCondition.InRepair => "În reparație",
        _ => condition.ToString()
    };

    private static DateTime? ToUtc(DateTime? dt)
    {
        if (!dt.HasValue) return null;
        return DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
    }

    private static (int? userId, string? userName) ExtractUser(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return (null, "Sistem");
        }

        int? userId = null;
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        if (int.TryParse(sub, out var parsedId))
        {
            userId = parsedId;
        }

        var userName = user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("name")?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? "Utilizator";

        return (userId, userName);
    }
}
