using ErpServer.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpServer.Data;

public static class StockMockSeeder
{
    public static async Task SeedMockDataAsync(ErpDbContext dbContext, bool force = false)
    {
        // 1. Ensure the 5 requested sites exist
        var siteNames = new[]
        {
            ("Baza Tomesti", "Tomești, Iași", "Baza centrală de unde pleacă toate echipamentele și materialele", true),
            ("Valea Adanca Strada Veche", "Valea Adâncă, Iași", "Proiect ansamblu rezidențial Strada Veche", false),
            ("Aedificium-Kaufland", "Iași", "Extindere galerie comercială Kaufland", false),
            ("Zara Home Palas", "Palas Mall, Iași", "Amenajare spațiu comercial Zara Home", false),
            ("Casa Aboaice", "Iași", "Construcție vilă rezidențială unifamilială", false),
        };

        var sitesMap = new Dictionary<string, ConstructionSite>();

        foreach (var (name, location, desc, isBase) in siteNames)
        {
            var site = await dbContext.Sites.FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower() || (isBase && s.IsBase));
            if (site == null)
            {
                site = new ConstructionSite
                {
                    Name = name,
                    Location = location,
                    Description = desc,
                    IsBase = isBase,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Sites.Add(site);
                await dbContext.SaveChangesAsync();
            }
            else
            {
                // Ensure name matches user specification
                site.Name = name;
                site.IsBase = isBase;
                await dbContext.SaveChangesAsync();
            }
            sitesMap[name] = site;
        }

        var baseSite = sitesMap["Baza Tomesti"];
        var allSites = sitesMap.Values.ToList();

        // Check if stocks already seeded
        var currentStocksCount = await dbContext.Stocks.CountAsync();
        if (currentStocksCount >= 90 && !force)
        {
            return;
        }

        if (force)
        {
            dbContext.StockMovements.RemoveRange(dbContext.StockMovements);
            dbContext.Stocks.RemoveRange(dbContext.Stocks);
            var validSiteIds = sitesMap.Values.Select(s => s.Id).ToList();
            var extraSites = await dbContext.Sites.Where(s => !validSiteIds.Contains(s.Id)).ToListAsync();
            if (extraSites.Any())
            {
                dbContext.Sites.RemoveRange(extraSites);
            }
            await dbContext.SaveChangesAsync();
        }

        // 2. Define 100 realistic mock items
        var itemsDefinition = Get100Items();

        var rnd = new Random(42); // deterministic seed for realistic consistency
        var baseDate = DateTime.UtcNow.AddMonths(-3);

        for (int i = 0; i < itemsDefinition.Count; i++)
        {
            var def = itemsDefinition[i];
            var assignedSite = allSites[rnd.Next(allSites.Count)];
            var createdDate = baseDate.AddDays(rnd.Next(0, 60));
            var invoiceDate = createdDate.AddDays(-rnd.Next(1, 10));
            var deliveryDate = createdDate.AddDays(-rnd.Next(0, 3));

            var stock = new Stock
            {
                Name = def.Name,
                Category = def.Category,
                Code = def.Code,
                SerialNumber = def.SerialNumber,
                UnitOfMeasurement = def.UnitOfMeasurement,
                Quantity = def.Quantity,
                UnitPrice = def.UnitPrice,
                TotalPrice = def.Quantity * def.UnitPrice,
                Supplier = def.Supplier,
                InvoiceNumber = $"FACT-2026-{1000 + i}",
                InvoiceDate = DateTime.SpecifyKind(invoiceDate, DateTimeKind.Utc),
                DeliveryNoteNumber = $"AVZ-{8000 + i}",
                DeliveryNoteDate = DateTime.SpecifyKind(deliveryDate, DateTimeKind.Utc),
                Condition = def.Condition,
                SiteId = assignedSite.Id,
                CreatedAt = DateTime.SpecifyKind(createdDate, DateTimeKind.Utc)
            };

            dbContext.Stocks.Add(stock);
            await dbContext.SaveChangesAsync();

            // 3. Generate History (movement trail)
            // Step 1: Initial entry at Baza Tomesti
            var initialEntryMovement = new StockMovement
            {
                StockId = stock.Id,
                StockName = stock.Name,
                FromSiteId = null,
                ToSiteId = baseSite.Id,
                Quantity = stock.Quantity,
                MovedAt = DateTime.SpecifyKind(createdDate, DateTimeKind.Utc),
                MovedByUserName = "Ing. Andrei Popescu",
                Notes = $"Intrare recepționată de la {stock.Supplier} conform facturii {stock.InvoiceNumber}",
                MovementType = "Intrare în gestiune"
            };
            dbContext.StockMovements.Add(initialEntryMovement);

            // Step 2: If currently at another site, add the transfer movement from Baza Tomesti to that site!
            if (assignedSite.Id != baseSite.Id)
            {
                var transferDate = createdDate.AddDays(rnd.Next(1, 20));
                if (transferDate > DateTime.UtcNow) transferDate = DateTime.UtcNow.AddHours(-rnd.Next(1, 48));

                var transferMovement = new StockMovement
                {
                    StockId = stock.Id,
                    StockName = stock.Name,
                    FromSiteId = baseSite.Id,
                    ToSiteId = assignedSite.Id,
                    Quantity = stock.Quantity,
                    MovedAt = DateTime.SpecifyKind(transferDate, DateTimeKind.Utc),
                    MovedByUserName = "Dispecerat Baza Tomești",
                    Notes = $"Transfer pe șantier conform bonului de transport #{500 + i} / necesar execuție",
                    MovementType = "Transfer complet",
                    DriverName = rnd.Next(2) == 0 ? "Vasile Dumitru" : "Ion Moraru"
                };
                dbContext.StockMovements.Add(transferMovement);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private record ItemDef(
        string Name,
        StockCategory Category,
        string Code,
        string? SerialNumber,
        string UnitOfMeasurement,
        decimal Quantity,
        decimal UnitPrice,
        string Supplier,
        StockCondition Condition
    );

    private static List<ItemDef> Get100Items()
    {
        return new List<ItemDef>
        {
            // === SCULE (Power Tools) - 20 items ===
            new("Ciocan rotopercutor Bosch GBH 2-26 DFR", StockCategory.PowerTools, "SC-001", "SN-BSH-88492", "buc", 4, 850.50m, "Dedeman", StockCondition.Good),
            new("Ciocan demolator Hilti TE 1000-AVR", StockCategory.PowerTools, "SC-002", "SN-HLT-99214", "buc", 2, 4200.00m, "Hilti România", StockCondition.New),
            new("Polizor unghiular DeWalt DWE492 230mm", StockCategory.PowerTools, "SC-003", "SN-DWT-10928", "buc", 5, 620.00m, "Arabesque", StockCondition.Good),
            new("Mașină de găurit și înșurubat Makita DDF485", StockCategory.PowerTools, "SC-004", "SN-MKT-33910", "buc", 6, 540.00m, "Makita Store", StockCondition.New),
            new("Fierăstrău circular Makita HS7601", StockCategory.PowerTools, "SC-005", "SN-MKT-44120", "buc", 3, 710.00m, "Dedeman", StockCondition.Good),
            new("Fierăstrău sabie Bosch GSA 120", StockCategory.PowerTools, "SC-006", "SN-BSH-77182", "buc", 3, 590.00m, "Dedeman", StockCondition.Used),
            new("Laser rotativ Hilti PR 30-HVS", StockCategory.PowerTools, "SC-007", "SN-HLT-55190", "buc", 1, 5800.00m, "Hilti România", StockCondition.Good),
            new("Nivelă laser autonivelantă DeWalt DW088K", StockCategory.PowerTools, "SC-008", "SN-DWT-66102", "buc", 4, 780.00m, "Hornbach", StockCondition.New),
            new("Vibrator beton Enar Dingo 230V cu lance 4m", StockCategory.PowerTools, "SC-009", "SN-ENR-22910", "buc", 2, 1950.00m, "Arabesque", StockCondition.Good),
            new("Mașină de canelat pereți Bosch GNF 35 CA", StockCategory.PowerTools, "SC-010", "SN-BSH-33109", "buc", 2, 2350.00m, "Dedeman", StockCondition.Good),
            new("Aspirator industrial Hilti VC 20-U", StockCategory.PowerTools, "SC-011", "SN-HLT-11827", "buc", 2, 2890.00m, "Hilti România", StockCondition.Used),
            new("Mașină de debitat metale Makita LW1400", StockCategory.PowerTools, "SC-012", "SN-MKT-88231", "buc", 2, 1150.00m, "Makita Store", StockCondition.Good),
            new("Pistol de bătut cuie pneumatic DeWalt DPN90C", StockCategory.PowerTools, "SC-013", "SN-DWT-44190", "buc", 3, 1420.00m, "Arabesque", StockCondition.New),
            new("Malaxor electric Rubi Rubimix 9", StockCategory.PowerTools, "SC-014", "SN-RUB-99120", "buc", 3, 490.00m, "Dedeman", StockCondition.Good),
            new("Generator de curent Honda EG 5500 CL 5.5kW", StockCategory.PowerTools, "SC-015", "SN-HND-77123", "buc", 1, 6200.00m, "Arabesque", StockCondition.Good),
            new("Motopompă ape uzate Honda WT 30X", StockCategory.PowerTools, "SC-016", "SN-HND-55120", "buc", 1, 4800.00m, "Arabesque", StockCondition.InRepair),
            new("Mai compactor Weber SRV 590 62kg", StockCategory.PowerTools, "SC-017", "SN-WBR-11092", "buc", 2, 8500.00m, "Arabesque", StockCondition.Good),
            new("Placă compactoare Bomag BVP 18/45", StockCategory.PowerTools, "SC-018", "SN-BMG-44210", "buc", 1, 7900.00m, "Arabesque", StockCondition.Good),
            new("Pistol cu aer cald Bosch GHG 20-63", StockCategory.PowerTools, "SC-019", "SN-BSH-11029", "buc", 4, 380.00m, "Dedeman", StockCondition.New),
            new("Mașină profesională tăiat gresie Rubi TZ-1020", StockCategory.PowerTools, "SC-020", "SN-RUB-88190", "buc", 2, 2850.00m, "Dedeman", StockCondition.New),

            // === UNELTE (Hand Tools) - 20 items ===
            new("Trusă chei tubulare Bahco 106 piese", StockCategory.HandTools, "UN-001", null, "trusă", 4, 680.00m, "Dedeman", StockCondition.Good),
            new("Ciocan dulgher Stanley FatMax 600g", StockCategory.HandTools, "UN-002", null, "buc", 12, 145.00m, "Dedeman", StockCondition.New),
            new("Baros forjat Stanley 5kg coadă fibră sticlă", StockCategory.HandTools, "UN-003", null, "buc", 6, 185.00m, "Hornbach", StockCondition.Good),
            new("Daltă profesională Rennsteig SDS 300mm", StockCategory.HandTools, "UN-004", null, "buc", 15, 68.00m, "Arabesque", StockCondition.Used),
            new("Clește tăiat fier beton Knipex 600mm", StockCategory.HandTools, "UN-005", null, "buc", 4, 320.00m, "Dedeman", StockCondition.Good),
            new("Clește fierar-betonist Knipex 250mm", StockCategory.HandTools, "UN-006", null, "buc", 10, 115.00m, "Dedeman", StockCondition.New),
            new("Nivelă cu bulă profesională Stabila R300 120cm", StockCategory.HandTools, "UN-007", null, "buc", 6, 280.00m, "Hornbach", StockCondition.New),
            new("Nivelă cu bulă Stabila 200cm", StockCategory.HandTools, "UN-008", null, "buc", 3, 390.00m, "Hornbach", StockCondition.Good),
            new("Ruletă profesională Stanley Tylon 8m", StockCategory.HandTools, "UN-009", null, "buc", 20, 48.00m, "Dedeman", StockCondition.New),
            new("Ruletă topografică bandă fibră 50m", StockCategory.HandTools, "UN-010", null, "buc", 5, 120.00m, "Arabesque", StockCondition.Good),
            new("Fierăstrău manual Bahco Superior 550mm", StockCategory.HandTools, "UN-011", null, "buc", 8, 95.00m, "Dedeman", StockCondition.Good),
            new("Rindea manuală lemn Stanley No. 4", StockCategory.HandTools, "UN-012", null, "buc", 3, 220.00m, "Dedeman", StockCondition.Used),
            new("Mistrie triunghiulară inox Marshalltown 180mm", StockCategory.HandTools, "UN-013", null, "buc", 14, 85.00m, "Arabesque", StockCondition.Good),
            new("Mistrie trapezoidală zidărie 160mm", StockCategory.HandTools, "UN-014", null, "buc", 12, 45.00m, "Dedeman", StockCondition.New),
            new("Gletieră dințată inox 10mm Bihui", StockCategory.HandTools, "UN-015", null, "buc", 16, 65.00m, "Dedeman", StockCondition.New),
            new("Gletieră netedă inox 280mm Kubala", StockCategory.HandTools, "UN-016", null, "buc", 18, 55.00m, "Arabesque", StockCondition.Good),
            new("Dric poliuretan 400x180mm pentru tencuială", StockCategory.HandTools, "UN-017", null, "buc", 10, 32.00m, "Dedeman", StockCondition.Used),
            new("Pistol profesional pentru cartușe Tajima Convoy", StockCategory.HandTools, "UN-018", null, "buc", 8, 110.00m, "Hornbach", StockCondition.Good),
            new("Foarfecă tablă Erdi Bessey dreapta", StockCategory.HandTools, "UN-019", null, "buc", 5, 160.00m, "Dedeman", StockCondition.Good),
            new("Cheie reglabilă franceză Irega 12 țoli", StockCategory.HandTools, "UN-020", null, "buc", 6, 175.00m, "Dedeman", StockCondition.Good),

            // === ECHIPAMENT DE PROTECȚIE - 20 items ===
            new("Cască de protecție JSP Evo 3 galbenă", StockCategory.ProtectiveEquipment, "EP-001", null, "buc", 25, 42.00m, "Arabesque", StockCondition.New),
            new("Cască de protecție JSP Evo 3 albă (ingineri)", StockCategory.ProtectiveEquipment, "EP-002", null, "buc", 15, 45.00m, "Arabesque", StockCondition.New),
            new("Ham de siguranță complet Delta Plus cu centură", StockCategory.ProtectiveEquipment, "EP-003", null, "buc", 8, 380.00m, "Dedeman", StockCondition.Good),
            new("Ochelari de protecție 3M SecureFit fumurii", StockCategory.ProtectiveEquipment, "EP-004", null, "buc", 30, 28.00m, "Arabesque", StockCondition.New),
            new("Ochelari de protecție 3M transparenți", StockCategory.ProtectiveEquipment, "EP-005", null, "buc", 35, 25.00m, "Arabesque", StockCondition.New),
            new("Antifoane externe pliabile Peltor Optime II", StockCategory.ProtectiveEquipment, "EP-006", null, "buc", 12, 115.00m, "Dedeman", StockCondition.Good),
            new("Mască respirat 3M seria 6000 filtre ABEK1", StockCategory.ProtectiveEquipment, "EP-007", null, "buc", 10, 185.00m, "Dedeman", StockCondition.New),
            new("Bocanci protecție S3 Uvex 1 sport mărimea 42", StockCategory.ProtectiveEquipment, "EP-008", null, "perechi", 8, 290.00m, "Arabesque", StockCondition.New),
            new("Bocanci protecție S3 Uvex 1 sport mărimea 43", StockCategory.ProtectiveEquipment, "EP-009", null, "perechi", 10, 290.00m, "Arabesque", StockCondition.New),
            new("Bocanci protecție S3 Uvex 1 sport mărimea 44", StockCategory.ProtectiveEquipment, "EP-010", null, "perechi", 6, 290.00m, "Arabesque", StockCondition.New),
            new("Veste reflectorizante galbene clasa 2", StockCategory.ProtectiveEquipment, "EP-011", null, "buc", 50, 15.00m, "Dedeman", StockCondition.New),
            new("Mănuși protecție nitril Ansell HyFlex", StockCategory.ProtectiveEquipment, "EP-012", null, "perechi", 60, 14.50m, "Arabesque", StockCondition.New),
            new("Mănuși termoizolante iarnă Delta Plus", StockCategory.ProtectiveEquipment, "EP-013", null, "perechi", 30, 28.00m, "Arabesque", StockCondition.New),
            new("Vizieră rabatabilă protecție polizare JSP", StockCategory.ProtectiveEquipment, "EP-014", null, "buc", 8, 65.00m, "Dedeman", StockCondition.Good),
            new("Cizme electroizolante cauciuc 20kV", StockCategory.ProtectiveEquipment, "EP-015", null, "perechi", 4, 340.00m, "Arabesque", StockCondition.Good),
            new("Pelerină de ploaie impermeabilă PVC galbenă", StockCategory.ProtectiveEquipment, "EP-016", null, "buc", 20, 38.00m, "Dedeman", StockCondition.New),
            new("Salopetă lucru cu pieptar Cerva Max", StockCategory.ProtectiveEquipment, "EP-017", null, "buc", 16, 145.00m, "Arabesque", StockCondition.New),
            new("Costum protecție chimică unică folosință Tyvek", StockCategory.ProtectiveEquipment, "EP-018", null, "buc", 25, 48.00m, "Dedeman", StockCondition.New),
            new("Centură poziționare lucru la înălțime Beal", StockCategory.ProtectiveEquipment, "EP-019", null, "buc", 6, 290.00m, "Dedeman", StockCondition.Good),
            new("Opritor de cădere retractabil 10m Petzl", StockCategory.ProtectiveEquipment, "EP-020", null, "buc", 4, 890.00m, "Dedeman", StockCondition.Good),

            // === COFRAJE (Formwork) - 20 items ===
            new("Panou de cofrare Doka 3-SO 2000x500x21mm", StockCategory.Formwork, "CF-001", null, "buc", 40, 165.00m, "Doka România", StockCondition.Good),
            new("Panou de cofrare Doka 3-SO 2500x500x21mm", StockCategory.Formwork, "CF-002", null, "buc", 35, 195.00m, "Doka România", StockCondition.Good),
            new("Grindă de lemn Doka H20 Top 2.90m", StockCategory.Formwork, "CF-003", null, "buc", 50, 85.00m, "Doka România", StockCondition.Good),
            new("Grindă de lemn Doka H20 Top 3.90m", StockCategory.Formwork, "CF-004", null, "buc", 40, 115.00m, "Doka România", StockCondition.Good),
            new("Pop telescopic metalic Doka Eurex 20 300 (3m)", StockCategory.Formwork, "CF-005", null, "buc", 60, 210.00m, "Doka România", StockCondition.Good),
            new("Pop telescopic metalic Doka Eurex 20 400 (4m)", StockCategory.Formwork, "CF-006", null, "buc", 45, 260.00m, "Doka România", StockCondition.Used),
            new("Furcă simplă de sprijin pentru popi Doka", StockCategory.Formwork, "CF-007", null, "buc", 55, 38.00m, "Doka România", StockCondition.Good),
            new("Trepied zincat rabatabil pentru popi metalici", StockCategory.Formwork, "CF-008", null, "buc", 30, 140.00m, "Doka România", StockCondition.Good),
            new("Clemă rapidă montaj panouri Doka Frami", StockCategory.Formwork, "CF-009", null, "buc", 80, 48.00m, "Doka România", StockCondition.Good),
            new("Clește zincat de strângere cofrare Rapid", StockCategory.Formwork, "CF-010", null, "buc", 60, 32.00m, "Arabesque", StockCondition.Good),
            new("Bară tirant cofrare Dywidag 15mm lungime 1.5m", StockCategory.Formwork, "CF-011", null, "buc", 70, 45.00m, "Doka România", StockCondition.Good),
            new("Piuliță fluture zincată Dywidag 15/100mm", StockCategory.Formwork, "CF-012", null, "buc", 100, 24.00m, "Doka România", StockCondition.Good),
            new("Țeavă distanțier PVC cofrare 22mm (bară 2m)", StockCategory.Formwork, "CF-013", null, "buc", 150, 9.50m, "Arabesque", StockCondition.New),
            new("Conuri etanșare capăt distanțier PVC 22mm (sac)", StockCategory.Formwork, "CF-014", null, "saci", 15, 65.00m, "Arabesque", StockCondition.New),
            new("Ulei decofrol mineral Doka Optix bidon 25L", StockCategory.Formwork, "CF-015", null, "bidoane", 8, 380.00m, "Doka România", StockCondition.New),
            new("Distanțieri plastic tip fibră beton 25/30mm (sac)", StockCategory.Formwork, "CF-016", null, "saci", 20, 85.00m, "Arabesque", StockCondition.New),
            new("Panou tristrat Doka 1500x500x21mm", StockCategory.Formwork, "CF-017", null, "buc", 30, 125.00m, "Doka România", StockCondition.Used),
            new("Colțar exterior cofrare Frami 1.20m", StockCategory.Formwork, "CF-018", null, "buc", 12, 340.00m, "Doka România", StockCondition.Good),
            new("Echer fixare grindă cofraje H20", StockCategory.Formwork, "CF-019", null, "buc", 25, 55.00m, "Doka România", StockCondition.Good),
            new("Consolă betonare pentru platformă Doka", StockCategory.Formwork, "CF-020", null, "buc", 16, 280.00m, "Doka România", StockCondition.Good),

            // === CONSUMABILE (Consumables) - 20 items ===
            new("Disc debitat metal Kronenflex 230x2.0mm", StockCategory.Consumables, "CS-001", null, "buc", 80, 12.50m, "Wurth", StockCondition.New),
            new("Disc diamantat universal beton Bosch Pro 230mm", StockCategory.Consumables, "CS-002", null, "buc", 15, 145.00m, "Dedeman", StockCondition.New),
            new("Disc debitat inox Klingspor 125x1.0mm", StockCategory.Consumables, "CS-003", null, "buc", 120, 5.80m, "Wurth", StockCondition.New),
            new("Set burghie SDS-plus Bosch 7 piese (5-12mm)", StockCategory.Consumables, "CS-004", null, "seturi", 10, 85.00m, "Dedeman", StockCondition.New),
            new("Burghiu SDS-max 28x570mm beton Makita", StockCategory.Consumables, "CS-005", null, "buc", 4, 280.00m, "Makita Store", StockCondition.New),
            new("Spumă poliuretanică montaj Soudafoam Gun 750ml", StockCategory.Consumables, "CS-006", null, "tuburi", 48, 28.50m, "Arabesque", StockCondition.New),
            new("Silicon sanitar Ceresit CS 25 alb 280ml", StockCategory.Consumables, "CS-007", null, "tuburi", 36, 22.00m, "Dedeman", StockCondition.New),
            new("Ancoră chimică bicomponentă Hilti HIT-HY 200 330ml", StockCategory.Consumables, "CS-008", null, "tuburi", 24, 165.00m, "Hilti România", StockCondition.New),
            new("Tije filetate zincate M12 clasa 8.8 (1m)", StockCategory.Consumables, "CS-009", null, "buc", 60, 11.20m, "Wurth", StockCondition.New),
            new("Tije filetate zincate M16 clasa 8.8 (1m)", StockCategory.Consumables, "CS-010", null, "buc", 40, 18.50m, "Wurth", StockCondition.New),
            new("Cuie construcții cap conic 100mm (cutie 5kg)", StockCategory.Consumables, "CS-011", null, "cutii", 25, 45.00m, "Arabesque", StockCondition.New),
            new("Cuie dulgherie răsucite 80mm (cutie 5kg)", StockCategory.Consumables, "CS-012", null, "cutii", 20, 52.00m, "Arabesque", StockCondition.New),
            new("Sârmă neagră legat armături 1.2mm (bobină 20kg)", StockCategory.Consumables, "CS-013", null, "bobine", 15, 135.00m, "Arabesque", StockCondition.New),
            new("Bandă avertizoare alb-roșie 500m", StockCategory.Consumables, "CS-014", null, "role", 10, 38.00m, "Dedeman", StockCondition.New),
            new("Folie polietilenă impermeabilă construcții 100mp", StockCategory.Consumables, "CS-015", null, "role", 8, 125.00m, "Arabesque", StockCondition.New),
            new("Sac ciment Holcim Structo 40kg", StockCategory.Consumables, "CS-016", null, "saci", 50, 34.00m, "Arabesque", StockCondition.New),
            new("Mortar hidroizolant Ceresit CR 65 sac 25kg", StockCategory.Consumables, "CS-017", null, "saci", 20, 68.00m, "Dedeman", StockCondition.New),
            new("Spray vopsea marcare topografică neon 500ml", StockCategory.Consumables, "CS-018", null, "tuburi", 24, 29.00m, "Hornbach", StockCondition.New),
            new("Pânze fierăstrău sabie Bosch metal/lemn (set 5)", StockCategory.Consumables, "CS-019", null, "seturi", 12, 65.00m, "Dedeman", StockCondition.New),
            new("Șuruburi autofiletante Rigips 3.5x25mm (cutie 1000)", StockCategory.Consumables, "CS-020", null, "cutii", 15, 38.00m, "Arabesque", StockCondition.New),
        };
    }
}
